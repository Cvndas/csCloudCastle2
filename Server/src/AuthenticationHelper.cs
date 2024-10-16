#define PRINTING_AUTH_STATES


using System.Data;
using CloudLib;

namespace Server.src;



internal class AuthenticationHelper
{
    public AuthenticationHelper(int id, CancellationToken token)
    {
        // Note : It's quite arbitrary which data goes in here. Could use a refactor in the future.
        _helperData = new AuthenticationHelperData {
            Id = id,
            ConnectionResources = null,
            LoginAttempts = 0,
            RegistrationAttempts = 0,
            AccountsCreated = 0
        };

        CR_hasWork = false;
        _hasWorkLock = new object();
        _cancellationToken = token;

        _authHelperState = ServerStates.NOT_CONNECTED;

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
            _helperData.LoginAttempts = 0;
            _helperData.RegistrationAttempts = 0;
            _helperData.AccountsCreated = 0;

            CR_hasWork = true; // This bool is probably completely unnecessary, except for the assert.
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
        internal int LoginAttempts { get; set; }
        internal int AccountsCreated { get; set; }
        internal ConnectionResources? ConnectionResources { get; set; }
    }

    // ---------- Private Variables -------- // 
    private readonly AuthenticationHelperData _helperData;
    private ServerStates _authHelperState;
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

                _authHelperState = ServerStates.ASSIGNED_TO_CLIENT;

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
        Debug.Assert(_authHelperState == ServerStates.ASSIGNED_TO_CLIENT);
        CancellationTokenSource authProcessTimerSource =
            new CancellationTokenSource(ServerConstants.AUTH_PROCESS_TIMEOUT_SECONDS);
        CancellationToken authProcessTimerToken = authProcessTimerSource.Token;

        while (true) {
            DPrintAuthStates();
            switch (_authHelperState) {
                case ServerStates.ASSIGNED_TO_CLIENT:
                    ProcessAuthenticationChoice(authProcessTimerToken);
                    break;
                case ServerStates.REGISTRATION_REQUEST_RECEIVED:
                    ProcessRegistrationAttempt();
                    break;
                case ServerStates.BREAKING_CONNECTION:
                    // TODO Before login
                    Debug.Assert(false);
                    break;
                case ServerStates.PASSED_CONN_INFO_TO_DASHBOARD:
                    Debug.Assert(false);
                    break;
                default:
                    Console.WriteLine("Invalid state reached.");
                    return;
            }
        }
    }

    private void ProcessAuthenticationChoice(CancellationToken authProcessTimerToken)
    {
        (ClientFlags flag, _) = SMail.ReceiveMessageCancellable(_helperData.ConnectionResources!.Stream!, authProcessTimerToken);

        if (SMail.ClientDisconnected(flag)) {
            Console.WriteLine(_consolePreamble + "Client disconnected in ProcessAuthenticationChoice() - bc");
            _authHelperState = ServerStates.BREAKING_CONNECTION;
        }
        else if (SMail.ReadWasCanceled(flag)) {
            Console.WriteLine(_consolePreamble + "Client timeout in ProcessAuthenticationChoice() - bc");
            _authHelperState = ServerStates.BREAKING_CONNECTION;
        }

        else if (flag == ClientFlags.LOGIN_INIT) {
            _authHelperState = ServerStates.LOGIN_REQUEST_RECEIVED;
        }
        else if (flag == ClientFlags.REGISTRATION_INIT) {
            _authHelperState = ServerStates.REGISTRATION_REQUEST_RECEIVED;
        }

        else {
            Console.WriteLine(_consolePreamble + "Received invalid flag in ProcessAuthenticationChoice: " + flag + " - bc");
            _authHelperState = ServerStates.BREAKING_CONNECTION;
        }
    }

    private void ProcessRegistrationAttempt()
    {
        // TODO Next - Finish, Server Side 
        // And make this timer be used in the async read.
        CancellationTokenSource registrationTimerSource =
            new CancellationTokenSource(ServerConstants.REGISTER_TIMEOUT_SECONDS);
        CancellationToken registrationTimer = registrationTimerSource.Token;

        // The unmodified client will send no Registration requests if it has reached the max.
        if (_helperData.AccountsCreated > ServerConstants.MAX_ACCOUNTS_PER_SESSION) {
            // therefore, if this code is triggered, the user is hacking
            _authHelperState = ServerStates.BREAKING_CONNECTION;
            return;
        }
        // --------------------------- READING ----------------------------- //
        (ClientFlags flag, string payload) =
            SMail.ReceiveStringCancellable(_helperData.ConnectionResources!.Stream!, registrationTimer);
        if (SMail.ClientDisconnected(flag)) {
            _authHelperState = ServerStates.BREAKING_CONNECTION;
            Console.WriteLine(_consolePreamble + "client disconnected in ProcessRegistrationAttempt() - bc");
            return;
        }
        else if (SMail.ReadWasCanceled(flag)) {
            _authHelperState = ServerStates.BREAKING_CONNECTION;
            Console.WriteLine(_consolePreamble + "timeout in ProcessRegistrationAttempt() - bc");
            return;
        }
        // ----------------------------------------------------------------- //

        // --------------------------- HANDLING USER RESPONSES --------------------- //
        // The user should handle each failure case personally. We just need to know if it's correct or not.
        // If it's incorrect, then the user modified the client.
        if (!(MessageValidation.ValidateUsernamePassword(payload) == MessageValidationResult.OK)) {
            Console.WriteLine(_consolePreamble + "user modified client, sent invalid username password in ProcessRegistrationAttempt() - bc");
            _authHelperState = ServerStates.BREAKING_CONNECTION;
            return;
        }

        DatabaseFlags result = UserDatabase.TryToRegister(payload);
        if (result == DatabaseFlags.ACCOUNT_CREATED) {
            _helperData.AccountsCreated += 1;
            SMail.SendFlag(_helperData.ConnectionResources.Stream!, ServerFlags.REGISTRATION_SUCCESSFUL);
            _authHelperState = ServerStates.ASSIGNED_TO_CLIENT;
            return;
        }
        else if (result == DatabaseFlags.USERNAME_TAKEN) {
            SMail.SendFlag(_helperData.ConnectionResources.Stream!, ServerFlags.USERNAME_TAKEN);
        }
        else if (result == DatabaseFlags.USERNAME_PASS_VALIDATION_ERROR) {
            Console.WriteLine(_consolePreamble + "programming error in ProcessRegistrationAttempt(): wrong user_pass format - bc");
            _authHelperState = ServerStates.BREAKING_CONNECTION;
            return;
        }
        else if (result == DatabaseFlags.DATABASE_ERROR) {
            Console.WriteLine(_consolePreamble+"database error in ProcessRegistrationAttempt()");
            _authHelperState = ServerStates.BREAKING_CONNECTION;
            return;
        }

        // TODO : Assert that this is always called if no disconnection AND registration was unsuccessful
        _helperData.RegistrationAttempts += 1;
        if (_helperData.RegistrationAttempts >= ServerConstants.MAX_REGISTRATION_ATTEMPTS) {
            Console.WriteLine(_consolePreamble + "user made too many registration attempts in ProcessRegistrationAttempt() - bc");
            _authHelperState = ServerStates.BREAKING_CONNECTION;
        }
    }

    private void TransferResources()
    {
        // TODO after login

        //but this one line is necessary for asserts in Cleanup by the ListenerThread process.
        _helperData.ConnectionResources = null;
    }


    private void DPrintAuthStates()
    {
#if PRINTING_AUTH_STATES
        Console.WriteLine(_authHelperState);
#endif
    }
}