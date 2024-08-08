
using NativeWebSocket;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Config
{
    public static bool hide_communication = false;
    public static WebSocket client;
    public static Guid uuid;
    public static Queue<string> msg_queue;
    public static bool connected;
    public static Player spieler;
    public static List<Player> players;
    public static List<Sprite> player_icons;


}
