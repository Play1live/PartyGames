using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenschAergerDichNichtServer : MonoBehaviour
{
    [SerializeField] GameObject Lobby;
    private GameObject[] Playerlist;
    [SerializeField] GameObject Games;
    [SerializeField] GameObject[] Maps;
    [SerializeField] GameObject SpielprotokollContent;
    [SerializeField] GameObject W�rfel;
    private Coroutine WuerfelCoroutine;
    private Coroutine BotCoroutine;
    [SerializeField] GameObject InfoBoard;
    bool[] PlayerConnected;

    [SerializeField] AudioSource DisconnectSound;

    MenschAegerDichNichtBoard board;
    private bool clientCanPickField;
    private bool ServerAllowZugWahl;

    private List<string> broadcastmsgs;

    void OnEnable()
    {
        broadcastmsgs = new List<string>();
        PlayerConnected = new bool[Config.SERVER_MAX_CONNECTIONS];
        Lobby.SetActive(true);
        Games.SetActive(false);
        InitLobby();

        StartCoroutine(NewBroadcast());
    }

    void Update()
    {
        #region Server
        if (!Config.SERVER_STARTED)
        {
            SceneManager.LoadSceneAsync("Startup");
            return;
        }
        /*
        // Broadcastet alle MSGs nacheinander
        if (broadcastmsgs.Count != 0)
        {
            string msg = broadcastmsgs[0];
            broadcastmsgs.RemoveAt(0);
            Broadcast(msg);
        }*/

        foreach (Player spieler in Config.PLAYERLIST)
        {
            if (spieler.isConnected == false)
                continue;
            #region Sucht nach neuen Nachrichten
            if (spieler.isConnected == true)
            {
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

            #region Spieler Disconnected Message
            for (int i = 0; i < Config.PLAYERLIST.Length; i++)
            {
                if (Config.PLAYERLIST[i].isConnected == false)
                {
                    if (Config.PLAYERLIST[i].isDisconnected == true)
                    {
                        Logging.log(Logging.LogType.Normal, "Mensch�rgerDichNichtServer", "Update", "Spieler hat die Verbindung getrennt. ID: " + Config.PLAYERLIST[i].id);
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
        Logging.log(Logging.LogType.Normal, "Mensch�rgerDichNichtServer", "OnApplicationQuit", "Server wird geschlossen.");
        Config.SERVER_TCP.Server.Close();
    }

    IEnumerator NewBroadcast()
    {
        while (true)
        {
            // Broadcastet alle MSGs nacheinander
            if (broadcastmsgs.Count != 0)
            {
                string msg = broadcastmsgs[0];
                broadcastmsgs.RemoveAt(0);
                Broadcast(msg);
                yield return null;
            }
            yield return new WaitForSeconds(0.0001f);
        }
        yield break;
    }

    #region Server Stuff  
    #region Kommunikation
    /// <summary>
    /// Sendet eine Nachricht an den �bergebenen Spieler
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
            Logging.log(Logging.LogType.Warning, "Mensch�rgerDichNichtServer", "SendMSG", "Nachricht an Client: " + sc.id + " (" + sc.name + ") konnte nicht gesendet werden.", e);
            // Verbindung zum Client wird getrennt
            ClientClosed(sc);
        }
    }
    /// <summary>
    /// Sendet eine Nachricht an alle Spieler der liste
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
    /// Sendet eine Nachricht an alle verbundenen Spieler
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
    private void BroadcastNew(string data)
    {
        broadcastmsgs.Add(data);
    }
    /// <summary>
    /// Einkommende Nachrichten die von Spielern an den Server gesendet werden
    /// </summary>
    /// <param name="spieler">Spieler</param>
    /// <param name="data">Nachricht</param>
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
    /// Einkommende Befehle von Spielern
    /// </summary>
    /// <param name="player">Spieler</param>
    /// <param name="data">Befehlsargumente</param>
    /// <param name="cmd">Befehl</param>
    private void Commands(Player player, string data, string cmd)
    {
        // Zeigt alle einkommenden Nachrichten an
        Logging.log(Logging.LogType.Debug, "Mensch�rgerDichNichtServer", "Commands", "Eingehende Nachricht: " + player.name + " " + player.id + " -> " + cmd + "   ---   " + data);
        // Sucht nach Command
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "Mensch�rgerDichNichtServer", "Commands", "Unkown Command: " + cmd + " -> " + data);
                break;

            case "#ClientClosed":
                if (Lobby.activeInHierarchy)
                    UpdateLobby();
                if (Games.activeInHierarchy)
                    SpielerWirdZumBot(player);
                PlayDisconnectSound();
                ClientClosed(player);
                break;
            case "#TestConnection":
                break;
            case "#ClientFocusChange":
                break;

            case "#JoinMenschAergerDichNicht":
                PlayerConnected[player.id - 1] = true;
                UpdateLobby();
                break;

            case "#Wuerfel":
                Wuerfel(data);
                break;
            case "#ClientWaehltFeld":
                ClientW�hltFeld(player, data);
                break;
        }
    }
    #endregion
    /// <summary>
    /// Spieler beendet das Spiel
    /// </summary>
    /// <param name="player">Spieler</param>
    private void ClientClosed(Player player)
    {
        player.icon = Resources.Load<Sprite>("Images/ProfileIcons/empty");
        player.name = "";
        player.points = 0;
        player.isConnected = false;
        player.isDisconnected = true;
    }
    /// <summary>
    /// Spiel Verlassen & Zur�ck in die Lobby laden
    /// </summary>
    public void SpielVerlassenButton()
    {
        Logging.log(Logging.LogType.Debug, "Mensch�rgerDichNichtServer", "SpielVerlassenButton", "Spiel wird beendet. L�dt ins Hauptmen�.");
        SceneManager.LoadScene("Startup");
        BroadcastNew("#ZurueckInsHauptmenue");
    }
    /// <summary>
    /// Spielt den Disconnect Sound ab
    /// </summary>
    private void PlayDisconnectSound()
    {
        DisconnectSound.Play();
    }
    /// <summary>
    /// Initialisiert die LobbyAnzeigen
    /// </summary>
    private void InitLobby()
    {
        MenschAegerDichNichtBoard.bots = 0;
        Playerlist = new GameObject[Lobby.transform.GetChild(1).childCount];
        for (int i = 0; i < Lobby.transform.GetChild(1).childCount; i++)
        {
            Playerlist[i] = Lobby.transform.GetChild(1).GetChild(i).gameObject;
            Playerlist[i].SetActive(false);
        }
        UpdateLobbyServer();
        foreach (GameObject go in Maps)
        {
            go.SetActive(false);
        }
    }
    private void UpdateLobbyServer()
    {
        // init Server
        Playerlist[0].SetActive(true);
        Playerlist[0].GetComponentInChildren<TMP_Text>().text = Config.PLAYER_NAME;
        // init Spieler
        for (int i = 1; i < Config.SERVER_MAX_CONNECTIONS + 1; i++)
        {
            int index = i - 1;
            if (Config.PLAYERLIST[index].isConnected && Config.PLAYERLIST[index].name.Length > 0)
            {
                Playerlist[i].SetActive(true);
                Playerlist[i].GetComponentInChildren<TMP_Text>().text = Config.PLAYERLIST[index].name;
            }
            else
            {
                Playerlist[i].SetActive(false);
                Playerlist[i].GetComponentInChildren<TMP_Text>().text = "";
            }
        }
        // init Bots
        for (int i = Config.SERVER_MAX_CONNECTIONS + 1; i < Config.SERVER_MAX_CONNECTIONS + 1 + 8; i++)
        {
            int index = i - (1 + Config.SERVER_MAX_CONNECTIONS);
            Playerlist[i].GetComponentInChildren<TMP_Text>().text = MenschAegerDichNichtBoard.botnames[index];
            if (index < MenschAegerDichNichtBoard.bots)
                Playerlist[i].SetActive(true);
            else
                Playerlist[i].SetActive(false);
        }
    }
    /// <summary>
    /// Aktualisiert die LobbyAnzeige f�r alle
    /// </summary>
    private void UpdateLobby()
    {
        UpdateLobbyServer();
        string msg = "";
        for (int i = 0; i < Playerlist.Length; i++)
        {
            if (Playerlist[i].activeInHierarchy)
                msg += "|" + Playerlist[i].GetComponentInChildren<TMP_Text>().text;
        }
        if (msg.Length > 0)
            msg = msg.Substring("|".Length);
        BroadcastNew("#UpdateLobby " + msg);
    }
    /// <summary>
    /// �ndert die Anzahl der Bots
    /// </summary>
    /// <param name="drop"></param>
    public void ChangeBots(TMP_Dropdown drop)
    {
        int botsCount = Int32.Parse(drop.options[drop.value].text);
        MenschAegerDichNichtBoard.bots = botsCount;
        UpdateLobby();
    }
    /// <summary>
    /// Toggelt ob der Server zuschaut und nur Bots spielen
    /// </summary>
    /// <param name="toggle"></param>
    public void ChangeWatchOnly(Toggle toggle)
    {
        MenschAegerDichNichtBoard.watchBots = toggle.isOn;
    }
    #region GameLogic
    /// <summary>
    /// Startet das Mensch�rgerDichNicht Spiel
    /// </summary>
    public void StartGame()
    {
        Logging.log(Logging.LogType.Normal, "MenschAergerDichNichtServer", "StartGame", "Spiel wird gestartet.");
        W�rfel.transform.GetChild(0).gameObject.SetActive(false);
        if (MenschAegerDichNichtBoard.watchBots)
        {
            Logging.log(Logging.LogType.Normal, "MenschAergerDichNichtServer", "StartGame", "Es werden nur Bots spielen, alle Clients werden getrennt.");
            BroadcastNew("#ServerClosed");
            for (int i = 0; i < Config.PLAYERLIST.Length; i++)
                ClientClosed(Config.PLAYERLIST[i]);
            UpdateLobby();
        }
        #region SpielerInit
        List<string> names = new List<string>();
        List<bool> bots = new List<bool>();
        List<Sprite> sprites = new List<Sprite>();
        for (int i = 0; i < Playerlist.Length; i++)
        {
            if (Playerlist[i].activeInHierarchy)
            {
                if (Playerlist[i].name.StartsWith("Server "))
                {
                    if (!MenschAegerDichNichtBoard.watchBots)
                    {
                        names.Add(Playerlist[i].GetComponentInChildren<TMP_Text>().text);
                        bots.Add(false);
                        sprites.Add(Config.SERVER_ICON);
                    }
                }
                else if (Playerlist[i].name.StartsWith("P "))
                {
                    string playername = Playerlist[i].GetComponentInChildren<TMP_Text>().text;
                    names.Add(playername);
                    bots.Add(false);
                    sprites.Add(Config.PLAYERLIST[Player.getPosByName(playername)].icon);
                }
                else if (Playerlist[i].name.StartsWith("Bot "))
                {
                    int botIcon = Int32.Parse(Playerlist[i].name.Replace("Bot (", "").Replace(")", ""));
                    Sprite botSprite = Resources.LoadAll<Sprite>("Images/Icons")[botIcon];
                    names.Add(Playerlist[i].GetComponentInChildren<TMP_Text>().text);
                    bots.Add(true);
                    sprites.Add(botSprite); 
                }
                else
                {
                    Logging.log(Logging.LogType.Warning, "MenschAergerDichNichtServer", "StartGame", "Spielertyp konnte nicht gefunden werden. " + Playerlist[i].name);
                }
            }
        }
        Logging.log(Logging.LogType.Debug, "MenschAergerDichNichtServer", "StartGame", names.Count + " Spieler werden spielen.");
        List<MenschAergerDichNichtPlayer> randomplayer = new List<MenschAergerDichNichtPlayer>();
        string broadcastPlayer = "";
        while (names.Count > 0)
        {
            int random = UnityEngine.Random.Range(0, names.Count);
            randomplayer.Add(new MenschAergerDichNichtPlayer(randomplayer.Count, names[random], bots[random], sprites[random]));
            broadcastPlayer += "[#]" + names[random] + "*" + bots[random] + "*" + sprites[random].name;
            names.RemoveAt(random);
            bots.RemoveAt(random);
            sprites.RemoveAt(random);
        }
        if (broadcastPlayer.Length > 3)
            broadcastPlayer = broadcastPlayer.Substring("[#]".Length);
        #endregion
        #region InitBoard
        TMP_Dropdown MapDrop = GameObject.Find("ServerSide/MapAuswahl").GetComponent<TMP_Dropdown>();
        int mapInt = Int32.Parse(MapDrop.options[MapDrop.value].text.Replace(" Spieler", ""));
        Logging.log(Logging.LogType.Debug, "MenschAergerDichNichtServer", "StartGame", "Folgende Map wurde gew�hlt: " + mapInt + "P");
        Lobby.SetActive(false);
        Games.SetActive(true);
        GameObject selectedMap = null;
        foreach (GameObject go in Maps)
            if (go.name.Equals(mapInt + "P"))
                selectedMap = go;
        if (selectedMap == null)
            return;
        selectedMap.SetActive(true);

        int RunWaySize = selectedMap.transform.GetChild(0).childCount;

        if (randomplayer.Count == mapInt)
        {
            BroadcastNew("#StartGame [PLAYER]" + broadcastPlayer + "[PLAYER][MAP]" + selectedMap.name + "[MAP][TEAMSIZE]" + mapInt + "[TEAMSIZE][RUNWAY]" + RunWaySize + "[RUNWAY]");
            board = new MenschAegerDichNichtBoard(selectedMap, RunWaySize, mapInt, randomplayer);
        }
        else
        {
            Logging.log(Logging.LogType.Warning, "MenschAergerDichNichtServer", "StartGame", "Die Spieleranzahl stimmt nicht mit der gew�hlten Map �berein.");
            Lobby.SetActive(true);
            Games.SetActive(false);
            foreach (GameObject go in Maps)
                go.SetActive(false);
            return;
        }
        clientCanPickField = false;

        Debug.LogWarning(board.GetBoardString());
        #endregion

        // Hide PlayerAnimation
        selectedMap.transform.GetChild(4).gameObject.SetActive(false);

        string time = DateTime.Now.Hour + ":";
        if (DateTime.Now.Minute < 10)
            time += "0" + DateTime.Now.Minute;
        else
            time += DateTime.Now.Minute;
        AddMSGToProtokoll("Spiel wurde gestartet. " + time);
        DisplayMSGInfoBoard("Spiel wird geladen.");

        StartTurn();
    }
    /// <summary>
    /// F�gt eine Nachricht dem Spielprotokoll hinzu
    /// </summary>
    /// <param name="msg"></param>
    public void AddMSGToProtokoll(string msg)
    {
        GameObject go = Instantiate(SpielprotokollContent.transform.GetChild(0).gameObject, SpielprotokollContent.transform.GetChild(0).position, SpielprotokollContent.transform.GetChild(0).rotation);
        go.name = "MSG_" + SpielprotokollContent.transform.childCount;
        go.transform.SetParent(SpielprotokollContent.transform);
        go.transform.GetComponentInChildren<TMP_Text>().text = msg;
        go.transform.localScale = new Vector3(1, 1, 1);
        go.SetActive(true);
    }
    /// <summary>
    /// Blendet eine Nachricht im InfoBoard ein
    /// </summary>
    /// <param name="msg"></param>
    public void DisplayMSGInfoBoard(string msg)
    {
        InfoBoard.GetComponentInChildren<TMP_Text>().text = msg;
    }
    /// <summary>
    /// Startet einen neuen Zug eines Spielers
    /// </summary>
    public void StartTurn()
    {
        MenschAergerDichNichtPlayer player = board.PlayerTurnSelect();
        Logging.log(Logging.LogType.Debug, "MenschAergerDichNichtServer", "StartTurn", "Der Spieler " + player.name + " ist dran.");
        if (player.GetAllInStartOrHome())
            player.availableDices = 3;
        else
            player.availableDices = 1;
        AddMSGToProtokoll(board.TEAM_COLORS[player.gamerid] + player.name + "</color></b> ist dran.");
        StartTurnSelectType(player);

        BroadcastNew("#SpielerTurn " + player.name);
    }
    /// <summary>
    /// Startet den Zug eines Spielers, (neu oder bei 6 oder alle im Start...)
    /// </summary>
    /// <param name="player"></param>
    bool TurnSelectbelegt = false;
    private void StartTurnSelectType(MenschAergerDichNichtPlayer player)
    {
        if (TurnSelectbelegt)
            return;
        TurnSelectbelegt = true;
        Logging.log(Logging.LogType.Debug, "MenschAergerDichNichtServer", "StartTurnSelectType", "Der Spieler " + player.name + " darf ziehen.");
        DisplayMSGInfoBoard(player.name + " ist dran!");
        // Spieler
        if (!player.isBot)
        {
            // Server
            if (player.PlayerImage == Config.SERVER_ICON)
            {
                StartTurnServer();
                TurnSelectbelegt = false;
                return;
            }
            // Client
            else
            {
                foreach (Player p in Config.PLAYERLIST)
                {
                    if (p.icon == player.PlayerImage && p.name == player.name)
                    {
                        StartTurnClient(p);
                        TurnSelectbelegt = false;
                        return;
                    }
                }
            }
        }
        // Bot
        else
        {
            StartTurnBot();
            TurnSelectbelegt = false;
            return;
        }
        TurnSelectbelegt = false;
    }
    /// <summary>
    /// Der Server ist dran
    /// </summary>
    private void StartTurnServer()
    {
        Logging.log(Logging.LogType.Debug, "MenschAergerDichNichtServer", "StartTurnServer", "Der Server ist dran und kann w�rfeln");
        DisplayMSGInfoBoard("Du bist dran!\n Du kannst w�rfeln.");

        WuerfelAktivieren(true);
    }
    /// <summary>
    /// Client wird freigeschaltet
    /// </summary>
    /// <param name="p"></param>
    private void StartTurnClient(Player p)
    {
        Logging.log(Logging.LogType.Debug, "MenschAergerDichNichtServer", "StartTurnClient", "Der Client " + p.name + " ist dran und darf nun w�rfeln und sich bewegen.");
        clientCanPickField = true;
    }
    /// <summary>
    /// Bot Zug wird gestartet
    /// </summary>
    private void StartTurnBot()
    {
        if (BotCoroutine != null)
            StopCoroutine(BotCoroutine);
        BotCoroutine = StartCoroutine(StartBotWuerfelVerzoegert());
    }
    /// <summary>
    /// L�sst den Bot laufen
    /// </summary>
    public void LaufenTurnBot()
    {
        Logging.log(Logging.LogType.Debug, "MenschAergerDichNichtServer", "LaufenTurnBot", "Der Bot w�hlt nun ein Feld zum Laufen");
        List<MenschAegerDichNichtFeld> felder = board.GetPlayerTurn().GetAvailableMoves();
        // kann nicht laufen
        if (felder.Count == 0)
        {
            return;
        }
        // kann laufen
        else
        {
            int random = UnityEngine.Random.Range(0, felder.Count);
            SpielerW�hltFeld(felder[random].GetFeld());
        }
        return;
    }
    /// <summary>
    /// Startet das W�rfeln des Bots verz�gert
    /// </summary>
    /// <returns></returns>
    IEnumerator StartBotWuerfelVerzoegert()
    {
        yield return new WaitForSeconds(3);
        WuerfelStarteAnimation();
        yield break;
    }
    /// <summary>
    /// Aktiviert den W�rfeln Button
    /// </summary>
    /// <param name="aktivieren"></param>
    private void WuerfelAktivieren(bool aktivieren)
    {
        W�rfel.transform.GetChild(0).gameObject.SetActive(aktivieren);
    }
    /// <summary>
    /// Ein Client w�rfelt. Wird verarbeitet und gesendet
    /// </summary>
    /// <param name="data"></param>
    private void Wuerfel(string data)
    {
        board.GetPlayerTurn().availableDices--;
        int result = Int32.Parse(data);
        if (result == 6)
            board.GetPlayerTurn().availableDices = 1;
        Logging.log(Logging.LogType.Debug, "MenschAergerDichNichtServer", "Wuerfel", "Client w�rfelt. Result: " + result);
        BroadcastNew("#Wuerfel " + result + "*" + board.GetPlayerTurn().name);

        if (WuerfelCoroutine != null)
            StopCoroutine(WuerfelCoroutine);
        WuerfelCoroutine = StartCoroutine(WuerfelAnimation(result));
    }
    /// <summary>
    /// Startet die W�rfelanimation
    /// </summary>
    public void WuerfelStarteAnimation()
    {
        if (!Config.isServer)
            return;

        WuerfelAktivieren(false);
        board.GetPlayerTurn().availableDices--;
        int result = UnityEngine.Random.Range(1, 7);
        if (board.GetPlayerTurn().GetAllInStartOrHome())
            result = 6; // TODO nur zum testen
        if (result == 6)
            board.GetPlayerTurn().availableDices = 1;
        Logging.log(Logging.LogType.Debug, "MenschAergerDichNichtServer", "WuerfelStarteAnimation", "Es wird gew�rfelt. Result: " + result);
        BroadcastNew("#Wuerfel " + result + "*" + board.GetPlayerTurn().name);

        if (WuerfelCoroutine != null)
            StopCoroutine(WuerfelCoroutine);
        WuerfelCoroutine = StartCoroutine(WuerfelAnimation(result));
    }
    /// <summary>
    /// W�rfelanimation
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    IEnumerator WuerfelAnimation(int result)
    {
        int count = 0;
        List<Sprite> wuerfel = new List<Sprite>();
        for (int i = 1; i <= 6; i++)
        {
            wuerfel.Add(Resources.Load<Sprite>("Images/GUI/w�rfel "+i));
        }
        // Roll Time
        DateTime swtichTime = DateTime.Now.AddSeconds(2);
        while (DateTime.Compare(DateTime.Now, swtichTime) < 0)
        {
            W�rfel.GetComponent<Image>().sprite = wuerfel[(count++) % wuerfel.Count];
            //Debug.LogWarning(0.005f * count);
            yield return new WaitForSeconds(0.005f*count);
        }
        // Roll to 6
        while (wuerfel[wuerfel.Count-1] == wuerfel[count % wuerfel.Count])
        {
            W�rfel.GetComponent<Image>().sprite = wuerfel[(count++) % wuerfel.Count];
            yield return new WaitForSeconds(0.005f * count);
        }
        // Roll Until selected
        while (!wuerfel[count % wuerfel.Count].name.Equals("w�rfel " + result))
        {
            W�rfel.GetComponent<Image>().sprite = wuerfel[(count++) % wuerfel.Count];
            yield return new WaitForSeconds(0.005f * count);
        }
        W�rfel.GetComponent<Image>().sprite = wuerfel[(count) % wuerfel.Count];
        yield return null;
        StartCoroutine(ParseWuerfelResult(result));
        yield break;
    }
    /// <summary>
    /// Nutzt das W�rfelergebnis.
    /// - Spieler muss nochmal w�rfeln, (am start)
    /// - Spieler darf laufen und dann zug vorbei
    /// - Spieler darf laufen und nochmal w�rfeln
    /// - w�rfel zuende
    /// - Sieg pr�fen
    /// </summary>
    /// <returns></returns>
    IEnumerator ParseWuerfelResult(int result)
    {
        Logging.log(Logging.LogType.Debug, "MenschAergerDichNicht", "ParseWuerfelResult", "Ergebnis wird verarbeitet.");
        AddMSGToProtokoll(board.TEAM_COLORS[board.GetPlayerTurn().gamerid] + board.GetPlayerTurn().name + "</color></b> w�rfelt " + W�rfel.GetComponent<Image>().sprite.name.Replace("w�rfel ", ""));
        board.GetPlayerTurn().MarkAvailableMoves(result);

        // Spieler ist ein bot
        if (board.GetPlayerTurn().isBot)
        {
            yield return new WaitForSeconds(2);
            LaufenTurnBot(); // kann laufen
            board.ClearMarkierungen();
            // Schaut ob das Spiel zuende ist
            if (board.GetPlayerTurn().HasPlayerWon()) // Spieler hat gewonnen
            {
                AddMSGToProtokoll(board.TEAM_COLORS[board.GetPlayerTurn().gamerid] + board.GetPlayerTurn().name + "</color></b> hat gewonnen!");
                yield break;
            }
            // Schaut ob der Zug des Spielers beendet ist
            if (CheckForEndOfTurn())
            {
                board.GetPlayerTurn().wuerfel = 0;
                StartTurn(); // Starte neuen Zug
            }
            // Zug noch nicht vorbei
            else
            {
                StartTurnSelectType(board.GetPlayerTurn());
            }
            yield break;
        }
        // Spieler ist ein Spieler
        else
        {
            // Ist Server
            if (Config.SERVER_ICON == board.GetPlayerTurn().PlayerImage)
            {
                // Spieler kann nicht laufen
                if (board.GetPlayerTurn().GetAvailableMoves().Count == 0)
                {
                    board.ClearMarkierungen();
                    yield return new WaitForSeconds(0.5f);
                    // Schaut ob der Zug des Spielers beendet ist
                    if (CheckForEndOfTurn())
                    {
                        board.GetPlayerTurn().wuerfel = 0;
                        StartTurn(); // Starte neuen Zug
                    }
                    // Zug noch nicht vorbei
                    else
                    {
                        StartTurnSelectType(board.GetPlayerTurn());
                    }
                    yield break;
                }
                StartCoroutine(BlockZugWahlFuer2Sek());
            }
            // Ist Client
            else 
            {        
                // Spieler kann nicht laufen
                if (board.GetPlayerTurn().GetAvailableMoves().Count == 0)
                {
                    board.ClearMarkierungen();
                    // Schaut ob der Zug des Spielers beendet ist
                    if (CheckForEndOfTurn())
                    {
                        board.GetPlayerTurn().wuerfel = 0;
                        StartTurn(); // Starte neuen Zug
                    }
                    // Zug noch nicht vorbei
                    else
                    {
                        StartTurnSelectType(board.GetPlayerTurn());
                    }
                    yield break;
                }

                clientCanPickField = true;
            }
        }
        yield break;
    }
    /// <summary>
    /// Sperrt die Zugwahl f�r X Sekunden, damit es nicht zu Fehlern bei Clients kommt
    /// </summary>
    /// <returns></returns>
    IEnumerator BlockZugWahlFuer2Sek()
    {
        ServerAllowZugWahl = false;
        yield return new WaitForSeconds(2);
        ServerAllowZugWahl = true;
        yield break;
    }
    /// <summary>
    /// der Server w�hlt ein Feld zum Laufen
    /// </summary>
    /// <param name="FeldName"></param>
    public void ServerW�hltFeld(GameObject FeldName)
    {
        if (!Config.isServer)
            return;
        if (!ServerAllowZugWahl)
            return;
        // Server ist aktuell nicht dran
        if (board.GetPlayerTurn().isBot || Config.SERVER_ICON != board.GetPlayerTurn().PlayerImage)
            return;
        Logging.log(Logging.LogType.Debug, "MenschAergerDichNicht", "ServerW�hltFeld", "Server w�hlt: " + FeldName.name);
        
        SpielerW�hltFeld(FeldName);
    }
    /// <summary>
    /// Der Client w�hlt ein Feld zum Laufen
    /// </summary>
    /// <param name="p"></param>
    /// <param name="data"></param>
    private void ClientW�hltFeld(Player p, string data)
    {
        // Doppeltes tippen verhindern
        if (clientCanPickField == false)
            return;
        // Pr�ft ob der Sendende Spieler auch dran ist
        if (board.GetPlayerTurn().isBot || p.icon != board.GetPlayerTurn().PlayerImage || p.name != board.GetPlayerTurn().name)
            return;
        Logging.log(Logging.LogType.Debug, "MenschAergerDichNicht", "ServerW�hltFeld", "Client " + p.name + " w�hlt: " + data);

        clientCanPickField = false;

        foreach (MenschAegerDichNichtFeld feld in board.GetPlayerTurn().GetAvailableMoves())
        {
            if (feld.GetFeld().name == data)
            {
                SpielerW�hltFeld(feld.GetFeld());
                return;
            }
        }
    }
    /// <summary>
    /// F�gt die Farben der Spieler ein
    /// [C]1[C]Henryk[/COLOR] nachricht [C]2[C]Ron[/COLOR]
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    private string GenerateColorsIntoMultipleNames(string message)
    {
        message = message.Replace("[/COLOR]", "</color></b>");
        int gamerid = Int32.Parse(message.Replace("[C]", "|").Split('|')[1]);
        message = message.Replace("[C]" + gamerid + "[C]", board.TEAM_COLORS[gamerid]);
        gamerid = Int32.Parse(message.Replace("[C]", "|").Split('|')[1]);
        message = message.Replace("[C]" + gamerid + "[C]", board.TEAM_COLORS[gamerid]);

        return message;
    }
    /// <summary>
    /// Verarbeitet die Feldwahl
    /// </summary>
    /// <param name="FeldName"></param>
    private void SpielerW�hltFeld(GameObject FeldName)
    {
        BroadcastNew("#SpielerWaehltFeld " + FeldName.name);
        // Wenn der Server dran ist, schauen ob das Feld markiert ist
        foreach (MenschAegerDichNichtFeld feld in board.GetPlayerTurn().GetAvailableMoves())
        {
            // Das gew�hlte Feld
            if (feld.GetFeld().Equals(FeldName))
            {
                string ausgabe = board.GetPlayerTurn().Move(feld);
                board.ClearMarkierungen();

                if (ausgabe.Length > 2)
                    AddMSGToProtokoll(GenerateColorsIntoMultipleNames(ausgabe));

                // Schaut ob das Spiel zuende ist
                if (board.GetPlayerTurn().HasPlayerWon()) // Spieler hat gewonnen
                {
                    AddMSGToProtokoll(board.TEAM_COLORS[board.GetPlayerTurn().gamerid] + board.GetPlayerTurn().name + "</color></b> hat gewonnen!");
                    return;
                }

                // Schaut ob der Zug des Spielers beendet ist
                if (CheckForEndOfTurn())
                {
                    board.GetPlayerTurn().wuerfel = 0;
                    StartTurn(); // Starte neuen Zug
                }
                // Zug noch nicht vorbei
                else
                {
                    StartTurnSelectType(board.GetPlayerTurn());
                }
                return;
            }
        }
        return;
    }
    /// <summary>
    /// Wenn ein Spieler verl�sst, soll dieser automatisch zu einem Bot werden
    /// </summary>
    private void SpielerWirdZumBot(Player p)
    {
        BroadcastNew("#PlayerMergesBot " + p.name);
        // Der Spieler ist gerade dran
        if (board.GetPlayerTurn().name == p.name && board.GetPlayerTurn().PlayerImage == p.icon)
        {
            AddMSGToProtokoll(board.TEAM_COLORS[board.GetPlayerTurn().gamerid] + p.name + "</color></b> hat das Spiel verlassen.");
            AddMSGToProtokoll(board.TEAM_COLORS[board.GetPlayerTurn().gamerid] + p.name + "</color></b> wird nun von einem <b>Bot</b> �bernommen!");
            board.GetPlayerTurn().SetPlayerIntoBot();
            // Spieler hat bereits gew�rfelt
            if (board.GetPlayerTurn().GetAvailableMoves().Count > 0)
            {
                // verz�gern um 2 sek
                StartCoroutine(BotLaufenVerzoergert());
            }
            // Spieler muss noch w�rfeln
            else
            {
                if (BotCoroutine != null)
                    StopCoroutine(BotCoroutine);
                BotCoroutine = StartCoroutine(StartBotWuerfelVerzoegert());
            }
        }
        // Der Spieler ist nicht dran
        else
        {
            foreach (MenschAergerDichNichtPlayer player in board.GetPlayerList())
            {
                if (player.name == p.name && player.PlayerImage == p.icon)
                {
                    AddMSGToProtokoll(board.TEAM_COLORS[board.GetPlayerTurn().gamerid] + p.name + "</color></b> hat das Spiel verlassen.");
                    AddMSGToProtokoll(board.TEAM_COLORS[board.GetPlayerTurn().gamerid] + p.name + "</color></b> wird nun von einem <b>Bot</b> �bernommen!");
                    player.SetPlayerIntoBot();
                    break;
                }
            }
        }
    }
    /// <summary>
    /// Nachdem ein Bot einen Spieler �bernommen hat, startet dieser nach 2 Sekunden
    /// </summary>
    /// <returns></returns>
    IEnumerator BotLaufenVerzoergert()
    {
        yield return new WaitForSeconds(2);
        LaufenTurnBot(); // kann laufen
        board.ClearMarkierungen();
        // Schaut ob das Spiel zuende ist
        if (board.GetPlayerTurn().HasPlayerWon()) // Spieler hat gewonnen
        {
            AddMSGToProtokoll(board.TEAM_COLORS[board.GetPlayerTurn().gamerid] + board.GetPlayerTurn().name + "</color></b> hat gewonnen!");
            yield break;
        }
        // Schaut ob der Zug des Spielers beendet ist
        if (CheckForEndOfTurn())
        {
            board.GetPlayerTurn().wuerfel = 0;
            StartTurn(); // Starte neuen Zug
        }
        // Zug noch nicht vorbei
        else
        {
            StartTurnSelectType(board.GetPlayerTurn());
        }
        yield break;
    }
    /// <summary>
    /// Pr�ft ob der Zug des Spielers beendet ist
    /// </summary>
    /// <returns></returns>
    private bool CheckForEndOfTurn()
    {
        if (board.GetPlayerTurn().availableDices <= 0)
            return true;
        else
            return false;
    }

    #endregion
}
