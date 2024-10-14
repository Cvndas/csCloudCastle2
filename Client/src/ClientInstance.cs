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
            RegistrationAttempst = 0,
            AuthChoiceAttempts = 0
        };
    }

    public void Start()
    {
        try {
            RunAuthenticatorStateMachine();
        }
        catch (ExitingProgramException e) {
            Console.WriteLine("Exiting program message: " + e.Message);
            _connectionResources?.Cleanup();
        }
        catch (Exception) {
            throw;
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
        Console.WriteLine("STATE: " + _clientState);
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
                        Console.WriteLine("Success? what the fuck is happening?");
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
            throw new ExitingProgramException("Caught IO Exception in RunAuthenticatorStateMachine: " + e.Message);
        }
        catch (InvalidStateTransitionException) {
            throw;
        }
    }

    private void ConnectToServer()
    {
        Console.WriteLine("Connecting to the server...");
        IPEndPoint ipEndPoint = new(ServerConstants.SERVER_IP, ServerConstants.SERVER_PORT);
        TcpClient tcpClient = new TcpClient();
        tcpClient.Connect(ipEndPoint);
        NetworkStream stream = tcpClient.GetStream();
        _connectionResources = new ConnectionResources() { TcpClient = tcpClient, Stream = stream };

        _clientState = ClientStates.CONNECTED;
    }


    // Made this function a bit more complicated to avoid having to mark this, and its callers, as async.
    private void WaitForAuthenticationHelper()
    {
        // 10 second timer before being kicked out. Timer is tested to work.
        CancellationTokenSource source = new(10000);
        CancellationToken token = source.Token;

        while (true) {

            ServerFlags? serverFlag = null;
            byte[]? payload = null;
            object resultIsReadyLock = new object();
            bool resultIsReady = false;

            object errorEvalLock = new object();
            string errorEval = "incomplete";

            // ----------------------------- Reading data from the server ------------------------------------ //
            Task.Run(async () => {
                try {
                    (serverFlag, payload) = await ClientReceiveMessageCancellable(_connectionResources!.Stream!, token);

                    if (serverFlag == ServerFlags.READ_CANCELED) {
                        lock (errorEvalLock) {
                            errorEval = "OPCANCEL";
                            Monitor.Pulse(errorEvalLock);
                        }
                    }
                    else {
                        lock (errorEvalLock) {
                            errorEval = "FINE";
                            Monitor.Pulse(errorEvalLock);
                        }
                        lock (resultIsReadyLock) {
                            resultIsReady = true;
                            Monitor.Pulse(resultIsReadyLock);
                        }
                    }
                }

                catch (IOException) {
                    lock (errorEvalLock) {
                        errorEval = "IOCRASH";
                        Monitor.Pulse(errorEvalLock);
                    }
                }
            });

            // ----------------------------------------------------------------------------------------------- //


            // ------------------------- SYNCHRONIZATION ------------------------------- //
            lock (errorEvalLock) {
                if (errorEval == "incomplete") {
                    Monitor.Wait(errorEvalLock);
                }

                if (errorEval == "IOCRASH") {
                    throw new IOException("Disconnection in WaitForAuthenticationHelper");
                }
                else if (errorEval == "OPCANCEL") {
                    Console.WriteLine("Server timeout - Try again at a later time.");
                    throw new ExitingProgramException("Server timeout.");
                }
                else if (errorEval != "FINE"){
                    throw new ExitingProgramException("Programmer error: Wrong errorEval set in WaitForAuthenticationHelper");
                }
            }

            lock (resultIsReadyLock) {
                if (!resultIsReady) {
                    Monitor.Wait(resultIsReadyLock);
                }
            }

            Debug.Assert(serverFlag != null);
            Debug.Assert(payload != null);
            // ------------------------------------------------------------------------- //


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
            else if (serverFlag == ServerFlags.OVERLOADED) {
                Console.WriteLine("The server is overloaded. Try again later.");
                throw new ExitingProgramException("The server was overloaded.");
            }
            else {
                Console.WriteLine("This is triggered at least.");
                throw new ExitingProgramException("Received invalid flag from Server: " + serverFlag);
            }
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
                _personalData.AuthChoiceAttempts += 1;
                // TODO : Do this server side
                if (_personalData.AuthChoiceAttempts > ServerConstants.MAX_AUTH_CHOICE_ATTEMTPS) {
                    Console.WriteLine("Too many attempts were made. Learn to read.");
                    throw new ExitingProgramException("Too many Auth Choice attempts.");
                }
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

