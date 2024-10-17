#define TIMEOUT_DISABLED
using System.Net;
using CloudLib;

namespace CloudLib;

/// <summary>
/// Constants for how the Server and Client interact, on top of the protocol
/// defined in ProtocolHeader and ProtocolConstants.
/// </summary>
public static class ServerConstants
{

#if TIMEOUT_DISABLED
    public static readonly int TIMEOUT_MULTIPLIER = 200;
#else
    public static readonly int TIMEOUT_MULTIPLIER = 1;
#endif

    public static readonly IPAddress SERVER_IP = IPAddress.Loopback;
    public static readonly int SERVER_PORT = 56789;


    public const int MAX_LOGIN_ATTEMPTS = 3;
    public const int MAX_REGISTRATION_ATTEMPTS = 3;
    public const int MAX_AUTH_CHOICE_ATTEMTPS = 3;
    public const int MAX_ACCOUNTS_PER_SESSION = 2;

    public const int MAX_USERNAME_LEN = 15;
    public const int MIN_USERNAME_LEN = 2;
    public const int MAX_PASSWORD_LEN = 15;
    public const int MIN_PASSWORD_LEN = 3;

    public const int MAX_FILE_SIZE = ProtocolConstants.MAX_PAYLOAD_LEN;
    public const int MAX_STORED_FILES = 5;
    public const int STORAGE_CAPACITY = MAX_FILE_SIZE * MAX_STORED_FILES;

    public const int MAX_UPLOAD_TOKENS = 3;
    public const int MAX_DOWNLOAD_TOKENS = 3;
    public const int TOKEN_REFILL_MINUTES = 3;

    public const int MAX_FILENAME_LEN = sizeof(char) * 15;


    public const int HELPERS_PER_AUTHENTICATION_MANAGER = 2;
    public const int MAX_AUTHENTICATION_MANAGERS = 2;
    public const int MAX_USERS_IN_AUTHENTICATION_QUEUE = 2;

    public const int AUTH_MANAGER_BASE_ID = 100;
    public const int AUTH_MANAGER_ID_INCREMENT = 100;



    public static readonly int AUTH_MANAGER_CLEANUP_TIMEFRAME_SECONDS = 1 * TIMEOUT_MULTIPLIER;
    public static readonly int AUTH_ASSIGNMENT_TIMEOUT_SECONDS = 10 * TIMEOUT_MULTIPLIER;
    public static readonly int REGISTER_TIMEOUT_SECONDS = 15 * TIMEOUT_MULTIPLIER;
    public static readonly int LOGIN_TIMEOUT_SECONDS = 15 * TIMEOUT_MULTIPLIER;
    public static readonly int AUTH_PROCESS_TIMEOUT_SECONDS = (REGISTER_TIMEOUT_SECONDS + LOGIN_TIMEOUT_SECONDS + 20) * TIMEOUT_MULTIPLIER;
    public static readonly int REGISTRATION_RESPONSE_TIMEOUT_SECONDS = 3 * TIMEOUT_MULTIPLIER;
    public static readonly int LOGIN_RESPONSE_TIMEOUT_SECONDS = 3 * TIMEOUT_MULTIPLIER;

    /// <summary>
    /// Used by the client to talk to the user
    /// </summary>
    public const string USER_FACING_DISCONNECTION_MSG = "Lost connection to the server.";

    /// <summary>
    /// Used by the client to talk to the user
    /// </summary>
    public const string  USER_FACING_SERVER_TIMEOUT_MSG = "Server timeout. Please try again another time.";
}