#define PRINTING_AUTH_STATES


using System.Data;
using CloudLib;

namespace Server.src;

// TODO Before Dashboard: Thoroughly test the authentication system. 

internal class AuthenticationHelper
{
    public AuthenticationHelper(int id, CancellationToken token)
    {
        // Note : It's quite arbitrary which data goes in here. Could use a refactor in the future.
        _helperData = new AuthenticationHelperData {
            Id = id,
            ConnectionResources = null,
            LoginAttemptsMade = 0,
            RegistrationAttempts = 0,
            AccountsCreated = 0
        };

        CR_hasWork = false;
        _hasWorkLock = new object();
        _cancellationToken = token;

        _authHelperState = ServerState.NOT_CONNECTED;

        _consolePreamble = $"AuthHelper {_helperData.Id}: ";

        _authenticationHelperThread = new(AuthenticationHelperJob);
        _authenticationHelperThread.Start();
    }

    // Thread: Active thread of this helper.

    public void AssignClient(ConnectionResources resources)
    {
        lock (_hasWorkLock) {
            Debug.Assert(CR_hasWork == false);

            _helperData.ConnectionResources = resources;
            _helperData.LoginAttemptsMade = 0;
            _helperData.RegistrationAttempts = 0;
            _helperData.AccountsCreated = 0;

            CR_hasWork = true; // This bool is probably completely unnecessary, except for the assert.

            // Inform the client that he has been assigned
            SMail.SendFlag(resources.Stream!, ServerFlag.AUTHENTICATOR_HELPER_ASSIGNED);

            Monitor.Pulse(_hasWorkLock);
        }
    }

    /// <summary>
    /// This should only ever be called by the ListenerInstance
    /// </summary>
    public void PulseToJoinThread()
    {
        Debug.Assert(this.GetType() == typeof(ListenerInstance));
        lock (_hasWorkLock) {
            Monitor.Pulse(_hasWorkLock);
        }
        Debug.Assert(_helperData!.ConnectionResources!.Stream == null);
        Debug.Assert(_helperData!.ConnectionResources!.TcpClient == null);
        JoinAuthHelperThread();
    }

    private class AuthenticationHelperData
    {
        // Id remains consistent throughout
        // ConnectionResources may be transferred around.
        internal int Id { get; init; }
        internal int RegistrationAttempts { get; set; }
        internal int LoginAttemptsMade { get; set; }
        internal int AccountsCreated { get; set; }
        internal ConnectionResources? ConnectionResources { get; set; }
    }

    // ---------- Private Variables -------- // 
    private readonly AuthenticationHelperData _helperData;
    private ServerState _authHelperState;
    private CancellationToken _cancellationToken;
    private Thread _authenticationHelperThread;
    private string _consolePreamble = "";


    // ----------- CRITICAL --------------- //
    /// <summary>
    /// Thread: AuthHelper, waiting for work <br/>
    /// Thread: AuthenticationManager, pulsing that there is work <br/>
    /// Thread: ListenerInstance, Pulsing that CancellationToken has been invoked.
    /// </summary>
    private bool CR_hasWork;
    private readonly object _hasWorkLock;
    // ------------------------------------ // 









    // -------------- Private Methods ------- // 
    private void AuthenticationHelperJob()
    {
        while (true) {
            try {
                lock (_hasWorkLock) {
                    Debug.Assert(CR_hasWork == false);
                    // Pulse 1: A client's resources have been added (unimplemented for now)
                    // Pulse 2: Cancellation has been requested
                    Monitor.Wait(_hasWorkLock);
                    Debug.Assert(CR_hasWork == true);
                }
                if (_cancellationToken.IsCancellationRequested) {
                    Console.WriteLine($"Helper: {_helperData.Id} has had his cancellation token invoked.");
                    break;
                }

                _authHelperState = ServerState.ASSIGNED_TO_CLIENT;

                RunAuthenticationHelperStateMachine();

            }
            catch (IOException) {
                Console.WriteLine($"Client of {_helperData.Id} disconnected.");
                _helperData?.ConnectionResources?.Cleanup();
            }
            catch (ClientTimeoutException) {
                Console.WriteLine($"Client of {_helperData.Id} timed out. Terminating the connection.");
                _helperData?.ConnectionResources?.Cleanup();
            }
            catch (Exception e) {
                Console.WriteLine($"Unexpected exception in AuthenticatorHelperJob of {_helperData.Id}.");
                Console.WriteLine("Exception Type: " + e.GetType());
                Console.WriteLine("Message: " + e.Message);
                Console.WriteLine("Terminating the connection nonetheless.");
                _helperData?.ConnectionResources?.Cleanup();
            }
            finally {
                // Note: this code block will probably be reached while the client's resources are valid, and
                // passed into another part of the system.
                Console.WriteLine("Unimplemented the \"finally\" part of AuthenticatorHelperJob");
                Debug.Assert(false);
            }
        }

        Console.WriteLine($"Helper: {_helperData.Id} has exited his thread, and it's ready to be joined.");
    }

    // Thread: Listener
    private void JoinAuthHelperThread()
    {
        Console.WriteLine($"AuthHelper: {_helperData.Id} is trying to join his Job thread..");
        _authenticationHelperThread.Join();
        Console.WriteLine($"AuthHelper: {_helperData.Id} has joined his Job thread.");
    }


    // !!! !!! State Machine !!! !!! //
    private void RunAuthenticationHelperStateMachine()
    {
        // Being assigned takes place in AuthenticationHelperjob
        Debug.Assert(_authHelperState == ServerState.ASSIGNED_TO_CLIENT);
        CancellationTokenSource authProcessTimerSource =
            new CancellationTokenSource(ServerConstants.AUTH_PROCESS_TIMEOUT_SECONDS * 1000);
        CancellationToken authProcessTimerToken = authProcessTimerSource.Token;

        while (true) {
            try {
                DPrintAuthStates();
                switch (_authHelperState) {
                    case ServerState.ASSIGNED_TO_CLIENT:
                        ProcessAuthenticationChoice(authProcessTimerToken);
                        break;
                    case ServerState.REGISTRATION_REQUEST_RECEIVED:
                        ProcessRegistrationAttempt();
                        break;
                    case ServerState.LOGIN_REQUEST_RECEIVED:
                        ProcessLoginAttempt();
                        break;
                    case ServerState.BREAKING_CONNECTION:
                        // TODO Before login
                        Debug.Assert(false);
                        break;
                    case ServerState.PASSING_CONN_INFO_TO_DASHBOARD:
                        Debug.Assert(false);
                        // PassConnectionInfoToDashboard()
                        break;
                    default:
                        Console.WriteLine("Invalid state reached.");
                        return;
                }

            }
            catch (Exception e) {
                Console.WriteLine(_consolePreamble + "unexpected exception caught in State Machine loop: " + e.Message);
                Console.WriteLine("Closing connection to the client.");
                _authHelperState = ServerState.BREAKING_CONNECTION;
            }
        }
    }

    private void ProcessAuthenticationChoice(CancellationToken authProcessTimerToken)
    {
        (ClientFlag flag, _) = SMail.ReceiveMessageCancellable(_helperData.ConnectionResources!.Stream!, authProcessTimerToken);

        if (SMail.ClientDisconnected(flag)) {
            Console.WriteLine(_consolePreamble + "Client disconnected in ProcessAuthenticationChoice() - bc");
            _authHelperState = ServerState.BREAKING_CONNECTION;
        }
        else if (SMail.ReadWasCancelled(flag)) {
            Console.WriteLine(_consolePreamble + "Client timeout in ProcessAuthenticationChoice() - bc");
            _authHelperState = ServerState.BREAKING_CONNECTION;
        }

        else if (flag == ClientFlag.LOGIN_INIT) {
            _authHelperState = ServerState.LOGIN_REQUEST_RECEIVED;
        }
        else if (flag == ClientFlag.REGISTRATION_INIT) {
            _authHelperState = ServerState.REGISTRATION_REQUEST_RECEIVED;
        }

        else {
            Console.WriteLine(_consolePreamble + "Received invalid flag in ProcessAuthenticationChoice: " + flag + " - bc");
            _authHelperState = ServerState.BREAKING_CONNECTION;
        }
    }

    private void ProcessLoginAttempt()
    {
        // User must have sent the response before this timer runs out.
        CancellationTokenSource loginTimerSource =
            new CancellationTokenSource(ServerConstants.LOGIN_TIMEOUT_SECONDS * 1000);
        CancellationToken loginTimer = loginTimerSource.Token;

        // The unmodified client will send no Login Request requests if it has reached the max.
        if (_helperData.LoginAttemptsMade >= ServerConstants.MAX_LOGIN_ATTEMPTS) {
            // therefore, if this code is triggered, the user is hacking
            _authHelperState = ServerState.BREAKING_CONNECTION;
            return;
        }

        // +++ Part 1: Reading user input.
        (ClientFlag receivedFlag, string receivedPayload) =
            SMail.ReceiveStringCancellable(_helperData.ConnectionResources!.Stream!, loginTimer);

        // +++ Part 2: Checking for errors in the receivedFlag
        if (SMail.ClientDisconnected(receivedFlag)) {
            Console.WriteLine(_consolePreamble + "client disconnected in ProcessLoginAttempt()");
            _authHelperState = ServerState.BREAKING_CONNECTION;
            return;
        }
        else if (SMail.ReadWasCancelled(receivedFlag)) {
            Console.WriteLine(_consolePreamble + "client was too slow to respond in ProcessLoginAttempt().");
            _authHelperState = ServerState.BREAKING_CONNECTION;
            return;
        }
        else if (receivedFlag != ClientFlag.LOGIN_USERNAME_PASSWORD) {
            Console.WriteLine(_consolePreamble + "received invalid flag from user. in ProcessLoginAttempt()");
            _authHelperState = ServerState.BREAKING_CONNECTION;
            return;
        }

        // +++ Part 3: Validating the payload.
        MessageValidationResult evalResult = MessageValidation.ValidateUsernamePassword(receivedPayload);

        if (evalResult == MessageValidationResult.WRONG_MESSAGE_FORMAT) {
            Console.WriteLine("User didn't validate message in ProcessLoginAttempt()");
            _authHelperState = ServerState.BREAKING_CONNECTION;
            return;
        }

        // Password and Username validation should not apply retroactively to stored passwords, as the 
        // validation system may change over time. Even if the validation doesn't match current rules,
        // since those rules only apply to modern registration, it doesn't apply here.


        // +++ Part 4: Checking if the login info is correct.
        string[] usernamePasswordArray = receivedPayload.Split(" ");
        Debug.Assert(usernamePasswordArray.Length == 2); // Since it passed format validation, this should work.
        string username = usernamePasswordArray[0];
        string password = usernamePasswordArray[1];
        DatabaseFlags databaseResponseFlag = UserDatabase.TryToLogin(username, password);

        // +++ Part 5: Handling the database response.
        switch (databaseResponseFlag) {

            // -------------- Success!! -------------
            case DatabaseFlags.KEY_VALUE_MATCHES:
                _authHelperState = ServerState.PASSING_CONN_INFO_TO_DASHBOARD;
                SMail.SendFlag(_helperData.ConnectionResources.Stream!, ServerFlag.LOGIN_SUCCEEDED);
                return;


            case DatabaseFlags.INVALID_FLAG:
                Debug.Assert(false, "unfinished UserDatabase.TryToLogin()");
                _authHelperState = ServerState.BREAKING_CONNECTION;
                return;
            case DatabaseFlags.DATABASE_ERROR:
                SMail.SendFlag(_helperData.ConnectionResources.Stream!, ServerFlag.DATABASE_ERROR);
                _authHelperState = ServerState.BREAKING_CONNECTION;
                return;
            case DatabaseFlags.KEY_DOESNT_EXIST:
                SMail.SendFlag(_helperData.ConnectionResources.Stream!, ServerFlag.USERNAME_DOESNT_EXIST);
                _helperData.LoginAttemptsMade += 1;
                break;
            case DatabaseFlags.VALUE_DOESNT_MATCH:
                SMail.SendFlag(_helperData.ConnectionResources.Stream!, ServerFlag.WRONG_PASSWORD);
                _helperData.LoginAttemptsMade += 1;
                break;
            default:
                Console.WriteLine(_consolePreamble + "unhandled case in part 5 of ProcessLogin()");
                _authHelperState = ServerState.BREAKING_CONNECTION;
                return;
        }
        Console.WriteLine(_consolePreamble + "user failed to log in.");
    }


    private void ProcessRegistrationAttempt()
    {
        // User must have sent the response before this timer runs out.
        CancellationTokenSource registrationTimerSource =
            new CancellationTokenSource(ServerConstants.REGISTER_TIMEOUT_SECONDS * 1000);
        CancellationToken registrationTimer = registrationTimerSource.Token;

        if (_helperData.RegistrationAttempts >= ServerConstants.MAX_REGISTRATION_ATTEMPTS) {
            Console.WriteLine(_consolePreamble + "user, who is hacking, made too many registration attempts in ProcessRegistrationAttempt() - bc");
            _authHelperState = ServerState.BREAKING_CONNECTION;
        }

        // The unmodified client will send no Registration requests if it has reached the max.
        if (_helperData.AccountsCreated > ServerConstants.MAX_ACCOUNTS_PER_SESSION) {
            // therefore, if this code is triggered, the user is hacking
            _authHelperState = ServerState.BREAKING_CONNECTION;
            return;
        }
        // --------------------------- READING ----------------------------- //
        (ClientFlag flag, string payload) =
            SMail.ReceiveStringCancellable(_helperData.ConnectionResources!.Stream!, registrationTimer);
        if (SMail.ClientDisconnected(flag)) {
            _authHelperState = ServerState.BREAKING_CONNECTION;
            Console.WriteLine(_consolePreamble + "client disconnected in ProcessRegistrationAttempt() - bc");
            return;
        }
        else if (SMail.ReadWasCancelled(flag)) {
            _authHelperState = ServerState.BREAKING_CONNECTION;
            Console.WriteLine(_consolePreamble + "timeout in ProcessRegistrationAttempt() - bc");
            return;
        }
        else if (flag != ClientFlag.REGISTRATION_USERNAME_PASSWORD) {
            Console.WriteLine(_consolePreamble + "received invalid flag from user in ProcessRegistrationAttempt");
            _authHelperState = ServerState.BREAKING_CONNECTION;
            return;
        }
        // ----------------------------------------------------------------- //

        // --------------------------- HANDLING DATABASE RESPONSES --------------------- //
        // The user should handle each failure case personally. We just need to know if it's correct or not.
        // If it's incorrect, then the user modified the client.
        if (!(MessageValidation.ValidateUsernamePassword(payload) == MessageValidationResult.OK)) {
            Console.WriteLine(_consolePreamble + "user modified client, sent invalid username password in ProcessRegistrationAttempt() - bc");
            _authHelperState = ServerState.BREAKING_CONNECTION;
            return;
        }

        string[] payloadSplit = payload.Split(" ");
        string username = payloadSplit[0];
        string password = payloadSplit[1];
        DatabaseFlags result = UserDatabase.TryToRegister(username, password);

        // ----------- SUCCESS --------------- //
        if (result == DatabaseFlags.KEY_VALUE_DEPOSITED) {
            _helperData.AccountsCreated += 1;
            SMail.SendFlag(_helperData.ConnectionResources.Stream!, ServerFlag.REGISTRATION_SUCCESSFUL);
            _authHelperState = ServerState.ASSIGNED_TO_CLIENT;
            return;
        }
        // --------------------------------------- //

        else if (result == DatabaseFlags.KEY_ALREADY_EXISTS) {
            SMail.SendFlag(_helperData.ConnectionResources.Stream!, ServerFlag.USERNAME_TAKEN);
            _helperData.RegistrationAttempts += 1;
        }
        else if (result == DatabaseFlags.USERNAME_PASS_VALIDATION_ERROR) {
            Console.WriteLine(_consolePreamble + "programming error in ProcessRegistrationAttempt(): wrong user_pass format - bc");
            _authHelperState = ServerState.BREAKING_CONNECTION;
            return;
        }
        else if (result == DatabaseFlags.DATABASE_ERROR) {
            Console.WriteLine(_consolePreamble + "database error in ProcessRegistrationAttempt()");
            _authHelperState = ServerState.BREAKING_CONNECTION;
            return;
        }
        // --------------------------------------------------------------------------- //
        Debug.Assert(result == DatabaseFlags.KEY_VALUE_DEPOSITED);
    }

    private void TransferResourcesToDashboard()
    {
        // TODO after login

        //but this one line is necessary for asserts in Cleanup by the ListenerThread process.
        _helperData.ConnectionResources = null;
    }


    private void DPrintAuthStates()
    {
#if PRINTING_AUTH_STATES
        Console.WriteLine("STATE: " + _authHelperState);
#endif
    }
}