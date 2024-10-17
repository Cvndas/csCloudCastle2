using CloudLib;
using System.IO;
using System.Text.Json;

namespace Server.src;

internal static class UserDatabase
{
    static UserDatabase()
    {
        Debug.Assert(CWD.EndsWith("CloudCastle2"));
        _registered_users_json_lock = new object();

        // 1. Create the registered accounts file, if it doesn't exist yet.
        if (!Directory.Exists(SERVER_DIRECTORY_PATH)) {
            throw new Exception("Server directory didn't exist. Error in UserDatabase()");
        }
        if (!Directory.Exists(DATABASE_DIRECTORY_PATH)) {
            Directory.CreateDirectory(DATABASE_DIRECTORY_PATH);
        }
        if (!Directory.Exists(REGISTERED_ACCOUNTS_DIRECTORY_PATH)) {
            Directory.CreateDirectory(REGISTERED_ACCOUNTS_DIRECTORY_PATH);
        }
        if (!File.Exists(CR_REGISTERED_ACCOUNTS_FILE_PATH)) {
            File.Create(CR_REGISTERED_ACCOUNTS_FILE_PATH);
        }

        // 2. Create the CloudStorage folder, if it doesn't exixt yet.
        if (!Directory.Exists(CLOUD_STORAGE_DIRECTORY_PATH)) {
            Directory.CreateDirectory(CLOUD_STORAGE_DIRECTORY_PATH);
        }
    }

    // ------------------- CONSTANTS --------------------- // 
    private static readonly string CWD = Directory.GetCurrentDirectory();
    private static readonly string SERVER_FOLDER_NAME = "Server";
    private static readonly string DATABASE_FOLDER_NAME = "UserDatabase";
    private static readonly string CLOUD_STORAGE_FOLDER_NAME = "CloudStorage";
    private static readonly string REGISTERED_ACCOUNTS_FOLDER_NAME = "RegisteredAccounts";
    private static readonly string REGISTERED_ACCOUNTS_FILE_NAME = "registered_users.json";
    // banned accounts
    // banned ips
    // etc.

    private static readonly string SERVER_DIRECTORY_PATH = Path.Combine(CWD, SERVER_FOLDER_NAME);
    private static readonly string DATABASE_DIRECTORY_PATH = Path.Combine(SERVER_DIRECTORY_PATH, DATABASE_FOLDER_NAME);
    private static readonly string REGISTERED_ACCOUNTS_DIRECTORY_PATH = Path.Combine(DATABASE_DIRECTORY_PATH, REGISTERED_ACCOUNTS_FOLDER_NAME);
    private static readonly string CR_REGISTERED_ACCOUNTS_FILE_PATH = Path.Combine(REGISTERED_ACCOUNTS_DIRECTORY_PATH, REGISTERED_ACCOUNTS_FILE_NAME);
    private static readonly string CLOUD_STORAGE_DIRECTORY_PATH = Path.Combine(DATABASE_DIRECTORY_PATH, CLOUD_STORAGE_FOLDER_NAME);
    // ---------------------------------------------------- // 


    private static readonly object _registered_users_json_lock;

    public static DatabaseFlags TryToRegister(string username, string password)
    {
        Debug.Assert(File.Exists(CR_REGISTERED_ACCOUNTS_FILE_PATH));

        DatabaseFlags retFlag = DatabaseFlags.INVALID_FLAG;

        lock (_registered_users_json_lock) {
            try {
                // Check if the username already existed.
                if (JsonHelpers.KeyExists(CR_REGISTERED_ACCOUNTS_FILE_PATH, username)) {
                    retFlag = DatabaseFlags.KEY_ALREADY_EXISTS;
                }
                // If not, you're good to go.
                else {
                    JsonHelpers.AddKeyValuePair(CR_REGISTERED_ACCOUNTS_FILE_PATH, username, password);
                    retFlag = DatabaseFlags.KEY_VALUE_DEPOSITED;
                }
            }
            catch (Exception e) {
                Console.WriteLine("Error in TryToRegister: " + e.Message);
            }
        }

        return retFlag;
    }

    public static DatabaseFlags TryToLogin(string username, string password)
    {
        // TODO : Next
        return DatabaseFlags.INVALID_FLAG;
    }
}