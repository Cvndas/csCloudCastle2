using System.Text.Json;


namespace CloudLib;
public static class DatabaseJsonHelpers
{
    public static bool KeyExists(string filepath, string key)
    {
        string fileContent = File.ReadAllText(filepath);
        if (fileContent == "") {
            return false;
        }

        var fileContentDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(fileContent)
                                  ??
                                  throw new Exception("registered_users.json is corrupt.");

        if (fileContentDictionary!.ContainsKey(key)) {
            return true;
        }
        else {
            return false;
        }
    }

    public static void AddKeyValuePair(string filepath, string key, string value)
    {
        string oldFileContent = File.ReadAllText(filepath);
        string newFileContent;

        if (oldFileContent == "") {
            var newfileContentDictionary = new Dictionary<string, string>{
                {key, value}
            };
            newFileContent = JsonSerializer.Serialize(newfileContentDictionary);
        }
        else {
            var oldFileContentDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(oldFileContent)
                                         ??
                                         throw new Exception("registered_users.json is corrupt");

            oldFileContentDictionary.Add(key, value);
            newFileContent = JsonSerializer.Serialize(oldFileContentDictionary);
        }
        File.WriteAllText(filepath, newFileContent);
    }

    /// <summary>
    /// Note: not thread safe. Caller must lock on the filepath.
    /// </summary>
    /// <returns></returns>
    public static DatabaseFlag KeyValStatus(string filepath, string key, string value)
    {
        string fileContent = File.ReadAllText(filepath);
        if (fileContent == ""){
            return DatabaseFlag.KEY_DOESNT_EXIST;
        }

        var fileContentDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(fileContent)
                                  ??
                                  throw new Exception("registered_users.json is corrupt");
        if (!fileContentDictionary.ContainsKey(key)){
            return DatabaseFlag.KEY_DOESNT_EXIST;
        }
        if (!fileContentDictionary.TryGetValue(key, out string? valueFromDatabase)) {
            Console.WriteLine("Database had no value for key " + key);
            return DatabaseFlag.DATABASE_ERROR;
        }
        if (valueFromDatabase == value){
            return DatabaseFlag.KEY_VALUE_MATCHES;
        }
        else {
            return DatabaseFlag.VALUE_DOESNT_MATCH;
        }
    }
}
