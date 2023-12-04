using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class QuizServer : MonoBehaviour
{
    GameObject Frage;
    GameObject FragenAnzeige;
    GameObject FragenIndex1;
    GameObject FragenIndex2;
    GameObject BuzzerAnzeige;
    bool buzzerIsOn = false;
    int aktuelleFrage = 0;
    GameObject FalscheAntworten;
    GameObject AustabbenAnzeigen;
    GameObject TextEingabeAnzeige;
    GameObject TextAntwortenAnzeige;
    GameObject[,] SpielerAnzeige;
    GameObject[] SchaetzfragenAnzeige;
    TMP_InputField[] SchaetzfragenSpielerInput;
    [SerializeField] GameObject SchaetzfragenAnimationController;
    bool[] PlayerConnected;
    int PunkteProRichtige = 3;
    int PunkteProFalsche = 1;
    TMP_Text BuzzerDelay;

    [SerializeField] AudioSource BuzzerSound;
    [SerializeField] AudioSource RichtigeAntwortSound;
    [SerializeField] AudioSource FalscheAntwortSound;
    [SerializeField] AudioSource DisconnectSound;

    void OnEnable()
    {
        PlayerConnected = new bool[Config.SERVER_MAX_CONNECTIONS];
        InitAnzeigen();
        InitQuiz();
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
                PlayDisconnectSound();
                break;
            case "#TestConnection":
                break;
            case "#ClientFocusChange":
                ClientFocusChange(player, data);
                break;

            case "#JoinQuiz":
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
        Logging.log(Logging.LogType.Debug, "QuizServer", "SpielVerlassenButton", "Spiel wird beendet. Lädt ins Hauptmenü.");
        //SceneManager.LoadScene("Startup");
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
        // Server 
        FalscheAntworten.GetComponent<TMP_Text>().text = "Falsche Antworten: "+Config.SERVER_PLAYER.points;
        Logging.log(Logging.LogType.Debug, "QuizServer", "UpdateSpieler", msg);
        return msg;
    }
    /// <summary>
    /// Initialisiert die Anzeigen zu beginn
    /// </summary>
    private void InitAnzeigen()
    {
        Logging.log(Logging.LogType.Debug, "QuizServer", "InitAnzeigen", "Anzeigen werden initialisiert.");
        SchaetzfragenAnimationController.SetActive(false);

        // Fragen Anzeige
        Frage = GameObject.Find("Frage");
        Frage.GetComponentInChildren<TMP_Text>().text = "";
        GameObject.Find("ServerSide/FrageAnzeigenToggle").GetComponent<Toggle>().isOn = false;
        FragenAnzeige = GameObject.Find("ServerSide/FrageWirdAngezeigt");
        FragenAnzeige.SetActive(false);
        FragenIndex1 = GameObject.Find("QuizAnzeigen/FragenIndex1");
        FragenIndex1.GetComponentInChildren<TMP_Text>().text = "";
        FragenIndex2 = GameObject.Find("QuizAnzeigen/FragenIndex2");
        FragenIndex2.GetComponentInChildren<TMP_Text>().text = "";
        FalscheAntworten = GameObject.Find("QuizAnzeigen/FalscheAntwortenCounter");
        Config.SERVER_PLAYER.points = 0;
        FalscheAntworten.GetComponent<TMP_Text>().text = "Falsche Antworten: "+Config.SERVER_PLAYER.points;
        // Buzzer Deaktivieren
        GameObject.Find("ServerSide/BuzzerAktivierenToggle").GetComponent<Toggle>().isOn = false;
        BuzzerAnzeige = GameObject.Find("ServerSide/BuzzerIstAktiviert");
        BuzzerAnzeige.SetActive(false);
        buzzerIsOn = false;
        // Austabben wird gezeigt
        GameObject.Find("ServerSide/AusgetabtSpielernZeigenToggle").GetComponent<Toggle>().isOn = false;
        AustabbenAnzeigen = GameObject.Find("ServerSide/AusgetabtWirdSpielernGezeigen");
        AustabbenAnzeigen.SetActive(false);
        // Spieler Texteingabe
        GameObject.Find("ServerSide/TexteingabeAnzeigenToggle").GetComponent<Toggle>().isOn = false;
        TextEingabeAnzeige = GameObject.Find("ServerSide/TexteingabeWirdAngezeigt");
        TextEingabeAnzeige.SetActive(false);
        GameObject.Find("ServerSide/TextantwortenAnzeigenToggle").GetComponent<Toggle>().isOn = false;
        TextAntwortenAnzeige = GameObject.Find("ServerSide/TextantwortenWerdenAngezeigt");
        TextAntwortenAnzeige.SetActive(false);
        // Punkte Pro Richtige Antwort
        GameObject.Find("ServerSide/PunkteProRichtigeAntwort").GetComponent<TMP_InputField>().text = ""+PunkteProRichtige;
        // Punkte Pro Falsche Antwort
        GameObject.Find("ServerSide/PunkteProFalscheAntwort").GetComponent<TMP_InputField>().text = ""+PunkteProFalsche;
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
        // Change Quiz
        GameObject ChangeQuiz = GameObject.Find("ServerSide/ChangeQuiz");
        ChangeQuiz.GetComponent<TMP_Dropdown>().ClearOptions();
        List<string> quizzes = new List<string>();
        foreach (Quiz quiz in Config.QUIZ_SPIEL.getQuizze())
            quizzes.Add(quiz.getTitel());
        ChangeQuiz.GetComponent<TMP_Dropdown>().AddOptions(quizzes);
        ChangeQuiz.GetComponent<TMP_Dropdown>().value = Config.QUIZ_SPIEL.getIndex(Config.QUIZ_SPIEL.getSelected());

        SchaetzfragenAnzeige = new GameObject[20];
        SchaetzfragenAnzeige[0] = GameObject.Find("SchaetzfragenAnimation/Grid");
        SchaetzfragenAnzeige[1] = GameObject.Find("SchaetzfragenAnimation/Grid/MinGrenze");
        SchaetzfragenAnzeige[2] = GameObject.Find("SchaetzfragenAnimation/Grid/ZielGrenze");
        SchaetzfragenAnzeige[3] = GameObject.Find("SchaetzfragenAnimation/Grid/MaxGrenze");

        SchaetzfragenAnzeige[4] = GameObject.Find("SchaetzfragenAnimation/Grid/Icon (1)");
        SchaetzfragenAnzeige[5] = GameObject.Find("SchaetzfragenAnimation/Grid/Icon (1)/Data");
        SchaetzfragenAnzeige[5].SetActive(false);
        SchaetzfragenAnzeige[6] = GameObject.Find("SchaetzfragenAnimation/Grid/Icon (2)");
        SchaetzfragenAnzeige[7] = GameObject.Find("SchaetzfragenAnimation/Grid/Icon (2)/Data");
        SchaetzfragenAnzeige[7].SetActive(false);
        SchaetzfragenAnzeige[8] = GameObject.Find("SchaetzfragenAnimation/Grid/Icon (3)");
        SchaetzfragenAnzeige[9] = GameObject.Find("SchaetzfragenAnimation/Grid/Icon (3)/Data");
        SchaetzfragenAnzeige[9].SetActive(false);
        SchaetzfragenAnzeige[10] = GameObject.Find("SchaetzfragenAnimation/Grid/Icon (4)");
        SchaetzfragenAnzeige[11] = GameObject.Find("SchaetzfragenAnimation/Grid/Icon (4)/Data");
        SchaetzfragenAnzeige[11].SetActive(false);
        SchaetzfragenAnzeige[12] = GameObject.Find("SchaetzfragenAnimation/Grid/Icon (5)");
        SchaetzfragenAnzeige[13] = GameObject.Find("SchaetzfragenAnimation/Grid/Icon (5)/Data");
        SchaetzfragenAnzeige[13].SetActive(false);
        SchaetzfragenAnzeige[14] = GameObject.Find("SchaetzfragenAnimation/Grid/Icon (6)");
        SchaetzfragenAnzeige[15] = GameObject.Find("SchaetzfragenAnimation/Grid/Icon (6)/Data");
        SchaetzfragenAnzeige[15].SetActive(false);
        SchaetzfragenAnzeige[16] = GameObject.Find("SchaetzfragenAnimation/Grid/Icon (7)");
        SchaetzfragenAnzeige[17] = GameObject.Find("SchaetzfragenAnimation/Grid/Icon (7)/Data");
        SchaetzfragenAnzeige[17].SetActive(false);
        SchaetzfragenAnzeige[18] = GameObject.Find("SchaetzfragenAnimation/Grid/Icon (8)");
        SchaetzfragenAnzeige[19] = GameObject.Find("SchaetzfragenAnimation/Grid/Icon (8)/Data");
        SchaetzfragenAnzeige[19].SetActive(false);

        SchaetzfragenAnzeige[0].SetActive(false);

        foreach (Player p in Config.PLAYERLIST)
        {
            if (p.isConnected)
            {
                GameObject.Find("SchaetzfragenAnimation/Spieler/Spieler (" + p.id + ")").GetComponent<Image>().sprite = p.icon2.icon;
            }
            else
            {
                GameObject.Find("SchaetzfragenAnimation/Spieler/Spieler (" + p.id + ")").gameObject.SetActive(false);
            }
        }

        BuzzerDelay = GameObject.Find("BuzzerDelay").GetComponent<TMP_Text>();
        BuzzerDelay.text = "";

        SchaetzfragenSpielerInput = new TMP_InputField[Config.PLAYERLIST.Length];
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            SchaetzfragenSpielerInput[i] = GameObject.Find("SchaetzfragenAnimation/Spieler/Spieler ("+(i+1)+")/Input").GetComponent<TMP_InputField>();
        }
    }
    #region Quiz Fragen Anzeige
    /// <summary>
    /// Initialisiert die Anzeigen des Quizzes
    /// </summary>
    private void InitQuiz()
    {
        Logging.log(Logging.LogType.Debug, "QuizServer", "InitQuiz", "Quizelemente werden aktualisiert.");
        aktuelleFrage = 0;
        GameObject.Find("QuizAnzeigen/Titel").GetComponent<TMP_Text>().text = Config.QUIZ_SPIEL.getSelected().getTitel();
        GameObject.Find("QuizAnzeigen/FragenIndex2").GetComponentInChildren<TMP_Text>().text = "";
        GameObject.Find("Frage").GetComponentInChildren<TMP_Text>().text = "";
        if (Config.QUIZ_SPIEL.getSelected().getFragenCount() > 0)
        {
            LoadQuestionIntoScene(0);
        }
    }
    /// <summary>
    /// Ändert das ausgewählte Quiz
    /// </summary>
    /// <param name="drop">Quizauswahl</param>
    public void ChangeQuiz(TMP_Dropdown drop)
    {
        Logging.log(Logging.LogType.Debug, "QuizServer", "ChangeQuiz", "Spiel wird gewechselt: " + drop.options[drop.value]);
        // Wählt neues Quiz aus
        Config.QUIZ_SPIEL.setSelected(Config.QUIZ_SPIEL.getQuizByIndex(drop.value));
        Logging.log(Logging.LogType.Normal, "QuizServer", "ChangeQuiz", "Quiz starts: " + Config.QUIZ_SPIEL.getSelected().getTitel());
        // Aktualisiert die Anzeigen
        aktuelleFrage = 0;
        GameObject.Find("QuizAnzeigen/Titel").GetComponent<TMP_Text>().text = Config.QUIZ_SPIEL.getSelected().getTitel();
        GameObject.Find("QuizAnzeigen/FragenIndex2").GetComponentInChildren<TMP_Text>().text = "";
        GameObject.Find("QuizAnzeigen/FragenIndex1").GetComponentInChildren<TMP_Text>().text = "";
        GameObject.Find("Frage").GetComponentInChildren<TMP_Text>().text = "";
        if (Config.QUIZ_SPIEL.getSelected().getFragenCount() > 0)
        {
            LoadQuestionIntoScene(0);
        }
    }
    /// <summary>
    /// Navigiert durch die Fragen, zeigt/versteckt diese
    /// </summary>
    /// <param name="type">Navigationsargument</param>
    public void NavigateThroughQuestions(string type)
    {
        switch (type)
        {
            default:
                Logging.log(Logging.LogType.Error, "QuizServer", "NavigateThroughQuestions", "Unbekannter Typ: " + type);
                break;
            case "previous":
                if (aktuelleFrage <= 0)
                    return;
                aktuelleFrage--;
                LoadQuestionIntoScene(aktuelleFrage);
                break;
            case "next":
                if (aktuelleFrage >= (Config.QUIZ_SPIEL.getSelected().getFragenCount() - 1))
                    return;
                aktuelleFrage++;
                LoadQuestionIntoScene(aktuelleFrage);
                break;
            case "show":
                GameObject.Find("Frage").GetComponentInChildren<TMP_Text>().text = Config.QUIZ_SPIEL.getSelected().getFrage(aktuelleFrage).getFrage().Replace("\\n", "\n");
                GameObject.Find("QuizAnzeigen/FragenIndex1").GetComponentInChildren<TMP_Text>().text = (aktuelleFrage + 1) + "/" + Config.QUIZ_SPIEL.getSelected().getFragenCount();
                ServerUtils.BroadcastImmediate(Config.GAME_TITLE + "#FragenAnzeige [BOOL]" + FragenAnzeige.activeInHierarchy + "[BOOL][FRAGE]" + Config.QUIZ_SPIEL.getSelected().getFrage(aktuelleFrage).getFrage());
                break;
            case "clear":
                GameObject.Find("Frage").GetComponentInChildren<TMP_Text>().text = "";
                GameObject.Find("QuizAnzeigen/FragenIndex1").GetComponentInChildren<TMP_Text>().text = "";
                ServerUtils.BroadcastImmediate(Config.GAME_TITLE +"#FragenAnzeige [BOOL]" + FragenAnzeige.activeInHierarchy + "[BOOL][FRAGE] ");
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
    /// Lädt die Frage nach Index, für die Server Vorschau
    /// </summary>
    /// <param name="index">Lädt eine Frage des aktuellen Quiz in die Scene</param>
    private void LoadQuestionIntoScene(int index)
    {
        Logging.log(Logging.LogType.Debug, "QuizServer", "LoadQuestionIntoScene", "Lädt die Frage: " + Config.QUIZ_SPIEL.getSelected().getFrage(index).getFrage() + " in die Scene.");
        GameObject.Find("QuizAnzeigen/FragenVorschau").GetComponent<TMP_Text>().text = "Frage:\n"+Config.QUIZ_SPIEL.getSelected().getFrage(index).getFrage().Replace("\\n", "\n");
        GameObject.Find("QuizAnzeigen/AntwortVorschau").GetComponent<TMP_Text>().text = "Antwort:\n"+ Config.QUIZ_SPIEL.getSelected().getFrage(index).getAntwort().Replace("\\n", "\n");
        GameObject.Find("QuizAnzeigen/InfoVorschau").GetComponent<TMP_Text>().text = "Info:\n"+Config.QUIZ_SPIEL.getSelected().getFrage(index).getInfo().Replace("\\n", "\n");
        GameObject.Find("QuizAnzeigen/FragenIndex2").GetComponentInChildren<TMP_Text>().text = (aktuelleFrage+1)+"/" + Config.QUIZ_SPIEL.getSelected().getFragenCount();
    }
    #endregion
    #region Buzzer
    /// <summary>
    /// Aktiviert/Deaktiviert den Buzzer für alle Spieler
    /// </summary>
    /// <param name="toggle">Toggle</param>
    public void BuzzerAktivierenToggle(Toggle toggle)
    {
        Logging.log(Logging.LogType.Debug, "QuizServer", "BuzzerAktivierenToggle", "Buzzer wird aktiviert: " + toggle.isOn);
        buzzerIsOn = toggle.isOn;
        BuzzerAnzeige.SetActive(toggle.isOn);
    }
    /// <summary>
    /// Spielt Sound ab, sperrt den Buzzer und zeigt den Spieler an
    /// </summary>
    /// <param name="p">Spieler</param>
    private DateTime BuzzeredTime;
    private void SpielerBuzzered(Player p)
    {
        if (!buzzerIsOn)
        {
            Logging.log(Logging.LogType.Normal, "QuizServer", "SpielerBuzzered", p.name + " - " + DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second + ":" + DateTime.Now.Millisecond);
            ServerUtils.SendMSG("#BuzzeredTime " + (DateTime.Now - BuzzeredTime).TotalMilliseconds, p, false);
            return;
        }
        Logging.log(Logging.LogType.Warning, "QuizServer", "SpielerBuzzered", "B: " + p.name + " - " + DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second + ":" + DateTime.Now.Millisecond);
        buzzerIsOn = false;
        ServerUtils.BroadcastImmediate(Config.GAME_TITLE +"#AudioBuzzerPressed " + p.id);
        ServerUtils.BroadcastImmediate(Config.GAME_TITLE + "#SpielerIstDran " + p.id);
        BuzzerSound.Play();
        SpielerAnzeige[p.id - 1, 1].SetActive(true);
        BuzzeredTime = DateTime.Now;
    }
    /// <summary>
    /// Gibt den Buzzer für alle Spieler frei
    /// </summary>
    public void SpielerBuzzerFreigeben()
    {
        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
            SpielerAnzeige[i, 1].SetActive(false);
        buzzerIsOn = BuzzerAnzeige.activeInHierarchy;
        Logging.log(Logging.LogType.Warning, "QuizServer", "SpielerBuzzerFreigeben", "Buzzer freigegeben");
        ServerUtils.BroadcastImmediate(Config.GAME_TITLE +"#BuzzerFreigeben");
    }
    #endregion
    #region Spieler Ausgetabt Anzeige
    /// <summary>
    /// Austaben wird allen/keinen Spielern angezeigt
    /// </summary>
    /// <param name="toggle">Toggle</param>
    public void AustabenAllenZeigenToggle(Toggle toggle)
    {
        Logging.log(Logging.LogType.Debug, "QuizServer", "AustabenAllenZeigenToggle", "Angeige: " + toggle.isOn);
        AustabbenAnzeigen.SetActive(toggle.isOn);
        if (toggle.isOn == false)
            ServerUtils.BroadcastImmediate(Config.GAME_TITLE +"#SpielerAusgetabt 0");
    }
    /// <summary>
    /// Spieler Tabt aus, wird ggf allen gezeigt
    /// </summary>
    /// <param name="player">Spieler</param>
    /// <param name="data">Ein-/Ausgetabt</param>
    private void ClientFocusChange(Player player, string data)
    {
        Logging.log(Logging.LogType.Debug, "QuizServer", "ClientFocusChange", "Spieler " + player.name + " ist ausgetabt: " + data);
        bool ausgetabt = !Boolean.Parse(data);
        SpielerAnzeige[(player.id - 1), 3].SetActive(ausgetabt); // Ausgetabt Einblednung
        if (AustabbenAnzeigen.activeInHierarchy)
            ServerUtils.BroadcastImmediate(Config.GAME_TITLE +"#SpielerAusgetabt " + player.id + " " + ausgetabt);
    }
    #endregion
    #region Frage
    /// <summary>
    /// Zeigt/Versteckt die Frage für alle Spieler
    /// </summary>
    /// <param name="toggle">Toggle</param>
    public void FrageAnzeigenToggle(Toggle toggle)
    {
        FragenAnzeige.SetActive(toggle.isOn);
        ServerUtils.BroadcastImmediate(Config.GAME_TITLE +"#FragenAnzeige [BOOL]"+toggle.isOn+"[BOOL][FRAGE]"+Frage.GetComponentInChildren<TMP_Text>().text.Replace("\n", "\\n"));
    }
    /// <summary>
    /// Blendet die selbst eingegebene Frage ein
    /// </summary>
    /// <param name="input">Texteingabefeld</param>
    public void EigeneFrageEinblenden(TMP_InputField input)
    {
        Frage.GetComponentInChildren<TMP_Text>().text = input.text.Replace("\\n", "\n");
        if (FragenAnzeige.activeInHierarchy)
            ServerUtils.BroadcastImmediate(Config.GAME_TITLE +"#FragenAnzeige [BOOL]" + FragenAnzeige.activeInHierarchy + "[BOOL][FRAGE]" + input.text);
        input.text = "";
    }
    #endregion
    #region Textantworten der Spieler
    /// <summary>
    /// Blendet die Texteingabe für die Spieler ein
    /// </summary>
    /// <param name="toggle">Toggle</param>
    public void TexteingabeAnzeigenToggle(Toggle toggle)
    {
        TextEingabeAnzeige.SetActive(toggle.isOn);
        ServerUtils.BroadcastImmediate(Config.GAME_TITLE +"#TexteingabeAnzeigen "+ toggle.isOn);
    }
    /// <summary>
    /// Aktualisiert die Antwort die der Spieler eingibt
    /// </summary>
    /// <param name="p">Spieler</param>
    /// <param name="data">Texteingabe</param>
    private void SpielerAntwortEingabe(Player p, string data)
    {
        SpielerAnzeige[p.id - 1, 6].GetComponentInChildren<TMP_InputField>().text = data;

        // Parse Eingabe, wenn Schätzfragen aktiviert sind
        ParseEingabeZuSchaetz(p, data);
    }
    char[] moeglicheEingaben = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ',', '.' };
    private void ParseEingabeZuSchaetz(Player p, string data)
    {
        int zahlstart = -1;
        int zahlende = -1;
        for (int i = 0; i < data.Length; i++)
        {
            // Buchstabe an Index i ist nicht in Liste enthalten
            if (Array.IndexOf(moeglicheEingaben, data[i]) > -1)
            {
                if (zahlstart == -1)
                    zahlstart = i;

                zahlende = i;
            }
            // Wenn Zahl Vorbei ist, abbrechen
            if (zahlende != i && zahlende > -1)
                break;
            try
            {
                string antwort = data.Substring(zahlstart, zahlende - zahlstart + 1).Replace(".", "");
                // maximiert komma trennung
                if (antwort.Contains(","))
                {
                    if (antwort.Split(',').Length > 2)
                        antwort = antwort.Split(',')[0] + "," + antwort.Split(',')[1];
                }

                SchaetzfragenSpielerInput[Player.getPosInLists(p.id)].text = antwort+"";
            }
            catch {}
        }
    }
    /// <summary>
    /// Blendet die Textantworten der Spieler ein
    /// </summary>
    /// <param name="toggle">Toggle</param>
    public void TextantwortenAnzeigeToggle(Toggle toggle)
    {
        TextAntwortenAnzeige.SetActive(toggle.isOn);
        if (!toggle.isOn)
        {
            ServerUtils.BroadcastImmediate(Config.GAME_TITLE +"#TextantwortenAnzeigen [BOOL]" + toggle.isOn + "[BOOL]");
            return;
        }
        string msg = "";
        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
        {
            msg = msg + "[ID" + (i + 1) + "]" + SpielerAnzeige[i, 6].GetComponentInChildren<TMP_InputField>().text + "[ID" + (i + 1) + "]";
        }
        ServerUtils.BroadcastImmediate(Config.GAME_TITLE +"#TextantwortenAnzeigen [BOOL]"+toggle.isOn+"[BOOL][TEXT]"+ msg);
    }
    #endregion
    #region Punkte
    /// <summary>
    /// Punkte Pro Richtige Antworten Anzeigen
    /// </summary>
    /// <param name="input">Punkteingabe</param>
    public void ChangePunkteProRichtigeAntwort(TMP_InputField input)
    {
        PunkteProRichtige = Int32.Parse(input.text);
    }
    /// <summary>
    /// Punkte Pro Falsche Antworten Anzeigen
    /// </summary>
    /// <param name="input">Punkteingabe</param>
    public void ChangePunkteProFalscheAntwort(TMP_InputField input)
    {
        PunkteProFalsche = Int32.Parse(input.text);
    }
    /// <summary>
    /// Vergibt an den Spieler Punkte für eine richtige Antwort
    /// </summary>
    /// <param name="player">Spieler</param>
    public void PunkteRichtigeAntwort(GameObject player)
    {
        int pId = Int32.Parse(player.transform.parent.parent.name.Replace("Player (", "").Replace(")", ""));
        ServerUtils.BroadcastImmediate(Config.GAME_TITLE +"#AudioRichtigeAntwort " + pId + "*" + PunkteProRichtige);
        RichtigeAntwortSound.Play();
        int pIndex = Player.getPosInLists(pId);
        Config.PLAYERLIST[pIndex].points += PunkteProRichtige;
        UpdateSpieler();
    }
    /// <summary>
    /// Vergibt an alle anderen Spieler Punkte bei einer falschen Antwort
    /// </summary>
    /// <param name="player">Spieler</param>
    public void PunkteFalscheAntwort(GameObject player)
    {
        int pId = Int32.Parse(player.transform.parent.parent.name.Replace("Player (", "").Replace(")", ""));
        ServerUtils.BroadcastImmediate(Config.GAME_TITLE + "#AudioFalscheAntwort "+ pId + "*" + PunkteProFalsche);
        FalscheAntwortSound.Play();
        foreach (Player p in Config.PLAYERLIST)
        {
            if (pId != p.id && p.isConnected)
                p.points += PunkteProFalsche;
        }
        Config.SERVER_PLAYER.points += PunkteProFalsche;
        UpdateSpieler();
    }
    /// <summary>
    /// Ändert die Punkte des Spielers (+-1)
    /// </summary>
    /// <param name="button">Spieler</param>
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
    /// <param name="input">Punkteingabe</param>
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
    /// <param name="button">Spieler</param>
    public void SpielerIstDran(GameObject button)
    {
        int pId = Int32.Parse(button.transform.parent.parent.name.Replace("Player (", "").Replace(")", ""));
        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
            SpielerAnzeige[(pId - 1), 1].SetActive(false);
        SpielerAnzeige[(pId - 1), 1].SetActive(true);
        buzzerIsOn = false;
        ServerUtils.BroadcastImmediate(Config.GAME_TITLE +"#SpielerIstDran "+pId);
    }
    /// <summary>
    /// Versteckt den Icon Rand beim Spieler
    /// </summary>
    /// <param name="button">Spieler</param>
    public void SpielerIstNichtDran(GameObject button)
    {
        int pId = Int32.Parse(button.transform.parent.parent.name.Replace("Player (", "").Replace(")", ""));
        SpielerAnzeige[(pId - 1), 1].SetActive(false);

        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
            if (SpielerAnzeige[i, 1].activeInHierarchy)
                return;
        buzzerIsOn = BuzzerAnzeige.activeInHierarchy; // Buzzer wird erst aktiviert wenn keiner mehr dran ist
        ServerUtils.BroadcastImmediate(Config.GAME_TITLE +"#SpielerIstNichtDran " + pId);
    }
    #endregion
    #region Schätzfragen Animation
    /// <summary>
    /// Zeigt das Ziel der Schätzfragenanimation an
    /// </summary>
    /// <param name="toggle">Toggle</param>
    public void toggleZielAnzeige(Toggle toggle)
    {
        SchaetzfragenAnzeige[2].SetActive(toggle.isOn);
        ServerUtils.BroadcastImmediate(Config.GAME_TITLE +"#AnimationZiel "+ toggle.isOn);
    }
    /// <summary>
    /// Aktualisiert die Grenzen der Schätzfragenanimation
    /// </summary>
    public void updateGrenzen()
    {
        SchaetzfragenAnzeige[1].GetComponentInChildren<TMP_Text>().text = GameObject.Find("SchaetzfragenAnimation/MinGrenzeFestlegen").GetComponent<TMP_InputField>().text + GameObject.Find("SchaetzfragenAnimation/EinheitAngeben").GetComponent<TMP_InputField>().text;
        SchaetzfragenAnzeige[2].GetComponentInChildren<TMP_Text>().text = GameObject.Find("SchaetzfragenAnimation/ZielGrenzeFestlegen").GetComponent<TMP_InputField>().text + GameObject.Find("SchaetzfragenAnimation/EinheitAngeben").GetComponent<TMP_InputField>().text;
        SchaetzfragenAnzeige[3].GetComponentInChildren<TMP_Text>().text = GameObject.Find("SchaetzfragenAnimation/MaxGrenzeFestlegen").GetComponent<TMP_InputField>().text + GameObject.Find("SchaetzfragenAnimation/EinheitAngeben").GetComponent<TMP_InputField>().text;
        SchaetzfragenAnzeige[2].SetActive(GameObject.Find("SchaetzfragenAnimation/ZielAnzeigen").GetComponent<Toggle>().isOn);
    }
    /// <summary>
    /// Zeigt den Startzustand der Animation
    /// </summary>
    public void zeigeAnimationAn()
    {
        SchaetzfragenAnzeige[0].SetActive(true);
        SchaetzfragenAnimationController.SetActive(false);
        int komma = Int32.Parse(GameObject.Find("SchaetzfragenAnimation/KommastellenFestlegen").GetComponent<TMP_InputField>().text);

        // Zeigt die Spielerschätzungen an
        foreach (Player p in Config.PLAYERLIST)
        {
            if (p.isConnected)
            {
                // Schneidet zuviele Kommastellen ab
                string schaetzung = SchaetzfragenSpielerInput[Player.getPosInLists(p.id)].text;
                if (schaetzung.Contains(","))
                {
                    string kommas = schaetzung.Split(',')[1]+"00000000000";
                    kommas = kommas.Substring(0, komma);
                    schaetzung = schaetzung.Split(',')[0]+ "," + kommas;
                }

                SchaetzfragenAnzeige[(4 + 2 * (p.id - 1))].transform.GetChild(1).GetComponent<TMP_Text>().text = schaetzung + GameObject.Find("SchaetzfragenAnimation/EinheitAngeben").GetComponent<TMP_InputField>().text;
                SchaetzfragenAnzeige[(4 + 2 * (p.id - 1))].transform.GetChild(3).gameObject.SetActive(false);
                SchaetzfragenAnzeige[(4 + 2 * (p.id - 1))].GetComponent<Image>().sprite = p.icon2.icon;

            }
        }
        BerechneSchritteProEinheit();


        // Zeigt Sieger für Server an
        string einheit = GameObject.Find("SchaetzfragenAnimation/EinheitAngeben").GetComponent<TMP_InputField>().text;
        double schatung = double.Parse(SchaetzfragenAnzeige[4].GetComponentInChildren<TMP_Text>().text.Replace(einheit , ""));
        double ziel = double.Parse(SchaetzfragenAnzeige[2].GetComponentInChildren<TMP_Text>().text.Replace(einheit, ""));
        double diff = Math.Abs(schatung - ziel);
        foreach (Player p in Config.PLAYERLIST)
        {
            if (!SchaetzfragenAnzeige[(4 + 2 * (p.id - 1))].activeInHierarchy)
                continue;

            double spieler = double.Parse(SchaetzfragenAnzeige[(4 + 2 * (p.id - 1))].GetComponentInChildren<TMP_Text>().text.Replace(einheit, ""));
            double spielerdiff = Math.Abs(spieler - ziel);
            if (spielerdiff < diff)
            {
                schatung = spieler;
                diff = spielerdiff;
            }
        }
        foreach (Player p in Config.PLAYERLIST)
        {
            if (!SchaetzfragenAnzeige[(4 + 2 * (p.id - 1))].activeInHierarchy)
                continue;
            double spieler = double.Parse(SchaetzfragenAnzeige[(4 + 2 * (p.id - 1))].GetComponentInChildren<TMP_Text>().text.Replace(einheit, ""));
            double spielerdiff = Math.Abs(spieler - ziel);
            if (spielerdiff == diff)
                SchaetzfragenAnzeige[(4 + 2 * (p.id - 1))].transform.GetChild(3).gameObject.SetActive(true);
        }
    }
    /// <summary>
    /// Berechnet Daten für die Schätzfragenanimation
    /// </summary>
    public void BerechneSchritteProEinheit()
    {
        if (GameObject.Find("SchaetzfragenAnimation/EinheitAngeben").GetComponent<TMP_InputField>().text == "")
            GameObject.Find("SchaetzfragenAnimation/EinheitAngeben").GetComponent<TMP_InputField>().text = " ";

        float StartWert = float.Parse(SchaetzfragenAnzeige[1].GetComponentInChildren<TMP_Text>().text.Replace(GameObject.Find("SchaetzfragenAnimation/EinheitAngeben").GetComponent<TMP_InputField>().text, ""));
        float ZielWert = float.Parse(SchaetzfragenAnzeige[2].GetComponentInChildren<TMP_Text>().text.Replace(GameObject.Find("SchaetzfragenAnimation/EinheitAngeben").GetComponent<TMP_InputField>().text, ""));
        float MaxWert = float.Parse(SchaetzfragenAnzeige[3].GetComponentInChildren<TMP_Text>().text.Replace(GameObject.Find("SchaetzfragenAnimation/EinheitAngeben").GetComponent<TMP_InputField>().text, ""));
        float StartPosition = SchaetzfragenAnzeige[1].transform.localPosition.x;
        float MaxPosition = SchaetzfragenAnzeige[3].transform.localPosition.x;
        float DifftoNull = Math.Abs(StartPosition);
        float DiffToMax = MaxPosition - StartPosition;
        float WertToMax = DiffToMax / (MaxWert-StartWert);
        double spielerwert = 0;

        // Ziel Bewegen
        SchaetzfragenAnzeige[2].transform.localPosition = new Vector3(WertToMax*(ZielWert-StartWert) - DifftoNull ,SchaetzfragenAnzeige[2].transform.localPosition.y, 0);

        // SpielerData berechnen
        string data_text = "";
        data_text += "[START_WERT]" + StartWert + "[START_WERT]";
        data_text += "[ZIEL_WERT]" + ZielWert + "[ZIEL_WERT]";
        data_text += "[MAX_WERT]" + MaxWert + "[MAX_WERT]";
        data_text += "[START_POSITION]" + StartPosition + "[START_POSITION]";
        data_text += "[MAX_POSITION]" + MaxPosition + "[MAX_POSITION]";
        data_text += "[DIFF_NULL]" + DifftoNull + "[DIFF_NULL]";
        data_text += "[DIFF_MAX]" + DiffToMax + "[DIFF_MAX]";
        data_text += "[DISTANCE_PER_MOVE]" + WertToMax + "[DISTANCE_PER_MOVE]";
        data_text += "[EINHEIT]" + GameObject.Find("SchaetzfragenAnimation/EinheitAngeben").GetComponent<TMP_InputField>().text + "[EINHEIT]";
        data_text += "[KOMMASTELLEN]" + GameObject.Find("SchaetzfragenAnimation/KommastellenFestlegen").GetComponent<TMP_InputField>().text + "[KOMMASTELLEN]";
        data_text += "[SPIELER_WERT]" + spielerwert + "[SPIELER_WERT]";

        string broadcastmsg = "";
        // Spieler
        foreach (Player p in Config.PLAYERLIST)
        {
            SchaetzfragenAnzeige[(4 + 2 * (p.id - 1))].transform.localPosition = new Vector3(StartPosition, SchaetzfragenAnzeige[(4 + 2 * (p.id - 1))].transform.localPosition.y, 0);
            if (p.isConnected)
            {
                spielerwert = double.Parse(GameObject.Find("SchaetzfragenAnimation/Grid/Icon (" + p.id + ")").GetComponentInChildren<TMP_Text>().text.Replace(GameObject.Find("SchaetzfragenAnimation/EinheitAngeben").GetComponent<TMP_InputField>().text, ""));
                data_text = data_text.Replace("[SPIELER_WERT]", "|").Split('|')[0] + "[SPIELER_WERT]" + spielerwert + "[SPIELER_WERT]";
                SchaetzfragenAnzeige[(5 + 2 * (p.id - 1))].GetComponent<TMP_Text>().text = data_text;
                broadcastmsg += "[" + p.id + "]"+spielerwert+"[" + p.id + "]";
            }
            else
            {
                // Hide disconnected
                SchaetzfragenAnzeige[(4 + 2 * (p.id - 1))].SetActive(false);
                broadcastmsg += "[" + p.id + "]0[" + p.id + "]";
            }
        }
        ServerUtils.BroadcastImmediate(Config.GAME_TITLE +"#AnimationInfo " + data_text.Replace("[SPIELER_WERT]", "|").Split('|')[0] + broadcastmsg);
    }
    /// <summary>
    /// Startet die Schätzfragenanimation
    /// </summary>
    public void AnimationStarten()
    {
        ServerUtils.BroadcastImmediate(Config.GAME_TITLE +"#AnimationStart");
        SchaetzfragenAnimationController.SetActive(true);
    }
    /// <summary>
    /// Beendet die Schätzfragenanimation
    /// </summary>
    public void AnimationBeenden()
    {
        ServerUtils.BroadcastImmediate(Config.GAME_TITLE +"#AnimationBeenden");
        SchaetzfragenAnzeige[0].SetActive(false);
        SchaetzfragenAnimationController.SetActive(false);
    }
    #endregion
}
