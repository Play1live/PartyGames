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

    // Start is called before the first frame update
    void Start()
    {
        #region Startet Server
        try
        {
            Config.SERVER_TCP = new TcpListener(IPAddress.Any, Config.SERVER_CONNECTION_PORT); // TODO:
            Config.SERVER_TCP.Start();
            startListening();
            Config.SERVER_STARTED = true;
            Logging.add(new Logging(Logging.Type.Normal, "Server", "Start", "Server gestartet. Port: " + Config.SERVER_CONNECTION_PORT));
            GameObject.Find("ConnectingToServer_LBL").gameObject.GetComponent<TMP_Text>().text = "Server wurde gestartet.";
        }
        catch (Exception e)
        {
            Logging.add(new Logging(Logging.Type.Fatal, "Server", "Start", "Server kann nicht gestartet werden", e));
            Config.HAUPTMENUE_FEHLERMELDUNG = "Server kann nicht gestartet werden.";
            try
            {
                Config.SERVER_TCP.Server.Close();
            }
            catch (Exception e1)
            {
                Logging.add(new Logging(Logging.Type.Fatal, "Server", "Start", "Socket kann nicht geschlossen werden.", e1));
            }
            //Logging.add(new Logging(Logging.Type.Normal, "Server", "Start", "Client wird in das Hauptmenü geladen."));
            return;
        }
        #endregion

        // Potenziell Fehleranfällig, wenn Dateien nicht korrekt sind
        try
        {
            SetupSpiele.LoadGameFiles();
        } catch (Exception e)
        {
            Debug.LogError("Es kam zu einem Fehler beim Laden der Spieldateien!\n"+e);
        }

        Hauptmenue.SetActive(false);
        Lobby.SetActive(true);
        ServerControl.SetActive(true);
        SpielerMiniGames[0].transform.parent.gameObject.SetActive(false);

        if (ServerControlGameSelection.activeInHierarchy)
            DisplayGameFiles();
        UpdateSpieler();
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
        Debug.Log(player.name + " " + player.id + " -> "+ cmd + "   ---   " + data);
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

            case "#ClientSetName":
                ClientSetName(player, data);
                break;
            case "#SpielerIconChange":
                SpielerIconChange(player);
                break;
            case "#ChangePlayerName":
                ChangePlayerName(player, data);
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

    private void ClientSetName(Player player, String data)
    {
        string version = data.Replace("[VERSION]", "|").Split('|')[1];
        // Spieler hat eine falsche Version
        if (version != Config.APPLICATION_VERSION)
        {
            Logging.add(new Logging(Logging.Type.Warning, "Server", "ClientSetName", "Spieler ID: " + player.id + " versucht mit einer falschen Version beizutreten.Spieler Version: " + version + " | Server Version: " + Config.APPLICATION_VERSION));
            SendMessage("#WrongVersion " + Application.version, player);
            return;
        }
        // Legt Spielernamen fest
        string name = data.Replace("[NAME]", "|").Split('|')[1];
        if (name.Length > Config.MAX_PLAYER_NAME_LENGTH)
            name = name.Substring(0, Config.MAX_PLAYER_NAME_LENGTH);
        if (name == Config.PLAYER_NAME)
            name = name + "1";
        foreach (Player pl in Config.PLAYERLIST)
        {
            if (pl.name == name)
            {
                name = name + "1";
                SendMessage("#SpielerChangeName " + name, player);
            }
        }
        player.name = name;
        // Sendet Update an alle Spieler & Updatet Spieler Anzeigen#SpielerChangeName
        UpdateSpielerBroadcast();
    }
    public void UpdateRemoteConfig()
    {
        Broadcast("#UpdateRemoteConfig");
        LoadConfigs.FetchRemoteConfig();
    }

    private string UpdateSpieler()
    {
        string msg = "#UpdateSpieler [ID]0[ID][NAME]"+Config.PLAYER_NAME+"[NAME][PUNKTE]"+Config.SERVER_PLAYER_POINTS+"[PUNKTE][ICON]"+Config.SERVER_DEFAULT_ICON.name+"[ICON]";
        int connectedplayer = 1;
        List<string> spielerIDNameList = new List<string>();
        spielerIDNameList.Add("");
        foreach (Player player in Config.PLAYERLIST)
        {
            msg += "[TRENNER][ID]" + player.id + "[ID][NAME]" + player.name + "[NAME][PUNKTE]" + player.points + "[PUNKTE][ICON]" + player.icon.name+ "[ICON]";
            if (player.isConnected)
            {
                connectedplayer++;
                SpielerAnzeigeLobby[player.id].SetActive(true);
                SpielerAnzeigeLobby[player.id].GetComponentsInChildren<Image>()[1].sprite = player.icon;
                SpielerAnzeigeLobby[player.id].GetComponentsInChildren<TMP_Text>()[0].text = player.name;

                spielerIDNameList.Add(player.id+"| "+player.name);
            }
            else
                SpielerAnzeigeLobby[player.id].SetActive(false);
        }
       //msg = msg.Substring(0, msg.Length - 9);

        GameObject.Find("Lobby/Title_LBL/Spieleranzahl").GetComponent<TMP_Text>().text = connectedplayer+"/"+(Config.PLAYERLIST.Length+1);
        SpielerAnzeigeLobby[0].SetActive(true);
        SpielerAnzeigeLobby[0].GetComponentsInChildren<Image>()[1].sprite = Config.SERVER_DEFAULT_ICON;
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
            foreach (Sprite sprite in Config.PLAYER_ICONS)
                bilderliste.Add(sprite.name);
            GameObject.Find("ServerControl/ControlField/SpielerIconWechseln/Bilder").GetComponent<TMP_Dropdown>().AddOptions(bilderliste);
        }

        return msg;
    }

    // Sendet aktualisierte Spielerinfos an alle Spieler
    // -> UpdateSpieler()
    private void UpdateSpielerBroadcast()
    {
        Broadcast(UpdateSpieler(), Config.PLAYERLIST);
    }
    /**
     * Wirft den angegebenen Spieler raus
     */
    public void SpielerRauswerfen(TMP_Dropdown dropdown)
    {
        if (dropdown.options[dropdown.value].text == "")
            return;
        int playerid = Int32.Parse(dropdown.options[dropdown.value].text.Split('|')[0]);
        SendMessage("#ServerClosed", Config.PLAYERLIST[playerid - 1]);
        Config.PLAYERLIST[playerid - 1].name = "";
        Config.PLAYERLIST[playerid - 1].isConnected = false;
        Config.PLAYERLIST[playerid - 1].isDisconnected = true;
        UpdateSpielerBroadcast();
    }
    #region Spieler Namen Ändern
    /**
     * Erlaubt/Verbietet Namenswechsel bei Spielern
     */
    public void SpielerUmbenennenToggle(Toggle toggle)
    {
        Config.ALLOW_PLAYERNAME_CHANGE = toggle.isOn;
        Broadcast("#AllowNameChange "+ toggle.isOn);
    }
    /**
     * Benennt einen Spieler um
     */
    public void SpielerUmbenennenNamen(TMP_InputField input)
    {
        TMP_Dropdown drop = GameObject.Find("ServerControl/ControlField/SpielerUmbenennen/Dropdown").GetComponent<TMP_Dropdown>();
        if (drop.options[drop.value].text == "")
        {
            Config.PLAYER_NAME = input.text;
            UpdateSpielerBroadcast();
            return;
        }
        int id = Int32.Parse(drop.options[drop.value].text.Split('|')[0]);
        string name = input.text;
        if (name.Length > Config.MAX_PLAYER_NAME_LENGTH)
            name = name.Substring(0, Config.MAX_PLAYER_NAME_LENGTH);
        if (name == Config.PLAYER_NAME)
            name = name + "1";
        foreach (Player pl in Config.PLAYERLIST)
        {
            if (pl.name == name)
            {
                name = name + "1";
            }
        }
        Config.PLAYERLIST[id - 1].name = name;
        UpdateSpielerBroadcast();
    }
    /**
     * Ändert Namen von Spieler aus gesenet
     */
    private void ChangePlayerName(Player p, string data)
    {
        if (!Config.ALLOW_PLAYERNAME_CHANGE)
            return;
        string name = data;
        if (name.Length > Config.MAX_PLAYER_NAME_LENGTH)
            name = name.Substring(0, Config.MAX_PLAYER_NAME_LENGTH);
        if (name == Config.PLAYER_NAME)
            name = name + "1";
        foreach (Player pl in Config.PLAYERLIST)
        {
            if (pl.name == name)
            {
                name = name + "1";
            }
        }
        p.name = name;
        UpdateSpielerBroadcast();
    }
    #endregion
    #region Spieler Icon Ändern
    /**
     * Erlaubt Spielern das Icon zu wechseln
     */
    public void SpielerIconToggle(Toggle toggle)
    {
        Config.ALLOW_ICON_CHANGE = toggle.isOn;
    }
    /**
     *  Wechselt das Icon eines Spielers
     */
    public void SpielerIconWechsel(TMP_Dropdown Icon)
    {
        TMP_Dropdown drop = GameObject.Find("ServerControl/ControlField/SpielerIconWechseln/Dropdown").GetComponent<TMP_Dropdown>();
        if (drop.options[drop.value].text == "")
        {
            Config.SERVER_DEFAULT_ICON = Resources.Load<Sprite>("Images/ProfileIcons/" + Icon.options[Icon.value].text);
            UpdateSpielerBroadcast();
            return;
        }
        int id = Int32.Parse(drop.options[drop.value].text.Split('|')[0]);
        Config.PLAYERLIST[id - 1].icon = Resources.Load<Sprite>("Images/ProfileIcons/" + Icon.options[Icon.value].text);
        UpdateSpielerBroadcast();
    }
    /**
     * Icon Wechsel eines Spielers
     */
    private void SpielerIconChange(Player p)
    {
        if (!Config.ALLOW_ICON_CHANGE)
            return;
        p.icon = Config.PLAYER_ICONS[(Config.PLAYER_ICONS.IndexOf(p.icon) + 1) % Config.PLAYER_ICONS.Count];
        if (p.icon.name == "empty")
            p.icon = Config.PLAYER_ICONS[(Config.PLAYER_ICONS.IndexOf(p.icon) + 1) % Config.PLAYER_ICONS.Count];
        Debug.LogWarning(Config.PLAYER_ICONS.Count);
        UpdateSpielerBroadcast();
    }
    /**
     * Ändert das Icon des Servers
     */
    public void ServerIconChange()
    {
        if (!Config.isServer)
            return;
        Config.SERVER_DEFAULT_ICON = Config.PLAYER_ICONS[(Config.PLAYER_ICONS.IndexOf(Config.SERVER_DEFAULT_ICON) + 1) % Config.PLAYER_ICONS.Count];
        UpdateSpielerBroadcast();
    }
    #endregion
    #region MiniSpielauswahl
    public void SwitchMiniGame(TMP_Dropdown drop)
    {
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
    private void SwitchToTickTackToe()
    {
        SpielerMiniGames[0].SetActive(true);
        Broadcast("#SwitchToTickTackToe");
    }
    private void StartTickTackToe(Player player)
    {
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
        SendMessage("#TickTackToeZug " + msg, player);
    }
    private void TickTackToeSpielerZug(Player player, string data)
    {
        // Freie Felder berechnen
        List<int> freieFelder = TickTackToe.GetFreieFelder(data);
        List<string> belegteFelder = TickTackToe.GetBelegteFelder(data);
        // CheckForWin
        if (TickTackToe.CheckForEnd(freieFelder, belegteFelder))
        {
            SendMessage("#TickTackToeZugEnde " + data, player);
            return;
        }
        // Ziehen
        belegteFelder = TickTackToe.ServerZiehen(freieFelder, belegteFelder);
        freieFelder = TickTackToe.GetFreieFelder(belegteFelder);
        //Check for End
        if (TickTackToe.CheckForEnd(freieFelder, belegteFelder))
            SendMessage("#TickTackToeZugEnde " + TickTackToe.PrintBelegteFelder(belegteFelder), player);
        else
            SendMessage("#TickTackToeZug " + TickTackToe.PrintBelegteFelder(belegteFelder), player);
    }
    #endregion
    #endregion

    /**
     * Wechsel zw. Spielauswahl & Kontrollfelder
     */
    public void WechselGameSelControlFie(string s)
    {
        if (s == "Spielauswahl")
        {
            ServerControlGameSelection.SetActive(true);
            ServerControlControlField.SetActive(false);
        }
        else if (s == "Kontrollfelder")
        {
            ServerControlControlField.SetActive(true);
            ServerControlGameSelection.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Unbekannte Auswahl: WechselGameSelControlFie -> " + s);
        }
    }
    public void DisplayGameFiles()
    {
        TMP_Dropdown QuizDropdown = GameObject.Find("GameSelection/Quiz/QuizAuswahl").GetComponent<TMP_Dropdown>();
        QuizDropdown.ClearOptions();
        QuizDropdown.AddOptions(Config.QUIZ_SPIEL.getQuizzeAsStringList());
    }

    #region Starte Spiele
    public void StarteQuiz(TMP_Dropdown drop)
    {
        Config.QUIZ_SPIEL.setSelected(Config.QUIZ_SPIEL.getQuizByIndex(drop.value));
        // TODO: Sendet Befehle das Spieler mitladen sollen
        Debug.LogWarning(Config.QUIZ_SPIEL.getSelected().getTitel());
        SceneManager.LoadScene("Quiz");
    }
    #endregion
}