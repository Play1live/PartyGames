using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NeandertalerClient : MonoBehaviour
{
    [SerializeField] AudioSource DisconnectSound;
    [SerializeField] AudioSource SpielerIstDran;
    [SerializeField] AudioSource DisplayX;
    [SerializeField] AudioSource FalschGeraten;
    [SerializeField] AudioSource Erraten;
    [SerializeField] AudioSource Beeep;
    [SerializeField] AudioSource Moeoop;

    private GameObject Karte;
    private GameObject[] TurnPlayer;
    private GameObject Skip;
    private GameObject Abbrechen;
    private GameObject RundeStarten;
    private GameObject HistoryContentElement;
    private GameObject[,] PlayerAnzeige;

    // Start is called before the first frame update
    void Start()
    {
        if (!Config.CLIENT_STARTED)
            return;
        ClientUtils.SendToServer("#JoinNeandertaler");
        InitGame();
        StartCoroutine(TestConnectionToServer());
    }

    // Update is called once per frame
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
        ClientUtils.SendToServer("#ClientFocusChange " + focus);
    }

    private void OnApplicationQuit()
    {
        Logging.log(Logging.LogType.Normal, "TabuClient", "OnApplicationQuit", "Client wird geschlossen.");
        ClientUtils.SendToServer("#ClientClosed");
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
        Logging.log(Logging.LogType.Debug, "TabuClient", "TestConnectionToServer", "Testet die Verbindumg zum Server.");
        while (Config.CLIENT_STARTED)
        {
            ClientUtils.SendToServer("#TestConnection");
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
    /// Einkommende Nachrichten die vom Sever
    /// </summary>
    /// <param name="data">Nachricht</param>
    private void OnIncomingData(string data)
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
        //Debug.LogWarning(cmd + "  ->  " + data);
        Logging.log(Logging.LogType.Debug, "TabuClient", "Commands", "Eingehende Nachricht: " + cmd + " -> " + data);
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "TabuClient", "Commands", "Unkown Command: " + cmd + " -> " + data);
                break;

            #region Universal Commands
            case "#ServerClosed":
                Logging.log(Logging.LogType.Normal, "TabuClient", "Commands", "Verbindumg zum Server wurde beendet. Lade ins Hauptmenü.");
                CloseSocket();
                SceneManager.LoadSceneAsync("Startup");
                break;
            case "#UpdateRemoteConfig":
                Logging.log(Logging.LogType.Normal, "TabuClient", "Commands", "RemoteConfig wird neugeladen");
                LoadConfigs.FetchRemoteConfig();
                break;
            case "#ZurueckInsHauptmenue":
                Logging.log(Logging.LogType.Normal, "TabuClient", "Commands", "Spiel wird beendet. Lade ins Hauptmenü");
                SceneManager.LoadSceneAsync("Startup");
                break;
            #endregion

            case "#UpdateSpieler":
                UpdateSpieler(data);
                break;
            case "#StartRunde":
                StartRunde(data);
                break;
            case "#RundeEnde":
                RundeEnde(data);
                break;
            case "#SkipWord":
                SkipWord(data);
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
    private void InitGame()
    {
        Karte = GameObject.Find("Spielbrett/Karte");
        Karte.GetComponentInChildren<TMP_Text>().text = "Loading...";
        Karte.SetActive(false);

        Skip = GameObject.Find("Spielbrett/Skip");
        Skip.SetActive(false);

        Abbrechen = GameObject.Find("Spielbrett/Abbrechen");
        Abbrechen.SetActive(false);

        TurnPlayer = new GameObject[3];
        TurnPlayer[0] = GameObject.Find("Spielbrett/TurnPlayer");
        TurnPlayer[1] = GameObject.Find("Spielbrett/TurnPlayer/BuzzerPressed");
        TurnPlayer[2] = GameObject.Find("Spielbrett/TurnPlayer/Icon");
        TurnPlayer[0].SetActive(false);

        RundeStarten = GameObject.Find("Spielbrett/RundeStarten");
        RundeStarten.SetActive(false);

        HistoryContentElement = GameObject.Find("WortHistory/Viewport/Content/Object*0");
        HistoryContentElement.SetActive(false);

        PlayerAnzeige = new GameObject[9, 6];
        for (int i = 0; i < 9; i++)
        {
            GameObject p = GameObject.Find("Player (" + (i + 1) + ")");
            PlayerAnzeige[i, 0] = p.transform.GetChild(0).gameObject; // ServerControll
            PlayerAnzeige[i, 0].SetActive(false);
            PlayerAnzeige[i, 1] = p.transform.GetChild(1).gameObject; // Buzzered
            PlayerAnzeige[i, 1].SetActive(false);
            PlayerAnzeige[i, 2] = p.transform.GetChild(2).gameObject; // Icon
            PlayerAnzeige[i, 3] = p.transform.GetChild(3).gameObject; // Ausgetabbt
            PlayerAnzeige[i, 3].SetActive(false);
            PlayerAnzeige[i, 4] = p.transform.GetChild(4).GetChild(1).gameObject; // Name
            PlayerAnzeige[i, 5] = p.transform.GetChild(4).GetChild(2).gameObject; // Punkte
            p.SetActive(false);
        }

        RundeStarten.SetActive(true);
    }

    private void UpdateSpieler(string data)
    {
        if (data.EndsWith("[DISCONNECT]"))
            DisconnectSound.Play();
        for (int i = 0; i < 9; i++)
            PlayerAnzeige[i, 0].SetActive(false);
        string[] stuff = data.Replace("[TRENNER]", "|").Split('|');
        foreach (var item in stuff)
        {
            int id = Int32.Parse(item.Replace("[ID]", "|").Split('|')[1]);
            string name = item.Replace("[NAME]", "|").Split('|')[1];
            string punkte = item.Replace("[PUNKTE]", "|").Split('|')[1];
            bool online = bool.Parse(item.Replace("[ONLINE]", "|").Split('|')[1]);

            PlayerAnzeige[id, 0].transform.parent.gameObject.SetActive(online);
            PlayerAnzeige[id, 5].GetComponent<TMP_Text>().text = punkte;
            PlayerAnzeige[id, 4].GetComponent<TMP_Text>().text = name;

            PlayerAnzeige[id, 2].GetComponent<Image>().sprite = Player.getPlayerIconByPlayerName(name).icon;
        }
        Logging.log(Logging.LogType.Debug, "NeandertalerClient", "UpdateSpieler", data);
    }

    public void ClientStartRunde()
    {
        if (!Config.CLIENT_STARTED)
            return;
        ClientUtils.SendToServer("#ClientStartRunde");
    }
    private void StartRunde(string data)
    {
        RundeStarten.SetActive(false);

        int id = Int32.Parse(data.Split('~')[0]);
        string wort = data.Split('~')[1];
        // Der dran ist
        if (id == Config.PLAYER_ID)
        {
            Skip.SetActive(true);
            Abbrechen.SetActive(true);
            Karte.SetActive(true);
        }
        Karte.GetComponentInChildren<TMP_Text>().text = wort;

        // Bei jedem
        TurnPlayer[2].GetComponent<Image>().sprite = PlayerAnzeige[id, 2].GetComponent<Image>().sprite;
        TurnPlayer[0].SetActive(true);
        for (int i = 0; i < Config.PLAYERLIST.Length + 1; i++)
        {
            PlayerAnzeige[i, 0].SetActive(false);
            if (id == Config.PLAYER_ID)
                PlayerAnzeige[i, 0].SetActive(true);
            PlayerAnzeige[i, 1].SetActive(false);
        }
        PlayerAnzeige[id, 0].SetActive(false);
        PlayerAnzeige[id, 1].SetActive(true);
    }

    public void ClientRundeEnde(TMP_Text name)
    {
        if (!Config.CLIENT_STARTED)
            return;
        ClientUtils.SendToServer("#ClientRundeEnde " + name.text);
    }
    public void ClientAbbrechen()
    {
        if (!Config.CLIENT_STARTED)
            return;
        ClientUtils.SendToServer("#ClientAbbrechen");
    }
    private void RundeEnde(string data)
    {
        int id = Int32.Parse(data.Split('~')[0]);
        Player p;
        if (Config.SERVER_PLAYER.id == id)
            p = Config.SERVER_PLAYER;
        else
            p = Config.PLAYERLIST[Player.getPosInLists(id)];
        string name = data.Split('~')[1];
        int index = Int32.Parse(data.Split('~')[2]);
        int nextid = Int32.Parse(data.Split('~')[3]);
        Player nextp;
        if (Config.SERVER_PLAYER.id == nextid)
            nextp = Config.SERVER_PLAYER;
        else
            nextp = Config.PLAYERLIST[Player.getPosInLists(nextid)];
        string wort = data.Split('~')[4];


        // UI
        Skip.SetActive(false);
        Abbrechen.SetActive(false);
        Karte.SetActive(false);
        for (int i = 0; i < Config.PLAYERLIST.Length + 1; i++)
        {
            PlayerAnzeige[i, 0].SetActive(false);
            PlayerAnzeige[i, 1].SetActive(false);
        }
        AddWortHistory(TurnPlayer[2].GetComponent<Image>().sprite, index + 1, Karte.GetComponentInChildren<TMP_Text>().text);

        // Punkte vergeben
        if (index == 0) // Jemand war Richtig
        {
            Erraten.Play();
            if (Config.SERVER_PLAYER.name == name)
            {
                Config.SERVER_PLAYER.points++;
                PlayerAnzeige[0, 5].GetComponent<TMP_Text>().text = Config.SERVER_PLAYER.points + "";
            }
            else
                foreach (var item in Config.PLAYERLIST)
                    if (item.name == name)
                    {
                        item.points++;
                        PlayerAnzeige[item.id, 5].GetComponent<TMP_Text>().text = item.points + "";
                    }
        }
        else if (index == 1) // Abbrechen
        {
            FalschGeraten.Play();
            if (Config.SERVER_PLAYER.id == id)
                Config.SERVER_PLAYER.points--;
            else
                foreach (var item in Config.PLAYERLIST)
                    if (item.id == id)
                        item.points--;
            PlayerAnzeige[id, 5].GetComponent<TMP_Text>().text = p.points + "";
        }

        // nächsten Spieler bestimmen
        TurnPlayer[2].GetComponent<Image>().sprite = nextp.icon2.icon;
        if (nextp.id == Config.PLAYER_ID)
        {
            Skip.SetActive(true);
            Abbrechen.SetActive(true);
            Karte.SetActive(true);
        }
        Karte.GetComponentInChildren<TMP_Text>().text = wort;
        // Bei jedem
        TurnPlayer[2].GetComponent<Image>().sprite = nextp.icon2.icon;
        TurnPlayer[0].SetActive(true);
        if (nextp.id == Config.PLAYER_ID)
        {
            for (int i = 0; i < Config.PLAYERLIST.Length + 1; i++)
            {
                PlayerAnzeige[i, 0].SetActive(true);
            }
        }
        PlayerAnzeige[nextp.id, 0].SetActive(false);
        PlayerAnzeige[nextp.id, 1].SetActive(true);
    }

    public void ClientSkip()
    {
        if (!Config.CLIENT_STARTED)
            return;
        ClientUtils.SendToServer("#Skip");
    }
    private void SkipWord(string data)
    {
        AddWortHistory(TurnPlayer[2].GetComponent<Image>().sprite, 0, Karte.GetComponentInChildren<TMP_Text>().text);

        Karte.GetComponentInChildren<TMP_Text>().text = data;
    }

    private void AddWortHistory(Sprite icon, int index, string wort)
    {
        Transform content = HistoryContentElement.transform.parent;

        GameObject newObject = GameObject.Instantiate(content.GetChild(0).gameObject, content, false);
        newObject.transform.localScale = new Vector3(1, 1, 1);
        newObject.name = "Object" + "*" + content.childCount;
        newObject.SetActive(true);
        newObject.transform.GetChild(0).GetComponent<Image>().sprite = icon;
        newObject.transform.GetChild(1).GetComponent<TMP_Text>().text = wort;
        if (index == 0) // Skip
        {
            newObject.transform.GetChild(2).GetChild(0).gameObject.SetActive(true);
        }
        else if (index == 1) // Richtig
        {
            newObject.transform.GetChild(2).GetChild(1).gameObject.SetActive(true);
        }
        StartCoroutine(ChangeWortHistoryText(newObject, wort));
    }
    private IEnumerator ChangeWortHistoryText(GameObject go, string data)
    {
        yield return null;
        go.transform.GetChild(1).GetComponent<TMP_Text>().text = data + " ";
        yield return null;
        go.transform.GetChild(1).GetComponent<TMP_Text>().text = data;
        yield break;
    }
}
