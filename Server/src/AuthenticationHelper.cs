#define PRINTING_AUTH_STATES


using System.Data;
using CloudLib;

namespace Server.src;



internal class AuthenticationHelper
{
    public AuthenticationHelper(int id, CancellationToken token)
    {
        _helperData = new AuthenticationHelperData {
            Id = id,
            Resources = null
        };

        CR_hasWork = false;
        _hasWorkLock = new object();
        _cancellationToken = token;

        _authHelperState = ServerStates.NOT_CONNECTED;

        _authenticationHelperThread = new(AuthenticatorHelperJob);
        _authenticationHelperThread.Start();
    }

    // Thread: Active thread of this helper.

    public void AssignClient(ConnectionResources resources)
    {
        lock (_hasWorkLock) {
            Debug.Assert(CR_hasWork == false);

            _helperData.Resources = resources;

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
        Debug.Assert(_helperData!.Resources!.Stream == null);
        Debug.Assert(_helperData!.Resources!.TcpClient == null);
        JoinAuthHelperThread();
    }

    private class AuthenticationHelperData
    {
        // Id remains consistent throughout
        // ConnectionResources may be transferred around.
        internal int Id { get; init; }
        internal ConnectionResources? Resources { get; set; }
    }

    // ---------- Private Variables -------- // 
    private readonly AuthenticationHelperData _helperData;
    private ServerStates _authHelperState;
    private CancellationToken _cancellationToken;
    private Thread _authenticationHelperThread;


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
    private void AuthenticatorHelperJob()
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
                    WriteLine($"Helper: {_helperData.Id} has had his cancellation token invoked.");
                    break;
                }

                _authHelperState = ServerStates.ASSIGNED_TO_CLIENT;

                RunAuthenticatorHelperStateMachine();

            }
            catch (IOException) {
                WriteLine($"Client of {_helperData.Id} disconnected.");
                _helperData?.Resources?.Cleanup();
            }
            catch (ClientTimeoutException) {
                WriteLine($"Client of {_helperData.Id} timed out. Terminating the connection.");
                _helperData?.Resources?.Cleanup();
            }
            catch (Exception e) {
                WriteLine($"Unexpected exception in AuthenticatorHelperJob of {_helperData.Id}.");
                WriteLine("Exception Type: " + e.GetType());
                WriteLine("Message: " + e.Message);
                WriteLine("Terminating the connection nonetheless.");
                _helperData?.Resources?.Cleanup();
            }
            finally {
                // Note: this code block will probably be reached while the client's resources are valid, and
                // passed into another part of the system.
                WriteLine("Unimplemented the \"finally\" part of AuthenticatorHelperJob");
                Debug.Assert(false);
            }
        }

        WriteLine($"Helper: {_helperData.Id} has exited his thread, and it's ready to be joined.");
    }

    private void JoinAuthHelperThread()
    {
        WriteLine($"AuthHelper: {_helperData.Id} is trying to join his Job thread..");
        _authenticationHelperThread.Join();
        WriteLine($"AuthHelper: {_helperData.Id} has joined his Job thread.");
    }


    // !!! !!! State Machine !!! !!! //
    private void RunAuthenticatorHelperStateMachine()
    {
        Debug.Assert(_authHelperState == ServerStates.ASSIGNED_TO_CLIENT);

        while (true) {
            DPrintAuthStates();
            switch (_authHelperState) {
                case ServerStates.ASSIGNED_TO_CLIENT:
                    ProcessAuthenticationChoice();
                    break;
                default:
                    WriteLine("Invalid state reached.");
                    return;
            }
        }
    }

    private void ProcessAuthenticationChoice()
    {
        // TODO : After implementing the Auth Helper thread pool and Free Auth Helper queue.
        Debug.Assert(false);
        return;
    }

    private void TransferResources()
    {
        // TODO after registration, but this is necessary for asserts in Cleanup by the ListenerThread process.
        _helperData.Resources = null;
    }


    private void DPrintAuthStates()
    {
#if PRINTING_AUTH_STATES
        WriteLine(_authHelperState);
#endif
    }
}