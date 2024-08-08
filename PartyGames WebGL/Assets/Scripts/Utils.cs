
using NativeWebSocket;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Utils
{
    public static string EncryptDecrypt(string text, int key)
    {
        char[] chars = text.ToCharArray();  // Konvertiere den Text in ein Array von Zeichen
        for (int i = 0; i < chars.Length; i++)
        {
            chars[i] = (char)(chars[i] ^ key);  // Verwende XOR auf jedes Zeichen mit dem Schlüssel
        }
        return new string(chars);  // Konvertiere das Array zurück in einen String
    }
    public static void Log(string text)
    {
        if (!Config.hide_communication)
            Debug.Log(text);
    }
    public static void LoadPlayerIcons()
    {
        Config.player_icons = new List<Sprite>();
        foreach (var sprite in Resources.LoadAll<Sprite>("Images/ProfileIcons/"))
        {
            if (sprite.name.Equals("empty"))
                continue;
            if (sprite.name.StartsWith("#"))
                continue;
            Config.player_icons.Add(sprite);
        }
    }
}

public class ClientUtils
{
    public static void SetupConnection()
    {
        // Erstelle und verbinde den WebSocket
        Config.client = new WebSocket("ws://playgamesip.ddns.net:14002");

        Config.client.OnOpen += () => ClientUtils.OnSocketOpened();
        Config.msg_queue = new System.Collections.Generic.Queue<string>();
        Config.client.OnMessage += (message) => ClientUtils.OnSocketMessage(message);
        Config.client.OnError += (exception) => ClientUtils.OnSocketError(exception);
        Config.client.OnClose += (code) => ClientUtils.OnSocketClose(code);
    }
    public static void OnSocketOpened()
    {
        Utils.Log("WebSocket connection opened.");
    }

    public static void OnSocketMessage(byte[] msg)
    {
        string message = System.Text.Encoding.UTF8.GetString(msg);
        Utils.Log($"Received Message: {message}");
        lock (Config.msg_queue)
        {
            Config.msg_queue.Enqueue(message);
        }
    }

    public static void OnSocketError(string exception)
    {
        Utils.Log($"WebSocket encountered an error: {exception}");
    }

    public static void OnSocketClose(WebSocketCloseCode closeCode)
    {
        Utils.Log($"WebSocket closed with code: {closeCode}");
        SceneManager.LoadScene("Startup");
    }

    public static void SendMessage(string gametitle, string cmd, string message)
    {
        if (Config.client.State == WebSocketState.Open)
        {
            Config.client.SendText(gametitle + "|" + cmd + "|" + message);
            Utils.Log("send: " + cmd + "|" + message);
        }
    }
}