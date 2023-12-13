using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SloxikonServer : MonoBehaviour
{
    GameObject BuzzerAnzeige;
    bool buzzerIsOn = false;
    int aktuellesThema = 0;
    bool liveupdate = false;
    string[] playeranswers;

    GameObject Timer;
    TMP_InputField TimerSekunden;
    Coroutine TimerCoroutine;

    GameObject AustabbenAnzeigen;
    GameObject TextEingabeAnzeige;
    GameObject TextAntwortenAnzeige;
    GameObject PlayerLiveUpdateAnzeige;
    GameObject[,] SpielerAnzeige;
    bool[] PlayerConnected;
    int PunkteProRichtige = 3;
    int PunkteProFalsche = 1;

    GameObject Thema;
    GameObject[] Antworten;

    [SerializeField] AudioSource BuzzerSound;
    [SerializeField] AudioSource RichtigeAntwortSound;
    [SerializeField] AudioSource FalscheAntwortSound;
    [SerializeField] AudioSource Moeoep;
    [SerializeField] AudioSource Beeep;
    [SerializeField] AudioSource DisconnectSound;

    void OnEnable()
    {
        PlayerConnected = new bool[Config.SERVER_MAX_CONNECTIONS];
        if (!Config.SERVER_STARTED)
            return;
        InitAnzeigen();
        InitSloxikon();
    }

    void Update()
    {
        #region Server
        if (!Config.SERVER_STARTED)
        {
            SceneManager.LoadScene("Startup");
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
                    //StreamReader reader = new StreamReader(stream, true);
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
        Logging.log(Logging.LogType.Normal, "Server", "OnApplicationQuit", "Server wird geschlossen");
        Config.SERVER_TCP.Server.Close();
    }

    #region Server Stuff
    #region Kommunikation
    /// <summary>
    /// Einkommende Nachrichten die von Spielern an den Server gesendet werden.
    /// </summary>
    /// <param name="spieler"></param>
    /// <param name="data"></param>
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
    /// <param name="player"></param>
    /// <param name="data"></param>
    /// <param name="cmd"></param>
    private void Commands(Player player, string data, string cmd)
    {
        // Zeigt alle einkommenden Nachrichten an
        Logging.log(Logging.LogType.Debug, "SloxikonServer", "Commands", "Eingehende Nachricht: " + cmd + " -> " + data);
        // Sucht nach Command
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "SloxikonServer", "Commands", "Unkown Command: " + cmd + " -> " + data);
                break;

            case "#ClientClosed":
                ServerUtils.ClientClosed(player);
                UpdateSpielerBroadcast();
                PlayDisconnectSound();
                break;
            case "#TestConnection":
                break;
            case "#ClientFocusChange":
                ClientFocusChange(player, data);
                break;

            case "#JoinSloxikon":
                PlayerConnected[player.id - 1] = true;
                UpdateSpielerBroadcast();
                break;
            case "#SpielerBuzzered":
                SpielerBuzzered(player);
                break;
            case "#SpielerAntwortEingabe":
                SpielerAntwortEingabe(player, data);
                break;
        }
    }
    #endregion
    /// <summary>
    /// Spiel Verlassen & Zurück in die Lobby laden
    /// </summary>
    public void SpielVerlassenButton()
    {
        ServerUtils.LoadKronen(Config.PLAYERLIST);
        ServerUtils.BroadcastImmediate("#ZurueckInsHauptmenue");
    }
    /// <summary>
    /// Sendet aktualisierte Spielerinfos an alle Spieler
    /// </summary>
    private void UpdateSpielerBroadcast()
    {
        ServerUtils.BroadcastImmediate(UpdateSpieler());
    }
    /// <summary>
    /// Aktualisiert die Spieler Anzeige Informationen & gibt diese als Text zurück
    /// </summary>
    /// <returns></returns>
    private string UpdateSpieler()
    {
        string msg = "#UpdateSpieler [ID]0[ID][PUNKTE]" + Config.SERVER_PLAYER.points + "[PUNKTE]";
        int connectedplayer = 0;
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            Player p = Config.PLAYERLIST[i];
            msg += "[TRENNER][ID]" + p.id + "[ID][PUNKTE]" + p.points + "[PUNKTE][ONLINE]"+p.isConnected+"[ONLINE]";
            if (p.isConnected && PlayerConnected[i])
            {
                connectedplayer++;
                SpielerAnzeige[i, 0].SetActive(true);
                SpielerAnzeige[i, 2].GetComponent<Image>().sprite = p.icon2.icon;
                SpielerAnzeige[i, 4].GetComponent<TMP_Text>().text = p.name;
                SpielerAnzeige[i, 5].GetComponent<TMP_Text>().text = p.points+"";
            }
            else
                SpielerAnzeige[i, 0].SetActive(false);

        }
        return msg;
    }
    /// <summary>
    /// Spielt den Disconnect Sound ab
    /// </summary>
    private void PlayDisconnectSound()
    {
        DisconnectSound.Play();
    }
    /// <summary>
    /// Initialisiert die Anzeigen zu beginn
    /// </summary>
    private void InitAnzeigen()
    {
        Logging.log(Logging.LogType.Debug, "SloxikonServer", "InitAnzeigen", "Initialisiert die Anzeigen");
        // Buzzer Deaktivieren
        GameObject.Find("ServerSide/BuzzerAktivierenToggle").GetComponent<Toggle>().isOn = false;
        BuzzerAnzeige = GameObject.Find("ServerSide/BuzzerIstAktiviert");
        BuzzerAnzeige.SetActive(false);
        buzzerIsOn = false;
        // Austabben wird gezeigt
        GameObject.Find("ServerSide/AusgetabtSpielernZeigenToggle").GetComponent<Toggle>().isOn = false;
        AustabbenAnzeigen = GameObject.Find("ServerSide/AusgetabtWirdSpielernGezeigen");
        AustabbenAnzeigen.SetActive(false);
        // Spieler Anzeige
        SpielerAnzeige = new GameObject[Config.SERVER_MAX_CONNECTIONS, 7]; // Anzahl benötigter Elemente
        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
        {
            PlayerConnected[i] = false;
            SpielerAnzeige[i, 0] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")"); // Spieler Anzeige
            SpielerAnzeige[i, 1] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/BuzzerPressed"); // BuzzerPressed Umrandung
            SpielerAnzeige[i, 2] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Icon"); // Spieler Icon
            SpielerAnzeige[i, 3] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Ausgetabt"); // Ausgetabt Einblednung
            SpielerAnzeige[i, 4] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Infobar/Name"); // Spieler Name
            SpielerAnzeige[i, 5] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Infobar/Punkte"); // Spieler Punkte
            SpielerAnzeige[i, 6] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/SpielerAntwort"); // Spieler Antwort

            SpielerAnzeige[i, 0].SetActive(false); // Spieler Anzeige
            SpielerAnzeige[i, 1].SetActive(false); // BuzzerPressed Umrandung
            SpielerAnzeige[i, 3].SetActive(false); // Ausgetabt Einblendung
            SpielerAnzeige[i, 6].SetActive(true); // Spieler Antwort
        }
    }
    #region Sloxikon Anzeige
    /// <summary>
    /// Initialisiert die Anzeigen des Quizzes
    /// </summary>
    private void InitSloxikon()
    {
        Logging.log(Logging.LogType.Debug, "SloxikonServer", "InitSloxikon", "Initialisiert Sloxikonanzeigen");
        GameObject.Find("Sloxikon/Titel").GetComponent<TMP_Text>().text = "";//Config.SLOXIKON_SPIEL.getSelected().getTitel();
        GameObject.Find("Server/ThemenVorschau").GetComponent<TMP_Text>().text = Config.SLOXIKON_SPIEL.getSelected().getThemenListe();
        playeranswers = new string[Config.PLAYERLIST.Length];
        aktuellesThema = 0;
        PlayerLiveUpdateAnzeige = GameObject.Find("Server/PlayerLiveAnswerUpdateAnzeige");
        PlayerLiveUpdateAnzeige.SetActive(false);
        GameObject.Find("Server/Element").GetComponent<TMP_Text>().text = Config.SLOXIKON_SPIEL.getSelected().getThemen()[aktuellesThema];
        Thema = GameObject.Find("Sloxikon/Thema");
        Thema.SetActive(false);
        Antworten = new GameObject[9];
        for (int i = 0; i < 9; i++)
        {
            Antworten[i] = GameObject.Find("Sloxikon/Grid/Answer ("+(i+1)+")");
            Antworten[i].SetActive(false);
            Antworten[i].transform.GetChild(1).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/ProfileIcons/empty");
            Antworten[i].transform.GetChild(2).GetComponentInChildren<TMP_InputField>().text = "";
            GameObject grid = Antworten[i].transform.GetChild(3).gameObject;

            for (int j = 0; j < 8; j++)
            {
                GameObject child = grid.transform.GetChild(j).gameObject;
                if (Config.SERVER_MAX_CONNECTIONS > j && Config.PLAYERLIST[j].isConnected)
                {
                    child.GetComponent<Image>().sprite = Config.PLAYERLIST[j].icon2.icon;
                    child.GetComponent<Image>().color = new Color(255, 255, 255, 0.5f);
                    child.gameObject.SetActive(true);
                }
                else
                {
                    child.GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/ProfileIcons/empty");
                    child.GetComponent<Image>().color = new Color(255, 255, 255, 1);
                    child.gameObject.SetActive(false);
                }
            }
        }
        Timer = GameObject.Find("Sloxikon/TimerSekundenAnzeige");
        Timer.SetActive(false);
        TimerSekunden = GameObject.Find("Sloxikon/Server/TimerDauer").GetComponent<TMP_InputField>();

        DisplayRandomSequence();
    }
    /// <summary>
    /// Navigiert durch die Themen
    /// </summary>
    /// <param name="vor">+-1 (vor/zurück)</param>
    public void NavigateThrough(int vor)
    {
        if ((aktuellesThema + vor) >= Config.SLOXIKON_SPIEL.getSelected().getThemen().Count)
        {
            aktuellesThema = Config.SLOXIKON_SPIEL.getSelected().getThemen().Count - 1;
            return;
        }
        if ((aktuellesThema + vor) < 0)
        {
            aktuellesThema = 0;
            return;
        }
        aktuellesThema = aktuellesThema + vor;
        GameObject.Find("Server/Element").GetComponent<TMP_Text>().text = Config.SLOXIKON_SPIEL.getSelected().getThemen()[aktuellesThema];
    }
    public void ShowTitel()
    {
        Logging.log(Logging.LogType.Debug, "SloxikonServer", "ShowTitel", "Zeige Titel des Games an. " + Config.SLOXIKON_SPIEL.getSelected().getTitel());
        ServerUtils.BroadcastImmediate("#SloxikonShowTitel " + Config.SLOXIKON_SPIEL.getSelected().getTitel());
        GameObject.Find("Sloxikon/Titel").GetComponent<TMP_Text>().text = Config.SLOXIKON_SPIEL.getSelected().getTitel();
    }
    /// <summary>
    /// Blendet alle Anzeigen aus
    /// </summary>
    public void HideAll()
    {
        Logging.log(Logging.LogType.Debug, "SloxikonServer", "HideAll", "Blendet Anzeigen aus");
        ServerUtils.BroadcastImmediate("#SloxikonHideAll");
        Thema.SetActive(false);
        // Falls jemand disconnected alle Anzeigen ausblenden
        for (int i = 0; i < Config.PLAYERLIST.Length + 1; i++)
        {
            Antworten[i].SetActive(false);
        }
    }
    /// <summary>
    /// Blendet ausgewähltes Thema ein
    /// </summary>
    public void ShowThema()
    {
        Logging.log(Logging.LogType.Debug, "SloxikonServer", "ShowThema", "Zeige neues Thema. " + Config.SLOXIKON_SPIEL.getSelected().getThemen()[aktuellesThema]);
        // Zuweisung wer welche antwort gibt
        List<int> playerList = new List<int>();
        playerList.Add(0);
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            if (Config.PLAYERLIST[i].isConnected)
                playerList.Add(Config.PLAYERLIST[i].id);
        }
        List<int> positionList = new List<int>();
        positionList.AddRange(playerList);

        ServerUtils.BroadcastImmediate("#SloxikonShowThema " + Config.SLOXIKON_SPIEL.getSelected().getThemen()[aktuellesThema] + "|" + playerList.Count);
        Thema.SetActive(true);
        Thema.GetComponentInChildren<TMP_InputField>().text = Config.SLOXIKON_SPIEL.getSelected().getThemen()[aktuellesThema];

        // Falls jemand disconnected alle Anzeigen ausblenden
        for (int i = 0; i < Config.PLAYERLIST.Length + 1; i++)
        {
            Antworten[i].SetActive(false);
        }

        while (playerList.Count > 0)
        {
            int pid = playerList[UnityEngine.Random.Range(0, playerList.Count)];
            playerList.Remove(pid);
            int pos = positionList[UnityEngine.Random.Range(0, playerList.Count)];
            positionList.Remove(pos);
            
            #region Server Einblenden
            if (pid == 0)
            {
                // Server ShowTXT & ShowOwner aktualisieren
                Antworten[pos].transform.GetChild(0).GetChild(0).GetComponent<Button>().interactable = true; // ShowTXT
                Antworten[pos].transform.GetChild(0).GetChild(1).GetComponent<Button>().interactable = true; // ShowOwner
                // Source
                Antworten[pos].transform.GetChild(1).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/ProfileIcons/empty");
                Antworten[pos].transform.GetChild(1).GetComponentInChildren<TMP_Text>().text = "0";
                Antworten[pos].transform.GetChild(1).GetComponent<Image>().color = new Color(255, 255, 255, 0.5f);
                Antworten[pos].SetActive(true);
                Antworten[pos].transform.GetChild(2).GetComponentInChildren<TMP_InputField>().text = Config.SLOXIKON_SPIEL.getSelected().getAntwort()[aktuellesThema];
            }
            #endregion
            #region Clients Einbinden
            else
            {
                // Server ShowTXT & ShowOwner aktualisieren
                Antworten[pos].transform.GetChild(0).GetChild(0).GetComponent<Button>().interactable = true; // ShowTXT
                Antworten[pos].transform.GetChild(0).GetChild(1).GetComponent<Button>().interactable = true; // ShowOwner
                // Source
                Antworten[pos].transform.GetChild(1).GetComponent<Image>().sprite = Config.PLAYERLIST[Player.getPosInLists(pid)].icon2.icon;
                Antworten[pos].transform.GetChild(1).GetComponentInChildren<TMP_Text>().text = pid + "";
                Antworten[pos].transform.GetChild(1).GetComponent<Image>().color = new Color(255, 255, 255, 0.5f);
                Antworten[pos].SetActive(true);
                Antworten[pos].transform.GetChild(2).GetComponentInChildren<TMP_InputField>().text = "";
            }
            #endregion
        }
        // Blendet spielerselect ein
        for (int j = 0; j < Config.PLAYERLIST.Length; j++)
        {
            if (Config.PLAYERLIST[j].isConnected)
            {
                for (int i = 0; i < Config.PLAYERLIST.Length; i++)
                {
                    Antworten[i].transform.GetChild(3).GetChild(j).GetComponent<Button>().image.sprite = Config.PLAYERLIST[j].icon2.icon;
                    Antworten[i].transform.GetChild(3).GetChild(j).GetComponent<Button>().image.color = new Color(255, 255, 255, 0.5f);
                    Antworten[i].transform.GetChild(3).GetChild(j).GetComponent<Button>().interactable = true;
                    Antworten[i].transform.GetChild(3).GetChild(j).gameObject.SetActive(true);
                }
            }
        }
        DisplayRandomSequence();
    }
    /// <summary>
    /// Legt eine zufällige Reihnfolge der Spieler fest
    /// </summary>
    private void DisplayRandomSequence()
    {
        GameObject parent = GameObject.Find("Sloxikon/Server/ReihnfolgenAuswahl");
        for (int i = 0; i < parent.transform.childCount; i++)
            parent.transform.GetChild(i).gameObject.SetActive(false);

        List<int> ids = new List<int>();
        foreach (Player p in Config.PLAYERLIST)
        {
            if (p.isConnected && p.name.Length > 0)
                ids.Add(p.id);
        }

        int length = ids.Count;
        for (int i = 0; i < length; i++)
        {
            int random = ids[UnityEngine.Random.Range(0, ids.Count)];
            ids.Remove(random);
            parent.transform.GetChild(i).GetComponent<Button>().image.sprite = Config.PLAYERLIST[Player.getPosInLists(random)].icon2.icon;
            parent.transform.GetChild(i).gameObject.SetActive(true);
        }
    }
    /// <summary>
    /// Blendet den aktuellen Spieler an, der dran ist
    /// </summary>
    public void SelectPlayersTurn(GameObject go)
    {
        Sprite sprite = go.GetComponent<Button>().image.sprite;

        foreach (Player p in Config.PLAYERLIST)
        {
            if (sprite == p.icon2.icon)
            {
                ServerUtils.BroadcastImmediate("#SpielersTurn " + p.id);
                for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
                    SpielerAnzeige[i, 1].SetActive(false);
                SpielerAnzeige[(p.id - 1), 1].SetActive(true);
                break;
            }
        }
    }
    /// <summary>
    /// Blendet alle Antwortmöglichkeiten ein
    /// </summary>
    public void ShowAllAntworten()
    {
        Logging.log(Logging.LogType.Debug, "SloxikonServer", "ShowAllAntworten", "Zeige alle Antwortmöglichkeiten an.");
        // für den Server vorher schon anzeigen
        int anz = 0;
        for (int i = 0; i < 9; i++)
        {
            if (Antworten[i].activeInHierarchy)
                anz++;
        }

        string msg = "";
        for (int i = 0; i < 9; i++)
        {
            msg += "[" + i + "] " + Antworten[i].transform.GetChild(2).GetComponentInChildren<TMP_InputField>().text + "[" + i + "]";
            // Blendet ShowTXT button aus
            Antworten[i].transform.GetChild(0).GetChild(0).GetComponent<Button>().interactable = false;
        }
        ServerUtils.BroadcastImmediate("#SloxikonShowAllAntworten " + anz + "|" + msg);
    }
    /// <summary>
    /// Blendet bestimmte Antwort ein
    /// </summary>
    /// <param name="antwortindex">1-9</param>
    public void ShowAntwort(int antwortindex)
    {
        Logging.log(Logging.LogType.Debug, "SloxikonServer", "ShowAntwort", "Zeige Antwortmöglichkeit " + antwortindex + " an.");
        antwortindex = antwortindex - 1;
        string msg = antwortindex + "| " + Antworten[antwortindex].transform.GetChild(2).GetComponentInChildren<TMP_InputField>().text;
        // Blendet ShowTXT button aus
        Antworten[antwortindex].transform.GetChild(0).GetChild(0).GetComponent<Button>().interactable = false;
        ServerUtils.BroadcastImmediate("#SloxikonShowAntwort " + msg);       
    }
    /// <summary>
    /// Blendet den Autor der Antwortmöglichkeit ein
    /// </summary>
    /// <param name="ownerindex">1-9</param>
    public void ShowOwner(int ownerindex)
    {
        Logging.log(Logging.LogType.Debug, "SloxikonServer", "ShowOwner", "Zeige den Autor der Antwortmöglichkeit " + ownerindex + " an.");
        ownerindex = ownerindex - 1;
        Antworten[ownerindex].transform.GetChild(1).GetComponent<Image>().color = new Color(255, 255, 255, 1f);
        // Blendet ShowTXT button aus
        Antworten[ownerindex].transform.GetChild(0).GetChild(1).GetComponent<Button>().interactable = false;
        ServerUtils.BroadcastImmediate("#SloxikonShowOwner " + ownerindex + "|" + Antworten[ownerindex].transform.GetChild(1).GetComponent<Image>().sprite.name);
    }
    /// <summary>
    /// Wählt für Spieler eine Antwortmöglichkeit aus
    /// </summary>
    /// <param name="Player">Spieler</param>
    public void PlayerSelectAnswer(GameObject Player)
    {
        Logging.log(Logging.LogType.Debug, "SloxikonServer", "PlayerSelectAnswer", "Spieler " + Player.name + " wählt Antwortmöglichkeit " + Player.transform.parent.parent.name);
        int pid = Int32.Parse(Player.name.Replace("P (", "").Replace(")", "")) - 1;
        int answer = Int32.Parse(Player.transform.parent.parent.name.Replace("Answer (", "").Replace(")", "")) - 1;

        ServerUtils.BroadcastImmediate("#SloxikonPlayerSelectedAnswer " + answer + "|" + pid);

        for (int i = 0; i < Config.PLAYERLIST.Length + 1; i++)
        {
            Antworten[i].transform.GetChild(3).GetChild(pid).GetComponent<Button>().image.color = new Color(255, 255, 255, 0.50f);
            Antworten[i].transform.GetChild(3).GetChild(pid).GetComponent<Button>().interactable = true;
        }
        Antworten[answer].transform.GetChild(3).GetChild(pid).GetComponent<Button>().image.color = new Color(255, 255, 255, 1f);
        Antworten[answer].transform.GetChild(3).GetChild(pid).GetComponent<Button>().interactable = false;
    }
    /// <summary>
    /// Toggelt die live Aktualisierung 
    /// </summary>
    /// <param name="toggle">Toggle</param>
    public void ToggleLivePlayerAnswers(Toggle toggle)
    {
        liveupdate = toggle.isOn;
        PlayerLiveUpdateAnzeige.SetActive(toggle.isOn);
        if (toggle.isOn)
        {
            StartCoroutine(DisplayLivePlayerAnswers());
        }
        else
        {
            Logging.log(Logging.LogType.Debug, "SloxikonServer", "ToggleLivePlayerAnswers", "Spielerantworteneingabe werden nicht mehr aktualisiert.");
            StopCoroutine(DisplayLivePlayerAnswers());
        }
    }
    /// <summary>
    /// Aktualisiert die Spielereingaben regelmäßig
    /// </summary>
    IEnumerator DisplayLivePlayerAnswers()
    {
        Logging.log(Logging.LogType.Debug, "SloxikonServer", "DisplayLivePlayerAnswers", "Spielerantworteneingabe werden ab jetzt aktualisiert.");
        while (liveupdate) {
            for (int i = 0; i < Config.PLAYERLIST.Length; i++)
            {
                int id = Int32.Parse(Antworten[i].transform.GetChild(1).GetComponentInChildren<TMP_Text>().text);
                if (id != 0)
                    Antworten[i].transform.GetChild(2).GetComponentInChildren<TMP_InputField>().text = playeranswers[id - 1];
                //else
                    //Antworten[i].transform.GetChild(2).GetComponentInChildren<TMP_InputField>().text = Config.SLOXIKON_SPIEL.getSelected().getThemen()[aktuellesThema];
            }   
            yield return new WaitForSeconds(0.25f);
        }
        yield return null;
    }
    /// <summary>
    /// Startet den Timer und bricht den alten, falls dieser noch läuft, ab
    /// </summary>
    public void TimerStarten()
    {
        Logging.log(Logging.LogType.Debug, "SloxikonServer", "TimerStarten", "Startet den Timer");
        if (TimerSekunden.text.Length == 0)
            return;
        if (TimerCoroutine != null)
        {
            StopCoroutine(TimerCoroutine);
            TimerCoroutine = null;
        }
        int sekunden = Int32.Parse(TimerSekunden.text);
        ServerUtils.BroadcastImmediate("#SloxikonTimerStarten " + (sekunden));
        TimerCoroutine = StartCoroutine(RunTimer(sekunden));
    }
    /// <summary>
    /// Beendet den Timer
    /// </summary>
    public void TimerStop()
    {
        Logging.log(Logging.LogType.Debug, "SloxikonServer", "TimerStop", "Beendet den Timer");
        if (TimerSekunden.text.Length == 0)
            return;
        ServerUtils.BroadcastImmediate("#SloxikonTimerStop");
        Timer.SetActive(false);
        StopCoroutine(TimerCoroutine);
    }
    /// <summary>
    /// Timer läuft
    /// </summary>
    /// <param name="seconds">Dauer</param>
    IEnumerator RunTimer(int seconds)
    {
        Logging.log(Logging.LogType.Debug, "SloxikonServer", "RunTimer", "Timer läuft: " + seconds);
        Timer.SetActive(true);

        while (seconds >= 0)
        {
            Timer.GetComponentInChildren<TMP_Text>().text = "" + seconds;

            if (seconds == 0)
            {
                Beeep.Play();
            }
            // Moep Sound bei sekunden
            if (seconds == 1 || seconds == 2 || seconds == 3 || seconds == 10 || seconds == 30 || seconds == 60) // 10-0
            {
                Moeoep.Play();
            }
            seconds--;
            yield return new WaitForSecondsRealtime(1);
        }
        Timer.SetActive(false);
        yield break;
    }
    #endregion
    #region Buzzer
    /// <summary>
    /// Aktiviert/Deaktiviert den Buzzer für alle Spieler
    /// </summary>
    /// <param name="toggle"></param>
    public void BuzzerAktivierenToggle(Toggle toggle)
    {
        buzzerIsOn = toggle.isOn;
        BuzzerAnzeige.SetActive(toggle.isOn);
    }
    /// <summary>
    /// Spielt Sound ab, sperrt den Buzzer und zeigt den Spieler an
    /// </summary>
    /// <param name="p"></param>
    private void SpielerBuzzered(Player p)
    {
        if (!buzzerIsOn)
        {
            Logging.log(Logging.LogType.Normal, "SloxikonServer", "SpielerBuzzered", p.name + " - " + DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second + ":" + DateTime.Now.Millisecond);
            return;
        }
        Logging.log(Logging.LogType.Warning, "SloxikonServer", "SpielerBuzzered", "B: " + p.name + " - " + DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second + ":" + DateTime.Now.Millisecond);
        buzzerIsOn = false;
        ServerUtils.BroadcastImmediate("#AudioBuzzerPressed " + p.id);
        BuzzerSound.Play();
        SpielerAnzeige[p.id - 1, 1].SetActive(true);
    }
    /// <summary>
    /// Gibt den Buzzer für alle Spieler frei
    /// </summary>
    public void SpielerBuzzerFreigeben()
    {
        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
            SpielerAnzeige[i, 1].SetActive(false);
        buzzerIsOn = BuzzerAnzeige.activeInHierarchy;
        Logging.log(Logging.LogType.Warning, "SloxikonServer", "SpielerBuzzerFreigeben", "Buzzer wurde freigegeben.");
        ServerUtils.BroadcastImmediate("#BuzzerFreigeben");
    }
    #endregion
    #region Spieler Ausgetabt Anzeige
    /// <summary>
    /// Austaben wird allen/keinen Spielern angezeigt
    /// </summary>
    /// <param name="toggle"></param>
    public void AustabenAllenZeigenToggle(Toggle toggle)
    {
        AustabbenAnzeigen.SetActive(toggle.isOn);
        if (toggle.isOn == false)
            ServerUtils.BroadcastImmediate("#SpielerAusgetabt 0");
    }
    /// <summary>
    /// Spieler Tabt aus, wird ggf allen gezeigt
    /// </summary>
    /// <param name="player"></param>
    /// <param name="data"></param>
    private void ClientFocusChange(Player player, string data)
    {
        bool ausgetabt = !Boolean.Parse(data);
        SpielerAnzeige[(player.id - 1), 3].SetActive(ausgetabt); // Ausgetabt Einblednung
        if (AustabbenAnzeigen.activeInHierarchy)
            ServerUtils.BroadcastImmediate("#SpielerAusgetabt " + player.id + " " + ausgetabt);
    }
    #endregion
    #region Textantworten der Spieler
    /// <summary>
    /// Blendet die Texteingabe für die Spieler ein
    /// </summary>
    /// <param name="toggle"></param>
    public void TexteingabeAnzeigenToggle(Toggle toggle)
    {
        TextEingabeAnzeige.SetActive(toggle.isOn);
        ServerUtils.BroadcastImmediate("#TexteingabeAnzeigen "+ toggle.isOn);
    }
    /// <summary>
    /// Aktualisiert die Antwort die der Spieler eingibt
    /// </summary>
    /// <param name="p"></param>
    /// <param name="data"></param>
    private void SpielerAntwortEingabe(Player p, string data)
    {
        SpielerAnzeige[p.id - 1, 6].GetComponentInChildren<TMP_InputField>().text = data;

        // Speicherung der Eingabe
        playeranswers[p.id - 1] = data;
    }
    /// <summary>
    /// Blendet die Textantworten der Spieler ein
    /// </summary>
    /// <param name="toggle"></param>
    public void TextantwortenAnzeigeToggle(Toggle toggle)
    {
        TextAntwortenAnzeige.SetActive(toggle.isOn);
        if (!toggle.isOn)
        {
            ServerUtils.BroadcastImmediate("#TextantwortenAnzeigen [BOOL]" + toggle.isOn + "[BOOL]");
            return;
        }
        string msg = "";
        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
        {
            msg = msg + "[ID" + (i + 1) + "]" + SpielerAnzeige[i, 6].GetComponentInChildren<TMP_InputField>().text + "[ID" + (i + 1) + "]";
        }
        ServerUtils.BroadcastImmediate("#TextantwortenAnzeigen [BOOL]"+toggle.isOn+"[BOOL][TEXT]"+ msg);
    }
    #endregion
    #region Punkte
    /// <summary>
    /// Punkte Pro Richtige Antworten Anzeigen
    /// </summary>
    /// <param name="input"></param>
    public void ChangePunkteProRichtigeAntwort(TMP_InputField input)
    {
        PunkteProRichtige = Int32.Parse(input.text);
    }
    /// <summary>
    /// Punkte Pro Falsche Antworten Anzeigen
    /// </summary>
    /// <param name="input"></param>
    public void ChangePunkteProFalscheAntwort(TMP_InputField input)
    {
        PunkteProFalsche = Int32.Parse(input.text);
    }
    /// <summary>
    /// Ändert die Punkte des Spielers (+-1)
    /// </summary>
    /// <param name="button"></param>
    public void PunkteManuellAendern(GameObject button)
    {
        int pId = Int32.Parse(button.transform.parent.parent.name.Replace("Player (", "").Replace(")", ""));
        int pIndex = Player.getPosInLists(pId);

        if (button.name == "+1")
        {
            Config.PLAYERLIST[pIndex].points += 1;
        }
        if (button.name == "-1")
        {
            Config.PLAYERLIST[pIndex].points -= 1;
        }
        UpdateSpielerBroadcast();
    }
    /// <summary>
    /// Ändert die Punkte des Spielers, variable Punkte
    /// </summary>
    /// <param name="input"></param>
    public void PunkteManuellAendern(TMP_InputField input)
    {
        int pId = Int32.Parse(input.transform.parent.parent.name.Replace("Player (", "").Replace(")", ""));
        int pIndex = Player.getPosInLists(pId);
        int punkte = Int32.Parse(input.text);
        input.text = "";

        Config.PLAYERLIST[pIndex].points += punkte;
        UpdateSpielerBroadcast();
    }
    #endregion
    #region Spieler ist (Nicht-)Dran
    /// <summary>
    /// Aktiviert den Icon Rand beim Spieler
    /// </summary>
    /// <param name="button"></param>
    public void SpielerIstDran(GameObject button)
    {
        int pId = Int32.Parse(button.transform.parent.parent.name.Replace("Player (", "").Replace(")", ""));
        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
            SpielerAnzeige[(pId - 1), 1].SetActive(false);
        SpielerAnzeige[(pId - 1), 1].SetActive(true);
        buzzerIsOn = false;
        ServerUtils.BroadcastImmediate("#SpielerIstDran "+pId);
    }
    /// <summary>
    /// Versteckt den Icon Rand beim Spieler
    /// </summary>
    /// <param name="button"></param>
    public void SpielerIstNichtDran(GameObject button)
    {
        int pId = Int32.Parse(button.transform.parent.parent.name.Replace("Player (", "").Replace(")", ""));
        SpielerAnzeige[(pId - 1), 1].SetActive(false);

        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
            if (SpielerAnzeige[i, 1].activeInHierarchy)
                return;
        buzzerIsOn = BuzzerAnzeige.activeInHierarchy; // Buzzer wird erst aktiviert wenn keiner mehr dran ist
        ServerUtils.BroadcastImmediate("#SpielerIstNichtDran " + pId);
    }
    #endregion
}
