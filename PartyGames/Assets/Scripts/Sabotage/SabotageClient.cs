using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SabotageClient : MonoBehaviour
{
    bool pressingbuzzer = false;
    int connectedPlayers;
    [SerializeField] AudioSource Beeep;
    [SerializeField] AudioSource Moeoop;
    [SerializeField] AudioSource DisconnectSound;
    [SerializeField] AudioSource Buzzer;
    [SerializeField] AudioSource Correct;
    [SerializeField] AudioSource Wrong;

    SabotagePlayer[] sabotagePlayers;
    GameObject WerIstSabo;

    bool LobbyShowAllPlayerPoints;
    GameObject Lobby;
    Slider LobbyTokenSlider;
    Slider LobbyTimer;

    GameObject SaboteurWahlAufloesung;
    GameObject SaboteurWahlAufloesungAbstimmung;
    GameObject SaboteurWahlAufloesungPunkteverteilung;
    Slider AbstimmungTimer;

    GameObject Diktat;
    TMP_InputField DiktatLoesung;
    Slider DiktatTimer;
    GameObject DiktatSaboHinweis;

    GameObject Sortieren;
    Slider SortierenTimer;
    GameObject SortierenListe;
    GameObject SortierenAuswahl;
    GameObject SortierenLoesung;
    GameObject SortierenSaboHinweis;

    GameObject DerZugLuegt;
    GameObject DerZugLuegtAnzeigen;
    GameObject DerZugLuegtSaboHinweis;

    GameObject Tabu;
    Slider TabuTimer;
    GameObject TabuSaboHinweis;

    GameObject Auswahlstrategie;
    Transform AuswahlstrategieGrid;
    Slider AuswahlstrategieTimer;
    GameObject AuswahlstrategieSaboHinweis;

    GameObject Sloxikon;
    Slider SloxikonTimer;
    GameObject SloxikonVorschlag1;
    GameObject SloxikonVorschlag2;
    GameObject SloxikonVorschlag3;
    GameObject SloxikonSaboEingabe;

    void OnEnable()
    {
        if (!Config.CLIENT_STARTED)
            return;
        InitAnzeigen();
        ClientUtils.SendToServer("#JoinSabotage");

        StartCoroutine(TestConnectionToServer());
    }

    void Update()
    {
        // Leertaste kann Buzzern
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!pressingbuzzer)
            {
                pressingbuzzer = true;
                SpielerBuzzered();
            }
        }
        else if (Input.GetKeyUp(KeyCode.Space) && pressingbuzzer)
        {
            pressingbuzzer = false;
        }

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
        Logging.log(Logging.LogType.Normal, "SabotageClient", "OnApplicationQuit", "Client wird geschlossen.");
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
        Logging.log(Logging.LogType.Debug, "SabotageClient", "TestConnectionToServer", "Testet die Verbindumg zum Server.");
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
        Logging.log(Logging.LogType.Debug, "SabotageClient", "Commands", "Eingehende Nachricht: " + cmd + " -> " + data);
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "SabotageClient", "Commands", "Unkown Command: " + cmd + " -> " + data);
                break;

            #region Universal Commands
            case "#ServerClosed":
                Logging.log(Logging.LogType.Normal, "SabotageClient", "Commands", "Verbindumg zum Server wurde beendet. Lade ins Hauptmenü.");
                CloseSocket();
                SceneManager.LoadSceneAsync("Startup");
                break;
            case "#ZurueckInsHauptmenue":
                Logging.log(Logging.LogType.Normal, "SabotageClient", "Commands", "Spiel wird beendet. Lade ins Hauptmenü");
                SceneManager.LoadSceneAsync("Startup");
                break;
            case "#ClientFocusChange":
                int id = int.Parse(data.Split('*')[0])-1;
                if (id < 0 || id >= sabotagePlayers.Length)
                    break;
                if (sabotagePlayers == null || sabotagePlayers.Length < id || sabotagePlayers[id] == null)
                    break;
                sabotagePlayers[id].SetAusgetabbt(!bool.Parse(data.Split('*')[1]));
                //Player player = Config.PLAYERLIST[id];
                //Config.SABOTAGE_SPIEL.getPlayerByPlayer(sabotagePlayers, player).SetAusgetabbt(!bool.Parse(data.Split('*')[1]));
                break;
            #endregion
            case "#UpdateSpieler":
                UpdateSpieler(data);
                break;
            case "#UpdateTeamSaboPunkte":
                UpdateTeamSaboPunkte(data);
                break;

            case "#LobbyStartTokenPlacement":
                LobbyStartTokenPlacement(data);
                break;
            case "#LobbyPlayerSeeAllPointsToggle":
                LobbyPlayerSeeAllPointsToggle(data); 
                break;
            case "#DuBistSaboteur":
                DuBistSaboteur(data);
                break;

            case "#AbstimmungStart":
                AbstimmungStart();
                break;
            case "#AufloesungStart":
                AufloesungStart(data);
                break;
            case "#AufloesungZeigeSabos":
                AufloesungZeigeSabos(data);
                break;
            case "#AufloesungZeigeSabosNicht":
                AufloesungZeigeSabosNicht(data);
                break;
            case "#AufloesungZurLobby":
                AufloesungZurLobby();
                break;
            case "#AbstimmungRunTimer":
                AbstimmungRunTimer(data);
                break;

            case "#StartDiktat":
                StartDiktat();
                break;
            case "#DiktatSaboTipp":
                DiktatSaboTipp(data);
                break;
            case "#DiktatCheckInputs":
                DiktatCheckInputs(data); 
                break;
            case "#DiktatZurAuflösung":
                DiktatZurAuflösung();
                break;
            case "#DiktatRunTimer":
                DiktatRunTimer(data);
                break;
            case "#DiktatStopTimer":
                DiktatStopTimer();
                break;

            case "#StartSortieren":
                StartSortieren();
                break;
            case "#SortierenSaboTipp":
                SortierenSaboTipp(data);
                break;
            case "#SortierenRunTimer":
                SortierenRunTimer(data);
                break;
            case "#SortierenStopTimer":
                SortierenStopTimer();
                break;
            case "#SortierenShowGrenzen":
                SortierenShowGrenzen(data);
                break;
            case "#SortierenInitElement":
                SortierenShowElementInit(data); 
                break;
            case "#SortierenShowElement":
                SortierenShowElement(data);
                break;
            case "#SortierenZurAuflösung":
                SortierenZurAuflösung();
                break;

            case "#StartDerZugLuegt":
                StartDerZugLuegt();
                break;
            case "#DerZugLuegtShowRound":
                DerZugLuegtShowRound(data);
                break;
            case "#DerZugLuegtStartElement":
                DerZugLuegtStartElement(data);
                break;
            case "#DerZugLuegtRichtig":
                DerZugLuegtRichtig(data);
                break;
            case "#DerZugLuegtFalsch":
                DerZugLuegtFalsch(data);
                break;
            case "#DerZugLuegtZurAuflösung":
                DerZugLuegtZurAuflösung();
                break;
            case "#DerZugLuegtBuzzer":
                DerZugLuegtClientBuzzer();
                break;

            case "#StartTabu":
                StartTabu();
                break;
            case "#TabuRunTimer":
                TabuRunTimer(data);
                break;
            case "#TabuStopTimer":
                TabuStopTimer();
                break;
            case "#TabuRichtig":
                TabuRichtig(data);
                break;
            case "#TabuFalsch":
                TabuFalsch(data);
                break;
            case "#TabuGrenzwertig":
                TabuGrenzwertig();
                break;
            case "#TabuShowKarteToPlayer":
                TabuShowKarteToPlayer(data);
                break;
            case "#TabuZurAuflösung":
                TabuZurAuflösung();
                break;
            case "#TabuSaboTipp":
                TabuSaboTipp(data);
                break;
            case "#TabuNewTabuWords":
                TabuShowNewWords(data);
                break;

            case "#StartAuswahlstrategie":
                StartAuswahlstrategie();
                break;
            case "#AuswahlstrategieRunTimer":
                AuswahlstrategieRunTimer(data);
                break;
            case "#AuswahlstrategieStopTimer":
                AuswahlstrategieStopTimer();
                break;
            case "#AuswahlstrategieShowSaboTipp":
                AuswahlstrategieShowSaboTipp(data);
                break;
            case "#AuswahlstrategieShowFirstAuswahl":
                AuswahlstrategieShowFirstAuswahl(data);
                break;
            case "#AuswahlstrategieShowSecondAuswahl":
                AuswahlstrategieShowSecondAuswahl(data);
                break;
            case "#AuswahlstrategieRichtig":
                AuswahlstrategieRichtig(data);
                break;
            case "#AuswahlstrategieFalsch":
                AuswahlstrategieFalsch(data);
                break;
            case "#AuswahlstrategieZurAuflösung":
                AuswahlstrategieZurAuflösung();
                break;

            case "#StartSloxikon":
                StartSloxikon();
                break;
            case "#SloxikonRunTimer":
                SloxikonRunTimer(data);
                break;
            case "#SloxikonStopTimer":
                SloxikonStopTimer();
                break;
            case "#SloxikonShowSaboTipp":
                SloxikonChangeText(data);
                break;
            case "#SloxikonClearFelder":
                SloxikonClearFelder();
                break;
            case "#SloxikonShowSaboEingabe":
                SloxikonShowSaboEingabe(data);
                break;
            case "#SloxikonZeigeMoeglichkeiten":
                SloxikonZeigeMoeglichkeiten(data);
                break;
            case "#SloxikonZurAuflösung":
                SloxikonZurAuflösung();
                break;
            case "#SloxikonSaboEingaben":
                SloxikonSaboEingaben(data); 
                break;
        } 
    }
    /// <summary>
    /// Initialisiert die Anzeigen der Scene
    /// </summary>
    private void InitAnzeigen()
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "InitAnzeigen", "Anzeigen werden initialisiert...");
        connectedPlayers = 0;

        // Allgemein
        sabotagePlayers = new SabotagePlayer[5];
        for (int i = 0; i < sabotagePlayers.Length; i++)
            sabotagePlayers[i] = new SabotagePlayer(Config.PLAYERLIST[i], GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")"));

        Transform modi = GameObject.Find("Modi").transform;
        for (int i = 0; i < modi.childCount; i++)
            modi.GetChild(i).gameObject.SetActive(true);

        // Lobby
        LobbyShowAllPlayerPoints = true;
        Lobby = GameObject.Find("Modi/Lobby");
        Lobby.SetActive(true);
        Lobby.transform.GetChild(0).gameObject.SetActive(true);
        Lobby.transform.GetChild(1).gameObject.SetActive(true);
        LobbyTokenSlider = GameObject.Find("Lobby/Client/TokenSlider").GetComponent<Slider>();
        LobbyTokenSlider.maxValue = 1;
        LobbyTokenSlider.minValue = 0;
        LobbyTokenSlider.value = 0;
        LobbyTokenSlider.gameObject.SetActive(false);
        LobbyTimer = GameObject.Find("Lobby/Timer").GetComponent<Slider>();
        LobbyTimer.maxValue = 1;
        LobbyTimer.minValue = 0;
        LobbyTimer.value = 0;
        LobbyTimer.gameObject.SetActive(false);
        GameObject.Find("Lobby/Server").SetActive(false);

        // SaboteurAnzeige
        WerIstSabo = GameObject.Find("SpielerAnzeigen/WerIstSaboteur");
        WerIstSabo.SetActive(false);
        WerIstSabo.transform.GetChild(0).GetComponent<TMP_Text>().text = "Keiner";
        WerIstSabo.transform.GetChild(1).GetComponent<TMP_Text>().text = "Du bist alleine";

        // SaboteurWahl & Aufloesung
        SaboteurWahlAufloesung = GameObject.Find("SaboteurWahl&Aufloesung");
        modi = SaboteurWahlAufloesung.transform;
        for (int i = 0; i < modi.childCount - 1; i++)
            modi.GetChild(i).gameObject.SetActive(true);
        SaboteurWahlAufloesung = GameObject.Find("SaboteurWahl&Aufloesung");
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
        DiktatSaboHinweis = GameObject.Find("Diktat/SaboHinweis");
        DiktatSaboHinweis.SetActive(false);
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
        SortierenAuswahl.SetActive(false);
        SortierenLoesung = GameObject.Find("Sortieren/LösungListe");
        SortierenLoesung.gameObject.SetActive(false);
        SortierenSaboHinweis = GameObject.Find("Sortieren/SaboHinweis");
        SortierenSaboHinweis.SetActive(false);
        Sortieren = GameObject.Find("Modi/Sortieren");
        Sortieren.SetActive(false);

        // DerZugLuegt
        DerZugLuegtAnzeigen = GameObject.Find("DerZugLuegt/GameObject");
        DerZugLuegtAnzeigen.SetActive(false);
        DerZugLuegtSaboHinweis = GameObject.Find("DerZugLuegt/SaboHinweis");
        DerZugLuegtSaboHinweis.SetActive(false);
        DerZugLuegt = GameObject.Find("Modi/DerZugLuegt");
        DerZugLuegt.gameObject.SetActive(false);

        // Tabu
        TabuTimer = GameObject.Find("Tabu/Timer").GetComponent<Slider>();
        TabuTimer.maxValue = 1;
        TabuTimer.minValue = 0;
        TabuTimer.value = 0;
        TabuSaboHinweis = GameObject.Find("Tabu/SaboHinweis");
        TabuSaboHinweis.SetActive(false);
        Tabu = GameObject.Find("Modi/Tabu");
        Tabu.gameObject.SetActive(false);

        // Auswahlstrategie
        AuswahlstrategieTimer = GameObject.Find("Auswahlstrategie/Timer").GetComponent<Slider>();
        AuswahlstrategieTimer.maxValue = 1;
        AuswahlstrategieTimer.minValue = 0;
        AuswahlstrategieTimer.value = 0;
        AuswahlstrategieSaboHinweis = GameObject.Find("Auswahlstrategie/SaboHinweis");
        AuswahlstrategieSaboHinweis.SetActive(false);
        AuswahlstrategieGrid = GameObject.Find("Auswahlstrategie/Grid").transform;
        AuswahlstrategieGrid.gameObject.SetActive(false);
        Auswahlstrategie = GameObject.Find("Modi/Auswahlstrategie");
        Auswahlstrategie.gameObject.SetActive(false);

        // Sloxikon
        SloxikonTimer = GameObject.Find("Sloxikon/Timer").GetComponent<Slider>();
        SloxikonTimer.maxValue = 1;
        SloxikonTimer.minValue = 0;
        SloxikonTimer.value = 0;
        Sloxikon = GameObject.Find("Modi/Sloxikon");
        Sloxikon.gameObject.SetActive(false);
        SloxikonVorschlag1 = Sloxikon.transform.GetChild(3).gameObject;
        SloxikonVorschlag2 = Sloxikon.transform.GetChild(4).gameObject;
        SloxikonVorschlag3 = Sloxikon.transform.GetChild(5).gameObject;
        SloxikonSaboEingabe = Sloxikon.transform.GetChild(6).gameObject;
    }
    /// <summary>
    /// Aktualisiert die Spieler Anzeigen
    /// </summary>
    /// <param name="data">#UpdateSpieler ...</param>
    private void UpdateSpieler(string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "UpdateSpieler", data);
        int connectedplayer = 0;
        for (int i = 0; i < sabotagePlayers.Length; i++)
        {
            string infos = data.Replace("[" + (i+1) + "]", "|").Split('|')[1];
            sabotagePlayers[i].SetPunkte(int.Parse(infos.Replace("[PUNKTE]", "|").Split('|')[1]));

            if (bool.Parse(infos.Replace("[ONLINE]", "|").Split('|')[1]))
                connectedplayer++;
            else
                sabotagePlayers[i].DeleteImage();
        }
        if (!LobbyShowAllPlayerPoints)
            foreach (var item in sabotagePlayers)
                if (item.player.id != Config.PLAYER_ID)
                    item.HidePunkte();
        if (connectedplayer < connectedPlayers)
        {
            connectedPlayers = connectedplayer;
            DisconnectSound.Play();
        }
    }
    /// <summary>
    /// Sendet eine Buzzer Anfrage an den Server
    /// </summary>
    public void SpielerBuzzered()
    {
        ClientUtils.SendToServer("#SpielerBuzzered");
    }
    
    #region Lobby
    private void LobbyPlayerSeeAllPointsToggle(string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "LobbyPlayerSeeAllPointsToggle", data);
        LobbyShowAllPlayerPoints = bool.Parse(data);

        if (!LobbyShowAllPlayerPoints)
        {
            foreach (var item in sabotagePlayers)
                if (item.player.id != Config.PLAYER_ID)
                    item.HidePunkte();
        }
        else
        {
            foreach (var item in sabotagePlayers)
                if (item.player.id != Config.PLAYER_ID)
                    item.AddPunkte(0);
        } 
    }
    Coroutine lobbytokens;
    private void LobbyStartTokenPlacement(string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "LobbyStartTokenPlacement", data);
        sabotagePlayers[Config.PLAYER_ID - 1].placedTokens = 0;
        sabotagePlayers[Config.PLAYER_ID - 1].saboteurTokens = int.Parse(data.Split("|")[1].Split("~")[Config.PLAYER_ID - 1]);

        if (sabotagePlayers[Config.PLAYER_ID - 1].saboteurTokens == 0)
        {
            LobbyTokenSlider.gameObject.SetActive(false);
        }
        else
        {
            LobbyTokenSlider.minValue = 0;
            LobbyTokenSlider.maxValue = sabotagePlayers[Config.PLAYER_ID - 1].saboteurTokens;
            LobbyTokenSlider.value = 0;
            LobbyTokenSlider.gameObject.SetActive(true);
        }

        if (lobbytokens != null)
            StopCoroutine(lobbytokens);
        lobbytokens = StartCoroutine(LobbyRunTimer(int.Parse(data.Split("|")[0])));
    }
    IEnumerator LobbyRunTimer(int seconds)
    {
        yield return null;
        LobbyTimer.minValue = 0;
        LobbyTimer.maxValue = seconds * 1000; // Umrechnung in Millisekunden
        LobbyTimer.gameObject.SetActive(true);
        int milis = seconds * 1000;

        Logging.log(Logging.LogType.Debug, "SabotageClient", "LobbyRunTimer", "Seconds: " + seconds + "  Timer started: " + DateTime.Now.ToString("HH:mm:ss:ffff"));
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
        Logging.log(Logging.LogType.Debug, "SabotageClient", "LobbyRunTimer", "Seconds: " + seconds + "  Timer ended: " + DateTime.Now.ToString("HH:mm:ss:ffff"));
        LobbyTimer.gameObject.SetActive(false);
        LobbyTokenSlider.gameObject.SetActive(false);
        yield break;
    }
    public void SaboteurTokenSlider(Slider slider)
    {
        if (!Config.CLIENT_STARTED)
            return;
        Logging.log(Logging.LogType.Debug, "SabotageClient", "SaboteurTokenSlider", "Value: " + slider.value + " MaxValue: " + slider.maxValue + " MinValue: " + slider.minValue);
        slider.transform.GetChild(0).GetComponent<TMP_Text>().text = slider.value + "";
        sabotagePlayers[Config.PLAYER_ID - 1].placedTokens = int.Parse(slider.value + "");
        ClientUtils.SendToServer("#LobbyClientplazedTokens " + slider.value);
    }
    #endregion
    #region Wahl & Abstimmung
    private void StartWahlAbstimmung()
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "StartWahlAbstimmung", "");
        // Blende alles aus
        Transform modi = GameObject.Find("Modi").transform;
        for (int i = 0; i < modi.childCount; i++)
            modi.GetChild(i).gameObject.SetActive(false);

        SaboteurWahlAufloesung.SetActive(true);
        SaboteurWahlAufloesungAbstimmung.SetActive(false);
        SaboteurWahlAufloesungPunkteverteilung.SetActive(false);
        GameObject ServerSide = GameObject.Find("SaboteurWahl&Aufloesung/Server");
        if (ServerSide != null)
            ServerSide.gameObject.SetActive(false);
    }
    // Abstimmung
    private void AbstimmungStart()
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "AbstimmungStart", ""); 
        SaboteurWahlAufloesungAbstimmung.SetActive(true);
        SaboteurWahlAufloesungPunkteverteilung.SetActive(false);
        GameObject.Find("SaboteurWahl&Aufloesung/Abstimmung/Server").SetActive(false);

        if (sabotagePlayers[Config.PLAYER_ID-1].isSaboteur)
        {
            SaboteurWahlAufloesungAbstimmung.transform.GetChild(0).gameObject.SetActive(true);
            SaboteurWahlAufloesungAbstimmung.transform.GetChild(1).gameObject.SetActive(false);
        }
        else
        {
            SaboteurWahlAufloesungAbstimmung.transform.GetChild(0).gameObject.SetActive(false);
            SaboteurWahlAufloesungAbstimmung.transform.GetChild(1).gameObject.SetActive(true);
            for (int i = 0; i < sabotagePlayers.Length; i++)
            {
                GameObject.Find("SaboteurWahl&Aufloesung/Abstimmung/DuBistKeinSabo/Icon (" + (sabotagePlayers[i].player.id - 1) + ")").transform.GetChild(0).
                    GetComponent<Image>().sprite = sabotagePlayers[i].player.icon2.icon;
                GameObject.Find("SaboteurWahl&Aufloesung/Abstimmung/DuBistKeinSabo/Icon (" + (sabotagePlayers[i].player.id - 1) + ")").transform.GetChild(0).
                    GetComponent<Button>().interactable = true;
                GameObject.Find("SaboteurWahl&Aufloesung/Abstimmung/DuBistKeinSabo/Icon (" + (sabotagePlayers[i].player.id - 1) + ")").
                    GetComponent<Image>().enabled = false;
            }
            GameObject.Find("SaboteurWahl&Aufloesung/Abstimmung/DuBistKeinSabo/Icon (" + (Config.PLAYER_ID - 1) + ")").transform.GetChild(0).
                    GetComponent<Button>().interactable = false;
            GameObject.Find("SaboteurWahl&Aufloesung/Abstimmung/DuBistKeinSabo/Icon (" + (Config.PLAYER_ID - 1) + ")").
                    GetComponent<Image>().enabled = false;
        }
    }
    private void AbstimmungRunTimer(string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "AbstimmungRunTimer", data);
        StartCoroutine(AbstimmungRunTimer(int.Parse(data)));
    }
    IEnumerator AbstimmungRunTimer(int seconds)
    {
        yield return null;
        AbstimmungTimer.minValue = 0;
        AbstimmungTimer.maxValue = seconds * 1000; // Umrechnung in Millisekunden
        AbstimmungTimer.gameObject.SetActive(true);
        int milis = seconds * 1000;

        Logging.log(Logging.LogType.Debug, "SabotageClient", "AbstimmungRunTimer", "Seconds: " + seconds + "  Timer started: " + DateTime.Now.ToString("HH:mm:ss:ffff"));
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
        Logging.log(Logging.LogType.Debug, "SabotageClient", "AbstimmungRunTimer", "Seconds: " + seconds + "  Timer ended: " + DateTime.Now.ToString("HH:mm:ss:ffff"));

        AbstimmungTimer.gameObject.SetActive(false);
        yield return new WaitForSeconds(2f);

        yield break;
    }
    public void AbstimmungClientStimmtFuer(GameObject btnImage)
    {
        if (!Config.CLIENT_STARTED)
            return;

        Logging.log(Logging.LogType.Debug, "SabotageClient", "AbstimmungClientStimmtFuer", btnImage.name);

        if (int.Parse(btnImage.name) == (Config.PLAYER_ID - 1))
            return;

        if (btnImage.transform.parent.GetComponent<Image>().enabled)
        {
            btnImage.transform.parent.GetComponent<Image>().enabled = false;
            ClientUtils.SendToServer("#ClientStimmtGegen " + btnImage.gameObject.name);
        }
        else
        {
            btnImage.transform.parent.GetComponent<Image>().enabled = true;
            ClientUtils.SendToServer("#ClientStimmtFuer " + btnImage.gameObject.name);
        }
    }
    private void AufloesungStart(string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "AufloesungStart", data);
        SaboteurWahlAufloesungAbstimmung.SetActive(false);
        SaboteurWahlAufloesungPunkteverteilung.SetActive(true);
        SaboteurWahlAufloesungPunkteverteilung.transform.GetChild(0).gameObject.SetActive(false);

        for (int i = 0; i < sabotagePlayers.Length; i++)
        {
            SaboteurWahlAufloesungPunkteverteilung.transform.GetChild(1 + i).gameObject.GetComponent<TMP_Text>().text = "" + data.Split('|')[0].Split('~')[i];
        }
        for (int i = 0; i < data.Split('|')[1].Split('~').Length; i++)
        {
            sabotagePlayers[i].SetHiddenPoins(int.Parse(data.Split('|')[1].Split('~')[i]));
        }
            

        Transform SaboAnzeige = SaboteurWahlAufloesungPunkteverteilung.transform.GetChild(6);
        SaboAnzeige.gameObject.SetActive(false);
        SaboAnzeige.GetChild(2).GetComponent<TMP_Text>().text = "";
        SaboAnzeige.GetChild(0).gameObject.SetActive(false);
        SaboAnzeige.GetChild(1).gameObject.SetActive(false);
    }
    // Punkteverteilung
    private void AufloesungZeigeSabos(string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "AufloesungZeigeSabos", data);
        Transform SaboAnzeige = SaboteurWahlAufloesungPunkteverteilung.transform.GetChild(6);
        SaboAnzeige.gameObject.SetActive(false);
        SaboAnzeige.GetChild(2).GetComponent<TMP_Text>().text = "";

        SaboAnzeige.GetChild(0).gameObject.SetActive(false);
        SaboAnzeige.GetChild(1).gameObject.SetActive(false);
        SaboAnzeige.gameObject.SetActive(true);

        StartCoroutine(AufloesungVerteilePunkte(SaboAnzeige, int.Parse(data.Split('|')[0]), int.Parse(data.Split('|')[1]), int.Parse(data.Split('|')[2]), data.Split('|')[3], data.Split('|')[4]));
    }
    private void AufloesungZeigeSabosNicht(string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "AufloesungZeigeSabosNicht", data);
        Transform SaboAnzeige = SaboteurWahlAufloesungPunkteverteilung.transform.GetChild(6);
        SaboAnzeige.gameObject.SetActive(false);
        SaboAnzeige.GetChild(2).GetComponent<TMP_Text>().text = "";

        SaboAnzeige.GetChild(0).gameObject.SetActive(false);
        SaboAnzeige.GetChild(1).gameObject.SetActive(false);
        SaboAnzeige.gameObject.SetActive(true);

        StartCoroutine(AufloesungVerteilePunkte(SaboAnzeige, int.Parse(data.Split('|')[0]), int.Parse(data.Split('|')[1]), int.Parse(data.Split('|')[2]), data.Split('|')[3], data.Split('|')[4], false));
    }
    IEnumerator AufloesungVerteilePunkte(Transform SaboAnzeige, int countSabos, int teampunkte, int sabopunkte, string bonuspunkte, string sabostring, bool showSabos = true)
    {
        yield return new WaitForSeconds(3);
        string sabos = "";
        Sprite Sabo1 = null;
        Sprite Sabo2 = null;
        foreach (var item in sabotagePlayers)
        {
            if (sabostring.Contains(item.player.id+""))
            {
                item.ClientSetSabo(true);
                sabos += " & " + item.player.name;
                if (Sabo1 == null)
                    Sabo1 = item.player.icon2.icon;
                else if (Sabo2 == null)
                    Sabo2 = item.player.icon2.icon;
            }
            else
            {
                item.SetSaboteur(false);
            }
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

        if (!LobbyShowAllPlayerPoints)
            foreach (var item in sabotagePlayers)
                if (item.player.id != Config.PLAYER_ID)
                    item.HidePunkte();
        yield break;
    }
    private void AufloesungZurLobby()
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "AufloesungZurLobby", "");
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
    private void StartDiktat()
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "StartDiktat", "");
        Lobby.SetActive(false);
        Diktat.SetActive(true);
        diktatblocksendChange = false;
        GameObject ServerSide = GameObject.Find("Diktat/ServerSide");
        if (ServerSide != null)
            ServerSide.gameObject.SetActive(false);
        DiktatSaboHinweis.SetActive(false);
        if (sabotagePlayers[Config.PLAYER_ID - 1].isSaboteur)
            DiktatSaboHinweis.SetActive(true);
        DiktatLoesung.gameObject.SetActive(false);
        // Leere Eingabefelder
        Transform SpielerEingabeFelder = GameObject.Find("Diktat/SpielerEingabeFelder").transform;
        for (int i = 0; i < SpielerEingabeFelder.childCount; i++)
        {
            SpielerEingabeFelder.GetChild(i).GetComponent<TMP_InputField>().text = "";
            SpielerEingabeFelder.GetChild(i).GetComponent<TMP_InputField>().interactable = false;
        }
        SpielerEingabeFelder.GetChild(Config.PLAYER_ID - 1).GetComponent<TMP_InputField>().interactable = true;
    }
    private void DiktatSaboTipp(string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "DiktatSaboTipp", data);
        if (Config.SABOTAGE_SPIEL.getPlayerByPlayer(sabotagePlayers, Config.PLAYERLIST[Config.PLAYER_ID - 1]).isSaboteur)
        {
            DiktatLoesung.text = data;
            DiktatLoesung.gameObject.SetActive(true);
        }
        else
            DiktatLoesung.gameObject.SetActive(false);

        DiktatSaboHinweis.SetActive(false);
        if (sabotagePlayers[Config.PLAYER_ID - 1].isSaboteur)
            DiktatSaboHinweis.SetActive(true);

        diktatblocksendChange = false;
        // Leere Eingabefelder
        Transform SpielerEingabeFelder = GameObject.Find("Diktat/SpielerEingabeFelder").transform;
        for (int i = 0; i < SpielerEingabeFelder.childCount; i++)
        {
            SpielerEingabeFelder.GetChild(i).GetComponent<TMP_InputField>().text = "";
            SpielerEingabeFelder.GetChild(i).GetComponent<TMP_InputField>().interactable = false;
        }
        SpielerEingabeFelder.GetChild(Config.PLAYER_ID - 1).GetComponent<TMP_InputField>().interactable = true;
    }
    public void DiktatClientEingabe(TMP_InputField input)
    {
        if (!Config.CLIENT_STARTED)
            return;
        if (diktatblocksendChange)
            return;
        Logging.log(Logging.LogType.Debug, "SabotageClient", "DiktatClientEingabe", input.text);
        ClientUtils.SendToServer("#DiktatPlayerInput " + input.text);
    }
    private void DiktatCheckInputs(string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "DiktatCheckInputs", data);
        diktatblocksendChange = true;
        int correct = int.Parse(data.Split('|')[0]);
        int wrong = int.Parse(data.Split('|')[1]);
        string result = data.Split('|')[2];
        string playerinputs = data.Split('|')[3];
        Logging.log(Logging.LogType.Debug, "SabotageClient", "DiktatCheckInputs", "Richtig: " + correct + " Falsch: " + wrong);

        StartCoroutine(DiktatShowResults(wrong, correct, result, playerinputs.Split('~')));
    }
    bool diktatblocksendChange;
    private IEnumerator DiktatShowResults(int wrong, int correct, string result, string[] playerresults)
    {
        diktatblocksendChange = true;
        DiktatLoesung.text = result;
        DiktatLoesung.gameObject.SetActive(true);
        Transform SpielerEingabeFelder = GameObject.Find("Diktat/SpielerEingabeFelder").transform;
        for (int i = 0; i < SpielerEingabeFelder.childCount; i++)
        {
            SpielerEingabeFelder.GetChild(i).GetComponent<TMP_InputField>().interactable = false;
            SpielerEingabeFelder.GetChild(i).GetComponent<TMP_InputField>().text =
                playerresults[i].Replace("<color=\"red\">", "").Replace("<color=\"green\">", "").Replace("</color>", "").Replace("</b>", "").Replace("<b>", "");
        }
        yield return new WaitForSecondsRealtime(3f);
        for (int i = 0; i < SpielerEingabeFelder.childCount; i++)
        {
            SpielerEingabeFelder.GetChild(i).GetComponent<TMP_InputField>().text = playerresults[i];
        }

        diktatblocksendChange = false;
        AddSaboteurPoints(wrong * 10);
        AddTeamPoints(correct * 10);
        yield break;
    }
    private void DiktatRunTimer(string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "DiktatRunTimer", data);
        if (diktattimer != null)
            StopCoroutine(diktattimer);
        diktattimer = StartCoroutine(RunTimer(int.Parse(data)));
    }
    Coroutine diktattimer;
    private void DiktatStopTimer()
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "DiktatStopTimer", "");
        if (diktattimer != null)
            StopCoroutine(diktattimer);
        DiktatTimer.gameObject.SetActive(false);
    }
    IEnumerator RunTimer(int seconds)
    {
        yield return null;
        DiktatTimer.minValue = 0;
        DiktatTimer.maxValue = seconds * 1000; // Umrechnung in Millisekunden
        DiktatTimer.gameObject.SetActive(true);
        int milis = seconds * 1000;

        Logging.log(Logging.LogType.Debug, "SabotageClient", "RunTimer", "Seconds: " + seconds + "  Timer started: " + DateTime.Now.ToString("HH:mm:ss:ffff"));
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
        Logging.log(Logging.LogType.Debug, "SabotageClient", "RunTimer", "Seconds: " + seconds + "  Timer ended: " + DateTime.Now.ToString("HH:mm:ss:ffff"));

        DiktatTimer.gameObject.SetActive(false);
        yield break;
    }
    private void DiktatZurAuflösung()
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "DiktatZurAuflösung", "");
        StartWahlAbstimmung();
    }
    #endregion
    #region Sortieren
    private void StartSortieren()
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "StartSortieren", "");
        Lobby.SetActive(false);
        Sortieren.SetActive(true);
        GameObject ServerSide = GameObject.Find("Sortieren/ServerSide");
        if (ServerSide != null)
            ServerSide.gameObject.SetActive(false);
        SortierenSaboHinweis.SetActive(false);
        if (sabotagePlayers[Config.PLAYER_ID - 1].isSaboteur)
            SortierenSaboHinweis.SetActive(true);

        for (int i = 0; i < SortierenListe.transform.childCount; i++)
            SortierenListe.transform.GetChild(i).gameObject.SetActive(false);
        SortierenListe.SetActive(true);

        SortierenAuswahl.SetActive(false);
        SortierenTimer.gameObject.SetActive(false);
        SortierenLoesung.SetActive(false);
    }
    private void SortierenSaboTipp(string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "SortierenSaboTipp", data);
        for (int i = 0; i < SortierenListe.transform.childCount; i++)
            SortierenListe.transform.GetChild(i).gameObject.SetActive(false);

        SortierenSaboHinweis.SetActive(false);
        if (sabotagePlayers[Config.PLAYER_ID - 1].isSaboteur)
            SortierenSaboHinweis.SetActive(true);

        SortierenAuswahl.SetActive(false);
        string sortby = data.Split('|')[0];
        List<string> elements = new List<string>();
        for (int i = 1; i < data.Split('|').Length; i++)
            elements.Add(data.Split('|')[i]);

        if (Config.SABOTAGE_SPIEL.getPlayerByPlayer(sabotagePlayers, Config.PLAYERLIST[Config.PLAYER_ID - 1]).isSaboteur)
        {
            SortierenLoesung.transform.GetChild(1).GetComponent<TMP_InputField>().text = sortby.Split('-')[0];
            SortierenLoesung.transform.GetChild(SortierenLoesung.transform.childCount - 1).GetComponent<TMP_InputField>().text = sortby.Split('-')[1];
            for (int i = 0; i < elements.Count; i++)
            {
                SortierenLoesung.transform.GetChild(i + 2).GetComponent<TMP_InputField>().text = elements[i];
                SortierenLoesung.transform.GetChild(i + 2).GetChild(1).gameObject.SetActive(false);
                SortierenLoesung.transform.GetChild(i + 2).GetChild(2).gameObject.SetActive(false);
                SortierenLoesung.transform.GetChild(i + 2).GetChild(3).gameObject.SetActive(false);
            }
            SortierenLoesung.gameObject.SetActive(true);
        }
        else
            SortierenLoesung.gameObject.SetActive(false);
    }
    private void SortierenRunTimer(string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "SortierenRunTimer", data);
        if (sortierentimer != null)
            StopCoroutine(sortierentimer);
        sortierentimer = StartCoroutine(SortierenRunTimer(int.Parse(data)));
    }
    Coroutine sortierentimer;
    private void SortierenStopTimer()
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "SortierenStopTimer", "");
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

        Logging.log(Logging.LogType.Debug, "SabotageClient", "SortierenRunTimer", "Seconds: " + seconds + "  Timer started: " + DateTime.Now.ToString("HH:mm:ss:ffff"));
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
        Logging.log(Logging.LogType.Debug, "SabotageClient", "SortierenRunTimer", "Seconds: " + seconds + "  Timer ended: " + DateTime.Now.ToString("HH:mm:ss:ffff"));

        SortierenTimer.gameObject.SetActive(false);
        yield break;
    }
    private void SortierenShowGrenzen(string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "SortierenShowGrenzen", data);
        SortierenListe.transform.GetChild(0).gameObject.SetActive(true);
        SortierenListe.transform.GetChild(0).GetComponent<TMP_InputField>().text = data.Split('-')[0];
        SortierenListe.transform.GetChild(SortierenListe.transform.childCount - 1).gameObject.SetActive(true);
        SortierenListe.transform.GetChild(SortierenListe.transform.childCount - 1).GetComponent<TMP_InputField>().text = data.Split('-')[1];
    }
    private void SortierenShowElementInit(string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "SortierenShowElementInit", data);
        int itemIndex = int.Parse(data.Split('|')[0]);
        string item = data.Split('|')[1];
        List<string> tempAuswahl = new List<string>();
        tempAuswahl.AddRange(data.Split('|')[2].Split('~'));
        tempAuswahl.Remove(item);
        SortierenAuswahl.SetActive(true);
        int tempindex = 0;
        while (tempAuswahl.Count > 0)
        {
            string temp = tempAuswahl[UnityEngine.Random.Range(0, tempAuswahl.Count)];
            tempAuswahl.Remove(temp);
            SortierenAuswahl.transform.GetChild(tempindex + 1).GetComponent<TMP_InputField>().text = temp;
            SortierenAuswahl.transform.GetChild(tempindex + 1).gameObject.SetActive(true);
            tempindex++;
        }

        SortierenListe.transform.GetChild(itemIndex + 1).gameObject.SetActive(true);
        SortierenListe.transform.GetChild(itemIndex + 1).GetComponent<TMP_InputField>().text = item;
    }
    private void SortierenShowElement(string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "SortierenShowElement", data);
        bool isCorrect = bool.Parse(data.Split('|')[0]);
        int itemIndex = int.Parse(data.Split('|')[1]);
        string item = data.Split('|')[2];

        if (isCorrect)
            AddTeamPoints(10);
        else
            AddSaboteurPoints(10);

        SortierenListe.transform.GetChild(itemIndex + 1).gameObject.SetActive(true);
        SortierenListe.transform.GetChild(itemIndex + 1).GetComponent<TMP_InputField>().text = item;
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
    private void SortierenZurAuflösung()
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "SortierenZurAuflösung", "");
        StartWahlAbstimmung();
    }
    #endregion
    #region DerZugLuegt
    Coroutine derzugluegtRunElement;
    private void StartDerZugLuegt()
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "StartDerZugLuegt", "");
        Lobby.SetActive(false);
        DerZugLuegt.SetActive(true);
        DerZugLuegtAnzeigen.SetActive(false);
        GameObject ServerSide = GameObject.Find("DerZugLuegt/ServerSide");
        if (ServerSide != null)
            ServerSide.gameObject.SetActive(false);
        DerZugLuegtSaboHinweis.SetActive(false);
        if (sabotagePlayers[Config.PLAYER_ID - 1].isSaboteur)
            DerZugLuegtSaboHinweis.SetActive(true);
    }
    private void DerZugLuegtShowRound(string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "DerZugLuegtShowRound", data);
        DerZugLuegtAnzeigen.SetActive(true);
        string thema = data.Split('|')[0];
        string elements = data.Split('|')[1].Replace("~", "\n");

        GameObject.Find("DerZugLuegt/GameObject/Title").GetComponent<TMP_Text>().text = thema;

        if (sabotagePlayers[Config.PLAYER_ID - 1].isSaboteur)
            GameObject.Find("DerZugLuegt/GameObject/SaboTipp").GetComponent<TMP_Text>().text = elements;
        else
            GameObject.Find("DerZugLuegt/GameObject/SaboTipp").GetComponent<TMP_Text>().text = "";
    }
    private void DerZugLuegtStartElement(string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "DerZugLuegtStartElement", data);
        if (derzugluegtRunElement != null)
            StopCoroutine(derzugluegtRunElement);
        derzugluegtRunElement = StartCoroutine(DerZugLuegtRunElement(data));
    }
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
        //yield return new WaitForSeconds(1f);
        try
        {
            GameObject.Find("DerZugLuegt/GameObject/Element").GetComponent<TMP_Text>().text = "";
        }
        catch
        {
        }

        yield break;
    }
    private void DerZugLuegtRichtig(string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "DerZugLuegtRichtig", data);
        SetTeamPoints(int.Parse(data));
        Correct.Play();
    }
    private void DerZugLuegtFalsch(string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "DerZugLuegtFalsch", data);
        SetSaboteurPoints(int.Parse(data));
        Wrong.Play();
    }
    private void DerZugLuegtClientBuzzer()
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "DerZugLuegtClientBuzzer", "");
        Buzzer.Play();
    }
    private void DerZugLuegtZurAuflösung()
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "DerZugLuegtZurAuflösung", "");
        StartWahlAbstimmung();
    }
    public void DerZugLuegtBuzzer()
    {
        if (!Config.CLIENT_STARTED)
            return;
        Logging.log(Logging.LogType.Debug, "SabotageClient", "DerZugLuegtBuzzer", "");
        ClientUtils.SendToServer("#DerZugLuegtBuzzer");
    }
    #endregion
    #region Tabu
    private void StartTabu()
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "StartTabu", "");
        Lobby.SetActive(false);
        Tabu.SetActive(true);
        TabuTimer.gameObject.SetActive(false);
        GameObject ServerSide = GameObject.Find("Tabu/ServerSide");
        if (ServerSide != null)
            ServerSide.gameObject.SetActive(false);
        GameObject.Find("Tabu/GameObject/SaboTipp").GetComponent<TMP_Text>().text = "";
        tabushowkarte = false;
        TabuSaboHinweis.SetActive(false);
        if (sabotagePlayers[Config.PLAYER_ID - 1].isSaboteur)
            TabuSaboHinweis.SetActive(true);
    }
    private void TabuSaboTipp(string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "TabuSaboTipp", data);
        GameObject.Find("Tabu/GameObject/Wort").GetComponent<TMP_Text>().text = "";
        GameObject.Find("Tabu/GameObject/Tabu").GetComponent<TMP_Text>().text = "";

        if (sabotagePlayers[Config.PLAYER_ID - 1].isSaboteur)
            GameObject.Find("Tabu/GameObject/SaboTipp").GetComponent<TMP_Text>().text = data;
        else
            GameObject.Find("Tabu/GameObject/SaboTipp").GetComponent<TMP_Text>().text = "";

        tabushowkarte = false;
        TabuSaboHinweis.SetActive(false);
        if (sabotagePlayers[Config.PLAYER_ID - 1].isSaboteur)
            TabuSaboHinweis.SetActive(true);
    }
    Coroutine tabutimer;
    private void TabuRunTimer(string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "TabuRunTimer", data);
        if (tabutimer != null)
            StopCoroutine(tabutimer);
        tabutimer = StartCoroutine(TabuRunTimer(int.Parse(data)));
    }
    private void TabuStopTimer()
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "TabuStopTimer", "");
        if (tabutimer != null)
            StopCoroutine(tabutimer);
        TabuTimer.gameObject.SetActive(false);
    }
    IEnumerator TabuRunTimer(int seconds)
    {
        yield return null;
        TabuTimer.minValue = 0;
        TabuTimer.maxValue = seconds * 1000; // Umrechnung in Millisekunden
        TabuTimer.value = seconds * 1000;
        TabuTimer.gameObject.SetActive(true);
        int milis = seconds * 1000;

        Logging.log(Logging.LogType.Debug, "SabotageClient", "TabuRunTimer", "Seconds: " + seconds + "  Timer startet: " + DateTime.Now.ToString("HH:mm:ss:ffff"));
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
        Logging.log(Logging.LogType.Debug, "SabotageClient", "TabuRunTimer", "Seconds: " + seconds + "  Timer ended: " + DateTime.Now.ToString("HH:mm:ss:ffff"));

        TabuTimer.gameObject.SetActive(false);
        yield break;
    }
    private void TabuGrenzwertig()
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "TabuGrenzwertig", "");
        if (TabuTimer.value <= 10 * 1000)
            TabuTimer.value = 0;
        else
            TabuTimer.value -= 10 * 1000;
    }
    private void TabuRichtig(string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "TabuRichtig", data);
        SetTeamPoints(int.Parse(data.Split('|')[0]));
        SetSaboteurPoints(int.Parse(data.Split('|')[1]));
    }
    private void TabuFalsch(string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "TabuFalsch", data);
        SetTeamPoints(int.Parse(data.Split('|')[0]));
        SetSaboteurPoints(int.Parse(data.Split('|')[1]));

        if (tabutimer != null)
            StopCoroutine(tabutimer);
        TabuTimer.gameObject.SetActive(false);
    }
    bool tabushowkarte;
    private void TabuShowKarteToPlayer(string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "TabuShowKarteToPlayer", data);
        if (int.Parse(data.Split('|')[0]) == Config.PLAYER_ID)
            tabushowkarte = true;

        if (tabushowkarte == true)
        {
            GameObject.Find("Tabu/GameObject/Wort").GetComponent<TMP_Text>().text = data.Split('|')[1];
            GameObject.Find("Tabu/GameObject/Tabu").GetComponent<TMP_Text>().text = "<color=red>No-Go: </color>" + data.Split('|')[2];
        }
    }
    private void TabuShowNewWords(string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "TabuShowNewWords", tabushowkarte + " - " + data);
        if (tabushowkarte)
            GameObject.Find("Tabu/GameObject/Tabu").GetComponent<TMP_Text>().text = "<color=red>No-Go: </color>" + data;
    }
    private void TabuZurAuflösung()
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "TabuZurAuflösung", "");
        ServerUtils.BroadcastImmediate("#TabuZurAuflösung");
        StartWahlAbstimmung();
    }
    #endregion
    #region Auswahlstrategie
    private void StartAuswahlstrategie()
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "StartAuswahlstrategie", "");
        Lobby.SetActive(false);
        Auswahlstrategie.SetActive(true);
        AuswahlstrategieTimer.gameObject.SetActive(false);
        AuswahlstrategieGrid.gameObject.SetActive(false);
        AuswahlstrategieSaboHinweis.SetActive(false);
        if (sabotagePlayers[Config.PLAYER_ID - 1].isSaboteur)
            AuswahlstrategieSaboHinweis.SetActive(true);
        GameObject ServerSide = GameObject.Find("Auswahlstrategie/ServerSide");
        if (ServerSide != null)
            ServerSide.gameObject.SetActive(false);
    }
    Coroutine auswahlstrategietimer;
    private void AuswahlstrategieRunTimer(string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "AuswahlstrategieRunTimer", data);
        if (auswahlstrategietimer != null)
            StopCoroutine(auswahlstrategietimer);
        auswahlstrategietimer = StartCoroutine(AuswahlstrategieRunTimer(int.Parse(data)));
    }
    private void AuswahlstrategieStopTimer()
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "AuswahlstrategieStopTimer", "");
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

        Logging.log(Logging.LogType.Debug, "SabotageClient", "AuswahlstrategieRunTimer", "Seconds: " + seconds + "  Timer started: " + DateTime.Now.ToString("HH:mm:ss:ffff"));
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
        Logging.log(Logging.LogType.Debug, "SabotageClient", "AuswahlstrategieRunTimer", "Seconds: " + seconds + "  Timer ended: " + DateTime.Now.ToString("HH:mm:ss:ffff"));

        AuswahlstrategieTimer.gameObject.SetActive(false);
        yield break;
    }
    private void AuswahlstrategieShowSaboTipp(string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "AuswahlstrategieShowSaboTipp", data);
        AuswahlstrategieSaboHinweis.SetActive(false);
        if (sabotagePlayers[Config.PLAYER_ID - 1].isSaboteur)
            AuswahlstrategieSaboHinweis.SetActive(true);

        if (sabotagePlayers[Config.PLAYER_ID - 1].isSaboteur)
        {
            string[] list = data.Split('~');
            AuswahlstrategieGrid.gameObject.SetActive(true);
            for (int i = 0; i < list.Length; i++)
            {
                AuswahlstrategieGrid.GetChild(i).GetChild(0).GetComponent<Image>().sprite = Resources.Load<Sprite>("Spiele/Sabotage/Auswahlstrategie/" + list[i]);
                AuswahlstrategieGrid.GetChild(i).GetComponent<Image>().enabled = false;
            }
        }
        else
        {
            AuswahlstrategieGrid.gameObject.SetActive(false);
        }
    }
    private void AuswahlstrategieShowFirstAuswahl(string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "AuswahlstrategieShowFirstAuswahl", data);
        string playerturn = data.Split('|')[1];
        if (playerturn.Contains((Config.PLAYER_ID-1) + "") || sabotagePlayers[Config.PLAYER_ID-1].isSaboteur)
        {
            string[] list = data.Split('|')[0].Split('~');
            AuswahlstrategieGrid.gameObject.SetActive(true);
            for (int i = 0; i < list.Length; i++)
            {
                AuswahlstrategieGrid.GetChild(i).GetChild(0).GetComponent<Image>().sprite = Resources.Load<Sprite>("Spiele/Sabotage/Auswahlstrategie/" + list[i]);
                AuswahlstrategieGrid.GetChild(i).GetComponent<Image>().enabled = false;
            }
        }
        else
        {
            AuswahlstrategieGrid.gameObject.SetActive(false);
        }
    }
    private void AuswahlstrategieShowSecondAuswahl(string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "AuswahlstrategieShowSecondAuswahl", data);
        string playerturn = data.Split('|')[1];
        string[] list = data.Split('|')[0].Split('~');
        int auswahlitem = int.Parse(data.Split('|')[2]);

        AuswahlstrategieGrid.gameObject.SetActive(true);
        for (int i = 0; i < list.Length; i++)
        {
            AuswahlstrategieGrid.GetChild(i).GetChild(0).GetComponent<Image>().sprite = Resources.Load<Sprite>("Spiele/Sabotage/Auswahlstrategie/" + list[i]);
            AuswahlstrategieGrid.GetChild(i).GetComponent<Image>().enabled = false;
        }
        if (auswahlitem >= 0)
            if (playerturn.Contains((Config.PLAYER_ID-1) + "") || sabotagePlayers[Config.PLAYER_ID - 1].isSaboteur)
                AuswahlstrategieGrid.GetChild(auswahlitem).GetComponent<Image>().enabled = true;
    }
    private void AuswahlstrategieRichtig(string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "AuswahlstrategieRichtig", data);
        SetTeamPoints(int.Parse(data.Split('|')[0]));
        SetSaboteurPoints(int.Parse(data.Split('|')[1]));
        int auswahlitem = int.Parse(data.Split('|')[2]);
        for (int i = 0; i < 7; i++)
            AuswahlstrategieGrid.GetChild(i).GetComponent<Image>().enabled = false;
        if (auswahlitem >= 0)
            AuswahlstrategieGrid.GetChild(auswahlitem).GetComponent<Image>().enabled = true;
    }
    private void AuswahlstrategieFalsch(string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "AuswahlstrategieFalsch", data);
        SetTeamPoints(int.Parse(data.Split('|')[0]));
        SetSaboteurPoints(int.Parse(data.Split('|')[1]));
        int auswahlitem = int.Parse(data.Split('|')[2]);
        for (int i = 0; i < 7; i++)
            AuswahlstrategieGrid.GetChild(i).GetComponent<Image>().enabled = false;
        if (auswahlitem >= 0)
            AuswahlstrategieGrid.GetChild(auswahlitem).GetComponent<Image>().enabled = true;
    }
    public void AuswahlstrategieZurAuflösung()
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "AuswahlstrategieZurAuflösung", "");
        StartWahlAbstimmung();
    }
    #endregion
    #region Sloxikon
    private void StartSloxikon()
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "StartSloxikon", "");
        Lobby.SetActive(false);
        Sloxikon.SetActive(true);
        Sloxikon.transform.GetChild(0).gameObject.SetActive(true);
        GameObject ServerSide = GameObject.Find("Sloxikon/SaboHinweis");
        if (ServerSide != null && !sabotagePlayers[Config.PLAYER_ID-1].isSaboteur)
            ServerSide.gameObject.SetActive(false);
        ServerSide = GameObject.Find("Sloxikon/ServerSide/Index");
        if (ServerSide != null)
            ServerSide.gameObject.SetActive(false);
        ServerSide = GameObject.Find("Sloxikon/ServerSide");
        if (ServerSide != null)
            ServerSide.gameObject.SetActive(false);
        SloxikonSaboEingabe = Sloxikon.transform.GetChild(6).gameObject;
        SloxikonSaboEingabe.SetActive(false);
        SloxikonTimer.gameObject.SetActive(false);
        sloxikonVerionGO = new List<GameObject>();
        sloxikonVerionGO.Add(SloxikonVorschlag1);
        sloxikonVerionGO.Add(SloxikonVorschlag2);
        sloxikonVerionGO.Add(SloxikonVorschlag3);
        foreach (var item in sloxikonVerionGO)
        {
            SloxikonVersionSetLoesung(item, false);
            SloxikonVersionActivateButton(item, false);
            item.transform.GetChild(2).gameObject.SetActive(false);
        }
    }
    private void SloxikonRunTimer(string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "SloxikonRunTimer", data);
        if (auswahlstrategietimer != null)
            StopCoroutine(auswahlstrategietimer);
        auswahlstrategietimer = StartCoroutine(SloxikonRunTimer(int.Parse(data)));
    }
    private void SloxikonStopTimer()
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "SloxikonStopTimer", "");
        if (auswahlstrategietimer != null)
            StopCoroutine(auswahlstrategietimer);
        AuswahlstrategieTimer.gameObject.SetActive(false);
    }
    IEnumerator SloxikonRunTimer(int seconds)
    {
        yield return null;
        SloxikonTimer.minValue = 0;
        SloxikonTimer.maxValue = seconds * 1000; // Umrechnung in Millisekunden
        SloxikonTimer.gameObject.SetActive(true);
        int milis = seconds * 1000;

        Logging.log(Logging.LogType.Debug, "SabotageClient", "SloxikonRunTimer", "Seconds: " + seconds + "  Timer started: " + DateTime.Now.ToString("HH:mm:ss:ffff"));
        while (milis >= 0)
        {
            SloxikonTimer.GetComponentInChildren<Slider>().value = milis;

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
        Logging.log(Logging.LogType.Debug, "SabotageClient", "SloxikonRunTimer", "Seconds: " + seconds + "  Timer ended: " + DateTime.Now.ToString("HH:mm:ss:ffff"));

        SloxikonTimer.gameObject.SetActive(false);
        yield break;
    }
    private List<string> sloxikonVersions;
    private List<GameObject> sloxikonVerionGO;
    private string sloxikonSabos;
    private void SloxikonChangeText(string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "SloxikonChangeText", data + "");
        foreach (var item in sloxikonVerionGO)
            SloxikonVersionActivateButton(item, false);
        sloxikonVersions = new List<string>();
        sloxikonVersions.AddRange(data.Split("|")[0].Split('~'));
        sloxikonSabos = data.Split("|")[2];
        for (int i = 0; i < sloxikonVerionGO.Count; i++)
        {
            SloxikonVersionSetName(sloxikonVerionGO[i], sloxikonVersions[i]);
            SloxikonVersionActivateButton(sloxikonVerionGO[i], false);
            SloxikonVersionSetVotes(sloxikonVerionGO[i], "");
            if (sloxikonVersions[i].Equals("Loesung"))
            {
                if (sabotagePlayers[Config.PLAYER_ID - 1].isSaboteur)
                {
                    SloxikonVersionSetText(sloxikonVerionGO[i], data.Split("|")[1]);
                    SloxikonVersionSetLoesung(sloxikonVerionGO[i], true);
                }
                else
                {
                    SloxikonVersionSetText(sloxikonVerionGO[i], "");
                    SloxikonVersionSetLoesung(sloxikonVerionGO[i], false);
                }
            }
            else
            {
                SloxikonVersionSetText(sloxikonVerionGO[i], "");
                SloxikonVersionSetLoesung(sloxikonVerionGO[i], false);
            }
        }
    }
    private void SloxikonVersionSetVotes(GameObject go, string text)
    {
        go.transform.GetChild(2).GetComponent<TMP_Text>().text = text;
    }
    private string SloxikonVersionGetVotes(GameObject go)
    {
        return go.transform.GetChild(2).GetComponent<TMP_Text>().text;
    }
    private void SloxikonVersionActivateButton(GameObject go, bool status)
    {
        go.transform.GetChild(1).gameObject.SetActive(status);
    }
    private string SloxikonVersionGetName(GameObject go)
    {
        return go.name;
    }
    private void SloxikonVersionSetName(GameObject go, string name)
    {
        go.name = name;
    }
    private void SloxikonVersionSetText(GameObject go, string text)
    {
        go.transform.GetChild(0).GetComponent<TMP_InputField>().text = text;
    }
    private string SloxikonVersionGetText(GameObject go)
    {
        return go.transform.GetChild(0).GetComponent<TMP_InputField>().text;
    }
    private void SloxikonVersionSetLoesung(GameObject go, bool isLoesung)
    {
        go.GetComponent<Image>().enabled = isLoesung;
    }
    private bool SloxikonVersionGetLoesung(GameObject go)
    {
        return go.GetComponent<Image>().enabled;
    }
    private void SloxikonClearFelder()
    {
        SloxikonVersionSetText(SloxikonVorschlag1, "");
        SloxikonVersionSetText(SloxikonVorschlag2, "");
        SloxikonVersionSetText(SloxikonVorschlag3, "");
        SloxikonVersionSetLoesung(SloxikonVorschlag1, false);
        SloxikonVersionSetLoesung(SloxikonVorschlag2, false);
        SloxikonVersionSetLoesung(SloxikonVorschlag3, false);
        SloxikonVersionActivateButton(SloxikonVorschlag1, false);
        SloxikonVersionActivateButton(SloxikonVorschlag2, false);
        SloxikonVersionActivateButton(SloxikonVorschlag3, false);
    }
    private void SloxikonShowSaboEingabe(string data)
    {
        if (sabotagePlayers[Config.PLAYER_ID-1].isSaboteur)
            SloxikonSaboEingabe.SetActive(bool.Parse(data));
        else
            SloxikonSaboEingabe.SetActive(false);
    }
    private void SloxikonZeigeMoeglichkeiten(string data)
    {
        SloxikonVersionSetText(SloxikonVorschlag1, data.Split('|')[0]);
        SloxikonVersionSetText(SloxikonVorschlag2, data.Split('|')[1]);
        SloxikonVersionSetText(SloxikonVorschlag3, data.Split('|')[2]);

        if (sabotagePlayers[Config.PLAYER_ID - 1].isSaboteur)
        {
            foreach (var item in sloxikonVerionGO)
                SloxikonVersionActivateButton(item, false);
        }
        else
        {
            foreach (var item in sloxikonVerionGO)
                SloxikonVersionActivateButton(item, true);
        }
    }
    public void SloxikonWaehleVorschlag(GameObject go)
    {
        if (!Config.CLIENT_STARTED)
        {
            go.transform.GetChild(1).gameObject.SetActive(false);
            return;
        }
        ClientUtils.SendToServer("#SloxikonWaehleVorschlag " + go.name);
    }
    public void SloxikonSaboEingabeFeld(TMP_InputField input)
    {
        if (!Config.CLIENT_STARTED)
        {
            input.gameObject.SetActive(false);
            return;
        }
        if (sabotagePlayers[Config.PLAYER_ID - 1].isSaboteur)
        {
            ClientUtils.SendToServer("#SloxikonSaboEingabeFeld " + input.text);
            foreach (var item in sloxikonVerionGO)
            {
                if (item.name.Equals("Sabo1") && int.Parse(sloxikonSabos.Split('~')[0]) == Config.PLAYER_ID)
                {
                    SloxikonVersionSetText(item, input.text);
                    break;
                }
                else if (item.name.Equals("Sabo2") && int.Parse(sloxikonSabos.Split('~')[1]) == Config.PLAYER_ID)
                {
                    SloxikonVersionSetText(item, input.text);
                    break;
                }
            }
        }
    }
    private void SloxikonSaboEingaben(string data)
    {
        foreach (var item in sloxikonVerionGO)
        {
            if (SloxikonVersionGetName(item).Equals(data.Split('|')[0]))
            {
                SloxikonVersionSetText(item, data.Split('|')[1]);
                break;
            }
        }
    }
    private void SloxikonZurAuflösung()
    {
        Logging.log(Logging.LogType.Debug, "SabotageServer", "SloxikonZurAuflösung", "");
        StartWahlAbstimmung();
    }
    #endregion

    #region Utils
    private void DuBistSaboteur(string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "DuBistSaboteur", data);
        foreach (var item in sabotagePlayers)
            item.SetSaboteur(false);

        foreach (var item in sabotagePlayers)
            if (data.Contains(item.player.name))
                item.ClientSetSabo(true);

        if (!data.Contains(Config.PLAYER_NAME))
        {
            WerIstSabo.SetActive(false);
            return;
        }
        // TODO: Animieren

        if (data.Length == 0)
        {
            WerIstSabo.SetActive(false);
            return;
        }
        WerIstSabo.SetActive(true);
        WerIstSabo.transform.GetChild(0).GetComponent<TMP_Text>().text = "DU BIST SABOTEUR";
        if (data.Split('~').Length == 1)
            WerIstSabo.transform.GetChild(1).GetComponent<TMP_Text>().text = "";
        else
        {
            string namen = data.Replace(Config.PLAYER_NAME, "").Replace("~", ",");
            if (namen.StartsWith(","))
                namen = namen.Substring(1);
            if (namen.EndsWith(","))
                namen = namen.Substring(0, namen.Length - 1);
            WerIstSabo.transform.GetChild(1).GetComponent<TMP_Text>().text = "mit " + namen;
        }
    }
    private void UpdateTeamSaboPunkte(string data)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "UpdateTeamSaboPunkte", data);
        SetSaboteurPoints(int.Parse(data.Split('|')[0]));
        SetTeamPoints(int.Parse(data.Split('|')[1]));
    }
    private void SetSaboteurPoints(int punkte)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "SetSaboteurPoints", ""+punkte);
        GameObject.Find("Punktetafel/SaboteurPunkte").GetComponent<TMP_InputField>().text = "" + punkte;
    }
    private void SetTeamPoints(int punkte)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "SetTeamPoints", ""+punkte);
        GameObject.Find("Punktetafel/TeamPunkte").GetComponent<TMP_InputField>().text = "" + punkte;
    }
    private void AddSaboteurPoints(int punkte)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "AddSaboteurPoints", ""+punkte);
        GameObject.Find("Punktetafel/SaboteurPunkte").GetComponent<TMP_InputField>().text = "" +
            (int.Parse(GameObject.Find("Punktetafel/SaboteurPunkte").GetComponent<TMP_InputField>().text) + punkte);
    }
    private void AddTeamPoints(int punkte)
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "AddTeamPoints", ""+punkte);
        GameObject.Find("Punktetafel/TeamPunkte").GetComponent<TMP_InputField>().text = "" +
            (int.Parse(GameObject.Find("Punktetafel/TeamPunkte").GetComponent<TMP_InputField>().text) + punkte);
    }
    #endregion
}