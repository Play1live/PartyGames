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
    private List<string> broadcastmsgs;

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
    private GameObject RichtigFalsch1;
    private GameObject RichtigFalsch2;
    private GameObject RundeStarten;

    private List<string> teamrotList;
    private List<string> teamblauList;
    private string TeamTurn;
    private bool started;

    private Coroutine TimerCoroutine;
    private TabuGamePacks selectedPack;
    private TabuItem selectedItem;

    void OnEnable()
    {
        broadcastmsgs = new List<string>();
        PlayerConnected = new bool[Config.SERVER_MAX_CONNECTIONS];
        InitGame();
        StartCoroutine(NewBroadcast());
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

            #region Spieler Disconnected Message
            for (int i = 0; i < Config.PLAYERLIST.Length; i++)
            {
                if (Config.PLAYERLIST[i].isConnected == false)
                {
                    if (Config.PLAYERLIST[i].isDisconnected == true)
                    {
                        Logging.log(Logging.LogType.Normal, "TabuServer", "Update", "Spieler hat die Verbindung getrennt. ID: " + Config.PLAYERLIST[i].id);
                        //Broadcast(Config.PLAYERLIST[i].name + " has disconnected", Config.PLAYERLIST);
                        Config.PLAYERLIST[i].isConnected = false;
                        Config.PLAYERLIST[i].isDisconnected = false;
                        Config.SERVER_ALL_CONNECTED = false;
                        Config.PLAYERLIST[i].name = "";
                    }
                }
            }
            #endregion
        }
        #endregion
    }

    private void OnApplicationQuit()
    {
        Broadcast("#ServerClosed", Config.PLAYERLIST);
        Logging.log(Logging.LogType.Normal, "TabuServer", "OnApplicationQuit", "Server wird geschlossen.");
        Config.SERVER_TCP.Server.Close();
    }

    IEnumerator NewBroadcast()
    {
        while (true)
        {
            // Broadcastet alle MSGs nacheinander
            if (broadcastmsgs.Count != 0)
            {
                string msg = broadcastmsgs[0];
                broadcastmsgs.RemoveAt(0);
                Broadcast(msg);
                yield return null;
            }
            //yield return new WaitForSeconds(0.005f);
            yield return new WaitForSeconds(0.01f);
        }
        yield break;
    }

    #region Server Stuff  
    #region Kommunikation
    /// <summary>
    /// Sendet eine Nachricht an den übergebenen Spieler
    /// </summary>
    /// <param name="data">Nachricht</param>
    /// <param name="sc">Spieler</param>
    private void SendMSG(string data, Player sc)
    {
        try
        {
            StreamWriter writer = new StreamWriter(sc.tcp.GetStream());
            writer.WriteLine(data);
            writer.Flush();
        }
        catch (Exception e)
        {
            Logging.log(Logging.LogType.Warning, "TabuServer", "SendMSG", "Nachricht an Client: " + sc.id + " (" + sc.name + ") konnte nicht gesendet werden.", e);
            // Verbindung zum Client wird getrennt
            ClientClosed(sc);
        }
    }
    /// <summary>
    /// Sendet eine Nachricht an alle Spieler der liste
    /// </summary>
    /// <param name="data">Nachricht</param>
    /// <param name="spieler">Spielerliste</param>
    private void Broadcast(string data, Player[] spieler)
    {
        foreach (Player sc in spieler)
        {
            if (sc.isConnected)
                SendMSG(data, sc);
        }
    }
    /// <summary>
    /// Sendet eine Nachricht an alle verbundenen Spieler
    /// </summary>
    /// <param name="data">Nachricht</param>
    private void Broadcast(string data)
    {
        foreach (Player sc in Config.PLAYERLIST)
        {
            if (sc.isConnected)
                SendMSG(data, sc);
        }
    }
    private void BroadcastNew(string data)
    {
        broadcastmsgs.Add(data);
    }
    /// <summary>
    /// Einkommende Nachrichten die von Spielern an den Server gesendet werden
    /// </summary>
    /// <param name="spieler">Spieler</param>
    /// <param name="data">Nachricht</param>
    private void OnIncommingData(Player spieler, string data)
    {
        string cmd;
        if (data.Contains(" "))
            cmd = data.Split(' ')[0];
        else
            cmd = data;
        data = data.Replace(cmd + " ", "");

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
        Logging.log(Logging.LogType.Debug, "TabuServer", "Commands", "Eingehende Nachricht: " + player.name + " " + player.id + " -> " + cmd + "   ---   " + data);
        // Sucht nach Command
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "TabuServer", "Commands", "Unkown Command: " + cmd + " -> " + data);
                break;

            case "#ClientClosed":
                PlayDisconnectSound();
                teamrotList.Remove(player.name);
                teamblauList.Remove(player.name);
                ClientClosed(player);
                JoinTeam("", "");
                break;
            case "#TestConnection":
                break;
            case "#ClientFocusChange":
                break;

            case "#JoinTabu":
                PlayerConnected[player.id - 1] = true;
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
            case "#ClientRichtigGeraten":
                ClientRichtigGeraten(player);
                break;
        }
    }
    #endregion
    /// <summary>
    /// Spieler beendet das Spiel
    /// </summary>
    /// <param name="player">Spieler</param>
    private void ClientClosed(Player player)
    {
        player.icon = Resources.Load<Sprite>("Images/ProfileIcons/empty");
        player.name = "";
        player.points = 0;
        player.isConnected = false;
        player.isDisconnected = true;
    }
    /// <summary>
    /// Spiel Verlassen & Zurück in die Lobby laden
    /// </summary>
    public void SpielVerlassenButton()
    {
        Logging.log(Logging.LogType.Debug, "TabuServer", "SpielVerlassenButton", "Spiel wird beendet. Lädt ins Hauptmenü.");
        SceneManager.LoadScene("Startup");
        BroadcastNew("#ZurueckInsHauptmenue");
    }
    /// <summary>
    /// Spielt den Disconnect Sound ab
    /// </summary>
    private void PlayDisconnectSound()
    {
        DisconnectSound.Play();
    }
    #region GameLogic
    private void InitGame()
    {
        BackgroundColor = GameObject.Find("Spielbrett/BackgroundColor");
        BackgroundColor.SetActive(false);
        TeamRot = GameObject.Find("Spielbrett/TeamRot");
        teamrotList = new List<string>();
        JoinTeamRot = GameObject.Find("Spielbrett/JoinRot");
        teamrotPunkte = 0;
        SetTeamPoints("ROT", teamrotPunkte);
        TeamBlau = GameObject.Find("Spielbrett/TeamBlau");
        teamblauList = new List<string>();
        JoinTeamBlau = GameObject.Find("Spielbrett/JoinBlau");
        teamblauPunkte = 0;
        SetTeamPoints("BLAU", teamblauPunkte);
        Timer = GameObject.Find("Spielbrett/Timer");
        Timer.SetActive(false);
        Kreuze = GameObject.Find("Spielbrett/Kreuze");
        Kreuze.SetActive(false);
        Karte = GameObject.Find("Spielbrett/Karte");
        Karte.SetActive(false);
        RichtigFalsch1 = GameObject.Find("Spielbrett/RichtigFalsch (0)");
        RichtigFalsch1.SetActive(false);
        RichtigFalsch2 = GameObject.Find("Spielbrett/RichtigFalsch (1)");
        RichtigFalsch2.SetActive(false);
        RundeStarten = GameObject.Find("Spielbrett/RundeStarten");
        RundeStarten.SetActive(false);
        started = false;

        teamblauList.Add(Config.PLAYER_NAME);
        foreach (Player item in Config.PLAYERLIST)
            if (item.name.Length > 0 && item.isConnected)
                teamblauList.Add(item.name);

        TeamTurn = "ROT";

        // Random Pack select
        string[] packs = new string[] { "Blau", "Gelb", "Lila", "Rot" };
        int random = UnityEngine.Random.Range(0, packs.Length);
        selectedPack = new TabuGamePacks(packs[random], "Spiele/Tabu/" + packs[random]);
        GameObject.Find("ServerSide/PackTitle").GetComponent<TMP_Text>().text = selectedPack.titel;
    }
    private void StartTimer()
    {
        if (TimerCoroutine != null)
            StopCoroutine(TimerCoroutine);
        TimerCoroutine = StartCoroutine(RunTimer(61));
    }
    private IEnumerator RunTimer(int seconds)
    {
        Timer.SetActive(true);

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
            seconds--;
            yield return new WaitForSecondsRealtime(1);
        }
        Timer.SetActive(false);
        RundeEnde(-1, "Zeit");
        yield break;
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
            {
                RundeStarten.SetActive(true);
            }
            else if (TeamTurn.Equals("BLAU") && teamblauList.Contains(Config.PLAYER_NAME))
            {
                RundeStarten.SetActive(true);
            }
            else
            {
                RundeStarten.SetActive(false);
            }
        }
    }
    private void JoinTeam(string team, string player)
    {
        if (team.Equals("") && player.Equals(""))
        {
            // Anzeigen aktualisieren
            string teamrot1 = "";
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
                        PlayerObject.GetChild(0).GetComponent<Image>().sprite = Config.SERVER_ICON;
                        teamrot1 += "[#]" + teamrotList[index] + "~" + Config.SERVER_ICON.name;
                    }
                    // Client
                    else
                    {
                        PlayerObject.GetChild(0).GetComponent<Image>().sprite = Player.getSpriteByPlayerName(teamrotList[index]);
                        teamrot1 += "[#]" + teamrotList[index] + "~" + Player.getSpriteByPlayerName(teamrotList[index]).name;
                    }
                    PlayerObject.gameObject.SetActive(true);
                }
            }
            if (teamrot1.Length > 3)
                teamrot1 = teamrot1.Substring("[#]".Length);
            string teamblau1 = "";
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
                        PlayerObject.GetChild(0).GetComponent<Image>().sprite = Config.SERVER_ICON;
                        teamblau1 += "[#]" + teamblauList[index] + "~" + Config.SERVER_ICON.name;
                    }
                    // Client
                    else
                    {
                        PlayerObject.GetChild(0).GetComponent<Image>().sprite = Player.getSpriteByPlayerName(teamblauList[index]);
                        teamblau1 += "[#]" + teamblauList[index] + "~" + Player.getSpriteByPlayerName(teamblauList[index]).name;
                    }
                    PlayerObject.gameObject.SetActive(true);
                }
            }
            if (teamblau1.Length > 3)
                teamblau1 = teamblau1.Substring("[#]".Length);
            // Listen Broadcast
            BroadcastNew("#TeamUpdate " + teamrot1 + "|" + teamblau1);
            return;
        }

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
                    PlayerObject.GetChild(0).GetComponent<Image>().sprite = Config.SERVER_ICON;
                    teamrot += "[#]" + teamrotList[index] + "~" + Config.SERVER_ICON.name;
                }
                // Client
                else 
                { 
                    PlayerObject.GetChild(0).GetComponent<Image>().sprite = Player.getSpriteByPlayerName(teamrotList[index]);
                    teamrot += "[#]" + teamrotList[index] + "~" + Player.getSpriteByPlayerName(teamrotList[index]).name;
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
                    PlayerObject.GetChild(0).GetComponent<Image>().sprite = Config.SERVER_ICON;
                    teamblau += "[#]" + teamblauList[index] + "~" + Config.SERVER_ICON.name;
                }
                // Client
                else
                {
                    PlayerObject.GetChild(0).GetComponent<Image>().sprite = Player.getSpriteByPlayerName(teamblauList[index]);
                    teamblau += "[#]" + teamblauList[index] + "~" + Player.getSpriteByPlayerName(teamblauList[index]).name;
                }
                PlayerObject.gameObject.SetActive(true);
            }
        }
        if (teamblau.Length > 3)
            teamblau = teamblau.Substring("[#]".Length);
        // Listen Broadcast
        BroadcastNew("#TeamUpdate " + teamrot + "|" + teamblau);
    }
    private void SetKreuzClient(Player p, string data)
    {
        SetKreuz(p.name);
    }
    public void SetKreuzServer()
    {
        if (!Config.SERVER_STARTED)
            return;
        RichtigFalsch1.SetActive(false);
        RichtigFalsch2.SetActive(false);

        if (RichtigFalsch1.GetComponentInChildren<TMP_Text>().text.Equals("Erraten"))
            ServerRichtigGeraten();
        if (RichtigFalsch1.GetComponentInChildren<TMP_Text>().text.Equals("Fehler"))
            SetKreuz(Config.PLAYER_NAME);
    }
    private void SetKreuz(string player)
    {
        if (!started)
            return;
        if (TeamTurn == "ROT")
        {
            if (teamblauList.Contains(player))
            {
                // 1
                if (teamblauList.Count <= 2)
                {
                    if (!Kreuze.transform.GetChild(2).gameObject.activeInHierarchy)
                    {
                        Kreuze.transform.GetChild(2).gameObject.SetActive(true);
                        BroadcastNew("#KreuzAn 2");
                        ParseAlleKreuzeAn(player);
                    }
                    else
                    {
                        // Alle Kreuze bereits an
                        ParseAlleKreuzeAn(player);
                    }
                }
                // 2
                else if (teamblauList.Count <= 3)
                {
                    if (!Kreuze.transform.GetChild(0).gameObject.activeInHierarchy)
                    {
                        Kreuze.transform.GetChild(0).gameObject.SetActive(true);
                        BroadcastNew("#KreuzAn 0");
                        DisplayX.Play();
                    }
                    else if (!Kreuze.transform.GetChild(2).gameObject.activeInHierarchy)
                    {
                        Kreuze.transform.GetChild(2).gameObject.SetActive(true);
                        BroadcastNew("#KreuzAn 2");
                        ParseAlleKreuzeAn(player);
                    }
                    else
                    {
                        // Alle Kreuze bereits an
                        ParseAlleKreuzeAn(player);
                    }
                }
                // 3
                else
                {
                    if (teamblauList.Contains(player))
                    {
                        if (!Kreuze.transform.GetChild(0).gameObject.activeInHierarchy)
                        {
                            Kreuze.transform.GetChild(0).gameObject.SetActive(true);
                            BroadcastNew("#KreuzAn 0");
                            DisplayX.Play();
                        }
                        else if (!Kreuze.transform.GetChild(1).gameObject.activeInHierarchy)
                        {
                            Kreuze.transform.GetChild(1).gameObject.SetActive(true);
                            BroadcastNew("#KreuzAn 1");
                            DisplayX.Play();
                        }
                        else if (!Kreuze.transform.GetChild(2).gameObject.activeInHierarchy)
                        {
                            Kreuze.transform.GetChild(2).gameObject.SetActive(true);
                            BroadcastNew("#KreuzAn 2");
                            ParseAlleKreuzeAn(player);
                        }
                        else
                        {
                            // Alle Kreuze bereits an
                            ParseAlleKreuzeAn(player);
                        }
                    }
                }
            }
        }
        else if (TeamTurn == "BLAU")
        {
            if (teamrotList.Contains(player))
            {
                // 1
                if (teamblauList.Count <= 2)
                {
                    if (!Kreuze.transform.GetChild(2).gameObject.activeInHierarchy)
                    {
                        Kreuze.transform.GetChild(2).gameObject.SetActive(true);
                        BroadcastNew("#KreuzAn 2");
                        ParseAlleKreuzeAn(player);
                    }
                    else
                    {
                        // Alle Kreuze bereits an
                        ParseAlleKreuzeAn(player);
                    }
                }
                // 2
                else if (teamblauList.Count <= 3)
                {
                    if (!Kreuze.transform.GetChild(1).gameObject.activeInHierarchy)
                    {
                        Kreuze.transform.GetChild(1).gameObject.SetActive(true);
                        BroadcastNew("#KreuzAn 1");
                        DisplayX.Play();
                    }
                    else if (!Kreuze.transform.GetChild(2).gameObject.activeInHierarchy)
                    {
                        Kreuze.transform.GetChild(2).gameObject.SetActive(true);
                        BroadcastNew("#KreuzAn 2");
                        ParseAlleKreuzeAn(player);
                    }
                    else
                    {
                        // Alle Kreuze bereits an
                        ParseAlleKreuzeAn(player);
                    }
                }
                // 3
                else
                {
                    if (!Kreuze.transform.GetChild(0).gameObject.activeInHierarchy)
                    {
                        Kreuze.transform.GetChild(0).gameObject.SetActive(true);
                        BroadcastNew("#KreuzAn 0");
                        DisplayX.Play();
                    }
                    else if (!Kreuze.transform.GetChild(1).gameObject.activeInHierarchy)
                    {
                        Kreuze.transform.GetChild(1).gameObject.SetActive(true);
                        BroadcastNew("#KreuzAn 1");
                        DisplayX.Play();
                    }
                    else if (!Kreuze.transform.GetChild(2).gameObject.activeInHierarchy)
                    {
                        Kreuze.transform.GetChild(2).gameObject.SetActive(true);
                        BroadcastNew("#KreuzAn 2");
                        ParseAlleKreuzeAn(player);
                    }
                    else
                    {
                        // Alle Kreuze bereits an
                        ParseAlleKreuzeAn(player);
                    }
                }
            }
        }
        else
            Logging.log(Logging.LogType.Error, "TabuServer", "SetKreuz", "Team ist unbekannt: " + TeamTurn);
    }
    private void ClientRichtigGeraten(Player p)
    {
        RichtigGeraten(p.name);
    }
    private void ServerRichtigGeraten()
    {
        if (!Config.SERVER_STARTED)
            return;
        RichtigGeraten(Config.PLAYER_NAME);
    }
    private void RichtigGeraten(string name)
    {
        RundeEnde(+1, name);
    }
    private void ParseAlleKreuzeAn(string name)
    {
        RundeEnde(-1, name);
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
        Karte.transform.GetChild(0).GetChild(3).GetComponent<TMP_Text>().text = verboteneWorte.Replace("\\n", "\n")+"\n ";
        yield return new WaitForSeconds(0.0001f);
        Karte.transform.GetChild(0).GetChild(3).GetComponent<TMP_Text>().text = verboteneWorte.Replace("\\n", "\n");
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
    private void RundeEnde(int points, string playername)
    {
        StopCoroutine(TimerCoroutine);
        Timer.SetActive(false);

        BroadcastNew("#RundeEnde " + TeamTurn + "|" + points + "|" + playername);
        DisplayKarte(true, selectedItem.geheimwort, selectedItem.verboteneWorte);

        if (points == +1)
        {
            Erraten.Play();
            StartCoroutine(AnimateBackground("WIN"));
            if (TeamTurn.Equals("ROT"))
            {
                teamrotPunkte += 1;
                SetTeamPoints("ROT", teamrotPunkte);
            }
            else if (TeamTurn.Equals("BLAU"))
            {
                teamblauPunkte += 1;
                SetTeamPoints("BLAU", teamblauPunkte);
            }
        }
        else if (points == -1)
        {
            FalschGeraten.Play();
            StartCoroutine(AnimateBackground("LOSE"));
            if (TeamTurn.Equals("ROT"))
            {
                teamblauPunkte += 1;
                SetTeamPoints("BLAU", teamblauPunkte);
            }
            else if (TeamTurn.Equals("BLAU"))
            {
                teamrotPunkte += 1;
                SetTeamPoints("ROT", teamrotPunkte);
            }
        }
        else
            Logging.log(Logging.LogType.Error, "TabuServer", "RundeEnde", "Fehler: " + points + " " + TeamTurn);

        RichtigFalsch1.SetActive(false);
        RichtigFalsch2.SetActive(false);
        JoinTeamBlau.SetActive(true);
        JoinTeamRot.SetActive(true);
        if (TeamTurn.Equals("ROT"))
            TeamTurn = "BLAU";
        else if (TeamTurn.Equals("BLAU"))
            TeamTurn = "ROT";
        started = false;
        if (TeamTurn.Equals("ROT") && teamrotList.Contains(Config.PLAYER_NAME))
            RundeStarten.SetActive(true);
        else if (TeamTurn.Equals("BLAU") && teamblauList.Contains(Config.PLAYER_NAME))
            RundeStarten.SetActive(true);
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
    private void StartRunde(string name)
    {
        started = true;
        selectedItem = selectedPack.GetRandomItem();

        BroadcastNew("#StartRunde " + name + "|" + TeamTurn + "|" + selectedItem.geheimwort + "|" + selectedItem.verboteneWorte);
        RundeStarten.SetActive(false);
        JoinTeamRot.SetActive(false);
        JoinTeamBlau.SetActive(false);
        for (int i = 0; i < Kreuze.transform.childCount; i++)
            Kreuze.transform.GetChild(i).gameObject.SetActive(false);
        Kreuze.SetActive(true);
        if (TeamTurn.Equals("ROT") && name == Config.PLAYER_NAME)
        {
            RichtigFalsch1.GetComponentInChildren<TMP_Text>().text = "Erraten";
            RichtigFalsch2.GetComponentInChildren<TMP_Text>().text = "Erraten";
            RichtigFalsch1.SetActive(true);
            RichtigFalsch2.SetActive(true);
        }
        else if (TeamTurn.Equals("BLAU") && name == Config.PLAYER_NAME)
        {
            RichtigFalsch1.GetComponentInChildren<TMP_Text>().text = "Erraten";
            RichtigFalsch2.GetComponentInChildren<TMP_Text>().text = "Erraten";
            RichtigFalsch1.SetActive(true);
            RichtigFalsch2.SetActive(true);
        }
        else if (TeamTurn.Equals("ROT") && teamblauList.Contains(Config.PLAYER_NAME))
        {
            RichtigFalsch1.GetComponentInChildren<TMP_Text>().text = "Fehler";
            RichtigFalsch2.GetComponentInChildren<TMP_Text>().text = "Fehler";
            RichtigFalsch1.SetActive(true);
            RichtigFalsch2.SetActive(true);
        }
        else if (TeamTurn.Equals("BLAU") && teamrotList.Contains(Config.PLAYER_NAME))
        {
            RichtigFalsch1.GetComponentInChildren<TMP_Text>().text = "Fehler";
            RichtigFalsch2.GetComponentInChildren<TMP_Text>().text = "Fehler";
            RichtigFalsch1.SetActive(true);
            RichtigFalsch2.SetActive(true);
        }
        else
        {
            RichtigFalsch1.SetActive(false);
            RichtigFalsch2.SetActive(false);
        }
        // Blende Karte für Spieler der dran ist ein
        if (name == Config.PLAYER_NAME)
        {
            DisplayKarte(true, selectedItem.geheimwort, selectedItem.verboteneWorte);
        }
        else
        {
            Karte.transform.GetChild(0).GetChild(1).GetComponent<TMP_Text>().text = name;
            if (TeamTurn.Equals("BLAU") && teamrotList.Contains(Config.PLAYER_NAME))
            {
                DisplayKarte(true, name, selectedItem.verboteneWorte);
            }
            else if (TeamTurn.Equals("ROT") && teamblauList.Contains(Config.PLAYER_NAME))
            {
                DisplayKarte(true, name, selectedItem.verboteneWorte);
            }
            else if (TeamTurn.Equals("BLAU") && teamblauList.Contains(Config.PLAYER_NAME))
            {
                DisplayKarte(true, name, "");
                SpielerIstDran.Play();
            }
            else if (TeamTurn.Equals("ROT") && teamrotList.Contains(Config.PLAYER_NAME))
            {
                DisplayKarte(true, name, "");
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


    #endregion
}