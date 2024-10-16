using CloudLib;
namespace Client.src;


// +++++++++++ AUTHENTICATION PROCESS +++++++++++++++ //
internal partial class ClientInstance
{

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
                    case ClientStates.LOGIN_CHOSEN:
                        ProcessLogin();
                        break;
                    case ClientStates.REGISTRATION_CHOSEN:
                        ProcessRegistration();
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


    private void WaitForAuthenticationHelper()
    {
        CancellationTokenSource source = new(ServerConstants.AUTH_ASSIGNMENT_TIMEOUT_SECONDS * 1000);
        CancellationToken token = source.Token;

        while (true) {
            (ServerFlags? serverFlag, byte[] payload) =
                CMail.ReceiveMessageCancellable(_connectionResources!.Stream!, token);


            // --------- SUCCESS CASE --------------- //
            if (serverFlag == ServerFlags.AUTHENTICATOR_HELPER_ASSIGNED) {
                Debug.Assert(payload.Count() == 0);
                Console.WriteLine("You are connected to the server!");
                _clientState = ClientStates.ASSIGNED;
                return;
            }
            // -------------------------------------- // 

            else if (serverFlag == ServerFlags.QUEUE_POSITION) {
                Debug.Assert(payload.Count() > 0);
                string currentPosition = Encoding.UTF8.GetString(payload);
                Console.WriteLine("Position in queue: " + currentPosition);
            }



            // Error checking
            if (serverFlag == ServerFlags.DISCONNECTION) {
                throw new IOException("Disconnected from server in WaitForAuthenticationHelper().");
            }
            if (serverFlag == ServerFlags.READ_CANCELED) {
                Console.WriteLine("Server timeout - Try again later.");
                throw new ExitingProgramException("Server timeout in WaitForAuthenticationHelper()");
            }
            if (serverFlag == ServerFlags.OVERLOADED) {
                Console.WriteLine("The server is overloaded. Try again later.");
                throw new ExitingProgramException("The server was overloaded.");
            }
            else {
                throw new ExitingProgramException("Received unexpected " + serverFlag + " from the server in WaitForAuthenticationHelper()");
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
                CMail.SendFlag(_connectionResources!.Stream!, ClientFlags.LOGIN_INIT);
                _clientState = ClientStates.LOGIN_CHOSEN;
                return;
            }
            else if (userResponse == "r") {
                CMail.SendFlag(_connectionResources!.Stream!, ClientFlags.REGISTRATION_INIT);
                _clientState = ClientStates.REGISTRATION_CHOSEN;
                return;
            }
            else {
                Console.WriteLine("Invalid choice.");
                _personalData.AuthChoiceAttempts += 1;
                if (_personalData.AuthChoiceAttempts > ServerConstants.MAX_AUTH_CHOICE_ATTEMTPS) {
                    Console.WriteLine("Too many attempts were made. Learn to read.");
                    throw new ExitingProgramException("Too many Auth Choice attempts.");
                }
            }
        }
    }

    private void ProcessLogin()
    {
        // Check for login attempts on client side
        // Make sure the server also checks for login attempts, and closes the connection if too many. Doesn't need 
        // to send flag. Let hacked clients break. 
        Debug.Assert(false);

        _personalData.LoginAttempts += 1;
    }

    private void ProcessRegistration()
    {
        // TODO Next - Finish, Client Side
        if (_personalData.AccountsCreated > ServerConstants.MAX_ACCOUNTS_PER_SESSION) {
            Console.WriteLine("You already made " + ServerConstants.MAX_ACCOUNTS_PER_SESSION + " accounts. Just log in, bro.");
            _clientState = ClientStates.CHOOSING_AUTHENTICATION_METHOD;
            return;
        }

        Lazy<string> formatSpecsUsername = new Lazy<string>(() =>
            $"Minimum length: {ServerConstants.MIN_USERNAME_LEN}, maximum length: {ServerConstants.MAX_USERNAME_LEN}"
        );
        Lazy<string> formatSpecsPassword = new Lazy<string>(() =>
            $"Minimum length: {ServerConstants.MIN_PASSWORD_LEN}, maximum length: {ServerConstants.MAX_PASSWORD_LEN}"
        );

        Console.WriteLine("Please provide your credentials in the following format: (NOTE! Passwords are stored in plain text.)");
        Console.WriteLine("[username password]");
        string? userCreds = Console.ReadLine() ?? throw new ExitingProgramException("ProcessRegistration(): User input was null.");

        MessageValidationResult credsFormatEval = MessageValidation.ValidateUsernamePassword(userCreds);
        switch (credsFormatEval) {
            case MessageValidationResult.OK:
                break;
            case MessageValidationResult.USERNAME_TOO_LONG:
                Console.WriteLine("Username was too long: " + formatSpecsUsername);
                _personalData.RegistrationAttempst += 1;
                return;
            case MessageValidationResult.USERNAME_TOO_SHORT:
                Console.WriteLine("Username was too short: " + formatSpecsUsername);
                _personalData.RegistrationAttempst += 1;
                return;
            case MessageValidationResult.PASSWORD_TOO_LONG:
                Console.WriteLine("Password was too long: " + formatSpecsPassword);
                _personalData.RegistrationAttempst += 1;
                return;
            case MessageValidationResult.PASSWORD_TOO_SHORT:
                Console.WriteLine("Password was too short: " + formatSpecsPassword);
                _personalData.RegistrationAttempst += 1;
                return;
            case MessageValidationResult.WRONG_MESSAGE_FORMAT:
                Console.WriteLine("Format was incorrect.");
                _personalData.RegistrationAttempst += 1;
                return;
        }


        CMail.SendString(ClientFlags.REGISTRATION_USERNAME_PASSWORD, _connectionResources!.Stream!, userCreds);
        ServerFlags serverResponseFlag = CMail.ReceiveFlag(_connectionResources!.Stream!);

        if (serverResponseFlag == ServerFlags.DISCONNECTION) {
            Console.WriteLine("Either you disconnected from the server, "
                            + "or you spent too goddamn long on coming up with a username.\n"
                            + "The timelimit is " + ServerConstants.REGISTER_TIMEOUT_SECONDS + "seconds, retard.");
            throw new ExitingProgramException("Disconnection in ProcessRegistration()");
        }
        // ------------ SUCCESS -------------------------------------------- // 
        if (serverResponseFlag == ServerFlags.REGISTRATION_SUCCESSFUL) {
            Console.WriteLine("Account created successfully!");
            _personalData.AccountsCreated += 1;
            // Go back to the screen where you choose between logging in and registering.
            _clientState = ClientStates.CHOOSING_AUTHENTICATION_METHOD;
            return;
        }
        // ------------------------------------------------------------------ //

        else if (serverResponseFlag == ServerFlags.USERNAME_TAKEN) {
            Console.WriteLine("Username was already taken.");
        }
        else if (serverResponseFlag == ServerFlags.DATABASE_ERROR){
            Console.WriteLine("The database experienced an error. Try again some other time.");
            throw new ExitingProgramException("Database error in ProcessRegistration()");
        }

        _personalData.RegistrationAttempst += 1;
        if (_personalData.RegistrationAttempst > ServerConstants.MAX_REGISTRATION_ATTEMPTS) {
            throw new ExitingProgramException("Too many registration attempts made.");
        }
    }
}