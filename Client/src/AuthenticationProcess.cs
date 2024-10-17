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
                    case ClientState.NOT_CONNECTED:
                        ConnectToServer();
                        break;

                    case ClientState.CONNECTED:
                        WaitForAuthenticationHelper();
                        break;

                    case ClientState.ASSIGNED:
                        ProcessAuthChoice();
                        break;

                    case ClientState.LOGIN_CHOSEN:
                        if (_personalData.LoginAttemptsMade >= ServerConstants.MAX_LOGIN_ATTEMPTS) {
                            Console.WriteLine("Too many login attempts made.");
                            throw new ExitingProgramException("Too many login attempts.");
                        }
                        else {
                            ProcessLogin();
                        }
                        break;

                    case ClientState.REGISTRATION_CHOSEN:
                        if (_personalData.RegistrationAttemptsMade >= ServerConstants.MAX_REGISTRATION_ATTEMPTS) {
                            Console.WriteLine("Too many registration attempts made.");
                            _clientState = ClientState.ASSIGNED;
                        }
                        else {
                            ProcessRegistration();
                        }
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

        _clientState = ClientState.CONNECTED;
    }


    private void WaitForAuthenticationHelper()
    {
        CancellationTokenSource source = new(ServerConstants.AUTH_ASSIGNMENT_TIMEOUT_SECONDS * 1000);
        CancellationToken token = source.Token;

        while (true) {
            (ServerFlag? serverFlag, byte[] payload) =
                CMail.ReceiveMessageCancellable(_connectionResources!.Stream!, token);


            // --------- SUCCESS CASE --------------- //
            if (serverFlag == ServerFlag.AUTHENTICATOR_HELPER_ASSIGNED) {
                Debug.Assert(payload.Count() == 0);
                Console.WriteLine("You are connected to the server!");
                _clientState = ClientState.ASSIGNED;
                return;
            }
            // -------------------------------------- // 

            else if (serverFlag == ServerFlag.QUEUE_POSITION) {
                Debug.Assert(payload.Count() > 0);
                string currentPosition = Encoding.UTF8.GetString(payload);
                Console.WriteLine("Position in queue: " + currentPosition);
            }


            // Error checking
            if (serverFlag == ServerFlag.DISCONNECTION) {
                throw new IOException("Disconnected from server in WaitForAuthenticationHelper().");
            }
            if (serverFlag == ServerFlag.READ_CANCELED) {
                Console.WriteLine("Server timeout - Try again later.");
                throw new ExitingProgramException("Server timeout in WaitForAuthenticationHelper()");
            }
            if (serverFlag == ServerFlag.OVERLOADED) {
                Console.WriteLine("The server is overloaded. Try again later.");
                throw new ExitingProgramException("The server was overloaded.");
            }
            else {
                throw new ExitingProgramException("Received unexpected " + serverFlag + " from the server in WaitForAuthenticationHelper()");
            }
        }
    }


    private void ProcessAuthChoice()
    {
        while (true) {
            Console.WriteLine("login: l ||| register: r");

            string? userResponse = Console.ReadLine();

            if (userResponse == null) {
                Console.WriteLine("Failed to receive input");
                throw new ExitingProgramException("Failed to receive input in ReceiveAuthChoice");
            }
            if (userResponse == "l") {
                CMail.SendFlag(_connectionResources!.Stream!, ClientFlag.LOGIN_INIT);
                _clientState = ClientState.LOGIN_CHOSEN;
                return;
            }
            else if (userResponse == "r") {
                if (_personalData.AccountsCreated >= ServerConstants.MAX_ACCOUNTS_PER_SESSION) {
                    Console.WriteLine("You already made " + ServerConstants.MAX_ACCOUNTS_PER_SESSION + " accounts. Just log in, bro.");
                    _clientState = ClientState.ASSIGNED;
                    return;
                }
                CMail.SendFlag(_connectionResources!.Stream!, ClientFlag.REGISTRATION_INIT);
                _clientState = ClientState.REGISTRATION_CHOSEN;
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

        // +++ Part 1: Get user input.
        Console.WriteLine("Please provide your credentials (NOTE! Passwords are stored in plain text.)");
        Console.Write("[username password] ");
        string userCreds = Console.ReadLine() ?? throw new ExitingProgramException("ProcessLogin(): User input was null.");

        MessageValidationResult credsFormatEval = MessageValidation.ValidateUsernamePassword(userCreds);
        switch (credsFormatEval) {
            case MessageValidationResult.OK:
                break;
            case MessageValidationResult.WRONG_MESSAGE_FORMAT:
                Console.WriteLine("Format was incorrect.");
                _personalData.LoginAttemptsMade += 1;
                return;
            default:
                // Any legitimate attempt at login should be handled by the server, to prevent password cracking.
                // Also, previously stored Username and Password may not adhere to current MessageValidation rules.
                break;
        }
        // +++ Part 2: Send it to the server.
        CMail.SendString(ClientFlag.LOGIN_USERNAME_PASSWORD, _connectionResources!.Stream!, userCreds);

        // +++ Part 3: Receive server's response
        CancellationTokenSource loginTimeoutSource =
            new CancellationTokenSource(ServerConstants.LOGIN_RESPONSE_TIMEOUT_SECONDS * 1000);
        CancellationToken loginTimeout = loginTimeoutSource.Token;
        ServerFlag receivedFlag = CMail.ReceiveFlagCancellable(_connectionResources!.Stream!, loginTimeout);

        // +++ Part 4: Handle the server's response
        switch (receivedFlag) {
            case ServerFlag.LOGIN_SUCCEEDED:
                Console.WriteLine("Login successful!.");
                _clientState = ClientState.MOVING_TO_DASHBOARD;
                break;
            case ServerFlag.DISCONNECTION:
                Console.WriteLine(ServerConstants.USER_FACING_DISCONNECTION_MSG);
                throw new ExitingProgramException("Disconnection in ProcessLogin(). Potentially a uslow-timeout.");
            case ServerFlag.READ_CANCELED:
                Console.WriteLine(ServerConstants.USER_FACING_SERVER_TIMEOUT_MSG);
                throw new ExitingProgramException("sslow-timeout in ProcessLogin()");
            case ServerFlag.WRONG_PASSWORD:
                Console.WriteLine("Wrong password. Try again.");
                _personalData.LoginAttemptsMade += 1;
                return;
            case ServerFlag.USERNAME_DOESNT_EXIST:
                Console.WriteLine("User doesn't exist. Try again.");
                _personalData.LoginAttemptsMade += 1;
                return;
            case ServerFlag.DATABASE_ERROR:
                Console.WriteLine("Database error. Try again another time.");
                throw new ExitingProgramException("Database error on server side");
            default:
                throw new ExitingProgramException("Unhandled case in ProcessLogin()");
        }
    }

    private void ProcessRegistration()
    {

        Lazy<string> formatSpecsUsername = new Lazy<string>(() =>
            $"Minimum length: {ServerConstants.MIN_USERNAME_LEN}, maximum length: {ServerConstants.MAX_USERNAME_LEN}"
        );
        Lazy<string> formatSpecsPassword = new Lazy<string>(() =>
            $"Minimum length: {ServerConstants.MIN_PASSWORD_LEN}, maximum length: {ServerConstants.MAX_PASSWORD_LEN}"
        );

        // +++ Part 1: Receiving user input
        Console.WriteLine("Please provide your credentials (NOTE! Passwords are stored in plain text.)");
        Console.Write("[username password] ");
        string? userCreds = Console.ReadLine() ?? throw new ExitingProgramException("ProcessRegistration(): User input was null.");

        MessageValidationResult credsFormatEval = MessageValidation.ValidateUsernamePassword(userCreds);
        switch (credsFormatEval) {
            case MessageValidationResult.OK:
                break;
            case MessageValidationResult.USERNAME_TOO_LONG:
                Console.WriteLine("Username was too long: " + formatSpecsUsername);
                _personalData.RegistrationAttemptsMade += 1;
                return;
            case MessageValidationResult.USERNAME_TOO_SHORT:
                Console.WriteLine("Username was too short: " + formatSpecsUsername);
                _personalData.RegistrationAttemptsMade += 1;
                return;
            case MessageValidationResult.PASSWORD_TOO_LONG:
                Console.WriteLine("Password was too long: " + formatSpecsPassword);
                _personalData.RegistrationAttemptsMade += 1;
                return;
            case MessageValidationResult.PASSWORD_TOO_SHORT:
                Console.WriteLine("Password was too short: " + formatSpecsPassword);
                _personalData.RegistrationAttemptsMade += 1;
                return;
            case MessageValidationResult.WRONG_MESSAGE_FORMAT:
                Console.WriteLine("Format was incorrect.");
                _personalData.RegistrationAttemptsMade += 1;
                return;
        }

        // +++ Part 2: Sending to the server
        CMail.SendString(ClientFlag.REGISTRATION_USERNAME_PASSWORD, _connectionResources!.Stream!, userCreds);

        // +++ Part 3: Receiving and processing the server's response
        CancellationTokenSource registrationResponseTimeoutSource =
            new CancellationTokenSource(ServerConstants.REGISTRATION_RESPONSE_TIMEOUT_SECONDS * 1000);
        CancellationToken registrationResponseTimeout = registrationResponseTimeoutSource.Token;
        ServerFlag serverResponseFlag =
            CMail.ReceiveFlagCancellable(_connectionResources!.Stream!, registrationResponseTimeout);

        // ------------------------------- SUCCESS -------------------------------------------- // 
        if (serverResponseFlag == ServerFlag.REGISTRATION_SUCCESSFUL) {
            Console.WriteLine("Account created successfully!");
            _personalData.AccountsCreated += 1;
            // Go back to the screen where you choose between logging in and registering.
            _clientState = ClientState.ASSIGNED;
            return;
        }
        // ------------------------------------------------------------------------------------ //

        else if (serverResponseFlag == ServerFlag.USERNAME_TAKEN) {
            Console.WriteLine("Username was already taken.");
            _personalData.RegistrationAttemptsMade += 1;
            return;
        }

        else if (serverResponseFlag == ServerFlag.READ_CANCELED) {
            Console.WriteLine(ServerConstants.USER_FACING_SERVER_TIMEOUT_MSG);
            throw new ExitingProgramException("Server timeout while waiting for registration validation.");
        }

        else if (serverResponseFlag == ServerFlag.DISCONNECTION) {
            Console.WriteLine(ServerConstants.USER_FACING_DISCONNECTION_MSG);
            throw new ExitingProgramException("Disconnection in ProcessRegistration(), possibly timeout.");
        }

        else if (serverResponseFlag == ServerFlag.DATABASE_ERROR) {
            Console.WriteLine("The database experienced an error. Try again some other time.");
            throw new ExitingProgramException("Database error in ProcessRegistration()");
        }

    }
}