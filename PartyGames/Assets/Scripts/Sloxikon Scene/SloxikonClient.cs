using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SloxikonClient : MonoBehaviour
{
    GameObject SpielerAntwortEingabe;
    GameObject[,] SpielerAnzeige;
    bool pressingbuzzer = false;

    GameObject Thema;
    GameObject[] Antworten;
    Coroutine TimerCoroutine;

    GameObject Timer;

    [SerializeField] AudioSource BuzzerSound;
    [SerializeField] AudioSource RichtigeAntwortSound;
    [SerializeField] AudioSource FalscheAntwortSound;
    [SerializeField] AudioSource Moeoep;
    [SerializeField] AudioSource Beeep;

    void OnEnable()
    {
        if (!Config.CLIENT_STARTED)
            return;

        InitAnzeigen();
        SendToServer("#JoinSloxikon");

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
        SendToServer("#ClientFocusChange " + focus);
    }

    private void OnApplicationQuit()
    {
        Logging.log(Logging.LogType.Normal, "SloxikonClient", "OnApplicationQuit", "Client wird geschlossen.");
        SendToServer("#ClientClosed");
        CloseSocket();
    }

    /// <summary>
    /// Testet die Verbindung zum Server
    /// </summary>
    /// <returns></returns>
    IEnumerator TestConnectionToServer()
    {
        while (Config.CLIENT_STARTED)
        {
            SendToServer("#TestConnection");
            yield return new WaitForSeconds(10);
        }
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

        Logging.log(Logging.LogType.Normal, "SloxikonClient", "CloseSocket", "Verbindung zum Server wurde getrennt. Client wird in das Hauptmenü geladen.");
    }
    #endregion
    #region Kommunikation
    /// <summary>
    /// Sendet eine Nachricht an den Server.
    /// </summary>
    /// <param name="data"></param>
    public void SendToServer(string data)
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
            Logging.log(Logging.LogType.Warning, "SloxikonClient", "SendToServer", "Nachricht an Server konnte nicht gesendet werden.", e);
            Config.HAUPTMENUE_FEHLERMELDUNG = "Verbindung zum Server wurde verloren.";
            CloseSocket();
            SceneManager.LoadSceneAsync("StartUp");
        }
    }
    /// <summary>
    /// Einkommende Nachrichten die vom Sever
    /// </summary>
    /// <param name="data"></param>
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
    /// <param name="data"></param>
    /// <param name="cmd"></param>
    public void Commands(string data, string cmd)
    {
        Logging.log(Logging.LogType.Debug, "SloxikonClient", "Commands", "Eingehende Nachricht: " + cmd + " -> " + data);
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "SloxikonClient", "Commands", "Unkown Command: " + cmd + " -> " + data);
                break;

            #region Universal Commands
            case "#ServerClosed":
                Logging.log(Logging.LogType.Normal, "SloxikonClient", "Commands", "Verbindung zum Server wurde getrennt");
                CloseSocket();
                SceneManager.LoadSceneAsync("Startup");
                break;
            case "#UpdateRemoteConfig":
                Logging.log(Logging.LogType.Normal, "SloxikonClient", "Commands", "RemoteConfig wird aktualisiert");
                LoadConfigs.FetchRemoteConfig();
                break;
            case "#ZurueckInsHauptmenue":
                Logging.log(Logging.LogType.Debug, "SloxikonClient", "Commands", "Spiel wurde beendet, lade ins Hauptmenü.");
                SceneManager.LoadSceneAsync("Startup");
                break;
            #endregion

            case "#UpdateSpieler":
                UpdateSpieler(data);
                break;
            case "#SpielerAusgetabt":
                SpielerAusgetabt(data);
                break;
            case "#TexteingabeAnzeigen":
                TexteingabeAnzeigen(data);
                break;
            case "#TextantwortenAnzeigen":
                TextantwortenAnzeigen(data);
                break;
            case "#SpielerIstDran":
                SpielerIstDran(data);
                break;
            case "#SpielerIstNichtDran":
                SpielerIstNichtDran(data);
                break;
            case "#AudioBuzzerPressed":
                AudioBuzzerPressed(data);
                break;
            case "#AudioRichtigeAntwort":
                AudioRichtigeAntwort();
                break;
            case "#AudioFalscheAntwort":
                AudioFalscheAntwort();
                break;
            case "#BuzzerFreigeben":
                BuzzerFreigeben();
                break;

            case "#SloxikonShowTitel":
                ShowTitel(data);
                break;
            case "#SloxikonHideAll":
                HideAll();
                break;
            case "#SloxikonShowThema":
                ShowThema(data);
                break;
            case "#SloxikonShowAllAntworten":
                ShowAllAntworten(data);
                break;
            case "#SloxikonShowAntwort":
                ShowAntwort(data);
                break;
            case "#SloxikonShowOwner":
                ShowOwner(data);
                break;
            case "#SloxikonPlayerSelectedAnswer":
                PlayerSelectAnswer(data);
                break;
            case "#SloxikonTimerStarten":
                TimerStarten(data);
                break;
            case "#SloxikonTimerStop":
                TimerStop();
                break;
        } 
    }
    /// <summary>
    /// Initialisiert die Anzeigen der Scene
    /// </summary>
    private void InitAnzeigen()
    {
        Logging.log(Logging.LogType.Debug, "SloxikonClient", "InitAnzeigen", "Initialisiert die Anzeigen");
        // Spieler Texteingabe
        SpielerAntwortEingabe = GameObject.Find("SpielerAntwortEingabe");
        SpielerAntwortEingabe.SetActive(true);
        // Spieler Anzeige
        SpielerAnzeige = new GameObject[Config.SERVER_MAX_CONNECTIONS, 7]; // Anzahl benötigter Elemente
        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
        {
            SpielerAnzeige[i, 0] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")"); // Spieler Anzeige
            SpielerAnzeige[i, 1] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/BuzzerPressed"); // BuzzerPressed Umrandung
            SpielerAnzeige[i, 2] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Icon"); // Spieler Icon
            SpielerAnzeige[i, 3] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Ausgetabt"); // Ausgetabt Einblednung
            SpielerAnzeige[i, 4] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Infobar/Name"); // Spieler Name
            SpielerAnzeige[i, 5] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Infobar/Punkte"); // Spieler Punkte
            SpielerAnzeige[i, 6] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/SpielerAntwort"); // Spieler Antwort

            try
            {
                GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/ServerControl").SetActive(false); // Server Control für Spieler ausblenden
            }
            catch {}
            SpielerAnzeige[i, 0].SetActive(false);
            SpielerAnzeige[i, 1].SetActive(false);
            SpielerAnzeige[i, 3].SetActive(false);
            SpielerAnzeige[i, 6].SetActive(false);
        }


        // Init Sloxikon
        GameObject.Find("Sloxikon/Titel").GetComponent<TMP_Text>().text = "";
        Thema = GameObject.Find("Sloxikon/Thema");
        Thema.SetActive(false);
        Antworten = new GameObject[9];
        for (int i = 0; i < 9; i++)
        {
            Antworten[i] = GameObject.Find("Sloxikon/Grid/Answer (" + (i + 1) + ")");
            Antworten[i].SetActive(false);
            Antworten[i].transform.GetChild(0).gameObject.SetActive(false);
            Antworten[i].transform.GetChild(1).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/ProfileIcons/empty");
            Antworten[i].transform.GetChild(2).GetComponentInChildren<TMP_InputField>().text = "";
            GameObject grid = Antworten[i].transform.GetChild(3).gameObject;

            for (int j = 0; j < 8; j++)
            {
                GameObject child = grid.transform.GetChild(j).gameObject;
                if (Config.PLAYERLIST[j].name.Length > 1)
                {
                    child.GetComponent<Image>().sprite = Config.PLAYERLIST[j].icon;
                    child.GetComponent<Image>().color = new Color(255, 255, 255, 1f);
                    child.gameObject.SetActive(false);
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
    }
    /// <summary>
    /// Blendet den Spieltitel ein
    /// </summary>
    /// <param name="data"></param>
    private void ShowTitel(string data)
    {
        Logging.log(Logging.LogType.Debug, "SloxikonClient", "ShowTitel", "Zeige Titel: " + data);
        GameObject.Find("Sloxikon/Titel").GetComponent<TMP_Text>().text = data;
    }
    /// <summary>
    /// Versteckt die Themenanzeigen
    /// </summary>
    private void HideAll()
    {
        Logging.log(Logging.LogType.Debug, "SloxikonClient", "HideAll", "Zeige blende Anzeigen aus");
        Thema.SetActive(false);
        // Falls jemand disconnected alle Anzeigen ausblenden
        for (int i = 0; i < Config.PLAYERLIST.Length + 1; i++)
        {
            Antworten[i].SetActive(false);
        }
    }
    /// <summary>
    /// Blendet das aktuelle Thema ein
    /// </summary>
    /// <param name="data"></param>
    private void ShowThema(string data)
    {
        Logging.log(Logging.LogType.Debug, "SloxikonClient", "ShowThema", "Zeigt Thema an: " + data);
        string thema = data.Split('|')[0];
        int anzahl = Int32.Parse(data.Split('|')[1]);

        Thema.SetActive(true);
        Thema.GetComponentInChildren<TMP_InputField>().text = thema;

        // Falls jemand disconnected alle Anzeigen ausblenden
        for (int i = 0; i < Config.PLAYERLIST.Length + 1; i++)
        {
            Antworten[i].SetActive(false);
        }
        // Anzeigen für verbundene Spieler einblenden
        for (int i = 0; i < Config.PLAYERLIST.Length + 1; i++)
        {
            if (i < anzahl)
            {
                Antworten[i].SetActive(true);
                Antworten[i].transform.GetChild(1).gameObject.SetActive(false);
                Antworten[i].transform.GetChild(2).gameObject.SetActive(true);
                Antworten[i].transform.GetChild(2).GetComponentInChildren<TMP_InputField>().text = "";
            }
            else
            {
                Antworten[i].SetActive(false);
            }
        }

        // Blendet spielerselect aus
        for (int j = 0; j < Config.PLAYERLIST.Length; j++)
        {
            if (Config.PLAYERLIST[j].name.Length > 1)
            {
                for (int i = 0; i < Config.PLAYERLIST.Length + 1; i++)
                {
                    Antworten[i].transform.GetChild(3).GetChild(j).GetComponent<Button>().image.sprite = Config.PLAYERLIST[j].icon;
                    Antworten[i].transform.GetChild(3).GetChild(j).GetComponent<Button>().image.color = new Color(255, 255, 255, 1f);
                    Antworten[i].transform.GetChild(3).GetChild(j).GetComponent<Button>().interactable = false;
                    Antworten[i].transform.GetChild(3).GetChild(j).gameObject.SetActive(false);
                }
            }
        }
    }
    /// <summary>
    /// Blendet alle Antwortmöglichkeiten ein
    /// </summary>
    /// <param name="data"></param>
    private void ShowAllAntworten(string data)
    {
        Logging.log(Logging.LogType.Debug, "SloxikonClient", "ShowAllAntworten", "Zeigt alle Antwortmöglichkeiten: " + data);
        int anz = Int32.Parse(data.Split('|')[0]);
        string msg = data.Split('|')[1];

        for (int i = 0; i < anz; i++)
        {
            Antworten[i].transform.GetChild(2).GetComponentInChildren<TMP_InputField>().text = msg.Replace("[" + i + "]", "|").Split('|')[1];
            Antworten[i].transform.GetChild(0).GetChild(0).GetComponent<Button>().interactable = false;
            Antworten[i].transform.GetChild(0).GetChild(0).gameObject.SetActive(true);
        }
    }
    /// <summary>
    /// Blendet eine Antwortmöglichkeit ein
    /// </summary>
    /// <param name="data"></param>
    private void ShowAntwort(string data)
    {
        Logging.log(Logging.LogType.Debug, "SloxikonClient", "ShowAntwort", "Zeigt Antwortmöglichkeit: " + data);
        int pid = Int32.Parse(data.Split('|')[0]);
        string antwort = data.Split('|')[1];

        Antworten[pid].transform.GetChild(2).GetComponentInChildren<TMP_InputField>().text = antwort;
        Antworten[pid].SetActive(true);
    }
    /// <summary>
    /// Zeigt den Autor der Antwortmöglichkeit an
    /// </summary>
    /// <param name="data"></param>
    private void ShowOwner(string data)
    {
        Logging.log(Logging.LogType.Debug, "SloxikonClient", "ShowOwner", "Zeigt Autor der Antwortmöglichkeit: " + data);
        int pid = Int32.Parse(data.Split('|')[0]);
        string spritename = data.Split('|')[1];

        Antworten[pid].transform.GetChild(1).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/ProfileIcons/"+spritename);
        Antworten[pid].transform.GetChild(1).gameObject.SetActive(true);
        Antworten[pid].SetActive(true);
    }
    /// <summary>
    /// Startet den Timer und bricht den alten, falls dieser noch läuft, ab
    /// </summary>
    private void TimerStarten(string data)
    {
        Logging.log(Logging.LogType.Debug, "SloxikonClient", "TimerStarten", "Startet den Timer");

        if (TimerCoroutine != null)
            StopCoroutine(TimerCoroutine);
        int sekunden = Int32.Parse(data);
        TimerCoroutine = StartCoroutine(RunTimer(sekunden));
    }
    /// <summary>
    /// Beendet den Timer
    /// </summary>
    private void TimerStop()
    {
        Logging.log(Logging.LogType.Debug, "SloxikonClient", "TimerStop", "Beendet den Timer");
        Timer.SetActive(false);
        if (TimerCoroutine != null)
        {
            StopCoroutine(TimerCoroutine);
            TimerCoroutine = null;
        }
    }
    /// <summary>
    /// Timer läuft
    /// </summary>
    /// <param name="seconds">Dauer</param>
    IEnumerator RunTimer(int seconds)
    {
        Logging.log(Logging.LogType.Debug, "SloxikonClient", "RunTimer", "Timer läuft: " + seconds);
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
    /// <summary>
    /// Spieler wählt Antwortmöglichkeit ein
    /// </summary>
    /// <param name="data">Spieler & Antwortmöglichkeit</param>
    private void PlayerSelectAnswer(string data)
    {
        Logging.log(Logging.LogType.Debug, "SloxikonClient", "PlayerSelectAnswer", "Spieler wählt Antwortmöglichkeit: " + data);
        int answer = Int32.Parse(data.Split('|')[0]);
        int pid = Int32.Parse(data.Split('|')[1]);

        for (int i = 0; i < Config.PLAYERLIST.Length + 1; i++)
        {
            Antworten[i].transform.GetChild(3).GetChild(pid).GetComponent<Button>().image.sprite = Config.PLAYERLIST[pid].icon;
            Antworten[i].transform.GetChild(3).GetChild(pid).GetComponent<Button>().image.color = new Color(255, 255, 255, 0.5f);
            Antworten[i].transform.GetChild(3).GetChild(pid).GetComponent<Button>().interactable = false;
            Antworten[i].transform.GetChild(3).GetChild(pid).gameObject.SetActive(false);
        }
        Antworten[answer].transform.GetChild(3).GetChild(pid).GetComponent<Button>().image.color = new Color(255, 255, 255, 1f);
        Antworten[answer].transform.GetChild(3).GetChild(pid).GetComponent<Button>().interactable = false;
        Antworten[answer].transform.GetChild(3).GetChild(pid).gameObject.SetActive(true);
    }
    /// <summary>
    /// Aktualisiert die Spieler Anzeigen
    /// </summary>
    /// <param name="data"></param>
    private void UpdateSpieler(string data)
    {
        string[] player = data.Replace("[TRENNER]", "|").Split('|');
        foreach (string sp in player)
        {
            int pId = Int32.Parse(sp.Replace("[ID]", "|").Split('|')[1]);

            // Display ServerInfos
            if (pId == 0)
            {
            }
            // Display Client Infos
            else
            {
                int pos = Player.getPosInLists(pId);
                // Update PlayerInfos
                //Config.PLAYERLIST[pos].name = sp.Replace("[NAME]", "|").Split('|')[1];
                Config.PLAYERLIST[pos].points = Int32.Parse(sp.Replace("[PUNKTE]", "|").Split('|')[1]);
                //Config.PLAYERLIST[pos].icon = Resources.Load<Sprite>("Images/ProfileIcons/" + sp.Replace("[ICON]", "|").Split('|')[1]);
                // Display PlayerInfos                
                SpielerAnzeige[pos, 2].GetComponent<Image>().sprite = Config.PLAYERLIST[pos].icon;
                SpielerAnzeige[pos, 4].GetComponent<TMP_Text>().text = Config.PLAYERLIST[pos].name;
                SpielerAnzeige[pos, 5].GetComponent<TMP_Text>().text = Config.PLAYERLIST[pos].points+"";
                // Verbundene Spieler anzeigen
                if (Config.PLAYERLIST[pos].name != "")
                {
                    SpielerAnzeige[pos, 0].SetActive(true);
                }
                else
                {
                    SpielerAnzeige[pos, 0].SetActive(false);
                }
            }
        }
    }
    /// <summary>
    /// Sendet eine Buzzer Anfrage an den Server
    /// </summary>
    public void SpielerBuzzered()
    {
        SendToServer("#SpielerBuzzered");
    }
    /// <summary>
    /// Gibt den Buzzer frei
    /// </summary>
    private void BuzzerFreigeben()
    {
        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
            SpielerAnzeige[i, 1].SetActive(false);
    }
    /// <summary>
    /// Spielt Sound des Buzzers ab und zeigt welcher Spieler diesen gedrückt hat
    /// </summary>
    /// <param name="data"></param>
    private void AudioBuzzerPressed(string data)
    {
        BuzzerSound.Play();
        int pId = Int32.Parse(data);
        SpielerAnzeige[Player.getPosInLists(pId), 1].SetActive(true);
    }
    /// <summary>
    /// Zeigt an, welcher Spieler dran ist
    /// </summary>
    /// <param name="data"></param>
    private void SpielerIstDran(string data)
    {
        int pId = Int32.Parse(data);
        SpielerAnzeige[Player.getPosInLists(pId), 1].SetActive(true);
    }
    /// <summary>
    /// Deaktiviert die Spieler ist dran anzeige
    /// </summary>
    /// <param name="data"></param>
    private void SpielerIstNichtDran(string data)
    {
        int pId = Int32.Parse(data);
        SpielerAnzeige[Player.getPosInLists(pId), 1].SetActive(false);
    }
    /// <summary>
    /// Spielt den Sound für eine richtige Antwort ab
    /// </summary>
    private void AudioRichtigeAntwort()
    {
        RichtigeAntwortSound.Play();
    }
    /// <summary>
    /// Spielt den Sound für eine falsche Antwort ab
    /// </summary>
    private void AudioFalscheAntwort()
    {
        FalscheAntwortSound.Play();
    }
    /// <summary>
    /// Zeigt an, ob ein Spieler austabt
    /// </summary>
    /// <param name="data"></param>
    private void SpielerAusgetabt(string data)
    {
        // AustabenAnzeigen wird deaktiviert
        if (data == "0")
        {
            for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
            {
                SpielerAnzeige[i, 3].SetActive(false);
            }
        }
        // Austaben für Spieler anzeigen
        else
        {
            int id = Int32.Parse(data.Split(' ')[0]);
            int pos = Player.getPosInLists(id);
            bool type = Boolean.Parse(data.Split(' ')[1]);

            SpielerAnzeige[pos, 3].SetActive(type);
        }
    }
    /// <summary>
    /// Zeigt das Texteingabefeld an
    /// </summary>
    /// <param name="data"></param>
    private void TexteingabeAnzeigen(string data)
    {
        bool anzeigen = Boolean.Parse(data);
        if (anzeigen) {
            SpielerAntwortEingabe.SetActive(true);
            SpielerAntwortEingabe.GetComponentInChildren<TMP_InputField>().text = "";
        }
        else
            SpielerAntwortEingabe.SetActive(false);
    }
    /// <summary>
    /// Zeigt die Textantworten aller Spieler an
    /// </summary>
    /// <param name="data"></param>
    private void TextantwortenAnzeigen(string data)
    {
        bool anzeigen = Boolean.Parse(data.Replace("[BOOL]", "|").Split('|')[1]);
        if (!anzeigen)
        {
            for (int i = 1; i <= Config.SERVER_MAX_CONNECTIONS; i++)
            {
                SpielerAnzeige[Player.getPosInLists(i), 6].SetActive(false);
            }
            return;
        }
        string text = data.Replace("[TEXT]", "|").Split('|')[1];
        for (int i = 1; i <= Config.SERVER_MAX_CONNECTIONS; i++)
        {
            SpielerAnzeige[Player.getPosInLists(i), 6].SetActive(true);
            SpielerAnzeige[Player.getPosInLists(i), 6].GetComponentInChildren<TMP_InputField>().text = text.Replace("[ID"+i+"]","|").Split('|')[1];
        }
    }
    /// <summary>
    /// Sendet die Antworteingabe an den Server
    /// </summary>
    /// <param name="input"></param>
    public void SpielerAntwortEingabeInput(TMP_InputField input)
    {
        SendToServer("#SpielerAntwortEingabe "+input.text);
    }
}