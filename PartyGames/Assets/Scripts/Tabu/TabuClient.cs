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
    private GameObject RichtigFalsch1;
    private GameObject RichtigFalsch2;
    private GameObject RundeStarten;

    private List<string> teamrotList;
    private List<string> teamblauList;
    private string TeamTurn;
    private bool started;

    private Coroutine TimerCoroutine;
    private TabuGamePacks NormalPack;
    private TabuItem selectedItem;

    void OnEnable()
    {
        if (!Config.CLIENT_STARTED)
            return;
        SendToServer("#JoinTabu");
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
        SendToServer("#ClientFocusChange " + focus);
    }

    private void OnApplicationQuit()
    {
        Logging.log(Logging.LogType.Normal, "TabuClient", "OnApplicationQuit", "Client wird geschlossen.");
        SendToServer("#ClientClosed");
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
            SendToServer("#TestConnection");
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
    /// Sendet eine Nachricht an den Server.
    /// </summary>
    /// <param name="data">Nachricht</param>
    private void SendToServer(string data)
    {
        if (!Config.CLIENT_STARTED)
            return;

        try
        {
            NetworkStream stream = Config.CLIENT_TCP.GetStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.WriteLine(data);
            writer.Flush();
        }
        catch (Exception e)
        {
            Logging.log(Logging.LogType.Warning, "TabuClient", "SendToServer", "Nachricht an Server konnte nicht gesendet werden.", e);
            Config.HAUPTMENUE_FEHLERMELDUNG = "Verbindung zum Server wurde verloren.";
            CloseSocket();
            SceneManager.LoadSceneAsync("StartUp");
        }
    }
    /// <summary>
    /// Einkommende Nachrichten die vom Sever
    /// </summary>
    /// <param name="data">Nachricht</param>
    private void OnIncomingData(string data)
    {
        string cmd;
        if (data.Contains(" "))
            cmd = data.Split(' ')[0];
        else
            cmd = data;
        data = data.Replace(cmd + " ", "");

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
        RichtigFalsch1 = GameObject.Find("Spielbrett/RichtigFalsch (0)");
        RichtigFalsch1.SetActive(false);
        RichtigFalsch2 = GameObject.Find("Spielbrett/RichtigFalsch (1)");
        RichtigFalsch2.SetActive(false);
        RundeStarten = GameObject.Find("Spielbrett/RundeStarten");
        RundeStarten.SetActive(false);
        started = false;

        TeamTurn = "ROT";
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
        SendToServer("#ClientJoinTeam " +team);
    }
    private int playercount;
    private void JoinTeam(string data)
    {
        teamblauList.Clear();
        teamrotList.Clear();
        string teamrot = data.Split('|')[0];
        string[] temp = new string[0];
        if (teamrot.Length > 0)
        {
            temp = teamrot.Replace("[#]", "|").Split('|');
        }
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
        {
            temp = teamblau.Replace("[#]", "|").Split('|');
        }
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
        RichtigFalsch1.SetActive(false);
        RichtigFalsch2.SetActive(false);

        if (RichtigFalsch1.GetComponentInChildren<TMP_Text>().text.Equals("Erraten"))
            SendToServer("#ClientRichtigGeraten");
        if (RichtigFalsch1.GetComponentInChildren<TMP_Text>().text.Equals("Fehler"))
            SendToServer("#ClientKreuz");
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
        Karte.transform.GetChild(0).GetChild(3).GetComponent<TMP_Text>().text = verboteneWorte.Replace("\\n", "\n") + "\n ";
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
    private void RundeEnde(string data)
    {
        StopCoroutine(TimerCoroutine);
        Timer.SetActive(false);

        TeamTurn = data.Split('|')[0];
        string points = data.Split('|')[1];
        string name = data.Split('|')[2];
        DisplayKarte(true, selectedItem.geheimwort, selectedItem.verboteneWorte);

        if (points == "+1" || points == "1")
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
        else if (points == "-1")
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
    private void KreuzAn(string data)
    {
        int index = Int32.Parse(data);
        Kreuze.transform.GetChild(index).gameObject.SetActive(true);
        if (index < 2)
            DisplayX.Play();
    }
    public void ClientStartRunde()
    {
        if (!Config.CLIENT_STARTED)
            return;
        SendToServer("#ClientStartRunde");
    }
    private void StartRunde(string data)
    {
        string name = data.Split('|')[0];
        TeamTurn = data.Split('|')[1];
        selectedItem = new TabuItem(data.Split('|')[2], data.Split('|')[3]);
        started = true;

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