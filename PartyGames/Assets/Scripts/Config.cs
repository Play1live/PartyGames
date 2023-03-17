using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using UnityEngine;

public class Config
{
    // Program Data
    public static string APPLICATION_VERSION = Application.version;
    public static string MedienPath = Application.persistentDataPath + @"\Medien";
    public static List<Logging> log;

    // Server Infos
    public static bool isServer = false;
    public static string SERVER_CONNECTION_IP = "192.168.1.217";
    public static int SERVER_CONNECTION_PORT = 11001;
    public static int SERVER_MAX_CONNECTIONS = 8;
    public static TcpListener SERVER_TCP;
    public static bool SERVER_STARTED;
    public static bool SERVER_ALL_CONNECTED;
    public static Sprite SERVER_DEFAULT_ICON = Resources.Load<Sprite>("Images/ProfileIcons/empty");
    public static int SERVER_PLAYER_POINTS = 0;

    // Client Infos
    public static TcpClient CLIENT_TCP;
    public static bool CLIENT_STARTED;

    // Spieler
    public static Player[] PLAYERLIST;
    public static List<Sprite> PLAYER_ICONS;
    public static string PLAYER_NAME = "Spieler" + UnityEngine.Random.Range(1000, 10000);
    public static int MAX_PLAYER_NAME_LENGTH = 12;
    public static int PLAYER_ID = 0;

    // Spiele
    public static FlaggenSpiel FLAGGEN_SPIEL;
    public static QuizSpiel QUIZ_SPIEL;
    public static ListenSpiel LISTEN_SPIEL;
    public static MosaikSpiel MOSAIK_SPIEL;
    public static GeheimwörterSpiel GEHEIMWOERTER_SPIEL;
    public static WerBietetMehrSpiel WERBIETETMEHR_SPIEL;
    public static AuktionSpiel AUKTION_SPIEL;

    // Scenenspezifisches
    // Hauptmenue
    public static string HAUPTMENUE_FEHLERMELDUNG = "";
    // Lobby
    public static bool ALLOW_PLAYERNAME_CHANGE = false;
    public static bool ALLOW_ICON_CHANGE = false;
}
