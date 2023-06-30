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
    private GameObject Timer;
    private DateTime RoundStart;
    private int protokollmsgs;
    [SerializeField] GameObject Würfel;
    private Coroutine WuerfelCoroutine;
    [SerializeField] GameObject InfoBoard;

    [SerializeField] AudioSource DisconnectSound;
    [SerializeField] AudioSource SpielerZieht;
    [SerializeField] AudioSource SpielerIstDran;
    [SerializeField] AudioSource SpielerWirdGeschlagen;
    [SerializeField] AudioSource SiegerStehtFest;

    MenschAegerDichNichtBoard board;
    private bool gameIsRunning;
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
        #region Prüft auf Nachrichten vom Server
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
        Logging.log(Logging.LogType.Normal, "MenschÄrgerDichNichtClient", "OnApplicationQuit", "Client wird geschlossen.");
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
        Logging.log(Logging.LogType.Debug, "MenschÄrgerDichNichtClient", "TestConnectionToServer", "Testet die Verbindumg zum Server.");
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
            Logging.log(Logging.LogType.Warning, "MenschÄrgerDichNichtClient", "SendToServer", "Nachricht an Server konnte nicht gesendet werden.", e);
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
        Logging.log(Logging.LogType.Debug, "MenschÄrgerDichNichtClient", "Commands", "Eingehende Nachricht: " + cmd + " -> " + data);
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "MenschÄrgerDichNichtClient", "Commands", "Unkown Command: " + cmd + " -> " + data);
                break;

            #region Universal Commands
            case "#ServerClosed":
                Logging.log(Logging.LogType.Normal, "MenschÄrgerDichNichtClient", "Commands", "Verbindumg zum Server wurde beendet. Lade ins Hauptmenü.");
                CloseSocket();
                SceneManager.LoadSceneAsync("Startup");
                break;
            case "#UpdateRemoteConfig":
                Logging.log(Logging.LogType.Normal, "MenschÄrgerDichNichtClient", "Commands", "RemoteConfig wird neugeladen");
                LoadConfigs.FetchRemoteConfig();
                break;
            case "#ZurueckInsHauptmenue":
                Logging.log(Logging.LogType.Normal, "MenschÄrgerDichNichtClient", "Commands", "Spiel wird beendet. Lade ins Hauptmenü");
                SceneManager.LoadSceneAsync("Startup");
                break;
            #endregion

            case "#UpdateLobby":
                UpdateLobby(data);
                break;
            case "#StartGame":
                StartGame(data);
                break;


            case "#StartTurn":
                StartTurn(data);
                break;
            case "#AddProtokoll":
                AddMSGToProtokoll(data);
                break;
            case "#AktiviereWuerfel":
                AktiviereWuerfel(data);
                break;
            case "#Wuerfel":
                Wuerfel(data);
                break;
            case "#UpdateBoard":
                UpdateBoard(data);
                break;
            case "#MarkMarkierungen":
                MarkMarkierungen(data);
                break;
            case "#SpielIstVorbeiMSGs":
                SpielIstVorbeiMSGs(data);
                break;
            case "#PlayWirdGeschlagenSound":
                SpielerWirdGeschlagen.Play();
                break;
            case "#PlaySpielerZieht":
                SpielerZieht.Play();
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
        Logging.log(Logging.LogType.Debug, "MenschÄrgerDichNichtClient", "InitLobby", "Lobby wird initialisiert.");
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
        gameIsRunning = true;
        RoundStart = DateTime.Now;
        protokollmsgs = 0;
        Timer = SpielprotokollContent.transform.parent.parent.parent.GetChild(0).gameObject;
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
            Logging.log(Logging.LogType.Warning, "MenschAergerDichNichtClient", "StartGame", "Ausgewählte Map konnte nicht gefunden werden.");
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
        //AddMSGToProtokoll("Spiel wurde gestartet. " + time);
        DisplayMSGInfoBoard("Spiel wird geladen...");
        //AddMSGToProtokoll("...");

        StartCoroutine(RunVisibleTimer());


        WuerfelAktivieren(false);
        //StartTurn(board.PlayerTurnSelect().name);
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
        int sec = min * 60 + DateTime.Now.Second - starttime.Second;

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
    private void AddMSGToProtokoll(string msg)
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
    }
    private void SpielIstVorbeiMSGs(string msg)
    {
        SiegerStehtFest.Play();
        foreach (string item in msg.Replace("[#]", "|").Split('|'))
        {
            AddMSGToProtokoll(item);
        }
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

        int pid = Int32.Parse(data.Split('*')[0]);
        int dices = Int32.Parse(data.Split('*')[1]);

        MenschAergerDichNichtPlayer player = board.GetPlayerList()[pid];
        board.SetPlayerTurn(player);
        player.availableDices = dices;

        //AddMSGToProtokoll(board.TEAM_COLORS[player.gamerid] + player.name + "</color></b> ist dran.");
        if (Config.PLAYER_NAME == player.name)
        {
            DisplayMSGInfoBoard("Du bist dran!\n Du kannst würfeln.");
            WuerfelAktivieren(true);
            SpielerIstDran.Play();
        }
        else
        {
            DisplayMSGInfoBoard(player.name + " ist dran!");
        }
        AktiviereWuerfel(player.name);
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
            DisplayMSGInfoBoard("Du bist dran!\n Du kannst würfeln.");

            WuerfelAktivieren(true);
        }
    }
    private void UpdateBoard(string data)
    {
        if (board == null)
        {
            Logging.log(Logging.LogType.Error, "MenschÄrgerDichNicht", "UpdateBoard", "Board kann nicht geupdatet werden, da es noch nicht gestartet wurde.");
            return;
        }
        board.ClearMarkierungen();
        // Clear Board
        foreach (MenschAegerDichNichtFeld feld in board.GetRunWay())
            feld.DisplayPlayer(new MenschAergerDichNichtPlayer(-1, "ERROR", false, Resources.Load<Sprite>("Images/ProfileIcons/empty")));
        foreach (MenschAergerDichNichtBase team in board.GetStarts())
            foreach (MenschAegerDichNichtFeld feld in team.GetBases())
                feld.DisplayPlayer(new MenschAergerDichNichtPlayer(-1, "ERROR", false, Resources.Load<Sprite>("Images/ProfileIcons/empty")));
        foreach (MenschAergerDichNichtBase team in board.GetHomes())
            foreach (MenschAegerDichNichtFeld feld in team.GetBases())
                feld.DisplayPlayer(new MenschAergerDichNichtPlayer(-1, "ERROR", false, Resources.Load<Sprite>("Images/ProfileIcons/empty")));

        // Display Update
        string[] runway = data.Replace("[RUNWAY]", "|").Split('|')[1].Replace("[#]", "|").Split('|');
        if (data.Replace("[RUNWAY]", "|").Split('|')[1].Length > 0)
        {
            foreach (string item in runway)
            {
                int fieldindex = Int32.Parse(item.Split('*')[0]);
                int playerid = Int32.Parse(item.Split('*')[1]);
                board.GetRunWay()[fieldindex].DisplayPlayer(board.GetPlayerList()[playerid]);
            }
        }
        if (data.Replace("[STARTS]", "|").Split('|')[1].Length > 0)
        {
            string[] starts = data.Replace("[STARTS]", "|").Split('|')[1].Replace("[#]", "|").Split('|');
            foreach (string item in starts)
            {
                int teamindex = Int32.Parse(item.Split('*')[0]);
                int fieldindex = Int32.Parse(item.Split('*')[1]);
                int playerid = Int32.Parse(item.Split('*')[2]); 
                board.GetStarts()[teamindex].GetBases()[fieldindex].DisplayPlayer(board.GetPlayerList()[playerid]);
            }
        }
        if (data.Replace("[HOMES]", "|").Split('|')[1].Length > 0)
        {
            string[] homes = data.Replace("[HOMES]", "|").Split('|')[1].Replace("[#]", "|").Split('|');
            foreach (string item in homes)
            {
                int teamindex = Int32.Parse(item.Split('*')[0]);
                int fieldindex = Int32.Parse(item.Split('*')[1]);
                int playerid = Int32.Parse(item.Split('*')[2]);
                board.GetHomes()[teamindex].GetBases()[fieldindex].DisplayPlayer(board.GetPlayerList()[playerid]);
            }
        }

        // Schauen ob der Spieler noch dran ist
        if (Config.PLAYER_NAME == board.GetPlayerTurn().name && board.GetPlayerTurn().availableDices > 0)
        {
            StartCoroutine(AktiviereWuerfelTime(2));
            //WuerfelAktivieren(true);
        }
    }
    /// <summary>
    /// Markiert mögliche Felder
    /// </summary>
    /// <param name="data"></param>
    private void MarkMarkierungen(string data)
    {
        board.ClearMarkierungen();
        string[] runway = data.Replace("[RUNWAY]", "|").Split('|')[1].Replace("[#]", "|").Split('|');
        if (data.Replace("[RUNWAY]", "|").Split('|')[1].Length > 0)
        {
            foreach (string item in runway)
            {
                int fieldindex = Int32.Parse(item);
                this.board.GetRunWay()[fieldindex].MarkSelectableField();
            }
        }
        if (data.Replace("[HOMES]", "|").Split('|')[1].Length > 0)
        {
            string[] homes = data.Replace("[HOMES]", "|").Split('|')[1].Replace("[#]", "|").Split('|');
            foreach (string item in homes)
            {
                int teamindex = Int32.Parse(item.Split('*')[0]);
                int fieldindex = Int32.Parse(item.Split('*')[1]);
                this.board.GetHomes()[teamindex].GetBases()[fieldindex].MarkSelectableField();
            }
        }

        // Wenn spieler dran erlaube Ziehen
        if (board.GetPlayerTurn().name == Config.PLAYER_NAME)
        {
            ClientAllowZugWahl = true;
        }
    }
    /// <summary>
    /// Startet den Würfel
    /// </summary>
    /// <param name="data"></param>
    private void Wuerfel(string data)
    {
        if (data.Split('*')[1] == Config.PLAYER_NAME)
            return;
        Logging.log(Logging.LogType.Debug, "MenschAergerDichNichtClient", "Wuerfel", "Startet den Würfel eines anderen Spielers: " + data);

        if (data.Split('*')[1] != board.GetPlayerTurn().name)
        {
            //StartTurn(data.Split('*')[1]);
            board.SetPlayerTurn(board.GetPlayerList()[board.GetPlayerByName(data.Split('*')[1])]);
        }

        board.GetPlayerTurn().availableDices--;
        int result = Int32.Parse(data.Split('*')[0]);
        if (result == 6)
            board.GetPlayerTurn().availableDices = 1;

        if (WuerfelCoroutine != null)
            StopCoroutine(WuerfelCoroutine);
        WuerfelCoroutine = StartCoroutine(WuerfelAnimation(result));
    }    
    /// <summary>
    /// Aktiviere den Würfel, falls man der Spieler ist
    /// </summary>
    /// <param name="data"></param>
    private void AktiviereWuerfel(string data)
    {
        if (data == Config.PLAYER_NAME)
            WuerfelAktivieren(true);
    }
    private IEnumerator AktiviereWuerfelTime(int sec)
    {
        yield return new WaitForSeconds(sec);
        WuerfelAktivieren(true);
        yield break;
    }
    /// <summary>
    /// Aktiviert den Button zum Würfeln
    /// </summary>
    /// <param name="aktivieren"></param>
    private void WuerfelAktivieren(bool aktivieren)
    {
        Würfel.transform.GetChild(0).gameObject.SetActive(aktivieren);
    }
    /// <summary>
    /// Startet die Würfelanimation und sendet die Infos an den Server
    /// </summary>
    public void WuerfelStarteAnimation()
    {
        if (Config.isServer)
            return;

        WuerfelAktivieren(false);
        board.GetPlayerTurn().availableDices--;
        int result = UnityEngine.Random.Range(1, 7);
        Logging.log(Logging.LogType.Debug, "MenschAergerDichNichtClient", "WuerfelStarteAnimation", "Client würfelt: " + result);
        if (result == 6)
            board.GetPlayerTurn().availableDices = 1;
        SendToServer("#Wuerfel " + result);

        if (WuerfelCoroutine != null)
            StopCoroutine(WuerfelCoroutine);
        WuerfelCoroutine = StartCoroutine(WuerfelAnimation(result));
    }
    /// <summary>
    /// Animation des Würfels wird abgespielt
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    IEnumerator WuerfelAnimation(int result)
    {
        int count = 0;
        List<Sprite> wuerfel = new List<Sprite>();
        for (int i = 1; i <= 6; i++)
        {
            wuerfel.Add(Resources.Load<Sprite>("Images/GUI/würfel " + i));
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
        //StartCoroutine(ParseWuerfelResult(result));

        // aktiviere würfel falls keine reaktivierung kam
        yield return new WaitForSeconds(5);
        if (board.GetPlayerTurn().name == Config.PLAYER_NAME && board.GetPlayerTurn().availableDices > 0 && !Würfel.transform.GetChild(0).gameObject.activeInHierarchy)
        {
            Debug.LogWarning("AktiviereNachZeit");
            SendToServer("#AktiviereNachZeit");
        }
        yield break;
    }
    /// <summary>
    /// Generiert die Farben des Spielers in die Nachricht
    /// [C][C]Henryk[/COLOR] nachricht [C][C]Ron[/COLOR] Nur für 2 
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
    /// Client wählt ein Feld zum Laufen, sendet Anfrage an den Server
    /// </summary>
    /// <param name="FeldName"></param>
    public void ClientWähltFeld(GameObject FeldName)
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
        Logging.log(Logging.LogType.Debug, "MenschAergerDichNichtClient", "ClientWähltFeld", "Client wählt ein Feld zum Laufen: " + FeldName.name);
        ClientAllowZugWahl = false;

        SendToServer("#ClientWaehltFeld " + FeldName.name);
    }
    #endregion
}