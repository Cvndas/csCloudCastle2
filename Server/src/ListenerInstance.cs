using CloudLib;

namespace Server.src;

internal class ListenerInstance
{
    private static ListenerInstance? _instance;
    public static ListenerInstance Instance {
        get {
            _instance ??= new ListenerInstance();
            return _instance;
        }
    }

    public void StopListening()
    {
        _tcpListener?.Stop();
    }

    private ListenerInstance()
    {
        _tcpListener = new(ServerConstants.SERVER_IP, ServerConstants.SERVER_PORT);
        _authenticationManagers = new List<AuthenticationManager>();
    }

    private readonly TcpListener _tcpListener;


    /// <summary>
    /// Thread:  Main thread
    /// </summary>
    public void Start()
    {
        Stopwatch stopwatch = new Stopwatch();
        TimeSpan AuthManagerCleanupTime = TimeSpan.FromSeconds(20);
        while (true) {
            TcpClient newTcpClient = _tcpListener.AcceptTcpClient();
            NetworkStream newClientStream = newTcpClient.GetStream();
            ConnectionResources newClientResources = new ConnectionResources {
                TcpClient = newTcpClient,
                Stream = newClientStream
            };

            try {
                AddToAuthenticationQueue(newClientResources);

                if (stopwatch.Elapsed.TotalSeconds > AuthManagerCleanupTime.TotalSeconds) {
                    // TODO Future: make this run asynchronously, and protect the managers via a lock.
                    WriteLine("Listener thread is going to cleanup the AuthenticationManagerStack.");
                    CleanupAuthenticationManagerStack();
                    WriteLine("Listener thread has SUCCESSFULLY cleaned the AuthenticationManagerStack.");
                    stopwatch.Restart();
                }

            }
            catch (IOException) {
                WriteLine("Main thread: User disconnected while being placed in an AuthenticationQueue");
            }
        }
    }





    // --- private variables --- //
    // First off, a list of AuthenticationManagers.
    private List<AuthenticationManager> _authenticationManagers;
    // ----------------- // 




    // --- Private methods --- // 
    // ----------------------- //

    private void AddToAuthenticationQueue(ConnectionResources resources)
    {
        bool resourcesDeposited = false;

        int i = 0;
        for (i = 0; i < _authenticationManagers.Count; i++) {
            resourcesDeposited = _authenticationManagers.ElementAt(i).TryToEnqueueUser(resources);
            if (resourcesDeposited) {
                break;
            }
        }

        if (resourcesDeposited) {
            return;
        }
        // If the user hasn't been added to any queues...
        else {
            int managerCount = _authenticationManagers.Count;
            if (managerCount < ServerConstants.MAX_AUTHENTICATION_MANAGERS) {
                _authenticationManagers.Add(new AuthenticationManager(i * 1000));
                AddToAuthenticationQueue(resources);
                return;
            }
            else {
                ServerSendFlag(resources!.Stream!, ServerFlags.OVERLOADED);
            }
        }
    }

    private void CleanupAuthenticationManagerStack()
    {
        // This could very well be bugged. While loop is a better strategy for sure. 
        for (int i = 0; i < _authenticationManagers.Count; i++) {
            if (_authenticationManagers[i].DisposeIfNobodyIsWorking()) {
                _authenticationManagers.RemoveAt(i);
                i -= 1; // The list shifts left after removal, so compensate for that.
            }
        }
    }



    private void PopAuthenticationManagerStack()
    {

    }
}