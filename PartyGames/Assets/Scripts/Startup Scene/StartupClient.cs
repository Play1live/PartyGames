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
    [SerializeField] GameObject[] MiniGames;
    private string ticktacktoe = "";
    private string ticktacktoeRes = "W0WL0LD0D";
    private bool PingWarteAufAntwort = false;

    [SerializeField] GameObject UmbenennenFeld;

    void OnEnable()
    {
        InitPlayerLobby();

        MiniGames[0].SetActive(true);
        #region Client Verbindungsaufbau zum Server
        if (!Config.CLIENT_STARTED)
        {
            // Create the socket
            try
            {
                Config.CLIENT_TCP = new TcpClient(Config.SERVER_CONNECTION_IP, Config.SERVER_CONNECTION_PORT);
                Config.CLIENT_STARTED = true;
                Logging.add(Logging.Type.Normal, "StartupClient", "Start", "Verbindung zum Server wurde hergestellt.");
                Config.HAUPTMENUE_FEHLERMELDUNG = "Verbindung zum Server wurde hergestellt.";
            }
            catch (Exception e)
            {
                Logging.add(Logging.Type.Fatal, "StartupClient", "Start", "Verbindung zum Server nicht möglich.", e);
                Config.HAUPTMENUE_FEHLERMELDUNG = "Verbindung zum Server nicht möglich. \n" + e;
                Config.CLIENT_STARTED = false;
                try
                {
                    CloseSocket();
                }
                catch (Exception e1)
                {
                    Logging.add(Logging.Type.Fatal, "StartupClient", "Start", "Socket konnte nicht geschlossen werden.", e1);
                    //ConnectingToServerLBL.GetComponent<TMP_Text>().text = ConnectingToServerLBL.GetComponent<TMP_Text>().text + "\n\nVerbindung zum Server nicht möglich." + e;
                }
                transform.gameObject.SetActive(false);
                Logging.add(Logging.Type.Normal, "StartupClient", "Start", "Client wird ins Hauptmenü geladen.");
                SceneManager.LoadSceneAsync("Startup");
                return;
            }
            // Verbindung erfolgreich
            Config.HAUPTMENUE_FEHLERMELDUNG = "";
        }
        else
        {
            Hauptmenue.SetActive(false);
            Lobby.SetActive(true);
            SendToServer("#GetSpielerUpdate");
        }
        #endregion

        StartCoroutine(TestConnectionToServer());
        StartCoroutine(TestIfStartConnectionError());
    }
    
    IEnumerator TestIfStartConnectionError()
    {
        yield return new WaitForSeconds(10);
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            if (SpielerAnzeigeLobby[i + 1].transform.GetChild(2).GetComponent<TMP_Text>().text == Config.PLAYER_NAME)
            {
                yield break;
            }
        }
        CloseSocket();
        Config.HAUPTMENUE_FEHLERMELDUNG = "Verbindung zum Server war fehlerhaft. Bitte versuche es erneut.";
        SceneManager.LoadSceneAsync("StartUp");
    }

    IEnumerator TestConnectionToServer()
    {
        while (Config.CLIENT_STARTED)
        {
            while (PingWarteAufAntwort)
            {
                yield return new WaitForSeconds(1);
            }
            // WaitForChangedResult???
            PingWarteAufAntwort = true;
            SendToServer("#TestConnection");
            Config.PingTime = DateTime.Now;
            // TODO: 5 sekunden warten bis antwort kam
            yield return new WaitForSeconds(5);
        }
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
        Logging.add(Logging.Type.Normal, "StartupClient", "OnApplicationQuit", "Client wird geschlossen.");
        SendToServer("#ClientClosed");
        CloseSocket();
    }

    #region Verbindungen
    /**
     * Trennt die Verbindung zum Server
     */
    private void CloseSocket()
    {
        if (!Config.CLIENT_STARTED)
            return;

        Config.CLIENT_TCP.Close();
        Config.CLIENT_STARTED = false;

        Logging.add(Logging.Type.Normal, "StartupClient", "CloseSocket", "Verbindung zum Server wurde getrennt. Client wird in das Hauptmenü geladen.");
    }
    #endregion

    #region Kommunikation
    /**
     * Sendet einen Command zum Server
     */
    public void SendToServer(string data)
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
            Logging.add(Logging.Type.Error, "Client", "SendToServer", "Nachricht an Server konnte nicht gesendet werden." + e);
            Config.HAUPTMENUE_FEHLERMELDUNG = "Verbindung zum Server wurde verloren.";
            CloseSocket();
            SceneManager.LoadSceneAsync("StartUp");
        }
    }
    /**
     * Eingehende Nachrichten vom Server
     */
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
    /**
     * Verarbeitet die Commands vom Server
     */
    public void Commands(string data, string cmd)
    {
        //Debug.Log("Eingehend: " + cmd + " -> " + data);
        switch (cmd)
        {
            default:
                Logging.add(Logging.Type.Warning, "StartupClient", "Commands", "Unkown Command -> " + cmd + " - " + data);
                break;

            #region Universal Commands
            case "#ServerClosed":
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
            #endregion

            case "#UpdateRemoteConfig":
                LoadConfigs.FetchRemoteConfig();
                break;
            case "#SetID":
                SetID(data);
                break;
            case "#WrongVersion":
                WrongVersion(data);
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
            case "#SpielerChangeName":
                SpielerChangeName(data);
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

    /**
     * Aktualisiert die eigene ID und die der anderen
     * #SetID <1-8>
     */
    private void SetID(String data)
    {
        int idparse;
        try
        {
            idparse = Int32.Parse(data);
        }
        catch (Exception e)
        {
            Logging.add(Logging.Type.Error, "StartupClient", "SetID", "ID konnte nicht geladen werden.", e);
            return;
        }
        // IDs festlegen
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            Config.PLAYERLIST[i].id = (i + 1);
        }
        Config.PLAYER_ID = idparse;

        /*
        Config.PLAYER_ID = idparse;
        idparse--;
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            Config.PLAYERLIST[i].id = (idparse + i) % Config.PLAYERLIST.Length + 1;
        }*/
        SendToServer("#ClientSetName [NAME]" + Config.PLAYER_NAME + "[NAME][VERSION]" + Config.APPLICATION_VERSION + "[VERSION]");

        Hauptmenue.SetActive(false);
        Lobby.SetActive(true);
    }
    /**
     * Ändert den Namen des Spielers
     */
    private void SpielerChangeName(string data)
    {
        Config.PLAYER_NAME = data;

        //ChangeIcon();
        SendToServer("#SpielerIconChange 0"); // Für namentliches Icon
    }
    /**
     * Beendet beitrittsversuche, wenn der Server eine andere Version hat
     * #WrongVersion <version>
     */
    private void WrongVersion(String data)
    {
        GameObject.Find("ConnectingToServer_LBL").gameObject.GetComponent<TMP_Text>().text = "Du versuchst mit einer falschen Version beizutreten.\n Deine Version: " + Config.APPLICATION_VERSION + "\n Benötigte Version: " + data;
        Logging.add(Logging.Type.Error, "StartupClient", "WrongVersion", "Du versuchst mit einer falschen Version beizutreten. Deine Version: " + Config.APPLICATION_VERSION + "- Benötigte Version: " + data);
        CloseSocket();
        SceneManager.LoadSceneAsync("StartUpScene");
    }
    /**
     *  Zeigt Namensänderung an
     */
    private void AllowNameChange(string data)
    {
        UmbenennenFeld.SetActive(Boolean.Parse(data));
    }
    /**
     * Sendet Namensänderung an Server
     */
    public void ChangePlayerName(TMP_InputField input)
    {
        if (Config.PLAYERLIST[Config.PLAYER_ID].name == input.text)
            return;
        SendToServer("#ChangePlayerName " + input.text);
    }
    /**
     * Sendet eine Iconeänderung an den Server
     */
    public void ChangeIcon()
    {
        SendToServer("#SpielerIconChange");
    }
    /**
     * Init Lobby Anzeigen
     */
    private void InitPlayerLobby()
    {
        // Für Server Host
        SpielerAnzeigeLobby[0].transform.GetChild(4).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/GUI/Top4");
        SpielerAnzeigeLobby[0].transform.GetChild(4).gameObject.SetActive(false);
        SpielerAnzeigeLobby[0].transform.GetChild(5).GetComponent<TMP_Text>().text = "";
        SpielerAnzeigeLobby[0].transform.GetChild(6).gameObject.SetActive(false);

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
    /**
     * Updated die Spieler
     * #UpdateSpieler
     */
    private void UpdateSpieler(String data)
    {
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
                Logging.add(Logging.Type.Error, "Client", "UpdateSpieler", "ID konnte nicht geladen werden.", e);
                return;
            }

            // Display ServerInfos
            if (id == 0)
            {
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
                    SpielerAnzeigeLobby[id].SetActive(true);
                    spieleranzahl++;
                }
                else
                {
                    SpielerAnzeigeLobby[id].SetActive(false);
                }
            }
            if (Lobby.activeInHierarchy)
                GameObject.Find("Lobby/Title_LBL/Spieleranzahl").GetComponent<TMP_Text>().text = spieleranzahl + "/" + (Config.PLAYERLIST.Length + 1);
        }
    }

    private void UpdatePing(string data)
    {
        SpielerAnzeigeLobby[0].transform.GetChild(3).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/Ping/" + data.Replace("[0]", "|").Split('|')[1]);
        foreach (Player p in Config.PLAYERLIST)
        {
            SpielerAnzeigeLobby[p.id].transform.GetChild(3).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/Ping/" + data.Replace("[" + p.id + "]", "|").Split('|')[1]);
        }
    }

    private void UpdateCrowns(string data)
    {
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

    /**
     * Lädt in die angegeben SpielScene
     */
    private void StarteSpiel(string data)
    {
        Logging.add(new Logging(Logging.Type.Normal, "Client", "StarteSpiel", "Spiel wird geladen: " + data));
        switch (data)
        {
            default:
                Logging.add(new Logging(Logging.Type.Fatal, "Client", "StarteSpiel", "Unbekanntes Spiel das geladen werden soll. Beende Verbindung"));
                SendToServer("#ClientClosed");
                CloseSocket();
                SceneManager.LoadSceneAsync("StartUpScene");
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
        }
    }

    #region MiniGames
    #region TickTackToe
    /**
     * Zeigt das TickTackToe Spiel an
     */
    private void SwitchToTickTackToe()
    {
        foreach (GameObject go in MiniGames)
            go.SetActive(false);

        MiniGames[0].SetActive(true);
    }
    /**
     * Startet TickTackToe
     */
    public void StartTickTackToe()
    {
        ticktacktoe = "";
        SendToServer("#StartTickTackToe");
    }
    /**
     * Zeigt den Zug des Servers an
     */
    private void TickTackToeZug(string data)
    {
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
    /**
     * TickTackToe ist beendet, Speichert statistik
     */
    private void TickTackToeZugEnde(string data)
    {
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
    /**
     * Macht einen Zug in TickTackToe 
     */
    public void TickTackToeButtonPress(GameObject button)
    {
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