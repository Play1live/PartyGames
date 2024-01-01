using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

    GameObject SaboteurWahlAufloesung;
    GameObject SaboteurWahlAufloesungAbstimmung;
    GameObject SaboteurWahlAufloesungPunkteverteilung;

    GameObject Diktat;
    TMP_InputField DiktatLoesung;
    Slider DiktatTimer;

    GameObject Sortieren;
    Slider SortierenTimer;
    GameObject SortierenListe;
    GameObject SortierenAuswahl;
    GameObject SortierenLoesung;

    GameObject Memory;
    Slider MemoryTimer;
    GameObject MemoryGrid;
    Image MemoryTap1;
    Image MemoryTap2;

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
                UpdateSpielerBroadcast();
                break;
            case "#ClientFocusChange":
                ServerUtils.BroadcastImmediate("#ClientFocusChange " + player.id + "*" + data);
                Config.SABOTAGE_SPIEL.getPlayerByPlayer(sabotagePlayers, player).SetAusgetabbt(!bool.Parse(data));
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
        int playerid = int.Parse(input.transform.parent.parent.name.Replace("Player (", "").Replace(")", ""));
        sabotagePlayers[playerid - 1].SetPunkte(int.Parse(input.text));

        UpdateSpielerBroadcast();
    }
    /// <summary>
    /// Initialisiert die Anzeigen zu beginn
    /// </summary>
    private void InitAnzeigen()
    {
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
        GameObject.Find("Lobby/Server/StartSpielIndex").GetComponent<TMP_Text>().text = "Starte Spiel: " + Config.SABOTAGE_SPIEL.spielindex;
        GameObject.Find("Punktetafel/SaboteurPunkte").GetComponent<TMP_InputField>().interactable = true;
        GameObject.Find("Punktetafel/TeamPunkte").GetComponent<TMP_InputField>().interactable = true;

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

        // Memory
        MemoryTimer = GameObject.Find("Memory/Timer").GetComponent<Slider>();
        MemoryTimer.maxValue = 1;
        MemoryTimer.minValue = 0;
        MemoryTimer.value = 0;
        MemoryTap1 = GameObject.Find("Memory/ServerSide/- (1)").transform.GetChild(0).GetChild(0).GetComponent<Image>();
        MemoryTap1.sprite = null;
        MemoryTap2 = GameObject.Find("Memory/ServerSide/- (2)").transform.GetChild(0).GetChild(0).GetComponent<Image>();
        MemoryTap2.sprite = null;
        MemoryGrid = GameObject.Find("Memory/Grid");
        MemoryGrid.gameObject.SetActive(false);
        Memory = GameObject.Find("Modi/Memory");
        Memory.gameObject.SetActive(false);

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
    public void LobbyChangeSpielIndex(int change)
    {
        if (change == -1 && Config.SABOTAGE_SPIEL.spielindex <= 0)
            Config.SABOTAGE_SPIEL.spielindex = 0;
        else
            Config.SABOTAGE_SPIEL.spielindex += change;
        GameObject.Find("Lobby/Server/StartSpielIndex").GetComponent<TMP_Text>().text = "Starte Spiel: " + Config.SABOTAGE_SPIEL.spielindex;
        // TODO: Animiere den Spielstart
    }
    public void UpdateTeamSaboPunkte()
    {
        ServerUtils.BroadcastImmediate("#UpdateTeamSaboPunkte " + GameObject.Find("Punktetafel/SaboteurPunkte").GetComponent<TMP_InputField>().text + "|" + GameObject.Find("Punktetafel/TeamPunkte").GetComponent<TMP_InputField>().text);
    }
    #endregion
    #region Wahl & Abstimmung
    private void StartWahlAbstimmung()
    {
        // Blende alles aus
        Transform modi = GameObject.Find("Modi").transform;
        for (int i = 0; i < modi.childCount; i++)
            modi.GetChild(i).gameObject.SetActive(false);

        SaboteurWahlAufloesung.SetActive(true);
        SaboteurWahlAufloesungAbstimmung.SetActive(false);
        SaboteurWahlAufloesungPunkteverteilung.SetActive(false);
        abstimmungClientStimme = new int[5];
    }
    // Abstimmung
    public void AbstimmungStart()
    {
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
        abstimmungClientStimme = new int[5];
    }
    int[] abstimmungClientStimme;
    private void ClientStimmtFuer(Player p, string data)
    {
        abstimmungClientStimme[p.id - 1] = int.Parse(data);
        int[] votes = new int[5];
        foreach (var item in abstimmungClientStimme)
        {
            if (item == 0)
                continue;
            votes[item-1]++;
        }

        for (int i = 0;i < abstimmungClientStimme.Length;i++)
        {
            GameObject.Find("SaboteurWahl&Aufloesung/Abstimmung/Server/Icon (" + (i+1) + ")").
                GetComponentInChildren<TMP_Text>().text = "" + votes[i];
        }
    }
    string aufloesungBonusPunkte;
    public void AufloesungStart()
    {
        SaboteurWahlAufloesungAbstimmung.SetActive(false);
        SaboteurWahlAufloesungPunkteverteilung.SetActive(true);
        SaboteurWahlAufloesungPunkteverteilung.transform.GetChild(6).gameObject.SetActive(false);

        int[] votes = new int[5];
        foreach (var item in abstimmungClientStimme)
        {
            if (item == 0)
                continue;
            votes[item - 1]++;
        }

        int countSabos = 0;
        foreach (var item in sabotagePlayers)
            if (item.isSaboteur)
                countSabos++;

        string aufoesung = "";
        if (countSabos == 1)
        {
            for (int i = 0; i < sabotagePlayers.Length; i++)
            {
                if (votes[i] == 0)
                    aufoesung += "~" + "+100";
                else if (votes[i] == 1)
                    aufoesung += "~" + "0";
                else if (votes[i] == 2)
                    aufoesung += "~" + "-" + GetSaboteurPoints() * 0.5;
                else if (votes[i] == 3)
                    aufoesung += "~" + "-" + GetSaboteurPoints() * 0.75;
                else if (votes[i] == 4)
                    aufoesung += "~" + "-" + GetSaboteurPoints() * 1.1;
            }
        }
        else if (countSabos == 2)
        {
            for (int i = 0; i < sabotagePlayers.Length; i++)
            {
                if (votes[i] == 0)
                    aufoesung += "~" + "+100";
                else if (votes[i] == 1)
                    aufoesung += "~" + "-" + GetSaboteurPoints() * 0.5;
                else if (votes[i] == 2)
                    aufoesung += "~" + "-" + GetSaboteurPoints() * 0.75;
                else if (votes[i] == 3)
                    aufoesung += "~" + "-" + GetSaboteurPoints() * 1.1;
            }
        }
        else
            aufoesung = "_0~0~0~0~0";
        aufoesung = aufoesung.Substring(1);
        aufloesungBonusPunkte = aufoesung;
        ServerUtils.BroadcastImmediate("#AufloesungStart " + aufoesung);

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
        ServerUtils.BroadcastImmediate("#AufloesungZeigeSabos " + countSabos + "|" + teampunkte + "|" + sabopunkte + "|" + aufloesungBonusPunkte + "|" + sabos);

        StartCoroutine(AufloesungVerteilePunkte(SaboAnzeige, countSabos, teampunkte, sabopunkte, aufloesungBonusPunkte));
    }
    IEnumerator AufloesungVerteilePunkte(Transform SaboAnzeige, int countSabos, int teampunkte, int sabopunkte, string bonuspunkte)
    {
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
            if (sabotagePlayers[i].isSaboteur)
                sabotagePlayers[i].AddPunkte(int.Parse(bonuspunkte.Split('~')[i]));
            else
                SaboteurWahlAufloesungPunkteverteilung.transform.GetChild(1 + i).gameObject.GetComponent<TMP_Text>().text = "";
        }
        yield break;
    }
    public void AufloesungZurLobby()
    {
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
        ServerUtils.BroadcastImmediate("#StartDiktat");
        Lobby.SetActive(false);
        Diktat.SetActive(true);
        DiktatLoesung.gameObject.SetActive(false);
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
        string diktat = Config.SABOTAGE_SPIEL.diktat.GetNew(change);
        GameObject.Find("Diktat/ServerSide/Index").GetComponent<TMP_Text>().text = "Text: " + (Config.SABOTAGE_SPIEL.diktat.index+1) + "/" + Config.SABOTAGE_SPIEL.diktat.saetze.Count;
        DiktatLoesung.text = diktat;
        DiktatLoesung.gameObject.SetActive(true);

        // Leere Eingabefelder
        Transform SpielerEingabeFelder = GameObject.Find("Diktat/SpielerEingabeFelder").transform;
        for (int i = 0; i < SpielerEingabeFelder.childCount; i++)
        {
            SpielerEingabeFelder.GetChild(i).GetComponent<TMP_InputField>().text = "";
            SpielerEingabeFelder.GetChild(i).GetComponent<TMP_InputField>().interactable = true;
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
    public void DiktatGenSabos()
    {
        foreach (var item in sabotagePlayers)
            item.SetSaboteur(false);
        DiktatLoesung.gameObject.SetActive(false);

        GenSaboteurForRound(1);
        string names = "";
        foreach (var item in sabotagePlayers)
        {
            if (item.isSaboteur)
                names += "\n" + item.player.name;
        }
        if (names.Length > 0)
            names = names.Substring("\n".Length);
        WerIstSabo.transform.GetChild(0).GetComponent<TMP_Text>().text = names;

        // TODO: broadcast, wenn mans nicht ist muss feld ausblenden
        ServerUtils.BroadcastImmediate("#DuBistSaboteur " + names.Replace("\n", "~"));
    }
    public void DiktatRunTimer(TMP_InputField input)
    {
        diktatBlockEingabe = false;
        ServerUtils.BroadcastImmediate("#DiktatRunTimer " + input.text);
        StartCoroutine(DiktatRunTimer(int.Parse(input.text)));
    }
    IEnumerator DiktatRunTimer(int seconds)
    {
        yield return null;
        DiktatTimer.minValue = 0;
        DiktatTimer.maxValue = seconds * 1000; // Umrechnung in Millisekunden
        DiktatTimer.gameObject.SetActive(true);
        int milis = seconds * 1000;

        Debug.Log("Timer startet: " + DateTime.Now.ToString("HH:mm:ss:ffff"));
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
        Debug.Log("Timer ended: " + DateTime.Now.ToString("HH:mm:ss:ffff"));
       
        diktatBlockEingabe = true;
        DiktatTimer.gameObject.SetActive(false);
        yield break;
    }
    public void DiktatZurAuflösung()
    {
        ServerUtils.BroadcastImmediate("#DiktatZurAuflösung");
        StartWahlAbstimmung();
    }
    #endregion
    #region Sortieren
    public void StartSortieren()
    {
        ServerUtils.BroadcastImmediate("#StartSortieren");
        Lobby.SetActive(false);
        Sortieren.SetActive(true);

        for (int i = 0; i < SortierenListe.transform.childCount; i++)
            SortierenListe.transform.GetChild(i).gameObject.SetActive(false);
        SortierenListe.SetActive(true);

        SortierenTimer.gameObject.SetActive(false);
        SortierenAuswahl.SetActive(false);
        SortierenLoesung.SetActive(false);
    }
    public void SortierenChangeText(int change)
    {
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
        ServerUtils.BroadcastImmediate("#SortierenRunTimer " + input.text);
        StartCoroutine(SortierenRunTimer(int.Parse(input.text)));
    }
    IEnumerator SortierenRunTimer(int seconds)
    {
        yield return null;
        SortierenTimer.minValue = 0;
        SortierenTimer.maxValue = seconds * 1000; // Umrechnung in Millisekunden
        SortierenTimer.gameObject.SetActive(true);
        int milis = seconds * 1000;

        Debug.Log("Timer startet: " + DateTime.Now.ToString("HH:mm:ss:ffff"));
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
        Debug.Log("Timer ended: " + DateTime.Now.ToString("HH:mm:ss:ffff"));

        SortierenTimer.gameObject.SetActive(false);
        yield break;
    }
    public void SortierenGenSabos()
    {
        foreach (var item in sabotagePlayers)
            item.SetSaboteur(false);

        GenSaboteurForRound(2);
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
    public void SortierenShowGrenzen()
    {
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
        ServerUtils.BroadcastImmediate("#SortierenInitElement " + itemIndex + "|" + item + "|" + string.Join("~", Config.SABOTAGE_SPIEL.sortieren.GetInhalt()));
        
        for (int i = 2; i < SortierenLoesung.transform.childCount-1; i++)
            SortierenLoesung.transform.GetChild(i).GetChild(3).gameObject.SetActive(false);

        SortierenLoesung.transform.GetChild(itemIndex + 2).GetChild(1).gameObject.SetActive(false);
        SortierenLoesung.transform.GetChild(itemIndex + 2).GetChild(2).gameObject.SetActive(false);
        SortierenLoesung.transform.GetChild(itemIndex + 2).GetChild(3).gameObject.SetActive(false);

        SortierenListe.transform.GetChild(itemIndex + 1).gameObject.SetActive(true);
        SortierenListe.transform.GetChild(itemIndex + 1).GetComponent<TMP_InputField>().text = item;
        SortierenListe.transform.GetChild(itemIndex + 1).GetComponentInChildren<TMP_Text>().text = "1";

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

        SortierenListe.transform.GetChild(itemIndex + 1).gameObject.SetActive(true);
        SortierenListe.transform.GetChild(itemIndex + 1).GetComponent<TMP_InputField>().text = item;
        int tempcount = 0;
        for (int i = 1; i < SortierenListe.transform.childCount-1; i++)
        {
            if (SortierenListe.transform.GetChild(i).gameObject.activeInHierarchy)
            {
                tempcount++;
                SortierenListe.transform.GetChild(i).GetChild(1).GetComponent<TMP_Text>().text = "" + tempcount;
            }
        }
    }
    public void SortierenZurAuflösung()
    {
        ServerUtils.BroadcastImmediate("#MemoryZurAuflösung");
        StartWahlAbstimmung();
    }
    #endregion
    #region Memory
    public void StartMemory()
    {
        ServerUtils.BroadcastImmediate("#StartMemory");
        Lobby.SetActive(false);
        Memory.SetActive(true);
        MemoryTimer.gameObject.SetActive(false);
        MemoryGrid.SetActive(false);
        MemoryTap1.sprite = null;
        MemoryTap2.sprite = null;
        memoryblockclickitem = false;
        SetTeamPoints(500);
    }
    public void MemoryShowGrid()
    {
        string sequence = Config.SABOTAGE_SPIEL.memory.getSequence();
        ServerUtils.BroadcastImmediate("#MemoryShowGrid " + sequence);
        for (int i = 0; i < sequence.Split('~').Length; i++)
        {
            MemoryGrid.transform.GetChild(i).GetChild(0).GetChild(0).GetComponent<Image>().sprite = Resources.Load<Sprite>("Spiele/Sabotage/Memory/" + sequence.Split('~')[i]);
            MemoryGrid.transform.GetChild(i).GetChild(1).gameObject.SetActive(true);
            MemoryGrid.transform.GetChild(i).GetChild(1).GetComponentInChildren<TMP_Text>().text = "" + (i+1);
            MemoryGrid.transform.GetChild(i).GetChild(1).GetComponent<Button>().enabled = true;
            MemoryGrid.transform.GetChild(i).GetChild(1).GetComponent<Button>().interactable = true; // man sieht keine Lösung
        }
        MemoryGrid.SetActive(true);
    }
    bool memoryblockclickitem;
    public void MemoryClickItem(GameObject go)
    {
        if (!Config.SERVER_STARTED)
            return;
        if (memoryblockclickitem)
            return;
        string name = go.name.Replace("- (", "").Replace(")", "");
        if (MemoryTap1.sprite == null)
        {
            go.transform.GetChild(1).gameObject.SetActive(false);
            
            MemoryTap1.sprite = go.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite;
            MemoryTap1.name = name;
            go.transform.GetChild(1).gameObject.SetActive(false);
            ServerUtils.BroadcastImmediate("#MemoryClickItem " + MemoryTap1.name + "~" + MemoryTap1.sprite.name);
            return;
        }
        if (MemoryTap2.sprite == null)
        {
            memoryblockclickitem = true;
            go.transform.GetChild(1).gameObject.SetActive(false);

            MemoryTap2.sprite = go.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite;
            MemoryTap2.name = name;
            go.transform.GetChild(1).gameObject.SetActive(false);
            // Start Auflösung
            ServerUtils.BroadcastImmediate("#MemoryClickItems " + MemoryTap1.name + "~" + MemoryTap1.sprite.name + "|" + MemoryTap2.name + "~" + MemoryTap2.sprite.name);
            StartCoroutine(MemoryVerifyClicks(int.Parse(MemoryTap1.name), MemoryTap1.sprite.name, int.Parse(MemoryTap2.name), MemoryTap2.sprite.name));
        }
    }
    IEnumerator MemoryVerifyClicks(int pos1, string icon1, int pos2, string icon2)
    {
        yield return new WaitForSeconds(3f);
        if (!icon1.Equals(icon2))
        {
            AddTeamPoints(-5);
            AddSaboteurPoints(5);
            MemoryGrid.transform.GetChild(pos1).GetChild(1).gameObject.SetActive(true);
            MemoryGrid.transform.GetChild(pos2).GetChild(1).gameObject.SetActive(true);
        }

        MemoryTap1.sprite = null;
        MemoryTap2.sprite = null;
        memoryblockclickitem = false;
        yield break;
    }
    public void MemoryRunTimer(TMP_InputField input)
    {
        ServerUtils.BroadcastImmediate("#MemoryRunTimer " + input.text);
        StartCoroutine(MemoryRunTimer(int.Parse(input.text)));
    }
    IEnumerator MemoryRunTimer(int seconds)
    {
        yield return null;
        MemoryTimer.minValue = 0;
        MemoryTimer.maxValue = seconds * 1000; // Umrechnung in Millisekunden
        MemoryTimer.gameObject.SetActive(true);
        int milis = seconds * 1000;

        Debug.Log("Timer startet: " + DateTime.Now.ToString("HH:mm:ss:ffff"));
        while (milis >= 0)
        {
            MemoryTimer.GetComponentInChildren<Slider>().value = milis;

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
        Debug.Log("Timer ended: " + DateTime.Now.ToString("HH:mm:ss:ffff"));

        MemoryTimer.gameObject.SetActive(false);
        yield break;
    }
    public void MemoryGenSabos()
    {
        foreach (var item in sabotagePlayers)
            item.SetSaboteur(false);

        GenSaboteurForRound(1);
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
    public void MemoryZurAuflösung()
    {
        ServerUtils.BroadcastImmediate("#MemoryZurAuflösung");
        StartWahlAbstimmung();
    }
    #endregion
    #region DerZugLuegt
    Coroutine derzugluegtRunElement;
    public void StartDerZugLuegt()
    {
        derzugluegtBlockBuzzer = true;
        ServerUtils.BroadcastImmediate("#StartDerZugLuegt");
        Lobby.SetActive(false);
        DerZugLuegt.SetActive(true);
        DerZugLuegtAnzeigen.SetActive(false);
        GameObject.Find("DerZugLuegt/ClientBuzzer").SetActive(false);
        GameObject.Find("DerZugLuegt/ServerSide/Freigeben").GetComponentInChildren<TMP_Text>().text = "Freigeben";
    }
    public void DerZugLuegtChangeText(int change)
    {
        DerZugLuegtAnzeigen.SetActive(true);
        int newIndex = Config.SABOTAGE_SPIEL.derzugluegt.ChangeIndex(change);
        GameObject.Find("DerZugLuegt/ServerSide/Index").GetComponent<TMP_Text>().text = "Text: " + (newIndex + 1) + "/" + Config.SABOTAGE_SPIEL.derzugluegt.rounds.Count;

        Transform Elements = GameObject.Find("DerZugLuegt/ServerSide/Elements").transform;
        for (int i = 0; i < Elements.childCount; i++)
        {
            Elements.GetChild(i).GetComponentInChildren<TMP_Text>().text = Config.SABOTAGE_SPIEL.derzugluegt.GetElementType(i).ToString().Replace("True", "<color=red>Drücken</color>").Replace("False", "") + ": " + Config.SABOTAGE_SPIEL.derzugluegt.GetElement(i);
            Elements.GetChild(i).GetComponent<Button>().interactable = true;
        }
        GameObject.Find("DerZugLuegt/ServerSide/Thema").GetComponent<TMP_Text>().text = Config.SABOTAGE_SPIEL.derzugluegt.GetThema();
    }
    public void DerZugLuegtShowRound()
    {
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
        string element = ob.GetComponentInChildren<TMP_Text>().text;
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
        AddTeamPoints(50);
        ServerUtils.BroadcastImmediate("#DerZugLuegtRichtig " + GetTeamPoints());
        GameObject.Find("DerZugLuegt/ServerSide/Freigeben").GetComponentInChildren<TMP_Text>().text = "Freigeben";
        Correct.Play();
        derzugluegtBlockBuzzer = false;
    }
    public void DerZugLuegtFalsch()
    {
        AddSaboteurPoints(50);
        ServerUtils.BroadcastImmediate("#DerZugLuegtFalsch " + GetSaboteurPoints());
        GameObject.Find("DerZugLuegt/ServerSide/Freigeben").GetComponentInChildren<TMP_Text>().text = "Freigeben";
        Wrong.Play();
        derzugluegtBlockBuzzer = false;
    }
    public void DerZugLuegtFreigeben()
    {
        GameObject.Find("DerZugLuegt/ServerSide/Freigeben").GetComponentInChildren<TMP_Text>().text = "Freigeben";
        derzugluegtBlockBuzzer = false;
    }
    private void DerZugLuegtClientBuzzer(Player p)
    {
        if (derzugluegtBlockBuzzer)
            return;
        derzugluegtBlockBuzzer = true;

        ServerUtils.BroadcastImmediate("#DerZugLuegtBuzzer");
        GameObject.Find("DerZugLuegt/ServerSide/Freigeben").GetComponentInChildren<TMP_Text>().text = "Freigeben\nP:" + p.name;
        Buzzer.Play();
    }
    public void DerZugLuegtGenSabos()
    {
        foreach (var item in sabotagePlayers)
            item.SetSaboteur(false);

        GenSaboteurForRound(2);
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
    public void DerZugLuegtZurAuflösung()
    {
        ServerUtils.BroadcastImmediate("#DerZugLuegtZurAuflösung");
        StartWahlAbstimmung();
    }
    #endregion
    #region Tabu
    public void StartTabu()
    {
        ServerUtils.BroadcastImmediate("#StartTabu");
        Lobby.SetActive(false);
        Tabu.SetActive(true);
        TabuTimer.gameObject.SetActive(false);
        GameObject.Find("Tabu/GameObject/SaboTipp").SetActive(false);

        tabuRichtigePunkte = 0;
    }
    Coroutine tabutimer;
    int tabuRichtigePunkte;
    public void TabuRunTimer(TMP_InputField input)
    {
        ServerUtils.BroadcastImmediate("#TabuRunTimer " + input.text);
        if (tabutimer != null)
            StopCoroutine(tabutimer);
        tabutimer = StartCoroutine(TabuRunTimer(int.Parse(input.text)));
    }
    public void TabuStopTimer()
    {
        ServerUtils.BroadcastImmediate("#TabuStopTimer");
        if (tabutimer != null)
            StopCoroutine(tabutimer);
        TabuTimer.gameObject.SetActive(false);
    }
    IEnumerator TabuRunTimer(int seconds)
    {
        yield return null;
        TabuTimer.minValue = 0;
        TabuTimer.maxValue = seconds * 1000; // Umrechnung in Millisekunden
        TabuTimer.gameObject.SetActive(true);
        int milis = seconds * 1000;

        Debug.Log("Timer startet: " + DateTime.Now.ToString("HH:mm:ss:ffff"));
        while (milis >= 0)
        {
            TabuTimer.GetComponentInChildren<Slider>().value = milis;

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
        Debug.Log("Timer ended: " + DateTime.Now.ToString("HH:mm:ss:ffff"));

        TabuTimer.gameObject.SetActive(false);
        yield break;
    }
    public void TabuRichtig()
    {
        tabuRichtigePunkte += 50;
        AddTeamPoints(50);

        ServerUtils.BroadcastImmediate("#TabuRichtig " + GetTeamPoints() + "|" + GetSaboteurPoints());
    }
    public void TabuFalsch()
    {
        AddTeamPoints(-tabuRichtigePunkte);
        tabuRichtigePunkte = 0;
        AddSaboteurPoints(500);
        
        ServerUtils.BroadcastImmediate("#TabuFalsch " + GetTeamPoints() + "|" + GetSaboteurPoints());

        if (tabutimer != null)
            StopCoroutine(tabutimer);
        TabuTimer.gameObject.SetActive(false);
    }
    public void TabuShowKarteToPlayer(GameObject go)
    {
        int index = int.Parse(go.name);
        string tabu = Config.SABOTAGE_SPIEL.tabu.GetWort() + "|" + Config.SABOTAGE_SPIEL.tabu.GetTabus();
        ServerUtils.SendMSG("#TabuShowKarteToPlayer " + tabu, sabotagePlayers[index].player, false);
    }
    public void TabuChangeText(int change)
    {
        tabuRichtigePunkte = 0;
        int index = Config.SABOTAGE_SPIEL.tabu.ChangeIndex(change);
        GameObject.Find("Tabu/ServerSide/Index").GetComponent<TMP_Text>().text = "Text: " + (Config.SABOTAGE_SPIEL.tabu.index + 1) + "/" + Config.SABOTAGE_SPIEL.tabu.tabus.Count;

        string tabu = Config.SABOTAGE_SPIEL.tabu.GetWort() + "|" + Config.SABOTAGE_SPIEL.tabu.GetTabus();
        ServerUtils.BroadcastImmediate("#TabuSaboTipp " + tabu.Split('|')[0]);

        GameObject.Find("Tabu/GameObject/Wort").GetComponent<TMP_Text>().text = "Start: " +sabotagePlayers[index].player.name + " - " + tabu.Split('|')[0];
        GameObject.Find("Tabu/GameObject/Tabu").GetComponent<TMP_Text>().text = "<color=red>No-Go: </color>" + tabu.Split('|')[1];
    }
    public void TabuGenSabos()
    {
        foreach (var item in sabotagePlayers)
            item.SetSaboteur(false);

        GenSaboteurForRound(2);
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
    public void TabuZurAuflösung()
    {
        ServerUtils.BroadcastImmediate("#TabuZurAuflösung");
        StartWahlAbstimmung();
    }
    #endregion
    #region Auswahlstrategie
    public void StartAuswahlstrategie()
    {
        ServerUtils.BroadcastImmediate("#StartAuswahlstrategie");
        Lobby.SetActive(false);
        Auswahlstrategie.SetActive(true);
        AuswahlstrategieTimer.gameObject.SetActive(false);
        AuswahlstrategieGrid.gameObject.SetActive(false);
    }
    List<Sprite> auswahlstrategieAuswahl1;
    List<Sprite> auswahlstrategieAuswahl2;
    Coroutine auswahlstrategietimer;
    string auswahlstrategieGewaehltesBild;
    public void AuswahlstrategieRunTimer(TMP_InputField input)
    {
        ServerUtils.BroadcastImmediate("#AuswahlstrategieRunTimer " + input.text);
        if (auswahlstrategietimer != null)
            StopCoroutine(auswahlstrategietimer);
        auswahlstrategietimer = StartCoroutine(AuswahlstrategieRunTimer(int.Parse(input.text)));
    }
    public void AuswahlstrategieStopTimer()
    {
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

        Debug.Log("Timer startet: " + DateTime.Now.ToString("HH:mm:ss:ffff"));
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
        Debug.Log("Timer ended: " + DateTime.Now.ToString("HH:mm:ss:ffff"));

        AuswahlstrategieTimer.gameObject.SetActive(false);
        yield break;
    }
    public void AuswahlstrategieChangeText(int change)
    {
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
        AddTeamPoints(500);
        int auswahlitem = -1;
        for (int i = 0; i < 7; i++)
            if (AuswahlstrategieGrid.GetChild(i).GetComponent<Image>().enabled)
                auswahlitem = i;

        ServerUtils.BroadcastImmediate("#AuswahlstrategieRichtig " + GetTeamPoints() + "|" + GetSaboteurPoints() + "|" + auswahlitem);
    }
    public void AuswahlstrategieFalsch()
    {
        AddSaboteurPoints(500);
        int auswahlitem = -1;
        for (int i = 0; i < 7; i++)
            if (AuswahlstrategieGrid.GetChild(i).GetComponent<Image>().enabled)
                auswahlitem = i;

        ServerUtils.BroadcastImmediate("#AuswahlstrategieFalsch " + GetTeamPoints() + "|" + GetSaboteurPoints() + "|" + auswahlitem);
    }
    public void AuswahlstrategieGenSabos()
    {
        foreach (var item in sabotagePlayers)
            item.SetSaboteur(false);

        GenSaboteurForRound(2);
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
    public void AuswahlstrategieZurAuflösung()
    {
        ServerUtils.BroadcastImmediate("#AuswahlstrategieZurAuflösung");
        StartWahlAbstimmung();
    }
    #endregion

    #region Utils
    private void GenSaboteurForRound(int saboteurCount) // 1 oder 2
    {
        // Diktat                       // s1
        // Sortieren (Listen)           // s2 + s4
        // Memory                       // s3
        // Der Zug Lügt                 // s4 + s5
        // Tabu                         // s5 + s3
        // Auswahlstrategie             // s2 + s1

        List<SabotagePlayer> nonSabos = new List<SabotagePlayer>();
        foreach (var item in sabotagePlayers)
            if (item.wasSaboteur == 0)
                nonSabos.Add(item);
        List<SabotagePlayer> allSabos = new List<SabotagePlayer>();
        foreach (var item in sabotagePlayers)
            if (item.wasSaboteur < 2)
                allSabos.Add(item);

        if (allSabos.Count == 0)
            return;

        if (saboteurCount == 2)
        {
            if (nonSabos.Count > 0)
            {
                SabotagePlayer newSabo = nonSabos[UnityEngine.Random.Range(0, nonSabos.Count)];
                allSabos.Remove(newSabo);
                nonSabos.Remove(newSabo);
                newSabo.SetSaboteur(true);
                saboteurCount--;
            }

            for (int i = 0; i < saboteurCount; i++)
            {
                SabotagePlayer newSabo = allSabos[UnityEngine.Random.Range(0, allSabos.Count)];
                allSabos.Remove(newSabo);
                newSabo.SetSaboteur(true);
            }
        }
        else
        {
            SabotagePlayer newSabo = allSabos[UnityEngine.Random.Range(0, allSabos.Count)];
            allSabos.Remove(newSabo);
            newSabo.SetSaboteur(true);
        }
    }
    private void SetSaboteurPoints(int punkte)
    {
        GameObject.Find("Punktetafel/SaboteurPunkte").GetComponent<TMP_InputField>().text = "" + punkte;
    }
    private void SetTeamPoints(int punkte)
    {
        GameObject.Find("Punktetafel/TeamPunkte").GetComponent<TMP_InputField>().text = "" + punkte;
    }
    private void AddSaboteurPoints(int punkte)
    {
        GameObject.Find("Punktetafel/SaboteurPunkte").GetComponent<TMP_InputField>().text = "" +
            (int.Parse(GameObject.Find("Punktetafel/SaboteurPunkte").GetComponent<TMP_InputField>().text) + punkte);
    }
    private void AddTeamPoints(int punkte)
    {
        GameObject.Find("Punktetafel/TeamPunkte").GetComponent<TMP_InputField>().text = "" +
            (int.Parse(GameObject.Find("Punktetafel/TeamPunkte").GetComponent<TMP_InputField>().text) + punkte);
    }
    private int GetSaboteurPoints()
    {
        return int.Parse(GameObject.Find("Punktetafel/SaboteurPunkte").GetComponent<TMP_InputField>().text);
    }
    private int GetTeamPoints()
    {
        return int.Parse(GameObject.Find("Punktetafel/TeamPunkte").GetComponent<TMP_InputField>().text);
    }
    public void ChangeTeamsPoints()
    {
        ServerUtils.BroadcastImmediate("#TeamPunkte " +
            GameObject.Find("Punktetafel/TeamPunkte").GetComponent<TMP_InputField>().text + "*" +
            GameObject.Find("Punktetafel/SaboteurPunkte").GetComponent<TMP_InputField>().text);
    }
    #endregion
}
