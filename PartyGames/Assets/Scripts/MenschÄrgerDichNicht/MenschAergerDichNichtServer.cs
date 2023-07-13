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
    private GameObject Timer;
    private DateTime RoundStart;
    private int protokollmsgs;
    [SerializeField] GameObject Würfel;
    private Coroutine WuerfelCoroutine;
    private Coroutine BotCoroutine;
    private Coroutine StartTurnDelayedCoroutine;
    [SerializeField] GameObject InfoBoard;
    bool[] PlayerConnected;

    [SerializeField] AudioSource DisconnectSound;
    [SerializeField] AudioSource SpielerZieht;
    [SerializeField] AudioSource SpielerIstDran;
    [SerializeField] AudioSource SpielerWirdGeschlagen;
    [SerializeField] AudioSource SiegerStehtFest;

    MenschAegerDichNichtBoard board;
    private bool gameIsRunning;
    private bool clientCanPickField;
    private bool ServerAllowZugWahl;
    private bool BotWillReplaceServer;

    //private List<string> broadcastmsgs;

    void OnEnable()
    {
        StartCoroutine(ServerUtils.Broadcast());
        //broadcastmsgs = new List<string>();
        //StartCoroutine(NewBroadcast());
        PlayerConnected = new bool[Config.SERVER_MAX_CONNECTIONS];
        Lobby.SetActive(true);
        Games.SetActive(false);
        InitLobby();
    }

    void Update()
    {
        #region Server
        if (!Config.SERVER_STARTED)
        {
            SceneManager.LoadSceneAsync("Startup");
            return;
        }

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
        }
        #endregion
    }

    private void OnApplicationQuit()
    {
        ServerUtils.BroadcastImmediate(Config.GLOBAL_TITLE + "#ServerClosed");
        Logging.log(Logging.LogType.Normal, "MenschÄrgerDichNichtServer", "OnApplicationQuit", "Server wird geschlossen.");
        Config.SERVER_TCP.Server.Close();
    }

    #region Server Stuff  
    #region Kommunikation
    /// <summary>
    /// Einkommende Nachrichten die von Spielern an den Server gesendet werden
    /// </summary>
    /// <param name="spieler">Spieler</param>
    /// <param name="data">Nachricht</param>
    private void OnIncommingData(Player spieler, string data)
    {
        if (!data.StartsWith(Config.GAME_TITLE) && !data.StartsWith(Config.GLOBAL_TITLE))
        {
            Logging.log(Logging.LogType.Warning, "", "OnIncommingData", "Wrong Prefix: " + data);
            return;
        }
        data = Utils.ParseCMDGameTitle(data, Config.isServer);

        string cmd;
        if (data.Contains(" "))
        {
            cmd = data.Split(' ')[0];
            data = data.Substring(cmd.Length + 1);
        }
        else
            cmd = data;

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
        Logging.log(Logging.LogType.Debug, "MenschÄrgerDichNichtServer", "Commands", "Eingehende Nachricht: " + player.name + " " + player.id + " -> " + cmd + "   ---   " + data);
        // Sucht nach Command
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "MenschÄrgerDichNichtServer", "Commands", "Unkown Command: " + cmd + " -> " + data);
                break;

            case "#ClientClosed":
                if (Lobby.activeInHierarchy)
                    UpdateLobby();
                if (Games.activeInHierarchy)
                    SpielerWirdZumBot(player);
                PlayDisconnectSound();
                ServerUtils.ClientClosed(player);
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
                ClientWähltFeld(player, data);
                break;
        }
    }
    #endregion
    /// <summary>
    /// Spiel Verlassen & Zurück in die Lobby laden
    /// </summary>
    public void SpielVerlassenButton()
    {
        Logging.log(Logging.LogType.Debug, "MenschÄrgerDichNichtServer", "SpielVerlassenButton", "Spiel wird beendet. Lädt ins Hauptmenü.");
        //SceneManager.LoadScene("Startup");
        ServerUtils.AddBroadcast("#ZurueckInsHauptmenue");
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
    /// Aktualisiert die LobbyAnzeige für alle
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
        ServerUtils.AddBroadcast("#UpdateLobby " + msg);
    }
    /// <summary>
    /// Ändert die Anzahl der Bots
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
    /// Startet das MenschÄrgerDichNicht Spiel
    /// </summary>
    public void StartGame()
    {
        Logging.log(Logging.LogType.Normal, "MenschAergerDichNichtServer", "StartGame", "Spiel wird gestartet.");
        Würfel.transform.GetChild(0).gameObject.SetActive(false);
        if (MenschAegerDichNichtBoard.watchBots)
        {
            Logging.log(Logging.LogType.Normal, "MenschAergerDichNichtServer", "StartGame", "Es werden nur Bots spielen, alle Clients werden getrennt.");
            ServerUtils.AddBroadcast("#ServerClosed");
            for (int i = 0; i < Config.PLAYERLIST.Length; i++)
                ServerUtils.ClientClosed(Config.PLAYERLIST[i]);
            UpdateLobby();
        }
        gameIsRunning = true;
        RoundStart = DateTime.Now;
        protokollmsgs = 0;
        Timer = SpielprotokollContent.transform.parent.parent.parent.GetChild(0).gameObject;
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
                        sprites.Add(Config.SERVER_PLAYER.icon);
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
        Logging.log(Logging.LogType.Debug, "MenschAergerDichNichtServer", "StartGame", "Folgende Map wurde gewählt: " + mapInt + "P");
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
            ServerUtils.AddBroadcast("#StartGame [PLAYER]" + broadcastPlayer + "[PLAYER][MAP]" + selectedMap.name + "[MAP][TEAMSIZE]" + mapInt + "[TEAMSIZE][RUNWAY]" + RunWaySize + "[RUNWAY]");
            board = new MenschAegerDichNichtBoard(selectedMap, RunWaySize, mapInt, randomplayer);
        }
        else
        {
            Logging.log(Logging.LogType.Warning, "MenschAergerDichNichtServer", "StartGame", "Die Spieleranzahl stimmt nicht mit der gewählten Map überein.");
            Lobby.SetActive(true);
            Games.SetActive(false);
            foreach (GameObject go in Maps)
                go.SetActive(false);
            return;
        }
        clientCanPickField = false;

        //Debug.LogWarning(board.GetBoardString());
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
        AddMSGToProtokoll("...");

        StartCoroutine(RunVisibleTimer());

        StartTurn();
    }
    /// <summary>
    /// Zeigt die RundenTimer an
    /// </summary>
    /// <returns></returns>
    IEnumerator RunVisibleTimer()
    {
        while (gameIsRunning)
        {
            yield return new WaitForSeconds(1f);
            Timer.GetComponent<TMP_Text>().text = GetTimerDiff(RoundStart);
        }
        yield break;
    }
    /// <summary>
    /// Gibt die Differenz der Zeit zurück
    /// </summary>
    /// <param name="starttime"></param>
    /// <returns></returns>
    private string GetTimerDiff(DateTime starttime)
    {
        int hour = (DateTime.Now.Day - starttime.Day) * 24 + DateTime.Now.Hour - starttime.Hour;
        int min = hour * 60 + DateTime.Now.Minute - starttime.Minute;
        int sec = min*60 + DateTime.Now.Second - starttime.Second;

        int minutes = (int)(sec / 60);
        int seconds = sec - (60 * minutes);

        if (seconds < 10)
            return minutes + ":0" + seconds;
        else
            return minutes + ":" + seconds;
    }
    /// <summary>
    /// Fügt eine Nachricht dem Spielprotokoll hinzu
    /// </summary>
    /// <param name="msg"></param>
    public void AddMSGToProtokoll(string msg)
    {
        GameObject go = Instantiate(SpielprotokollContent.transform.GetChild(0).gameObject, SpielprotokollContent.transform.GetChild(0).position, SpielprotokollContent.transform.GetChild(0).rotation);
        go.name = "MSG_" + protokollmsgs++;
        go.transform.SetParent(SpielprotokollContent.transform);
        go.transform.GetComponentInChildren<TMP_Text>().text = msg;
        go.transform.localScale = new Vector3(1, 1, 1);
        go.SetActive(true);

        // Limitiert die Anzahl der Protokoll Nachrichten
        if (SpielprotokollContent.transform.childCount > 100)
        {
            Destroy(SpielprotokollContent.transform.GetChild(3).gameObject);
        }
        ServerUtils.AddBroadcast("#AddProtokoll " + msg);
    }
    /// <summary>
    /// Blendet eine Nachricht im InfoBoard ein
    /// </summary>
    /// <param name="msg"></param>
    public void DisplayMSGInfoBoard(string msg)
    {
        InfoBoard.GetComponentInChildren<TMP_Text>().text = msg;
    }
    private void SendBoardUpdate()
    {
        ServerUtils.AddBroadcast("#UpdateBoard " + board.GetBoardString());
        //ServerUtils.AddBroadcast("#UpdateBoard " + board.GetBoardString());
    }
    public void BotReplaceServer(Toggle toggle)
    {
        BotWillReplaceServer = toggle.isOn;
    }
    public void ResendIstDran()
    {
        board.ClearMarkierungen();
        MenschAergerDichNichtPlayer player = board.GetPlayerTurn();
        if (player.GetAllInStartOrHome())
            player.availableDices = 3;
        else
            player.availableDices = 1;
        AddMSGToProtokoll(board.TEAM_COLORS[player.gamerid] + player.name + "</color></b> ist dran.");
        DisplayMSGInfoBoard(player.name + " ist dran!");
        StartTurnSelectType(player);

        ServerUtils.AddBroadcast("#StartTurn " + player.gamerid + "*" + player.availableDices);
        if (player.name == Config.PLAYER_NAME)
            SpielerIstDran.Play();
    }
    /// <summary>
    /// Startet einen neuen Zug eines Spielers
    /// </summary>
    public void StartTurn()
    {
        if (StartTurnDelayedCoroutine != null)
            StopCoroutine(StartTurnDelayedCoroutine);
        StartTurnDelayedCoroutine = StartCoroutine(StartTurnDelayed());
    }
    IEnumerator StartTurnDelayed()
    {
        if (MenschAegerDichNichtBoard.watchBots)
            yield return new WaitForSeconds(0.1f);
        else
            yield return new WaitForSeconds(1f);

        MenschAergerDichNichtPlayer player = board.PlayerTurnSelect();
        Logging.log(Logging.LogType.Debug, "MenschAergerDichNichtServer", "StartTurn", "Der Spieler " + player.name + " ist dran.");
        if (player.GetAllInStartOrHome())
            player.availableDices = 3;
        else
            player.availableDices = 1;
        AddMSGToProtokoll(board.TEAM_COLORS[player.gamerid] + player.name + "</color></b> ist dran.");
        DisplayMSGInfoBoard(player.name + " ist dran!");
        StartTurnSelectType(player);
        if (player.name == Config.PLAYER_NAME)
            SpielerIstDran.Play();

        ServerUtils.AddBroadcast("#StartTurn " + player.gamerid + "*" + player.availableDices);
        yield break;
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
        // Spieler
        if (!player.isBot)
        {
            // Server
            if (player.PlayerImage == Config.SERVER_PLAYER.icon)
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
        Logging.log(Logging.LogType.Debug, "MenschAergerDichNichtServer", "StartTurnServer", "Der Server ist dran und kann würfeln");
        DisplayMSGInfoBoard("Du bist dran!\n Du kannst würfeln.");

        //WuerfelAktivieren(true);
        StartCoroutine(WuerfelAktuivierenTime(1));

        if (BotWillReplaceServer)
        {
            WuerfelAktivieren(false);
            if (BotCoroutine != null)
                StopCoroutine(BotCoroutine);
            BotCoroutine = StartCoroutine(StartBotWuerfelVerzoegert());
        }
    }
    /// <summary>
    /// Client wird freigeschaltet
    /// </summary>
    /// <param name="p"></param>
    private void StartTurnClient(Player p)
    {
        Logging.log(Logging.LogType.Debug, "MenschAergerDichNichtServer", "StartTurnClient", "Der Client " + p.name + " ist dran und darf nun würfeln und sich bewegen.");
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
    /// Lässt den Bot laufen
    /// </summary>
    public void LaufenTurnBot()
    {
        Logging.log(Logging.LogType.Debug, "MenschAergerDichNichtServer", "LaufenTurnBot", "Der Bot wählt nun ein Feld zum Laufen");
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
            SpielerWähltFeld(felder[random].GetFeld());
        }
        return;
    }
    /// <summary>
    /// Startet das Würfeln des Bots verzögert
    /// </summary>
    /// <returns></returns>
    IEnumerator StartBotWuerfelVerzoegert()
    {
        if (MenschAegerDichNichtBoard.watchBots)
            yield return new WaitForSeconds(0.1f);
        else 
            yield return new WaitForSeconds(1f);

        WuerfelStarteAnimation();
        yield break;
    }
    /// <summary>
    /// Aktiviert den Würfeln Button
    /// </summary>
    /// <param name="aktivieren"></param>
    private void WuerfelAktivieren(bool aktivieren)
    {
        Würfel.transform.GetChild(0).gameObject.SetActive(aktivieren);
    }
    private IEnumerator WuerfelAktuivierenTime(int sec)
    {
        yield return new WaitForSeconds(sec);
        WuerfelAktivieren(true);
        yield break;
    }
    /// <summary>
    /// Ein Client würfelt. Wird verarbeitet und gesendet
    /// </summary>
    /// <param name="data"></param>
    private void Wuerfel(string data)
    {
        board.GetPlayerTurn().availableDices--;
        int result = Int32.Parse(data);
        if (result == 6)
            board.GetPlayerTurn().availableDices = 1;
        Logging.log(Logging.LogType.Debug, "MenschAergerDichNichtServer", "Wuerfel", "Client würfelt. Result: " + result);
        ServerUtils.AddBroadcast("#Wuerfel " + result + "*" + board.GetPlayerTurn().name);

        if (WuerfelCoroutine != null)
            StopCoroutine(WuerfelCoroutine);
        WuerfelCoroutine = StartCoroutine(WuerfelAnimation(result));
    }
    /// <summary>
    /// Startet die Würfelanimation
    /// </summary>
    public void WuerfelStarteAnimation()
    {
        if (!Config.isServer)
            return;

        WuerfelAktivieren(false);
        board.GetPlayerTurn().availableDices--;
        int result = UnityEngine.Random.Range(1, 7);
        if (result == 6)
            board.GetPlayerTurn().availableDices = 1;
        board.GetPlayerTurn().wuerfelCounter++;
        Logging.log(Logging.LogType.Debug, "MenschAergerDichNichtServer", "WuerfelStarteAnimation", "Es wird gewürfelt. Result: " + result);
        ServerUtils.AddBroadcast("#Wuerfel " + result + "*" + board.GetPlayerTurn().name);

        if (WuerfelCoroutine != null)
            StopCoroutine(WuerfelCoroutine);
        WuerfelCoroutine = StartCoroutine(WuerfelAnimation(result));
    }
    /// <summary>
    /// Würfelanimation
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    IEnumerator WuerfelAnimation(int result)
    {
        int count = 0;
        List<Sprite> wuerfel = new List<Sprite>();
        for (int i = 1; i <= 6; i++)
        {
            wuerfel.Add(Resources.Load<Sprite>("Images/GUI/würfel "+i));
        }
        // Roll to 6
        Würfel.GetComponent<Image>().sprite = wuerfel[(count++)];
        yield return new WaitForSeconds(0.05f);
        Würfel.GetComponent<Image>().sprite = wuerfel[(count++)];
        yield return new WaitForSeconds(0.05f);
        while (wuerfel[wuerfel.Count - 1] == wuerfel[count % wuerfel.Count])
        {
            Würfel.GetComponent<Image>().sprite = wuerfel[(count++) % wuerfel.Count];
            yield return new WaitForSeconds(0.05f);
        }
        // Roll to 6
        while (wuerfel[wuerfel.Count-1] == wuerfel[count % wuerfel.Count])
        {
            Würfel.GetComponent<Image>().sprite = wuerfel[(count++) % wuerfel.Count];
            yield return new WaitForSeconds(0.05f);
        }
        // Roll to 6
        while (wuerfel[wuerfel.Count - 1] == wuerfel[count % wuerfel.Count])
        {
            Würfel.GetComponent<Image>().sprite = wuerfel[(count++) % wuerfel.Count];
            yield return new WaitForSeconds(0.05f);
        }
        // Roll to 6
        while (wuerfel[wuerfel.Count - 1] == wuerfel[count % wuerfel.Count])
        {
            Würfel.GetComponent<Image>().sprite = wuerfel[(count++) % wuerfel.Count];
            yield return new WaitForSeconds(0.05f);
        }
        // Roll Until selected
        while (!wuerfel[count % wuerfel.Count].name.Equals("würfel " + result))
        {
            Würfel.GetComponent<Image>().sprite = wuerfel[(count++) % wuerfel.Count];
            yield return new WaitForSeconds(0.05f);
        }
        Würfel.GetComponent<Image>().sprite = wuerfel[(count) % wuerfel.Count];
        yield return null;
        StartCoroutine(ParseWuerfelResult(result));
        yield break;
    }
    /// <summary>
    /// Nutzt das Würfelergebnis.
    /// - Spieler muss nochmal würfeln, (am start)
    /// - Spieler darf laufen und dann zug vorbei
    /// - Spieler darf laufen und nochmal würfeln
    /// - würfel zuende
    /// - Sieg prüfen
    /// </summary>
    /// <returns></returns>
    IEnumerator ParseWuerfelResult(int result)
    {
        Logging.log(Logging.LogType.Debug, "MenschAergerDichNicht", "ParseWuerfelResult", "Ergebnis wird verarbeitet.");
        AddMSGToProtokoll(board.TEAM_COLORS[board.GetPlayerTurn().gamerid] + board.GetPlayerTurn().name + "</color></b> würfelt " + Würfel.GetComponent<Image>().sprite.name.Replace("würfel ", ""));
        board.GetPlayerTurn().MarkAvailableMoves(result);

        ServerUtils.AddBroadcast("#MarkMarkierungen " + board.PrintMarkierungen());

        // Spieler ist ein bot
        if (board.GetPlayerTurn().isBot)
        {
            if (MenschAegerDichNichtBoard.watchBots)
                yield return new WaitForSeconds(0.01f);
            else
                yield return new WaitForSeconds(1f);
            LaufenTurnBot(); // kann laufen
            board.ClearMarkierungen();
            if (board.GetPlayerTurn().HasPlayerWon()) // Spieler hat gewonnen
            {
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
            if (Config.SERVER_PLAYER.icon == board.GetPlayerTurn().PlayerImage)
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

                if (BotWillReplaceServer)
                {
                    yield return new WaitForSeconds(2f);
                    LaufenTurnBot(); // kann laufen
                    board.ClearMarkierungen();
                    if (board.GetPlayerTurn().HasPlayerWon()) // Spieler hat gewonnen
                    {
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
                }
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
                        ServerUtils.AddBroadcast("#AktiviereWuerfel " + board.GetPlayerTurn().name);
                    }
                    yield break;
                }

                clientCanPickField = true;
            }
        }
        yield break;
    }
    /// <summary>
    /// Sperrt die Zugwahl für X Sekunden, damit es nicht zu Fehlern bei Clients kommt
    /// </summary>
    /// <returns></returns>
    IEnumerator BlockZugWahlFuer2Sek()
    {
        ServerAllowZugWahl = false;
        yield return new WaitForSeconds(0.5f);
        ServerAllowZugWahl = true;
        yield break;
    }
    /// <summary>
    /// der Server wählt ein Feld zum Laufen
    /// </summary>
    /// <param name="FeldName"></param>
    public void ServerWähltFeld(GameObject FeldName)
    {
        if (!Config.isServer)
            return;
        if (!ServerAllowZugWahl)
            return;
        // Server ist aktuell nicht dran
        if (board.GetPlayerTurn().isBot || Config.SERVER_PLAYER.icon != board.GetPlayerTurn().PlayerImage)
            return;
        Logging.log(Logging.LogType.Debug, "MenschAergerDichNicht", "ServerWähltFeld", "Server wählt: " + FeldName.name);
        
        SpielerWähltFeld(FeldName);
    }
    /// <summary>
    /// Der Client wählt ein Feld zum Laufen
    /// </summary>
    /// <param name="p"></param>
    /// <param name="data"></param>
    private void ClientWähltFeld(Player p, string data)
    {
        // Doppeltes tippen verhindern
        if (clientCanPickField == false)
            return;
        // Prüft ob der Sendende Spieler auch dran ist
        if (board.GetPlayerTurn().isBot || p.icon != board.GetPlayerTurn().PlayerImage || p.name != board.GetPlayerTurn().name)
            return;
        Logging.log(Logging.LogType.Debug, "MenschAergerDichNicht", "ServerWähltFeld", "Client " + p.name + " wählt: " + data);

        clientCanPickField = false;

        foreach (MenschAegerDichNichtFeld feld in board.GetPlayerTurn().GetAvailableMoves())
        {
            if (feld.GetFeld().name == data)
            {
                SpielerWähltFeld(feld.GetFeld());
                return;
            }
        }
    }
    /// <summary>
    /// Fügt die Farben der Spieler ein
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
    private void SpielerWähltFeld(GameObject FeldName)
    {
        //BroadcastNew("#SpielerWaehltFeld " + FeldName.name); // TODO: hier board update
        // Wenn der Server dran ist, schauen ob das Feld markiert ist
        foreach (MenschAegerDichNichtFeld feld in board.GetPlayerTurn().GetAvailableMoves())
        {
            // Das gewählte Feld
            if (feld.GetFeld().Equals(FeldName))
            {
                string ausgabe = board.GetPlayerTurn().Move(feld);
                board.PrintBoard();
                board.ClearMarkierungen();

                SendBoardUpdate();

                if (ausgabe.Length > 2)
                    AddMSGToProtokoll(GenerateColorsIntoMultipleNames(ausgabe));

                // Schaut ob das Spiel zuende ist
                if (board.GetPlayerTurn().HasPlayerWon()) // Spieler hat gewonnen
                {
                    string time = DateTime.Now.Hour + ":";
                    if (DateTime.Now.Minute < 10)
                        time += "0" + DateTime.Now.Minute;
                    else
                        time += DateTime.Now.Minute;
                    AddMSGToProtokoll(board.TEAM_COLORS[board.GetPlayerTurn().gamerid] + board.GetPlayerTurn().name + "</color></b> hat gewonnen! " + time);
                    SendFinishUpdate();
                    gameIsRunning = false;
                    Logging.log(Logging.LogType.Normal, "MenschAergerDichNichtServer", "SpielerWähltFeld", board.GetPlayerTurn().name + " hat gewonnen! " + time);
                    return;
                }
                // Move / schlag sounds falls spiel noch nicht vorbei ist
                if (ausgabe.Contains(" schlägt "))
                {
                    ServerUtils.AddBroadcast("#PlayWirdGeschlagenSound");
                    SpielerWirdGeschlagen.Play();
                }
                else
                {
                    ServerUtils.AddBroadcast("#PlaySpielerZieht");
                    SpielerZieht.Play();
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
    
    private void SendFinishUpdate()
    {
        string msg = "";
        #region Count Würfel würfe
        // Die meisten Würfel würfe
        MenschAergerDichNichtPlayer pMW1 = board.GetPlayerList()[0];
        foreach (MenschAergerDichNichtPlayer pMW2 in board.GetPlayerList())
        {
            if (pMW2.wuerfelCounter > pMW1.wuerfelCounter)
            {
                pMW1 = pMW2;
            }
        }
        // Die wenigsten Würfel würfe
        MenschAergerDichNichtPlayer pWW1 = board.GetPlayerList()[0];
        foreach (MenschAergerDichNichtPlayer pWW2 in board.GetPlayerList())
        {
            if (pWW2.wuerfelCounter < pWW1.wuerfelCounter)
            {
                pWW1 = pWW2;
            }
        }
        // Alle gleichviel?
        if (pMW1 == pWW1)
        {
            msg += "[#]Alle haben " + pMW1.wuerfelCounter + "x Gewürfelt.";
        }
        else
        {
            msg += "[#]" + board.TEAM_COLORS[pMW1.gamerid] + pMW1.name + "</color></b> hat am häufigsten Gewürfelt. " + pMW1.wuerfelCounter;
            msg += "[#]" + board.TEAM_COLORS[pWW1.gamerid] + pWW1.name + "</color></b> hat am wenigsten Gewürfelt. " + pWW1.wuerfelCounter;
        }
        #endregion

        #region Count Figuren geschlagen
        // Die meisten Figuren geschlagen
        MenschAergerDichNichtPlayer pMF1 = board.GetPlayerList()[0];
        foreach (MenschAergerDichNichtPlayer pMF2 in board.GetPlayerList())
        {
            if (pMF2.schlagCounter > pMF1.schlagCounter)
            {
                pMF1 = pMF2;
            }
        }
        // Die wenigsten Figuren geschlagen
        MenschAergerDichNichtPlayer pWF1 = board.GetPlayerList()[0];
        foreach (MenschAergerDichNichtPlayer pWF2 in board.GetPlayerList())
        {
            if (pWF2.schlagCounter < pWF1.schlagCounter)
            {
                pWF1 = pWF2;
            }
        }
        // Alle gleichviel?
        if (pMF1 == pWF1)
        {
            msg += "[#]Alle haben " + pMF1.schlagCounter + " Schläge.";
        }
        else
        {
            msg += "[#]" + board.TEAM_COLORS[pMF1.gamerid] + pMF1.name + "</color></b> hat die meisten Schläge. " + pMF1.schlagCounter;
            msg += "[#]" + board.TEAM_COLORS[pWF1.gamerid] + pWF1.name + "</color></b> hat die wenigsten Schläge. " + pWF1.schlagCounter;
        }
        #endregion

        #region Count Deaths
        // Die meisten Deaths
        MenschAergerDichNichtPlayer pMD1 = board.GetPlayerList()[0];
        foreach (MenschAergerDichNichtPlayer pMD2 in board.GetPlayerList())
        {
            if (pMD2.deathCounter > pMD1.deathCounter)
            {
                pMD1 = pMD2;
            }
        }
        // Die wenigsten Deaths
        MenschAergerDichNichtPlayer pWD1 = board.GetPlayerList()[0];
        foreach (MenschAergerDichNichtPlayer pWD2 in board.GetPlayerList())
        {
            if (pWD2.deathCounter < pWD1.deathCounter)
            {
                pWD1 = pWD2;
            }
        }
        // Alle gleichviel?
        if (pMD1 == pWD1)
        {
            msg += "[#]Alle haben " + pMD1.deathCounter + " Tode.";
        }
        else
        {
            msg += "[#]" + board.TEAM_COLORS[pMD1.gamerid] + pMD1.name + "</color></b> hat die meisten Tode. " + pMD1.deathCounter;
            msg += "[#]" + board.TEAM_COLORS[pWD1.gamerid] + pWD1.name + "</color></b> hat die wenigsten Tode. " + pWD1.deathCounter;
        }
        #endregion

        // Ausgabe
        if (msg.Length > 3)
            msg = msg.Substring("[#]".Length);
        ServerUtils.AddBroadcast("#SpielIstVorbeiMSGs " + msg);
        SiegerStehtFest.Play();
        foreach (string item in msg.Replace("[#]", "|").Split('|'))
        {
            AddMSGToProtokoll(item);
        }
    }
    /// <summary>
    /// Wenn ein Spieler verlässt, soll dieser automatisch zu einem Bot werden
    /// </summary>
    private void SpielerWirdZumBot(Player p)
    {
        ServerUtils.AddBroadcast("#PlayerMergesBot " + p.name);
        // Der Spieler ist gerade dran
        if (board.GetPlayerTurn().name == p.name && board.GetPlayerTurn().PlayerImage == p.icon)
        {
            AddMSGToProtokoll(board.TEAM_COLORS[board.GetPlayerTurn().gamerid] + p.name + "</color></b> hat das Spiel verlassen.");
            AddMSGToProtokoll(board.TEAM_COLORS[board.GetPlayerTurn().gamerid] + p.name + "</color></b> wird nun von einem <b>Bot</b> übernommen!");
            board.GetPlayerTurn().SetPlayerIntoBot();
            // Spieler hat bereits gewürfelt
            if (board.GetPlayerTurn().GetAvailableMoves().Count > 0)
            {
                // verzögern um 2 sek
                StartCoroutine(BotLaufenVerzoergert());
            }
            // Spieler muss noch würfeln
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
                    AddMSGToProtokoll(board.TEAM_COLORS[player.gamerid] + p.name + "</color></b> hat das Spiel verlassen.");
                    AddMSGToProtokoll(board.TEAM_COLORS[player.gamerid] + p.name + "</color></b> wird nun von einem <b>Bot</b> übernommen!");
                    player.SetPlayerIntoBot();
                    break;
                }
            }
        }
    }
    /// <summary>
    /// Nachdem ein Bot einen Spieler übernommen hat, startet dieser nach 2 Sekunden
    /// </summary>
    /// <returns></returns>
    IEnumerator BotLaufenVerzoergert()
    {
        if (MenschAegerDichNichtBoard.watchBots)
            yield return new WaitForSeconds(0.1f);
        else
            yield return new WaitForSeconds(1f);
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
    /// Prüft ob der Zug des Spielers beendet ist
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
