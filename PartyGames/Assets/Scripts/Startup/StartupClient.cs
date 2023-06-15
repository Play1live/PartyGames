using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartupClient : MonoBehaviour
{
    [SerializeField] GameObject Controller;
    [SerializeField] GameObject Hauptmenue;
    [SerializeField] GameObject Lobby;
    [SerializeField] GameObject[] SpielerAnzeigeLobby;
    [SerializeField] GameObject SpielVorschauElemente;
    [SerializeField] GameObject[] MiniGames;
    private string ticktacktoe = "";
    private string ticktacktoeRes = "W0WL0LD0D";
    private bool PingWarteAufAntwort = false;
    private string UpdateClientGameVorschau = "";
    Coroutine UpdateClientGameVorschauCoroutine;
    private int connectedPlayer;

    [SerializeField] GameObject UmbenennenFeld;

    [SerializeField] AudioSource ConnectSound;
    [SerializeField] AudioSource DisconnectSound;

    void OnEnable()
    {
        InitPlayerLobby();

        MiniGames[0].transform.parent.parent.gameObject.SetActive(true);
        MiniGames[0].SetActive(true);
        #region Client Verbindungsaufbau zum Server
        if (!Config.CLIENT_STARTED)
        {
            StarteClient();
        }
        else
        {
            Hauptmenue.SetActive(false);
            Lobby.SetActive(true);
            SendToServer("#GetSpielerUpdate");
        }
        #endregion
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
        Logging.log(Logging.LogType.Normal, "StartupClient", "OnApplicationQuit", "Client wird geschlossen.");
        SendToServer("#ClientClosed");
        CloseSocket();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    public void ZurueckZumHauptmenue()
    {
        Logging.log(Logging.LogType.Normal, "StartupClient", "ZurueckZumHauptmenue", "Spieler wird ins Hauptmenü geladen und Server- & Client-Verbindung wird beendet.");
        if (!Config.isServer && Config.CLIENT_STARTED)
        {
            SendToServer("#ClientClosed");
            Config.CLIENT_TCP.Close();
            Config.CLIENT_STARTED = false;
            SceneManager.LoadSceneAsync("Startup");
            GameObject.Find("ClientController").gameObject.SetActive(false);
        }
    }
    /// <summary>
    /// Testet nach der Verbindung zum Server, ob die Verbindung erfolgreich und ohne Fehler funktioniert hat. 
    /// Falls die Verbindung fehlerhaft war, wird diese beendet und der Client lädt ins Hauptmenü.
    /// </summary>
    IEnumerator TestIfStartConnectionError()
    {
        string safename = Config.PLAYER_NAME;
        Logging.log(Logging.LogType.Normal, "StartupClient", "TestIfStartConnectionError", "Testet in 10 Sekunden (oder wenn die Spieleranzeige aktualisiert wird) ob die Verbindung erfolgreich war.");
        DateTime in10sec = DateTime.Now.AddSeconds(5);
        yield return new WaitForSeconds(1);
        // Spielerliste nicht aktuell, erfrage update
        if (!SearchNameInPlayerList(Config.PLAYER_NAME))
        {
            SendToServer("#GetSpielerUpdate"); // TODO geht nicht
        }
        yield return new WaitUntil(() => (DateTime.Compare(DateTime.Now, in10sec) > 0 || SearchNameInPlayerList(Config.PLAYER_NAME)));

        if (SearchNameInPlayerList(Config.PLAYER_NAME))
        {
            Logging.log(Logging.LogType.Normal, "StartupClient", "TestIfStartConnectionError", "Verbindung zum Server war erfolgreich.");
            yield break;
        }

        Config.PLAYER_NAME = safename;
        CloseSocket();
        Logging.log(Logging.LogType.Warning, "StartupClient", "TestIfStartConnectionError", "Verbindung zum Server war fehlerhaft.");
        Config.HAUPTMENUE_FEHLERMELDUNG = "Verbindung zum Server war fehlerhaft. Bitte versuche es erneut.\nWenn dieser Fehler erneut auftritt, starte das Programm bitte neu!";
        SceneManager.LoadSceneAsync("Startup");
    }
    /// <summary>
    /// Durchsucht die Spielerliste nach einem Namen
    /// </summary>
    /// <param name="name">Namen</param>
    /// <returns>true: Name ist enthalten; false: Name ist nicht enthalten</returns>
    private bool SearchNameInPlayerList(string name)
    {
        for (int i = 0; i <= Config.PLAYERLIST.Length; i++)
        {
            if (SpielerAnzeigeLobby[i].transform.GetChild(2).GetComponent<TMP_Text>().text.Equals(""))
                continue;
            if (SpielerAnzeigeLobby[i].transform.GetChild(2).GetComponent<TMP_Text>().text.Equals(name))
                return true;
        }
        return false;
    }
    /// <summary>
    /// Testet alle paar Sekunden die Verbindung zum Server. 
    /// Dient zugleich der Ping Berechnung und Aktualisierung.
    /// </summary>
    IEnumerator TestConnectionToServer()
    {
        yield return new WaitForSeconds(5);
        Logging.log(Logging.LogType.Debug, "StartupClient", "TestConnectionToServer", "Startet den Verbindungstest zum Server.");
        while (Config.CLIENT_STARTED)
        {
            yield return new WaitUntil(() => PingWarteAufAntwort == false);
            Logging.log(Logging.LogType.Debug, "StartupClient", "TestConnectionToServer", "Testet die Verbindung zum Server.");
            PingWarteAufAntwort = true;
            SendToServer("#TestConnection");
            Config.PingTime = DateTime.Now;
            yield return new WaitForSeconds(5);
        }
    }
    #region Verbindungen
    /// <summary>
    /// Startet den Client mit der Verbindung zum Server
    /// </summary>
    private void StarteClient()
    {
        Logging.log(Logging.LogType.Normal, "StartupClient", "StarteClient", "Verbindung zwischen Client und Server wird aufgebaut...");
        try
        {
            //Config.CLIENT_TCP = new TcpClient("localhost", Config.SERVER_CONNECTION_PORT);
            Config.CLIENT_TCP = new TcpClient(Config.SERVER_CONNECTION_IP, Config.SERVER_CONNECTION_PORT);
            Config.CLIENT_TCP.Client.NoDelay = true;
            Config.CLIENT_STARTED = true;
            Logging.log(Logging.LogType.Normal, "StartupClient", "StarteClient", "Verbindung wurde erfolgreich aufgebaut.");
            Config.HAUPTMENUE_FEHLERMELDUNG = "Verbindung zum Server wurde hergestellt.";
        }
        catch (Exception e)
        {
            Logging.log(Logging.LogType.Warning, "StartupClient", "StarteClient", "Verbindung zum Server nicht möglich.", e);
            Config.HAUPTMENUE_FEHLERMELDUNG = "Verbindung zum Server nicht möglich.\nStelle sicher das Daten zum Server richtig eingegeben sind.";
            Config.CLIENT_STARTED = false;
            try
            {
                CloseSocket();
            }
            catch (Exception e1)
            {
                Logging.log(Logging.LogType.Error, "StartupClient", "StarteClient", "Socket konnte nicht geschlossen werden.", e1);
            }
            transform.gameObject.SetActive(false);
            Logging.log(Logging.LogType.Normal, "StartupClient", "StarteClient", "Client wird ins Hauptmenü geladen.");
            SceneManager.LoadSceneAsync("Startup");
            return;
        }
        // Verbindung erfolgreich
        Config.HAUPTMENUE_FEHLERMELDUNG = "";
    }
    /// <summary>
    /// Trennt die Verbindung zum Server
    /// </summary>
    private void CloseSocket()
    {
        if (!Config.CLIENT_STARTED)
            return;

        Config.CLIENT_TCP.Close();
        Config.CLIENT_STARTED = false;

        Logging.log(Logging.LogType.Normal, "StartupClient", "CloseSocket", "Verbindung zum Server wurde getrennt. Client wird in das Hauptmenü geladen.");
    }
    #endregion
    #region Kommunikation
    /// <summary>
    /// Sendet einen Befehl zum Server
    /// </summary>
    /// <param name="data"></param>
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
            Logging.log(Logging.LogType.Warning, "StartupClient", "SendToServer", "Nachricht an Server konnte nicht gesendet werden. Nachricht: " + data, e);
            Config.HAUPTMENUE_FEHLERMELDUNG = "Verbindung zum Server wurde verloren.";
            CloseSocket();
            SceneManager.LoadSceneAsync("StartUp");
        }
    }
    /// <summary>
    /// Eingehende Nachrichten vom Server
    /// </summary>
    /// <param name="data">Eingehende Nachricht</param>
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
    /// Verarbeitet die Befehle vom Server
    /// </summary>
    /// <param name="data">Befehldaten</param>
    /// <param name="cmd">Befehl</param>
    private void Commands(string data, string cmd)
    {
        Logging.log(Logging.LogType.Debug, "StartupClient", "Commands", "Eingehende Nachricht vom Server: " + cmd + " -> " + data);
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "StartupClient", "Commands", "Unkown Command: " + cmd + " -> " + data);
                break;

            case "#ServerClosed":
                Logging.log(Logging.LogType.Normal, "StartupClient", "Commands", "Die Verbindung wurde vom Server beendet.");
                CloseSocket();
                Config.HAUPTMENUE_FEHLERMELDUNG = "Verbindung wurde Serverseitig beendet.";
                SceneManager.LoadSceneAsync("StartUp");
                break;
            case "#ConnectionEstablished":
                DateTime timenow = DateTime.Now;
                DateTime timebefore = Config.PingTime;
                int diffmillis = (timenow.Millisecond-timebefore.Millisecond) + (timenow.Second - timebefore.Second)*1000 + (timenow.Minute - timebefore.Minute)*1000*60 + (timenow.Hour - timebefore.Hour)*1000*60*60 + (timenow.Day - timebefore.Day)*1000*60*60*24;
                // Ping ist diffmillis / 2 (hin und rückweg)
                int ping = diffmillis / 2;
                SendToServer("#PlayerPing " + ping);
                PingWarteAufAntwort = false;
                break;

            case "#UpdateRemoteConfig":
                LoadConfigs.FetchRemoteConfig();
                break;
            case "#SetID":
                SetID(data);
                PlayConnectSound();
                break;
            case "#WrongVersion":
                StopCoroutine(TestIfStartConnectionError());
                WrongVersion(data);
                break;
            case "#ServerFull":
                StopCoroutine(TestIfStartConnectionError());
                ServerFull();
                break;
            case "#SpielerChangeName":
                SpielerChangeName(data);
                break;
            case "#UpdateSpieler":
                UpdateSpieler(data);
                break;
            case "#UpdateCrowns":
                UpdateCrowns(data);
                break;
            case "#UpdatePing":
                UpdatePing(data);
                break;
            case "#AllowNameChange":
                AllowNameChange(data);
                break;

            case "#StarteSpiel":
                StarteSpiel(data);
                break;

            // MiniGames
            case "#SwitchToTickTackToe":
                SwitchToTickTackToe();
                break;
            case "#TickTackToeZug":
                TickTackToeZug(data);
                break;
            case "#TickTackToeZugEnde":
                TickTackToeZugEnde(data);
                break;
        }
    }
    /// <summary>
    /// Aktualisiert die eigene ID und die der anderen
    /// </summary>
    /// <param name="data">#SetID <1-8></param>
    private void SetID(string data)
    {
        int idparse;
        try
        {
            idparse = Int32.Parse(data.Replace("[ID]", "|").Split('|')[1]);
        }
        catch (Exception e)
        {
            Logging.log(Logging.LogType.Error, "StartupClient", "SetID", "ID konnte nicht geladen werden. ID: "+ data, e);
            return;
        }
        // IDs festlegen
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            Config.PLAYERLIST[i].id = (i + 1);
        }
        Config.PLAYER_ID = idparse;

        UpdateClientGameVorschau = data.Replace("[GAMEFILES]", "|").Split('|')[1];
        if (UpdateClientGameVorschauCoroutine != null)
        {
            StopCoroutine(UpdateClientGameVorschauCoroutine);
            UpdateClientGameVorschauCoroutine = null;
        }
        UpdateClientGameVorschauCoroutine = StartCoroutine(ShowGameVorschauElemente());

        SendToServer("#ClientSetName [NAME]" + Config.PLAYER_NAME + "[NAME][VERSION]" + Config.APPLICATION_VERSION + "[VERSION]");

        Hauptmenue.SetActive(false);
        Lobby.SetActive(true);

        //StopCoroutine(TestIfStartConnectionError());
        StartCoroutine(TestIfStartConnectionError());
    }
    /// <summary>
    /// Ändert den Namen des Spielers
    /// </summary>
    /// <param name="data"></param>
    private void SpielerChangeName(string data)
    {
        Logging.log(Logging.LogType.Normal, "StartupClient", "SpielerChangeName", "Spielername ist nun: " + data);
        Config.PLAYER_NAME = data;

        SendToServer("#SpielerIconChange 0"); // Für namentliches Icon

        //StopCoroutine(TestConnectionToServer());
        StartCoroutine(TestConnectionToServer());
    }
    /// <summary>
    /// Beendet beitrittsversuche, wenn der Server eine andere Version hat
    /// </summary>
    /// <param name="data">#WrongVersion <version></param>
    private void WrongVersion(string data)
    {
        Logging.log(Logging.LogType.Warning, "StartupClient", "WrongVersion", "Client versucht mit einer falschen Version beizutreten. Deine Version: " + Config.APPLICATION_VERSION + "- Benötigte Version: " + data);
        Config.HAUPTMENUE_FEHLERMELDUNG = "Du versuchst mit einer falschen Version beizutreten.\n Deine Version: " + Config.APPLICATION_VERSION + "\n Benötigte Version: " + data;
        CloseSocket();
        SceneManager.LoadSceneAsync("Startup");
    }
    /// <summary>
    /// beendet den Beitrittsversuch, wenn der Server voll ist
    /// </summary>
    /// <param name="data"></param>
    private void ServerFull()
    {
        Logging.log(Logging.LogType.Warning, "StartupClient", "ServerFull", "Client versucht beizutreten. Server ist aber voll!");
        Config.HAUPTMENUE_FEHLERMELDUNG = "Der Server ist bereits voll.";
        CloseSocket();
        SceneManager.LoadSceneAsync("Startup");
    }
    /// <summary>
    /// Zeigt das Namensänderungsfeld an
    /// </summary>
    /// <param name="data"></param>
    private void AllowNameChange(string data)
    {
        Logging.log(Logging.LogType.Normal, "StartupClient", "AllowNameChange", "Namensänderungen sind nun möglich: " + data);
        UmbenennenFeld.SetActive(Boolean.Parse(data));
    }
    /// <summary>
    /// Sendet Namensänderunganfrage an Server
    /// </summary>
    /// <param name="input"></param>
    public void ChangePlayerName(TMP_InputField input)
    {
        if (Config.PLAYERLIST[Config.PLAYER_ID].name == input.text || input.text.Length == 0)
            return;
        Logging.log(Logging.LogType.Normal, "StartupClient", "ChangePlayerName", "Namens wechsel Anfrage: " + input.text);
        SendToServer("#ChangePlayerName " + input.text);
    }
    /// <summary>
    /// Sendet eine Iconeänderungsanfrage an den Server
    /// </summary>
    public void ChangeIcon()
    {
        Logging.log(Logging.LogType.Normal, "StartupClient", "ChangeIcon", "Icon wechsel Anfrage.");
        SendToServer("#SpielerIconChange");
    }
    /// <summary>
    /// Initialisiert die Lobbyanzeigen
    /// </summary>
    private void InitPlayerLobby()
    {
        Logging.log(Logging.LogType.Normal, "StartupClient", "InitPlayerLobby", "Spielerlobby wird initialisiert.");
        // Für Server Host
        SpielerAnzeigeLobby[0].transform.GetChild(4).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/GUI/Top4");
        SpielerAnzeigeLobby[0].transform.GetChild(4).gameObject.SetActive(false);
        SpielerAnzeigeLobby[0].transform.GetChild(5).GetComponent<TMP_Text>().text = "";
        SpielerAnzeigeLobby[0].transform.GetChild(6).gameObject.SetActive(false);
        connectedPlayer = 0;
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
            SpielerAnzeigeLobby[i + 1].transform.GetChild(6).gameObject.SetActive(false);
        }
    }
    /// <summary>
    /// Updated die Spieleranzeige
    /// </summary>
    /// <param name="data">#UpdateSpieler [ID]<0-8>[ID][NAMEN]<>[NAMEN][PUNKTE]<>[PUNKTE][ICON]<>[ICON][TRENNER]...</param>
    private void UpdateSpieler(string data)
    {
        Logging.log(Logging.LogType.Debug, "StartupClient", "UpdateSpieler", "Spieleranzeige wird aktualisiert. "+ data);

        string[] spieler = data.Replace("[TRENNER]", "|").Split('|');
        int spieleranzahl = 1;
        foreach (string sp in spieler)
        {
            int id;
            try
            {
                id = Int32.Parse(sp.Replace("[ID]", "|").Split('|')[1]);
            }
            catch (Exception e)
            {
                Logging.log(Logging.LogType.Error, "StartupClient", "UpdateSpieler", "ID konnte nicht geladen werden. Data: "+ data, e);
                return;
            }

            // Display ServerInfos
            if (id == 0)
            {
                Config.SERVER_ICON = Resources.Load<Sprite>("Images/ProfileIcons/" + sp.Replace("[ICON]", "|").Split('|')[1]);
                SpielerAnzeigeLobby[0].SetActive(true);
                SpielerAnzeigeLobby[0].GetComponentsInChildren<Image>()[1].sprite = Resources.Load<Sprite>("Images/ProfileIcons/" + sp.Replace("[ICON]", "|").Split('|')[1]);
                SpielerAnzeigeLobby[0].GetComponentsInChildren<TMP_Text>()[0].text = sp.Replace("[NAME]", "|").Split('|')[1];
            }
            // Display ClientInfos
            else
            {
                if (id == Config.PLAYER_ID)
                {
                    Config.PLAYER_NAME = sp.Replace("[NAME]", "|").Split('|')[1];
                }
                int pos = Player.getPosInLists(id);
                // Update PlayerInfos
                Config.PLAYERLIST[pos].name = sp.Replace("[NAME]", "|").Split('|')[1];
                Config.PLAYERLIST[pos].points = Int32.Parse(sp.Replace("[PUNKTE]", "|").Split('|')[1]);
                Config.PLAYERLIST[pos].icon = Resources.Load<Sprite>("Images/ProfileIcons/" + sp.Replace("[ICON]", "|").Split('|')[1]);
                // Display PlayerInfos                
                SpielerAnzeigeLobby[id].GetComponentsInChildren<Image>()[1].sprite = Config.PLAYERLIST[pos].icon;
                SpielerAnzeigeLobby[id].GetComponentInChildren<TMP_Text>().text = Config.PLAYERLIST[pos].name;
                if (Config.PLAYERLIST[pos].name != "")
                {
                    if (!SpielerAnzeigeLobby[id].activeInHierarchy)
                        PlayConnectSound();
                    
                    SpielerAnzeigeLobby[id].SetActive(true);
                    spieleranzahl++;
                }
                else
                {
                    if (SpielerAnzeigeLobby[id].activeInHierarchy)
                        PlayDisconnectSound();

                    SpielerAnzeigeLobby[id].SetActive(false);
                }
            }
            if (Lobby.activeInHierarchy)
                GameObject.Find("Lobby/Title_LBL/Spieleranzahl").GetComponent<TMP_Text>().text = spieleranzahl + "/" + (Config.PLAYERLIST.Length + 1);
        }

        if (spieleranzahl != connectedPlayer)
        {
            connectedPlayer = spieleranzahl;

            if (UpdateClientGameVorschauCoroutine != null)
                StopCoroutine(UpdateClientGameVorschauCoroutine);
            UpdateClientGameVorschauCoroutine = StartCoroutine(ShowGameVorschauElemente());
        }
    }
    /// <summary>
    /// Aktualisiert die Pinganzeige der Spieler
    /// </summary>
    /// <param name="data">Images der Pinganzeige aller Spieler</param>
    private void UpdatePing(string data)
    {
        Logging.log(Logging.LogType.Debug, "StartupClient", "UpdatePing", "Pinganzeigen wird aktualisiert. " + data);
        SpielerAnzeigeLobby[0].transform.GetChild(3).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/Ping/" + data.Replace("[0]", "|").Split('|')[1]);
        foreach (Player p in Config.PLAYERLIST)
        {
            SpielerAnzeigeLobby[p.id].transform.GetChild(3).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/Ping/" + data.Replace("[" + p.id + "]", "|").Split('|')[1]);
        }
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
    /// Aktualisiert die Kornenanzeige
    /// </summary>
    /// <param name="data">Jeweilige Kronenanzahl der Spieler</param>
    private void UpdateCrowns(string data)
    {
        Logging.log(Logging.LogType.Debug, "StartupClient", "UpdateCrowns", "Kronenanzeige wird aktualisiert. " + data);
        #region Speichert Zahlen
        // Server
        Config.SERVER_CROWNS = Int32.Parse(data.Replace("[0]","|").Split('|')[1]);
        foreach (Player p in Config.PLAYERLIST)
        {
            p.crowns = Int32.Parse(data.Replace("[" + p.id + "]", "|").Split('|')[1]);
        }
        #endregion


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
        }
        // Server
        if (Config.SERVER_CROWNS > top1)
        {
            top3 = top2;
            top2 = top1;
            top1 = Config.SERVER_CROWNS;
        }
        else if (Config.SERVER_CROWNS > top2)
        {
            top3 = top2;
            top2 = Config.SERVER_CROWNS;
        }
        else if (Config.SERVER_CROWNS > top3)
        {
            top3 = Config.SERVER_CROWNS;
        }
        #endregion


        // Keine Anzeigen wenn noch keiner Punkte hat
        if (top1 == 0)
        {
            SpielerAnzeigeLobby[0].transform.GetChild(5).GetComponent<TMP_Text>().text = "";
            for (int i = 0; i < Config.PLAYERLIST.Length; i++)
                SpielerAnzeigeLobby[i + 1].transform.GetChild(5).GetComponent<TMP_Text>().text = "";

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
            SpielerAnzeigeLobby[i + 1].transform.GetChild(5).GetComponent<TMP_Text>().text = "" + Config.PLAYERLIST[i].crowns;

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
        SpielerAnzeigeLobby[0].transform.GetChild(5).GetComponent<TMP_Text>().text = "" + Config.SERVER_CROWNS;

        if (Config.SERVER_CROWNS == top1)
            SpielerAnzeigeLobby[0].transform.GetChild(4).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/GUI/Top1");
        else if (Config.SERVER_CROWNS == top2)
            SpielerAnzeigeLobby[0].transform.GetChild(4).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/GUI/Top2");
        else if (Config.SERVER_CROWNS == top3)
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
        if (Config.SERVER_CROWNS > 0)
            SpielerAnzeigeLobby[0].transform.GetChild(4).gameObject.SetActive(true);
        else
        {
            SpielerAnzeigeLobby[0].transform.GetChild(5).GetComponent<TMP_Text>().text = "";
            SpielerAnzeigeLobby[0].transform.GetChild(4).gameObject.SetActive(false);
        }


        #endregion
    }
    /// <summary>
    /// [ANZ]<>[ANZ][0][0]....[9][SPIELER]<0-9>[SPIELER][TITEL]<..>[TITEL][AVAILABLE]<..>[AVAILABLE][9]
    /// Anz:     zeigt an wie viele Elemente eingeblendet werden
    /// [X]..[X] Enthält die Elemente
    /// [SPIELER]    blendet ein wie viele Spieler die Spiele spielen können & aus wenn zuviele gejoint sind
    /// [TITEL] enthält den angezeigten Text
    /// [AVAILABLE] enthält die Anzahl der verschiedenen Spiele, (wenn keine Verschiedenen [zb: UNO = 0])
    /// <summary>
    IEnumerator ShowGameVorschauElemente()
    {
        Logging.log(Logging.LogType.Normal, "StartupClient", "ShowGameVorschauElemente", "Aktualisiere Gamevorschau Anzeigen \n" + UpdateClientGameVorschau);
        string data = UpdateClientGameVorschau;
        if (data.Length == 0)
            yield break;
        
        int connected = 1;
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
            if (Config.PLAYERLIST[i].name.Length > 0)
                connected++;

        // Blendet alle Elemente aus
        for (int i = 0; i < SpielVorschauElemente.transform.childCount; i++)
        {
            SpielVorschauElemente.transform.GetChild(i).gameObject.SetActive(false);
            SpielVorschauElemente.transform.GetChild(i).GetChild(0).gameObject.SetActive(false);
            SpielVorschauElemente.transform.GetChild(i).GetChild(2).gameObject.SetActive(false);
        }
        // Blendet neue Infos ein
        int anz = Int32.Parse(data.Replace("[ANZ]", "|").Split('|')[1]);
        // Falls Anzahl größer als das was eingeblendet werden kann
        if (SpielVorschauElemente.transform.childCount < anz)
        {
            SendToServer("#ZuViele Spiele maximal einzublendende: " + anz + " Möglichkeiten: " + SpielVorschauElemente.transform.childCount);
            anz = SpielVorschauElemente.transform.childCount;
        }
        for (int i = 0; i < anz; i++)
        {
            //yield return null;
            string element = data.Replace("[" + i + "]", "|").Split('|')[1];
            string playeranz = element.Replace("[SPIELER-ANZ]", "|").Split('|')[1];
            int min = Int32.Parse(element.Replace("[MIN]", "|").Split('|')[1]);
            int max = Int32.Parse(element.Replace("[MAX]", "|").Split('|')[1]);
            string title = element.Replace("[TITEL]", "|").Split('|')[1];
            string available = element.Replace("[AVAILABLE]", "|").Split('|')[1];

            if (connected < min || connected > max)
                continue;

            SpielVorschauElemente.transform.GetChild(i).gameObject.SetActive(true);
            // SpielerAnzahl
            if (playeranz.Equals("0"))  // Ausblenden
            {
                SpielVorschauElemente.transform.GetChild(i).GetChild(0).gameObject.SetActive(false);
                SpielVorschauElemente.transform.GetChild(i).GetChild(0).GetComponentInChildren<TMP_Text>().text = playeranz;
            }
            else
            {
                SpielVorschauElemente.transform.GetChild(i).GetChild(0).gameObject.SetActive(true);
                SpielVorschauElemente.transform.GetChild(i).GetChild(0).GetComponentInChildren<TMP_Text>().text = playeranz;
            }

            // Titel
            SpielVorschauElemente.transform.GetChild(i).GetChild(1).GetComponent<TMP_Text>().text = title;

            //yield return null;
            // Available Einblendung
            if (available.Equals("0"))
            {
                SpielVorschauElemente.transform.GetChild(i).gameObject.SetActive(false);
            }
            else if (available.Equals("-1"))// Anzeigen ohne anzahl
            {
                SpielVorschauElemente.transform.GetChild(i).GetChild(2).gameObject.SetActive(false);
                SpielVorschauElemente.transform.GetChild(i).GetChild(2).GetComponentInChildren<TMP_Text>().text = "";
                SpielVorschauElemente.transform.GetChild(i).GetChild(2).gameObject.SetActive(false);
            }
            else
            {
                SpielVorschauElemente.transform.GetChild(i).GetChild(2).gameObject.SetActive(true);
                SpielVorschauElemente.transform.GetChild(i).GetChild(2).GetComponentInChildren<TMP_Text>().text = available;
            }
            yield return null;
        }
        yield return null;
        SpielVorschauElemente.transform.GetChild(0).GetChild(2).gameObject.SetActive(false);
        yield return null;
        SpielVorschauElemente.transform.GetChild(0).GetChild(2).gameObject.SetActive(true);
        yield break;
    }

    /// <summary>
    /// Lädt Spieler in die angegeben Spielscene
    /// </summary>
    /// <param name="data">Scenenname</param>
    private void StarteSpiel(string data)
    {
        Logging.log(Logging.LogType.Normal, "StartupClient", "StarteSpiel", "Spiel wird geladen: " + data);
        try
        {
            Config.GAME_TITLE = data;
            SceneManager.LoadScene(data);
        }
        catch (Exception e)
        {
            Logging.log(Logging.LogType.Error, "StartupClient", "StarteSpiel", "Unbekanntes Spiel soll geladen werden. Beende Verbindung. Spiel: " + data);
            SendToServer("#ClientClosed");
            CloseSocket();
            SceneManager.LoadSceneAsync("Startup");
        }
        
        /*switch (data)
        {
            default:
                Logging.log(Logging.LogType.Error, "StartupClient", "StarteSpiel", "Unbekanntes Spiel sooll geladen werden. Beende Verbindung. Spiel: " + data);
                SendToServer("#ClientClosed");
                CloseSocket();
                SceneManager.LoadSceneAsync("Startup");
                break;
            case "Flaggen":
                SceneManager.LoadScene(data);
                break;
            case "Quiz":
                SceneManager.LoadScene(data);
                break;
            case "Listen":
                SceneManager.LoadScene(data);
                break;
            case "Mosaik":
                SceneManager.LoadScene(data);
                break;
            case "Geheimwörter":
                SceneManager.LoadScene(data);
                break;
            case "WerBietetMehr":
                SceneManager.LoadScene(data);
                break;
            case "Auktion":
                SceneManager.LoadScene(data);
                break;
            case "Sloxikon":
                SceneManager.LoadScene(data);
                break;
        }*/
    }
    #region MiniGames
    #region TickTackToe
    /// <summary>
    /// Blendet das TickTackToe Spiel ein
    /// </summary>
    private void SwitchToTickTackToe()
    {
        Logging.log(Logging.LogType.Normal, "StartupClient", "SwitchToTickTackToe", "MiniSpiel wird zu TickTackToe gewechselt.");
        foreach (GameObject go in MiniGames)
            go.SetActive(false);

        MiniGames[0].transform.parent.gameObject.SetActive(true);
        MiniGames[0].SetActive(true);
    }
    /// <summary>
    /// Startet TickTackToe
    /// </summary>
    public void StartTickTackToe()
    {
        Logging.log(Logging.LogType.Normal, "StartupClient", "StartTickTackToe", "TickTackToe wird gestartet.");
        ticktacktoe = "";
        SendToServer("#StartTickTackToe");
    }
    /// <summary>
    /// Zeigt den Zug des Servers an
    /// </summary>
    /// <param name="data">Daten zum Zug des Servers</param>
    private void TickTackToeZug(string data)
    {
        Logging.log(Logging.LogType.Normal, "StartupClient", "TickTackToeZug", "TickTackToe Zug wird gewählt.");
        MiniGames[0].transform.GetChild(2).gameObject.SetActive(false);
        MiniGames[0].transform.GetChild(3).gameObject.SetActive(false);
        ticktacktoe = data;

        for (int i = 1; i <= 9; i++)
        {
            string feld = data.Replace("[" + i + "]", "|").Split('|')[1];
            if (feld == "X")
            {
                MiniGames[0].transform.GetChild(1).GetChild(i - 1).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/X");
            }
            else if (feld == "O")
            {
                MiniGames[0].transform.GetChild(1).GetChild(i - 1).GetComponent<Image>().sprite = Config.PLAYERLIST[Player.getPosInLists(Config.PLAYER_ID)].icon;
            }
            else
            {
                MiniGames[0].transform.GetChild(1).GetChild(i - 1).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/ProfileIcons/empty");
            }
        }
    }
    /// <summary>
    /// TickTackToe wird beendet. Statistik wird aktualisiert
    /// </summary>
    /// <param name="data">TickTackToe Result & Zug des Servers</param>
    private void TickTackToeZugEnde(string data)
    {
        Logging.log(Logging.LogType.Normal, "StartupClient", "TickTackToeZugEnde", "TickTackToe Spiel ist zuende.");
        // Save Result
        string result = data.Split('|')[1];
        data = data.Split('|')[2];
        int type = Int32.Parse(ticktacktoeRes.Split(result)[1])+1;
        ticktacktoeRes = ticktacktoeRes.Replace(result + (type - 1) + result, result + type + result);

        TickTackToeZug(data);
        MiniGames[0].transform.GetChild(2).gameObject.SetActive(true);
        MiniGames[0].transform.GetChild(3).gameObject.SetActive(true);
        MiniGames[0].transform.GetChild(4).GetComponent<TMP_Text>().text = "W:" + ticktacktoeRes.Split('W')[1] + " L:" + ticktacktoeRes.Split('L')[1] + " D:" + ticktacktoeRes.Split('D')[1];
    }
    /// <summary>
    /// Macht einen Zug in TickTackToe & sendet die daten an den Server
    /// </summary>
    /// <param name="button">Ausgewähltes Feld</param>
    public void TickTackToeButtonPress(GameObject button)
    {
        Logging.log(Logging.LogType.Normal, "StartupClient", "TickTackToeButtonPress", "Spieler zieht.");
        // Feld bereits belegt
        if (ticktacktoe.Replace("["+button.name+"]","|").Split('|')[1] != "")
        {
            return;
        }
        MiniGames[0].transform.GetChild(2).gameObject.SetActive(true);

        ticktacktoe = ticktacktoe.Replace("[" + button.name + "][" + button.name + "]", "["+ button.name + "]O[" + button.name + "]");
        SendToServer("#TickTackToeSpielerZug "+ ticktacktoe);
    }
    #endregion
    #endregion        
}