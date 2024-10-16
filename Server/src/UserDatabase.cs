using CloudLib;
using System.IO;

namespace Server.src;

internal class UserDatabase
{
    private static UserDatabase? _instance;
    private UserDatabase()
    {
        Debug.Assert(CWD.EndsWith("CloudCastle2"));

        // 1. Create the registered accounts file, if it doesn't exist yet.
        if (!Directory.Exists(SERVER_DIRECTORY_PATH)){
            throw new Exception("Server directory didn't exist. Error in UserDatabase()");
        }
        if (!Directory.Exists(DATABASE_DIRECTORY_PATH)){
            Directory.CreateDirectory(DATABASE_DIRECTORY_PATH);
        }
        if (!Directory.Exists(REGISTERED_ACCOUNTS_DIRECTORY_PATH)){
            Directory.CreateDirectory(REGISTERED_ACCOUNTS_DIRECTORY_PATH);
        }
        if (!File.Exists(REGISTERED_ACCOUNTS_FILE_PATH)){
            File.Create(REGISTERED_ACCOUNTS_FILE_PATH);
        }

        // 2. Create the CloudStorage folder, if it doesn't exixt yet.
        if (!Directory.Exists(CLOUD_STORAGE_DIRECTORY_PATH)){
            Directory.CreateDirectory(CLOUD_STORAGE_DIRECTORY_PATH);
        }
    }
    public static UserDatabase Instance {
        get {
            return _instance ??= new UserDatabase();
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
    private static readonly string REGISTERED_ACCOUNTS_FILE_PATH = Path.Combine(REGISTERED_ACCOUNTS_DIRECTORY_PATH, REGISTERED_ACCOUNTS_FILE_NAME);
    private static readonly string CLOUD_STORAGE_DIRECTORY_PATH = Path.Combine(DATABASE_DIRECTORY_PATH, CLOUD_STORAGE_FOLDER_NAME);


    public DatabaseFlags TryToRegister(string usernamePassword)
    {
        // TODO NEXT: Implement this.        

        return DatabaseFlags.INVALID_FLAG;
    }
}