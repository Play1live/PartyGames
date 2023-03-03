using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class QuizServer : MonoBehaviour
{
    GameObject Frage;
    GameObject FragenAnzeige;
    GameObject FragenIndex1;
    GameObject FragenIndex2;
    GameObject BuzzerAnzeige;
    bool buzzerIsOn = false;
    GameObject FalscheAntworten;
    GameObject TextEingabeAnzeige;
    GameObject TextAntwortenAnzeige;
    GameObject[,] SpielerAnzeige;
    bool[] PlayerConnected;
    int PunkteProRichtige = 4;
    int PunkteProFalsche = 1;

    // Start is called before the first frame update
    void OnEnable()
    {
        PlayerConnected = new bool[Config.SERVER_MAX_CONNECTIONS];
        InitAnzeigen();
        // InitQuiz
    }

    // Update is called once per frame
    void Update()
    {
        #region Server
        if (!Config.SERVER_STARTED)
        {
            SceneManager.LoadScene("Startup");
            return;
        }

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
            /*else*/
            if (spieler.isConnected == true)
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
                        Config.PLAYERLIST[i].name = "";
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

    #region Server Stuff
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
    /*private void startListening()
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
    }*/
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
    // Sendet eine Nachticht an alle verbundenen Spieler.
    private void Broadcast(string data)
    {
        foreach (Player sc in Config.PLAYERLIST)
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
    #endregion
    // Eingehende Commands der Spieler
    public void Commands(Player player, string data, string cmd)
    {
        // Zeigt alle einkommenden Nachrichten an
        Debug.Log(player.name + " " + player.id + " -> " + cmd + "   ---   " + data);
        // Sucht nach Command
        switch (cmd)
        {
            default:
                Debug.LogWarning("Unkown Command -> " + cmd + " - " + data);
                break;

            case "#ClientClosed":
                for (int i = 0; i < Config.PLAYERLIST.Length; i++)
                {
                    if (Config.PLAYERLIST[i].id == player.id)
                    {
                        Config.PLAYERLIST[i].name = "";
                        Config.PLAYERLIST[i].isConnected = false;
                        Config.PLAYERLIST[i].isDisconnected = true;
                        break;
                    }
                }
                UpdateSpielerBroadcast();
                break;
            case "#ClientFocusChange":
                Debug.Log("FocusChange (" + player.id + ") " + player.name + ": InGame: " + data);
                break;

            case "#JoinQuiz":
                PlayerConnected[player.id - 1] = true;
                UpdateSpielerBroadcast();
                break;
        }
    }
    #endregion
    // Fordert alle Clients auf die RemoteConfig neuzuladen
    public void UpdateRemoteConfig()
    {
        Broadcast("#UpdateRemoteConfig");
        LoadConfigs.FetchRemoteConfig();
    }
    // Sendet aktualisierte Spielerinfos an alle Spieler
    // -> UpdateSpieler()
    private void UpdateSpielerBroadcast()
    {
        Broadcast(UpdateSpieler(), Config.PLAYERLIST);
    }
    private string UpdateSpieler()
    {
        string msg = "#UpdateSpieler [ID]0[ID][NAME]" + Config.PLAYER_NAME + "[NAME][PUNKTE]" + Config.SERVER_PLAYER_POINTS + "[PUNKTE][ICON]" + Config.SERVER_DEFAULT_ICON.name + "[ICON]";
        int connectedplayer = 0;
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            Player p = Config.PLAYERLIST[i];
            msg += "[TRENNER][ID]" + p.id + "[ID][NAME]" + p.name + "[NAME][PUNKTE]" + p.points + "[PUNKTE][ICON]" + p.icon.name + "[ICON]";
            if (p.isConnected && PlayerConnected[i])
            {
                connectedplayer++;
                SpielerAnzeige[i, 0].SetActive(true);
                SpielerAnzeige[i, 2].GetComponent<Image>().sprite = p.icon;
                SpielerAnzeige[i, 4].GetComponent<TMP_Text>().text = p.name;
                SpielerAnzeige[i, 5].GetComponent<TMP_Text>().text = p.points+"";
            }
            else
                SpielerAnzeige[i, 0].SetActive(false);

        }
        // Server 
        FalscheAntworten.GetComponent<TMP_Text>().text = "Falsche Antworten: "+Config.SERVER_PLAYER_POINTS;
        return msg;
    }



    private void InitAnzeigen()
    {
        // Fragen Anzeige
        Frage = GameObject.Find("Frage");
        Frage.GetComponentInChildren<TMP_Text>().text = "";
        GameObject.Find("ServerSide/FrageAnzeigenToggle").GetComponent<Toggle>().isOn = false;
        FragenAnzeige = GameObject.Find("ServerSide/FrageWirdAngezeigt");
        FragenAnzeige.SetActive(false);
        FragenIndex1 = GameObject.Find("QuizAnzeigen/FragenIndex1");
        FragenIndex1.GetComponentInChildren<TMP_Text>().text = "0/0";
        FragenIndex2 = GameObject.Find("QuizAnzeigen/FragenIndex2");
        FragenIndex2.GetComponentInChildren<TMP_Text>().text = "0/0";
        FalscheAntworten = GameObject.Find("QuizAnzeigen/FalscheAntwortenCounter");
        Config.SERVER_PLAYER_POINTS = 0;
        FalscheAntworten.GetComponent<TMP_Text>().text = "Falsche Antworten: "+Config.SERVER_PLAYER_POINTS;
        // Buzzer Deaktivieren
        GameObject.Find("ServerSide/BuzzerAktivierenToggle").GetComponent<Toggle>().isOn = false;
        BuzzerAnzeige = GameObject.Find("ServerSide/BuzzerIstAktiviert");
        BuzzerAnzeige.SetActive(false);
        // Spieler Texteingabe
        GameObject.Find("ServerSide/TexteingabeAnzeigenToggle").GetComponent<Toggle>().isOn = false;
        TextEingabeAnzeige = GameObject.Find("ServerSide/TexteingabeWirdAngezeigt");
        TextEingabeAnzeige.SetActive(false);
        GameObject.Find("ServerSide/TextantwortenAnzeigenToggle").GetComponent<Toggle>().isOn = false;
        TextAntwortenAnzeige = GameObject.Find("ServerSide/TextantwortenWerdenAngezeigt");
        TextAntwortenAnzeige.SetActive(false);
        // Punkte Pro Richtige Antwort
        GameObject.Find("ServerSide/PunkteProRichtigeAntwort").GetComponent<TMP_InputField>().text = ""+PunkteProRichtige;
        // Punkte Pro Falsche Antwort
        GameObject.Find("ServerSide/PunkteProFalscheAntwort").GetComponent<TMP_InputField>().text = ""+PunkteProFalsche;
        // Spieler Anzeige
        SpielerAnzeige = new GameObject[Config.SERVER_MAX_CONNECTIONS, 7]; // Anzahl benötigter Elemente
        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
        {
            PlayerConnected[i] = false;
            SpielerAnzeige[i, 0] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")"); // Spieler Anzeige
            SpielerAnzeige[i, 1] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/BuzzerPressed"); // BuzzerPressed Umrandung
            SpielerAnzeige[i, 2] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Icon"); // Spieler Icon
            SpielerAnzeige[i, 3] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Ausgetabt"); // Ausgetabt Einblednung
            SpielerAnzeige[i, 4] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Infobar/Name"); // Spieler Name
            SpielerAnzeige[i, 5] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Infobar/Punkte"); // Spieler Punkte
            SpielerAnzeige[i, 6] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/SpielerAntwort"); // Spieler Antwort

            SpielerAnzeige[i, 0].SetActive(false); // Spieler Anzeige
            SpielerAnzeige[i, 1].SetActive(false); // BuzzerPressed Umrandung
            SpielerAnzeige[i, 3].SetActive(false); // Ausgetabt Einblendung
            SpielerAnzeige[i, 6].SetActive(false); // Spieler Antwort
        }
    }

    // Aktiviert/Deaktiviert den Buzzer für alle Spieler
    public void BuzzerAktivierenToggle(Toggle toggle)
    {
        buzzerIsOn = toggle.isOn;
        BuzzerAnzeige.SetActive(toggle.isOn);
        //Broadcast("#BuzzerToggle " + toggle.isOn); Ist für Spieler immer an, nur Server reagiert nicht drauf wenn aus
    }
    // Zeigt/Versteckt die Frage für alle Spieler
    public void FrageAnzeigenToggle(Toggle toggle)
    {
        FragenAnzeige.SetActive(toggle.isOn);
        Broadcast("#FragenAnzeige [BOOL]"+toggle.isOn+"[BOOL][FRAGE]"+Frage.GetComponentInChildren<TMP_Text>().text);
    }
    // Blendet die selbst eingegebene Frage ein
    public void EigeneFrageEinblenden(TMP_InputField input)
    {
        Frage.GetComponentInChildren<TMP_Text>().text = input.text;
        if (FragenAnzeige.activeInHierarchy)
            Broadcast("#FragenAnzeige [BOOL]" + FragenAnzeige.activeInHierarchy + "[BOOL][FRAGE]" + input.text);
        input.text = "";
    }
    // Blendet die Texteingabe für die Spieler ein
    public void TexteingabeAnzeigenToggle(Toggle toggle)
    {
        TextEingabeAnzeige.SetActive(toggle.isOn);
        Broadcast("#TexteingabeAnzeigen "+ toggle.isOn);
    }
    // Blendet die Textantworten der Spieler ein
    public void TextantwortenAnzeigeToggle(Toggle toggle)
    {
        TextAntwortenAnzeige.SetActive(toggle.isOn);
        string msg = "";

        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
        {
            msg = msg + "[ID" + (i + 1) + "]" + SpielerAnzeige[i, 6].GetComponentInChildren<TMP_InputField>().text + "[ID" + (i + 1) + "]";
        }
        Broadcast("#TextantwortenAnzeigen "+ msg);
    }
    // Punkte Pro Richtige Antworten Anzeigen
    public void ChangePunkteProRichtigeAntwort(TMP_InputField input)
    {
        PunkteProRichtige = Int32.Parse(input.text);
    }
    // Punkte Pro Falsche Antworten Anzeigen
    public void ChangePunkteProFalscheAntwort(TMP_InputField input)
    {
        PunkteProFalsche = Int32.Parse(input.text);
    }
    // Spiel Verlassen
    public void SpielVerlassenButton()
    {
        SceneManager.LoadScene("Startup");
        Broadcast("#ZurueckInsHauptmenue");
    }

}
