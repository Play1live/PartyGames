using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenschAergerDichNichtClient : MonoBehaviour
{
    [SerializeField] GameObject Lobby;
    private GameObject[] Playerlist;
    [SerializeField] GameObject Games;
    [SerializeField] GameObject[] Maps;
    [SerializeField] GameObject SpielprotokollContent;
    [SerializeField] GameObject W�rfel;
    private Coroutine WuerfelCoroutine;
    [SerializeField] GameObject InfoBoard;

    [SerializeField] AudioSource DisconnectSound;

    MenschAegerDichNichtBoard board;
    private bool ClientAllowZugWahl;

    void OnEnable()
    {
        Lobby.SetActive(true);
        Games.SetActive(false);
        InitLobby();

        if (!Config.CLIENT_STARTED)
            return;
        SendToServer("#JoinMenschAergerDichNicht");

        StartCoroutine(TestConnectionToServer());
    }

    void Update()
    {
        #region Pr�ft auf Nachrichten vom Server
        if (Config.CLIENT_STARTED)
        {
            NetworkStream stream = Config.CLIENT_TCP.GetStream();
            if (stream.DataAvailable)
            {
                StreamReader reader = new StreamReader(stream);
                string data = reader.ReadLine();
                if (data != null)
                    OnIncomingData(data);
            }
        }
        #endregion
    }

    private void OnApplicationFocus(bool focus)
    {
        SendToServer("#ClientFocusChange " + focus);
    }

    private void OnApplicationQuit()
    {
        Logging.log(Logging.LogType.Normal, "Mensch�rgerDichNichtClient", "OnApplicationQuit", "Client wird geschlossen.");
        SendToServer("#ClientClosed");
        CloseSocket();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    /// <summary>
    /// Testet die Verbindung zum Server
    /// </summary>
    IEnumerator TestConnectionToServer()
    {
        Logging.log(Logging.LogType.Debug, "Mensch�rgerDichNichtClient", "TestConnectionToServer", "Testet die Verbindumg zum Server.");
        while (Config.CLIENT_STARTED)
        {
            SendToServer("#TestConnection");
            yield return new WaitForSeconds(10);
        }
        yield break;
    }
    #region Verbindungen
    /// <summary>
    /// Trennt die Verbindung zum Server
    /// </summary>
    private void CloseSocket()
    {
        if (!Config.CLIENT_STARTED)
            return;

        Config.CLIENT_TCP.Close();
        Config.CLIENT_STARTED = false;
    }
    #endregion
    #region Kommunikation
    /// <summary>
    /// Sendet eine Nachricht an den Server.
    /// </summary>
    /// <param name="data">Nachricht</param>
    private void SendToServer(string data)
    {
        if (!Config.CLIENT_STARTED)
            return;

        try
        {
            NetworkStream stream = Config.CLIENT_TCP.GetStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.WriteLine(data);
            writer.Flush();
        }
        catch (Exception e)
        {
            Logging.log(Logging.LogType.Warning, "Mensch�rgerDichNichtClient", "SendToServer", "Nachricht an Server konnte nicht gesendet werden.", e);
            Config.HAUPTMENUE_FEHLERMELDUNG = "Verbindung zum Server wurde verloren.";
            CloseSocket();
            SceneManager.LoadSceneAsync("StartUp");
        }
    }
    /// <summary>
    /// Einkommende Nachrichten die vom Sever
    /// </summary>
    /// <param name="data">Nachricht</param>
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
    #endregion
    /// <summary>
    /// Eingehende Commands vom Server
    /// </summary>
    /// <param name="data">Befehlsargumente</param>
    /// <param name="cmd">Befehl</param>
    private void Commands(string data, string cmd)
    {
        Logging.log(Logging.LogType.Debug, "Mensch�rgerDichNichtClient", "Commands", "Eingehende Nachricht: " + cmd + " -> " + data);
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "Mensch�rgerDichNichtClient", "Commands", "Unkown Command: " + cmd + " -> " + data);
                break;

            #region Universal Commands
            case "#ServerClosed":
                Logging.log(Logging.LogType.Normal, "Mensch�rgerDichNichtClient", "Commands", "Verbindumg zum Server wurde beendet. Lade ins Hauptmen�.");
                CloseSocket();
                SceneManager.LoadSceneAsync("Startup");
                break;
            case "#UpdateRemoteConfig":
                Logging.log(Logging.LogType.Normal, "Mensch�rgerDichNichtClient", "Commands", "RemoteConfig wird neugeladen");
                LoadConfigs.FetchRemoteConfig();
                break;
            case "#ZurueckInsHauptmenue":
                Logging.log(Logging.LogType.Normal, "Mensch�rgerDichNichtClient", "Commands", "Spiel wird beendet. Lade ins Hauptmen�");
                SceneManager.LoadSceneAsync("Startup");
                break;
            #endregion

            case "#UpdateLobby":
                UpdateLobby(data);
                break;
            case "#StartGame":
                StartGame(data);
                break;

            case "#SpielerTurn":
                StartTurn(data);
                break;
            case "#Wuerfel":
                Wuerfel(data);
                break;
            case "#SpielerWaehltFeld":
                SpielerW�hltFeld(data);
                break;
            case "#StartTurnSelectType":
                StartTurnSelectType(board.GetPlayerTurn());
                break;
            case "#PlayerMergesBot":
                SpielerWirdZumBot(data);
                break;
        }
    }
    /// <summary>
    /// Spielt den Disconnect Sound ab
    /// </summary>
    private void PlayDisconnectSound()
    {
        DisconnectSound.Play();
    }
    /// <summary>
    /// Initialisiert die Anzeigen der Scene
    /// </summary>
    private void InitLobby()
    {
        Logging.log(Logging.LogType.Debug, "Mensch�rgerDichNichtClient", "InitLobby", "Lobby wird initialisiert.");
        Playerlist = new GameObject[Lobby.transform.GetChild(1).childCount];
        for (int i = 0; i < Lobby.transform.GetChild(1).childCount; i++)
        {
            Playerlist[i] = Lobby.transform.GetChild(1).GetChild(i).gameObject;
            Playerlist[i].SetActive(false);
        }
        // Blendet Maps aus
        foreach (GameObject go in Maps)
        {
            go.SetActive(false);
        }
    }
    /// <summary>
    /// Aktualisiert die Lobby
    /// </summary>
    private int ingameSpieler;
    private void UpdateLobby(string data)
    {
        Logging.log(Logging.LogType.Debug, "MenschAergerDichNichtClient", "UpdateLobby", "LobbyAnzeigen werden aktualisiert: " + data);
        for (int i = 0; i < Playerlist.Length; i++)
        {
            Playerlist[i].SetActive(false);
        }
        string[] elemente = data.Split('|');
        if (elemente.Length < ingameSpieler)
            PlayDisconnectSound();
        ingameSpieler = elemente.Length;
        for (int i = 0; i < elemente.Length; i++)
        {
            Playerlist[i].GetComponentInChildren<TMP_Text>().text = elemente[i];
            Playerlist[i].SetActive(true);
        }
    }
    #region GameLogic
    /// <summary>
    /// Startet das MenschAergerDichNicht Spiel
    /// </summary>
    /// <param name="data"></param>
    private void StartGame(string data)
    {
        Logging.log(Logging.LogType.Normal, "MenschAergerDichNichtClient", "StartGame", "Startet das Spiel");
        // PlayerInit
        List<MenschAergerDichNichtPlayer> randomplayer = new List<MenschAergerDichNichtPlayer>();
        string[] playerdata = data.Replace("[PLAYER]", "|").Split('|')[1].Replace("[#]", "|").Split('|');
        for (int i = 0; i < playerdata.Length; i++)
        {
            string name = playerdata[i].Split('*')[0];
            bool isbot = bool.Parse(playerdata[i].Split('*')[1]);
            Sprite sprite;
            if (isbot)
                sprite = Resources.Load<Sprite>("Images/Icons/" + playerdata[i].Split('*')[2]);
            else
                sprite = Resources.Load<Sprite>("Images/ProfileIcons/" + playerdata[i].Split('*')[2]);

            randomplayer.Add(new MenschAergerDichNichtPlayer(i, name, isbot, sprite));
        }
        // LoadMap
        GameObject selectedMap = null;
        string mapstring = data.Replace("[MAP]", "|").Split('|')[1];
        foreach (GameObject go in Maps)
        {
            go.SetActive(false);
            if (go.name.Equals(mapstring))
                selectedMap = go;
        }
        if (selectedMap == null)
        {
            Logging.log(Logging.LogType.Warning, "MenschAergerDichNichtClient", "StartGame", "Ausgew�hlte Map konnte nicht gefunden werden.");
            Lobby.SetActive(true);
            Games.SetActive(false);
            CloseSocket();
            SceneManager.LoadSceneAsync("Startup");
            return;
        }
        selectedMap.SetActive(true);
        int teamsize = Int32.Parse(data.Replace("[TEAMSIZE]", "|").Split('|')[1]);
        int runwaysize = Int32.Parse(data.Replace("[RUNWAY]", "|").Split('|')[1]);
        Lobby.SetActive(false);
        Games.SetActive(true);

        board = new MenschAegerDichNichtBoard(selectedMap, runwaysize, teamsize, randomplayer);

        // Hide PlayerAnimation
        selectedMap.transform.GetChild(4).gameObject.SetActive(false);

        string time = DateTime.Now.Hour + ":";
        if (DateTime.Now.Minute < 10)
            time += "0" + DateTime.Now.Minute;
        else
            time += DateTime.Now.Minute;
        AddMSGToProtokoll("Spiel wurde gestartet. " + time);
        DisplayMSGInfoBoard("Spiel wird geladen...");
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
    /// Startet einen neuen Zug
    /// </summary>
    /// <param name="data"></param>
    private void StartTurn(string data)
    {
        Logging.log(Logging.LogType.Debug, "MenschAergerDichNichtClient", "StartTurn", "Starte einen neuen Zug. " + data);
        board.ClearMarkierungen();
        WuerfelAktivieren(false);

        MenschAergerDichNichtPlayer player = board.PlayerTurnSelect();
        if (player.GetAllInStartOrHome())
            player.availableDices = 3;
        else
            player.availableDices = 1;
        AddMSGToProtokoll(board.TEAM_COLORS[player.gamerid] + player.name + "</color></b> ist dran.");
        StartTurnSelectType(player);
    }
    /// <summary>
    /// Startet den Zug eines Spielers
    /// </summary>
    /// <param name="player"></param>
    private void StartTurnSelectType(MenschAergerDichNichtPlayer player)
    {
        Logging.log(Logging.LogType.Debug, "MenschAergerDichNichtClient", "StartTurnSelectType", "Der Spieler " + player.name + " darf ziehen.");
        DisplayMSGInfoBoard(player.name + " ist dran!");
        // Spieler
        if (player.name == Config.PLAYER_NAME)
        {
            DisplayMSGInfoBoard("Du bist dran!\n Du kannst w�rfeln.");

            WuerfelAktivieren(true);
        }
    }
    /// <summary>
    /// Startet den W�rfel
    /// </summary>
    /// <param name="data"></param>
    private void Wuerfel(string data)
    {
        if (data.Split('*')[1] == Config.PLAYER_NAME)
            return;
        Logging.log(Logging.LogType.Debug, "MenschAergerDichNichtClient", "Wuerfel", "Startet den W�rfel eines anderen Spielers: " + data);

        board.GetPlayerTurn().availableDices--;
        int result = Int32.Parse(data.Split('*')[0]);
        if (result == 6)
            board.GetPlayerTurn().availableDices = 1;

        if (WuerfelCoroutine != null)
            StopCoroutine(WuerfelCoroutine);
        WuerfelCoroutine = StartCoroutine(WuerfelAnimation(result));
    }
    /// <summary>
    /// Aktiviert den Button zum W�rfeln
    /// </summary>
    /// <param name="aktivieren"></param>
    private void WuerfelAktivieren(bool aktivieren)
    {
        W�rfel.transform.GetChild(0).gameObject.SetActive(aktivieren);
    }
    /// <summary>
    /// Startet die W�rfelanimation und sendet die Infos an den Server
    /// </summary>
    public void WuerfelStarteAnimation()
    {
        if (Config.isServer)
            return;

        WuerfelAktivieren(false);
        board.GetPlayerTurn().availableDices--;
        int result = UnityEngine.Random.Range(1, 7);
        Logging.log(Logging.LogType.Debug, "MenschAergerDichNichtClient", "WuerfelStarteAnimation", "Client w�rfelt: " + result);
        if (board.GetPlayerTurn().GetAllInStartOrHome())
            result = 6; // TODO nur zum testen
        if (result == 6)
            board.GetPlayerTurn().availableDices = 1;
        SendToServer("#Wuerfel " + result);

        if (WuerfelCoroutine != null)
            StopCoroutine(WuerfelCoroutine);
        WuerfelCoroutine = StartCoroutine(WuerfelAnimation(result));
    }
    /// <summary>
    /// Animation des W�rfels wird abgespielt
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    IEnumerator WuerfelAnimation(int result)
    {
        int count = 0;
        List<Sprite> wuerfel = new List<Sprite>();
        for (int i = 1; i <= 6; i++)
        {
            wuerfel.Add(Resources.Load<Sprite>("Images/GUI/w�rfel " + i));
        }
        // Roll Time
        DateTime swtichTime = DateTime.Now.AddSeconds(2);
        while (DateTime.Compare(DateTime.Now, swtichTime) < 0)
        {
            W�rfel.GetComponent<Image>().sprite = wuerfel[(count++) % wuerfel.Count];
            //Debug.LogWarning(0.005f * count);
            yield return new WaitForSeconds(0.005f * count);
        }
        // Roll to 6
        while (wuerfel[wuerfel.Count - 1] == wuerfel[count % wuerfel.Count])
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
        Logging.log(Logging.LogType.Debug, "MenschAergerDichNichtClient", "ParseWuerfelResult", "Ergebnis wird verarbeitet");
        AddMSGToProtokoll(board.TEAM_COLORS[board.GetPlayerTurn().gamerid] + board.GetPlayerTurn().name + "</color></b> w�rfelt " + W�rfel.GetComponent<Image>().sprite.name.Replace("w�rfel ", ""));

        board.GetPlayerTurn().MarkAvailableMoves(result);

        // Spieler kann nicht laufen
        if (board.GetPlayerTurn().GetAvailableMoves().Count == 0)
        {
            board.ClearMarkierungen();
            // Schaut ob der Zug des Spielers beendet ist
            if (CheckForEndOfTurn())
            {
                board.GetPlayerTurn().wuerfel = 0;
            }
            // Zug noch nicht vorbei
            else
            {
                StartTurnSelectType(board.GetPlayerTurn());
            }
            yield break;
        }
        StartCoroutine(BlockZugWahlFuer2Sek());
        yield break;
    }
    /// <summary>
    /// Sperrt die Wahl eines Zuges f�r X Sekunden, um Fehler beim Senden zu vermeiden
    /// </summary>
    /// <returns></returns>
    IEnumerator BlockZugWahlFuer2Sek()
    {
        ClientAllowZugWahl = false;
        yield return new WaitForSeconds(2);
        ClientAllowZugWahl = true;
        yield break;
    }
    /// <summary>
    /// Generiert die Farben des Spielers in die Nachricht
    /// [C][C]Henryk[/COLOR] nachricht [C][C]Ron[/COLOR] Nur f�r 2 
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
    /// Ein anderer Spieler wahlt ein Feld zum laufen
    /// </summary>
    /// <param name="data"></param>
    private void SpielerW�hltFeld(string data)
    {
        // Wenn der Server dran ist, schauen ob das Feld markiert ist
        foreach (MenschAegerDichNichtFeld feld in board.GetPlayerTurn().GetAvailableMoves())
        {
            // Das gew�hlte Feld
            if (feld.GetFeld().name.Equals(data))
            {
                Logging.log(Logging.LogType.Debug, "MenschAergerDichNichtClient", "SpielerW�hltFeld", "Spieler " + feld.GetPlayer().name + " w�hlt " + feld.GetFeld().name);
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
                    //StartTurn(); // Starte neuen Zug
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
    /// Client w�hlt ein Feld zum Laufen, sendet Anfrage an den Server
    /// </summary>
    /// <param name="FeldName"></param>
    public void ClientW�hltFeld(GameObject FeldName)
    {        
        if (Config.isServer)
            return;
        if (!ClientAllowZugWahl)
            return;
        // Server ist aktuell nicht dran
        if (board.GetPlayerTurn().isBot || Config.PLAYER_NAME != board.GetPlayerTurn().name)
            return;

        // Nur selected
        if (!FeldName.transform.GetChild(2).gameObject.activeInHierarchy)
            return;
        Logging.log(Logging.LogType.Debug, "MenschAergerDichNichtClient", "ClientW�hltFeld", "Client w�hlt ein Feld zum Laufen: " + FeldName.name);

        SendToServer("#ClientWaehltFeld " + FeldName.name);
    }
    /// <summary>
    /// Pr�ft ob der Zug des Spielers vorbei ist
    /// </summary>
    /// <returns></returns>
    private bool CheckForEndOfTurn()
    {
        if (board.GetPlayerTurn().availableDices == 0)
            return true;
        else
            return false;
    }
    /// <summary>
    /// Wenn ein Spieler verl�sst, soll dieser automatisch zu einem Bot werden
    /// </summary>
    private void SpielerWirdZumBot(string data)
    {
        // Der Spieler ist gerade dran
        if (board.GetPlayerTurn().name == data)
        {
            AddMSGToProtokoll(board.TEAM_COLORS[board.GetPlayerTurn().gamerid] + data + "</color></b> hat das Spiel verlassen.");
            AddMSGToProtokoll(board.TEAM_COLORS[board.GetPlayerTurn().gamerid] + data + "</color></b> wird nun von einem <b>Bot</b> �bernommen!");
            board.GetPlayerTurn().SetPlayerIntoBot();
        }
        // Der Spieler ist nicht dran
        else
        {
            foreach (MenschAergerDichNichtPlayer player in board.GetPlayerList())
            {
                if (player.name == data)
                {
                    AddMSGToProtokoll(board.TEAM_COLORS[board.GetPlayerTurn().gamerid] + data + "</color></b> hat das Spiel verlassen.");
                    AddMSGToProtokoll(board.TEAM_COLORS[board.GetPlayerTurn().gamerid] + data + "</color></b> wird nun von einem <b>Bot</b> �bernommen!");
                    player.SetPlayerIntoBot();
                    break;
                }
            }
        }
    }
    #endregion
}