using CloudLib;

namespace Server.src;

internal class ListenerInstance
{
    private static ListenerInstance? _instance;
    public static ListenerInstance Instance {
        get {
            if (_instance == null) {
                _instance = new ListenerInstance();
                Console.WriteLine("Listener instantiated by thread " + Thread.CurrentThread.ManagedThreadId);
            }
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
        _idToGiveToAuthManager = ServerConstants.AUTH_MANAGER_BASE_ID;
        _idToGiveToUser = ServerConstants.USER_BASE_ID;
    }

    private readonly TcpListener _tcpListener;


    /// <summary>
    /// Thread:  Main thread
    /// </summary>
    public void Start()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        TimeSpan AuthManagerCleanupTime = TimeSpan.FromSeconds(ServerConstants.AUTH_MANAGER_CLEANUP_TIMEFRAME_SECONDS);
        _tcpListener.Start();
        while (true) {
            TcpClient newTcpClient = _tcpListener.AcceptTcpClient();
            NetworkStream newClientStream = newTcpClient.GetStream();

            _idToGiveToUser += 1;
            ConnectionResources newClientResources = new (newTcpClient, newClientStream, _idToGiveToUser);

            try {
                AddToAuthenticationQueue(newClientResources);

                if (stopwatch.Elapsed.TotalSeconds > AuthManagerCleanupTime.TotalSeconds) {

                    // TODO DLC: make this run asynchronously, and protect the managers via a lock.
                    Console.WriteLine("Listener thread is going to cleanup the AuthenticationManagerStack.");
                    CleanupAuthenticationManagerStack();
                    Console.WriteLine("Listener thread has SUCCESSFULLY cleaned the AuthenticationManagerStack.");
                    stopwatch.Restart();
                }

            }
            catch (IOException) {
                Console.WriteLine("Main thread: User disconnected while being placed in an AuthenticationQueue");
            }
        }
    }





    // --- private variables --- //
    // First off, a list of AuthenticationManagers.
    private List<AuthenticationManager> _authenticationManagers;
    private int _idToGiveToAuthManager;
    private int _idToGiveToUser;
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
                _authenticationManagers.Add(new AuthenticationManager(_idToGiveToAuthManager));
                AddToAuthenticationQueue(resources);
                _idToGiveToAuthManager += ServerConstants.AUTH_MANAGER_ID_INCREMENT;
                return;
            }
            else {
                SMail.SendFlag(resources!.stream!, ServerFlag.OVERLOADED);
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