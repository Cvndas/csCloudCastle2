namespace CloudLib;
public enum ClientFlag : byte
{
    INVALID_FLAG, // 0, to mark illegal messages received.
    DO_NOT_USE_READ_CANCELED, // 1, do not move
    DO_NOT_USE_TIMEOUT, // 2, do not move

    // ---- General ---- //
    OK, // Generic approval, prefer to avoid.
    QUITTING, // Whenever you want to tell the server explicitly to close the connection.
    DISCONNECTED,
    // ----------------- //

    // ---- Registration ---- // 
    REGISTRATION_INIT,
    REGISTRATION_USERNAME_PASSWORD,
    // ---------------------- // 


    // ---- Login ---- // 
    LOGIN_INIT,
    LOGIN_USERNAME_PASSWORD,
    // --------------- // 


    // ---- Dashboard ---- // 
    FILE_LIST_REQUEST,
    ENTER_CHAT_REQUEST,
    VIEW_TOKENS_REQUEST,
    // ----------------- // 


    // ---- Download ---- // 
    DOWNLOAD_REQUEST,
    DOWNLOAD_RECEIVED,
    // ------------------ // 


    // ---- Upload ---- // 
    UPLOAD_REQUEST,
    // ---------------- // 

    // ---- Chat ---- //
    CHAT_MESSAGE,
    EXIT_CHAT,
    // -------------- // 
}

// --------------------------------------------------- // 

public enum ServerFlag : byte
{
    INVALID_FLAG, // 0, to mark illegal messages.
    READ_CANCELED, // 1, do not move
    SERVER_TIMEOUT, // 2, do not move

    // ---- General ---- //
    OK, // Generic approval, prefer to avoid.
    DISCONNECTION,
    // ----------------- //

    // ---- Assignment Notifications ---- //
    AUTHENTICATOR_HELPER_ASSIGNED,
    QUEUE_POSITION,
    OVERLOADED,


    // ---- Registration ---- // 
    REGISTRATION_INIT_ACCEPTED,
    REGISTRATION_SUCCESSFUL,
    USERNAME_TAKEN,
    USERNAME_TOO_LONG,
    USERNAME_TOO_SHORT,
    PASSWORD_TOO_LONG,
    PASSWORD_TOO_SHORT,
    DATABASE_ERROR,
    MAX_REGISTRATION_ATTEMPTS_REACHED, // Terminate connection after sending.
    // ---------------------- // 


    // ---- Login ---- // 
    LOGIN_INIT_ACCEPTED,
    LOGIN_SUCCEEDED,
    USERNAME_DOESNT_EXIST,
    WRONG_PASSWORD,
    MAX_LOGIN_ATTEMPTES_REACHED, // Terminate connection after sending.
    // --------------- // 


    // ---- Dashboard ---- // 
    DISPLAYING_FILE_LIST,
    CHAT_READY,
    DISPLAYING_TOKENS,
    // ----------------- // 


    // ---- Download ---- // 
    DOWNLOAD_APPROVED,
    DOWNLOAD_REJECTED_FILENAME_DOESNT_EXIST,
    DOWNLOAD_REJECTED_TOKENS_DEPLETED,
    DOWNLOAD_INCOMING,
    DOWNLOAD_PAYLOAD,
    // ------------------ // 


    // ---- Upload ---- // 
    UPLOAD_APPROVED,
    UPLOAD_SUCCESS,
    UPLOAD_FAILURE,
    UPLOAD_REJECTED_FILENAME_TOO_LONG,
    UPLOAD_REJECTED_FILE_TOO_LARGE,
    UPLOAD_REJECTED_TOKENS_DEPLETED,
    UPLOAD_REJECTED_MAX_FILE_COUNT_REACHED,
    // ---------------- // 

    // ---- Chat ---- //
    CHAT_MESSAGE,
    CHAT_EXITED,
    // -------------- // 
}

public enum DatabaseFlag : byte
{
    INVALID_FLAG, // 0, do not move
    INVALID_IGNORE, // 1, do not move
    INVALID_IGNORE_TWO, // 2, do not move
    KEY_VALUE_DEPOSITED,
    USERNAME_PASS_VALIDATION_ERROR,
    DATABASE_ERROR,
    KEY_VALUE_MATCHES,
    KEY_DOESNT_EXIST,
    VALUE_DOESNT_MATCH,
    KEY_ALREADY_EXISTS,

}
