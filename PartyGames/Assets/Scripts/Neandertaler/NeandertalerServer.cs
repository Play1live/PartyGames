using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NeandertalerServer : MonoBehaviour
{
    int connectedcount;
    bool[] PlayerConnected;
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

    private string selectedItem;
    private List<Player> connectedP;

    // Start is called before the first frame update
    void Start()
    {
        PlayerConnected = new bool[Config.SERVER_MAX_CONNECTIONS + 1];
        PlayerConnected[0] = true;
        InitGame();
        connectedP = new List<Player>();
        connectedP.Add(Config.SERVER_PLAYER);
    }

    // Update is called once per frame
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
        Logging.log(Logging.LogType.Normal, "NeandertalerServer", "OnApplicationQuit", "Server wird geschlossen.");
        Config.SERVER_TCP.Server.Close();
    }

    private void OnApplicationFocus(bool focus)
    {
        ServerUtils.BroadcastImmediate("#ClientFocus 0~" + focus);
    }

    #region Server Stuff
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
    /// <summary>
    /// Einkommende Befehle von Spielern
    /// </summary>
    /// <param name="player">Spieler</param>
    /// <param name="data">Befehlsargumente</param>
    /// <param name="cmd">Befehl</param>
    private void Commands(Player player, string data, string cmd)
    {
        // Zeigt alle einkommenden Nachrichten an
        Logging.log(Logging.LogType.Debug, "NeandertalerServer", "Commands", "Eingehende Nachricht: " + player.name + " " + player.id + " -> " + cmd + "   ---   " + data);
        // Sucht nach Command
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "NeandertalerServer", "Commands", "Unkown Command: " + cmd + " -> " + data);
                break;

            case "#ClientClosed":
                DisconnectSound.Play();
                connectedcount = 0;
                ServerUtils.ClientClosed(player);
                UpdateSpielerBroadcast();
                break;
            case "#TestConnection":
                break;
            case "#ClientFocusChange":
                ServerUtils.BroadcastImmediate("#ClientFocus " + player.id + "~" + data);
                PlayerAnzeige[player.id, 3].SetActive(bool.Parse(data));
                break;

            case "#JoinNeandertaler":
                PlayerConnected[player.id] = true;
                connectedP.Add(player);
                UpdateSpielerBroadcast();
                break;
            case "#ClientStartRunde":
                ClientStartRunde(player);
                break;
            case "#ClientRundeEnde":
                ClientRundeEnde(player, data);
                break;
            case "#ClientAbbrechen":
                ClientAbbrechen(player);
                break;
            case "#Skip":
                SkipWord();
                break;
        }
    }
    #endregion
    /// <summary>
    /// Spiel Verlassen & Zurück in die Lobby laden
    /// </summary>
    public void SpielVerlassenButton()
    {
        Logging.log(Logging.LogType.Debug, "NeandertalerServer", "SpielVerlassenButton", "Spiel wird beendet. Lädt ins Hauptmenü.");
        Player[] plist = new Player[Config.PLAYERLIST.Length + 1];
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
            plist[i] = Config.PLAYERLIST[i];
        plist[Config.PLAYERLIST.Length] = Config.SERVER_PLAYER;
        ServerUtils.LoadKronen(plist);
        ServerUtils.BroadcastImmediate("#ZurueckInsHauptmenue");
    }

    #region GameLogic
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

        UpdateSpieler();
        RundeStarten.SetActive(true);
    }
    /// <summary>
    /// Sendet aktualisierte Spielerinfos an alle Spieler
    /// </summary>
    private void UpdateSpielerBroadcast()
    {
        ServerUtils.BroadcastImmediate(Config.GAME_TITLE + UpdateSpieler());
    }
    /// <summary>
    /// Aktualisiert die Spieler Anzeige Informationen & gibt diese als Text zurück
    /// </summary>
    /// <returns>#UpdateSpieler ...</returns>
    private string UpdateSpieler()
    {
        string msg = "#UpdateSpieler [ID]0[ID][PUNKTE]" + Config.SERVER_PLAYER.points + "[PUNKTE][ONLINE]true[ONLINE][NAME]"+ Config.SERVER_PLAYER.name + "[NAME]";
        int connectedplayer = 0;
        PlayerAnzeige[0, 0].transform.parent.gameObject.SetActive(true);
        PlayerAnzeige[0, 2].GetComponent<Image>().sprite = Config.SERVER_PLAYER.icon2.icon;
        PlayerAnzeige[0, 4].GetComponent<TMP_Text>().text = Config.SERVER_PLAYER.name;
        PlayerAnzeige[0, 5].GetComponent<TMP_Text>().text = Config.SERVER_PLAYER.points + "";

        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            Player p = Config.PLAYERLIST[i];
            msg += "[TRENNER][ID]" + p.id + "[ID][PUNKTE]" + p.points + "[PUNKTE][ONLINE]" + p.isConnected + "[ONLINE][NAME]"+p.name+"[NAME]";
            if (p.isConnected && PlayerConnected[i])
            {
                connectedplayer++;
                PlayerAnzeige[i+1, 0].transform.parent.gameObject.SetActive(true);
                PlayerAnzeige[i+1, 2].GetComponent<Image>().sprite = p.icon2.icon;
                PlayerAnzeige[i+1, 4].GetComponent<TMP_Text>().text = p.name;
                PlayerAnzeige[i+1, 5].GetComponent<TMP_Text>().text = p.points + "";
            }
            else
                PlayerAnzeige[i+1, 0].transform.parent.gameObject.SetActive(false);
        }

        if (connectedplayer < connectedcount)
        {
            msg = msg + "[DISCONNECT]";
            DisconnectSound.Play();
        }
        connectedcount = connectedplayer;
        Logging.log(Logging.LogType.Debug, "NeandertalerServer", "UpdateSpieler", msg);
        return msg;
    }

    private void ClientStartRunde(Player p)
    {
        StartRunde(p);
    }
    public void ServerStartRunde()
    {
        if (!Config.SERVER_STARTED)
            return;
        StartRunde(Config.SERVER_PLAYER);
    }
    private void StartRunde(Player p)
    {
        if (!RundeStarten.activeInHierarchy)
            return;
        RundeStarten.SetActive(false);
        selectedItem = Config.NEANDERTALER_SPIEL.getSelected().GetRandomItem(true);

        ServerUtils.BroadcastImmediate("#StartRunde " + p.id + "~" + selectedItem);

        // Der dran ist
        if (p.id == Config.SERVER_PLAYER.id || p.id == Config.PLAYER_ID)
        {
            Skip.SetActive(true);
            Abbrechen.SetActive(true);
            Karte.SetActive(true);
        }
        Karte.GetComponentInChildren<TMP_Text>().text = selectedItem;

        // Bei jedem
        TurnPlayer[2].GetComponent<Image>().sprite = p.icon2.icon;
        TurnPlayer[0].SetActive(true);
        for (int i = 0; i < Config.PLAYERLIST.Length + 1; i++)
        {
            PlayerAnzeige[i, 0].SetActive(false);
            if (p.id == Config.PLAYER_ID)
                PlayerAnzeige[i, 0].SetActive(true);
            PlayerAnzeige[i, 1].SetActive(false);
        }
        PlayerAnzeige[p.id, 0].SetActive(false);
        PlayerAnzeige[p.id, 1].SetActive(true);
    }
    
    private void ClientRundeEnde(Player p, string data)
    {
        RundeEnde(p, data, 0);
    }
    private void ClientAbbrechen(Player p)
    {
        RundeEnde(p, "", 1);
    }
    public void ServerRundeEnde(TMP_Text name)
    {
        if (!Config.SERVER_STARTED)
            return;
        RundeEnde(Config.SERVER_PLAYER, name.text, 0);
    }
    public void ServerAbbrechen()
    {
        if (!Config.SERVER_STARTED)
            return;
        RundeEnde(Config.SERVER_PLAYER, "", 1);
    }
    private void RundeEnde(Player p, string name, int index)
    {
        // UI
        Skip.SetActive(false);
        Abbrechen.SetActive(false);
        Karte.SetActive(false);
        for (int i = 0; i < Config.PLAYERLIST.Length + 1; i++)
        {
            PlayerAnzeige[i, 0].SetActive(false);
            PlayerAnzeige[i, 1].SetActive(false);
        }
        AddWortHistory(TurnPlayer[2].GetComponent<Image>().sprite, index + 1, selectedItem);
        // Punkte vergeben
        if (index == 0) // Jemand war Richtig
        {
            Erraten.Play();
            if (Config.SERVER_PLAYER.name == name)
            {
                Config.SERVER_PLAYER.points++;
                PlayerAnzeige[Config.SERVER_PLAYER.id, 5].GetComponent<TMP_Text>().text = Config.SERVER_PLAYER.points + "";
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
            if (Config.SERVER_PLAYER.id == p.id)
                Config.SERVER_PLAYER.points--;
            else 
                foreach (var item in Config.PLAYERLIST)
                    if (item.id == p.id)
                        item.points--;
            PlayerAnzeige[p.id, 5].GetComponent<TMP_Text>().text = p.points + "";
        }

        // nächsten Spieler bestimmen
        selectedItem = Config.NEANDERTALER_SPIEL.getSelected().GetRandomItem(true);
        Player next = connectedP[(connectedP.IndexOf(p) + 1) % connectedP.Count];
        TurnPlayer[2].GetComponent<Image>().sprite = next.icon2.icon;
        if (next.id == Config.SERVER_PLAYER.id)
        {
            Skip.SetActive(true);
            Abbrechen.SetActive(true);
            Karte.SetActive(true);
        }
        Karte.GetComponentInChildren<TMP_Text>().text = selectedItem;
        // Bei jedem
        TurnPlayer[2].GetComponent<Image>().sprite = next.icon2.icon;
        TurnPlayer[0].SetActive(true);
        if (next == Config.SERVER_PLAYER)
        {
            for (int i = 0; i < Config.PLAYERLIST.Length + 1; i++)
            {
                PlayerAnzeige[i, 0].SetActive(true);
            }
        }
        PlayerAnzeige[next.id, 0].SetActive(false);
        PlayerAnzeige[next.id, 1].SetActive(true);

        ServerUtils.BroadcastImmediate("#RundeEnde " + p.id + "~" + name + "~" + index + "~" + next.id + "~" + selectedItem);
    }

    public void ServerSkip()
    {
        if (!Config.SERVER_STARTED)
            return;
        SkipWord();
    }
    private void SkipWord()
    {
        AddWortHistory(TurnPlayer[2].GetComponent<Image>().sprite, 0, selectedItem);

        selectedItem = Config.NEANDERTALER_SPIEL.getSelected().GetRandomItem(true);
        ServerUtils.BroadcastImmediate("#SkipWord " + selectedItem);
        Karte.GetComponentInChildren<TMP_Text>().text = selectedItem;
    }
    #endregion
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
