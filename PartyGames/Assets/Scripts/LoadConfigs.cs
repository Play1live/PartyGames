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

    }

    #endregion
}
