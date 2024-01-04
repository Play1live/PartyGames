using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Windows;

public class SabotageServer : MonoBehaviour
{
    bool[] PlayerConnected;
    int connectedPlayers;
    [SerializeField] AudioSource Beeep;
    [SerializeField] AudioSource Moeoop;
    [SerializeField] AudioSource DisconnectSound;
    [SerializeField] AudioSource Buzzer;
    [SerializeField] AudioSource Correct;
    [SerializeField] AudioSource Wrong;

    SabotagePlayer[] sabotagePlayers;
    GameObject WerIstSabo;

    GameObject Lobby;
    Slider LobbyTimer;

    GameObject SaboteurWahlAufloesung;
    GameObject SaboteurWahlAufloesungAbstimmung;
    GameObject SaboteurWahlAufloesungPunkteverteilung;
    Slider AbstimmungTimer;

    GameObject Diktat;
    TMP_InputField DiktatLoesung;
    Slider DiktatTimer;

    GameObject Sortieren;
    Slider SortierenTimer;
    GameObject SortierenListe;
    GameObject SortierenAuswahl;
    GameObject SortierenLoesung;

    GameObject DerZugLuegt;
    GameObject DerZugLuegtAnzeigen;

    GameObject Tabu;
    Slider TabuTimer;

    GameObject Auswahlstrategie;
    Transform AuswahlstrategieGrid;
    Slider AuswahlstrategieTimer;

    void OnEnable()
    {
        PlayerConnected = new bool[Config.SERVER_MAX_CONNECTIONS];
        InitAnzeigen();
        int onlinePlayer = 0;
        foreach (var item in Config.PLAYERLIST)
            if (item.isConnected)
                onlinePlayer++;
        if (onlinePlayer > 5)
        {
            SceneManager.LoadSceneAsync("Startup");
            return;
        }
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
        Logging.log(Logging.LogType.Normal, "SabotageServer", "OnApplicationQuit", "Server wird geschlossen.");
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
        Logging.log(Logging.LogType.Debug, "SabotageServer", "Commands", "Eingehende Nachricht: " + player.name + " " + player.id + " -> " + cmd + "   ---   " + data);
        // Sucht nach Command
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "SabotageServer", "Commands", "Unkown Command: " + cmd + " -> " + data);
                break;

            case "#ClientClosed":
                ServerUtils.ClientClosed(player);
                UpdateSpielerBroadcast();
                DisconnectSound.Play();
                break;
            case "#TestConnection":
                break;
            case "#JoinSabotage":
                PlayerConnected[player.id - 1] = true;
                sabotagePlayers[player.id - 1].UpdateImage();
                UpdateSpielerBroadcast();
                break;
            case "#ClientFocusChange":
                ServerUtils.BroadcastImmediate("#ClientFocusChange " + player.id + "*" + data);
                Config.SABOTAGE_SPIEL.getPlayerByPlayer(sabotagePlayers, player).SetAusgetabbt(!bool.Parse(data));
                break;

            case "#LobbyClientplazedTokens":
                LobbyClientplazedTokens(player, data);
                break;

            case "#DiktatPlayerInput":
                DiktatPlayerInput(player, data);
                break;

            case "#DerZugLuegtBuzzer":
                DerZugLuegtClientBuzzer(player);
                break;

            case "#ClientStimmtFuer":
                ClientStimmtFuer(player, data); 
                break;
            case "#ClientStimmtGegen":
                ClientStimmtGegen(player, data);
                break;
        }
    }
    #endregion
    /// <summary>
    /// Spiel Verlassen & Zurück in die Lobby laden
    /// </summary>
    public void SpielVerlassenButton()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "SpielVerlassenButton", "Spiel wird beendet. Lädt ins Hauptmenü.");
        //SceneManager.LoadScene("Startup");
        foreach (var player in sabotagePlayers)
            player.player.points = player.points;
        ServerUtils.LoadKronen(Config.PLAYERLIST);
        ServerUtils.BroadcastImmediate(Config.GLOBAL_TITLE + "#ZurueckInsHauptmenue");
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
        string msg = "#UpdateSpieler ";
        int connectedplayer = 0;
        for (int i = 0; i < sabotagePlayers.Length; i++)
        {
            msg += "[" + sabotagePlayers[i].player.id + "][PUNKTE]" + sabotagePlayers[i].points + "[PUNKTE][ONLINE]" + sabotagePlayers[i].player.isConnected + "[ONLINE][" + sabotagePlayers[i].player.id + "]";
            sabotagePlayers[i].SetPunkte(sabotagePlayers[i].points);

            if (sabotagePlayers[i].player.isConnected && PlayerConnected[i])
                connectedplayer++;
            else
                sabotagePlayers[i].DeleteImage();
        }
        if (connectedplayer < connectedPlayers)
            connectedPlayers = connectedplayer;
        Logging.log(Logging.LogType.Debug, "SabotageServer", "UpdateSpieler", msg);
        return msg;
    }
    public void ChangePlayerPoints(TMP_InputField input)
    {
        if (!Config.SERVER_STARTED)
            return;
        Logging.log(Logging.LogType.Debug, "SabotageServer", "ChangePlayerPoints", input.text);
        int playerid = int.Parse(input.transform.parent.parent.name.Replace("Player (", "").Replace(")", ""));
        sabotagePlayers[playerid - 1].SetPunkte(int.Parse(input.text));

        UpdateSpielerBroadcast();
    }
    /// <summary>
    /// Initialisiert die Anzeigen zu beginn
    /// </summary>
    private void InitAnzeigen()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "InitAnzeigen", "Initialisiert die Anzeigen...");
        connectedPlayers = 0;
        Transform modi = GameObject.Find("Modi").transform;
        for (int i = 0; i < modi.childCount; i++)
            modi.GetChild(i).gameObject.SetActive(true);

        // Allgemein
        sabotagePlayers = new SabotagePlayer[5];
        for (int i = 0; i < sabotagePlayers.Length; i++)
        {
            sabotagePlayers[i] = new SabotagePlayer(Config.PLAYERLIST[i], GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")"));
        }

        // SaboteurAnzeige
        WerIstSabo = GameObject.Find("SpielerAnzeigen/WerIstSaboteur");
        WerIstSabo.SetActive(false);
        WerIstSabo.transform.GetChild(0).GetComponent<TMP_Text>().text = "Keiner";
        WerIstSabo.transform.GetChild(1).GetComponent<TMP_Text>().text = "Du bist alleine";

        // Lobby
        Lobby = GameObject.Find("Lobby");
        Lobby.SetActive(true);
        LobbyTimer = GameObject.Find("Lobby/Timer").GetComponent<Slider>();
        LobbyTimer.maxValue = 1;
        LobbyTimer.minValue = 0;
        LobbyTimer.value = 0;
        LobbyTimer.gameObject.SetActive(false);
        GameObject.Find("Lobby/Server/StartSpielIndex").GetComponent<TMP_Text>().text = "Starte Spiel: " + Config.SABOTAGE_SPIEL.spielindex;
        GameObject.Find("Punktetafel/SaboteurPunkte").GetComponent<TMP_InputField>().interactable = true;
        GameObject.Find("Punktetafel/TeamPunkte").GetComponent<TMP_InputField>().interactable = true;
        GameObject temp = GameObject.Find("Lobby/Client");
        if (temp != null)
            temp.gameObject.SetActive(false);

        // SaboteurWahl & Aufloesung
        SaboteurWahlAufloesung = GameObject.Find("SaboteurWahl&Aufloesung");
        modi = SaboteurWahlAufloesung.transform;
        for (int i = 0; i < modi.childCount-1; i++)
            modi.GetChild(i).gameObject.SetActive(true);
        SaboteurWahlAufloesung.SetActive(false);
        SaboteurWahlAufloesungAbstimmung = SaboteurWahlAufloesung.transform.GetChild(0).gameObject;
        SaboteurWahlAufloesungAbstimmung.SetActive(false);
        SaboteurWahlAufloesungPunkteverteilung = SaboteurWahlAufloesung.transform.GetChild(1).gameObject;
        SaboteurWahlAufloesungPunkteverteilung.SetActive(false);
        AbstimmungTimer = SaboteurWahlAufloesungAbstimmung.transform.GetChild(2).GetComponent<Slider>();
        AbstimmungTimer.maxValue = 1;
        AbstimmungTimer.minValue = 0;
        AbstimmungTimer.value = 0;
        AbstimmungTimer.gameObject.SetActive(false);

        // Diktat
        DiktatTimer = GameObject.Find("Diktat/Timer").GetComponent<Slider>();
        DiktatTimer.maxValue = 1;
        DiktatTimer.minValue = 0;
        DiktatTimer.value = 0;
        DiktatTimer.gameObject.SetActive(false);
        DiktatLoesung = GameObject.Find("Diktat/LösungsText").GetComponent<TMP_InputField>();
        DiktatLoesung.gameObject.SetActive(false);
        Diktat = GameObject.Find("Modi/Diktat");
        Diktat.SetActive(false);

        // Sortieren
        SortierenTimer = GameObject.Find("Sortieren/Timer").GetComponent<Slider>();
        SortierenTimer.maxValue = 1;
        SortierenTimer.minValue = 0;
        SortierenTimer.value = 0;
        SortierenListe = GameObject.Find("Sortieren/Liste");
        SortierenListe.gameObject.SetActive(false);
        SortierenAuswahl = GameObject.Find("Sortieren/Auswahl");
        SortierenAuswahl.gameObject.SetActive(false);
        SortierenLoesung = GameObject.Find("Sortieren/LösungListe");
        SortierenLoesung.gameObject.SetActive(false);
        Sortieren = GameObject.Find("Modi/Sortieren");
        Sortieren.SetActive(false);

        // DerZugLuegt
        DerZugLuegtAnzeigen = GameObject.Find("DerZugLuegt/GameObject");
        DerZugLuegtAnzeigen.SetActive(false);
        DerZugLuegt = GameObject.Find("Modi/DerZugLuegt");
        DerZugLuegt.gameObject.SetActive(false);

        // Tabu
        TabuTimer = GameObject.Find("Tabu/Timer").GetComponent<Slider>();
        TabuTimer.maxValue = 1;
        TabuTimer.minValue = 0;
        TabuTimer.value = 0;
        Tabu = GameObject.Find("Modi/Tabu");
        Tabu.gameObject.SetActive(false);

        // Auswahlstrategie
        AuswahlstrategieTimer = GameObject.Find("Auswahlstrategie/Timer").GetComponent<Slider>();
        AuswahlstrategieTimer.maxValue = 1;
        AuswahlstrategieTimer.minValue = 0;
        AuswahlstrategieTimer.value = 0;
        AuswahlstrategieGrid = GameObject.Find("Auswahlstrategie/Grid").transform;
        AuswahlstrategieGrid.gameObject.SetActive(false);
        Auswahlstrategie = GameObject.Find("Modi/Auswahlstrategie");
        Auswahlstrategie.gameObject.SetActive(false);
    }

    #region Lobby
    public void LobbyPlayerSeeAllPointsToggle(Toggle toggle)
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "LobbyPlayerSeeAllPointsToggle", toggle.isOn+"");
        ServerUtils.BroadcastImmediate("#LobbyPlayerSeeAllPointsToggle " + toggle.isOn);
    }
    public void LobbyChangeSpielIndex(int change)
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "LobbyChangeSpielIndex", change+"");
        if (change == -1 && Config.SABOTAGE_SPIEL.spielindex <= 0)
            Config.SABOTAGE_SPIEL.spielindex = 0;
        else
            Config.SABOTAGE_SPIEL.spielindex += change;
        GameObject.Find("Lobby/Server/StartSpielIndex").GetComponent<TMP_Text>().text = "Starte Spiel: " + Config.SABOTAGE_SPIEL.spielindex + "/6";
        // TODO: Animiere den Spielstart
    }
    public void UpdateTeamSaboPunkte()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "UpdateTeamSaboPunkte", "");
        ServerUtils.BroadcastImmediate("#UpdateTeamSaboPunkte " + GameObject.Find("Punktetafel/SaboteurPunkte").GetComponent<TMP_InputField>().text + "|" + GameObject.Find("Punktetafel/TeamPunkte").GetComponent<TMP_InputField>().text);
    }
    Coroutine lobbytokens;
    public void LobbyStartTokenPlazierungen()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "LobbyStartTokenPlazierungen", "");
        string playertokens = "";
        foreach (var item in sabotagePlayers)
            playertokens += "~" + item.saboteurTokens;
        if (playertokens.Length > 0)
            playertokens = playertokens.Substring(1);
        int timerseconds = 12;
        ServerUtils.BroadcastImmediate("#LobbyStartTokenPlacement " + timerseconds  + "|" + playertokens);

        if (lobbytokens != null)
            StopCoroutine(lobbytokens);
        lobbytokens = StartCoroutine(LobbyRunTimer(timerseconds)); // 120
    }
    bool lobbyblocktokens;
    IEnumerator LobbyRunTimer(int seconds)
    {
        yield return null;
        lobbyblocktokens = false;
        LobbyTimer.minValue = 0;
        LobbyTimer.maxValue = seconds * 1000; // Umrechnung in Millisekunden
        LobbyTimer.gameObject.SetActive(true);
        int milis = seconds * 1000;

        Logging.log(Logging.LogType.Debug, "SabotageServer", "LobbyRunTimer", "Seconds: " + seconds + "  Timer started: " + DateTime.Now.ToString("HH:mm:ss:ffff"));
        while (milis >= 0)
        {
            LobbyTimer.GetComponentInChildren<Slider>().value = milis;

            if (milis <= 0)
            {
                Beeep.Play();
            }
            // Moep Sound bei Sekunden
            if (milis == 1000 || milis == 2000 || milis == 3000)
            {
                Moeoop.Play();
            }
            yield return new WaitForSecondsRealtime(0.1f); // Alle 100 Millisekunden warten
            milis -= 100;
        }
        Logging.log(Logging.LogType.Debug, "SabotageServer", "LobbyRunTimer", "Seconds: " + seconds + "  Timer ended: " + DateTime.Now.ToString("HH:mm:ss:ffff"));

        yield return new WaitForSeconds(4f);
        lobbyblocktokens = true;
        yield return new WaitForSeconds(1f);
        for (int i = 0; i < sabotagePlayers.Length; i++)
        {
            sabotagePlayers[i].saboteurTokens -= sabotagePlayers[i].placedTokens;
            Logging.log(Logging.LogType.Normal, "SabotageServer", "LobbyRunTimer", sabotagePlayers[i].player.name + ": " + sabotagePlayers[i].placedTokens);
        }
        LobbyTimer.gameObject.SetActive(false);
        yield break;
    }
    private void LobbyClientplazedTokens(Player p, string data)
    {
        if (lobbyblocktokens)
            return;
        Logging.log(Logging.LogType.Debug, "SabotageServer", "LobbyClientplazedTokens", p.name + " - " + data);
        sabotagePlayers[p.id - 1].placedTokens = int.Parse(data);
    }
    public void LobbyGenSaboteurs(TMP_InputField input)
    {
        if (input.text.Length == 0 || input.text.Equals("0"))
            return;
        Logging.log(Logging.LogType.Debug, "SabotageServer", "LobbyClientplazedTokens", input.text);

        foreach (var item in sabotagePlayers)
            item.SetSaboteur(false);

        GenSaboteurForRound(int.Parse(input.text));
        string names = "";
        foreach (var item in sabotagePlayers)
        {
            if (item.isSaboteur)
                names += "\n" + item.player.name;
        }
        if (names.Length > 0)
            names = names.Substring("\n".Length);
        WerIstSabo.transform.GetChild(0).GetComponent<TMP_Text>().text = names;

        ServerUtils.BroadcastImmediate("#DuBistSaboteur " + names.Replace("\n", "~"));
    }
    #endregion
    #region Wahl & Abstimmung
    private void StartWahlAbstimmung()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "StartWahlAbstimmung", "");
        // Blende alles aus
        Transform modi = GameObject.Find("Modi").transform;
        for (int i = 0; i < modi.childCount; i++)
            modi.GetChild(i).gameObject.SetActive(false);

        SaboteurWahlAufloesung.SetActive(true);
        SaboteurWahlAufloesung.transform.GetChild(2).gameObject.SetActive(true);
        SaboteurWahlAufloesungAbstimmung.SetActive(false);
        SaboteurWahlAufloesungPunkteverteilung.SetActive(false);
        abstimmungClientStimme = new int[5, 5];
    }
    // Abstimmung
    public void AbstimmungStart()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "AbstimmungStart", "");
        ServerUtils.BroadcastImmediate("#AbstimmungStart");

        SaboteurWahlAufloesungAbstimmung.SetActive(true);
        SaboteurWahlAufloesungPunkteverteilung.SetActive(false);
        SaboteurWahlAufloesungAbstimmung.transform.GetChild(0).gameObject.SetActive(false);
        SaboteurWahlAufloesungAbstimmung.transform.GetChild(1).gameObject.SetActive(false);

        for (int i = 0; i < sabotagePlayers.Length; i++)
        {
            GameObject.Find("SaboteurWahl&Aufloesung/Abstimmung/Server/Icon (" + (sabotagePlayers[i].player.id) +")").
                GetComponent<Image>().sprite = sabotagePlayers[i].player.icon2.icon;
            GameObject.Find("SaboteurWahl&Aufloesung/Abstimmung/Server/Icon (" + (sabotagePlayers[i].player.id) + ")").
                GetComponentInChildren<TMP_Text>().text = "0";
            GameObject.Find("SaboteurWahl&Aufloesung/Abstimmung/Server/Icon (" + (sabotagePlayers[i].player.id) + ")").
                GetComponentInChildren<Button>().interactable = false;
        }
        abstimmungClientStimme = new int[5, 5];
    }
    public void AbstimmungRunTimer(TMP_InputField input)
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "AbstimmungRunTimer", "");
        ServerUtils.BroadcastImmediate("#AbstimmungRunTimer " + input.text);
        StartCoroutine(AbstimmungRunTimer(int.Parse(input.text)));
    }
    IEnumerator AbstimmungRunTimer(int seconds)
    {
        yield return null;
        AbstimmungTimer.minValue = 0;
        AbstimmungTimer.maxValue = seconds * 1000; // Umrechnung in Millisekunden
        AbstimmungTimer.gameObject.SetActive(true);
        int milis = seconds * 1000;

        Logging.log(Logging.LogType.Debug, "SabotageServer", "AbstimmungRunTimer", "Seconds: " + seconds + "  Timer started: " + DateTime.Now.ToString("HH:mm:ss:ffff"));
        while (milis >= 0)
        {
            AbstimmungTimer.GetComponentInChildren<Slider>().value = milis;

            if (milis <= 0)
            {
                Beeep.Play();
            }
            // Moep Sound bei Sekunden
            if (milis == 1000 || milis == 2000 || milis == 3000)
            {
                Moeoop.Play();
            }
            yield return new WaitForSecondsRealtime(0.1f); // Alle 100 Millisekunden warten
            milis -= 100;
        }
        Logging.log(Logging.LogType.Debug, "SabotageServer", "AbstimmungRunTimer", "Seconds: " + seconds + "  Timer ended: " + DateTime.Now.ToString("HH:mm:ss:ffff"));

        AbstimmungTimer.gameObject.SetActive(false);
        yield return new WaitForSeconds(2f);

        yield break;
    }
    int[,] abstimmungClientStimme;
    private void ClientStimmtFuer(Player p, string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "ClientStimmtFuer", p.name + " - " + data);
        abstimmungClientStimme[p.id - 1, int.Parse(data)] = 1;
        int[] votes = new int[5];
        for (int i = 0; i < 5; i++) // Player
        {
            for (int j = 0; j < 5; j++) // Votes
            {
                votes[i] += abstimmungClientStimme[i, j];
            }
            GameObject.Find("SaboteurWahl&Aufloesung/Abstimmung/Server/Icon (" + (i + 1) + ")").
                GetComponentInChildren<TMP_Text>().text = "" + votes[i];
        }
    }
    private void ClientStimmtGegen(Player p, string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "ClientStimmtGegen", p.name + " - " + data);
        abstimmungClientStimme[p.id - 1, int.Parse(data)] = 0;
        int[] votes = new int[5];
        for (int i = 0; i < 5; i++) // Player
        {
            for (int j = 0; j < 5; j++) // Votes
            {
                votes[i] += abstimmungClientStimme[i, j];
            }
            GameObject.Find("SaboteurWahl&Aufloesung/Abstimmung/Server/Icon (" + (i + 1) + ")").
                GetComponentInChildren<TMP_Text>().text = "" + votes[i];
        }
    }
    string aufloesungBonusPunkte;
    public void AufloesungStart()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "AufloesungStart", "");
        SaboteurWahlAufloesungAbstimmung.SetActive(false);
        SaboteurWahlAufloesungPunkteverteilung.SetActive(true);
        SaboteurWahlAufloesungPunkteverteilung.transform.GetChild(6).gameObject.SetActive(false);

        int[] wrongvotes = new int[5];
        for (int i = 0; i < 5; i++) // Player
        {
            for (int j = 0; j < 5; j++) // Votes
            {
                if (abstimmungClientStimme[i, j] == 1 && !sabotagePlayers[j].isSaboteur)
                {
                    wrongvotes[i] += -100;
                }
            }
        }
        string aktuellepunkte = "";
        for (int i = 0; i < sabotagePlayers.Length; i++)
        {
            sabotagePlayers[i].AddHiddenPoins(wrongvotes[i]);
            aktuellepunkte += "~" + sabotagePlayers[i].points;
        }
        if (aktuellepunkte.Length > 0)
            aktuellepunkte = aktuellepunkte.Substring(1);

        int[] votes = new int[5];
        for (int i = 0; i < 5; i++) // Player
        {
            for (int j = 0; j < 5; j++) // Votes
            {
                votes[j] += abstimmungClientStimme[i, j];
            }
        }
        string aufoesung = "";
        for (int i = 0; i < sabotagePlayers.Length; i++)
        {
            if (votes[i] == 0)
                aufoesung += "~" + "-0";
            else if (votes[i] == 1)
                aufoesung += "~" + "-" + ((int) (GetSaboteurPoints() * 0.1 + 0.5));
            else if (votes[i] == 2)
                aufoesung += "~" + "-" + ((int) (GetSaboteurPoints() * 0.5 + 0.5));
            else if (votes[i] == 3)
                aufoesung += "~" + "-" + ((int) (GetSaboteurPoints() * 0.75 + 0.5));
            else if (votes[i] == 4)
                aufoesung += "~" + "-" + ((int) (GetSaboteurPoints() * 1 + 0.5));
        }

        aufoesung = aufoesung.Substring(1);
        aufloesungBonusPunkte = aufoesung;
        ServerUtils.BroadcastImmediate("#AufloesungStart " + aufoesung + "|" + aktuellepunkte);

        for (int i = 0; i < votes.Length; i++)
            SaboteurWahlAufloesungPunkteverteilung.transform.GetChild(1 + i).gameObject.GetComponent<TMP_Text>().text = "" + aufoesung.Split('~')[i];

        Transform SaboAnzeige = SaboteurWahlAufloesungPunkteverteilung.transform.GetChild(6);
        SaboAnzeige.gameObject.SetActive(false);
        SaboAnzeige.GetChild(2).GetComponent<TMP_Text>().text = "";
        SaboAnzeige.GetChild(0).gameObject.SetActive(false);
        SaboAnzeige.GetChild(1).gameObject.SetActive(false);
    }
    // Punkteverteilung
    public void AufloesungZeigeSabos()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "AufloesungZeigeSabos", "");
        Transform SaboAnzeige = SaboteurWahlAufloesungPunkteverteilung.transform.GetChild(6);
        SaboAnzeige.gameObject.SetActive(false);
        SaboAnzeige.GetChild(2).GetComponent<TMP_Text>().text = "";
        int countSabos = 0;
        string sabos = "";
        foreach (var item in sabotagePlayers)
            if (item.isSaboteur)
            {
                countSabos++;
                sabos += "~" + item.player.id;
            }
        if (sabos.Length > 0)
            sabos = sabos.Substring(1);

        SaboAnzeige.GetChild(0).gameObject.SetActive(false);
        SaboAnzeige.GetChild(1).gameObject.SetActive(false);
        SaboAnzeige.gameObject.SetActive(true);

        int sabopunkte = int.Parse(GameObject.Find("Punktetafel/SaboteurPunkte").GetComponent<TMP_InputField>().text);
        int teampunkte = int.Parse(GameObject.Find("Punktetafel/TeamPunkte").GetComponent<TMP_InputField>().text);
        ServerUtils.BroadcastImmediate("#AufloesungZeigeSabos " + countSabos + "|" + teampunkte + "|" + sabopunkte + "|" + aufloesungBonusPunkte + "|" + sabos);

        StartCoroutine(AufloesungVerteilePunkte(SaboAnzeige, countSabos, teampunkte, sabopunkte, aufloesungBonusPunkte));
    }
    public void AufloesungZeigeSabosNicht()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "AufloesungZeigeSabosNicht", "");
        Transform SaboAnzeige = SaboteurWahlAufloesungPunkteverteilung.transform.GetChild(6);
        SaboAnzeige.gameObject.SetActive(false);
        SaboAnzeige.GetChild(2).GetComponent<TMP_Text>().text = "";
        int countSabos = 0;
        string sabos = "";
        foreach (var item in sabotagePlayers)
            if (item.isSaboteur)
            {
                countSabos++;
                sabos += "~" + item.player.id;
            }
        sabos = sabos.Substring(1);

        SaboAnzeige.GetChild(0).gameObject.SetActive(false);
        SaboAnzeige.GetChild(1).gameObject.SetActive(false);
        SaboAnzeige.gameObject.SetActive(true);

        int sabopunkte = int.Parse(GameObject.Find("Punktetafel/SaboteurPunkte").GetComponent<TMP_InputField>().text);
        int teampunkte = int.Parse(GameObject.Find("Punktetafel/TeamPunkte").GetComponent<TMP_InputField>().text);
        ServerUtils.BroadcastImmediate("#AufloesungZeigeSabosNicht " + countSabos + "|" + teampunkte + "|" + sabopunkte + "|" + aufloesungBonusPunkte + "|" + sabos);

        StartCoroutine(AufloesungVerteilePunkte(SaboAnzeige, countSabos, teampunkte, sabopunkte, aufloesungBonusPunkte, false));
    }
    IEnumerator AufloesungVerteilePunkte(Transform SaboAnzeige, int countSabos, int teampunkte, int sabopunkte, string bonuspunkte, bool showSabos = true)
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "AufloesungVerteilePunkte", "CountSabos: " + countSabos + " TeamPunkte: " + teampunkte + " SaboPunkte: " + sabopunkte + " BonusPunkte: " + bonuspunkte + " ShowSabos: " + showSabos);
        yield return new WaitForSeconds(3);
        string sabos = "";
        Sprite Sabo1 = null;
        Sprite Sabo2 = null;
        foreach (var item in sabotagePlayers)
            if (item.isSaboteur)
            {
                sabos += " & " + item.player.name;
                if (Sabo1 == null)
                    Sabo1 = item.player.icon2.icon;
                else if (Sabo2 == null)
                    Sabo2 = item.player.icon2.icon;
            }
        sabos = sabos.Substring(" & ".Length);
        if (showSabos == true)
        {
            if (countSabos == 1)
            {
                SaboAnzeige.GetChild(0).GetComponent<Image>().sprite = Sabo1;
                SaboAnzeige.GetChild(0).gameObject.SetActive(true);
            }
            else if (countSabos == 2)
            {
                SaboAnzeige.GetChild(1).GetChild(0).GetChild(0).GetComponent<Image>().sprite = Sabo2;
                SaboAnzeige.GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = Sabo1;
                SaboAnzeige.GetChild(1).gameObject.SetActive(true);
            }
            SaboAnzeige.GetChild(2).GetComponent<TMP_Text>().text = sabos;
        }
        yield return new WaitForSeconds(1);
        // Teampunkte & Saboteurpunkte
        foreach (var item in sabotagePlayers)
        {
            if (item.isSaboteur)
                item.AddPunkte(sabopunkte);
            else
                item.AddPunkte(teampunkte);
        }
        for (int i = 0; i < 5; i++)
        {
            if (sabotagePlayers[i].isSaboteur && showSabos)
                sabotagePlayers[i].AddPunkte(int.Parse(bonuspunkte.Split('~')[i]));
            else
                SaboteurWahlAufloesungPunkteverteilung.transform.GetChild(1 + i).gameObject.GetComponent<TMP_Text>().text = "";
        }
        // Strafpunkte bei falschen Votes

        yield break;
    }
    public void AufloesungZurLobby()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "AufloesungZurLobby", "");
        ServerUtils.BroadcastImmediate("#AufloesungZurLobby");
        SetTeamPoints(0);
        SetSaboteurPoints(0);
        WerIstSabo.SetActive(false);
        foreach (var item in sabotagePlayers)
            item.SetSaboteur(false);

        Transform modi = GameObject.Find("Modi").transform;
        for (int i = 0; i < modi.childCount; i++)
            modi.GetChild(i).gameObject.SetActive(false);
        Lobby.SetActive(true);
    }
    #endregion
    #region Diktat
    public void StartDiktat()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "StartDiktat", "");
        ServerUtils.BroadcastImmediate("#StartDiktat");
        Lobby.SetActive(false);
        Diktat.SetActive(true);
        Diktat.transform.GetChild(0).gameObject.SetActive(true);
        DiktatLoesung.gameObject.SetActive(false);
        GameObject ServerSide = GameObject.Find("Diktat/SaboHinweis");
        if (ServerSide != null)
            ServerSide.gameObject.SetActive(false);
        // Leere Eingabefelder
        Transform SpielerEingabeFelder = GameObject.Find("Diktat/SpielerEingabeFelder").transform;
        for (int i = 0; i < SpielerEingabeFelder.childCount; i++)
        {
            SpielerEingabeFelder.GetChild(i).GetComponent<TMP_InputField>().text = "";
            SpielerEingabeFelder.GetChild(i).GetComponent<TMP_InputField>().interactable = false;
        }
    }
    public void DiktatChangeText(int change)
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "DiktatChangeText", ""+change);
        string diktat = Config.SABOTAGE_SPIEL.diktat.GetNew(change);
        GameObject.Find("Diktat/ServerSide/Index").GetComponent<TMP_Text>().text = "Text: " + (Config.SABOTAGE_SPIEL.diktat.index+1) + "/" + Config.SABOTAGE_SPIEL.diktat.saetze.Count;
        DiktatLoesung.text = diktat;
        DiktatLoesung.gameObject.SetActive(true);

        // Leere Eingabefelder
        Transform SpielerEingabeFelder = GameObject.Find("Diktat/SpielerEingabeFelder").transform;
        for (int i = 0; i < SpielerEingabeFelder.childCount; i++)
        {
            SpielerEingabeFelder.GetChild(i).GetComponent<TMP_InputField>().text = "";
            SpielerEingabeFelder.GetChild(i).GetComponent<TMP_InputField>().interactable = false;
        }
        diktatBlockEingabe = false;
        ServerUtils.BroadcastImmediate("#DiktatSaboTipp " + diktat);
    }
    private bool diktatBlockEingabe;
    private void DiktatPlayerInput(Player p, string data)
    {
        if (diktatBlockEingabe)
            return;
        Transform SpielerEingabeFelder = GameObject.Find("Diktat/SpielerEingabeFelder").transform;
        SpielerEingabeFelder.GetChild(p.id - 1).GetComponent<TMP_InputField>().text = data;
    }
    public void DiktatCheckInputs()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "DiktatCheckInputs", "");
        diktatBlockEingabe = true;
        Transform SpielerEingabeFelder = GameObject.Find("Diktat/SpielerEingabeFelder").transform;
        int correct = 0, wrong = 0;
        string playerinputs = "";
        for (int i = 0; i < SpielerEingabeFelder.childCount; i ++)
        {
            SpielerEingabeFelder.GetChild(i).GetComponent<TMP_InputField>().interactable = false;
            string eingabe = SpielerEingabeFelder.GetChild(i).GetComponent<TMP_InputField>().text;
            eingabe = eingabe.Replace("<color=\"red\">", "").Replace("<color=\"green\">", "").Replace("</color>", "").Replace("<b>", "").Replace("</b>", "");
            string diff = Config.SABOTAGE_SPIEL.diktat.markDifferences(eingabe);
            if (diff.StartsWith(true.ToString()))
            {
                diff = diff.Substring(true.ToString().Length);
                correct++;
            }
            else if (diff.StartsWith(false.ToString()))
            {
                diff = diff.Substring(false.ToString().Length);
                wrong++;
            }
            playerinputs += "~" + diff;
        }
        playerinputs = playerinputs.Substring(1);
        Debug.Log("Richtig: " + correct + "   Falsch: " + wrong);

        ServerUtils.BroadcastImmediate("#DiktatCheckInputs " + correct + "|" + wrong + "|" + DiktatLoesung.text + "|" + playerinputs);
        StartCoroutine(DiktatShowResults(wrong, correct, DiktatLoesung.text, playerinputs.Split('~')));
    }
    private IEnumerator DiktatShowResults(int wrong, int correct, string result, string[] playerresults)
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "DiktatShowResults", "Wrong: " + wrong + " Correct: " + correct + " Result: " + result + " PlayerResults: [" + string.Join(", ", playerresults) + "]");
        DiktatLoesung.text = result;
        DiktatLoesung.gameObject.SetActive(true);
        Transform SpielerEingabeFelder = GameObject.Find("Diktat/SpielerEingabeFelder").transform;
        for (int i = 0; i < SpielerEingabeFelder.childCount; i++)
        {
            SpielerEingabeFelder.GetChild(i).GetComponent<TMP_InputField>().text = 
                playerresults[i].Replace("<color=\"red\">", "").Replace("<color=\"green\">", "").Replace("</color>", "").Replace("</b>", "").Replace("<b>", "");
        }
        yield return new WaitForSecondsRealtime(3f);
        for (int i = 0; i < SpielerEingabeFelder.childCount; i++)
        {
            SpielerEingabeFelder.GetChild(i).GetComponent<TMP_InputField>().text = playerresults[i];
        }

        AddSaboteurPoints(wrong * 10);
        AddTeamPoints(correct * 10);
        yield break;
    }
    public void DiktatRunTimer(TMP_InputField input)
    {
        if (input.text.Equals("0"))
            return;
        Logging.log(Logging.LogType.Debug, "SabotageServer", "DiktatRunTimer", input.text);
        diktatBlockEingabe = false;
        ServerUtils.BroadcastImmediate("#DiktatRunTimer " + input.text);
        if (diktattimer != null)
            StopCoroutine(diktattimer);
        diktattimer = StartCoroutine(DiktatRunTimer(int.Parse(input.text)));
    }
    Coroutine diktattimer;
    public void DiktatStopTimer()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "DiktatStopTimer", "");
        ServerUtils.BroadcastImmediate("#DiktatStopTimer");
        if (diktattimer != null)
            StopCoroutine(diktattimer);
        DiktatTimer.gameObject.SetActive(false);
    }
    IEnumerator DiktatRunTimer(int seconds)
    {
        yield return null;
        DiktatTimer.minValue = 0;
        DiktatTimer.maxValue = seconds * 1000; // Umrechnung in Millisekunden
        DiktatTimer.gameObject.SetActive(true);
        int milis = seconds * 1000;

        Logging.log(Logging.LogType.Debug, "SabotageServer", "DiktatRunTimer", "Seconds: " + seconds + "  Timer started: " + DateTime.Now.ToString("HH:mm:ss:ffff"));
        while (milis >= 0)
        {
            DiktatTimer.GetComponentInChildren<Slider>().value = milis;

            if (milis <= 0)
            {
                Beeep.Play();
            }
            // Moep Sound bei Sekunden
            if (milis == 1000 || milis == 2000 || milis == 3000)
            {
                Moeoop.Play();
            }
            yield return new WaitForSecondsRealtime(0.1f); // Alle 100 Millisekunden warten
            milis -= 100;
        }
        Logging.log(Logging.LogType.Debug, "SabotageServer", "DiktatRunTimer", "Seconds: " + seconds + "  Timer ended: " + DateTime.Now.ToString("HH:mm:ss:ffff"));
       
        DiktatTimer.gameObject.SetActive(false);
        yield return new WaitForSeconds(2f);
        diktatBlockEingabe = true;

        yield break;
    }
    public void DiktatZurAuflösung()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "DiktatZurAuflösung", "");
        ServerUtils.BroadcastImmediate("#DiktatZurAuflösung");
        StartWahlAbstimmung();
    }
    #endregion
    #region Sortieren
    public void StartSortieren()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "StartSortieren", "");
        ServerUtils.BroadcastImmediate("#StartSortieren");
        Lobby.SetActive(false);
        Sortieren.SetActive(true);
        Sortieren.transform.GetChild(0).gameObject.SetActive(true);
        GameObject ServerSide = GameObject.Find("Sortieren/SaboHinweis");
        if (ServerSide != null)
            ServerSide.gameObject.SetActive(false);

        for (int i = 0; i < SortierenListe.transform.childCount; i++)
            SortierenListe.transform.GetChild(i).gameObject.SetActive(false);
        SortierenListe.SetActive(true);

        SortierenTimer.gameObject.SetActive(false);
        SortierenAuswahl.SetActive(false);
        SortierenLoesung.SetActive(false);
    }
    public void SortierenChangeText(int change)
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "SortierenChangeText", ""+change);
        SortierenAuswahl.SetActive(false);
        int newIndex = Config.SABOTAGE_SPIEL.sortieren.ChangeIndex(change);
        GameObject.Find("Sortieren/ServerSide/Index").GetComponent<TMP_Text>().text = "Text: " + (newIndex+1) + "/" + Config.SABOTAGE_SPIEL.sortieren.runden.Count;

        string liste = Config.SABOTAGE_SPIEL.sortieren.GetSortBy();
        for (int i = 0; i < Config.SABOTAGE_SPIEL.sortieren.GetInhalt().Count; i++)
            liste += "|" + Config.SABOTAGE_SPIEL.sortieren.GetInhalt()[i];

        SortierenLoesung.SetActive(true);
        SortierenLoesung.transform.GetChild(1).GetComponent<TMP_InputField>().text = Config.SABOTAGE_SPIEL.sortieren.GetSortBy().Split('-')[0];
        SortierenLoesung.transform.GetChild(SortierenLoesung.transform.childCount-1).GetComponent<TMP_InputField>().text = Config.SABOTAGE_SPIEL.sortieren.GetSortBy().Split('-')[1];
        for (int i = 0; i < Config.SABOTAGE_SPIEL.sortieren.GetInhalt().Count; i++)
        {
            SortierenLoesung.transform.GetChild(i + 2).GetComponent<TMP_InputField>().text = Config.SABOTAGE_SPIEL.sortieren.GetInhalt()[i];
            SortierenLoesung.transform.GetChild(i + 2).GetChild(1).gameObject.SetActive(true);
            SortierenLoesung.transform.GetChild(i + 2).GetChild(2).gameObject.SetActive(true);
            SortierenLoesung.transform.GetChild(i + 2).GetChild(3).gameObject.SetActive(true);
        }

        for (int i = 2; i < SortierenLoesung.transform.childCount - 1; i++)
            SortierenLoesung.transform.GetChild(i).GetChild(3).gameObject.SetActive(true);

        for (int i = 0; i < SortierenListe.transform.childCount; i++)
            SortierenListe.transform.GetChild(i).gameObject.SetActive(false);

        ServerUtils.BroadcastImmediate("#SortierenSaboTipp " + liste);
    }
    public void SortierenRunTimer(TMP_InputField input)
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "SortierenRunTimer", ""+input.text);
        ServerUtils.BroadcastImmediate("#SortierenRunTimer " + input.text);
        if (sortierentimer != null)
            StopCoroutine(sortierentimer);
        sortierentimer = StartCoroutine(SortierenRunTimer(int.Parse(input.text)));
    }
    Coroutine sortierentimer;
    public void SortierenStopTimer()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "SortierenStopTimer", "");
        ServerUtils.BroadcastImmediate("#SortierenStopTimer");
        if (sortierentimer != null)
            StopCoroutine(sortierentimer);
        SortierenTimer.gameObject.SetActive(false);
    }
    IEnumerator SortierenRunTimer(int seconds)
    {
        yield return null;
        SortierenTimer.minValue = 0;
        SortierenTimer.maxValue = seconds * 1000; // Umrechnung in Millisekunden
        SortierenTimer.gameObject.SetActive(true);
        int milis = seconds * 1000;

        Logging.log(Logging.LogType.Debug, "SabotageServer", "SortierenRunTimer", "Seconds: " + seconds + "  Timer started: " + DateTime.Now.ToString("HH:mm:ss:ffff"));
        while (milis >= 0)
        {
            SortierenTimer.GetComponentInChildren<Slider>().value = milis;

            if (milis <= 0)
            {
                Beeep.Play();
            }
            // Moep Sound bei Sekunden
            if (milis == 1000 || milis == 2000 || milis == 3000)
            {
                Moeoop.Play();
            }
            yield return new WaitForSecondsRealtime(0.1f); // Alle 100 Millisekunden warten
            milis -= 100;
        }
        Logging.log(Logging.LogType.Debug, "SabotageServer", "SortierenRunTimer", "Seconds: " + seconds + "  Timer ended: " + DateTime.Now.ToString("HH:mm:ss:ffff"));

        SortierenTimer.gameObject.SetActive(false);
        yield break;
    }
    public void SortierenShowGrenzen()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "SortierenShowGrenzen", "");
        ServerUtils.BroadcastImmediate("#SortierenShowGrenzen " + Config.SABOTAGE_SPIEL.sortieren.GetSortBy());
        
        SortierenListe.transform.GetChild(0).gameObject.SetActive(true);
        SortierenListe.transform.GetChild(0).GetComponent<TMP_InputField>().text = Config.SABOTAGE_SPIEL.sortieren.GetSortBy().Split('-')[0];
        SortierenListe.transform.GetChild(SortierenListe.transform.childCount - 1).gameObject.SetActive(true);
        SortierenListe.transform.GetChild(SortierenListe.transform.childCount - 1).GetComponent<TMP_InputField>().text = Config.SABOTAGE_SPIEL.sortieren.GetSortBy().Split('-')[1];
    }
    public void SortierenShowElementInit(Button btn)
    {
        int itemIndex = int.Parse(btn.transform.parent.name.Replace("Element (", "").Replace(")", ""));
        string item = Config.SABOTAGE_SPIEL.sortieren.GetInhalt()[itemIndex];
        Logging.log(Logging.LogType.Debug, "SabotageServer", "SortierenShowElementInit", itemIndex + "|" + item);
        ServerUtils.BroadcastImmediate("#SortierenInitElement " + itemIndex + "|" + item + "|" + string.Join("~", Config.SABOTAGE_SPIEL.sortieren.GetInhalt()));
        
        for (int i = 2; i < SortierenLoesung.transform.childCount-1; i++)
            SortierenLoesung.transform.GetChild(i).GetChild(3).gameObject.SetActive(false);

        SortierenLoesung.transform.GetChild(itemIndex + 2).GetChild(1).gameObject.SetActive(false);
        SortierenLoesung.transform.GetChild(itemIndex + 2).GetChild(2).gameObject.SetActive(false);
        SortierenLoesung.transform.GetChild(itemIndex + 2).GetChild(3).gameObject.SetActive(false);

        SortierenListe.transform.GetChild(itemIndex + 1).gameObject.SetActive(true);
        SortierenListe.transform.GetChild(itemIndex + 1).GetComponent<TMP_InputField>().text = item;

        SortierenAuswahl.SetActive(true);
        List<string> tempauswahl = new List<string>();
        tempauswahl.AddRange(Config.SABOTAGE_SPIEL.sortieren.GetInhalt());
        tempauswahl.Remove(item);
        for (int i = 0; i < tempauswahl.Count; i++)
        {
            SortierenAuswahl.transform.GetChild(i + 1).GetComponent<TMP_InputField>().text = tempauswahl[i];
            SortierenAuswahl.transform.GetChild(i).gameObject.SetActive(true);
        }
    }
    public void SortierenShowElement(Button btn)
    {
        if (!Config.SERVER_STARTED)
            return;
        Logging.log(Logging.LogType.Debug, "SabotageServer", "SortierenShowElement", btn.name);

        bool isCorrect;
        if (btn.name.Equals("Richtig"))
            isCorrect = true;
        else if (btn.name.Equals("Falsch"))
            isCorrect = false;
        else
            isCorrect = false;

        int itemIndex = int.Parse(btn.transform.parent.name.Replace("Element (", "").Replace(")", ""));
        string item = Config.SABOTAGE_SPIEL.sortieren.GetInhalt()[itemIndex];

        ServerUtils.BroadcastImmediate("#SortierenShowElement " + isCorrect + "|" + itemIndex + "|" + item);

        SortierenListe.transform.GetChild(itemIndex + 1).gameObject.SetActive(true);
        SortierenListe.transform.GetChild(itemIndex + 1).GetComponent<TMP_InputField>().text = item;

        if (isCorrect)
            AddTeamPoints(10);
        else
            AddSaboteurPoints(10);

        btn.transform.parent.GetChild(1).gameObject.SetActive(false);
        btn.transform.parent.GetChild(2).gameObject.SetActive(false);

        for (int i = 1; i < SortierenAuswahl.transform.childCount; i++)
        {
            if (SortierenAuswahl.transform.GetChild(i).gameObject.activeInHierarchy)
            {
                if (SortierenAuswahl.transform.GetChild(i).GetComponent<TMP_InputField>().text == item)
                {
                    SortierenAuswahl.transform.GetChild(i).gameObject.SetActive(false);
                    break;
                }
            }
        }
    }
    public void SortierenZurAuflösung()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "SortierenZurAuflösung", "");
        ServerUtils.BroadcastImmediate("#MemoryZurAuflösung");
        StartWahlAbstimmung();
    }
    #endregion
    #region DerZugLuegt
    Coroutine derzugluegtRunElement;
    public void StartDerZugLuegt()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "StartDerZugLuegt", "");
        derzugluegtBlockBuzzer = true;
        ServerUtils.BroadcastImmediate("#StartDerZugLuegt");
        Lobby.SetActive(false);
        DerZugLuegt.SetActive(true);
        DerZugLuegt.transform.GetChild(0).gameObject.SetActive(true);
        DerZugLuegtAnzeigen.SetActive(false);
        GameObject ServerSide = GameObject.Find("DerZugLuegt/SaboHinweis");
        if (ServerSide != null)
            ServerSide.gameObject.SetActive(false);
        GameObject ClientBuzzer = GameObject.Find("DerZugLuegt/ClientBuzzer");
        if (ClientBuzzer != null)
            ClientBuzzer.gameObject.SetActive(false);
        GameObject.Find("DerZugLuegt/ServerSide/Freigeben").GetComponentInChildren<TMP_Text>().text = "Freigeben";
    }
    public void DerZugLuegtChangeText(int change)
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "DerZugLuegtChangeText", ""+change);
        DerZugLuegtAnzeigen.SetActive(true);
        int newIndex = Config.SABOTAGE_SPIEL.derzugluegt.ChangeIndex(change);
        GameObject.Find("DerZugLuegt/ServerSide/Index").GetComponent<TMP_Text>().text = "Text: " + (newIndex + 1) + "/" + Config.SABOTAGE_SPIEL.derzugluegt.rounds.Count;

        Transform Elements = GameObject.Find("DerZugLuegt/ServerSide/Elements").transform;
        for (int i = 0; i < Elements.childCount; i++)
        {
            Elements.GetChild(i).GetComponentInChildren<TMP_Text>().text = Config.SABOTAGE_SPIEL.derzugluegt.GetElementType(i).ToString().Replace("True", "<color=red>Drücken</color>").Replace("False", "") + "_" + Config.SABOTAGE_SPIEL.derzugluegt.GetElement(i);
            Elements.GetChild(i).GetComponent<Button>().interactable = true;
        }
        GameObject.Find("DerZugLuegt/ServerSide/Thema").GetComponent<TMP_Text>().text = Config.SABOTAGE_SPIEL.derzugluegt.GetThema();
    }
    public void DerZugLuegtShowRound()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "DerZugLuegtShowRound", "");
        string thema = Config.SABOTAGE_SPIEL.derzugluegt.GetThema();
        string elements = "Lösungen:";
        for (int i = 0; i < 10; i++)
        {
            if (Config.SABOTAGE_SPIEL.derzugluegt.GetElementType(i))
                elements += "~" + Config.SABOTAGE_SPIEL.derzugluegt.GetElement(i);
        }

        ServerUtils.BroadcastImmediate("#DerZugLuegtShowRound " + thema + "|" + elements);

        GameObject.Find("DerZugLuegt/GameObject/Title").GetComponent<TMP_Text>().text = thema;
        GameObject.Find("DerZugLuegt/ServerSide/Freigeben").GetComponentInChildren<TMP_Text>().text = "Freigeben";
    }
    public void DerZugLuegtStartElement(GameObject ob)
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "DerZugLuegtStartElement", ob.name);
        string element = ob.GetComponentInChildren<TMP_Text>().text.Split('_')[1];
        ob.GetComponent<Button>().interactable = false;
        ServerUtils.BroadcastImmediate("#DerZugLuegtStartElement " + element);
        derzugluegtBlockBuzzer = false;

        if (derzugluegtRunElement != null)
            StopCoroutine(derzugluegtRunElement);
        derzugluegtRunElement = StartCoroutine(DerZugLuegtRunElement(element));
    }
    bool derzugluegtBlockBuzzer;
    IEnumerator DerZugLuegtRunElement(string element)
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "DerZugLuegtRunElement", element);
        try
        {
            GameObject.Find("DerZugLuegt/GameObject/Grid/Image (0)").SetActive(true);
            GameObject.Find("DerZugLuegt/GameObject/Grid/Image (1)").SetActive(true);
            GameObject.Find("DerZugLuegt/GameObject/Grid/Image (2)").SetActive(true);
            GameObject.Find("DerZugLuegt/GameObject/Grid/Image (3)").SetActive(true);
            GameObject.Find("DerZugLuegt/GameObject/Grid/Image (4)").SetActive(true);
            GameObject.Find("DerZugLuegt/GameObject/Grid/Image (5)").SetActive(true);
            
            GameObject.Find("DerZugLuegt/GameObject/Element").GetComponent<TMP_Text>().text = element;
        }
        catch 
        {
        }
        yield return new WaitForSeconds(1f);
        try
        {
            GameObject.Find("DerZugLuegt/GameObject/Grid/Image (5)").SetActive(false);
        }
        catch
        {
        }
        yield return new WaitForSeconds(1f);
        try
        {
            GameObject.Find("DerZugLuegt/GameObject/Grid/Image (4)").SetActive(false);
        }
        catch
        {
        }
        yield return new WaitForSeconds(1f);
        try
        {
            GameObject.Find("DerZugLuegt/GameObject/Grid/Image (3)").SetActive(false);
        }
        catch
        {
        }
        yield return new WaitForSeconds(1f);
        try
        {
            GameObject.Find("DerZugLuegt/GameObject/Grid/Image (2)").SetActive(false);
        }
        catch
        {
        }
        yield return new WaitForSeconds(1f);
        try
        {
            GameObject.Find("DerZugLuegt/GameObject/Grid/Image (1)").SetActive(false);
        }
        catch
        {
        }
        yield return new WaitForSeconds(1f);
        try
        {
            GameObject.Find("DerZugLuegt/GameObject/Grid/Image (0)").SetActive(false);
        }
        catch
        {
        }
        yield return new WaitForSeconds(1f);
        try
        {
            GameObject.Find("DerZugLuegt/GameObject/Element").GetComponent<TMP_Text>().text = "";
        }
        catch
        {
        }
        
        yield break;
    }
    public void DerZugLuegtRichtig()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "DerZugLuegtRichtig", "");
        AddTeamPoints(50);
        ServerUtils.BroadcastImmediate("#DerZugLuegtRichtig " + GetTeamPoints());
        GameObject.Find("DerZugLuegt/ServerSide/Freigeben").GetComponentInChildren<TMP_Text>().text = "Freigeben";
        Correct.Play();
        derzugluegtBlockBuzzer = false;
    }
    public void DerZugLuegtFalsch()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "DerZugLuegtFalsch", "");
        AddSaboteurPoints(50);
        ServerUtils.BroadcastImmediate("#DerZugLuegtFalsch " + GetSaboteurPoints());
        GameObject.Find("DerZugLuegt/ServerSide/Freigeben").GetComponentInChildren<TMP_Text>().text = "Freigeben";
        Wrong.Play();
        derzugluegtBlockBuzzer = false;
    }
    public void DerZugLuegtFreigeben()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "DerZugLuegtFreigeben", "");
        GameObject.Find("DerZugLuegt/ServerSide/Freigeben").GetComponentInChildren<TMP_Text>().text = "Freigeben";
        derzugluegtBlockBuzzer = false;
    }
    private void DerZugLuegtClientBuzzer(Player p)
    {
        if (derzugluegtBlockBuzzer)
            return;
        Logging.log(Logging.LogType.Debug, "SabotageServer", "DerZugLuegtClientBuzzer", p.name);
        derzugluegtBlockBuzzer = true;

        ServerUtils.BroadcastImmediate("#DerZugLuegtBuzzer");
        GameObject.Find("DerZugLuegt/ServerSide/Freigeben").GetComponentInChildren<TMP_Text>().text = "Freigeben\nP:" + p.name;
        Buzzer.Play();
    }
    public void DerZugLuegtZurAuflösung()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "DerZugLuegtZurAuflösung", "");
        ServerUtils.BroadcastImmediate("#DerZugLuegtZurAuflösung");
        StartWahlAbstimmung();
    }
    #endregion
    #region Tabu
    public void StartTabu()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "StartTabu", "");
        ServerUtils.BroadcastImmediate("#StartTabu");
        Lobby.SetActive(false);
        Tabu.SetActive(true);
        Tabu.transform.GetChild(0).gameObject.SetActive(true);
        TabuTimer.gameObject.SetActive(false);
        GameObject ServerSide = GameObject.Find("Tabu/SaboHinweis");
        if (ServerSide != null)
            ServerSide.gameObject.SetActive(false);
        GameObject.Find("Tabu/GameObject/SaboTipp").SetActive(false);

        tabuRichtigePunkte = 0;
    }
    int tabuRichtigePunkte;
    public void TabuRunTimer(TMP_InputField input)
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "TabuRunTimer", input.text);
        ServerUtils.BroadcastImmediate("#TabuRunTimer " + input.text);
        if (tabutimer != null)
            StopCoroutine(tabutimer);
        tabutimer = StartCoroutine(TabuRunTimer(int.Parse(input.text)));
    }
    Coroutine tabutimer;
    public void TabuStopTimer()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "TabuStopTimer", "");
        ServerUtils.BroadcastImmediate("#TabuStopTimer");
        if (tabutimer != null)
            StopCoroutine(tabutimer);
        TabuTimer.gameObject.SetActive(false);
    }
    public void TabuGrenzwertig()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "TabuGrenzwertig", "");
        if (TabuTimer.value <= 10 * 1000)
            TabuTimer.value = 0;
        else
            TabuTimer.value -= 10 * 1000;

        ServerUtils.BroadcastImmediate("#TabuGrenzwertig");
    }
    IEnumerator TabuRunTimer(int seconds)
    {
        yield return null;
        TabuTimer.minValue = 0;
        TabuTimer.maxValue = seconds * 1000; // Umrechnung in Millisekunden
        TabuTimer.value = seconds * 1000;
        TabuTimer.gameObject.SetActive(true);
        int milis = seconds * 1000;

        Logging.log(Logging.LogType.Debug, "SabotageServer", "TabuRunTimer", "Seconds: " + seconds + "  Timer started: " + DateTime.Now.ToString("HH:mm:ss:ffff"));
        while (TabuTimer.value >= 0)
        {
            if (((int)(TabuTimer.value) - 100) <= 0)
                TabuTimer.value = 0;
            else
                TabuTimer.value -= 100;

            if (TabuTimer.value <= 0)
            {
                Beeep.Play();
                break;
            }
            // Moep Sound bei Sekunden
            if (TabuTimer.value == 1000 || TabuTimer.value == 2000 || TabuTimer.value == 3000)
            {
                Moeoop.Play();
            }
            yield return new WaitForSecondsRealtime(0.1f); // Alle 100 Millisekunden warten
        }
        Logging.log(Logging.LogType.Debug, "SabotageServer", "TabuRunTimer", "Seconds: " + seconds + "  Timer ended: " + DateTime.Now.ToString("HH:mm:ss:ffff"));

        TabuTimer.gameObject.SetActive(false);
        yield break;
    }
    public void TabuRichtig()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "TabuRichtig", "");
        tabuRichtigePunkte += (SabotageTabu.punkteProRichtig / 5);
        AddTeamPoints((SabotageTabu.punkteProRichtig / 5));

        ServerUtils.BroadcastImmediate("#TabuRichtig " + GetTeamPoints() + "|" + GetSaboteurPoints());
    }
    public void TabuFalsch()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "TabuFalsch", "");
        AddTeamPoints(-tabuRichtigePunkte);
        tabuRichtigePunkte = 0;
        AddSaboteurPoints(SabotageTabu.punkteProFalsch);
        
        ServerUtils.BroadcastImmediate("#TabuFalsch " + GetTeamPoints() + "|" + GetSaboteurPoints());

        if (tabutimer != null)
            StopCoroutine(tabutimer);
        TabuTimer.gameObject.SetActive(false);
    }
    public void TabuShowKarteToPlayer(GameObject go)
    {
        int index = int.Parse(go.name);
        string tabu = Config.SABOTAGE_SPIEL.tabu.GetWort() + "|" + Config.SABOTAGE_SPIEL.tabu.GetTabus();
        string zusatztabu = GameObject.Find("Tabu/ServerSide/AddZusatzTabuWorte").GetComponent<TMP_InputField>().text.Replace(" ", "   ");
        Logging.log(Logging.LogType.Debug, "SabotageServer", "TabuShowKarteToPlayer", index + "|" + tabu + "|" + zusatztabu);
        //ServerUtils.SendMSG("#TabuShowKarteToPlayer " + tabu + "   " + zusatztabu, sabotagePlayers[index].player, false);
        ServerUtils.BroadcastImmediate("#TabuShowKarteToPlayer " + sabotagePlayers[index].player.id + "|" + tabu + "   " + zusatztabu);
    }
    public void TabuChangeText(int change)
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "TabuChangeText", change+"");
        tabuRichtigePunkte = 0;
        int index = Config.SABOTAGE_SPIEL.tabu.ChangeIndex(change);
        GameObject.Find("Tabu/ServerSide/Index").GetComponent<TMP_Text>().text = "Text: " + (Config.SABOTAGE_SPIEL.tabu.index + 1) + "/" + Config.SABOTAGE_SPIEL.tabu.tabus.Count;

        string tabu = Config.SABOTAGE_SPIEL.tabu.GetWort() + "|" + Config.SABOTAGE_SPIEL.tabu.GetTabus();
        ServerUtils.BroadcastImmediate("#TabuSaboTipp " + Config.SABOTAGE_SPIEL.tabu.GetWort());

        // TODO: 
        GameObject.Find("Tabu/GameObject/Wort").GetComponent<TMP_Text>().text = "Start: " +sabotagePlayers[index % sabotagePlayers.Length].player.name + " - " + tabu.Split('|')[0];
        GameObject.Find("Tabu/GameObject/Tabu").GetComponent<TMP_Text>().text = "<color=red>No-Go: </color>" + tabu.Split('|')[1];

        // Reihnfolge gen
        Transform reihnfolge = GameObject.Find("Tabu/ServerSide/Reihnfolge").transform;
        List<SabotagePlayer> reihnfolgeplayer = new List<SabotagePlayer>();
        reihnfolgeplayer.AddRange(sabotagePlayers);
        reihnfolgeplayer.Remove(sabotagePlayers[index % sabotagePlayers.Length]);

        reihnfolge.GetChild(1).GetComponent<Image>().sprite = sabotagePlayers[index % sabotagePlayers.Length].player.icon2.icon;
        int temp = 0;
        while (reihnfolgeplayer.Count > 0)
        {
            SabotagePlayer p = reihnfolgeplayer[UnityEngine.Random.Range(0, reihnfolgeplayer.Count)];
            reihnfolgeplayer.Remove(p);
            reihnfolge.GetChild(2 + temp++).GetComponent<Image>().sprite = p.player.icon2.icon;
        }
    }
    public void TabuGenNewTabuWords(TMP_InputField input)
    {
        if (!Config.SERVER_STARTED)
            return;
        Logging.log(Logging.LogType.Debug, "SabotageServer", "TabuGenNewTabuWords", input.text);
        if (Config.SABOTAGE_SPIEL.tabu.GetIndex() >= 0)
            ServerUtils.BroadcastImmediate("#TabuNewTabuWords " + Config.SABOTAGE_SPIEL.tabu.GetTabus() + "   " + input.text.Replace(" ", "   "));
    }
    public void TabuZurAuflösung()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "TabuZurAuflösung", "");
        ServerUtils.BroadcastImmediate("#TabuZurAuflösung");
        StartWahlAbstimmung();
    }
    #endregion
    #region Auswahlstrategie
    public void StartAuswahlstrategie()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "StartAuswahlstrategie", "");
        ServerUtils.BroadcastImmediate("#StartAuswahlstrategie");
        Lobby.SetActive(false);
        Auswahlstrategie.SetActive(true);
        Auswahlstrategie.transform.GetChild(0).gameObject.SetActive(true);
        GameObject ServerSide = GameObject.Find("Auswahlstrategie/SaboHinweis");
        if (ServerSide != null)
            ServerSide.gameObject.SetActive(false);
        AuswahlstrategieTimer.gameObject.SetActive(false);
        AuswahlstrategieGrid.gameObject.SetActive(false);
    }
    List<Sprite> auswahlstrategieAuswahl1;
    List<Sprite> auswahlstrategieAuswahl2;
    Coroutine auswahlstrategietimer;
    string auswahlstrategieGewaehltesBild;
    public void AuswahlstrategieRunTimer(TMP_InputField input)
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "AuswahlstrategieRunTimer", input.text);
        ServerUtils.BroadcastImmediate("#AuswahlstrategieRunTimer " + input.text);
        if (auswahlstrategietimer != null)
            StopCoroutine(auswahlstrategietimer);
        auswahlstrategietimer = StartCoroutine(AuswahlstrategieRunTimer(int.Parse(input.text)));
    }
    public void AuswahlstrategieStopTimer()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "AuswahlstrategieStopTimer", "");
        ServerUtils.BroadcastImmediate("#AuswahlstrategieStopTimer");
        if (auswahlstrategietimer != null)
            StopCoroutine(auswahlstrategietimer);
        AuswahlstrategieTimer.gameObject.SetActive(false);
    }
    IEnumerator AuswahlstrategieRunTimer(int seconds)
    {
        yield return null;
        AuswahlstrategieTimer.minValue = 0;
        AuswahlstrategieTimer.maxValue = seconds * 1000; // Umrechnung in Millisekunden
        AuswahlstrategieTimer.gameObject.SetActive(true);
        int milis = seconds * 1000;

        Logging.log(Logging.LogType.Debug, "SabotageServer", "AuswahlstrategieRunTimer", "Seconds: " + seconds + "  Timer started: " + DateTime.Now.ToString("HH:mm:ss:ffff"));
        while (milis >= 0)
        {
            AuswahlstrategieTimer.GetComponentInChildren<Slider>().value = milis;

            if (milis <= 0)
            {
                Beeep.Play();
            }
            // Moep Sound bei Sekunden
            if (milis == 1000 || milis == 2000 || milis == 3000)
            {
                Moeoop.Play();
            }
            yield return new WaitForSecondsRealtime(0.1f); // Alle 100 Millisekunden warten
            milis -= 100;
        }
        Logging.log(Logging.LogType.Debug, "SabotageServer", "AuswahlstrategieRunTimer", "Seconds: " + seconds + "  Timer ended: " + DateTime.Now.ToString("HH:mm:ss:ffff"));

        AuswahlstrategieTimer.gameObject.SetActive(false);
        yield break;
    }
    public void AuswahlstrategieChangeText(int change)
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "AuswahlstrategieChangeText", change+"");
        int index = Config.SABOTAGE_SPIEL.auswahlstrategie.ChangeIndex(change);
        GameObject.Find("Auswahlstrategie/ServerSide/Index").GetComponent<TMP_Text>().text = "Text: " + (Config.SABOTAGE_SPIEL.auswahlstrategie.index + 1) + "/" + Config.SABOTAGE_SPIEL.auswahlstrategie.runden.Count;

        GameObject.Find("Auswahlstrategie/ServerSide/SpielerErsteAuswahl").GetComponent<TMP_Text>().text =
            sabotagePlayers[int.Parse(Config.SABOTAGE_SPIEL.auswahlstrategie.playerturn[index].Split("~")[0])].player.name + "\n" +
            sabotagePlayers[int.Parse(Config.SABOTAGE_SPIEL.auswahlstrategie.playerturn[index].Split("~")[1])].player.name;
        
        List<Sprite> temp = new List<Sprite>();
        temp.AddRange(Config.SABOTAGE_SPIEL.auswahlstrategie.GetList());
        auswahlstrategieAuswahl1 = new List<Sprite>();
        while (temp.Count > 0)
        {
            Sprite sp = temp[UnityEngine.Random.Range(0, temp.Count)];
            temp.Remove(sp);
            auswahlstrategieAuswahl1.Add(sp);
        }
        auswahlstrategieAuswahl2 = new List<Sprite>();
        temp = new List<Sprite>();
        temp.AddRange(Config.SABOTAGE_SPIEL.auswahlstrategie.GetList());
        while (temp.Count > 0)
        {
            Sprite sp = temp[UnityEngine.Random.Range(0, temp.Count)];
            temp.Remove(sp);
            auswahlstrategieAuswahl2.Add(sp);
        }

        AuswahlstrategieGrid.gameObject.SetActive(true);
        for (int i = 0; i < 7; i++)
        {
            AuswahlstrategieGrid.GetChild(i).GetChild(0).GetComponent<Image>().sprite = auswahlstrategieAuswahl1[i];
            AuswahlstrategieGrid.GetChild(i).GetComponent<Image>().enabled = false;
        }

        string list = "";
        foreach (var item in auswahlstrategieAuswahl1)
            list += "~" + item.name;
        if (list.Length > 0)
            list = list.Substring(1);

        ServerUtils.BroadcastImmediate("#AuswahlstrategieShowSaboTipp " + list);
    }
    public void AuswahlstrategieSelectItem(Button go)
    {
        if (!Config.SERVER_STARTED)
            return;
        Logging.log(Logging.LogType.Debug, "SabotageServer", "AuswahlstrategieSelectItem", go.name);

        if (go.gameObject.GetComponent<Image>().enabled)
        {
            go.gameObject.GetComponent<Image>().enabled = false;
        }
        else
        {
            for (int i = 0; i < 7;i++)
                AuswahlstrategieGrid.GetChild(i).GetComponent<Image>().enabled = false;
            go.gameObject.GetComponent<Image>().enabled = true;
            auswahlstrategieGewaehltesBild = go.transform.GetChild(0).GetComponent<Image>().sprite.name;
        }
    }
    public void AuswahlstrategieShowFirstAuswahl()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "AuswahlstrategieShowFirstAuswahl", "");
        string list = "";
        foreach (var item in auswahlstrategieAuswahl1)
            list += "~" + item.name;
        if (list.Length > 0)
            list = list.Substring(1);
        AuswahlstrategieGrid.gameObject.SetActive(true);
        for (int i = 0; i < 7; i++)
            AuswahlstrategieGrid.GetChild(i).GetChild(0).GetComponent<Image>().sprite = auswahlstrategieAuswahl1[i];

        ServerUtils.BroadcastImmediate("#AuswahlstrategieShowFirstAuswahl " + list + "|" + Config.SABOTAGE_SPIEL.auswahlstrategie.GetPlayerTurn());
    }
    public void AuswahlstrategieShowSecondAuswahl()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "AuswahlstrategieShowSecondAuswahl", "");
        string list = "";
        foreach (var item in auswahlstrategieAuswahl2)
            list += "~" + item.name;
        if (list.Length > 0)
            list = list.Substring(1);
        AuswahlstrategieGrid.gameObject.SetActive(true);
        for (int i = 0; i < 7; i++)
            AuswahlstrategieGrid.GetChild(i).GetChild(0).GetComponent<Image>().sprite = auswahlstrategieAuswahl2[i];

        for (int i = 0; i < 7; i++)
        {
            AuswahlstrategieGrid.GetChild(i).GetComponent<Image>().enabled = false;
            if (AuswahlstrategieGrid.GetChild(i).GetChild(0).GetComponent<Image>().sprite.name == auswahlstrategieGewaehltesBild)
                AuswahlstrategieGrid.GetChild(i).GetComponent<Image>().enabled = true;
        }

        int auswahlitem = -1;
        for (int i = 0; i < 7; i++)
            if (AuswahlstrategieGrid.GetChild(i).GetComponent<Image>().enabled)
                auswahlitem = i;

        ServerUtils.BroadcastImmediate("#AuswahlstrategieShowSecondAuswahl " + list + "|" + Config.SABOTAGE_SPIEL.auswahlstrategie.GetPlayerTurn() + "|" + auswahlitem);
    }
    public void AuswahlstrategieRichtig()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "AuswahlstrategieRichtig", "");
        AddTeamPoints(100);
        int auswahlitem = -1;
        for (int i = 0; i < 7; i++)
            if (AuswahlstrategieGrid.GetChild(i).GetComponent<Image>().enabled)
                auswahlitem = i;

        ServerUtils.BroadcastImmediate("#AuswahlstrategieRichtig " + GetTeamPoints() + "|" + GetSaboteurPoints() + "|" + auswahlitem);
    }
    public void AuswahlstrategieFalsch()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "AuswahlstrategieFalsch", "");
        AddSaboteurPoints(100);
        int auswahlitem = -1;
        for (int i = 0; i < 7; i++)
            if (AuswahlstrategieGrid.GetChild(i).GetComponent<Image>().enabled)
                auswahlitem = i;

        ServerUtils.BroadcastImmediate("#AuswahlstrategieFalsch " + GetTeamPoints() + "|" + GetSaboteurPoints() + "|" + auswahlitem);
    }
    public void AuswahlstrategieZurAuflösung()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "AuswahlstrategieZurAuflösung", "");
        ServerUtils.BroadcastImmediate("#AuswahlstrategieZurAuflösung");
        StartWahlAbstimmung();
    }
    #endregion

    #region Utils
    private void GenSaboteurForRound(int saboteurCount) // 1 oder 2
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "GenSaboteurForRound", ""+saboteurCount);
        // Diktat                       // s1
        // Sortieren (Listen)           // s2 + s4
        // Memory                       // s3
        // Der Zug Lügt                 // s4 + s5
        // Tabu                         // s5 + s3
        // Auswahlstrategie             // s2 + s1

        if (saboteurCount == 0)
            return;

        List<SabotagePlayer> sortSabos = new List<SabotagePlayer>();
        sortSabos.AddRange(sabotagePlayers);
        //sortSabos = sortSabos.OrderByDescending(player =>  player.placedTokens).ToList();
        // Sort by placedTokens
        for (int i = 1; i < sortSabos.Count; i++)
        {
            SabotagePlayer key = sortSabos[i];
            int j = i - 1;
            // Move elements of array[0..i-1] that are greater than key to one position ahead of their current position
            for (; j >= 0 && sortSabos[j].placedTokens < key.placedTokens; j--)
            {
                sortSabos[j + 1] = sortSabos[j];
            }
            sortSabos[j + 1] = key;
        }

        int security = 0;
        while (saboteurCount > 0)
        {
            security++;
            SabotagePlayer topplayer = sortSabos[0];
            List<SabotagePlayer> sabos1 = new List<SabotagePlayer>();
            for (int i = 0; i < sortSabos.Count; i++)
            {
                if (sortSabos[i].placedTokens == topplayer.placedTokens)
                    sabos1.Add(sortSabos[i]);
            }
            while (sabos1.Count > 0)
            {
                if (saboteurCount > 0)
                {
                    SabotagePlayer p = sabos1[UnityEngine.Random.Range(0, sabos1.Count)];
                    sabos1.Remove(p);
                    sortSabos.Remove(p);
                    p.SetSaboteur(true);
                    saboteurCount--;
                }
                if (saboteurCount == 0)
                    break;
            }
            if (security > 10)
                break;
        }

        for (int i = 0; i < sabotagePlayers.Length; i++)
        {
            sabotagePlayers[i].placedTokens  = 0;
        }

        /*List<SabotagePlayer> nonSabos = new List<SabotagePlayer>();
        foreach (var item in sabotagePlayers)
            if (item.wasSaboteur == 0)
                nonSabos.Add(item);
        List<SabotagePlayer> oneSabos = new List<SabotagePlayer>();
        foreach (var item in sabotagePlayers)
            if (item.wasSaboteur == 1)
                oneSabos.Add(item);
        List<SabotagePlayer> allSabos = new List<SabotagePlayer>();
        foreach (var item in sabotagePlayers)
            if (item.wasSaboteur < 3)
                allSabos.Add(item);

        if (allSabos.Count == 0)
            return;

        for (int i = 0; i < nonSabos.Count; i++)
        {
            if (saboteurCount > 0)
            {
                SabotagePlayer newSabo = nonSabos[UnityEngine.Random.Range(0, nonSabos.Count)];
                nonSabos.Remove(newSabo);
                allSabos.Remove(newSabo);
                newSabo.SetSaboteur(true);
                saboteurCount--;
            }
            else
                break;
        }

        for (int i = 0; i < oneSabos.Count; i++)
        {
            if (saboteurCount > 0)
            {
                SabotagePlayer newSabo = oneSabos[UnityEngine.Random.Range(0, oneSabos.Count)];
                oneSabos.Remove(newSabo);
                allSabos.Remove(newSabo);
                newSabo.SetSaboteur(true);
                saboteurCount--;
            }
            else
                break;
        }

        for (int i = 0; i < allSabos.Count; i++)
        {
            if (saboteurCount > 0)
            {
                SabotagePlayer newSabo = allSabos[UnityEngine.Random.Range(0, allSabos.Count)];
                allSabos.Remove(newSabo);
                newSabo.SetSaboteur(true);
                saboteurCount--;
            }
            else
                break;
        }*/
    }
    private void SetSaboteurPoints(int punkte)
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "SetSaboteurPoints", ""+ punkte);
        GameObject.Find("Punktetafel/SaboteurPunkte").GetComponent<TMP_InputField>().text = "" + punkte;
    }
    private void SetTeamPoints(int punkte)
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "SetTeamPoints", ""+ punkte);
        GameObject.Find("Punktetafel/TeamPunkte").GetComponent<TMP_InputField>().text = "" + punkte;
    }
    private void AddSaboteurPoints(int punkte)
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "AddSaboteurPoints", ""+ punkte);
        GameObject.Find("Punktetafel/SaboteurPunkte").GetComponent<TMP_InputField>().text = "" +
            (int.Parse(GameObject.Find("Punktetafel/SaboteurPunkte").GetComponent<TMP_InputField>().text) + punkte);
    }
    private void AddTeamPoints(int punkte)
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "AddTeamPoints", ""+ punkte);
        GameObject.Find("Punktetafel/TeamPunkte").GetComponent<TMP_InputField>().text = "" +
            (int.Parse(GameObject.Find("Punktetafel/TeamPunkte").GetComponent<TMP_InputField>().text) + punkte);
    }
    private int GetSaboteurPoints()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "GetSaboteurPoints", "");
        return int.Parse(GameObject.Find("Punktetafel/SaboteurPunkte").GetComponent<TMP_InputField>().text);
    }
    private int GetTeamPoints()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "GetTeamPoints", "");
        return int.Parse(GameObject.Find("Punktetafel/TeamPunkte").GetComponent<TMP_InputField>().text);
    }
    public void ChangeTeamsPoints()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "ChangeTeamsPoints", "");
        ServerUtils.BroadcastImmediate("#TeamPunkte " +
            GameObject.Find("Punktetafel/TeamPunkte").GetComponent<TMP_InputField>().text + "*" +
            GameObject.Find("Punktetafel/SaboteurPunkte").GetComponent<TMP_InputField>().text);
    }
    #endregion
}
