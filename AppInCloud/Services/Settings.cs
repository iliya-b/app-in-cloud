using System.Text.Json;

namespace AppInCloud.Services;


public class Settings {
    public static string REGISTRATION_ENABLED_CACHE_KEY = "EnableRegistration";

    public static void Update(string key, object value){
        if(key.Contains(':')) throw new Exception("Cannot save with key more than one layer deep");
        var configJson = File.ReadAllText("appsettings.json");
        var config = JsonSerializer.Deserialize<Dictionary<string, object>>(configJson);
        config[key] = value;
        var updatedConfigJson = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText("appsettings.json", updatedConfigJson);
    }
}