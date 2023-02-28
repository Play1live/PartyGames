using Unity.RemoteConfig;
using UnityEngine;

public class LoadConfigs
{
    public struct userAttributes { }
    public struct appAttriutes { }

    public static void FetchRemoteConfig()
    {
        Debug.Log("Fetching Config...");
        ConfigManager.FetchCompleted += ApplyRemoteSettings;
        ConfigManager.FetchConfigs<userAttributes, appAttriutes>(new userAttributes(), new appAttriutes());
    }

    private static void ApplyRemoteSettings(ConfigResponse config)
    {
        Config.SERVER_CONNECTION_IP = ConfigManager.appConfig.GetString("Server_Connection_IP");
        Config.SERVER_CONNECTION_PORT = ConfigManager.appConfig.GetInt("Server_Connection_Port");

        Debug.Log("Fetching completed!");
    }
}
