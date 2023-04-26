using System.Collections;
using System.Collections.Generic;
using Unity.RemoteConfig;
using UnityEngine;

public class LoadConfigs
{
#pragma warning disable CS0618 // Typ oder Element ist veraltet
    public struct userAttributes { }
    public struct appAttriutes { }

    /// <summary>
    /// Lädt die RemoteConfig
    /// </summary>
    public static void FetchRemoteConfig()
    {
        Logging.log(Logging.LogType.Normal, "LoadConfigs", "FetchRemoteConfig", "Fetching Config...");
        ConfigManager.FetchCompleted += ApplyRemoteSettings;
        ConfigManager.FetchConfigs<userAttributes, appAttriutes>(new userAttributes(), new appAttriutes());
    }
    /// <summary>
    /// Speichert und Aktualisiert neue Werte der RemoteConfig
    /// </summary>
    /// <param name="config"></param>
    private static void ApplyRemoteSettings(ConfigResponse config)
    {
        Config.SERVER_CONNECTION_IP = ConfigManager.appConfig.GetString("Server_Connection_IP");
        Logging.log(Logging.LogType.Debug, "LoadConfigs", "ApplyRemoteSettings", "Server IP: "+ Config.SERVER_CONNECTION_IP);
        Config.SERVER_CONNECTION_PORT = ConfigManager.appConfig.GetInt("Server_Connection_Port");
        Logging.log(Logging.LogType.Debug, "LoadConfigs", "ApplyRemoteSettings", "Server Port: " + Config.SERVER_CONNECTION_PORT);
        Config.FULLSCREEN = ConfigManager.appConfig.GetBool("Program_Fullscreen");
        Logging.log(Logging.LogType.Debug, "LoadConfigs", "ApplyRemoteSettings", "Fullscreen: " + Config.FULLSCREEN);

        Config.UPDATER_DOWNLOAD_URL = ConfigManager.appConfig.GetString("Updater_Download_Link");
        Logging.log(Logging.LogType.Debug, "LoadConfigs", "ApplyRemoteSettings", "Updater URL: " + Config.UPDATER_DOWNLOAD_URL);
        Config.UPDATER_LATEST_VERSION = ConfigManager.appConfig.GetString("Updater_Download_Version");
        Logging.log(Logging.LogType.Debug, "LoadConfigs", "ApplyRemoteSettings", "Updater Version: " + Config.UPDATER_LATEST_VERSION);

        MoveToPrimaryDisplay(); // Schiebt das Programm auf den Primären Monitor
        Logging.log(Logging.LogType.Normal, "LoadConfigs", "ApplyRemoteSettings", "Fetching Config completed...");
    }

    /// <summary>
    /// Bewegt das Spiel auf den 1. Bildschirm & legt Vollbild fest
    /// </summary>
    private static void MoveToPrimaryDisplay()
    {
        if (Config.FULLSCREEN == true)
        {
            Logging.log(Logging.LogType.Debug, "LoadConfigs", "MoveToPrimaryDisplay", "Bewege Programm auf den primären Bildschirm.");
            List<DisplayInfo> displays = new List<DisplayInfo>();
            Screen.GetDisplayLayout(displays);
            Logging.log(Logging.LogType.Debug, "LoadConfigs", "MoveToPrimaryDisplay", displays.Count + " Bildschirme erkannt.");
            if (displays?.Count > 1) // don't bother running if only one display exists...
            {
                Screen.MoveMainWindowTo(displays[0], new Vector2Int(displays[0].width / 2, displays[0].height / 2));
            }

            Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
        }
        else
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
        }
    }
}
