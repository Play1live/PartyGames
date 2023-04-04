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

    [SerializeField] GameObject gesperrtfuerSekundenAnzeige;
    DateTime allowedStartTime;

    void Start()
    {
        if (Config.SERVER_STARTED)
            SperreGameSelection();

        #region Startet Server
        if (!Config.SERVER_STARTED)
        {
            try
            {
                Config.SERVER_TCP = new TcpListener(IPAddress.Any, Config.SERVER_CONNECTION_PORT);
                Config.SERVER_TCP.Start();
                startListening();
                Config.SERVER_STARTED = true;
                Logging.add(Logging.Type.Normal, "StartupServer", "Start", "Server gestartet. Port: " + Config.SERVER_CONNECTION_PORT);
                Config.HAUPTMENUE_FEHLERMELDUNG = "Server wurde gestartet.";
            }
            catch (Exception e)
            {
                Logging.add(Logging.Type.Fatal, "StartupServer", "Start", "Server kann nicht gestartet werden", e);
                Config.HAUPTMENUE_FEHLERMELDUNG = "Server kann nicht gestartet werden.";
                Config.SERVER_STARTED = false;
                try
                {
                    Config.SERVER_TCP.Server.Close();
                }
                catch (Exception e1)
                {
                    Logging.add(Logging.Type.Fatal, "StartupServer", "Start", "Socket kann nicht geschlossen werden.", e1);
                }
                SceneManager.LoadScene("Startup");
                return;
            }
            // Wenn Server "Henryk" bild legen
            if (Config.PLAYER_NAME.ToLower().Equals("henryk") || Config.PLAYER_NAME.ToLower().Equals("play1live") || Config.PLAYER_NAME.ToLower().Equals("play"))
            {
                Config.SERVER_ICON = FindIconByName("Henryk");
                UpdateSpieler();
            }

            // Verbindung erfolgreich
            Config.HAUPTMENUE_FEHLERMELDUNG = "";
            SperreGameSelection();

           // StartCoroutine(TestConnectionToClients());
        }
        #endregion

        // Potenziell Fehleranfällig, wenn Dateien nicht korrekt sind
        try
        {
            //SetupSpiele.LoadGameFiles();
        } catch (Exception e)
        {
            Logging.add(Logging.Type.Error, "StartupServer", "Start", "Es kam zu einem Fehler beim Laden der Spieldateien!\n", e);
        }

        Hauptmenue.SetActive(false);
        Lobby.SetActive(true);
        ServerControl.SetActive(true);
        SpielerMiniGames[0].transform.parent.gameObject.SetActive(false);

        if (ServerControlGameSelection.activeInHierarchy && ServerControlGameSelection.transform.GetChild(1).gameObject.activeInHierarchy)
            DisplayGameFiles();
        UpdateSpieler();
    }

    IEnumerator TestConnectionToClients()
    {
        while (true)
        {
            foreach (Player p in Config.PLAYERLIST)
            {
                yield return new WaitForSeconds(15);
                if (!p.isConnected)
                    continue;
                SendMessage("#TestConnection", p);
            }
        }
    }

    private void OnEnable()
    {
        //EventSystem.SetActive(true);
        Hauptmenue.SetActive(false);
        Lobby.SetActive(true);
        ServerControl.SetActive(true);
        SpielerMiniGames[0].transform.parent.gameObject.SetActive(false);

        if (ServerControlGameSelection.activeInHierarchy && ServerControlGameSelection.transform.GetChild(1).gameObject.activeInHierarchy)
            DisplayGameFiles();
        UpdateSpieler();
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
                try
                {
                    /*
                    NetworkStream stream = spieler.tcp.GetStream();
                    if (stream.DataAvailable)
                    {
                        //StreamReader reader = new StreamReader(stream, true);
                        StreamReader reader = new StreamReader(stream);
                        string data = reader.ReadLine();

                        if (data != null)
                            OnIncommingData(spieler, data);
                    }*/
                }
                catch (Exception e)
                {
                    Logging.add(Logging.Type.Error, "StartupServer", "Update", "TODO: Nachricht kann nicht empfangen werden, Spieler wird gekickt: ", e);
                    ClientClosed(spieler);
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
                        Logging.add(Logging.Type.Normal, "StartupServer", "Update", "Spieler hat die Verbindung getrennt. ID: " + Config.PLAYERLIST[i].id);
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

    private void OnApplicationQuit()
    {
        Broadcast("#ServerClosed", Config.PLAYERLIST);
        Logging.add(Logging.Type.Normal, "StartupServer", "OnApplicationQuit", "Server wird geschlossen");
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
        Logging.add(Logging.Type.Normal, "Server", "AcceptTcpClient", "Spieler: " + freierS.id + " ist jetzt verbunden. IP:" + freierS.tcp.Client.RemoteEndPoint);
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
            Logging.add(Logging.Type.Error, "Server", "SendMessage", "Nachricht an Client: " + sc.id + " (" + sc.name + ") konnte nicht gesendet werden." + e);
            // Verbindung zum Client wird getrennt
            //ClientClosed(sc);
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
    /**
     * Verarbeitet die eingehenden Commands der Spieler
     */
    public void Commands(Player player, string data, string cmd)
    {
        // Zeigt alle einkommenden Nachrichten an
        //Logging.add(Logging.Type.Normal, "StartupServer", "Commands", player.name + " " + player.id + " -> " + cmd + "   ---   " + data);
        // Sucht nach Command
        switch (cmd)
        {
            default:
                Logging.add(Logging.Type.Warning, "StartupServer", "Commands", "Unkown Command: (" + player.id + ") " + player.name + " -> " + cmd + " - " + data);
                break;

            case "#ClientClosed":
                ClientClosed(player);
                UpdateSpielerBroadcast();
                break;
            case "#TestConnection":
                SendMessage("#ConnectionEstablished", player);
                break;
            case "#ClientFocusChange":
                //Debug.Log("FocusChange (" + player.id + ") " + player.name + ": InGame: " + data);
                break;
            case "#GetSpielerUpdate":
                UpdateSpielerBroadcast();
                break;

            case "#ClientSetName":
                ClientSetName(player, data);
                break;
            case "#SpielerIconChange":
                SpielerIconChange(player, data);
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
    /**
     * Fordert alle Spieler auf die RemoteConfig neuzuladen
     */
    public void UpdateRemoteConfig()
    {
        Broadcast("#UpdateRemoteConfig");
        LoadConfigs.FetchRemoteConfig();
    }
    /**
     * Sperrt die Gameselection für X sekunden, um fehler bei Scenen wechseln in der Verbindung zu verhindern
     */
    private void SperreGameSelection()
    {
        allowedStartTime = DateTime.Now.AddSeconds(5);
        for (int i = 0; i < ServerControlGameSelection.transform.childCount; i++)
        {
            ServerControlGameSelection.transform.GetChild(i).gameObject.SetActive(false);
        }
        gesperrtfuerSekundenAnzeige.SetActive(true);
    }
    private void UpdateGesperrtGameSelection()
    {
        gesperrtfuerSekundenAnzeige.GetComponent<TMP_Text>().text = "Spiele sind noch " + ((allowedStartTime.Hour - DateTime.Now.Hour)*60*60 + (allowedStartTime.Minute - DateTime.Now.Minute) * 60 + (allowedStartTime.Second - DateTime.Now.Second)) + " Sekunden gesperrt.";
    }
    /**
     * Entsperrt die Gameselection
     */
    public void EntsperreGameSelection()
    {
        allowedStartTime = DateTime.MinValue;
        for (int i = 0; i < ServerControlGameSelection.transform.childCount; i++)
        {
            ServerControlGameSelection.transform.GetChild(i).gameObject.SetActive(true);
        }
        gesperrtfuerSekundenAnzeige.SetActive(false);
        DisplayGameFiles();
    }

    /**
     * Spieler beendet das Spiel
     */
    private void ClientClosed(Player player)
    {
        player.icon = Resources.Load<Sprite>("Images/ProfileIcons/empty");
        player.name = "";
        player.points = 0;
        player.isConnected = false;
        player.isDisconnected = true;

        /*for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            if (Config.PLAYERLIST[i].id == player.id)
            {
                Config.PLAYERLIST[i].icon = Resources.Load<Sprite>("Images/ProfileIcons/empty");
                Config.PLAYERLIST[i].name = "";
                Config.PLAYERLIST[i].points = 0;
                Config.PLAYERLIST[i].isConnected = false;
                Config.PLAYERLIST[i].isDisconnected = true;
                break;
            }
        }*/
    }
    /**
     * Sendet die aktualisierten Spielerinfos an alle Spieler
     */
    private void UpdateSpielerBroadcast()
    {
        Broadcast(UpdateSpieler(), Config.PLAYERLIST);
    }
    /**
     * Updatet die Spieler Informations Anzeigen und gibt diese als String zurück 
     */
    private string UpdateSpieler()
    {
        string msg = "#UpdateSpieler [ID]0[ID][NAME]" + Config.PLAYER_NAME + "[NAME][PUNKTE]" + Config.SERVER_PLAYER_POINTS + "[PUNKTE][ICON]" + Config.SERVER_ICON.name + "[ICON]";
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
        //msg = msg.Substring(0, msg.Length - 9);

        GameObject.Find("Lobby/Title_LBL/Spieleranzahl").GetComponent<TMP_Text>().text = connectedplayer + "/" + (Config.PLAYERLIST.Length + 1);
        SpielerAnzeigeLobby[0].SetActive(true);
        SpielerAnzeigeLobby[0].GetComponentsInChildren<Image>()[1].sprite = Config.SERVER_ICON;
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
        Debug.Log(msg);
        return msg;
    }

    #region Spieler Namen Ändern
    /**
     * Prüft ob der übergebene Name bereits von einem Spieler oder dem Server belegt ist
     */
    private bool SpielernameIstBelegt(string name)
    {
        if (Config.PLAYER_NAME == name)
            return true;
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
            if (Config.PLAYERLIST[i].name == name)
                return true;
        return false;
    }
    /**
     * Speichert den Namen, den sich der neu Verbundene Spieler geben will.
     * Zudem wird die GameVersion verglichen.
     */
    private void ClientSetName(Player player, String data)
    {
        string version = data.Replace("[VERSION]", "|").Split('|')[1];
        // Spieler hat eine falsche Version
        if (version != Config.APPLICATION_VERSION)
        {
            Logging.add(Logging.Type.Warning, "Server", "ClientSetName", "Spieler ID: " + player.id + " versucht mit einer falschen Version beizutreten.Spieler Version: " + version + " | Server Version: " + Config.APPLICATION_VERSION);
            SendMessage("#WrongVersion " + Application.version, player);
            return;
        }
        // Legt Spielernamen fest
        string name = data.Replace("[NAME]", "|").Split('|')[1];
        if (name.Length > Config.MAX_PLAYER_NAME_LENGTH)
            name = name.Substring(0, Config.MAX_PLAYER_NAME_LENGTH);
        if (!SpielernameIstBelegt(name))
        {
            player.name = name;
            SendMessage("#SpielerChangeName " + name, player);
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
        player.name = name;
        SendMessage("#SpielerChangeName " + name, player);
        // Sendet Update an alle Spieler & Updatet Spieler Anzeigen#SpielerChangeName
        UpdateSpielerBroadcast();
    }   
    /**
     * Erlaubt/Verbietet Namenswechsel bei Spielern
     */
    public void SpielerUmbenennenToggle(Toggle toggle)
    {
        Config.ALLOW_PLAYERNAME_CHANGE = toggle.isOn;
        Broadcast("#AllowNameChange " + toggle.isOn);
    }
    /**
     * Spieler Ändert Namen
     */
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
        p.name = name;
        UpdateSpielerBroadcast();
    }
    /**
    * Benennt einen Spieler um
    */
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
            Config.PLAYERLIST[id - 1].name = name;
            UpdateSpielerBroadcast();
        }
    }
    #endregion
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
            Config.SERVER_ICON = Resources.Load<Sprite>("Images/ProfileIcons/" + Icon.options[Icon.value].text);
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
    private void SpielerIconChange(Player p, string data)
    {
        Sprite neuesIcon;
        if (data.Equals("0")) // Initial Änderung Icon
        {
            if (p.name.ToLower().Contains("spieler"))
            {
                neuesIcon = FindIconByName("Samurai");
                IconFestlegen(p, neuesIcon);
                return;
            }
            else if (p.name.ToLower().Contains("alan"))
            {
                neuesIcon = FindIconByName("Alan");
                IconFestlegen(p, neuesIcon);
                return;
            }
            else if (p.name.ToLower().Contains("fiona"))
            {
                neuesIcon = FindIconByName("Fiona");
                IconFestlegen(p, neuesIcon);
                return;
            }
            else if (p.name.ToLower().Contains("hannah") 
                || (p.name.ToLower().StartsWith("ha") && p.name.ToLower().EndsWith("nah")) 
                || (p.name.ToLower().StartsWith("han") && p.name.ToLower().EndsWith("ah"))
                || (p.name.ToLower().StartsWith("haa") && p.name.ToLower().EndsWith("aah") && p.name.ToLower().Substring(3, p.name.Length-3).Contains("nn")) )
            {
                neuesIcon = FindIconByName("Hannah");
                IconFestlegen(p, neuesIcon);
                return;
            }
            else if (p.name.ToLower().Contains("henryk"))
            {
                neuesIcon = FindIconByName("Henryk");
                IconFestlegen(p, neuesIcon);
                return;
            }
            else if (p.name.ToLower().Contains("maxe"))
            {
                neuesIcon = FindIconByName("Maxe");
                IconFestlegen(p, neuesIcon);
                return;
            }
            else if (p.name.ToLower().Contains("michi") ||
                p.name.ToLower().Contains("michelle") )
            {
                neuesIcon = FindIconByName("Michi");
                IconFestlegen(p, neuesIcon);
                return;
            }
            else if (p.name.ToLower().Contains("munk"))
            {
                neuesIcon = FindIconByName("Munk");
                IconFestlegen(p, neuesIcon);
                return;
            }
            else if (p.name.ToLower().Contains("nils")
                || p.name.ToLower().Contains("nille")
                || p.name.ToLower().Contains("kater")
                || p.name.ToLower().Contains("katerjunge"))
            {
                neuesIcon = FindIconByName("Nils");
                IconFestlegen(p, neuesIcon);
                return;
            }
            else if (p.name.ToLower().Contains("ronald")
                || p.name.ToLower().Contains("ron")
                || p.name.ToLower().Contains("sterni")
                || p.name.ToLower().Contains("sternfaust"))
            {
                neuesIcon = FindIconByName("Ronald");
                IconFestlegen(p, neuesIcon);
                return;
            }
            else
            {
                Debug.LogWarning("Unknown Playername for init Icon Change: "+ p.name);
            }
        }

        // Spieler gewollte änderung des Icons
        if (!Config.ALLOW_ICON_CHANGE)
            return;

        neuesIcon = Config.PLAYER_ICONS[(Config.PLAYER_ICONS.IndexOf(p.icon) + 1) % Config.PLAYER_ICONS.Count];
        IconFestlegen(p, neuesIcon);
    }
    private Sprite FindIconByName(string name)
    {
        foreach (Sprite sprite in Config.PLAYER_ICONS)
        {
            if (sprite.name.Equals(name))
                return sprite;
        }
        return null;
    }
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

    /**
     * Prüft ob das übergebene Icon bereits vom Server oder einem anderen Spieler benutzt wird
     */
    private bool IconWirdGeradeGenutzt(Sprite icon)
    {
        if (Config.SERVER_ICON == icon)
            return true;
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
            if (icon == Config.PLAYERLIST[i].icon || icon.name == "empty")
                return true;
        return false;
    }
    /**
     * Ändert das Icon des Servers
     */
    public void ServerIconChange()
    {
        if (!Config.isServer)
            return;
        Config.SERVER_ICON = Config.PLAYER_ICONS[(Config.PLAYER_ICONS.IndexOf(Config.SERVER_ICON) + 1) % Config.PLAYER_ICONS.Count];
        UpdateSpielerBroadcast();
    }
    #endregion
    #region MiniSpielauswahl
    /**
     * Wechselt das angezeigt Minigame in der Lobby für die Spieler
     */
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
    /**
     * Zeigt TickTackToe für alle Spieler an
     */
    private void SwitchToTickTackToe()
    {
        SpielerMiniGames[0].SetActive(true);
        Broadcast("#SwitchToTickTackToe");
    }
    /**
     * Bestimmt den ersten Zug gegen einen Spieler
     */
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
    /**
     * Lässt den Server einen Zug machen & prüft ob das Spiel beendet ist
     */
    private void TickTackToeSpielerZug(Player player, string data)
    {
        // Freie Felder berechnen
        List<int> freieFelder = TickTackToe.GetFreieFelder(data);
        List<string> belegteFelder = TickTackToe.GetBelegteFelder(data);
        // CheckForWin
        if (TickTackToe.CheckForEnd(freieFelder, belegteFelder))
        {
            SendMessage("#TickTackToeZugEnde |" + TickTackToe.getResult(belegteFelder) + "| " + data, player);
            return;
        }
        // Ziehen
        belegteFelder = TickTackToe.ServerZiehen(freieFelder, belegteFelder);
        freieFelder = TickTackToe.GetFreieFelder(belegteFelder);
        //Check for End
        if (TickTackToe.CheckForEnd(freieFelder, belegteFelder))
            SendMessage("#TickTackToeZugEnde |" + TickTackToe.getResult(belegteFelder) + "|" + TickTackToe.PrintBelegteFelder(belegteFelder), player);
        else
            SendMessage("#TickTackToeZug " + TickTackToe.PrintBelegteFelder(belegteFelder), player);
    }
    #endregion
    #endregion
    /**
     * Wechsel zw. Spielauswahl & Kontrollfelder des Servers
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
            UpdateSpieler();
        }
        else
        {
            Logging.add(Logging.Type.Warning, "StartupServer", "WechselGameSelectionFie", "Unbekannte Auswahl: WechselGameSelControlFie -> " + s);
        }
    }


    /**
     * Die geladenen Spiele in der GameÜbersicht an
     */
    public void DisplayGameFiles()
    {
        if (!ServerControlGameSelection.activeInHierarchy)
            return;

        TMP_Dropdown QuizDropdown = GameObject.Find("Quiz/QuizAuswahl").GetComponent<TMP_Dropdown>();
        QuizDropdown.ClearOptions();
        QuizDropdown.AddOptions(Config.QUIZ_SPIEL.getQuizzeAsStringList());

        TMP_Dropdown ListenDropdown = GameObject.Find("Listen/ListenAuswahl").GetComponent<TMP_Dropdown>();
        ListenDropdown.ClearOptions();
        ListenDropdown.AddOptions(Config.LISTEN_SPIEL.getListenAsStringList());

        TMP_Dropdown MosaikDropdown = GameObject.Find("Mosaik/Auswahl").GetComponent<TMP_Dropdown>();
        MosaikDropdown.ClearOptions();
        MosaikDropdown.AddOptions(Config.MOSAIK_SPIEL.getListenAsStringList());

        TMP_Dropdown GeheimwoerterDropdown = GameObject.Find("Geheimwörter/Auswahl").GetComponent<TMP_Dropdown>();
        GeheimwoerterDropdown.ClearOptions();
        GeheimwoerterDropdown.AddOptions(Config.GEHEIMWOERTER_SPIEL.getListenAsStringList());

        TMP_Dropdown WerBietetMehrDropdown = GameObject.Find("WerBietetMehr/Auswahl").GetComponent<TMP_Dropdown>();
        WerBietetMehrDropdown.ClearOptions();
        WerBietetMehrDropdown.AddOptions(Config.WERBIETETMEHR_SPIEL.getQuizzeAsStringList());

        TMP_Dropdown AuktionDropdown = GameObject.Find("Auktion/Auswahl").GetComponent<TMP_Dropdown>();
        AuktionDropdown.ClearOptions();
        AuktionDropdown.AddOptions(Config.AUKTION_SPIEL.getListenAsStringList());
    }
    #region Starte Spiele
    /**
     * Startet das Flaggen Spiel
     */
    public void StarteFlaggen()
    {
        Logging.add(Logging.Type.Normal, "StartupServer", "StarteFlaggen", "Flaggen starts");

        SceneManager.LoadScene("Flaggen");
        Broadcast("#StarteSpiel Flaggen");
    }
    /**
     * Startet das Quiz Spiel -> Alle Spieler laden in die neue Scene
     */
    public void StarteQuiz(TMP_Dropdown drop)
    {
        Config.QUIZ_SPIEL.setSelected(Config.QUIZ_SPIEL.getQuizByIndex(drop.value));
        Logging.add(Logging.Type.Normal, "StartupServer", "StarteQuiz", "Quiz starts: " + Config.QUIZ_SPIEL.getSelected().getTitel());

        SceneManager.LoadScene("Quiz");
        Broadcast("#StarteSpiel Quiz");
    }
    /**
     * Startet das Listen Spiel -> Alle Spieler laden in die neue Scene
     */
    public void StarteListen(TMP_Dropdown drop)
    {
        Config.LISTEN_SPIEL.setSelected(Config.LISTEN_SPIEL.getListe(drop.value));
        Logging.add(Logging.Type.Normal, "StartupServer", "StarteListen", "Listen starts: " + Config.LISTEN_SPIEL.getSelected().getTitel());

        SceneManager.LoadScene("Listen");
        Broadcast("#StarteSpiel Listen");
    }
    /**
     * Starte das Mosaik Spiel -> Alle Spieler laden in die neue Scene
     */
    public void StarteMosaik(TMP_Dropdown drop)
    {
        Config.MOSAIK_SPIEL.setSelected(Config.MOSAIK_SPIEL.getMosaik(drop.value));
        Logging.add(Logging.Type.Normal, "StartupServer", "StarteMosaik", "Mosaik starts: " + Config.MOSAIK_SPIEL.getSelected().getTitel());

        SceneManager.LoadScene("Mosaik");
        Broadcast("#StarteSpiel Mosaik");
    }
    /**
     * Starte das Geheimwörter Spiel -> Alle Spieler laden in die neue Scene
     */
    public void StarteGeheimwörter(TMP_Dropdown drop)
    {
        Config.GEHEIMWOERTER_SPIEL.setSelected(Config.GEHEIMWOERTER_SPIEL.getListe(drop.value));
        Logging.add(Logging.Type.Normal, "StartupServer", "StarteGeheimwörter", "Geheimwörter starts: " + Config.GEHEIMWOERTER_SPIEL.getSelected().getTitel());

        SceneManager.LoadScene("Geheimwörter");
        Broadcast("#StarteSpiel Geheimwörter");
    }
    /**
     * Starte das WerBietetMehr Spiel -> Alle Spieler laden in die neue Scene
     */
    public void StarteWerBietetMehr(TMP_Dropdown drop)
    {
        Config.WERBIETETMEHR_SPIEL.setSelected(Config.WERBIETETMEHR_SPIEL.getQuizByIndex(drop.value));
        Logging.add(Logging.Type.Normal, "StartupServer", "StarteWerBietetMehr", "WerBietetMehr starts: " + Config.WERBIETETMEHR_SPIEL.getSelected().getTitel());

        SceneManager.LoadScene("WerBietetMehr");
        Broadcast("#StarteSpiel WerBietetMehr");
    }
    /**
     * Starte das Auktion Spiel -> Alle Spieler laden in die neue Scene
     */
    public void StarteAuktion(TMP_Dropdown drop)
    {
        Config.AUKTION_SPIEL.setSelected(Config.AUKTION_SPIEL.getAuktion(drop.value));
        Logging.add(Logging.Type.Normal, "StartupServer", "StarteAuktion", "Auktion starts: " + Config.AUKTION_SPIEL.getSelected().getTitel());

        //SceneManager.LoadSceneAsync("Auktion");
        Broadcast("#StarteSpiel Auktion");
        SceneManager.LoadScene("Auktion");
    }
    #endregion
}