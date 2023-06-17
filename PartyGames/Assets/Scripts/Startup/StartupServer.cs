using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartupServer : MonoBehaviour
{
    [SerializeField] GameObject Hauptmenue;
    [SerializeField] GameObject Lobby;
    [SerializeField] GameObject[] SpielerMiniGames;
    [SerializeField] GameObject ServerControl;
    [SerializeField] GameObject ServerControlGameSelection;
    [SerializeField] GameObject ServerControlControlField;
    [SerializeField] GameObject[] SpielerAnzeigeLobby;

    [SerializeField] GameObject gesperrtfuerSekundenAnzeige;
    DateTime allowedStartTime;
    string UpdateClientGameVorschau = "";
    private int connectedPlayer;

    [SerializeField] AudioSource ConnectSound;
    [SerializeField] AudioSource DisconnectSound;

    void Start()
    {
        UpdateSpieler();
        StartCoroutine(ServerUtils.Broadcast());
    }

    void OnEnable()
    {
        if (Config.SERVER_STARTED)
            SperreGameSelection();

        if (!Config.SERVER_STARTED)
            StarteServer();

        InitPlayerLobby();

        Hauptmenue.SetActive(false);
        Lobby.SetActive(true);
        ServerControl.SetActive(true);
        SpielerMiniGames[0].transform.parent.gameObject.SetActive(false);

        if (ServerControlGameSelection.activeInHierarchy && ServerControlGameSelection.transform.GetChild(1).gameObject.activeInHierarchy)
            DisplayGameFiles();
        UpdateGameVorschau();
        UpdateSpieler();
        UpdateCrowns();
    }

    void Update()
    {
        #region Server
        if (!Config.SERVER_STARTED)
            return;

        // Schaut ob die Wartezeit vorbei ist
        if (allowedStartTime != null && allowedStartTime != DateTime.MinValue)
        {
            UpdateGesperrtGameSelection();
            if (DateTime.Compare(allowedStartTime, DateTime.Now) < 0)
            {
                EntsperreGameSelection();
            }
        }

        
        foreach (Player spieler in Config.PLAYERLIST)
        {
            if (spieler.isConnected == false)
                continue;

            #region Sucht nach neuen Nachrichten
            if (spieler.isConnected == true && spieler.tcp != null)
            {
                // Spieler Verbindung ist weg
                if (!spieler.tcp.Connected)
                {
                    Logging.log(Logging.LogType.Normal, "StartupServer", "Update", "Spieler hat die Verbindung verloren. " + spieler.name);
                    spieler.isConnected = false;
                    spieler.isDisconnected = false;
                    Config.SERVER_ALL_CONNECTED = false;
                    spieler.name = "";
                    continue;
                }

                NetworkStream stream = spieler.tcp.GetStream();
                if (stream.DataAvailable)
                {
                    StreamReader reader = new StreamReader(stream);
                    string data = reader.ReadLine();

                    if (data != null)
                        OnIncommingData(spieler, data);
                }
            }
            #endregion
        }
        #region Spieler Disconnected Message
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            if (Config.PLAYERLIST[i].isConnected == false)
            {
                if (Config.PLAYERLIST[i].isDisconnected == true)
                {
                    Logging.log(Logging.LogType.Normal, "StartupServer", "Update", "Spieler hat die Verbindung getrennt. ID: " + Config.PLAYERLIST[i].id);
                    Broadcast(Config.PLAYERLIST[i].name + " has disconnected", Config.PLAYERLIST);
                    Config.PLAYERLIST[i].isConnected = false;
                    Config.PLAYERLIST[i].isDisconnected = false;
                    Config.SERVER_ALL_CONNECTED = false;
                    Config.PLAYERLIST[i].name = "";
                }
            }
        }
        #endregion
        #endregion
    }

    void OnApplicationQuit()
    {
        ServerUtils.BroadcastImmediate("#ServerClosed");
        Logging.log(Logging.LogType.Normal, "StartupServer", "OnApplicationQuit", "Server wird geschlossen.");
        //Config.SERVER_TCP.Server.Close();
        Config.SERVER_TCP.Stop();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    public void ZurueckZumHauptmenue()
    {
        Logging.log(Logging.LogType.Normal, "StartupServer", "ZurueckZumHauptmenue", "Spieler wird ins Hauptmenü geladen und Server- & Client-Verbindung wird beendet.");
        if (Config.isServer && Config.SERVER_STARTED)
        {
            ServerUtils.AddBroadcast("#ServerClosed");
            Config.SERVER_TCP.Stop();
            Config.SERVER_STARTED = false;
            SceneManager.LoadSceneAsync("Startup");
            GameObject.Find("ServerController").gameObject.SetActive(false);
        }
    }
    #region Verbindungen
    /// <summary>
    /// Startet den Server
    /// </summary>
    private void StarteServer()
    {
        Logging.log(Logging.LogType.Normal, "StartupServer", "Start", "Server wird gestartet...");
        try
        {
            Config.SERVER_TCP = new TcpListener(IPAddress.Any, Config.SERVER_CONNECTION_PORT);
            Config.SERVER_TCP.Server.NoDelay = true;
            Config.SERVER_TCP.Start();
            startListening();
            Config.SERVER_STARTED = true;
            Logging.log(Logging.LogType.Normal, "StartupServer", "Start", "Server gestartet. Port: " + Config.SERVER_CONNECTION_PORT);
            Config.HAUPTMENUE_FEHLERMELDUNG = "Server wurde gestartet.";
            connectedPlayer = 0;
        }
        catch (Exception e)
        {
            Logging.log(Logging.LogType.Warning, "StartupServer", "Start", "Server kann nicht gestartet werden", e);
            Config.HAUPTMENUE_FEHLERMELDUNG = "Ein Server läuft bereits unter diesem Port.";
            Config.SERVER_STARTED = false;
            try
            {
                Config.SERVER_TCP.Server.Close();
            }
            catch (Exception e1)
            {
                Logging.log(Logging.LogType.Error, "StartupServer", "Start", "Socket kann nicht geschlossen werden.", e1);
            }
            SceneManager.LoadSceneAsync("Startup");
            return;
        }
        // Sucht ein passendes Icon für den Serverhost
        Config.SERVER_PLAYER.icon = FindFittingIconByName(Config.PLAYER_NAME);

        // Verbindung erfolgreich
        Config.HAUPTMENUE_FEHLERMELDUNG = "";
        SperreGameSelection();

        // Sendet PingUpdate alle paar sekunden
        StartCoroutine(UpdatePingOnTime());
    }
    /// <summary>
    /// Startet das empfangen von Nachrichten von Clients
    /// </summary>
    private void startListening()
    {
        Config.SERVER_TCP.BeginAcceptTcpClient(AcceptTcpClient, Config.SERVER_TCP);
    }
    /// <summary>
    /// Fügt Client der Empfangsliste hinzu (Config.PLAYLIST)
    /// </summary>
    /// <param name="ar"></param>
    private void AcceptTcpClient(IAsyncResult ar)
    {
        // Spieler sind voll
        // if (Config.SERVER_ALL_CONNECTED)
        // return;

        Logging.log(Logging.LogType.Debug, "StartupServer", "AcceptTcpClient", "Ein neuer Spieler verbindet sich...");
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
        {
            Logging.log(Logging.LogType.Warning, "StartupServer", "AcceptTcpClient", "Server ist voll. Spieler wird abgelehnt.");
            Player temp = new Player(100);
            temp.name = "full";
            TcpListener ll = (TcpListener)ar.AsyncState;
            temp.tcp = ll.EndAcceptTcpClient(ar);
            // Log ausgabe und Clientseite testen weil es nicht geht
            SendMSG("#ServerFull", temp);
            startListening();
            return;
        }

        TcpListener listener = (TcpListener)ar.AsyncState;
        freierS.isConnected = true;
        freierS.tcp = listener.EndAcceptTcpClient(ar);
        connectedPlayer++;
        Logging.log(Logging.LogType.Debug, "StartupServer", "AcceptTcpClient", "Ein neuer Spieler verbindet sich: "+ freierS.id);
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
        Logging.log(Logging.LogType.Debug, "StartupServer", "AcceptTcpClient", "Server ist voll: " + Config.SERVER_ALL_CONNECTED);
        startListening();

        // Sendet neuem Spieler zugehörige ID
        SendMSG("#SetID [ID]" + freierS.id + "[ID][GAMEFILES]" + UpdateClientGameVorschau, freierS);
        // Dazu die GameFiles
        Logging.log(Logging.LogType.Normal, "StartupServer", "AcceptTcpClient", "Spieler: " + freierS.id + " ist jetzt verbunden. IP:" + freierS.tcp.Client.RemoteEndPoint);
    }
    #endregion
    #region Kommunikation
    /// <summary>
    /// Sendet eine Nachricht an den angegebenen Spieler.
    /// </summary>
    /// <param name="data">Nachricht</param>
    /// <param name="sc">Spieler</param>
    private void SendMSG(string data, Player sc)
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
    /// Sendet eine Nachticht an alle übergebenen Spieler
    /// </summary>
    /// <param name="data">Nachricht</param>
    /// <param name="spieler">Spielerliste</param>
    private void Broadcast(string data, Player[] spieler)
    {
        foreach (Player sc in spieler)
        {
            if (sc.isConnected)
                SendMSG(data, sc);
        }
    }
    /// <summary>
    /// Sendet eine Nachticht an alle verbundenen Spieler. (Config.PLAYLIST)
    /// </summary>
    /// <param name="data">Nachricht</param>
    private void Broadcast(string data)
    {
        foreach (Player sc in Config.PLAYERLIST)
        {
            if (sc.isConnected)
                SendMSG(data, sc);
        }
    }
    /// <summary>
    /// Löscht Daten des Spielers von dem die Verbindung getrennt wurde
    /// </summary>
    /// <param name="player">Spieler</param>
    private void ClientClosed(Player player)
    {
        player.icon = Resources.Load<Sprite>("Images/ProfileIcons/empty");
        player.name = "";
        player.points = 0;
        player.crowns = 0;
        player.isConnected = false;
        player.isDisconnected = true;
    }
    /// <summary>
    /// Einkommende Nachrichten die von Spielern an den Server gesendet werden. 
    /// Extrahiert die Commands und gibt die in Commands() weiter
    /// </summary>
    /// <param name="spieler">Spieler</param>
    /// <param name="data">Daten inklusive Command</param>
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
    /// <summary>
    /// Verarbeitet die eingehenden Commands der Spieler
    /// </summary>
    /// <param name="player">Spieler</param>
    /// <param name="data">Daten</param>
    /// <param name="cmd">Command</param>
    private void Commands(Player player, string data, string cmd)
    {
        Logging.log(Logging.LogType.Debug, "StartupServer", "Commands", "Eingehende Nachricht: " + player.name + " " + player.id + " -> " + cmd + "   ---   " + data);
        // Sucht nach Command
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "StartupServer", "Commands", "Unkown Command: (" + player.id + ") " + player.name + " -> " + cmd + " - " + data);
                break;

            case "#ClientClosed":
                connectedPlayer--;
                ClientClosed(player);
                UpdateSpielerBroadcast();
                PlayDisconnectSound();
                break;
            case "#TestConnection":
                SendMSG("#ConnectionEstablished", player);
                break;
            case "#ClientFocusChange":
                break;
            case "#GetSpielerUpdate":
                UpdateSpielerBroadcast();
                break;

            case "#ClientSetName":
                ClientSetName(player, data);

                PlayConnectSound();
                break;
            case "#SpielerIconChange":
                SpielerIconChange(player, data);
                break;
            case "#ChangePlayerName":
                ChangePlayerName(player, data);
                break;
            case "#PlayerPing":
                PlayerPing(player, data);
                //UpdatePing();
                break;

            // Minigames
            case "#StartTickTackToe":
                StartTickTackToe(player);
                break;
            case "#TickTackToeSpielerZug":
                TickTackToeSpielerZug(player, data);
                break;
        }
    }
    /// <summary>
    /// Fordert alle Spieler auf die RemoteConfig neuzuladen 
    /// Lädt die Spieler des Servers neu
    /// </summary>
    public void UpdateRemoteConfig()
    {
        ServerUtils.AddBroadcast("#UpdateRemoteConfig");
        LoadConfigs.FetchRemoteConfig();
        StartCoroutine(LoadGameFilesAsync());
        UpdateGameVorschau();
    }
    /// <summary>
    /// Lädt die vorbereiteten Spieldateien
    /// </summary>
    IEnumerator LoadGameFilesAsync()
    {
        SetupSpiele.LoadGameFiles();
        yield return null;
        DisplayGameFiles();
        UpdateSpielerBroadcast();
        yield break;
    }
    /// <summary>
    /// Sperrt die Gameselection für 5 Sekunden, um Fehler bei Scenenwechseln in der Verbindung zu verhindern
    /// </summary>
    private void SperreGameSelection()
    {
        allowedStartTime = DateTime.Now.AddSeconds(2);
        for (int i = 0; i < ServerControlGameSelection.transform.childCount; i++)
        {
            ServerControlGameSelection.transform.GetChild(i).gameObject.SetActive(false);
        }
        gesperrtfuerSekundenAnzeige.SetActive(true);
    }
    /// <summary>
    /// Aktualisiert die Anzeige, wie viele Sekunden die Spielauswahl noch gesperrt ist
    /// </summary>
    private void UpdateGesperrtGameSelection()
    {
        gesperrtfuerSekundenAnzeige.GetComponent<TMP_Text>().text = "Spiele sind noch " + ((allowedStartTime.Hour - DateTime.Now.Hour)*60*60 + (allowedStartTime.Minute - DateTime.Now.Minute) * 60 + (allowedStartTime.Second - DateTime.Now.Second)) + " Sekunden gesperrt.";
    }
    /// <summary>
    /// Entsperrt die Gameselection
    /// </summary>
    private void EntsperreGameSelection()
    {
        allowedStartTime = DateTime.MinValue;
        for (int i = 0; i < ServerControlGameSelection.transform.childCount; i++)
        {
            ServerControlGameSelection.transform.GetChild(i).gameObject.SetActive(true);
        }
        gesperrtfuerSekundenAnzeige.SetActive(false);
        DisplayGameFiles();
    }
    /// <summary>
    /// Init Lobbyanzeigen
    /// </summary>
    private void InitPlayerLobby()
    {
        Logging.log(Logging.LogType.Debug, "StartupServer", "InitPlayerLobby", "Spieleranzeige wird aktualisiert.");
        // Für Server Host
        SpielerAnzeigeLobby[0].transform.GetChild(4).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/GUI/Top4");
        SpielerAnzeigeLobby[0].transform.GetChild(4).gameObject.SetActive(false);
        SpielerAnzeigeLobby[0].transform.GetChild(5).GetComponent<TMP_Text>().text = "";

        // Blendet Leere Spieler aus
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            if (Config.PLAYERLIST[i].name == "")
                SpielerAnzeigeLobby[i + 1].SetActive(false);
            else
                SpielerAnzeigeLobby[i + 1].SetActive(true);

            // Blendet Top3 Stuff aus
            SpielerAnzeigeLobby[i + 1].transform.GetChild(4).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/GUI/Top4");
            SpielerAnzeigeLobby[i + 1].transform.GetChild(4).gameObject.SetActive(false);
            SpielerAnzeigeLobby[i + 1].transform.GetChild(5).GetComponent<TMP_Text>().text = "";
        }
    }
    /// <summary>
    /// Sendet die aktualisierten Spielerinfos an alle Spieler
    /// </summary>
    private void UpdateSpielerBroadcast()
    {
        Broadcast(UpdateSpieler(), Config.PLAYERLIST);
    }
    /// <summary>
    /// Updatet die Spieler Informationsanzeigen und gibt diese als String zurück
    /// </summary>
    /// <returns>#UpdateSpieler [ID]<0-8>[ID][NAME]<>[NAME][PUNKTE]<>[PUNKTE][ICON]<>[ICON]</returns>
    private string UpdateSpieler()
    {
        string msg = "#UpdateSpieler [ID]0[ID][NAME]" + Config.PLAYER_NAME + "[NAME][PUNKTE]" + 
            Config.SERVER_PLAYER.points + "[PUNKTE][ICON]" + 
            Config.SERVER_PLAYER.icon.name + "[ICON]";
        int connectedplayer = 1;
        List<string> spielerIDNameList = new List<string>();
        spielerIDNameList.Add("");
        foreach (Player player in Config.PLAYERLIST)
        {
            msg += "[TRENNER][ID]" + player.id + "[ID][NAME]" + player.name + "[NAME][PUNKTE]" + player.points + "[PUNKTE][ICON]" + player.icon.name + "[ICON]";
            if (player.isConnected)
            {
                connectedplayer++;
                SpielerAnzeigeLobby[player.id].SetActive(true);
                SpielerAnzeigeLobby[player.id].GetComponentsInChildren<Image>()[1].sprite = player.icon;
                SpielerAnzeigeLobby[player.id].GetComponentsInChildren<TMP_Text>()[0].text = player.name;

                spielerIDNameList.Add(player.id + "| " + player.name);
            }
            else
                SpielerAnzeigeLobby[player.id].SetActive(false);
        }

        GameObject.Find("Lobby/Title_LBL/Spieleranzahl").GetComponent<TMP_Text>().text = connectedplayer + "/" + (Config.PLAYERLIST.Length + 1);
        SpielerAnzeigeLobby[0].SetActive(true);
        SpielerAnzeigeLobby[0].GetComponentsInChildren<Image>()[1].sprite = Config.SERVER_PLAYER.icon;
        SpielerAnzeigeLobby[0].GetComponentsInChildren<TMP_Text>()[0].text = Config.PLAYER_NAME;

        if (ServerControlControlField.activeInHierarchy)
        {
            GameObject.Find("ServerControl/ControlField/SpielerRauswerfen/Dropdown").GetComponent<TMP_Dropdown>().ClearOptions();
            GameObject.Find("ServerControl/ControlField/SpielerRauswerfen/Dropdown").GetComponent<TMP_Dropdown>().AddOptions(spielerIDNameList);
            GameObject.Find("ServerControl/ControlField/SpielerUmbenennen/Dropdown").GetComponent<TMP_Dropdown>().ClearOptions();
            GameObject.Find("ServerControl/ControlField/SpielerUmbenennen/Dropdown").GetComponent<TMP_Dropdown>().AddOptions(spielerIDNameList);
            GameObject.Find("ServerControl/ControlField/SpielerIconWechseln/Dropdown").GetComponent<TMP_Dropdown>().ClearOptions();
            GameObject.Find("ServerControl/ControlField/SpielerIconWechseln/Dropdown").GetComponent<TMP_Dropdown>().AddOptions(spielerIDNameList);

            GameObject.Find("ServerControl/ControlField/SpielerIconWechseln/Bilder").GetComponent<TMP_Dropdown>().ClearOptions();
            List<string> bilderliste = new List<string>();
            bilderliste.Add("empty");
            foreach (Sprite sprite in Config.PLAYER_ICONS)
                if (sprite.name != "empty")
                bilderliste.Add(sprite.name);
            GameObject.Find("ServerControl/ControlField/SpielerIconWechseln/Bilder").GetComponent<TMP_Dropdown>().AddOptions(bilderliste);
        }
        Logging.log(Logging.LogType.Debug, "StartupServer", "UpdateSpieler", msg);
        return msg;
    }
    /// <summary>
    /// Sendet ein Pingupdate an alle Spieler
    /// </summary>
    private void UpdatePing()
    {
        string msg = "#UpdatePing [0]" + SpielerAnzeigeLobby[0].transform.GetChild(3).GetComponent<Image>().sprite.name + "[0]";
        foreach (Player player in Config.PLAYERLIST)
        {
            msg += "[" + player.id + "]" + SpielerAnzeigeLobby[player.id].transform.GetChild(3).GetComponent<Image>().sprite.name + "[" + player.id + "]";
        }
        Logging.log(Logging.LogType.Debug, "StartupServer", "UpdatePing", msg);
        Broadcast(msg);
    }
    IEnumerator UpdatePingOnTime()
    {
        while (Config.SERVER_STARTED)
        {
            yield return new WaitForSeconds(5);
            if (connectedPlayer > 0)
                UpdatePing();
        }
    }
    /// <summary>
    /// Sendet ein Kronenupdate an alle Spieler
    /// </summary>
    private void UpdateCrowns()
    {
        int top1 = -1;
        int top2 = -1;
        int top3 = -1;
        #region Kronen Zahlen festlegen
        // Clients
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            Player p = Config.PLAYERLIST[i];
            if (p.crowns > top1)
            {
                top3 = top2;
                top2 = top1;
                top1 = p.crowns;
            }
            else if (p.crowns > top2)
            {
                top3 = top2;
                top2 = p.crowns;
            }
            else if (p.crowns > top3)
            {
                top3 = p.crowns;
            }

            if (top2 == top1)
            {
                top2 = top3;
                top3 = -1;
            }
            if (top3 == top2)
            {
                top3 = -1;
            }
        }
        // Server
        if (Config.SERVER_PLAYER.crowns > top1)
        {
            top3 = top2;
            top2 = top1;
            top1 = Config.SERVER_PLAYER.crowns;
        }
        else if (Config.SERVER_PLAYER.crowns > top2)
        {
            top3 = top2;
            top2 = Config.SERVER_PLAYER.crowns;
        }
        else if (Config.SERVER_PLAYER.crowns > top3)
        {
            top3 = Config.SERVER_PLAYER.crowns;
        }
        if (top2 == top1)
        {
            top2 = top3;
            top3 = -1;
        }
        if (top3 == top2)
        {
            top3 = -1;
        }
        #endregion


        // Keine Anzeigen wenn noch keiner Punkte hat
        if (top1 == 0)
        {
            SpielerAnzeigeLobby[0].transform.GetChild(5).GetComponent<TMP_Text>().text = "";
            for (int i = 0; i < Config.PLAYERLIST.Length; i++)
                SpielerAnzeigeLobby[i+1].transform.GetChild(5).GetComponent<TMP_Text>().text = "";

            for (int i = 0; i < (Config.PLAYERLIST.Length + 1); i++)
                SpielerAnzeigeLobby[i].transform.GetChild(4).gameObject.SetActive(false);
            return;
        }
        if (top2 == 0)
            top2 = -1;
        if (top3 == 0)
            top3 = -1;

        #region Anzeigen Aktualisieren
        // Clients
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            SpielerAnzeigeLobby[i+1].transform.GetChild(5).GetComponent<TMP_Text>().text = "" + Config.PLAYERLIST[i].crowns;

            if (Config.PLAYERLIST[i].crowns == top1)
                SpielerAnzeigeLobby[i + 1].transform.GetChild(4).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/GUI/Top1");
            else if (Config.PLAYERLIST[i].crowns == top2)
                SpielerAnzeigeLobby[i + 1].transform.GetChild(4).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/GUI/Top2");
            else if (Config.PLAYERLIST[i].crowns == top3)
                SpielerAnzeigeLobby[i + 1].transform.GetChild(4).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/GUI/Top3");
            else
                SpielerAnzeigeLobby[i + 1].transform.GetChild(4).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/GUI/Top4");
        }
        // Server
        SpielerAnzeigeLobby[0].transform.GetChild(5).GetComponent<TMP_Text>().text = "" + Config.SERVER_PLAYER.crowns;

        if (Config.SERVER_PLAYER.crowns == top1)
            SpielerAnzeigeLobby[0].transform.GetChild(4).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/GUI/Top1");
        else if (Config.SERVER_PLAYER.crowns == top2)
            SpielerAnzeigeLobby[0].transform.GetChild(4).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/GUI/Top2");
        else if (Config.SERVER_PLAYER.crowns == top3)
            SpielerAnzeigeLobby[0].transform.GetChild(4).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/GUI/Top3");
        else
            SpielerAnzeigeLobby[0].transform.GetChild(4).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/GUI/Top4");


        // Clients
        for (int i = 0; i < (Config.PLAYERLIST.Length + 1); i++)
        {
            if (i > 0)
                SpielerAnzeigeLobby[i].transform.GetChild(4).gameObject.SetActive(true);
            else
                SpielerAnzeigeLobby[i].transform.GetChild(4).gameObject.SetActive(false);
        }
        // Server
        if (Config.SERVER_PLAYER.crowns > 0)
            SpielerAnzeigeLobby[0].transform.GetChild(4).gameObject.SetActive(true);
        else
        {
            SpielerAnzeigeLobby[0].transform.GetChild(5).GetComponent<TMP_Text>().text = "";
            SpielerAnzeigeLobby[0].transform.GetChild(4).gameObject.SetActive(false);
        }


        #endregion

        string msg = "#UpdateCrowns [0]" + Config.SERVER_PLAYER.crowns + "[0]";
        foreach (Player player in Config.PLAYERLIST)
        {
            msg += "[" + player.id + "]" + player.crowns + "[" + player.id + "]";
        }
        Logging.log(Logging.LogType.Debug, "StartupServer", "UpdateCrowns", msg);
        Broadcast(msg);
    }
    #region Spieler Namen Ändern
    /// <summary>
    /// Prüft ob der übergebene Name bereits von einem Spieler oder dem Server belegt ist
    /// </summary>
    /// <param name="name">zu prüfender Name</param>
    /// <returns>true, false</returns>
    private bool SpielernameIstBelegt(string name)
    {
        if (Config.PLAYER_NAME == name)
            return true;
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
            if (Config.PLAYERLIST[i].name == name)
                return true;
        return false;
    }
    /// <summary>
    /// Spielt den Connect Sound ab
    /// </summary>
    private void PlayConnectSound()
    {
        ConnectSound.Play();
    }
    /// <summary>
    /// Spielt den Disconnect Sound ab
    /// </summary>
    private void PlayDisconnectSound()
    {
        DisconnectSound.Play();
    }
    /// <summary>
    /// Speichert den Namen, den sich der neu verbundene Spieler geben will.
    /// Zudem wird die Gameversion verglichen.
    /// </summary>
    /// <param name="player">Spieler</param>
    /// <param name="data">[NAME]<name>[NAME][VERSION]<gameversion>[VERSION]</param>
    private void ClientSetName(Player player, String data)
    {
        string version = data.Replace("[VERSION]", "|").Split('|')[1];
        // Spieler hat eine falsche Version
        if (version != Config.APPLICATION_VERSION)
        {
            Logging.log(Logging.LogType.Warning, "StartupServer", "ClientSetName", "Spieler ID: " + player.id + " versucht mit einer falschen Version beizutreten.Spieler Version: " + version + " | Server Version: " + Config.APPLICATION_VERSION);
            SendMSG("#WrongVersion " + Application.version, player);
            ClientClosed(player);
            return;
        }
        // Legt Spielernamen fest
        string name = data.Replace("[NAME]", "|").Split('|')[1];
        if (name.Length > Config.MAX_PLAYER_NAME_LENGTH)
            name = name.Substring(0, Config.MAX_PLAYER_NAME_LENGTH);
        if (!SpielernameIstBelegt(name))
        {
            Logging.log(Logging.LogType.Normal, "StartupServer", "ClientSetName", "Spieler " + player.name + " heißt jetzt " + name);
            player.name = name;
            SendMSG("#SpielerChangeName " + name, player);
            UpdateSpielerBroadcast();
            return;
        }
        for (int i = 0; i < 10; i++)
        {
            if (i == 0)
                name = name + i;
            if (SpielernameIstBelegt(name))
                name = name.Substring(0, name.Length - 1) + i;
            else
                break;
        }
        Logging.log(Logging.LogType.Normal, "StartupServer", "ClientSetName", "Spieler " + player.name + "heißt jetzt " + name);
        player.name = name;
        SendMSG("#SpielerChangeName " + name, player);
        // Sendet Update an alle Spieler & Updatet Spieler Anzeigen
        UpdateSpielerBroadcast();
    }
    /// <summary>
    /// Erlaubt/Verbietet Namenswechsel von Spielern
    /// </summary>
    /// <param name="toggle">Toggle</param>
    public void SpielerUmbenennenToggle(Toggle toggle)
    {
        Logging.log(Logging.LogType.Normal, "StartupServer", "SpielerUmbenennenToggle", "Spieler dürfen sich umbenennen: "+ toggle.isOn);
        Config.ALLOW_PLAYERNAME_CHANGE = toggle.isOn;
        ServerUtils.AddBroadcast("#AllowNameChange " + toggle.isOn);
    }
    /// <summary>
    /// Spieler ändert Namen
    /// </summary>
    /// <param name="p">Spieler</param>
    /// <param name="data">Neuer Name</param>
    private void ChangePlayerName(Player p, string data)
    {
        if (!Config.ALLOW_PLAYERNAME_CHANGE)
            return;
        // Legt Spielernamen fest
        string name = data;
        string vorher = name;
        if (name.Length > Config.MAX_PLAYER_NAME_LENGTH)
            name = name.Substring(0, Config.MAX_PLAYER_NAME_LENGTH);
        if (!SpielernameIstBelegt(name))
        {
            Logging.log(Logging.LogType.Normal, "StartupServer", "ChangePlayerName", "Spieler " + p.name + "heißt jetzt " + name);
            p.name = name;
            UpdateSpielerBroadcast();
            return;
        }
        for (int i = 0; i < 10; i++)
        {
            if (i == 0)
                name = name + i;
            if (SpielernameIstBelegt(name))
                name = name.Substring(0, name.Length - 1) + i;
            else
                break;
        }
        if (vorher == name)
            return;
        Logging.log(Logging.LogType.Normal, "StartupServer", "ChangePlayerName", "Spieler " + p.name + "heißt jetzt " + name);
        p.name = name;
        UpdateSpielerBroadcast();
    }
    /// <summary>
    /// Server benennt einen Spieler um
    /// </summary>
    /// <param name="input">Neuer Name des Spielers</param>
    public void SpielerUmbenennenNamen(TMP_InputField input)
    {
        TMP_Dropdown drop = GameObject.Find("ServerControl/ControlField/SpielerUmbenennen/Dropdown").GetComponent<TMP_Dropdown>();
        // Server
        if (drop.options[drop.value].text == "")
        {
            if (input.text == Config.PLAYER_NAME)
                return;
            // Legt Spielernamen fest
            string name = input.text;
            string vorher = name;
            if (name.Length > Config.MAX_PLAYER_NAME_LENGTH)
                name = name.Substring(0, Config.MAX_PLAYER_NAME_LENGTH);
            if (!SpielernameIstBelegt(name))
            {
                Logging.log(Logging.LogType.Normal, "StartupServer", "SpielerUmbenennenNamen", "Spieler " + Config.PLAYER_NAME + "heißt jetzt " + name);
                Config.PLAYER_NAME = name;
                UpdateSpielerBroadcast();
                return;
            }
            for (int i = 0; i < 10; i++)
            {
                if (i == 0)
                    name = name + i;
                if (SpielernameIstBelegt(name))
                    name = name.Substring(0, name.Length - 1) + i;
                else
                    break;
            }
            if (name == vorher)
                return;
            Logging.log(Logging.LogType.Normal, "StartupServer", "SpielerUmbenennenNamen", "Spieler " + Config.PLAYER_NAME + "heißt jetzt " + name);
            Config.PLAYER_NAME = name;
            UpdateSpielerBroadcast();
        }
        // Spieler
        else
        {
            int id = Int32.Parse(drop.options[drop.value].text.Split('|')[0]);
            if (input.text == Config.PLAYERLIST[id - 1].name)
                return;
            // Legt Spielernamen fest
            string name = input.text;
            string vorher = name;
            if (name.Length > Config.MAX_PLAYER_NAME_LENGTH)
                name = name.Substring(0, Config.MAX_PLAYER_NAME_LENGTH);
            if (!SpielernameIstBelegt(name))
            {
                Logging.log(Logging.LogType.Normal, "StartupServer", "SpielerUmbenennenNamen", "Spieler " + Config.PLAYERLIST[id - 1].name + "heißt jetzt " + name);
                Config.PLAYERLIST[id - 1].name = name;
                UpdateSpielerBroadcast();
                return;
            }
            for (int i = 0; i < 10; i++)
            {
                if (i == 0)
                    name = name + i;
                if (SpielernameIstBelegt(name))
                    name = name.Substring(0, name.Length - 1) + i;
                else
                    break;
            }
            if (name == vorher)
                return;

            Logging.log(Logging.LogType.Normal, "StartupServer", "SpielerUmbenennenNamen", "Spieler " + Config.PLAYERLIST[id - 1].name + "heißt jetzt " + name);
            Config.PLAYERLIST[id - 1].name = name;
            UpdateSpielerBroadcast();
        }
    }
    #endregion
    /// <summary>
    /// Server wirft den angegebenen Spieler raus
    /// </summary>
    /// <param name="dropdown">Spielerauswahl</param>
    public void SpielerRauswerfen(TMP_Dropdown dropdown)
    {
        if (dropdown.options[dropdown.value].text == "")
            return;
        int playerid = Int32.Parse(dropdown.options[dropdown.value].text.Split('|')[0]);
        
        SendMSG("#ServerClosed", Config.PLAYERLIST[playerid - 1]);
        Logging.log(Logging.LogType.Normal, "StartupServer", "SpielerRauswerfen", "Spieler " + Config.PLAYERLIST[playerid - 1].name + " wird gekickt.");
        ClientClosed(Config.PLAYERLIST[playerid - 1]);
        UpdateSpielerBroadcast();
    }
    #region Spieler Icon Ändern
    /// <summary>
    /// Erlaubt Spielern das Icon zu wechseln
    /// </summary>
    /// <param name="toggle">Toggle</param>
    public void SpielerIconToggle(Toggle toggle)
    {
        Logging.log(Logging.LogType.Normal, "StartupServer", "SpielerIconToggle", "Spieler dürfen ihr Icon wechseln: "+ toggle.isOn);
        Config.ALLOW_ICON_CHANGE = toggle.isOn;
    }
    /// <summary>
    /// Wechselt das Icon eines Spielers vom Server aus
    /// </summary>
    /// <param name="Icon">ID des Icons</param>
    public void SpielerIconWechsel(TMP_Dropdown Icon)
    {
        TMP_Dropdown drop = GameObject.Find("ServerControl/ControlField/SpielerIconWechseln/Dropdown").GetComponent<TMP_Dropdown>();
        if (drop.options[drop.value].text == "")
        {
            Config.SERVER_PLAYER.icon = Resources.Load<Sprite>("Images/ProfileIcons/" + Icon.options[Icon.value].text);
            Logging.log(Logging.LogType.Normal, "StartupServer", "SpielerIconWechsel", "Server hat nun das Icon: "+ Config.SERVER_PLAYER.icon.name);
            UpdateSpielerBroadcast();
            return;
        }
        int id = Int32.Parse(drop.options[drop.value].text.Split('|')[0]);
        Config.PLAYERLIST[id - 1].icon = Resources.Load<Sprite>("Images/ProfileIcons/" + Icon.options[Icon.value].text);
        Logging.log(Logging.LogType.Normal, "StartupServer", "SpielerIconWechsel", "Spieler " + Config.PLAYERLIST[id - 1].name + " hat nun das Icon: " + Config.PLAYERLIST[id - 1].icon.name);
        UpdateSpielerBroadcast();
    }
    /// <summary>
    /// Icon Wechsel eines Spielers auf Anfrage des Spielers
    /// </summary>
    /// <param name="p">Spieler</param>
    /// <param name="data">Name des geforderten Icons</param>
    private void SpielerIconChange(Player p, string data)
    {
        Sprite neuesIcon;
        if (data.Equals("0")) // Initial Änderung Icon
        {
            Logging.log(Logging.LogType.Normal, "StartupServer", "SpielerIconChange", "Spieler " + p.name + " bekommt sein initial Icon.");
            neuesIcon = FindFittingIconByName(p.name);
            if (neuesIcon == null)
                return;
            IconFestlegen(p, neuesIcon);
            return;
        }
        // Spieler gewollte änderung des Icons
        if (!Config.ALLOW_ICON_CHANGE)
            return;

        neuesIcon = Config.PLAYER_ICONS[(Config.PLAYER_ICONS.IndexOf(p.icon) + 1) % Config.PLAYER_ICONS.Count];
        Logging.log(Logging.LogType.Normal, "StartupServer", "SpielerIconChange", "Spieler " + p.name + " hat nun das Icon: "+ neuesIcon.name);
        IconFestlegen(p, neuesIcon);
    }
    /// <summary>
    /// Sucht passende SpielerIcons nach einem Namen
    /// </summary>
    /// <param name="name"></param>
    /// <returns>null wenn kein Name passt</returns>
    private Sprite FindFittingIconByName(string name)
    {
        if (name.ToLower().Contains("spieler"))
        {
            return FindIconByName("Discord");
        }
        else if (name.ToLower().Contains("alan"))
        {
            return FindIconByName("Alan");
        }
        else if (name.ToLower().Contains("fiona"))
        {
            return FindIconByName("Fiona");
        }
        else if (name.ToLower().Contains("hannah")
            || (name.ToLower().StartsWith("ha") && name.ToLower().EndsWith("nah"))
            || (name.ToLower().StartsWith("han") && name.ToLower().EndsWith("ah"))
            || (name.ToLower().StartsWith("haa") && name.ToLower().EndsWith("aah") && name.ToLower().Substring(3, name.Length - 3).Contains("nn")))
        {
            return FindIconByName("Hannah");
        }
        else if (name.ToLower().Contains("henryk") || name.ToLower().Contains("play1live"))
        {
            return FindIconByName("Henryk");
        }
        else if (name.ToLower().Contains("maxe"))
        {
            return FindIconByName("Maxe");
        }
        else if (name.ToLower().Contains("michi") ||
            name.ToLower().Contains("michelle"))
        {
            return  FindIconByName("Michi");
        }
        else if (name.ToLower().Contains("munck"))
        {
            return FindIconByName("Munk");
        }
        else if (name.ToLower().Contains("nils")
            || name.ToLower().Contains("nille")
            || name.ToLower().Contains("kater")
            || name.ToLower().Contains("katerjunge"))
        {
            return FindIconByName ("Nils");
        }
        else if (name.ToLower().Contains("ronald")
            || name.ToLower().Contains("ron")
            || name.ToLower().Contains("sterni")
            || name.ToLower().Contains("sternfaust"))
        {
            return FindIconByName("Ronald");
        }
        else
        {
            Logging.log(Logging.LogType.Warning, "StartupServer", "SpielerIconChange", "Spielername für Icons ist unbekannt: " + name);
            Sprite neuesIcon = Config.PLAYER_ICONS[1];
            for (int i = 1; i < Config.PLAYER_ICONS.Count; i++)
            {
                if (IconWirdGeradeGenutzt(neuesIcon))
                    neuesIcon = Config.PLAYER_ICONS[(Config.PLAYER_ICONS.IndexOf(neuesIcon) + 1) % Config.PLAYER_ICONS.Count];
                else
                    break;
            }
            return neuesIcon;
        }
    }
    /// <summary>
    /// Gibt ein Sprite eines Icons zurück das per Name gesucht wird
    /// </summary>
    /// <param name="name">Name des neuen Icons</param>
    /// <returns>Sprite, null</returns>
    private Sprite FindIconByName(string name)
    {
        foreach (Sprite sprite in Config.PLAYER_ICONS)
        {
            if (sprite.name.Equals(name))
                return sprite;
        }
        Logging.log(Logging.LogType.Warning, "StartupServer", "FindIconByName", "Icon " + name + " konnte nicht gefunden werden.");
        return null;
    }
    /// <summary>
    /// Spieler wird ein neues Icon zugewiesen
    /// </summary>
    /// <param name="p">Spieler</param>
    /// <param name="neuesIcon">Neues Icon für den Spieler</param>
    private void IconFestlegen(Player p, Sprite neuesIcon)
    {
        if (neuesIcon == null)
            neuesIcon = Config.PLAYER_ICONS[(Config.PLAYER_ICONS.IndexOf(neuesIcon) + 1) % Config.PLAYER_ICONS.Count];

        for (int i = 1; i < Config.PLAYER_ICONS.Count; i++)
        {
            if (IconWirdGeradeGenutzt(neuesIcon))
                neuesIcon = Config.PLAYER_ICONS[(Config.PLAYER_ICONS.IndexOf(neuesIcon) + 1) % Config.PLAYER_ICONS.Count];
            else
                break;
        }
        // Lege Icon fest
        p.icon = neuesIcon;
        // Update an alle
        UpdateSpielerBroadcast();
    }
    /// <summary>
    /// Prüft ob das übergebene Icon bereits vom Server oder einem anderen Spieler benutzt wird
    /// </summary>
    /// <param name="icon">Icon</param>
    /// <returns>true, false</returns>
    private bool IconWirdGeradeGenutzt(Sprite icon)
    {
        if (Config.SERVER_PLAYER.icon == icon)
            return true;
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
            if (icon == Config.PLAYERLIST[i].icon || icon.name == "empty")
                return true;
        return false;
    }
    /// <summary>
    /// Ändert das Icon des Servers
    /// </summary>
    public void ServerIconChange()
    {
        if (!Config.isServer)
            return;
        Config.SERVER_PLAYER.icon = Config.PLAYER_ICONS[(Config.PLAYER_ICONS.IndexOf(Config.SERVER_PLAYER.icon) + 1) % Config.PLAYER_ICONS.Count];
        Logging.log(Logging.LogType.Normal, "StartupServer", "ServerIconChange", "Server hat nun das Icon: " + Config.SERVER_PLAYER.icon.name);
        UpdateSpielerBroadcast();
    }
    #endregion
    /// <summary>
    /// Pinganzeige Update eines Spieler
    /// </summary>
    /// <param name="p">Spieler</param>
    /// <param name="data">Pingdaten</param>
    public void PlayerPing(Player p, string data)
    {
        Logging.log(Logging.LogType.Normal, "StartupServer", "PlayerPing", "PlayerPing: " + p.name + " -> " + data);
        int ping = Int32.Parse(data);
        if (ping <= 10)
        {
            SpielerAnzeigeLobby[p.id].transform.GetChild(3).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/Ping/Ping 3");
        }
        else if (ping > 10 && ping <= 30)
        {
            SpielerAnzeigeLobby[p.id].transform.GetChild(3).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/Ping/Ping 2");
        }
        else if (ping > 30 && ping <= 100)
        {
            SpielerAnzeigeLobby[p.id].transform.GetChild(3).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/Ping/Ping 1");
        }
        else if (ping > 100)
        {
            SpielerAnzeigeLobby[p.id].transform.GetChild(3).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/Ping/Ping 0");
        }
    }
    /// <summary>
    /// Aktualisiert den GameVorschau String
    /// </summary>
    private void UpdateGameVorschau()
    {
        List<string> gamelist = new List<string>();
        // Moderierte Games
        gamelist.Add("[SPIELER-ANZ]0[SPIELER-ANZ][MIN]0[MIN][MAX]" + Config.SERVER_MAX_CONNECTIONS + "[MAX][TITEL]<b><i>Moderierte Spiele</i></b>[TITEL][AVAILABLE]-1[AVAILABLE]");
        // Flaggen
        gamelist.Add("[SPIELER-ANZ]" + FlaggenSpiel.minPlayer + "-" + FlaggenSpiel.maxPlayer + "[SPIELER-ANZ][MIN]" + FlaggenSpiel.minPlayer + "[MIN][MAX]" + FlaggenSpiel.maxPlayer + "[MAX][TITEL]Flaggen[TITEL][AVAILABLE]" + Config.FLAGGEN_SPIEL.getFlaggen().Count + "[AVAILABLE]");
        // Quiz
        gamelist.Add("[SPIELER-ANZ]" + QuizSpiel.minPlayer + "-" + QuizSpiel.maxPlayer + "[SPIELER-ANZ][MIN]" + QuizSpiel.minPlayer + "[MIN][MAX]" + QuizSpiel.maxPlayer + "[MAX][TITEL]Quiz[TITEL][AVAILABLE]" + Config.QUIZ_SPIEL.getQuizze().Count + "[AVAILABLE]");
        // Listen
        gamelist.Add("[SPIELER-ANZ]" + ListenSpiel.minPlayer + "-" + ListenSpiel.maxPlayer + "[SPIELER-ANZ][MIN]" + ListenSpiel.minPlayer + "[MIN][MAX]" + ListenSpiel.maxPlayer + "[MAX][TITEL]Listen[TITEL][AVAILABLE]" + Config.LISTEN_SPIEL.getListen().Count + "[AVAILABLE]");
        // Mosaik
        gamelist.Add("[SPIELER-ANZ]" + MosaikSpiel.minPlayer + "-" + MosaikSpiel.maxPlayer + "[SPIELER-ANZ][MIN]" + MosaikSpiel.minPlayer + "[MIN][MAX]" + MosaikSpiel.maxPlayer + "[MAX][TITEL]Mosaik[TITEL][AVAILABLE]" + Config.MOSAIK_SPIEL.getMosaike().Count + "[AVAILABLE]");
        // WerBietetMehr
        gamelist.Add("[SPIELER-ANZ]" + WerBietetMehrSpiel.minPlayer + "-" + WerBietetMehrSpiel.maxPlayer + "[SPIELER-ANZ][MIN]" + WerBietetMehrSpiel.minPlayer + "[MIN][MAX]" + WerBietetMehrSpiel.maxPlayer + "[MAX][TITEL]WerBietetMehr[TITEL][AVAILABLE]" + Config.WERBIETETMEHR_SPIEL.getSpiele().Count + "[AVAILABLE]");
        // Geheimwörter
        gamelist.Add("[SPIELER-ANZ]" + GeheimwörterSpiel.minPlayer + "-" + GeheimwörterSpiel.maxPlayer + "[SPIELER-ANZ][MIN]" + GeheimwörterSpiel.minPlayer + "[MIN][MAX]" + GeheimwörterSpiel.maxPlayer + "[MAX][TITEL]Geheimwörter[TITEL][AVAILABLE]" + Config.GEHEIMWOERTER_SPIEL.getListen().Count + "[AVAILABLE]");
        // Auktion
        gamelist.Add("[SPIELER-ANZ]" + AuktionSpiel.minPlayer + "-" + AuktionSpiel.maxPlayer + "[SPIELER-ANZ][MIN]" + AuktionSpiel.minPlayer + "[MIN][MAX]" + AuktionSpiel.maxPlayer + "[MAX][TITEL]Auktion[TITEL][AVAILABLE]" + Config.AUKTION_SPIEL.getAuktionen().Count + "[AVAILABLE]");
        // Sloxikon
        gamelist.Add("[SPIELER-ANZ]" + SloxikonSpiel.minPlayer + "-" + SloxikonSpiel.maxPlayer + "[SPIELER-ANZ][MIN]" + SloxikonSpiel.minPlayer + "[MIN][MAX]" + SloxikonSpiel.maxPlayer + "[MAX][TITEL]Sloxikon[TITEL][AVAILABLE]" + Config.SLOXIKON_SPIEL.getGames().Count + "[AVAILABLE]");
        // Unmoderierte Games
        gamelist.Add("[SPIELER-ANZ]0[SPIELER-ANZ][MIN]0[MIN][MAX]" + Config.SERVER_MAX_CONNECTIONS + "[MAX][TITEL]<b><i>Unmoderierte Spiele</i></b>[TITEL][AVAILABLE]-1[AVAILABLE]");
        // MenschÄrgerDichNicht
        gamelist.Add("[SPIELER-ANZ]" + MenschAegerDichNichtBoard.minPlayer + "-" + MenschAegerDichNichtBoard.maxPlayer + "[SPIELER-ANZ][MIN]" + MenschAegerDichNichtBoard.minPlayer + "[MIN][MAX]" + MenschAegerDichNichtBoard.maxPlayer + "[MAX][TITEL]MenschÄrgerDichNicht[TITEL][AVAILABLE]-1[AVAILABLE]");
        // Kniffel
        gamelist.Add("[SPIELER-ANZ]" + KniffelBoard.minPlayer + "-" + KniffelBoard.maxPlayer + "[SPIELER-ANZ][MIN]" + KniffelBoard.minPlayer + "[MIN][MAX]" + KniffelBoard.maxPlayer + "[MAX][TITEL]Kniffel[TITEL][AVAILABLE]-1[AVAILABLE]");
        // Tabu
        gamelist.Add("[SPIELER-ANZ]" + TabuBoard.minPlayer + "-" + TabuBoard.maxPlayer + "[SPIELER-ANZ][MIN]" + TabuBoard.minPlayer + "[MIN][MAX]" + TabuBoard.maxPlayer + "[MAX][TITEL]Tabu[TITEL][AVAILABLE]-1[AVAILABLE]");


        string msg = "";
        for (int i = 0; i < gamelist.Count; i++)
        {
            msg += "[" + i + "]" + gamelist[i] + "[" + i + "]";
        }
        UpdateClientGameVorschau = "[ANZ]" + gamelist.Count + "[ANZ]" + msg;
        Logging.log(Logging.LogType.Normal, "StartupServer", "UpdateGameVorschau", "Gamevorschau: " + UpdateClientGameVorschau);
    }
    #region MiniSpielauswahl
    /// <summary>
    /// Wechselt das angezeigt Minigame in der Lobby für die Spieler
    /// </summary>
    /// <param name="drop">Minispiel Auswahl</param>
    public void SwitchMiniGame(TMP_Dropdown drop)
    {
        Logging.log(Logging.LogType.Normal, "StartupServer", "SwitchMiniGame", "MiniGame wird gewechselt: " + drop.options[drop.value]);
        foreach (GameObject go in SpielerMiniGames)
            go.SetActive(false);

        switch(drop.options[drop.value]+"")
        {
            default:
                SwitchToTickTackToe();
                break;
            case "TickTackToe":
                SwitchToTickTackToe();
                break;
        }
    }
    #region TickTackToe
    /// <summary>
    /// Zeigt TickTackToe für alle Spieler an
    /// </summary>
    private void SwitchToTickTackToe()
    {
        SpielerMiniGames[0].SetActive(true);
        ServerUtils.AddBroadcast("#SwitchToTickTackToe");
    }
    /// <summary>
    /// Bestimmt den ersten Zug gegen einen Spieler
    /// </summary>
    /// <param name="player">Spieler</param>
    private void StartTickTackToe(Player player)
    {
        Logging.log(Logging.LogType.Debug, "StartupServer", "StartTickTackToe", "Spieler " + player.name + " startet TickTackToe.");
        string msg = "";
        int beginner = UnityEngine.Random.Range(0, 2);
        int initzug = UnityEngine.Random.Range(1,10);
        if (beginner == 0) // Spieler beginnt
            initzug = 0;
        for (int i = 1; i <= 9; i++)
        {
            if (i == initzug)
                msg = msg + "[" + i + "]X[" + i + "]";
            else
                msg = msg + "[" + i + "][" + i + "]";
        }
        SendMSG("#TickTackToeZug " + msg, player);
    }
    /// <summary>
    /// Lässt den Server einen Zug machen & prüft ob das Spiel beendet ist
    /// </summary>
    /// <param name="player">Spieler</param>
    /// <param name="data">TickTackToe Daten</param>
    private void TickTackToeSpielerZug(Player player, string data)
    {
        // Freie Felder berechnen
        List<int> freieFelder = TickTackToe.GetFreieFelder(data);
        List<string> belegteFelder = TickTackToe.GetBelegteFelder(data);
        // CheckForWin
        if (TickTackToe.CheckForEnd(freieFelder, belegteFelder))
        {
            SendMSG("#TickTackToeZugEnde |" + TickTackToe.getResult(belegteFelder) + "| " + data, player);
            return;
        }
        // Ziehen
        belegteFelder = TickTackToe.ServerZiehen(freieFelder, belegteFelder);
        freieFelder = TickTackToe.GetFreieFelder(belegteFelder);
        //Check for End
        if (TickTackToe.CheckForEnd(freieFelder, belegteFelder))
            SendMSG("#TickTackToeZugEnde |" + TickTackToe.getResult(belegteFelder) + "|" + TickTackToe.PrintBelegteFelder(belegteFelder), player);
        else
            SendMSG("#TickTackToeZug " + TickTackToe.PrintBelegteFelder(belegteFelder), player);
    }
    #endregion
    #endregion
    /// <summary>
    /// Wechsel zwischen Spielauswahl & Kontrollfelder des Servers
    /// </summary>
    /// <param name="s">Spielauswahl oder Kontrollfelder</param>
    public void WechselGameSelControlFie(string s)
    {
        if (s == "Spielauswahl")
        {
            ServerControlGameSelection.SetActive(true);
            ServerControlControlField.SetActive(false);
            DisplayGameFiles();
        }
        else if (s == "Kontrollfelder")
        {
            ServerControlControlField.SetActive(true);
            ServerControlGameSelection.SetActive(false);

            UpdateSpieler();
        }
        else
        {
            Logging.log(Logging.LogType.Warning, "StartupServer", "WechselGameSelectionFie", "Unbekannte Auswahl: WechselGameSelControlFie -> " + s);
        }
    }
    #region Crowns
    /// <summary>
    /// Ändert die Anzahl der Kronen die ein Spieler besitzt
    /// </summary>
    public void CrownsAdd(GameObject button)
    {
        int pos = Int32.Parse(button.transform.parent.parent.name.Replace("Player (", "").Replace(")", "")) - 1;
        Logging.log(Logging.LogType.Debug, "StartupServer", "CrownsAdd", "Spieler ID: " + (pos+1) + " erhält einen Kronen Punkt.");
        // Server
        if (pos == -1)
        {
            if (button.name.Equals("+1"))
                Config.SERVER_PLAYER.crowns++;
            else if (button.name.Equals("-1"))
                Config.SERVER_PLAYER.crowns--;

            if (Config.SERVER_PLAYER.crowns < 0)
                Config.SERVER_PLAYER.crowns = 0;
        }
        // Clients
        else
        {
            if (button.name.Equals("+1"))
                Config.PLAYERLIST[pos].crowns++;
            else if (button.name.Equals("-1"))
                Config.PLAYERLIST[pos].crowns--;

            if (Config.PLAYERLIST[pos].crowns < 0)
                Config.PLAYERLIST[pos].crowns = 0;
        }
        UpdateCrowns();
    }
    /// <summary>
    /// Ändert die Anzahl der Kronen die ein Spieler besitzt
    /// </summary>
    /// <param name="input">Eingegebene Anzahl der Kronen die hinzugefügt werden</param>
    public void CrownsAddX(TMP_InputField input)
    {
        int punkte = 0;
        try
        {
            punkte = int.Parse(input.text);
            input.text = "";
        }
        catch (Exception e)
        {
            Logging.log(Logging.LogType.Warning, "StartupServer", "CrownsAddX", "Eingabe konnte nicht verarbeitet werden.", e);
            return;
        }
        int pos = Int32.Parse(input.transform.parent.parent.name.Replace("Player (", "").Replace(")", "")) - 1;
        Logging.log(Logging.LogType.Debug, "StartupServer", "CrownsAddX", "Spieler ID: " + (pos+1) + " erhält " + punkte + " Kronen Punkt.");
        // Server
        if (pos == -1)
        {
            Config.SERVER_PLAYER.crowns += punkte;
        }
        // Clients
        else
        {
            Config.PLAYERLIST[pos].crowns += punkte;
        }

        UpdateCrowns();
    }
    #endregion
    /// <summary>
    /// Zeigt die geladenen Spiele in der GameÜbersicht an
    /// </summary>
    private void DisplayGameFiles()
    {
        if (!ServerControlGameSelection.activeInHierarchy || !ServerControl.activeInHierarchy || gesperrtfuerSekundenAnzeige.activeInHierarchy)
            return;
        Logging.log(Logging.LogType.Debug, "StartupServer", "DisplayGameFiles", "Verfügbare Spiele werden angezeigt.");

        TMP_Dropdown QuizDropdown = GameObject.Find("Quiz/Quiz").GetComponent<TMP_Dropdown>();
        QuizDropdown.ClearOptions();
        QuizDropdown.AddOptions(Config.QUIZ_SPIEL.getGamesAsStringList());

        TMP_Dropdown ListenDropdown = GameObject.Find("Listen/Listen").GetComponent<TMP_Dropdown>();
        ListenDropdown.ClearOptions();
        ListenDropdown.AddOptions(Config.LISTEN_SPIEL.getGamesAsStringList());

        TMP_Dropdown MosaikDropdown = GameObject.Find("Mosaik/Mosaik").GetComponent<TMP_Dropdown>();
        MosaikDropdown.ClearOptions();
        MosaikDropdown.AddOptions(Config.MOSAIK_SPIEL.getGamesAsStringList());

        TMP_Dropdown GeheimwoerterDropdown = GameObject.Find("Geheimwörter/Geheimwörter").GetComponent<TMP_Dropdown>();
        GeheimwoerterDropdown.ClearOptions();
        GeheimwoerterDropdown.AddOptions(Config.GEHEIMWOERTER_SPIEL.getGamesAsStringList());

        TMP_Dropdown WerBietetMehrDropdown = GameObject.Find("WerBietetMehr/WerBietetMehr").GetComponent<TMP_Dropdown>();
        WerBietetMehrDropdown.ClearOptions();
        WerBietetMehrDropdown.AddOptions(Config.WERBIETETMEHR_SPIEL.getGamesAsList());

        TMP_Dropdown AuktionDropdown = GameObject.Find("Auktion/Auktion").GetComponent<TMP_Dropdown>();
        AuktionDropdown.ClearOptions();
        AuktionDropdown.AddOptions(Config.AUKTION_SPIEL.getGamesAsStringList());

        TMP_Dropdown SloxikonDropdown = GameObject.Find("Sloxikon/Sloxikon").GetComponent<TMP_Dropdown>();
        SloxikonDropdown.ClearOptions();
        SloxikonDropdown.AddOptions(Config.SLOXIKON_SPIEL.getGamesAsStringList());
    }
    #region Starte Spiele
    /// <summary>
    /// Startet das Spiel mit dem angegebenen Titel, falls dieses nicht existiert, wird der Server beendet
    /// </summary>
    /// <param name="spieltitel"></param>
    public void StarteSpiel(string spieltitel)
    {
        Logging.log(Logging.LogType.Normal, "StartupServer", "StarteSpiel", "Spiel wird geladen: " + spieltitel);
        Config.GAME_TITLE = spieltitel;

        if (spieltitel == "Quiz" && Config.QUIZ_SPIEL.getSelected() == null)
            return;
        else if (spieltitel == "Listen" && Config.LISTEN_SPIEL.getSelected() == null)
            return;
        else if (spieltitel == "Mosaik" && Config.MOSAIK_SPIEL.getSelected() == null)
            return;
        else if (spieltitel == "Geheimwörter" && Config.GEHEIMWOERTER_SPIEL.getSelected() == null)
            return;
        else if (spieltitel == "WerBietetMehr" && Config.WERBIETETMEHR_SPIEL.getSelected() == null)
            return;
        else if (spieltitel == "Auktion" && Config.AUKTION_SPIEL.getSelected() == null)
            return;
        else if (spieltitel == "Sloxikon" && Config.SLOXIKON_SPIEL.getSelected() == null)
            return;

        // Lädt Spiel & Senden an Clients, falls das Spiel nicht existiert, wird der Server geschlossen
        try
        {
            SceneManager.LoadScene(spieltitel);
            ServerUtils.AddBroadcast("#StarteSpiel " + spieltitel);
        }
        catch
        {
            Logging.log(Logging.LogType.Error, "StartupServer", "StarteSpiel", "Unbekanntes Spiel soll geladen werden. Server wird geschlossen. Spiel: " + spieltitel);
            ServerUtils.AddBroadcast("#ServerClosed");
            Logging.log(Logging.LogType.Normal, "StartupServer", "StarteSpiel", "Server wird geschlossen.");
            Config.SERVER_TCP.Stop();
            SceneManager.LoadSceneAsync("Startup");
            return;
        }
    }
    /// <summary>
    /// Wählt eine Spieldatei für das gewählte Spiel aus
    /// </summary>
    /// <param name="drop"></param>
    public void SelectGameStat(TMP_Dropdown drop)
    {
        switch (drop.name)
        {
            case "Quiz":
                Config.QUIZ_SPIEL.setSelected(Config.QUIZ_SPIEL.getQuizByIndex(drop.value));
                break;
            case "Listen":
                Config.LISTEN_SPIEL.setSelected(Config.LISTEN_SPIEL.getListe(drop.value));
                break;
            case "Mosaik":
                Config.MOSAIK_SPIEL.setSelected(Config.MOSAIK_SPIEL.getMosaik(drop.value));
                break;
            case "Geheimwörter":
                Config.GEHEIMWOERTER_SPIEL.setSelected(Config.GEHEIMWOERTER_SPIEL.getListe(drop.value));
                break;
            case "WerBietetMehr":
                Config.WERBIETETMEHR_SPIEL.setSelected(Config.WERBIETETMEHR_SPIEL.getQuizByIndex(drop.value));
                break;
            case "Auktion":
                Config.AUKTION_SPIEL.setSelected(Config.AUKTION_SPIEL.getAuktion(drop.value));
                break;
            case "Sloxikon":
                Config.SLOXIKON_SPIEL.setSelected(Config.SLOXIKON_SPIEL.getQuizByIndex(drop.value));
                break;
        }
    }
    #endregion

}