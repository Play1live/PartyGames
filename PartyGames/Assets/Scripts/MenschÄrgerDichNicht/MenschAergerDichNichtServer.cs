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
    [SerializeField] GameObject Würfel;
    private Coroutine WuerfelCoroutine;
    [SerializeField] GameObject InfoBoard;
    bool[] PlayerConnected;

    [SerializeField] AudioSource DisconnectSound;

    MenschAegerDichNichtBoard board;

    void OnEnable()
    {
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

            #region Spieler Disconnected Message
            for (int i = 0; i < Config.PLAYERLIST.Length; i++)
            {
                if (Config.PLAYERLIST[i].isConnected == false)
                {
                    if (Config.PLAYERLIST[i].isDisconnected == true)
                    {
                        Logging.log(Logging.LogType.Normal, "MenschÄrgerDichNichtServer", "Update", "Spieler hat die Verbindung getrennt. ID: " + Config.PLAYERLIST[i].id);
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
        Logging.log(Logging.LogType.Normal, "MenschÄrgerDichNichtServer", "OnApplicationQuit", "Server wird geschlossen.");
        Config.SERVER_TCP.Server.Close();
    }

    #region Server Stuff  
    #region Kommunikation
    /// <summary>
    /// Sendet eine Nachricht an den übergebenen Spieler
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
            Logging.log(Logging.LogType.Warning, "MenschÄrgerDichNichtServer", "SendMSG", "Nachricht an Client: " + sc.id + " (" + sc.name + ") konnte nicht gesendet werden.", e);
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
        Logging.log(Logging.LogType.Debug, "MenschÄrgerDichNichtServer", "Commands", "Eingehende Nachricht: " + player.name + " " + player.id + " -> " + cmd + "   ---   " + data);
        // Sucht nach Command
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "MenschÄrgerDichNichtServer", "Commands", "Unkown Command: " + cmd + " -> " + data);
                break;

            case "#ClientClosed":
                ClientClosed(player);
                //UpdateSpielerBroadcast(); // TODO: lobby Update  oder ingame soll dann bot übernehmen
                if (Lobby.activeInHierarchy)
                    UpdateLobby();
                PlayDisconnectSound();
                break;
            case "#TestConnection":
                break;
            case "#ClientFocusChange":
                break;

            case "#JoinMenschAergerDichNicht":
                PlayerConnected[player.id - 1] = true;
                UpdateLobby();
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
    /// Spiel Verlassen & Zurück in die Lobby laden
    /// </summary>
    public void SpielVerlassenButton()
    {
        Logging.log(Logging.LogType.Debug, "MenschÄrgerDichNichtServer", "SpielVerlassenButton", "Spiel wird beendet. Lädt ins Hauptmenü.");
        SceneManager.LoadScene("Startup");
        Broadcast("#ZurueckInsHauptmenue");
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
        Playerlist = new GameObject[Lobby.transform.GetChild(1).childCount];
        for (int i = 0; i < Lobby.transform.GetChild(1).childCount; i++)
            Playerlist[i] = Lobby.transform.GetChild(1).GetChild(i).gameObject;
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
        Broadcast("#UpdateLobby " + msg);
    }
    public void ChangeBots(TMP_Dropdown drop)
    {
        int botsCount = Int32.Parse(drop.options[drop.value].text);
        MenschAegerDichNichtBoard.bots = botsCount;
        UpdateLobby();
    }
    public void ChangeWatchOnly(Toggle toggle)
    {
        MenschAegerDichNichtBoard.watchBots = toggle.isOn;
    }
    #region GameLogic
    public void StartGame()
    {
        Logging.log(Logging.LogType.Normal, "MenschAergerDichNichtServer", "StartGame", "Spiel wird gestartet.");
        Würfel.transform.GetChild(0).gameObject.SetActive(false);
        if (MenschAegerDichNichtBoard.watchBots)
        {
            Logging.log(Logging.LogType.Normal, "MenschAergerDichNichtServer", "StartGame", "Es werden nur Bots spielen, alle Clients werden getrennt.");
            Broadcast("#ServerClosed");
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
            Broadcast("#StartGame [PLAYER]" + broadcastPlayer + "[PLAYER][MAP]" + selectedMap.name + "[MAP][TEAMSIZE]" + mapInt + "[TEAMSIZE][RUNWAY]" + RunWaySize + "[RUNWAY]");
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
    public void AddMSGToProtokoll(string msg)
    {
        GameObject go = Instantiate(SpielprotokollContent.transform.GetChild(0).gameObject, SpielprotokollContent.transform.GetChild(0).position, SpielprotokollContent.transform.GetChild(0).rotation);
        go.name = "MSG_" + SpielprotokollContent.transform.childCount;
        go.transform.SetParent(SpielprotokollContent.transform);
        go.transform.GetComponentInChildren<TMP_Text>().text = msg;
        go.transform.localScale = new Vector3(1, 1, 1);
        go.SetActive(true);
    }
    public void DisplayMSGInfoBoard(string msg)
    {
        InfoBoard.GetComponentInChildren<TMP_Text>().text = msg;
    }
    public void CheckEndOfTurn()
    {

    }
    public void StartTurn()
    {
        MenschAergerDichNichtPlayer player = board.PlayerTurnSelect();
        if (player.GetAllInStartOrHome())
            player.availableDices = 3;
        else
            player.availableDices = 1;
        AddMSGToProtokoll(player.name + " ist dran.");
        DisplayMSGInfoBoard(player.name + " ist dran!");
        StartTurnSelectType(player);
    }
    private void StartTurnSelectType(MenschAergerDichNichtPlayer player)
    {
        // Spieler
        if (!player.isBot)
        {
            // Server
            if (player.PlayerImage == Config.SERVER_ICON)
            {
                StartTurnServer();
                return;
            }
            // Client
            else
            {
                foreach (Player p in Config.PLAYERLIST)
                {
                    if (p.icon == player.PlayerImage)
                    {
                        StartTurnClient();
                        return;
                    }
                }
            }
        }
        // Bot
        else
        {
            StartTurnBot();
            return;
        }
    }
    private void StartTurnServer()
    {
        DisplayMSGInfoBoard("Du bist dran!\n Du kannst würfeln.");

        WuerfelAktivieren(true);
    }
    private void StartTurnClient()
    {

    }
    private void StartTurnBot()
    {
        StartCoroutine(StartBotWuerfelVerzoegert());
    }

    public void LaufenTurnBot()
    {
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
            board.GetPlayerTurn().Move(felder[random]);
        }
        return;
    }

    IEnumerator StartBotWuerfelVerzoegert()
    {
        yield return new WaitForSeconds(2);
        WuerfelStarteAnimation();
        yield break;
    }
    private void WuerfelAktivieren(bool aktivieren)
    {
        Würfel.transform.GetChild(0).gameObject.SetActive(aktivieren);
    }
    public void WuerfelStarteAnimation()
    {
        WuerfelAktivieren(false);
        board.GetPlayerTurn().availableDices--;
        int result = UnityEngine.Random.Range(1, 7);
        if (result == 6)
            board.GetPlayerTurn().availableDices = 1;
        Debug.LogWarning("Result: " + result);

        if (WuerfelCoroutine != null)
            StopCoroutine(WuerfelCoroutine);
        WuerfelCoroutine = StartCoroutine(WuerfelAnimation(result));
    }
    IEnumerator WuerfelAnimation(int result)
    {
        int count = 0;
        List<Sprite> wuerfel = new List<Sprite>();
        for (int i = 1; i <= 6; i++)
        {
            wuerfel.Add(Resources.Load<Sprite>("Images/GUI/würfel "+i));
        }
        // Roll Time
        DateTime swtichTime = DateTime.Now.AddSeconds(2);
        while (DateTime.Compare(DateTime.Now, swtichTime) < 0)
        {
            Würfel.GetComponent<Image>().sprite = wuerfel[(count++) % wuerfel.Count];
            //Debug.LogWarning(0.005f * count);
            yield return new WaitForSeconds(0.005f*count);
        }
        // Roll Random Sec
        int times = UnityEngine.Random.Range(5, 20);
        for (int i = 0; i < times; i++)
        {
            Würfel.GetComponent<Image>().sprite = wuerfel[(count++) % wuerfel.Count];
            yield return new WaitForSeconds(0.005f * count);
        }
        // Roll to 6
        while (wuerfel[wuerfel.Count-1] == wuerfel[count % wuerfel.Count])
        {
            Würfel.GetComponent<Image>().sprite = wuerfel[(count++) % wuerfel.Count];
            yield return new WaitForSeconds(0.005f * count);
        }
        // Roll Until selected
        while (!wuerfel[count % wuerfel.Count].name.Equals("würfel " + result))
        {
            Würfel.GetComponent<Image>().sprite = wuerfel[(count++) % wuerfel.Count];
            yield return new WaitForSeconds(0.005f * count);
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
        AddMSGToProtokoll(board.GetPlayerTurn().name + " würfelt " + Würfel.GetComponent<Image>().sprite.name.Replace("würfel ", ""));
        Debug.LogWarning("Wurde gewürfelt...");

        board.GetPlayerTurn().MarkAvailableMoves(result);

        // Spieler ist ein bot
        if (board.GetPlayerTurn().isBot)
        {
            yield return new WaitForSeconds(2);
            LaufenTurnBot(); // kann laufen
            board.ClearMarkierungen();
            if (CheckForEndOfTurn()) // Zug ist vorbei
            {
                if (board.GetPlayerTurn().HasPlayerWon()) // Spieler hat gewonnen
                {
                    Debug.LogWarning("Spieler hat gewonnen");
                    yield break;
                }
                else // Spieler hat noch nicht gewonnen, Zug zuende
                {
                    board.GetPlayerTurn().wuerfel = 0;
                    StartTurn(); // Starte neuen Zug
                    yield break;
                }
            }
            else
            {
                if (board.GetPlayerTurn().HasPlayerWon()) // Spieler hat gewonnen
                {
                    Debug.LogWarning("Spieler hat gewonnen");
                    yield break;
                }
                else // Spieler hat noch nicht gewonnen, nochmal würfeln
                {
                    StartTurnSelectType(board.GetPlayerTurn());
                    yield break;
                }
            }
        }
        // Spieler ist ein Spieler
        else
        {
            // Ist Server
            if (Config.SERVER_ICON == board.GetPlayerTurn().PlayerImage)
            {
                // kann laufen
                // Zugvorbei?
                // Sieg prüfen
                // wenn nicht wieder würfeln
            }
            // Ist Client
            else 
            {
                // kann laufen
                // Zugvorbei?
                // Sieg prüfen
                // wenn nicht wieder würfeln
            }
        }
        yield break;
    }
    private bool CheckForEndOfTurn()
    {
        if (board.GetPlayerTurn().availableDices == 0)
            return true;
        else
            return false;
    }

    #endregion
}
