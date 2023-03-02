using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        NetworkStream stream = Config.CLIENT_TCP.GetStream();
        StreamWriter writer = new StreamWriter(stream);
        writer.WriteLine("Test Nachricht");
        writer.Flush();
        Debug.Log("Nachricht gesendet");
    }

    // Update is called once per frame
    void Update()
    {
        #region Server
        // Wenn der Server läuft
        if (Config.CLIENT_STARTED)
        {
            NetworkStream stream = Config.CLIENT_TCP.GetStream();
            if (stream.DataAvailable)
            {
                StreamReader reader = new StreamReader(stream, true);
                string data = reader.ReadLine();
                if (data != null)
                    OnIncomingData(data);
            }
        }
        #endregion
    }

    // Sent to all GameObjects when the player gets or loses focus.
    private void OnApplicationFocus(bool focus)
    {
        SendToServer("#ClientFocusChange " + focus);
    }
    //Sent to all GameObjects before the application quits.
    private void OnApplicationQuit()
    {
        Logging.add(new Logging(Logging.Type.Normal, "Client", "OnApplicationQuit", "Client wird geschlossen."));
        SendToServer("#ClientClosed");
        CloseSocket();
    }

    #region Client-Funktionen
    #region Verbindungen
    // Trennt die Verbindung zum Server
    private void CloseSocket()
    {
        if (!Config.CLIENT_STARTED)
            return;

        Config.CLIENT_TCP.Close();
        Config.CLIENT_STARTED = false;

        Logging.add(new Logging(Logging.Type.Normal, "Client", "CloseSocket", "Verbindung zum Server wurde getrennt. Client wird in das Hauptmenü geladen."));
    }
    // Spieler hat die falsche Version
    private void WrongVersion(string data)
    {
        Config.CLIENT_TCP.Close();
        Logging.add(new Logging(Logging.Type.Warning, "Client", "Wrong Version", "Client hat eine veraltete Spielversion."));
    }
    #endregion

    #region Kommunikation
    // Sendet eine Nachricht an den Server.
    public void SendToServer(string data)
    {
        if (!Config.CLIENT_STARTED)
            return;

        NetworkStream stream = Config.CLIENT_TCP.GetStream();
        StreamWriter writer = new StreamWriter(stream);

        writer.WriteLine(data);
        writer.Flush();
    }
    //Einkommende Nachrichten die vom Sever an den Spiler gesendet werden.
    private void OnIncomingData(string data)
    {
        string cmd;
        if (data.Contains(" "))
            cmd = data.Split(' ')[0];
        else
            cmd = data;


        Commands(data, cmd);
    }
    // Eingehende Commands des Server's
    public void Commands(string data, string cmd)
    {
        Debug.Log("Eingehend: " + data);
        // löscht den Command aus data
        data = data.Replace(cmd + " ", "");
        // Sucht nach Command
    }
}
#endregion
#endregion