using System.Text.Json;


namespace CloudLib;
public static class JsonHelpers
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

        if (oldFileContent == ""){
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
}

// Add a few classes for exceptions with regards to JSON functionality. 