using System.Collections;
using System.Collections.Generic;
using Unity.RemoteConfig;
using UnityEngine;

public class LoadConfigs
{
    public struct userAttributes { }
    public struct appAttriutes { }

    public static void FetchRemoteConfig()
    {
        Logging.add(Logging.Type.Normal, "LoadConfigs", "FetchRemoteConfig", "Fetching Config...");
        ConfigManager.FetchCompleted += ApplyRemoteSettings;
        ConfigManager.FetchConfigs<userAttributes, appAttriutes>(new userAttributes(), new appAttriutes());
    }

    private static void ApplyRemoteSettings(ConfigResponse config)
    {
        Config.SERVER_CONNECTION_IP = ConfigManager.appConfig.GetString("Server_Connection_IP");
        Config.SERVER_CONNECTION_PORT = ConfigManager.appConfig.GetInt("Server_Connection_Port");
        Config.FULLSCREEN = ConfigManager.appConfig.GetBool("Program_Fullscreen");

        Logging.add(Logging.Type.Normal, "LoadConfigs", "FetchRemoteConfig", "Fetching Config completed...");
        
        
        MoveToPrimaryDisplay(); // Schiebt das Programm auf den Primären Monitor
    }

    private static void MoveToPrimaryDisplay()
    {
        // Bewegt Bild nicht, wenn kein Vollbild
        if (Config.FULLSCREEN == false)
            return;

        List<DisplayInfo> displays = new List<DisplayInfo>();
        Screen.GetDisplayLayout(displays);
        if (displays?.Count > 1) // don't bother running if only one display exists...
        {
            Screen.MoveMainWindowTo(displays[0], new Vector2Int(displays[0].width / 2, displays[0].height / 2)); 
        }

        if (Config.FULLSCREEN == true)
        {
            Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
        }
        else
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
        }
    }
}
