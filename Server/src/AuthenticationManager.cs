namespace Server.src;

internal class AuthenticationManager
{
    private static AuthenticationManager? _instance;
    public static AuthenticationManager Instance {
        get {
            _instance ??= new AuthenticationManager();
            return _instance;
        }
    }

    private AuthenticationManager()
    {
        CR_freeAuthHelpers = new (ServerConstants.HELPERS_PER_AUTHENTICATION_MANAGER);
        _freeAuthHelpersLock = new object();


    }

    // ----------- Private data ------------- // 

    // ----------- CRITICAL ----------------- // 
    List<AuthenticationHelper> CR_freeAuthHelpers;
    private readonly object _freeAuthHelpersLock;
    // -------------------------------------- // 

    // -------------------------------------- // 
}