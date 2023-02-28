using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartupClient : MonoBehaviour
{
    [SerializeField] GameObject Hauptmenue;
    [SerializeField] GameObject Lobby;

    // Start is called before the first frame update
    void Start()
    {
        #region Client Verbindungsaufbau zum Server
        // Create the socket
        try
        {
            Config.CLIENT_TCP = new TcpClient("localhost", 11001); // TODO: port
            Config.CLIENT_STARTED = true;
            Logging.add(new Logging(Logging.Type.Normal, "Client", "Start", "Verbindung zum Server wurde hergestellt."));
            GameObject.Find("ConnectingToServer_LBL").gameObject.GetComponent<TMP_Text>().text = "Verbindung zum Server wurde hergestellt.";
        }
        catch (Exception e)
        {
            Logging.add(new Logging(Logging.Type.Fatal, "Client", "Start", "Verbindung zum Server nicht möglich.", e));
            GameObject.Find("ConnectingToServer_LBL").gameObject.GetComponent<TMP_Text>().text = "Verbindung zum Server nicht möglich. \n" + e;
            try
            {
                CloseSocket();
            }
            catch (Exception e1)
            {
                Logging.add(new Logging(Logging.Type.Fatal, "Client", "Start", "Socket konnte nicht geschlossen werden.", e1));
                //ConnectingToServerLBL.GetComponent<TMP_Text>().text = ConnectingToServerLBL.GetComponent<TMP_Text>().text + "\n\nVerbindung zum Server nicht möglich." + e;
            }
            Logging.add(new Logging(Logging.Type.Normal, "Client", "Start", "Client wird ins Hauptmenü geladen."));
            return;
        }
        #endregion
    }

    // Update is called once per frame
    void Update()
    {
        #region Prüft auf Nachrichten vom Server
        if (Config.CLIENT_STARTED)
        {
            SendToServer("update message"); // TODO
            NetworkStream stream = Config.CLIENT_TCP.GetStream();
            if (stream.DataAvailable)
            {
                StreamReader reader = new StreamReader(stream);
                SendToServer("update message"); // TODO
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
        data = data.Replace(cmd + " ", "");

        Commands(data, cmd);
    }
    // Eingehende Commands des Server's
    public void Commands(string data, string cmd)
    {
        Debug.Log("Eingehend: " + cmd + " -> "+data);
        switch (cmd)
        {
            default:
                Debug.LogWarning("Unkown Command -> "+ cmd +" - "+data);
                break;

        }
    }
}
#endregion
#endregion