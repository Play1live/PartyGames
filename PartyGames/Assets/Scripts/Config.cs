using System;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

public class Config
{
    // Program Data
    public static ConfigFile APPLICATION_CONFIG;
    public static string APPLICATION_VERSION = Application.version;
    public static string MedienPath = Application.persistentDataPath + @"\Medien";
    public static bool DEBUG_MODE = false;
    public static bool REMOTECONFIG_FETCHTED = false;
    public static bool FULLSCREEN = false;
    public static bool APPLICATION_INITED = false;
    // Updater Data
    public static string UPDATER_DOWNLOAD_URL;
    public static string UPDATER_LATEST_VERSION;

    // Server Infos
    public static bool isServer = false;
    public static string SERVER_CONNECTION_IP = "localhost";
    public static int SERVER_CONNECTION_PORT = 11001;
    public static int SERVER_MAX_CONNECTIONS = 8;
    public static TcpListener SERVER_TCP;
    public static bool SERVER_STARTED;
    public static bool SERVER_ALL_CONNECTED;

    // Client Infos
    public static TcpClient CLIENT_TCP;
    public static bool CLIENT_STARTED;

    // Spieler stats
    public static Player SERVER_PLAYER;
    public static Player[] PLAYERLIST;
    public static List<Sprite> PLAYER_ICONS;
    public static int PLAYER_ID = 0;
    public static string PLAYER_NAME = "Spieler" + UnityEngine.Random.Range(1000, 10000);
    public static int MAX_PLAYER_NAME_LENGTH = 12;
    public static DateTime PingTime;

    // Spiele
    public static FlaggenSpiel FLAGGEN_SPIEL;
    public static QuizSpiel QUIZ_SPIEL;
    public static ListenSpiel LISTEN_SPIEL;
    public static MosaikSpiel MOSAIK_SPIEL;
    public static GeheimwörterSpiel GEHEIMWOERTER_SPIEL;
    public static WerBietetMehrSpiel WERBIETETMEHR_SPIEL;
    public static AuktionSpiel AUKTION_SPIEL;
    public static SloxikonSpiel SLOXIKON_SPIEL;

    // Scenenspezifisches
    public static string GAME_TITLE = "Startup";
    // Hauptmenue
    public static string HAUPTMENUE_FEHLERMELDUNG = "";
    // Lobby
    public static string LOBBY_FEHLERMELDUNG = "";
    public static bool ALLOW_PLAYERNAME_CHANGE = false;
    public static bool ALLOW_ICON_CHANGE = true;
}
