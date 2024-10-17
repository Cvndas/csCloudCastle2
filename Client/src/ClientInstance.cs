#define STATE_PRINTING

using CloudLib;

namespace Client.src;

/// <summary>
/// The class which handles the Client's experience.
/// </summary>
internal partial class ClientInstance
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
            LoginAttemptsMade = 0,
            RegistrationAttemptsMade = 0,
            AuthChoiceAttempts = 0,
            AccountsCreated = 0
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
    private ClientState _clientState;

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


}

