using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using TMPro;
using UnityEditor;
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
                Player player = Config.PLAYERLIST[int.Parse(data.Split('*')[0])-1];
                Config.SABOTAGE_SPIEL.getPlayerByPlayer(sabotagePlayers, player).SetAusgetabbt(!bool.Parse(data.Split('*')[1]));
                break;
            #endregion
            case "#UpdateSpieler":
                UpdateSpieler(data);
                break;
            case "#UpdateTeamSaboPunkte":
                UpdateTeamSaboPunkte(data);
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
            case "#AufloesungZurLobby":
                AufloesungZurLobby();
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

            case "#StartSortieren":
                StartSortieren();
                break;
            case "#SortierenSaboTipp":
                SortierenSaboTipp(data);
                break;
            case "#SortierenRunTimer":
                SortierenRunTimer(data);
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

            case "#StartMemory":
                StartMemory();
                break;
            case "#MemoryShowGrid":
                MemoryShowGrid(data);
                break;
            case "#MemoryClickItem":
                MemoryClickItem(data);
                break;
            case "#MemoryClickItems":
                MemoryClickItems(data);
                break;
            case "#MemoryRunTimer":
                MemoryRunTimer(data);
                break;
            case "#MemoryZurAuflösung":
                MemoryZurAuflösung();
                break;
        } 
    }
    /// <summary>
    /// Initialisiert die Anzeigen der Scene
    /// </summary>
    private void InitAnzeigen()
    {
        Logging.log(Logging.LogType.Debug, "SabotageClient", "InitAnzeigen", "Anzeigen werden initialisiert.");
        connectedPlayers = 0;

        // Allgemein
        sabotagePlayers = new SabotagePlayer[5];
        for (int i = 0; i < sabotagePlayers.Length; i++)
            sabotagePlayers[i] = new SabotagePlayer(Config.PLAYERLIST[i], GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")"));

        Transform modi = GameObject.Find("Modi").transform;
        for (int i = 0; i < modi.childCount; i++)
            modi.GetChild(i).gameObject.SetActive(true);

        // Lobby
        Lobby = GameObject.Find("Modi/Lobby");
        Lobby.SetActive(true);
        GameObject.Find("Lobby/Server").SetActive(false);

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
        SortierenAuswahl.SetActive(false);
        SortierenLoesung = GameObject.Find("Sortieren/LösungListe");
        SortierenLoesung.gameObject.SetActive(false);
        Sortieren = GameObject.Find("Modi/Sortieren");
        Sortieren.SetActive(false);

        // Memory
        MemoryTimer = GameObject.Find("Memory/Timer").GetComponent<Slider>();
        MemoryTimer.maxValue = 1;
        MemoryTimer.minValue = 0;
        MemoryTimer.value = 0;
        MemoryGrid = GameObject.Find("Memory/Grid");
        MemoryGrid.gameObject.SetActive(false);
        Memory = GameObject.Find("Modi/Memory");
        Memory.gameObject.SetActive(false);

        // SaboteurAnzeige
        WerIstSabo = GameObject.Find("SpielerAnzeigen/WerIstSaboteur");
        WerIstSabo.SetActive(false);
        WerIstSabo.transform.GetChild(0).GetComponent<TMP_Text>().text = "Keiner";
        WerIstSabo.transform.GetChild(1).GetComponent<TMP_Text>().text = "Du bist alleine";
    }
    /// <summary>
    /// Aktualisiert die Spieler Anzeigen
    /// </summary>
    /// <param name="data">#UpdateSpieler ...</param>
    private void UpdateSpieler(string data)
    {
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
        if (connectedplayer < connectedPlayers)
        {
            connectedPlayers = connectedplayer;
            DisconnectSound.Play();
        }
        Logging.log(Logging.LogType.Debug, "SabotageClient", "UpdateSpieler", data);
    }
    /// <summary>
    /// Sendet eine Buzzer Anfrage an den Server
    /// </summary>
    public void SpielerBuzzered()
    {
        ClientUtils.SendToServer("#SpielerBuzzered");
    }
    private void UpdateTeamSaboPunkte(string data)
    {
        SetSaboteurPoints(int.Parse(data.Split('|')[0]));
        SetTeamPoints(int.Parse(data.Split('|')[1]));
    }

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
        GameObject.Find("SaboteurWahl&Aufloesung/Server").SetActive(false);
    }
    // Abstimmung
    private void AbstimmungStart()
    {
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
                GameObject.Find("SaboteurWahl&Aufloesung/Abstimmung/DuBistKeinSabo/Icon (" + (sabotagePlayers[i].player.id) + ")").
                    GetComponent<Image>().sprite = sabotagePlayers[i].player.icon2.icon;
                GameObject.Find("SaboteurWahl&Aufloesung/Abstimmung/DuBistKeinSabo/Icon (" + (sabotagePlayers[i].player.id) + ")").
                    GetComponent<Button>().interactable = true;
            }
            GameObject.Find("SaboteurWahl&Aufloesung/Abstimmung/DuBistKeinSabo/Icon (" + (Config.PLAYER_ID) + ")").
                    GetComponent<Button>().interactable = false;
        }
    }
    public void AbstimmungClientStimmtFuer(GameObject btnImage)
    {
        if (!Config.CLIENT_STARTED)
            return;
        for (int i = 0; i < sabotagePlayers.Length; i++)
        {
            Button btn = GameObject.Find("SaboteurWahl&Aufloesung/Abstimmung/DuBistKeinSabo/Icon (" + (sabotagePlayers[i].player.id) + ")").GetComponent<Button>();
            if (btn.name.Equals(btnImage.name))
                btn.interactable = false;
            else
                btn.interactable = true;
        }
        GameObject.Find("SaboteurWahl&Aufloesung/Abstimmung/DuBistKeinSabo/Icon (" + (Config.PLAYER_ID) + ")").
                    GetComponent<Button>().interactable = false;

        ClientUtils.SendToServer("#ClientStimmtFuer " + btnImage.gameObject.name.Replace("Icon (", "").Replace(")", ""));
    }
    private void AufloesungStart(string data)
    {
        SaboteurWahlAufloesungAbstimmung.SetActive(false);
        SaboteurWahlAufloesungPunkteverteilung.SetActive(true);
        SaboteurWahlAufloesungPunkteverteilung.transform.GetChild(0).gameObject.SetActive(false);

        for (int i = 0; i < sabotagePlayers.Length; i++)
            SaboteurWahlAufloesungPunkteverteilung.transform.GetChild(1 + i).gameObject.GetComponent<TMP_Text>().text = "" + data.Split('~')[i];

        Transform SaboAnzeige = SaboteurWahlAufloesungPunkteverteilung.transform.GetChild(6);
        SaboAnzeige.gameObject.SetActive(false);
        SaboAnzeige.GetChild(2).GetComponent<TMP_Text>().text = "";
        SaboAnzeige.GetChild(0).gameObject.SetActive(false);
        SaboAnzeige.GetChild(1).gameObject.SetActive(false);
    }
    // Punkteverteilung
    private void AufloesungZeigeSabos(string data)
    {
        Transform SaboAnzeige = SaboteurWahlAufloesungPunkteverteilung.transform.GetChild(6);
        SaboAnzeige.gameObject.SetActive(false);
        SaboAnzeige.GetChild(2).GetComponent<TMP_Text>().text = "";

        SaboAnzeige.GetChild(0).gameObject.SetActive(false);
        SaboAnzeige.GetChild(1).gameObject.SetActive(false);
        SaboAnzeige.gameObject.SetActive(true);

        StartCoroutine(AufloesungVerteilePunkte(SaboAnzeige, int.Parse(data.Split('|')[0]), int.Parse(data.Split('|')[1]), int.Parse(data.Split('|')[2]), data.Split('|')[3], data.Split('|')[4]));
    }
    IEnumerator AufloesungVerteilePunkte(Transform SaboAnzeige, int countSabos, int teampunkte, int sabopunkte, string bonuspunkte, string sabostring)
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
    private void AufloesungZurLobby()
    {
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
        Lobby.SetActive(false);
        Diktat.SetActive(true);
        diktatblocksendChange = false;
        GameObject.Find("Diktat/ServerSide").gameObject.SetActive(false);
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
        if (Config.SABOTAGE_SPIEL.getPlayerByPlayer(sabotagePlayers, Config.PLAYERLIST[Config.PLAYER_ID - 1]).isSaboteur)
        {
            DiktatLoesung.text = data;
            DiktatLoesung.gameObject.SetActive(true);
        }
        else
            DiktatLoesung.gameObject.SetActive(false);

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
        ClientUtils.SendToServer("#DiktatPlayerInput " + input.text);
    }
    private void DiktatCheckInputs(string data)
    {
        diktatblocksendChange = true;
        int correct = int.Parse(data.Split('|')[0]);
        int wrong = int.Parse(data.Split('|')[1]);
        string result = data.Split('|')[2];
        string playerinputs = data.Split('|')[3];
        Debug.Log("Richtig: " + correct + "   Falsch: " + wrong);

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
        StartCoroutine(RunTimer(int.Parse(data)));
    }
    IEnumerator RunTimer(int seconds)
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

        DiktatTimer.gameObject.SetActive(false);
        yield break;
    }
    private void DiktatZurAuflösung()
    {
        StartWahlAbstimmung();
    }
    #endregion
    #region Sortieren
    private void StartSortieren()
    {
        Lobby.SetActive(false);
        Sortieren.SetActive(true);
        GameObject.Find("Sortieren/ServerSide").gameObject.SetActive(false);

        for (int i = 0; i < SortierenListe.transform.childCount; i++)
            SortierenListe.transform.GetChild(i).gameObject.SetActive(false);
        SortierenListe.SetActive(true);

        SortierenAuswahl.SetActive(false);
        SortierenTimer.gameObject.SetActive(false);
        SortierenLoesung.SetActive(false);
    }
    private void SortierenSaboTipp(string data)
    {
        for (int i = 0; i < SortierenListe.transform.childCount; i++)
            SortierenListe.transform.GetChild(i).gameObject.SetActive(false);

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
        StartCoroutine(SortierenRunTimer(int.Parse(data)));
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
    private void SortierenShowGrenzen(string data)
    {
        SortierenListe.transform.GetChild(0).gameObject.SetActive(true);
        SortierenListe.transform.GetChild(0).GetComponent<TMP_InputField>().text = data.Split('-')[0];
        SortierenListe.transform.GetChild(SortierenListe.transform.childCount - 1).gameObject.SetActive(true);
        SortierenListe.transform.GetChild(SortierenListe.transform.childCount - 1).GetComponent<TMP_InputField>().text = data.Split('-')[1];
    }
    private void SortierenShowElementInit(string data)
    {
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
        SortierenListe.transform.GetChild(itemIndex + 1).GetComponentInChildren<TMP_Text>().text = "1";
    }
    private void SortierenShowElement(string data)
    {
        bool isCorrect = bool.Parse(data.Split('|')[0]);
        int itemIndex = int.Parse(data.Split('|')[1]);
        string item = data.Split('|')[2];

        if (isCorrect)
            AddTeamPoints(10);
        else
            AddSaboteurPoints(10);

        SortierenListe.transform.GetChild(itemIndex + 1).gameObject.SetActive(true);
        SortierenListe.transform.GetChild(itemIndex + 1).GetComponent<TMP_InputField>().text = item;
        int tempcount = 0;
        for (int i = 1; i < SortierenListe.transform.childCount - 1; i++)
        {
            if (SortierenListe.transform.GetChild(i).gameObject.activeInHierarchy)
            {
                tempcount++;
                SortierenListe.transform.GetChild(i).GetChild(1).GetComponent<TMP_Text>().text = "" + tempcount;
            }
        }

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
        StartWahlAbstimmung();
    }
    #endregion
    #region Memory
    private void StartMemory()
    {
        Lobby.SetActive(false);
        Memory.SetActive(true);
        MemoryTimer.gameObject.SetActive(false);
        MemoryGrid.SetActive(false);
        GameObject.Find("Memory/ServerSide").gameObject.SetActive(false);

        SetTeamPoints(500);
    }
    private void MemoryShowGrid(string data)
    {
        string sequence = data;
        for (int i = 0; i < sequence.Split('~').Length; i++)
        {
            MemoryGrid.transform.GetChild(i).GetChild(0).GetChild(0).GetComponent<Image>().sprite = Resources.Load<Sprite>("Spiele/Sabotage/Memory/" + sequence.Split('~')[i]);
            MemoryGrid.transform.GetChild(i).GetChild(1).gameObject.SetActive(true);
            MemoryGrid.transform.GetChild(i).GetChild(1).GetComponentInChildren<TMP_Text>().text = "" + (i + 1);
            if (sabotagePlayers[Config.PLAYER_ID - 1].isSaboteur)
                MemoryGrid.transform.GetChild(i).GetChild(1).GetComponent<Button>().enabled = true;
            else
                MemoryGrid.transform.GetChild(i).GetChild(1).GetComponent<Button>().enabled = false;
            MemoryGrid.transform.GetChild(i).GetChild(1).GetComponent<Button>().interactable = false; // man sieht keine Lösung
        }
        MemoryGrid.SetActive(true);
    }
    private void MemoryClickItem(string data)
    {
        int pos1 = int.Parse(data.Split('~')[0]);
        MemoryGrid.transform.GetChild(pos1).GetChild(1).gameObject.SetActive(false);
    }
    private void MemoryClickItems(string data)
    {
        int pos1 = int.Parse(data.Split('|')[0].Split('~')[0]);
        MemoryGrid.transform.GetChild(pos1).GetChild(1).gameObject.SetActive(false);
        int pos2 = int.Parse(data.Split('|')[1].Split('~')[0]);
        MemoryGrid.transform.GetChild(pos2).GetChild(1).gameObject.SetActive(false);
        StartCoroutine(MemoryVerifyClicks(int.Parse(data.Split('|')[0].Split('~')[0]), data.Split('|')[0].Split('~')[1], int.Parse(data.Split('|')[1].Split('~')[0]), data.Split('|')[1].Split('~')[1]));
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
        yield break;
    }
    private void MemoryRunTimer(string input)
    {
        StartCoroutine(MemoryRunTimer(int.Parse(input)));
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
    private void MemoryZurAuflösung()
    {
        StartWahlAbstimmung();
    }
    #endregion

    #region Utils
    private void DuBistSaboteur(string data)
    {
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
                namen = namen.Substring(1);
            WerIstSabo.transform.GetChild(1).GetComponent<TMP_Text>().text = "mit " + namen;
        }
    }

    private void GenSaboteurForRound(int saboteurCount) // 1 oder 2
    {
        //diktat = new SabotageDiktat();  // s1
        // Sortieren (Listen)           // s2 + s4
        // Memory                       // s3
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
    #endregion
}