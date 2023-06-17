using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ServerUtils
{
    public static List<string> broadcastmsgs;
    /// <summary>
    /// Fügt eine Nachricht auf die Broadcast liste hinzu
    /// </summary>
    /// <param name="msg"></param>
    public static void AddBroadcast(string msg)
    {
        broadcastmsgs.Add(msg);
    }
    /// <summary>
    /// Sendet eine Nachticht an alle verbundenen Spieler. (Config.PLAYLIST)
    /// </summary>
    /// <param name="data">Nachricht</param>
    public static IEnumerator Broadcast()
    {
        broadcastmsgs = new List<string>();
        while (true)
        {
            // Broadcastet alle MSGs nacheinander
            if (broadcastmsgs.Count != 0)
            {
                string msg = broadcastmsgs[0];
                broadcastmsgs.RemoveAt(0);
                BroadcastImmediate(msg);
                yield return null;
            }
            yield return new WaitForSeconds(0.01f);
        }
    }
    /// <summary>
    /// Sendet eine Nachticht an alle verbundenen Spieler. (Config.PLAYLIST)
    /// </summary>
    /// <param name="data">Nachricht</param>
    public static void BroadcastImmediate(string msg)
    {
        foreach (Player sc in Config.PLAYERLIST)
            if (sc.isConnected)
                SendMSG(msg, sc);
    }
    /// <summary>
    /// Sendet eine Nachricht an den angegebenen Spieler.
    /// </summary>
    /// <param name="data">Nachricht</param>
    /// <param name="sc">Spieler</param>
    public static void SendMSG(string data, Player sc)
    {
        try
        {
            StreamWriter writer = new StreamWriter(sc.tcp.GetStream());
            writer.WriteLine(data);
            writer.Flush();
        }
        catch (Exception e)
        {
            Logging.log(Logging.LogType.Warning, "Server", "SendMSG", "Nachricht an Client: " + sc.id + " (" + sc.name + ") konnte nicht gesendet werden.", e);
            // Verbindung zum Client wird getrennt
            ClientClosed(sc);
        }
    }
    /// <summary>
    /// Löscht Daten des Spielers von dem die Verbindung getrennt wurde
    /// </summary>
    /// <param name="player">Spieler</param>
    public static void ClientClosed(Player player)
    {
        player.icon = Resources.Load<Sprite>("Images/ProfileIcons/empty");
        player.name = "";
        player.points = 0;
        player.crowns = 0;
        player.isConnected = false;
        player.isDisconnected = true;
    }
}
