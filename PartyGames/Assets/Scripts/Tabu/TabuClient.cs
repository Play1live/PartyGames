using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TabuClient : MonoBehaviour
{
    private GameObject[] Playerlist;

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
    private GameObject RundeStarten;
    private GameObject Skip;
    private Coroutine SkipCoroutine;
    private int timerseconds;
    private Coroutine HideRichtigFalschSecCoroutine;

    private List<string> teamrotList;
    private List<string> teamblauList;
    private string TeamTurn;
    private bool started;
    private string erklaerer;
    string wzahlenstring = "";

    private Coroutine TimerCoroutine;
    private TabuItem selectedItem;

    void OnEnable()
    {
        if (!Config.CLIENT_STARTED)
            return;
        ClientUtils.SendToServer("#JoinTabu");
        InitGame();
        StartCoroutine(TestConnectionToServer());
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

            case "#TeamUpdate":
                JoinTeam(data);
                break;
            case "#RundeEnde":
                RundeEnde(data);
                break;
            case "#StartRunde":
                StartRunde(data);
                break;
            case "#KreuzAn":
                KreuzAn(data);
                break;
            case "#TeamPunkte":
                teamrotPunkte = Int32.Parse(data.Split('|')[0]);
                teamblauPunkte = Int32.Parse(data.Split('|')[1]);
                SetTeamPoints("ROT", teamrotPunkte);
                SetTeamPoints("BLAU", teamblauPunkte);
                break;
            case "#DisplayKarte":
                DisplayKarte(true, data.Split('|')[0], data.Split('|')[1]);
                break;
            case "#SetGameType":
                SetGameType(data);
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
    /// <summary>
    /// Aktualisiert die Lobby
    /// </summary>
    int ingameSpieler = 0;
    private void UpdateLobby(string data)
    {
        Logging.log(Logging.LogType.Debug, "TabuClient", "UpdateLobby", "LobbyAnzeigen werden aktualisiert: " + data);
        for (int i = 0; i < Playerlist.Length; i++)
        {
            Playerlist[i].SetActive(false);
        }
        string[] elemente = data.Split('|');
        if (elemente.Length < ingameSpieler)
            PlayDisconnectSound();
        ingameSpieler = elemente.Length;
        for (int i = 0; i < elemente.Length; i++)
        {
            Playerlist[i].GetComponentInChildren<TMP_Text>().text = elemente[i];
            Playerlist[i].SetActive(true);
        }
    }

    #region GameLogic
    private void InitGame()
    {
        // Spielbrett
        selectedItem = new TabuItem("", "");
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
        Richtig = GameObject.Find("Spielbrett/Richtig");
        Richtig.SetActive(false);
        Falsch = GameObject.Find("Spielbrett/Falsch");
        Falsch.SetActive(false);
        RundeStarten = GameObject.Find("Spielbrett/RundeStarten");
        RundeStarten.SetActive(false);
        Skip = GameObject.Find("Spielbrett/Skip");
        Skip.SetActive(false);
        started = false;

        teamblauList.Add(Config.PLAYER_NAME);
        foreach (Player item in Config.PLAYERLIST)
            if (item.name.Length > 0)
                teamblauList.Add(item.name);

        TeamTurn = "ROT";
    }
    private void SetGameType(string data)
    {
        TabuSpiel.GameType = data;

        SetTeamPoints("ROT", teamrotPunkte = TabuData.InitTeamPoints("ROT"));
        SetTeamPoints("BLAU", teamblauPunkte = TabuData.InitTeamPoints("BLAU"));
    }
    private void StartTimer(int sec)
    {
        if (TimerCoroutine != null)
            StopCoroutine(TimerCoroutine);
        TimerCoroutine = StartCoroutine(RunTimer(sec, TabuData.TimerDecreasePoints()));
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
                    yield break;
            }
            else
                yield return new WaitForSecondsRealtime(1);
        }
        Timer.SetActive(false);
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
    public void ClientJoinTeam(string team)
    {
        if (!Config.CLIENT_STARTED)
            return;
        ClientUtils.SendToServer("#ClientJoinTeam " + team);
    }
    private int playercount;
    private void JoinTeam(string data)
    {
        teamblauList.Clear();
        teamrotList.Clear();
        string teamrot = data.Split('|')[0];
        string[] temp = new string[0];
        if (teamrot.Length > 0)
            temp = teamrot.Replace("[#]", "|").Split('|');
        for (int i = 1; i < TeamRot.transform.childCount; i++)
        {
            int index = i - 1;
            Transform PlayerObject = TeamRot.transform.GetChild(i);
            PlayerObject.gameObject.SetActive(false);
            if (temp.Length > index)
            {
                PlayerObject.GetChild(1).GetComponent<TMP_Text>().text = temp[index].Split("~")[0];
                teamrotList.Add(temp[index].Split("~")[0]);
                PlayerObject.GetChild(0).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/ProfileIcons/" + temp[index].Split("~")[1]);
                PlayerObject.gameObject.SetActive(true);
            }
        }
        string teamblau = data.Split('|')[1];
        if (teamblau.Length > 0)
            temp = teamblau.Replace("[#]", "|").Split('|');
        for (int i = 1; i < TeamBlau.transform.childCount; i++)
        {
            int index = i - 1;
            Transform PlayerObject = TeamBlau.transform.GetChild(i);
            PlayerObject.gameObject.SetActive(false);
            if (temp.Length > index)
            {
                PlayerObject.GetChild(1).GetComponent<TMP_Text>().text = temp[index].Split("~")[0];
                teamblauList.Add(temp[index].Split("~")[0]);
                PlayerObject.GetChild(0).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/ProfileIcons/" + temp[index].Split("~")[1]);
                PlayerObject.gameObject.SetActive(true);
            }
        }

        if ((teamblauList.Count + teamrotList.Count) < playercount)
            PlayDisconnectSound();
        playercount = teamblauList.Count + teamrotList.Count;

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
    public void SetKreuzClient()
    {
        if (!Config.CLIENT_STARTED)
            return;
        ClientUtils.SendToServer("#ClientKreuz " + selectedItem.geheimwort);
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
    public void RichtigGeraten()
    {
        if (!Config.CLIENT_STARTED)
            return;
        ClientUtils.SendToServer("#ClientRichtigGeraten");
    }
    public void SkipWort()
    {
        if (!Config.CLIENT_STARTED)
            return;
        ClientUtils.SendToServer("#ClientSkipWort");
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
            c = new Color(38f / 255f, 255f / 255f, 1f / 255f, 1f / 255f);
        }
        else if (type.Equals("LOSE"))
        {
            c = new Color(128f / 255f, 1f / 255f, 1f / 255f, 1f / 255f);
        }
        BackgroundColor.GetComponent<Image>().color = c;
        BackgroundColor.SetActive(true);
        for (int i = 0; i < 50; i++)
        {
            c.a = (float)(i / 255f);
            BackgroundColor.GetComponent<Image>().color = c;
            yield return new WaitForSecondsRealtime(0.001f);
        }
        yield return new WaitForSecondsRealtime(0.001f);
        for (int i = 50; i > 18; i -= 2)
        {
            c.a = (float)(i / 255f);
            BackgroundColor.GetComponent<Image>().color = c;
            yield return new WaitForSecondsRealtime(0.001f);
        }
        yield return new WaitForSecondsRealtime(0.001f);
        BackgroundColor.SetActive(false);
        yield break;
    }
    private IEnumerator SkipWortCoro()
    {
        yield return new WaitForSeconds(5);
        if (erklaerer.Equals(Config.PLAYER_NAME))
            Skip.SetActive(true);
        yield break;
    }
    private void RundeEnde(string data)
    {
        TeamTurn = data.Split('|')[0];
        string indicator = data.Split('|')[1];
        TabuSpiel.GameType = data.Split('|')[2];
        teamrotPunkte = Int32.Parse(data.Split('|')[3]);
        int teamrot = teamrotPunkte;
        SetTeamPoints("ROT", teamrotPunkte);
        teamblauPunkte = Int32.Parse(data.Split('|')[4]);
        int teamblau = teamblauPunkte;
        SetTeamPoints("BLAU", teamblauPunkte);
        erklaerer = data.Split("|")[5];
        selectedItem.geheimwort = data.Split('|')[8];
        selectedItem.tabuworte = data.Split('|')[9];
        wzahlenstring = data.Split('|')[7];

        if (TabuSpiel.GameType.Equals("1 Wort"))
        {
            // Falsch gedrückt
            if (indicator == "-2")
            {
                Kreuze.transform.GetChild(0).gameObject.SetActive(true);
                FalschGeraten.Play();
                StartCoroutine(AnimateBackground("LOSE"));
            }
            // Zeit vorbei
            else if (indicator == "-1")
            {
                FalschGeraten.Play();
                StartCoroutine(AnimateBackground("LOSE"));
            }
            // Richtig gedrückt
            else if (indicator == "1")
            {
                Erraten.Play();
                StartCoroutine(AnimateBackground("WIN"));
            }
            else
                Logging.log(Logging.LogType.Error, "TabuServer", "RundeEnde", "Fehler: " + indicator + " " + TeamTurn);

            EndTurn(teamrotPunkte, teamblauPunkte);
        }
        else if (TabuSpiel.GameType.Equals("Normal"))
        {
            // Skip Wort
            if (indicator == "-3")
            {
                if (erklaerer.Equals(Config.PLAYER_NAME))
                {
                    Skip.SetActive(false);
                    if (SkipCoroutine != null)
                        StopCoroutine(SkipCoroutine);
                    SkipCoroutine = StartCoroutine(SkipWortCoro());
                }
            }
            // Falsch gedrückt
            else if (indicator == "-2")
            {
                FalschGeraten.Play();
                StartCoroutine(AnimateBackground("LOSE"));
                if (HideRichtigFalschSecCoroutine != null)
                    StopCoroutine(HideRichtigFalschSecCoroutine);
                HideRichtigFalschSecCoroutine = StartCoroutine(HideRichtigFalschSec());
            }
            // Zeit vorbei
            else if (indicator == "-1")
            {
                EndTurn(teamrotPunkte, teamblauPunkte);
            }
            // Richtig gedrückt
            else if (indicator == "1")
            {
                Erraten.Play();
                StartCoroutine(AnimateBackground("WIN"));
                if (HideRichtigFalschSecCoroutine != null)
                    StopCoroutine(HideRichtigFalschSecCoroutine);
                HideRichtigFalschSecCoroutine = StartCoroutine(HideRichtigFalschSec());
            }
            else
                Logging.log(Logging.LogType.Error, "TabuServer", "RundeEnde", "Fehler: " + indicator + " " + TeamTurn);
        }
        else if (TabuSpiel.GameType.Equals("Timer"))
        {
            // Skip Wort
            if (indicator == "-3")
            {
                if (erklaerer.Equals(Config.PLAYER_NAME))
                {
                    Skip.SetActive(false);
                    if (SkipCoroutine != null)
                        StopCoroutine(SkipCoroutine);
                    SkipCoroutine = StartCoroutine(SkipWortCoro());
                }
                if (teamblauPunkte <= 0 || teamrotPunkte <= 0)
                    EndTurn(teamrotPunkte, teamblauPunkte);
            }
            // Falsch gedrückt
            else if (indicator == "-2")
            {
                FalschGeraten.Play();
                StartCoroutine(AnimateBackground("LOSE"));
                if (HideRichtigFalschSecCoroutine != null)
                    StopCoroutine(HideRichtigFalschSecCoroutine);
                HideRichtigFalschSecCoroutine = StartCoroutine(HideRichtigFalschSec());
                if (teamblauPunkte <= 0 || teamrotPunkte <= 0)
                    EndTurn(teamrotPunkte, teamblauPunkte);
            }
            // Zeit vorbei
            else if (indicator == "-1")
            {
                EndTurn(teamrotPunkte, teamblauPunkte);
            }
            // Richtig gedrückt
            else if (indicator == "1")
            {
                Erraten.Play();
                StartCoroutine(AnimateBackground("WIN"));
                if (HideRichtigFalschSecCoroutine != null)
                    StopCoroutine(HideRichtigFalschSecCoroutine);
                HideRichtigFalschSecCoroutine = StartCoroutine(HideRichtigFalschSec());
            }
            else
                Logging.log(Logging.LogType.Error, "TabuServer", "RundeEnde", "Fehler: " + indicator + " " + TeamTurn);
        }
        else if (TabuSpiel.GameType.Equals("Battle Royale"))
        {
            // Skip Wort
            if (indicator == "-3")
            {
                if (erklaerer.Equals(Config.PLAYER_NAME))
                {
                    Skip.SetActive(false);
                    if (SkipCoroutine != null)
                        StopCoroutine(SkipCoroutine);
                    SkipCoroutine = StartCoroutine(SkipWortCoro());
                }

                if (teamblauPunkte <= 0 || teamrotPunkte <= 0)
                    EndTurn(teamrotPunkte, teamblauPunkte);
            }
            // Falsch gedrückt
            else if (indicator == "-2")
            {
                FalschGeraten.Play();
                StartCoroutine(AnimateBackground("LOSE"));
                if (HideRichtigFalschSecCoroutine != null)
                    StopCoroutine(HideRichtigFalschSecCoroutine);
                HideRichtigFalschSecCoroutine = StartCoroutine(HideRichtigFalschSec());

                if (teamblauPunkte <= 0 || teamrotPunkte <= 0)
                {
                    EndTurn(teamrotPunkte, teamblauPunkte);
                    return;
                }

            }
            // Zeit vorbei
            else if (indicator == "-1")
            {
                EndTurn(teamrotPunkte, teamblauPunkte);
            }
            // Richtig gedrückt
            else if (indicator == "1")
            {
                Erraten.Play();
                StartCoroutine(AnimateBackground("WIN"));
                if (HideRichtigFalschSecCoroutine != null)
                    StopCoroutine(HideRichtigFalschSecCoroutine);
                HideRichtigFalschSecCoroutine = StartCoroutine(HideRichtigFalschSec());

                // Switch Team
                Skip.SetActive(false);
                if (erklaerer.Equals(Config.PLAYER_NAME))
                    Skip.SetActive(true);

                MarkErklaerer();
            }
            else
                Logging.log(Logging.LogType.Error, "TabuServer", "RundeEnde", "Fehler: " + indicator + " " + TeamTurn);
        }
        else
            Logging.log(Logging.LogType.Error, "TabuServer", "RundeEnde", "Unbekannter Typ: " + TabuSpiel.GameType);

        // Neue Karte
        if (started)
        {
            if (erklaerer.Equals(Config.PLAYER_NAME))
                DisplayKarte(bool.Parse(data.Split('|')[6]), selectedItem.geheimwort, TabuSpiel.getStringArrayToString(selectedItem.tabuworte, wzahlenstring));
            else if (teamrotList.Contains(Config.PLAYER_NAME) && TeamTurn.Equals("BLAU"))
                DisplayKarte(bool.Parse(data.Split('|')[6]), selectedItem.geheimwort, TabuSpiel.getStringArrayToString(selectedItem.tabuworte, wzahlenstring));
            else if (teamblauList.Contains(Config.PLAYER_NAME) && TeamTurn.Equals("ROT"))
                DisplayKarte(bool.Parse(data.Split('|')[6]), selectedItem.geheimwort, TabuSpiel.getStringArrayToString(selectedItem.tabuworte, wzahlenstring));
            else
                DisplayKarte(bool.Parse(data.Split('|')[6]), erklaerer, "");
        }
    }
    private void EndTurn(int rotpoints, int blaupoints)
    {
        if (!started)
            return;
        started = false;
        DisplayKarte(true, selectedItem.geheimwort, TabuSpiel.getStringArrayToString(selectedItem.tabuworte, wzahlenstring));
        StopCoroutine(TimerCoroutine);
        Timer.SetActive(false);
        Skip.SetActive(false);
        Richtig.SetActive(false);
        Falsch.SetActive(false);
        JoinTeamBlau.SetActive(true);
        JoinTeamRot.SetActive(true);
        MarkErklaerer();

        SetTeamPoints("ROT", rotpoints);
        SetTeamPoints("BLAU", blaupoints);

        if (TeamTurn.Equals("ROT") && teamrotList.Contains(Config.PLAYER_NAME))
            RundeStarten.SetActive(true);
        else if (TeamTurn.Equals("BLAU") && teamblauList.Contains(Config.PLAYER_NAME))
            RundeStarten.SetActive(true);

        if (TabuSpiel.GameType.Equals("Battle Royale"))
            RundeStarten.SetActive(false);
    }
    private void KreuzAn(string data)
    {
        int index = Int32.Parse(data);
        Kreuze.transform.GetChild(index).gameObject.SetActive(true);
        if (index > 0)
            DisplayX.Play();
    }
    public void ClientStartRunde()
    {
        if (!Config.CLIENT_STARTED)
            return;
        ClientUtils.SendToServer("#ClientStartRunde");
    }
    private void StartRunde(string data)
    {
        erklaerer = data.Split('|')[0];
        TeamTurn = data.Split('|')[1];
        TabuSpiel.GameType = data.Split('|')[2];
        timerseconds = Int32.Parse(data.Split('|')[3]);
        wzahlenstring = data.Split('|')[4];
        selectedItem.geheimwort = data.Split('|')[5];
        selectedItem.tabuworte = data.Split('|')[6];
        DisplayKarte(true, selectedItem.geheimwort, TabuSpiel.getStringArrayToString(selectedItem.tabuworte, wzahlenstring));
        started = true;

        Skip.SetActive(false);
        if (TabuSpiel.GameType.Equals("Normal") && erklaerer.Equals(Config.PLAYER_NAME))
            Skip.SetActive(true);
        else if (TabuSpiel.GameType.Equals("Timer") && erklaerer.Equals(Config.PLAYER_NAME))
            Skip.SetActive(true);
        else if (TabuSpiel.GameType.Equals("Battle Royale") && erklaerer.Equals(Config.PLAYER_NAME))
            Skip.SetActive(true);

        RundeStarten.SetActive(false);
        JoinTeamRot.SetActive(false);
        JoinTeamBlau.SetActive(false);
        for (int i = 0; i < Kreuze.transform.childCount; i++)
            Kreuze.transform.GetChild(i).gameObject.SetActive(false);
        Kreuze.SetActive(true);
        if (erklaerer == Config.PLAYER_NAME)
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
        if (erklaerer == Config.PLAYER_NAME)
        {
            DisplayKarte(true, selectedItem.geheimwort, TabuSpiel.getStringArrayToString(selectedItem.tabuworte, wzahlenstring));
        }
        else
        {
            Karte.transform.GetChild(0).GetChild(1).GetComponent<TMP_Text>().text = erklaerer;
            if (TeamTurn.Equals("BLAU") && teamrotList.Contains(Config.PLAYER_NAME))
            {
                DisplayKarte(true, selectedItem.geheimwort, TabuSpiel.getStringArrayToString(selectedItem.tabuworte, wzahlenstring));
            }
            else if (TeamTurn.Equals("ROT") && teamblauList.Contains(Config.PLAYER_NAME))
            {
                DisplayKarte(true, selectedItem.geheimwort, TabuSpiel.getStringArrayToString(selectedItem.tabuworte, wzahlenstring));
            }
            else if (TeamTurn.Equals("BLAU") && teamblauList.Contains(Config.PLAYER_NAME))
            {
                DisplayKarte(true, erklaerer, "");
                SpielerIstDran.Play();
            }
            else if (TeamTurn.Equals("ROT") && teamrotList.Contains(Config.PLAYER_NAME))
            {
                DisplayKarte(true, erklaerer, "");
                SpielerIstDran.Play();
            }
            else
            {
                Karte.transform.GetChild(0).GetChild(3).GetComponent<TMP_Text>().text = "";
            }
            Karte.SetActive(true);
        }
        MarkErklaerer();

        StartTimer(timerseconds);
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
}