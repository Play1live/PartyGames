using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
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

    [SerializeField] GameObject UmbenennenFeld;

    void OnEnable()
    {
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

        NetworkStream stream = Config.CLIENT_TCP.GetStream();
        StreamWriter writer = new StreamWriter(stream);
        writer.WriteLine(data);
        writer.Flush();
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
        Debug.Log("Eingehend: " + cmd + " -> " + data);
        switch (cmd)
        {
            default:
                Logging.add(Logging.Type.Warning, "StartupClient", "Commands", "Unkown Command -> " + cmd + " - " + data);
                break;

            #region Universal Commands
            case "#ServerClosed":
                CloseSocket();
                SceneManager.LoadSceneAsync("StartUp");
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
        Config.PLAYER_ID = idparse;
        idparse--;
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            Config.PLAYERLIST[i].id = (idparse + i) % Config.PLAYERLIST.Length + 1;
        }
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