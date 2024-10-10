#define STATE_PRINTING

using CloudLib;
using static CloudLib.SenderReceiver;

namespace Client.src;


class ClientInstance
{
    public static ClientInstance Instance()
    {
        if (_instance == null) {
            _instance = new ClientInstance();
        }
        return _instance!;
    }

    public void Start()
    {
        RunAuthenticatorStateMachine();
    }


    private static ClientInstance? _instance;
    private UserData _clientData;
    private ServerData _serverData;
    private ClientStates _clientState;

#if STATE_PRINTING
    private void DPrintState()
    {
        Console.WriteLine(_clientState);
    }
#endif

    private void SetUsername(string newUsername)
    {
        if (newUsername == "") {
            throw new WrongUsernameException("Username was \"\"");
        }
        _clientData.Username = newUsername;
    }

    private ClientInstance()
    {
        _clientData = new UserData {
            Username = "",
            LoginAttempts = 0,
            RegistrationAttempst = 0
        };
        _serverData = new ServerData {
            TcpClient = null,
            Stream = null
        };
    }

    private void CleanupConnectionResources()
    {
        _serverData.TcpClient?.Dispose();
        _serverData.Stream?.Dispose();
    }

    private void RunAuthenticatorStateMachine()
    {
        try {
            while (true) {
                DPrintState();
                switch (_clientState) {
                    case ClientStates.NOT_CONNECTED:
                        ConnectToServer();
                        break;
                    case ClientStates.CONNECTED:
                        WaitForAuthenticationHelper();
                        break;
                    default:
                        throw new InvalidStateTransitionException($"Invalid state in RunAuthenticatorStateMachine: {_clientState}");
                }
            }
        }
        catch (IOException e) {
            Console.WriteLine("IO Exception caught in RunAuthenticatorStateMachine: " + e.Message);
            _clientState = ClientStates.EXITING_PROGRAM;
            CleanupConnectionResources();
            throw new ExitProgram("IO Exception.");
        }
    }

    private void ConnectToServer()
    {
        Console.WriteLine("Connecting to the server...");
        IPEndPoint ipEndPoint = new(ServerConstants.SERVER_IP, ServerConstants.SERVER_PORT);
        _serverData.TcpClient = new TcpClient();
        _serverData.TcpClient.Connect(ipEndPoint);
        _serverData.Stream = _serverData.TcpClient.GetStream();
        _clientState = ClientStates.CONNECTED;
    }
    
    private void WaitForAuthenticationHelper(){
        // Self explanatory. Wait for server to send AUTHENTICATOR_HELPER_ASSIGNED
        (ServerFlags serverFlag, byte[] payload) = ClientReceiveMessage(_serverData.Stream!);
        if (serverFlag == ServerFlags.OK){

        }
        // TODO Next Session
    }
}