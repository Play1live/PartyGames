using System.Collections;
using System.Collections.Generic;
using Unity.RemoteConfig;
using UnityEngine;

public class LoadConfigs
{
    #region RemoteConfig Stuff
#pragma warning disable CS0618 // Typ oder Element ist veraltet
    public struct userAttributes { }
    public struct appAttriutes { }

    /// <summary>
    /// Lädt die RemoteConfig
    /// </summary>
    public static void FetchRemoteConfig()
    {
        Logging.log(Logging.LogType.Normal, "LoadConfigs", "FetchRemoteConfig", "Fetching Config...");
        ConfigManager.FetchCompleted += SaveRemoteSettings;
        ConfigManager.FetchConfigs<userAttributes, appAttriutes>(new userAttributes(), new appAttriutes());
    }
    /// <summary>
    /// Speichert und Aktualisiert neue Werte der RemoteConfig
    /// </summary>
    /// <param name="config"></param>
    private static void SaveRemoteSettings(ConfigResponse config)
    {
        // Server
        Config.SERVER_CONNECTION_IP = ConfigManager.appConfig.GetString("Server_Connection_IP");
        Logging.log(Logging.LogType.Normal, "LoadConfigs", "SaveRemoteSettings", "Server IP: "+ Config.SERVER_CONNECTION_IP);
        Config.SERVER_CONNECTION_PORT = ConfigManager.appConfig.GetInt("Server_Connection_Port");
        Logging.log(Logging.LogType.Normal, "LoadConfigs", "SaveRemoteSettings", "Server Port: " + Config.SERVER_CONNECTION_PORT);
        // Programm Einstellungen
        Config.FULLSCREEN = ConfigManager.appConfig.GetBool("Program_Fullscreen");
        Logging.log(Logging.LogType.Normal, "LoadConfigs", "SaveRemoteSettings", "Fullscreen: " + Config.FULLSCREEN);
        // Updater
        Config.UPDATER_LATEST_VERSION = ConfigManager.appConfig.GetString("Updater_Download_Version");
        Logging.log(Logging.LogType.Normal, "LoadConfigs", "SaveRemoteSettings", "Updater Version: " + Config.UPDATER_LATEST_VERSION);
        Config.UPDATER_DOWNLOAD_URL = ConfigManager.appConfig.GetString("Updater_Download_Link");
        Config.UPDATER_DOWNLOAD_URL.Replace("<version>", Config.UPDATER_LATEST_VERSION);
        Logging.log(Logging.LogType.Normal, "LoadConfigs", "SaveRemoteSettings", "Updater URL: " + Config.UPDATER_DOWNLOAD_URL);

        ApplyRemoteSettings();
        Logging.log(Logging.LogType.Normal, "LoadConfigs", "SaveRemoteSettings", "Fetching Config completed...");
        Config.REMOTECONFIG_FETCHTED = true;
    }
    private static void ApplyRemoteSettings()
    {
        MoveToPrimaryDisplayFullscreen(); // Schiebt das Programm auf den Primären Monitor
    }
    /// <summary>
    /// Bewegt das Spiel auf den 1. Bildschirm & legt Vollbild fest
    /// </summary>
    public static void MoveToPrimaryDisplayFullscreen()
    {
        /*
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

            Screen.SetResolution(Display.displays[0].renderingWidth, Display.displays[0].renderingHeight, true);
            Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
        }
        else
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
        }*/

        Utils.EinstellungenGrafikApply(true);
    }
    #endregion
}
