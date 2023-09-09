using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TabuServer : MonoBehaviour
{
    bool[] PlayerConnected;
    [SerializeField] AudioSource DisconnectSound;
    [SerializeField] AudioSource SpielerIstDran;
    [SerializeField] AudioSource DisplayX;
    [SerializeField] AudioSource FalschGeraten;
    [SerializeField] AudioSource Erraten;
    [SerializeField] AudioSource Beeep;
    [SerializeField] AudioSource Moeoop;

    private GameObject BackgroundColor;
    private GameObject TeamRot;
    private GameObject TeamBlau;
    private GameObject JoinTeamRot;
    private GameObject JoinTeamBlau;
    private int teamrotPunkte;
    private int teamblauPunkte;
    private GameObject Timer;
    private GameObject Kreuze;
    private GameObject Karte;
    private GameObject Richtig;
    private GameObject Falsch;
    private GameObject Skip;
    private Coroutine SkipCoroutine;
    private GameObject RundeStarten;
    private TMP_InputField TimerSec; 
    private int timerseconds;
    private Coroutine HideRichtigFalschSecCoroutine;
    private GameObject HistoryContentElement;

    private List<string> teamrotList;
    private List<string> teamblauList;
    private string TeamTurn;
    private bool started;
    private string erklaerer;
    private string lastErklaerer;

    private Coroutine TimerCoroutine;
    private TabuItem selectedItem;

    void OnEnable()
    {
        StartCoroutine(ServerUtils.Broadcast());
        PlayerConnected = new bool[Config.SERVER_MAX_CONNECTIONS];
        InitGame();
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
        Logging.log(Logging.LogType.Normal, "TabuServer", "OnApplicationQuit", "Server wird geschlossen.");
        Config.SERVER_TCP.Server.Close();
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
        Logging.log(Logging.LogType.Debug, "TabuServer", "Commands", "Eingehende Nachricht: " + player.name + " " + player.id + " -> " + cmd + "   ---   " + data);
        // Sucht nach Command
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "TabuServer", "Commands", "Unkown Command: " + cmd + " -> " + data);
                break;

            case "#ClientClosed":
                DisconnectSound.Play();
                teamrotList.Remove(player.name);
                teamblauList.Remove(player.name);
                ServerUtils.ClientClosed(player);
                JoinTeam("", "");
                break;
            case "#TestConnection":
                break;
            case "#ClientFocusChange":
                break;

            case "#JoinTabu":
                PlayerConnected[player.id - 1] = true;
                ServerUtils.SendMSG("#SetGameType " + TabuSpiel.GameType, player, false);
                break;
            case "#ClientKreuz":
                SetKreuzClient(player, data);
                break;
            case "#ClientJoinTeam":
                ClientJoinTeam(player, data);
                break;
            case "#ClientStartRunde":
                ClientStartRunde(player);
                break;
            case "#ClientSkipWort":
                ClientSkip(player);
                break;
            case "#ClientRichtigGeraten":
                ClientRichtigGeraten(player);
                break;
        }
    }
    #endregion
    /// <summary>
    /// Spiel Verlassen & Zurück in die Lobby laden
    /// </summary>
    public void SpielVerlassenButton()
    {
        Logging.log(Logging.LogType.Debug, "TabuServer", "SpielVerlassenButton", "Spiel wird beendet. Lädt ins Hauptmenü.");
        ServerUtils.AddBroadcast("#ZurueckInsHauptmenue");
    }
    #region GameLogic
    private void InitGame()
    {
        if (TabuSpiel.GameType.Equals(""))
            TabuSpiel.GameType = "1 Wort";
        // Spielbrett
        BackgroundColor = GameObject.Find("Spielbrett/BackgroundColor");
        BackgroundColor.SetActive(false);
        TeamRot = GameObject.Find("Spielbrett/TeamRot");
        teamrotList = new List<string>();
        JoinTeamRot = GameObject.Find("Spielbrett/JoinRot");
        SetTeamPoints("ROT", teamrotPunkte);
        TeamBlau = GameObject.Find("Spielbrett/TeamBlau");
        teamblauList = new List<string>();
        JoinTeamBlau = GameObject.Find("Spielbrett/JoinBlau");
        SetTeamPoints("BLAU", teamblauPunkte);
        Timer = GameObject.Find("Spielbrett/Timer");
        Timer.SetActive(false);
        Kreuze = GameObject.Find("Spielbrett/Kreuze");
        Kreuze.SetActive(false);
        Karte = GameObject.Find("Spielbrett/Karte");
        Karte.SetActive(false);
        Richtig = GameObject.Find("Spielbrett/Richtig");
        Richtig.SetActive(false);
        Falsch = GameObject.Find("Spielbrett/Falsch");
        Falsch.SetActive(false);
        RundeStarten = GameObject.Find("Spielbrett/RundeStarten");
        RundeStarten.SetActive(false);
        Skip = GameObject.Find("Spielbrett/Skip");
        Skip.SetActive(false);
        TimerSec = GameObject.Find("ServerSide/TimerSec").GetComponent<TMP_InputField>();
        timerseconds = 60;
        TimerSec.text = timerseconds + "";
        started = false;
        HistoryContentElement = GameObject.Find("WortHistory/Viewport/Content/Object*0");
        GameObject.Find("ServerSide/PackTitle").GetComponent<TMP_Text>().text = "Pack: " + Config.TABU_SPIEL.getSelected().getTitel();
        GameObject.Find("ServerSide/GameType").GetComponent<TMP_Text>().text = "GameType: " + TabuSpiel.GameType;

        teamblauList.Add(Config.PLAYER_NAME);
        foreach (Player item in Config.PLAYERLIST)
            if (item.name.Length > 0 && item.isConnected)
                teamblauList.Add(item.name);

        TeamTurn = "ROT";

        SetTeamPoints("ROT", teamrotPunkte = TabuData.InitTeamPoints("ROT"));
        SetTeamPoints("BLAU", teamblauPunkte = TabuData.InitTeamPoints("BLAU"));
    }
    private void SetTeamPoints(string team, int points)
    {
        if (team == "ROT")
        {
            TeamRot.transform.GetChild(0).GetComponentInChildren<TMP_Text>().text = "" + points;
        }
        else if (team == "BLAU")
        {
            TeamBlau.transform.GetChild(0).GetComponentInChildren<TMP_Text>().text = "" + points;
        }
        else
            Logging.log(Logging.LogType.Error, "TabuServer", "SetTeamPoints", "Team ist unbekannt: " + team);
    }
    private void ClientJoinTeam(Player p, string data)
    {
        JoinTeam(data, p.name);
    }
    public void ServerJoinTeam(string team)
    {
        if (!Config.SERVER_STARTED)
            return;
        JoinTeam(team, Config.PLAYER_NAME);

        if (!started)
        {
            if (TeamTurn.Equals("ROT") && teamrotList.Contains(Config.PLAYER_NAME))
                RundeStarten.SetActive(true);
            else if (TeamTurn.Equals("BLAU") && teamblauList.Contains(Config.PLAYER_NAME))
                RundeStarten.SetActive(true);
            else
                RundeStarten.SetActive(false);
        }
    }
    private void JoinTeam(string team, string player)
    {
        if (!team.Equals("") && !player.Equals(""))
        {
            // Aus Teams löschen
            teamrotList.Remove(player);
            teamblauList.Remove(player);
            // Team hinzufügen
            if (team.Equals("ROT"))
            {
                teamrotList.Add(player);
            }
            else if (team.Equals("BLAU"))
            {
                teamblauList.Add(player);
            }
            else
                Logging.log(Logging.LogType.Error, "TabuServer", "JoinTeam", "Team ist unbekannt: " + team);
        }
        // Anzeigen aktualisieren
        string teamrot = "";
        for (int i = 1; i < TeamRot.transform.childCount; i++)
        {
            int index = i - 1;
            Transform PlayerObject = TeamRot.transform.GetChild(i);
            PlayerObject.gameObject.SetActive(false);
            if (teamrotList.Count > index)
            {
                PlayerObject.GetChild(1).GetComponent<TMP_Text>().text = teamrotList[index];
                // Server
                if (teamrotList[index] == Config.PLAYER_NAME)
                {
                    PlayerObject.GetChild(0).GetComponent<Image>().sprite = Config.SERVER_PLAYER.icon2.icon;
                    teamrot += "[#]" + teamrotList[index] + "~" + Config.SERVER_PLAYER.icon2.id;
                }
                // Client
                else
                {
                    PlayerObject.GetChild(0).GetComponent<Image>().sprite = Player.getPlayerIconByPlayerName(teamrotList[index]).icon;
                    teamrot += "[#]" + teamrotList[index] + "~" + Player.getPlayerIconByPlayerName(teamrotList[index]).id;
                }
                PlayerObject.gameObject.SetActive(true);
            }
        }
        if (teamrot.Length > 3)
            teamrot = teamrot.Substring("[#]".Length);
        string teamblau = "";
        for (int i = 1; i < TeamBlau.transform.childCount; i++)
        {
            int index = i - 1;
            Transform PlayerObject = TeamBlau.transform.GetChild(i);
            PlayerObject.gameObject.SetActive(false);
            if (teamblauList.Count > index)
            {
                PlayerObject.GetChild(1).GetComponent<TMP_Text>().text = teamblauList[index];
                // Server
                if (teamblauList[index] == Config.PLAYER_NAME)
                {
                    PlayerObject.GetChild(0).GetComponent<Image>().sprite = Config.SERVER_PLAYER.icon2.icon;
                    teamblau += "[#]" + teamblauList[index] + "~" + Config.SERVER_PLAYER.icon2.id;
                }
                // Client
                else
                {
                    PlayerObject.GetChild(0).GetComponent<Image>().sprite = Player.getPlayerIconByPlayerName(teamblauList[index]).icon;
                    teamblau += "[#]" + teamblauList[index] + "~" + Player.getPlayerIconByPlayerName(teamblauList[index]).id;
                }
                PlayerObject.gameObject.SetActive(true);
            }
        }
        if (teamblau.Length > 3)
            teamblau = teamblau.Substring("[#]".Length);
        // Listen Broadcast
        ServerUtils.AddBroadcast("#TeamUpdate " + teamrot + "|" + teamblau);
    }
    private void SetKreuzClient(Player p, string data)
    {
        if (selectedItem.geheimwort != data)
            return;
        SetKreuz(p.name);
    }
    public void SetKreuzServer()
    {
        if (!Config.SERVER_STARTED)
            return;
        SetKreuz(Config.PLAYER_NAME);
    }
    private IEnumerator HideRichtigFalschSec()
    {
        Richtig.SetActive(false);
        Falsch.SetActive(false);
        yield return new WaitForSeconds(1);
        if (!started)
        {
            Richtig.SetActive(false);
            Falsch.SetActive(false);
        }
        else if (erklaerer == Config.PLAYER_NAME)
        {
            Richtig.SetActive(true);
            Falsch.SetActive(true);
        }
        else if (TeamTurn.Equals("ROT") && teamblauList.Contains(Config.PLAYER_NAME))
        {
            Richtig.SetActive(false);
            Falsch.SetActive(true);
        }
        else if (TeamTurn.Equals("BLAU") && teamrotList.Contains(Config.PLAYER_NAME))
        {
            Richtig.SetActive(false);
            Falsch.SetActive(true);
        }
        else
        {
            Richtig.SetActive(false);
            Falsch.SetActive(false);
        }
        yield break;
    } 
    private void SetKreuz(string player)
    {
        if (!started)
            return;
        RundeEnde(-2, name);
    }
    private void ClientRichtigGeraten(Player p)
    {
        RichtigGeraten(p.name);
    }
    public void ServerRichtigGeraten()
    {
        if (!Config.SERVER_STARTED)
            return;
        RichtigGeraten(Config.PLAYER_NAME);
    }
    private void RichtigGeraten(string name)
    {
        RundeEnde(+1, name);
    }
    private void ClientSkip(Player p)
    {
        SkipWort(p.name);
    }
    public void ServerSkip()
    {
        if (!Config.SERVER_STARTED)
            return;
        SkipWort(Config.PLAYER_NAME);
    }
    private void SkipWort(string name)
    {
        RundeEnde(-3, name);
    }
    private void StartTimer()
    {
        if (TimerCoroutine != null)
            StopCoroutine(TimerCoroutine);
        TimerCoroutine = StartCoroutine(RunTimer(timerseconds, TabuData.TimerDecreasePoints()));
    }
    private IEnumerator RunTimer(int seconds, bool decPoints)
    {
        bool showTimer = true;
        if (TabuSpiel.GameType.Equals("Battle Royale"))
            showTimer = false;

        Timer.SetActive(showTimer);

        while (seconds >= 0)
        {
            Timer.GetComponentInChildren<TMP_Text>().text = "" + seconds;

            if (seconds == 0)
            {
                Beeep.Play();
            }
            // Moep Sound bei sekunden
            if (seconds == 1 || seconds == 2 || seconds == 3)
            {
                Moeoop.Play();
            }
            if (showTimer)
                seconds--;
            if (decPoints)
            {
                if (TeamTurn.Equals("ROT"))
                    SetTeamPoints("ROT", teamrotPunkte--);
                else if (TeamTurn.Equals("BLAU"))
                    SetTeamPoints("BLAU", teamblauPunkte--);

                yield return new WaitForSecondsRealtime(1);

                if (teamrotPunkte <= 0 || teamblauPunkte <= 0)
                    RundeEnde(-1, "Zeit");
            }
            else
                yield return new WaitForSecondsRealtime(1);
        }
        Timer.SetActive(false);
        RundeEnde(-1, "Zeit");
        yield break;
    }
    private void ClientStartRunde(Player p)
    {
        StartRunde(p.name);
    }
    public void ServerStartRunde()
    {
        if (!Config.SERVER_STARTED)
            return;
        StartRunde(Config.PLAYER_NAME);
    }
    int[] wortzahlen;
    string displayworte;
    private void StartRunde(string playername)
    {
        started = true;
        erklaerer = playername;
        lastErklaerer = erklaerer;
        if (TabuSpiel.GameType.Equals("1 Wort"))
            selectedItem = Config.TABU_SPIEL.getSelected().GetRandomItem(true);
        else if (TabuSpiel.GameType.Equals("Battle Royale"))
        {
            selectedItem = Config.TABU_SPIEL.getSelected().GetRandomItem(true);
            SetTeamPoints("ROT", teamrotPunkte = TabuData.InitTeamPoints("ROT"));
            SetTeamPoints("BLAU", teamblauPunkte = TabuData.InitTeamPoints("BLAU"));
        }
        else
            selectedItem = Config.TABU_SPIEL.getSelected().GetRandomItem(false);

        if (TabuSpiel.GameType.Equals("Normal") && erklaerer.Equals(Config.PLAYER_NAME))
            Skip.SetActive(true);
        else if (TabuSpiel.GameType.Equals("Timer") && erklaerer.Equals(Config.PLAYER_NAME))
            Skip.SetActive(true);
        else if (TabuSpiel.GameType.Equals("Battle Royale") && erklaerer.Equals(Config.PLAYER_NAME))
            Skip.SetActive(true);

        wortzahlen = TabuSpiel.genWorteList(selectedItem);
        displayworte = TabuSpiel.getKartenWorte(selectedItem.tabuworte, wortzahlen);

        ServerUtils.AddBroadcast("#StartRunde " + playername + "|" + TeamTurn + "|" + TabuSpiel.GameType + "|" + timerseconds + "|" + teamrotPunkte + "|" + teamblauPunkte + "|" + TabuSpiel.getIntArrayToString(wortzahlen) + "|" + selectedItem.geheimwort + "|" + selectedItem.tabuworte);

        MarkErklaerer();

        RundeStarten.SetActive(false);
        JoinTeamRot.SetActive(false);
        JoinTeamBlau.SetActive(false);
        for (int i = 0; i < Kreuze.transform.childCount; i++)
            Kreuze.transform.GetChild(i).gameObject.SetActive(false);
        Kreuze.SetActive(true);
        if (playername == Config.PLAYER_NAME)
        {
            Richtig.SetActive(true);
            Falsch.SetActive(true);
        }
        else if (TeamTurn.Equals("ROT") && teamblauList.Contains(Config.PLAYER_NAME))
        {
            Richtig.SetActive(false);
            Falsch.SetActive(true);
        }
        else if (TeamTurn.Equals("BLAU") && teamrotList.Contains(Config.PLAYER_NAME))
        {
            Richtig.SetActive(false);
            Falsch.SetActive(true);
        }
        else
        {
            Richtig.SetActive(false);
            Falsch.SetActive(false);
        }
        // Blende Karte für Spieler der dran ist ein
        if (playername == Config.PLAYER_NAME)
        {
            DisplayKarte(true, selectedItem.geheimwort, displayworte);
        }
        else
        {
            Karte.transform.GetChild(0).GetChild(1).GetComponent<TMP_Text>().text = playername;
            if (TeamTurn.Equals("BLAU") && teamrotList.Contains(Config.PLAYER_NAME))
            {
                DisplayKarte(true, selectedItem.geheimwort, displayworte);
            }
            else if (TeamTurn.Equals("ROT") && teamblauList.Contains(Config.PLAYER_NAME))
            {
                DisplayKarte(true, selectedItem.geheimwort, displayworte);
            }
            else if (TeamTurn.Equals("BLAU") && teamblauList.Contains(Config.PLAYER_NAME))
            {
                DisplayKarte(true, playername, "");
                SpielerIstDran.Play();
            }
            else if (TeamTurn.Equals("ROT") && teamrotList.Contains(Config.PLAYER_NAME))
            {
                DisplayKarte(true, playername, "");
                SpielerIstDran.Play();
            }
            else
            {
                Karte.transform.GetChild(0).GetChild(3).GetComponent<TMP_Text>().text = "";
            }
            Karte.SetActive(true);
        }

        StartTimer();
    }
    private void DisplayKarte(bool show, string Titel, string verboteneWorte)
    {
        StartCoroutine(DisplayKarteEnumerator(show, Titel, verboteneWorte));
    }
    private IEnumerator DisplayKarteEnumerator(bool show, string Titel, string verboteneWorte)
    {
        Karte.SetActive(show);
        yield return new WaitForSeconds(0.0001f);
        Karte.transform.GetChild(0).GetChild(1).GetComponent<TMP_Text>().text = Titel;
        yield return new WaitForSeconds(0.0001f);
        Karte.transform.GetChild(0).GetChild(3).GetComponent<TMP_Text>().text = verboteneWorte.Replace("-", "\n") + "\n ";
        yield return new WaitForSeconds(0.0001f);
        Karte.transform.GetChild(0).GetChild(3).GetComponent<TMP_Text>().text = verboteneWorte.Replace("-", "\n");
        yield return new WaitForSeconds(0.0001f);
        Karte.transform.GetChild(0).GetChild(5).gameObject.SetActive(false);
        yield return new WaitForSeconds(0.0001f);
        Karte.transform.GetChild(0).GetChild(5).gameObject.SetActive(true);
    }
    private IEnumerator AnimateBackground(string type)
    {
        Color c = new Color();
        if (type.Equals("WIN"))
        {
            c = new Color(38f / 255f, 255f / 255f, 0f / 255f, 1f / 255f);

        }
        else if (type.Equals("LOSE"))
        {
            c = new Color(128f / 255f, 0f / 255f, 0f / 255f, 1f / 255f);
        }
        BackgroundColor.GetComponent<Image>().color = c;
        BackgroundColor.SetActive(true);
        for (int i = 0; i < 50; i++)
        {
            c.a = (float)(i / 255f);
            BackgroundColor.GetComponent<Image>().color = c;
            yield return new WaitForSeconds(0.001f);
        }
        yield return new WaitForSeconds(0.001f);
        for (int i = 50; i > 18; i -= 2)
        {
            c.a = (float)(i / 255f);
            BackgroundColor.GetComponent<Image>().color = c;
            yield return new WaitForSeconds(0.001f);
        }
        yield return new WaitForSeconds(0.001f);
        BackgroundColor.SetActive(false);
        yield break;
    }

    private IEnumerator SkipWort()
    {
        yield return new WaitForSeconds(5);
        if (erklaerer.Equals(Config.PLAYER_NAME))
            Skip.SetActive(true);
        yield break;
    }
    private void RundeEnde(int indicator, string playername)
    {
        Logging.log(Logging.LogType.Normal, "TabuServer", "RundeEnde", 
            "Indicator: " + indicator
            + " Player: " + playername
            + " Wort: " + selectedItem.geheimwort
            + " Blau: " + teamblauPunkte
            + " Rot: " + teamrotPunkte);

        #region WortHistory
        AddWortHistory(erklaerer, indicator, selectedItem.geheimwort, selectedItem.tabuworte);
        #endregion

        if (TabuSpiel.GameType.Equals("1 Wort"))
        {
            // Falsch gedrückt
            if (indicator == -2)
            {
                Kreuze.transform.GetChild(0).gameObject.SetActive(true);
                FalschGeraten.Play();
                StartCoroutine(AnimateBackground("LOSE"));
            }
            // Zeit vorbei
            else if (indicator == -1) {}
            // Richtig gedrückt
            else if (indicator == +1)
            {
                Erraten.Play();
                StartCoroutine(AnimateBackground("WIN"));
                if (TeamTurn.Equals("ROT"))
                    SetTeamPoints("ROT", teamrotPunkte += TabuData.P_1WORT[0]);
                else if (TeamTurn.Equals("BLAU"))
                    SetTeamPoints("BLAU", teamblauPunkte += TabuData.P_1WORT[0]);
            }
            else
                Logging.log(Logging.LogType.Error, "TabuServer", "RundeEnde", "Fehler: " + indicator + " " + TeamTurn);

            EndTurn(teamrotPunkte, teamblauPunkte);
        }
        else if (TabuSpiel.GameType.Equals("Normal"))
        {
            // Skip Wort
            if (indicator == -3)
            {
                if (erklaerer.Equals(Config.PLAYER_NAME))
                {
                    Skip.SetActive(false);
                    if (SkipCoroutine != null)
                        StopCoroutine(SkipCoroutine);
                    SkipCoroutine = StartCoroutine(SkipWort());
                }
            }
            // Falsch gedrückt
            else if (indicator == -2)
            {
                FalschGeraten.Play();
                StartCoroutine(AnimateBackground("LOSE"));
                if (HideRichtigFalschSecCoroutine != null)
                    StopCoroutine(HideRichtigFalschSecCoroutine);
                HideRichtigFalschSecCoroutine = StartCoroutine(HideRichtigFalschSec());
                if (TeamTurn.Equals("ROT"))
                    SetTeamPoints("ROT", teamrotPunkte += TabuData.P_NORMAL[1]);
                else if (TeamTurn.Equals("BLAU"))
                    SetTeamPoints("BLAU", teamblauPunkte += TabuData.P_NORMAL[1]);
            }
            // Zeit vorbei
            else if (indicator == -1)
            {
                EndTurn(teamrotPunkte, teamblauPunkte);
            }
            // Richtig gedrückt
            else if (indicator == +1)
            {
                Erraten.Play();
                StartCoroutine(AnimateBackground("WIN"));
                if (HideRichtigFalschSecCoroutine != null)
                    StopCoroutine(HideRichtigFalschSecCoroutine);
                HideRichtigFalschSecCoroutine = StartCoroutine(HideRichtigFalschSec());
                if (TeamTurn.Equals("ROT"))
                    SetTeamPoints("ROT", teamrotPunkte += TabuData.P_NORMAL[0]);
                else if (TeamTurn.Equals("BLAU"))
                    SetTeamPoints("BLAU", teamblauPunkte += TabuData.P_NORMAL[0]);
            }
            else
                Logging.log(Logging.LogType.Error, "TabuServer", "RundeEnde", "Fehler: " + indicator + " " + TeamTurn);
        }
        else if (TabuSpiel.GameType.Equals("Timer"))
        {
            // Skip Wort
            if (indicator == -3)
            {
                if (erklaerer.Equals(Config.PLAYER_NAME))
                {
                    Skip.SetActive(false);
                    if (SkipCoroutine != null)
                        StopCoroutine(SkipCoroutine);
                    SkipCoroutine = StartCoroutine(SkipWort());
                }
                if (TeamTurn.Equals("ROT"))
                    SetTeamPoints("ROT", teamrotPunkte += TabuData.P_TIMER[2]);
                if (TeamTurn.Equals("BLAU"))
                    SetTeamPoints("BLAU", teamblauPunkte += TabuData.P_TIMER[2]);
                if (teamblauPunkte <= 0 || teamrotPunkte <= 0)
                    EndTurn(teamrotPunkte, teamblauPunkte);
            }
            // Falsch gedrückt
            else if (indicator == -2)
            {
                FalschGeraten.Play();
                StartCoroutine(AnimateBackground("LOSE"));
                if (HideRichtigFalschSecCoroutine != null)
                    StopCoroutine(HideRichtigFalschSecCoroutine);
                HideRichtigFalschSecCoroutine = StartCoroutine(HideRichtigFalschSec());
                if (TeamTurn.Equals("ROT"))
                    SetTeamPoints("ROT", teamrotPunkte += TabuData.P_TIMER[1]);
                if (TeamTurn.Equals("BLAU"))
                    SetTeamPoints("BLAU", teamblauPunkte += TabuData.P_TIMER[1]);
                if (teamblauPunkte <= 0 || teamrotPunkte <= 0)
                    EndTurn(teamrotPunkte, teamblauPunkte);
            }
            // Zeit vorbei
            else if (indicator == -1)
            {
                EndTurn(teamrotPunkte, teamblauPunkte);
            }
            // Richtig gedrückt
            else if (indicator == +1)
            {
                Erraten.Play();
                StartCoroutine(AnimateBackground("WIN"));
                if (HideRichtigFalschSecCoroutine != null)
                    StopCoroutine(HideRichtigFalschSecCoroutine);
                HideRichtigFalschSecCoroutine = StartCoroutine(HideRichtigFalschSec());
                if (TeamTurn.Equals("ROT"))
                    SetTeamPoints("ROT", teamrotPunkte += TabuData.P_TIMER[0]);
                else if (TeamTurn.Equals("BLAU"))
                    SetTeamPoints("BLAU", teamblauPunkte += TabuData.P_TIMER[0]);
            }
            else
                Logging.log(Logging.LogType.Error, "TabuServer", "RundeEnde", "Fehler: " + indicator + " " + TeamTurn);
        }
        else if (TabuSpiel.GameType.Equals("Battle Royale"))
        {
            // Skip Wort
            if (indicator == -3)
            {
                if (erklaerer.Equals(Config.PLAYER_NAME))
                {
                    Skip.SetActive(false);
                    if (SkipCoroutine != null)
                        StopCoroutine(SkipCoroutine);
                    SkipCoroutine = StartCoroutine(SkipWort());
                }
                if (TeamTurn.Equals("ROT"))
                    SetTeamPoints("ROT", teamrotPunkte += TabuData.P_BATTLE_ROYALE[2]);
                if (TeamTurn.Equals("BLAU"))
                    SetTeamPoints("BLAU", teamblauPunkte += TabuData.P_BATTLE_ROYALE[2]);

                if (teamblauPunkte <= 0 || teamrotPunkte <= 0)
                    EndTurn(teamrotPunkte, teamblauPunkte);
            }
            // Falsch gedrückt
            else if (indicator == -2)
            {
                FalschGeraten.Play();
                StartCoroutine(AnimateBackground("LOSE"));
                if (HideRichtigFalschSecCoroutine != null)
                    StopCoroutine(HideRichtigFalschSecCoroutine);
                HideRichtigFalschSecCoroutine = StartCoroutine(HideRichtigFalschSec());
                if (TeamTurn.Equals("ROT"))
                    SetTeamPoints("ROT", teamrotPunkte += TabuData.P_BATTLE_ROYALE[1]);
                if (TeamTurn.Equals("BLAU"))
                    SetTeamPoints("BLAU", teamblauPunkte += TabuData.P_BATTLE_ROYALE[1]);

                if (teamblauPunkte <= 0 || teamrotPunkte <= 0)
                {
                    EndTurn(teamrotPunkte, teamblauPunkte);
                }

            }
            // Zeit vorbei
            else if (indicator == -1)
            {
                EndTurn(teamrotPunkte, teamblauPunkte);
            }
            // Richtig gedrückt
            else if (indicator == +1)
            {
                Erraten.Play();
                StartCoroutine(AnimateBackground("WIN"));
                if (HideRichtigFalschSecCoroutine != null)
                    StopCoroutine(HideRichtigFalschSecCoroutine);
                HideRichtigFalschSecCoroutine = StartCoroutine(HideRichtigFalschSec());
                if (TeamTurn.Equals("ROT"))
                    SetTeamPoints("ROT", teamrotPunkte += TabuData.P_BATTLE_ROYALE[0]);
                else if (TeamTurn.Equals("BLAU"))
                    SetTeamPoints("BLAU", teamblauPunkte += TabuData.P_BATTLE_ROYALE[0]);

                // Switch Team
                if (TeamTurn.Equals("ROT"))
                    TeamTurn = "BLAU";
                else
                    TeamTurn = "ROT";

                Skip.SetActive(false);
                string templastErklaerer = erklaerer;
                if (TeamTurn.Equals("ROT"))
                {
                    List<string> namen = new List<string>();
                    namen.AddRange(teamrotList);
                    namen.Remove(lastErklaerer);
                    if (namen.Count > 0)
                        erklaerer = namen[UnityEngine.Random.Range(0, namen.Count)];
                    else
                        erklaerer = teamrotList[UnityEngine.Random.Range(0, teamrotList.Count)];
                }
                else if (TeamTurn.Equals("BLAU"))
                {
                    List<string> namen = new List<string>();
                    namen.AddRange(teamblauList);
                    namen.Remove(lastErklaerer);
                    if (namen.Count > 0)
                        erklaerer = namen[UnityEngine.Random.Range(0, namen.Count)];
                    else
                        erklaerer = teamblauList[UnityEngine.Random.Range(0, teamblauList.Count)];
                }
                lastErklaerer = templastErklaerer;
                if (erklaerer.Equals(Config.PLAYER_NAME))
                    Skip.SetActive(true);

                MarkErklaerer();
            }
            else
                Logging.log(Logging.LogType.Error, "TabuServer", "RundeEnde", "Fehler: " + indicator + " " + TeamTurn);
        }

        // Neue Karte
        if (started)
        {
            if (TabuSpiel.GameType.Equals("Battle Royale"))
                selectedItem = Config.TABU_SPIEL.getSelected().GetRandomItem(true);
            else
                selectedItem = Config.TABU_SPIEL.getSelected().GetRandomItem(false);
            wortzahlen = TabuSpiel.genWorteList(selectedItem);
            displayworte = TabuSpiel.getKartenWorte(selectedItem.tabuworte, wortzahlen);
            if (erklaerer.Equals(Config.PLAYER_NAME))
                DisplayKarte(true, selectedItem.geheimwort, displayworte);
            else if (teamrotList.Contains(Config.PLAYER_NAME) && TeamTurn.Equals("BLAU"))
                DisplayKarte(true, selectedItem.geheimwort, displayworte);
            else if (teamblauList.Contains(Config.PLAYER_NAME) && TeamTurn.Equals("ROT"))
                DisplayKarte(true, selectedItem.geheimwort, displayworte);
            else
                DisplayKarte(true, erklaerer, "");
        }

        ServerUtils.AddBroadcast("#RundeEnde " + TeamTurn + "|" + indicator + "|" + TabuSpiel.GameType + "|" + teamrotPunkte + "|" + teamblauPunkte + "|" + erklaerer + "|" + true + "|" + TabuSpiel.getIntArrayToString(wortzahlen) + "|" + selectedItem.geheimwort + "|" + selectedItem.tabuworte);
    }
    private void EndTurn(int rotpoints, int blaupoints)
    {
        if (!started)
            return;
        started = false;
        DisplayKarte(true, selectedItem.geheimwort, displayworte);
        StopCoroutine(TimerCoroutine);
        Timer.SetActive(false);
        Skip.SetActive(false);
        Richtig.SetActive(false);
        Falsch.SetActive(false);
        JoinTeamBlau.SetActive(true);
        JoinTeamRot.SetActive(true);
        erklaerer = "";
        MarkErklaerer();

        SetTeamPoints("ROT", rotpoints);
        SetTeamPoints("BLAU", blaupoints);

        if (TeamTurn.Equals("ROT"))
            TeamTurn = "BLAU";
        else
            TeamTurn = "ROT";
        if (TeamTurn.Equals("ROT") && teamrotList.Contains(Config.PLAYER_NAME))
            RundeStarten.SetActive(true);
        else if (TeamTurn.Equals("BLAU") && teamblauList.Contains(Config.PLAYER_NAME))
            RundeStarten.SetActive(true);
    }
    public void ChangePoints(Button btn)
    {
        if (btn.name.StartsWith("Rot"))
        {
            if (btn.name.EndsWith("+1"))
            {
                teamrotPunkte++;
            }
            else if (btn.name.EndsWith("-1"))
            {
                teamrotPunkte--;
            }
            else
                Logging.log(Logging.LogType.Error, "TabuServer", "ChangePoints", "Button nicht bekannt: " + btn.name);
        }
        else if (btn.name.StartsWith("Blau"))
        {
            if (btn.name.EndsWith("+1"))
            {
                teamblauPunkte++;
            }
            else if (btn.name.EndsWith("-1"))
            {
                teamblauPunkte--;
            }
            else
                Logging.log(Logging.LogType.Error, "TabuServer", "ChangePoints", "Button nicht bekannt: " + btn.name);
        }
        else
            Logging.log(Logging.LogType.Error, "TabuServer", "ChangePoints", "Button nicht bekannt: " + btn.name);

        SetTeamPoints("ROT", teamrotPunkte);
        SetTeamPoints("BLAU", teamblauPunkte);
        ServerUtils.AddBroadcast("#TeamPunkte " + teamrotPunkte + "|" + teamblauPunkte);
    }
    public void ChangePoints(TMP_InputField input)
    {
        if (input.text.Length == 0)
            return;
        if (input.name.StartsWith("Rot"))
        {
            int points = int.Parse(input.text);
            teamrotPunkte += points;
        }
        else if (input.name.StartsWith("Blau"))
        {
            int points = int.Parse(input.text);
            teamblauPunkte += points;
        }
        else
            Logging.log(Logging.LogType.Error, "TabuServer", "ChangePoints", "InputField nicht bekannt: " + input.name);
        input.text = "";
        SetTeamPoints("ROT", teamrotPunkte);
        SetTeamPoints("BLAU", teamblauPunkte);
        ServerUtils.AddBroadcast("#TeamPunkte " + teamrotPunkte + "|" + teamblauPunkte);
    }
    public void ZufaelligeTeams()
    {
        if (teamblauList.Count + teamrotList.Count == 0)
            return;
        teamrotList.Clear();
        teamblauList.Clear();
        List<string> plisttemp = new List<string>();
        plisttemp.Add(Config.PLAYER_NAME);
        foreach (Player item in Config.PLAYERLIST)
            if (item.isConnected && item.name.Length > 0)
                plisttemp.Add(item.name);
        List<string> plist = new List<string>();
        while (plisttemp.Count > 0)
        {
            int random = UnityEngine.Random.Range(0, plisttemp.Count);
            plist.Add(plisttemp[random]);
            plisttemp.RemoveAt(random);
        }
        for (int i = 0; i < plist.Count; i++)
        {
            if (i % 2 == 0)
                teamrotList.Add(plist[i]);
            else
                teamblauList.Add(plist[i]);
        }

        // Anzeigen aktualisieren
        string teamrot = "";
        for (int i = 1; i < TeamRot.transform.childCount; i++)
        {
            int index = i - 1;
            Transform PlayerObject = TeamRot.transform.GetChild(i);
            PlayerObject.gameObject.SetActive(false);
            if (teamrotList.Count > index)
            {
                PlayerObject.GetChild(1).GetComponent<TMP_Text>().text = teamrotList[index];
                // Server
                if (teamrotList[index] == Config.PLAYER_NAME)
                {
                    PlayerObject.GetChild(0).GetComponent<Image>().sprite = Config.SERVER_PLAYER.icon2.icon;
                    teamrot += "[#]" + teamrotList[index] + "~" + Config.SERVER_PLAYER.icon2.id;
                }
                // Client
                else
                {
                    PlayerObject.GetChild(0).GetComponent<Image>().sprite = Player.getPlayerIconByPlayerName(teamrotList[index]).icon;
                    teamrot += "[#]" + teamrotList[index] + "~" + Player.getPlayerIconByPlayerName(teamrotList[index]).id;
                }
                PlayerObject.gameObject.SetActive(true);
            }
        }
        if (teamrot.Length > 3)
            teamrot = teamrot.Substring("[#]".Length);
        string teamblau = "";
        for (int i = 1; i < TeamBlau.transform.childCount; i++)
        {
            int index = i - 1;
            Transform PlayerObject = TeamBlau.transform.GetChild(i);
            PlayerObject.gameObject.SetActive(false);
            if (teamblauList.Count > index)
            {
                PlayerObject.GetChild(1).GetComponent<TMP_Text>().text = teamblauList[index];
                // Server
                if (teamblauList[index] == Config.PLAYER_NAME)
                {
                    PlayerObject.GetChild(0).GetComponent<Image>().sprite = Config.SERVER_PLAYER.icon2.icon;
                    teamblau += "[#]" + teamblauList[index] + "~" + Config.SERVER_PLAYER.icon2.id;
                }
                // Client
                else
                {
                    PlayerObject.GetChild(0).GetComponent<Image>().sprite = Player.getPlayerIconByPlayerName(teamblauList[index]).icon;
                    teamblau += "[#]" + teamblauList[index] + "~" + Player.getPlayerIconByPlayerName(teamblauList[index]).id;
                }
                PlayerObject.gameObject.SetActive(true);
            }
        }
        if (teamblau.Length > 3)
            teamblau = teamblau.Substring("[#]".Length);
        // Listen Broadcast
        ServerUtils.AddBroadcast("#TeamUpdate " + teamrot + "|" + teamblau);

        if (!started)
        {
            if (TeamTurn.Equals("ROT") && teamrotList.Contains(Config.PLAYER_NAME))
                RundeStarten.SetActive(true);
            else if (TeamTurn.Equals("BLAU") && teamblauList.Contains(Config.PLAYER_NAME))
                RundeStarten.SetActive(true);
            else
                RundeStarten.SetActive(false);
        }
    }
    public void ChangeTimerSec(TMP_InputField input)
    {
        if (input.text.Length == 0)
            return;
        int temp = Int32.Parse(input.text);
        if (temp < 0)
            return;
        timerseconds = temp;
    }
    #endregion
    private void MarkErklaerer()
    {
        // Färbt namen wieder weiß
        for (int i = 1; i < TeamRot.transform.childCount; i++)
        {
            int index = i - 1;
            Transform PlayerObject = TeamRot.transform.GetChild(i);
            if (teamrotList.Count > index)
            {
                PlayerObject.GetChild(1).GetComponent<TMP_Text>().text = teamrotList[index];
                if (teamrotList[index] == erklaerer)
                    PlayerObject.GetChild(1).GetComponent<TMP_Text>().text = "<color=green>" + teamrotList[index];
            }
        }
        for (int i = 1; i < TeamBlau.transform.childCount; i++)
        {
            int index = i - 1;
            Transform PlayerObject = TeamBlau.transform.GetChild(i);
            if (teamblauList.Count > index)
            {
                PlayerObject.GetChild(1).GetComponent<TMP_Text>().text = teamblauList[index];
                if (teamblauList[index] == erklaerer)
                    PlayerObject.GetChild(1).GetComponent<TMP_Text>().text = "<color=green>" + teamblauList[index];
            }
        }
    }
    private void AddWortHistory(string icon, int index, string wort, string tabus)
    {
        Transform content = HistoryContentElement.transform.parent;

        GameObject newObject = GameObject.Instantiate(content.GetChild(0).gameObject, content, false);
        newObject.transform.localScale = new Vector3(1, 1, 1);
        newObject.name = "Object" + "*" + content.childCount;
        newObject.SetActive(true);
        if (icon == Config.SERVER_PLAYER.name)
        {
            newObject.transform.GetChild(0).GetComponent<Image>().sprite = Config.SERVER_PLAYER.icon2.icon;
        }
        else
        {
            newObject.transform.GetChild(0).GetComponent<Image>().sprite = Player.getPlayerIconByPlayerName(icon).icon;
        }
        newObject.transform.GetChild(1).GetComponent<TMP_Text>().text = wort;
        newObject.transform.GetChild(1).GetChild(0).GetComponent<TMP_Text>().text = tabus.Replace("-", "\n");
        if (index == -3) // Skip
        {
            newObject.transform.GetChild(2).GetChild(0).gameObject.SetActive(true);
        }
        else if (index == -2) // Falsch
        {
            newObject.transform.GetChild(2).GetChild(2).gameObject.SetActive(true);
        }
        else if (index == -1) // Zeit
        {
            //newObject.transform.GetChild(2).GetChild(2).gameObject.SetActive(true);
        }
        else if (index == +1) // Richtig
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