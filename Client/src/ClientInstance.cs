#define STATE_PRINTING

using CloudLib;
using System.Timers;
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
    private UserData _userData;
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
        _userData.Username = newUsername;
    }

    private ClientInstance()
    {
        _userData = new UserData {
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
                    case ClientStates.ASSIGNED:
                        ReceiveAuthChoice();
                        break;

                    case ClientStates.EXITING_PROGRAM:
                        CleanupConnectionResources();
                        return;
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

    private async void WaitForAuthenticationHelper()
    {
        // TODO Before Dashboard: Use asynchronous receive, pass in cancellation token, invoke the cancellation
        // when System.Timers.Timer() is done. For this, implemen timeout for 5 seconds, then call Cleanup-like 
        // function to exit etc.  etc. 
        CancellationTokenSource source = new(5000);
        CancellationToken token = source.Token;
        try {

            while (true) {
                (ServerFlags serverFlag, byte[] payload) = await ClientReceiveMessageCancellable(_serverData.Stream!, token);
                if (serverFlag == ServerFlags.AUTHENTICATOR_HELPER_ASSIGNED) {
                    Debug.Assert(payload.Count() == 0);
                    Console.WriteLine("You are connected to the server!");
                    _clientState = ClientStates.CONNECTED;
                    return;
                }
                else if (serverFlag == ServerFlags.QUEUE_POSITION) {
                    Debug.Assert(payload.Count() > 0);
                    string currentPosition = Encoding.UTF8.GetString(payload);
                    Console.WriteLine("Position in queue: " + currentPosition);
                }
            }
        }
        catch (OperationCanceledException) {
            Console.WriteLine("Server timeout - Try again at a later time.");
            _clientState = ClientStates.EXITING_PROGRAM;
            return;
        }
    }
    private void ReceiveAuthChoice()
    {
        while (true) {
            Console.WriteLine("login: l ||| register: r");
            string? userResponse = Console.ReadLine();
            if (userResponse == null) {
                Console.WriteLine("Failed to receive input. Try again.");
                _clientState = ClientStates.EXITING_PROGRAM;
                return;
            }

            if (userResponse == "l") {
                ProcessLogin();
                return;
            }
            else if (userResponse == "r") {
                ProcessRegistration();
                return;
            }
            else {
                Console.WriteLine("Invalid choice.");
            }
        }
    }

    private void ProcessLogin()
    {
        // Check for timeout message from server
        // Check for login attempts on client side
        // Make sure the server also checks for login attempts, and closes the connection if too many. Doesn't need to send flag. Let hacked clients break. 
        _userData.LoginAttempts += 1;
        _clientState = ClientStates.ASSIGNED; // if something went wrong.
    }

    private void ProcessRegistration()
    {
        // Check for timeout message from server
        _clientState = ClientStates.ASSIGNED; // if success or if failure.
    }
}

