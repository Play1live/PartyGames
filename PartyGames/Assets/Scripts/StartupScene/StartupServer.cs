using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartupServer : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        #region Startet Server
        try
        {
            Config.SERVER_TCP = new TcpListener(IPAddress.Any, 11001); // TODO:
            Config.SERVER_TCP.Start();
            startListening();
            Config.SERVER_STARTED = true;
            Logging.add(new Logging(Logging.Type.Normal, "Server", "Start", "Server gestartet. Port: " + Config.SERVER_CONNECTION_PORT));
            GameObject.Find("ConnectingToServer_LBL").gameObject.GetComponent<TMP_Text>().text = "Server wurde gestartet.";
        }
        catch (Exception e)
        {
            Logging.add(new Logging(Logging.Type.Fatal, "Server", "Start", "Server kann nicht gestartet werden", e));
            GameObject.Find("ConnectingToServer_LBL").gameObject.GetComponent<TMP_Text>().text = "Server kann nicht gestartet werden.";
            try
            {
                Config.SERVER_TCP.Server.Close();
            }
            catch (Exception e1)
            {
                Logging.add(new Logging(Logging.Type.Fatal, "Server", "Start", "Socket kann nicht geschlossen werden.", e1));
            }
            Logging.add(new Logging(Logging.Type.Normal, "Server", "Start", "Client wird in das Hauptmenü geladen."));
            return;
        }
        #endregion
        // Füllt die Spielauswahl Anzeige
    }

    // Update is called once per frame
    void Update()
    {
        #region Server
        if (!Config.SERVER_STARTED)
            return;

        foreach (Player spieler in Config.PLAYERLIST)
        {

            if (spieler.isConnected == false)
                continue;

            
            #region Prüft ob Clients noch verbunden sind
            /*if (!isConnected(spieler.tcp) && spieler.isConnected == true)
            {
                Debug.LogWarning(spieler.id);
                spieler.tcp.Close();
                spieler.isConnected = false;
                spieler.isDisconnected = true;
                Logging.add(new Logging(Logging.Type.Normal, "Server", "Update", "Spieler ist nicht mehr Verbunden. ID: " + spieler.id));
                continue;
            }*/
            #endregion
            #region Sucht nach neuen Nachrichten
            /*else*/ if (spieler.isConnected == true)
            {
                NetworkStream stream = spieler.tcp.GetStream();
                if (stream.DataAvailable)
                {
                    //StreamReader reader = new StreamReader(stream, true);
                    StreamReader reader = new StreamReader(stream);
                    string data = reader.ReadLine();

                    if (data != null)
                        OnIncommingData(spieler, data);
                }
            }
            #endregion

            #region Spieler Disconnected Message
            for (int i = 0; i < Config.PLAYERLIST.Length; i++)
            {
                if (Config.PLAYERLIST[i].isConnected == false)
                {
                    if (Config.PLAYERLIST[i].isDisconnected == true)
                    {
                        Logging.add(new Logging(Logging.Type.Normal, "Server", "Update", "Spieler hat die Verbindung getrennt. ID: " + Config.PLAYERLIST[i].id));
                        Broadcast(Config.PLAYERLIST[i].name + " has disconnected", Config.PLAYERLIST);
                        Config.PLAYERLIST[i].isConnected = false;
                        Config.PLAYERLIST[i].isDisconnected = false;
                        Config.SERVER_ALL_CONNECTED = false;
                        Config.PLAYERLIST[i].name = "Disconnected";
                    }
                }
            }
            #endregion
        }
        #endregion
    }

    // Sent to all GameObjects before the application quits.
    private void OnApplicationQuit()
    {
        Broadcast("#ServerClosed", Config.PLAYERLIST);
        Logging.add(new Logging(Logging.Type.Normal, "Server", "OnApplicationQuit", "Server wird geschlossen"));
        Config.SERVER_TCP.Server.Close();
    }

    #region Server-Funktionen
    #region Verbindungen
    // Prüft ob ein Client noch mit dem Server verbunden ist
    private bool isConnected(TcpClient c)
    {
        /*try
        {
            if (c != null && c.Client != null && c.Client.Connected)
            {
                if (c.Client.Poll(0, SelectMode.SelectRead))
                {
                    return !(c.Client.Receive(new byte[1], SocketFlags.Peek) == 0);
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        catch
        {
            return false;
        }*/
        if (c != null && c.Client != null && c.Client.Connected)
        {
            if ((c.Client.Poll(0, SelectMode.SelectWrite)) && (!c.Client.Poll(0, SelectMode.SelectError)))
            {
                byte[] buffer = new byte[1];
                if (c.Client.Receive(buffer, SocketFlags.Peek) == 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }
    // Startet das empfangen von Nachrichten von Clients
    private void startListening()
    {
        Config.SERVER_TCP.BeginAcceptTcpClient(AcceptTcpClient, Config.SERVER_TCP);
    }
    // Fügt Client der Empfangsliste hinzu
    private void AcceptTcpClient(IAsyncResult ar)
    {
        // Spieler sind voll
        if (Config.SERVER_ALL_CONNECTED)
          return;

        // Sucht freien Spieler Platz
        Player freierS = null;
        foreach (Player sp in Config.PLAYERLIST)
        {
            if (sp.isConnected == false && sp.isDisconnected == false)
            {
                freierS = sp;
                break;
            }
        }
        // Spieler sind voll
        if (freierS == null)
            return;

        TcpListener listener = (TcpListener)ar.AsyncState;
        freierS.isConnected = true;
        freierS.tcp = listener.EndAcceptTcpClient(ar);

        // Prüft ob der Server voll ist
        bool tempAllConnected = true;
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            if (!Config.PLAYERLIST[i].isConnected)
            {
                tempAllConnected = false;
                break;
            }
        }
        Config.SERVER_ALL_CONNECTED = tempAllConnected;

        startListening();

        // Sendet neuem Spieler zugehörige ID
        SendMessage("#SetID " + freierS.id, freierS);
        Logging.add(new Logging(Logging.Type.Normal, "Server", "AcceptTcpClient", "Spieler: " + freierS.id + " ist jetzt verbunden. IP:" + freierS.tcp.Client.RemoteEndPoint));
    }
    #endregion

    #region Kommunikation
    // Sendet eine Nachricht an den angegebenen Spieler.
    private void SendMessage(string data, Player sc)
    {
        try
        {
            StreamWriter writer = new StreamWriter(sc.tcp.GetStream());
            writer.WriteLine(data);
            writer.Flush();
        }
        catch (Exception e)
        {
            Logging.add(new Logging(Logging.Type.Error, "Server", "SendMessage", "Nachricht an Client: " + sc.id + " (" + sc.name + ") konnte nicht gesendet werden." + e));
        }
    }
    // Sendet eine Nachticht an alle verbundenen Spieler.
    private void Broadcast(string data, Player[] spieler)
    {
        foreach (Player sc in spieler)
        {
            if (sc.isConnected)
                SendMessage(data, sc);
        }
    }
    //Einkommende Nachrichten die von Spielern an den Server gesendet werden.
    private void OnIncommingData(Player spieler, string data)
    {
        string cmd;
        if (data.Contains(" "))
            cmd = data.Split(' ')[0];
        else
            cmd = data;
        data = data.Replace(cmd + " ", "");

        Commands(spieler, data, cmd);
    }
    // Eingehende Commands der Spieler
    public void Commands(Player spieler, string data, string cmd)
    {
        // Zeigt alle einkommenden Nachrichten an
        Debug.Log(spieler.name + " " + spieler.id + " -> "+ cmd + "   ---   " + data);
        // Sucht nach Command
        
    }
}
#endregion
#endregion