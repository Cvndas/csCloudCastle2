#define STATE_PRINTING

using CloudLib;
using static CloudLib.SenderReceiver;

namespace Client.src;

/// <summary>
/// The class which manages the User's experience. 
/// </summary>
class ClientInstance
{
    public static ClientInstance Instance()
    {
        if (_instance == null) {
            _instance = new ClientInstance();
        }
        return _instance!;
    }

    private ClientInstance()
    {
        _personalData = new PersonalData {
            Username = "",
            LoginAttempts = 0,
            RegistrationAttempst = 0
        };
    }

    public void Start()
    {
        try {

            RunAuthenticatorStateMachine();
        }
        catch (ExitingProgramException) {
            Console.WriteLine("Exiting program.");
            _connectionResources?.Cleanup();
        }
        return;
    }

    // ------------------- VARIABLES -------------------------- // 

    private static ClientInstance? _instance;
    private PersonalData _personalData;
    private ConnectionResources? _connectionResources;
    private ClientStates _clientState;

    // -------------------------------------------------------- // 

    private void DPrintState()
    {
#if STATE_PRINTING
        Console.WriteLine(_clientState);
#endif
    }

    private void
    SetUsername(string newUsername)
    {
        if (newUsername == "") {
            throw new WrongUsernameException("Username was \"\"");
        }
        _personalData.Username = newUsername;
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

                    default:
                        throw new InvalidStateTransitionException
                                  ($"Invalid state in RunAuthenticatorStateMachine: {_clientState}");
                }
            }
        }
        catch (IOException e) {
            Console.WriteLine("IO Exception caught in RunAuthenticatorStateMachine: " + e.Message);
            throw new ExitingProgramException("Caught IO Exception in RunAuthenticatorStateMachine");
        }
    }

    private void
    ConnectToServer()
    {
        Console.WriteLine("Connecting to the server...");
        IPEndPoint ipEndPoint = new(ServerConstants.SERVER_IP, ServerConstants.SERVER_PORT);
        TcpClient tcpClient = new TcpClient();
        tcpClient.Connect(ipEndPoint);
        NetworkStream stream = tcpClient.GetStream();
        _connectionResources = new ConnectionResources() { TcpClient = tcpClient, Stream = stream };

        _clientState = ClientStates.CONNECTED;
    }

    private async void
    WaitForAuthenticationHelper()
    {
        // TODO - Test, before marking the authentication as complete.
        CancellationTokenSource source = new(5000);
        CancellationToken token = source.Token;
        try {

            while (true) {
                (ServerFlags serverFlag, byte[] payload) =
                    await ClientReceiveMessageCancellable(_connectionResources!.Stream!, token);

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
                Console.WriteLine("Failed to receive input");
                throw new ExitingProgramException("Failed to receive input in ReceiveAuthChoice");
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
        // Make sure the server also checks for login attempts, and closes the connection if too many. Doesn't need 
        // to send flag. Let hacked clients break. 
        _personalData.LoginAttempts += 1;
        _clientState = ClientStates.ASSIGNED; // if something went wrong.
    }

    private void ProcessRegistration()
    {
        // Check for timeout message from server
        _clientState = ClientStates.ASSIGNED; // if success or if failure.
    }
}

