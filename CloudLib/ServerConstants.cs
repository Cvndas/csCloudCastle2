using System.Net;
using CloudLib;

/// <summary>
/// Constants for how the Server and Client interact, on top of the protocol
/// defined in ProtocolHeader and ProtocolConstants.
/// </summary>
public static class ServerConstants
{
    public static readonly IPAddress SERVER_IP = IPAddress.Loopback;
    public static readonly int SERVER_PORT = 56789;


    public const int LOGIN_ATTEMPTS = 3;
    public const int REGISTRATION_ATTEMPTS = 3;
    public const int MAX_AUTH_CHOICE_ATTEMTPS = 3;

    public const int MAX_USERNAME_LEN = sizeof(char) * 15;
    public const int MIN_USERNAME_LEN = sizeof(char) * 3;
    public const int MAX_PASSWORD_LEN = sizeof(char) * 15;
    public const int MIN_PASSWORD_LEN = sizeof(char) * 3;

    public const int MAX_FILE_SIZE = CloudProtocol.MAX_PAYLOAD_LEN;
    public const int MAX_STORED_FILES = 5;
    public const int STORAGE_CAPACITY = MAX_FILE_SIZE * MAX_STORED_FILES;

    public const int MAX_UPLOAD_TOKENS = 3;
    public const int MAX_DOWNLOAD_TOKENS = 3;
    public const int TOKEN_REFILL_MINUTES = 3;

    public const int MAX_FILENAME_LEN = sizeof(char) * 15;


    public const int HELPERS_PER_AUTHENTICATION_MANAGER = 2;
    public const int MAX_AUTHENTICATION_MANAGERS = 2;
    public const int MAX_USERS_IN_AUTHENTICATION_QUEUE = 2;



}