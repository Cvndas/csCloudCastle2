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
    }

    private readonly TcpListener _tcpListener;


    /// <summary>
    /// Thread:  Main thread
    /// </summary>
    public void Start()
    {
        while (true) {
            _tcpListener.AcceptTcpClient();
            // TODO FIRST -> Accept a TCP connection, see if auth manager needs to be made,
            // then add it to its queue. 
        }
    }



    // --- private variables --- //
    // First off, a STACK of AuthenticationManagers.
    // ----------------- // 

    // --- Private methods --- // 
    // ----------------------- //

    public void AddNewAuthenticationManager()
    {
        // TODO 
    }

    public void FindAvailableAuthenticationManager()
    {
        // TODO
    }

    public void AddToAuthenticationQueue(ConnectionResources resources)
    {
        // TODO
    }
}