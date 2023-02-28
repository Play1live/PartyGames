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
    public static string SERVER_DISPLAY_NAME;

    // Client Infos
    public static TcpClient CLIENT_TCP;
    public static bool CLIENT_STARTED;

    // Spieler
    public static Player[] PLAYERLIST;
    public static string PLAYER_NAME = "Spieler" + UnityEngine.Random.Range(1000, 10000);

}
