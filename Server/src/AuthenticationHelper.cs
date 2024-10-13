#define PRINTING_AUTH_STATES


using System.Data;
using CloudLib;

namespace Server.src;



internal class AuthenticationHelper
{
    public AuthenticationHelper(int id)
    {
        _helperData = new AuthenticationHelperData {
            Id = id,
            Resources = null
        };

        CR_hasWork = false;
        _hasWorkLock = new object();

        _authHelperState = ServerStates.NOT_CONNECTED;

        Thread authenticationHelperThread = new(AuthenticatorHelperJob);
        authenticationHelperThread.Start();
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


    // ----------- CRITICAL --------------- //
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
                    Monitor.Wait(_hasWorkLock);
                    Debug.Assert(CR_hasWork == true);
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







    private void DPrintAuthStates()
    {
#if PRINTING_AUTH_STATES
        WriteLine(_authHelperState);
#endif
    }
}