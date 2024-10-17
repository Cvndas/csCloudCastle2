using CloudLib;

namespace Server.src;

internal class AuthenticationManager
{
    public AuthenticationManager(int id)
    {
        CR_freeAuthHelpers = new(ServerConstants.HELPERS_PER_AUTHENTICATION_MANAGER);
        _freeAuthHelpersLock = new object();

        CR_clientQueue = new Queue<ConnectionResources>(ServerConstants.MAX_USERS_IN_AUTHENTICATION_QUEUE);
        _clientQueueLock = new object();

        _tokenSource = new CancellationTokenSource();
        _token = _tokenSource.Token;

        for (int i = 0; i < ServerConstants.HELPERS_PER_AUTHENTICATION_MANAGER; i++) {
            CR_freeAuthHelpers.Enqueue(new AuthenticationHelper(id + i, _token));
        }

        _authenticationManagerThread = new Thread(() => AuthenticationManagerJob());
        _authenticationManagerThread.Start();

        _id = id;

        return;
    }

    /// <summary>
    /// True upon success <br/> False if queue was full.
    /// </summary>
    public bool TryToEnqueueUser(ConnectionResources resources)
    {
        bool ret = false;
        lock (_clientQueueLock) {
            if (CR_clientQueue.Count < ServerConstants.MAX_USERS_IN_AUTHENTICATION_QUEUE) {
                CR_clientQueue.Enqueue(resources);
                Monitor.Pulse(_clientQueueLock);
                ret = true;
            }
        }
        return ret;
    }

    /// <summary>
    /// Thread: Listener
    /// </summary>
    public bool DisposeIfNobodyIsWorking()
    {
        bool ret = false;
        lock (_clientQueueLock) {
            lock (_freeAuthHelpersLock) {
                bool noClientsInQueue = (CR_clientQueue.Count == 0);
                bool allHelpersAreFree = (CR_freeAuthHelpers.Count == ServerConstants.HELPERS_PER_AUTHENTICATION_MANAGER);
                if (noClientsInQueue && allHelpersAreFree) {


                    // +++++++++++++++++++++++++++ DEADLOCK RISK  ++++++++++++++++++++++++++++++++++++ // 
                    // ++ Should trigger all the helpers and the Manager to quit their, jobs,       ++ //
                    // ++ and all the helpers should set their connection resources record to null. ++ //
                    _tokenSource.Cancel();

                    // The auth helpers will be waiting for a signal that they can start working. 
                    // Wake them up, and they'll find that the _tokenSource was invoked.
                    foreach (var helper in CR_freeAuthHelpers) {
                        helper.PulseToJoinThread();
                    }
                    DisposeOfSelf();
                    // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++ // 


                    ret = true;
                }
                else {
                    Console.WriteLine($"AuthenticationManager: {_id} was not free, and has not been destroyed.");
                }
            }
        }
        return ret;
    }


    // ----------- Private data ------------- // 
    private CancellationTokenSource _tokenSource;
    private readonly CancellationToken _token;
    private Thread _authenticationManagerThread;
    private int _id;
    // -------------------------------------- //

    // ----------- CRITICAL ----------------- // 
    private readonly Queue<AuthenticationHelper> CR_freeAuthHelpers;
    private readonly object _freeAuthHelpersLock;
    // -------------------------------------- // 

    // ----------- CRITICAL ----------------- // 
    /// <summary>
    /// Thread: Listener: TryToEnqueueUser(), lock (no wait, no pulse)<br/>
    /// Thread: ?
    /// </summary>
    private Queue<ConnectionResources> CR_clientQueue;
    private readonly object _clientQueueLock;
    // -------------------------------------- // 


    // -------------------------------------- // 

    private void AuthenticationManagerJob()
    {
        while (true) {
            AuthenticationHelper? helper;
            lock (_freeAuthHelpersLock) {
                if (CR_freeAuthHelpers.Count > 0) {
                    helper = CR_freeAuthHelpers.Dequeue();
                }
                else {
                    Monitor.Wait(_freeAuthHelpersLock);
                    // Woken up either because of cancellation token, or because there is a client
                    CR_freeAuthHelpers.TryDequeue(out helper);
                }
            }
            Console.WriteLine("A Helper is ready.");

            if (_token.IsCancellationRequested) {
                break;
            }
            Debug.Assert(helper != null);

            ConnectionResources? clientResources;

            lock (_clientQueueLock) {
                if (CR_clientQueue.Count > 0) {
                    clientResources = CR_clientQueue.Dequeue();
                }
                else {
                    Monitor.Wait(_clientQueueLock);
                    // Woken up either because of cancellation token, or because there is a client
                    CR_clientQueue.TryDequeue(out clientResources);
                }
            }
            Console.WriteLine("A client has been chosen.");

            if (_token.IsCancellationRequested) {
                break;
            }
            Debug.Assert(clientResources != null);

            Console.WriteLine("About to assign client");
            helper.AssignClient(clientResources!);
            Console.WriteLine("Assigned client");

        }
        Console.WriteLine($"AuthenticationManager: {_id} has exited the Job, and is ready to be joined.");
    }

    private void DisposeOfSelf()
    {
        Console.WriteLine($"AuthenticationManager: {_id} has joined his Job thread.");
        Debug.Assert(_token.IsCancellationRequested);
        lock (_clientQueueLock) {
            Monitor.Pulse(_clientQueueLock);
        }
        lock (_freeAuthHelpersLock) {
            Monitor.Pulse(_clientQueueLock);
        }
        _authenticationManagerThread.Join();
        Console.WriteLine($"AuthenticationManager: {_id} has joined his Job thread.");
    }
}