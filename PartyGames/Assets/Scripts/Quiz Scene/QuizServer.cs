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
    [SerializeField] GameObject SchaetzfragenAnimationController;
    bool[] PlayerConnected;
    int PunkteProRichtige = 4;
    int PunkteProFalsche = 1;

    [SerializeField] AudioSource BuzzerSound;
    [SerializeField] AudioSource RichtigeAntwortSound;
    [SerializeField] AudioSource FalscheAntwortSound;

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
            SceneManager.LoadScene("Startup");
            return;
        }

        foreach (Player spieler in Config.PLAYERLIST)
        {

            if (spieler.isConnected == false)
                continue;


            #region Prüft ob Clients noch verbunden sind
            /*if (!isConnected(spieler.tcp) && spieler.isConnected == true)
            {
                Debug.LogWarning(spieler.id);
                spieler.tcp.Close();
                spieler.isConnected = false;
                spieler.isDisconnected = true;
                Logging.add(new Logging(Logging.Type.Normal, "Server", "Update", "Spieler ist nicht mehr Verbunden. ID: " + spieler.id));
                continue;
            }*/
            #endregion
            #region Sucht nach neuen Nachrichten
            /*else*/
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

            #region Spieler Disconnected Message
            for (int i = 0; i < Config.PLAYERLIST.Length; i++)
            {
                if (Config.PLAYERLIST[i].isConnected == false)
                {
                    if (Config.PLAYERLIST[i].isDisconnected == true)
                    {
                        Logging.add(Logging.Type.Normal, "QuizServer", "Update", "Spieler hat die Verbindung getrennt. ID: " + Config.PLAYERLIST[i].id);
                        Broadcast(Config.PLAYERLIST[i].name + " has disconnected", Config.PLAYERLIST);
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
        Logging.add(new Logging(Logging.Type.Normal, "Server", "OnApplicationQuit", "Server wird geschlossen"));
        Config.SERVER_TCP.Server.Close();
    }

    #region Server Stuff
    #region Verbindungen
    /**
     * Prüft ob eine Verbindung zum gegebenen Client noch besteht
     */
    private bool isConnected(TcpClient c)
    {
        /*try
        {
            if (c != null && c.Client != null && c.Client.Connected)
            {
                if (c.Client.Poll(0, SelectMode.SelectRead))
                {
                    return !(c.Client.Receive(new byte[1], SocketFlags.Peek) == 0);
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        catch
        {
            return false;
        }*/
        if (c != null && c.Client != null && c.Client.Connected)
        {
            if ((c.Client.Poll(0, SelectMode.SelectWrite)) && (!c.Client.Poll(0, SelectMode.SelectError)))
            {
                byte[] buffer = new byte[1];
                if (c.Client.Receive(buffer, SocketFlags.Peek) == 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }
    #endregion
    #region Kommunikation
    /**
     * Sendet eine Nachricht an den übergebenen Spieler
     */
    private void SendMessage(string data, Player sc)
    {
        try
        {
            StreamWriter writer = new StreamWriter(sc.tcp.GetStream());
            writer.WriteLine(data);
            writer.Flush();
        }
        catch (Exception e)
        {
            Logging.add(new Logging(Logging.Type.Error, "Server", "SendMessage", "Nachricht an Client: " + sc.id + " (" + sc.name + ") konnte nicht gesendet werden." + e));
        }
    }
    /**
     * Sendet eine Nachricht an alle verbundenen Spieler
     */
    private void Broadcast(string data, Player[] spieler)
    {
        foreach (Player sc in spieler)
        {
            if (sc.isConnected)
                SendMessage(data, sc);
        }
    }
    /**
     * Sendet eine Nachricht an alle verbundenen Spieler
     */
    private void Broadcast(string data)
    {
        foreach (Player sc in Config.PLAYERLIST)
        {
            if (sc.isConnected)
                SendMessage(data, sc);
        }
    }
    /**
     * Einkommende Nachrichten die von Spielern an den Server gesendet werden.
     */
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
    /**
     * Einkommende Befehle von Spielern
     */
    public void Commands(Player player, string data, string cmd)
    {
        // Zeigt alle einkommenden Nachrichten an
        Debug.Log(player.name + " " + player.id + " -> " + cmd + "   ---   " + data);
        // Sucht nach Command
        switch (cmd)
        {
            default:
                Logging.add(Logging.Type.Warning, "QuizServer", "Commands", "Unkown Command -> " + cmd + " - " + data);
                break;

            case "#ClientClosed":
                ClientClosed(player);
                UpdateSpielerBroadcast();
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
    /**
     * Fordert alle Clients auf die RemoteConfig neuzuladen
     */
    public void UpdateRemoteConfig()
    {
        Broadcast("#UpdateRemoteConfig");
        LoadConfigs.FetchRemoteConfig();
    }
    /**
     * Spieler beendet das Spiel
     */
    private void ClientClosed(Player player)
    {
        player.icon = Resources.Load<Sprite>("Images/ProfileIcons/empty");
        player.name = "";
        player.points = 0;
        player.isConnected = false;
        player.isDisconnected = true;
    }
    /**
     *  Spiel Verlassen & Zurück in die Lobby laden
     */
    public void SpielVerlassenButton()
    {
        SceneManager.LoadScene("Startup");
        Broadcast("#ZurueckInsHauptmenue");
    }
    /**
     * Sendet aktualisierte Spielerinfos an alle Spieler
     */
    private void UpdateSpielerBroadcast()
    {
        Broadcast(UpdateSpieler(), Config.PLAYERLIST);
    }
    /**
     * Aktualisiert die Spieler Anzeige Informationen & gibt diese als Text zurück
     */
    private string UpdateSpieler()
    {
        string msg = "#UpdateSpieler [ID]0[ID][PUNKTE]" + Config.SERVER_PLAYER_POINTS + "[PUNKTE]";
        int connectedplayer = 0;
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            Player p = Config.PLAYERLIST[i];
            msg += "[TRENNER][ID]" + p.id + "[ID][PUNKTE]" + p.points + "[PUNKTE]";
            if (p.isConnected && PlayerConnected[i])
            {
                connectedplayer++;
                SpielerAnzeige[i, 0].SetActive(true);
                SpielerAnzeige[i, 2].GetComponent<Image>().sprite = p.icon;
                SpielerAnzeige[i, 4].GetComponent<TMP_Text>().text = p.name;
                SpielerAnzeige[i, 5].GetComponent<TMP_Text>().text = p.points+"";
            }
            else
                SpielerAnzeige[i, 0].SetActive(false);

        }
        // Server 
        FalscheAntworten.GetComponent<TMP_Text>().text = "Falsche Antworten: "+Config.SERVER_PLAYER_POINTS;
        return msg;
    }
    /**
     * Initialisiert die Anzeigen zu beginn
     */
    private void InitAnzeigen()
    {
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
        Config.SERVER_PLAYER_POINTS = 0;
        FalscheAntworten.GetComponent<TMP_Text>().text = "Falsche Antworten: "+Config.SERVER_PLAYER_POINTS;
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

        // Schätzfragen
        //if (GameObject.Find("SchaetzfragenAnimation") != null)
            //GameObject.Find("SchaetzfragenAnimation").SetActive(false);
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
                GameObject.Find("SchaetzfragenAnimation/Spieler/Spieler (" + p.id + ")").GetComponent<Image>().sprite = p.icon;
            }
            else
            {
                GameObject.Find("SchaetzfragenAnimation/Spieler/Spieler (" + p.id + ")").gameObject.SetActive(false);
            }
        }
    }

    #region Quiz Fragen Anzeige
    /**
     * Initialisiert die Anzeigen des Quizzes
     */
    private void InitQuiz()
    {
        aktuelleFrage = 0;
        GameObject.Find("QuizAnzeigen/Titel").GetComponent<TMP_Text>().text = Config.QUIZ_SPIEL.getSelected().getTitel();
        GameObject.Find("QuizAnzeigen/FragenIndex2").GetComponentInChildren<TMP_Text>().text = "";
        GameObject.Find("Frage").GetComponentInChildren<TMP_Text>().text = "";
        if (Config.QUIZ_SPIEL.getSelected().getFragenCount() > 0)
        {
            LoadQuestionIntoScene(0);
        }
    }
    /**
     * Ändert das ausgewählte Quiz
     */
    public void ChangeQuiz(TMP_Dropdown drop)
    {
        // Wählt neues Quiz aus
        Config.QUIZ_SPIEL.setSelected(Config.QUIZ_SPIEL.getQuizByIndex(drop.value));
        Logging.add(Logging.Type.Normal, "QuizServer", "ChangeQuiz", "Quiz starts: " + Config.QUIZ_SPIEL.getSelected().getTitel());
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
    /**
     * Navigiert durch die Fragen, zeigt/versteckt diese
     */
    public void NavigateThroughQuestions(string type)
    {
        switch (type)
        {
            default:
                Debug.LogError("NavigateThroughQuestions: unbekannter Typ");
                Logging.add(Logging.Type.Error, "QuizServer", "NavigateThroughQuestions", "unbekannter Typ -> " + type);
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
                GameObject.Find("Frage").GetComponentInChildren<TMP_Text>().text = Config.QUIZ_SPIEL.getSelected().getFrage(aktuelleFrage).getFrage();
                GameObject.Find("QuizAnzeigen/FragenIndex1").GetComponentInChildren<TMP_Text>().text = (aktuelleFrage + 1) + "/" + Config.QUIZ_SPIEL.getSelected().getFragenCount();
                Broadcast("#FragenAnzeige [BOOL]" + FragenAnzeige.activeInHierarchy + "[BOOL][FRAGE]" + Frage.GetComponentInChildren<TMP_Text>().text);
                break;
            case "clear":
                GameObject.Find("Frage").GetComponentInChildren<TMP_Text>().text = "";
                GameObject.Find("QuizAnzeigen/FragenIndex1").GetComponentInChildren<TMP_Text>().text = "";
                Broadcast("#FragenAnzeige [BOOL]" + FragenAnzeige.activeInHierarchy + "[BOOL][FRAGE] ");
                break;
        }
    }
    /**
     * Lädt die Frage nach Index, für die Server Vorschau
     */
    private void LoadQuestionIntoScene(int index)
    {
        GameObject.Find("QuizAnzeigen/FragenVorschau").GetComponent<TMP_Text>().text = "Frage:\n"+Config.QUIZ_SPIEL.getSelected().getFrage(index).getFrage();
        GameObject.Find("QuizAnzeigen/AntwortVorschau").GetComponent<TMP_Text>().text = "Antwort:\n"+ Config.QUIZ_SPIEL.getSelected().getFrage(index).getAntwort();
        GameObject.Find("QuizAnzeigen/InfoVorschau").GetComponent<TMP_Text>().text = "Info:\n"+Config.QUIZ_SPIEL.getSelected().getFrage(index).getInfo();
        GameObject.Find("QuizAnzeigen/FragenIndex2").GetComponentInChildren<TMP_Text>().text = (aktuelleFrage+1)+"/" + Config.QUIZ_SPIEL.getSelected().getFragenCount();
    }
    #endregion
    #region Buzzer
    /**
     * Aktiviert/Deaktiviert den Buzzer für alle Spieler
     */
    public void BuzzerAktivierenToggle(Toggle toggle)
    {
        buzzerIsOn = toggle.isOn;
        BuzzerAnzeige.SetActive(toggle.isOn);
    }
    /**
     * Spielt Sound ab, sperrt den Buzzer und zeigt den Spieler an
     */
    private void SpielerBuzzered(Player p)
    {
        if (!buzzerIsOn)
            return;
        buzzerIsOn = false;
        Broadcast("#AudioBuzzerPressed " + p.id);
        BuzzerSound.Play();
        SpielerAnzeige[p.id - 1, 1].SetActive(true);
    }
    /**
     * Gibt den Buzzer für alle Spieler frei
     */
    public void SpielerBuzzerFreigeben()
    {
        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
            SpielerAnzeige[i, 1].SetActive(false);
        buzzerIsOn = BuzzerAnzeige.activeInHierarchy;
        Broadcast("#BuzzerFreigeben");
    }
    #endregion
    #region Spieler Ausgetabt Anzeige
    /**
     * Austaben wird allen/keinen Spielern angezeigt
     */
    public void AustabenAllenZeigenToggle(Toggle toggle)
    {
        AustabbenAnzeigen.SetActive(toggle.isOn);
        if (toggle.isOn == false)
            Broadcast("#SpielerAusgetabt 0");
    }
    /**
     * Spieler Tabt aus, wird ggf allen gezeigt
     */
    private void ClientFocusChange(Player player, string data)
    {
        bool ausgetabt = !Boolean.Parse(data);
        SpielerAnzeige[(player.id - 1), 3].SetActive(ausgetabt); // Ausgetabt Einblednung
        if (AustabbenAnzeigen.activeInHierarchy)
            Broadcast("#SpielerAusgetabt " + player.id + " " + ausgetabt);
    }
    #endregion
    #region Frage
    /**
     * Zeigt/Versteckt die Frage für alle Spieler
     */
    public void FrageAnzeigenToggle(Toggle toggle)
    {
        FragenAnzeige.SetActive(toggle.isOn);
        Broadcast("#FragenAnzeige [BOOL]"+toggle.isOn+"[BOOL][FRAGE]"+Frage.GetComponentInChildren<TMP_Text>().text);
    }
    /**
     * Blendet die selbst eingegebene Frage ein
     */
    public void EigeneFrageEinblenden(TMP_InputField input)
    {
        Frage.GetComponentInChildren<TMP_Text>().text = input.text;
        if (FragenAnzeige.activeInHierarchy)
            Broadcast("#FragenAnzeige [BOOL]" + FragenAnzeige.activeInHierarchy + "[BOOL][FRAGE]" + input.text);
        input.text = "";
    }
    #endregion
    #region Textantworten der Spieler
    /**
     * Blendet die Texteingabe für die Spieler ein
     */
    public void TexteingabeAnzeigenToggle(Toggle toggle)
    {
        TextEingabeAnzeige.SetActive(toggle.isOn);
        Broadcast("#TexteingabeAnzeigen "+ toggle.isOn);
    }
    /**
    * Aktualisiert die Antwort die der Spieler eingibt
    */
    public void SpielerAntwortEingabe(Player p, string data)
    {
        SpielerAnzeige[p.id - 1, 6].GetComponentInChildren<TMP_InputField>().text = data;
    }
    /**
     * Blendet die Textantworten der Spieler ein
     */
    public void TextantwortenAnzeigeToggle(Toggle toggle)
    {
        TextAntwortenAnzeige.SetActive(toggle.isOn);
        if (!toggle.isOn)
        {
            Broadcast("#TextantwortenAnzeigen [BOOL]" + toggle.isOn + "[BOOL]");
            return;
        }
        string msg = "";
        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
        {
            msg = msg + "[ID" + (i + 1) + "]" + SpielerAnzeige[i, 6].GetComponentInChildren<TMP_InputField>().text + "[ID" + (i + 1) + "]";
        }
        Broadcast("#TextantwortenAnzeigen [BOOL]"+toggle.isOn+"[BOOL][TEXT]"+ msg);
    }
    #endregion
    #region Punkte
    /**
     * Punkte Pro Richtige Antworten Anzeigen
     */
    public void ChangePunkteProRichtigeAntwort(TMP_InputField input)
    {
        PunkteProRichtige = Int32.Parse(input.text);
    }
    /**
     * Punkte Pro Falsche Antworten Anzeigen
     */
    public void ChangePunkteProFalscheAntwort(TMP_InputField input)
    {
        PunkteProFalsche = Int32.Parse(input.text);
    }
    
    /**
     * Vergibt an den Spieler Punkte für eine richtige Antwort
     */
    public void PunkteRichtigeAntwort(GameObject player)
    {
        Broadcast("#AudioRichtigeAntwort");
        RichtigeAntwortSound.Play();
        int pId = Int32.Parse(player.transform.parent.parent.name.Replace("Player (", "").Replace(")", ""));
        int pIndex = Player.getPosInLists(pId);
        Config.PLAYERLIST[pIndex].points += PunkteProRichtige;
        UpdateSpielerBroadcast();
    }
    /**
     * Vergibt an alle anderen Spieler Punkte bei einer falschen Antwort
     */
    public void PunkteFalscheAntwort(GameObject player)
    {
        Broadcast("#AudioFalscheAntwort");
        FalscheAntwortSound.Play();
        int pId = Int32.Parse(player.transform.parent.parent.name.Replace("Player (", "").Replace(")", ""));
        foreach (Player p in Config.PLAYERLIST)
        {
            if (pId != p.id && p.isConnected)
                p.points += PunkteProFalsche;
        }
        Config.SERVER_PLAYER_POINTS += PunkteProFalsche;
        UpdateSpielerBroadcast();
    }
    /**
     * Ändert die Punkte des Spielers (+-1)
     */
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
    /**
     * Ändert die Punkte des Spielers, variable Punkte
     */
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
    /**
     * Aktiviert den Icon Rand beim Spieler
     */
    public void SpielerIstDran(GameObject button)
    {
        int pId = Int32.Parse(button.transform.parent.parent.name.Replace("Player (", "").Replace(")", ""));
        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
            SpielerAnzeige[(pId - 1), 1].SetActive(false);
        SpielerAnzeige[(pId - 1), 1].SetActive(true);
        buzzerIsOn = false;
        Broadcast("#SpielerIstDran "+pId);
    }
    /**
     * Versteckt den Icon Rand beim Spieler
     */
    public void SpielerIstNichtDran(GameObject button)
    {
        int pId = Int32.Parse(button.transform.parent.parent.name.Replace("Player (", "").Replace(")", ""));
        SpielerAnzeige[(pId - 1), 1].SetActive(false);

        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
            if (SpielerAnzeige[i, 1].activeInHierarchy)
                return;
        buzzerIsOn = BuzzerAnzeige.activeInHierarchy; // Buzzer wird erst aktiviert wenn keiner mehr dran ist
        Broadcast("#SpielerIstNichtDran " + pId);
    }
    #endregion

    #region Schätzfragen Animation
    public void toggleZielAnzeige(Toggle toggle)
    {
        SchaetzfragenAnzeige[2].SetActive(toggle.isOn);
        Broadcast("#AnimationZiel "+ toggle.isOn);
    }
    public void updateGrenzen()
    {
        SchaetzfragenAnzeige[1].GetComponentInChildren<TMP_Text>().text = GameObject.Find("SchaetzfragenAnimation/MinGrenzeFestlegen").GetComponent<TMP_InputField>().text + GameObject.Find("SchaetzfragenAnimation/EinheitAngeben").GetComponent<TMP_InputField>().text;
        SchaetzfragenAnzeige[2].GetComponentInChildren<TMP_Text>().text = GameObject.Find("SchaetzfragenAnimation/ZielGrenzeFestlegen").GetComponent<TMP_InputField>().text + GameObject.Find("SchaetzfragenAnimation/EinheitAngeben").GetComponent<TMP_InputField>().text;
        SchaetzfragenAnzeige[3].GetComponentInChildren<TMP_Text>().text = GameObject.Find("SchaetzfragenAnimation/MaxGrenzeFestlegen").GetComponent<TMP_InputField>().text + GameObject.Find("SchaetzfragenAnimation/EinheitAngeben").GetComponent<TMP_InputField>().text;
        SchaetzfragenAnzeige[2].SetActive(GameObject.Find("SchaetzfragenAnimation/ZielAnzeigen").GetComponent<Toggle>().isOn);
    }
    public void zeigeAnimationAn()
    {
        SchaetzfragenAnzeige[0].SetActive(true);
        SchaetzfragenAnimationController.SetActive(false);
        // Zeigt die Spielerschätzungen an
        foreach (Player p in Config.PLAYERLIST)
        {
            if (p.isConnected)
            {
                SchaetzfragenAnzeige[(4 + 2 * (p.id - 1))].transform.GetChild(1).GetComponent<TMP_Text>().text = GameObject.Find("SchaetzfragenAnimation/Spieler/Spieler (" + p.id + ")").transform.GetChild(0).GetComponent<TMP_InputField>().text + GameObject.Find("SchaetzfragenAnimation/EinheitAngeben").GetComponent<TMP_InputField>().text;
                SchaetzfragenAnzeige[(4 + 2 * (p.id - 1))].transform.GetChild(3).gameObject.SetActive(false);
            }
        }
        BerechneSchritteProEinheit();


        // Zeigt Sieger für Server an
        string einheit = GameObject.Find("SchaetzfragenAnimation/EinheitAngeben").GetComponent<TMP_InputField>().text;
        float schatung = float.Parse(SchaetzfragenAnzeige[4].GetComponentInChildren<TMP_Text>().text.Replace(einheit , ""));
        float ziel = float.Parse(SchaetzfragenAnzeige[2].GetComponentInChildren<TMP_Text>().text.Replace(einheit, ""));
        float diff = Math.Abs(schatung - ziel);
        foreach (Player p in Config.PLAYERLIST)
        {
            if (!SchaetzfragenAnzeige[(4 + 2 * (p.id - 1))].activeInHierarchy)
                continue;
            float spieler = float.Parse(SchaetzfragenAnzeige[(4 + 2 * (p.id - 1))].GetComponentInChildren<TMP_Text>().text.Replace(einheit, ""));
            float spielerdiff = Math.Abs(spieler - ziel);
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
            float spieler = float.Parse(SchaetzfragenAnzeige[(4 + 2 * (p.id - 1))].GetComponentInChildren<TMP_Text>().text.Replace(einheit, ""));
            float spielerdiff = Math.Abs(spieler - ziel);
            if (spielerdiff == diff)
                SchaetzfragenAnzeige[(4 + 2 * (p.id - 1))].transform.GetChild(3).gameObject.SetActive(true);
        }
    }
   
    public void BerechneSchritteProEinheit()
    {
        float StartWert = float.Parse(SchaetzfragenAnzeige[1].GetComponentInChildren<TMP_Text>().text.Replace(GameObject.Find("SchaetzfragenAnimation/EinheitAngeben").GetComponent<TMP_InputField>().text, ""));
        float ZielWert = float.Parse(SchaetzfragenAnzeige[2].GetComponentInChildren<TMP_Text>().text.Replace(GameObject.Find("SchaetzfragenAnimation/EinheitAngeben").GetComponent<TMP_InputField>().text, ""));
        float MaxWert = float.Parse(SchaetzfragenAnzeige[3].GetComponentInChildren<TMP_Text>().text.Replace(GameObject.Find("SchaetzfragenAnimation/EinheitAngeben").GetComponent<TMP_InputField>().text, ""));
        float StartPosition = SchaetzfragenAnzeige[1].transform.localPosition.x;
        float MaxPosition = SchaetzfragenAnzeige[3].transform.localPosition.x;
        float DifftoNull = Math.Abs(StartPosition);
        float DiffToMax = MaxPosition - StartPosition;
        float WertToMax = DiffToMax / (MaxWert-StartWert);
        float spielerwert = 0;

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
                spielerwert = float.Parse(GameObject.Find("SchaetzfragenAnimation/Grid/Icon (" + p.id + ")").GetComponentInChildren<TMP_Text>().text.Replace(GameObject.Find("SchaetzfragenAnimation/EinheitAngeben").GetComponent<TMP_InputField>().text, ""));
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
        Broadcast("#AnimationInfo " + data_text.Replace("[SPIELER_WERT]", "|").Split('|')[0] + broadcastmsg);
    }

    public void AnimationStarten()
    {
        Broadcast("#AnimationStart");
        SchaetzfragenAnimationController.SetActive(true);
    }
    public void AnimationBeenden()
    {
        Broadcast("#AnimationBeenden");
        SchaetzfragenAnzeige[0].SetActive(false);
        SchaetzfragenAnimationController.SetActive(false);
    }
    #endregion
}
