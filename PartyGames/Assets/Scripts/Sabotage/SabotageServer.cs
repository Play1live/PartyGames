using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SabotageServer : MonoBehaviour
{
    bool[] PlayerConnected;


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
    Slider DiktatTimer;

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
        Logging.log(Logging.LogType.Normal, "QuizServer", "OnApplicationQuit", "Server wird geschlossen.");
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
        Logging.log(Logging.LogType.Debug, "QuizServer", "Commands", "Eingehende Nachricht: " + player.name + " " + player.id + " -> " + cmd + "   ---   " + data);
        // Sucht nach Command
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "QuizServer", "Commands", "Unkown Command: " + cmd + " -> " + data);
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
                ServerUtils.BroadcastImmediate("#ClientFocusChange " + data);
                Config.SABOTAGE_SPIEL.getPlayerByPlayer(sabotagePlayers, player).SetAusgetabbt(!bool.Parse(data));
                break;
        }
    }
    #endregion
    /// <summary>
    /// Spiel Verlassen & Zurück in die Lobby laden
    /// </summary>
    public void SpielVerlassenButton()
    {
        Logging.log(Logging.LogType.Debug, "QuizServer", "SpielVerlassenButton", "Spiel wird beendet. Lädt ins Hauptmenü.");
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
        string msg = "#UpdateSpieler [ID]0[ID][PUNKTE]" + Config.SERVER_PLAYER.points + "[PUNKTE]";
        // TODO UpdateSpieler
        Logging.log(Logging.LogType.Debug, "QuizServer", "UpdateSpieler", msg);
        return msg;
    }
    /// <summary>
    /// Initialisiert die Anzeigen zu beginn
    /// </summary>
    private void InitAnzeigen()
    {
        // TODO: InitAnzeigen
        Transform modi = GameObject.Find("Modi").transform;
        for (int i = 0; i < modi.childCount; i++)
            modi.GetChild(i).gameObject.SetActive(true);

        // Lobby
        Lobby = GameObject.Find("Lobby");
        Lobby.SetActive(true);
        GameObject.Find("Lobby/StartSpielIndex").GetComponent<TMP_Text>().text = "Starte Spiel: " + Config.SABOTAGE_SPIEL.spielindex;

        // SaboteurWahl & Aufloesung
        SaboteurWahlAufloesung = GameObject.Find("SaboteurWahl&Aufloesung");
        SaboteurWahlAufloesung.SetActive(false);
        SaboteurWahlAufloesungAbstimmung = SaboteurWahlAufloesung.transform.GetChild(0).gameObject;
        SaboteurWahlAufloesungPunkteverteilung = SaboteurWahlAufloesung.transform.GetChild(1).gameObject;

        // Diktat
        DiktatTimer = GameObject.Find("Diktat/Timer").GetComponent<Slider>();
        DiktatTimer.maxValue = 1;
        DiktatTimer.minValue = 0;
        DiktatTimer.value = 0;
        Diktat = GameObject.Find("Modi/Diktat");
        Diktat.SetActive(false);


        // Allgemein
        sabotagePlayers = new SabotagePlayer[5];
        for (int i = 0; i < sabotagePlayers.Length; i++)
        {
            sabotagePlayers[i] = new SabotagePlayer(Config.PLAYERLIST[i], GameObject.Find("SpielerAnzeige/Player (" + (i+1)+")"));
        }
        // SaboteurAnzeige
        WerIstSabo = GameObject.Find("SpielerAnzeigen/WerIstSaboteur");
        WerIstSabo.SetActive(false);
        WerIstSabo.transform.GetChild(0).GetComponent<TMP_Text>().text = "Keiner";
        WerIstSabo.transform.GetChild(1).GetComponent<TMP_Text>().text = "Du bist alleine";

    }

    #region Lobby
    public void LobbyChangeSpielIndex(int change)
    {
        if (change == -1 && Config.SABOTAGE_SPIEL.spielindex <= 0)
            Config.SABOTAGE_SPIEL.spielindex = 0;
        else
            Config.SABOTAGE_SPIEL.spielindex += change;
        GameObject.Find("Lobby/StartSpielIndex").GetComponent<TMP_Text>().text = "Starte Spiel: " + Config.SABOTAGE_SPIEL.spielindex;
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
    }
    public void AbstimmungStart()
    {
        SaboteurWahlAufloesungAbstimmung.SetActive(true);
        SaboteurWahlAufloesungPunkteverteilung.SetActive(false);

        for (int i = 0; i < sabotagePlayers.Length; i++)
        {
            GameObject.Find("SaboteurWahl&Aufloesung/Abstimmung/Server/Icon ("+ (sabotagePlayers[i].player.id+1) +")").
                GetComponent<Image>().sprite = sabotagePlayers[i].player.icon2.icon;
            GameObject.Find("SaboteurWahl&Aufloesung/Abstimmung/Server/Icon (" + (sabotagePlayers[i].player.id + 1) + ")").
                GetComponentInChildren<TMP_Text>().text = "0";
        }
    }
    public void AufloesungStart()
    {
        SaboteurWahlAufloesungAbstimmung.SetActive(false);
        SaboteurWahlAufloesungPunkteverteilung.SetActive(true);
    }
    // Abstimmung


    // Punkteverteilung
    public void AufloesungZeigeSabos()
    {
        // TODO: zeige Die sabos und blende aus sobald die Lobby aufgerufen wird
        // TODO: gebe den Sabos die zusatzpunkte
        // TODO: verteile die Team punkte
    }
    public void AufloesungZurLobby()
    {
        ServerUtils.BroadcastImmediate("#AufloesungZurLobby");
        // TODO: sabos blende aus sobald die Lobby aufgerufen wird
    }
    #endregion
    #region Diktat
    public void StartDiktat()
    {
        Lobby.SetActive(false);
        Diktat.SetActive(true);
    }
    public void DiktatChangeText(int change)
    {
        string diktat = Config.SABOTAGE_SPIEL.diktat.GetNew(change);
        GameObject.Find("Diktat/ServerSide/Index").GetComponent<TMP_Text>().text = "Text: " + (Config.SABOTAGE_SPIEL.diktat.index+1) + "/" + Config.SABOTAGE_SPIEL.diktat.saetze.Count;
        GameObject.Find("Diktat/LösungsText").GetComponent<TMP_InputField>().text = diktat;
        
        // Leere Eingabefelder
        Transform SpielerEingabeFelder = GameObject.Find("Diktat/SpielerEingabeFelder").transform;
        for (int i = 0; i < SpielerEingabeFelder.childCount; i++)
        {
            SpielerEingabeFelder.GetChild(i).GetComponent<TMP_InputField>().text = "";
            SpielerEingabeFelder.GetChild(i).GetComponent<TMP_InputField>().interactable = true;
        }
        diktatBlockEingabe = false;

        Config.SABOTAGE_SPIEL.SendToSaboteur(sabotagePlayers, "#DiktatLoesung " + diktat);
    }
    private bool diktatBlockEingabe;
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
            eingabe = eingabe.Replace("<color=\"red\">", "").Replace("<color=\"green\">", "").Replace("</color>", "");
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
            playerinputs += "|" + diff;
        }
        playerinputs = playerinputs.Substring(1);
        AddSaboteurPoints(wrong * 10);
        AddTeamPoints(correct * 10);
        Debug.Log("Richtig: " + correct + "   Falsch: " + wrong);

        ServerUtils.BroadcastImmediate("#DiktatCheckInputs " + playerinputs);
        StartCoroutine(DiktatShowResults(GameObject.Find("Diktat/LösungsText").GetComponent<TMP_InputField>().text, playerinputs.Split('|')));
    }
    private IEnumerator DiktatShowResults(string result, string[] playerresults)
    {
        Transform SpielerEingabeFelder = GameObject.Find("Diktat/SpielerEingabeFelder").transform;
        for (int i = 0; i < SpielerEingabeFelder.childCount; i++)
        {
            SpielerEingabeFelder.GetChild(i).GetComponent<TMP_InputField>().text = 
                playerresults[i].Replace("<color=\"red\">", "").Replace("<color=\"green\">", "").Replace("</color>", "").Replace("</b>", "").Replace("<b>", "");
        }
        yield return new WaitForSecondsRealtime(2f);
        for (int i = 0; i < SpielerEingabeFelder.childCount; i++)
        {
            SpielerEingabeFelder.GetChild(i).GetComponent<TMP_InputField>().text = playerresults[i];
        }
    }
    public void DiktatGenSabos()
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
        names = names.Substring("\n".Length);
        WerIstSabo.transform.GetChild(0).GetComponent<TMP_Text>().text = names;

        Config.SABOTAGE_SPIEL.SendToSaboteur(sabotagePlayers, "#DuBistSaboteur");
    }
    public void DiktatRunTimer(TMP_InputField input)
    {
        ServerUtils.BroadcastImmediate("#RunTimer " + input.text);
        StartCoroutine(RunTimer(int.Parse(input.text)));
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
        Debug.Log("Timer startet: " + DateTime.Now.ToString("HH:mm:ss:ffff"));

        DiktatTimer.gameObject.SetActive(false);
        yield break;
    }
    public void DiktatZurAuflösung()
    {
        ServerUtils.BroadcastImmediate("#DiktatZurAuflösung");
        StartWahlAbstimmung();
    }
    #endregion

    #region Utils
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
    public void ChangeTeamsPoints()
    {
        ServerUtils.BroadcastImmediate("#TeamPunkte " +
            GameObject.Find("Punktetafel/TeamPunkte").GetComponent<TMP_InputField>().text + "*" +
            GameObject.Find("Punktetafel/SaboteurPunkte").GetComponent<TMP_InputField>().text);
    }
    #endregion
}
