using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WerBietetMehrClient : MonoBehaviour
{
    GameObject Titel;
    GameObject Timer;
    GameObject Anzahl;
    GameObject[] Elemente;
    GameObject Kreuz1;
    GameObject Kreuz2;
    GameObject Kreuz3;

    GameObject SpielerAntwortEingabe;
    GameObject[,] SpielerAnzeige;
    bool pressingbuzzer = false;
    Coroutine timerCoroutine;

    [SerializeField] AudioSource BuzzerSound;
    [SerializeField] AudioSource RichtigeAntwortSound;
    [SerializeField] AudioSource FalscheAntwortSound;
    [SerializeField] AudioSource Moeoep;
    [SerializeField] AudioSource Beeep;
    [SerializeField] AudioSource DisconnectSound;

    void OnEnable()
    {
        InitAnzeigen();

        if (!Config.CLIENT_STARTED)
            return;

        ClientUtils.SendToServer("#JoinWerBietetMehr");
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
        Logging.log(Logging.LogType.Normal, "Client", "OnApplicationQuit", "Client wird geschlossen.");
        ClientUtils.SendToServer("#ClientClosed");
        CloseSocket();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    /// <summary>
    /// Testet die Verbindung zum Server alle 10 Sekunden. 
    /// Beendet den Server, sobald die Verbindung getrennt wurde.
    /// </summary>
    IEnumerator TestConnectionToServer()
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrClient", "TestConnectionToServer", "Testet ab sofort die Verbindumg zum Server.");
        while (Config.CLIENT_STARTED)
        {
            ClientUtils.SendToServer("#TestConnection");
            yield return new WaitForSeconds(10);
        }
        yield break;
    }
    /// <summary>
    /// Lässt den Timer ablaufen
    /// </summary>
    /// <param name="seconds">Dauer des Timers</param>
    IEnumerator RunTimer(int seconds)
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrClient", "RunTimer", "Timer wird gestartet: " + seconds);
        Timer.SetActive(true);

        while (seconds >= 0)
        {
            Timer.GetComponent<TMP_Text>().text = "" + seconds;

            if (seconds == 0)
            {
                Debug.Log(seconds);
                Beeep.Play();
            }
            // Moep Sound bei sekunden
            if (seconds == 1 || seconds == 2 || seconds == 3 || seconds == 10 || seconds == 30 || seconds == 60) // 10-0
            {
                Debug.Log(seconds);
                Moeoep.Play();
            }
            seconds--;
            yield return new WaitForSecondsRealtime(1);
        }
        Timer.SetActive(false);
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

        Logging.log(Logging.LogType.Normal, "WerBietetMehrClient", "CloseSocket", "Verbindung zum Server wurde getrennt. Client wird in das Hauptmenü geladen.");
    }
    #endregion
    #region Kommunikation
    /// <summary>
    /// Einkommende Nachrichten die vom Sever
    /// </summary>
    /// <param name="data">Eingehende Daten</param>
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
        Logging.log(Logging.LogType.Debug, "WerBietetMehrClient", "Commands", "Eingehende Nachricht vom Server: " + cmd + " -> " + data);
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "WerBietetMehrClient", "Commands", "Unkown Command: " + cmd + " -> " + data);
                break;

            #region Universal Commands
            case "#ServerClosed":
                Logging.log(Logging.LogType.Normal, "WerBietetMehrClient", "Commands", "Verbindung zum Server wurde getrennt.");
                CloseSocket();
                SceneManager.LoadSceneAsync("Startup");
                break;
            case "#UpdateRemoteConfig":
                Logging.log(Logging.LogType.Normal, "WerBietetMehrClient", "Commands", "RemoteConfig wird neugeladen.");
                LoadConfigs.FetchRemoteConfig();
                break;
            case "#ZurueckInsHauptmenue":
                Logging.log(Logging.LogType.Normal, "WerBietetMehrClient", "Commands", "Spiel wird beendet. Lade ins Hauptmenü.");
                SceneManager.LoadSceneAsync("Startup");
                break;
            #endregion
            #region
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
            #endregion
            case "#WMBKreuzeEinblenden":
                WMBKreuzeEinblenden(data);
                break;
            case "#WBMAnzahl":
                WBMAnzahl(data);
                break;
            case "#WBMAnsagen":
                AnsagenElementeEinblenden(data);
                break;
            case "#WBMTitel":
                TitelEinblenden(data);
                break;
            case "#WBMTimerStarten":
                TimerStarten(data);
                break;
            case "#WBMKreuzAusgrauen":
                KreuzeAusgrauen(data);
                break;
            case "#WBMAnsagenAnzahl":
                AnzahlManuellAendern(data);
                break;
            case "#WBMNew":
                ChangeListe();
                break;
            case "#WBMElement":
                ElementAuflösen(data);
                break;
        } 
    }

    /// <summary>
    /// Initialisiert die Anzeigen der Scene
    /// </summary>
    private void InitAnzeigen()
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrClient", "InitAnzeigen", "Initialisiert Anzeigen");
        // Spieler Texteingabe
        SpielerAntwortEingabe = GameObject.Find("Canvas/SpielerAntwortEingabe");
        SpielerAntwortEingabe.SetActive(false);
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
            GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/ServerControl").SetActive(false); // Server

            SpielerAnzeige[i, 0].SetActive(false); // Spieler Anzeige
            SpielerAnzeige[i, 1].SetActive(false); // BuzzerPressed Umrandung
            SpielerAnzeige[i, 3].SetActive(false); // Ausgetabt Einblendung
            SpielerAnzeige[i, 6].SetActive(false); // Spieler Antwort
        }

        Titel = GameObject.Find("WerBietetMehr/Outline/Titel");
        Titel.GetComponent<TMP_Text>().text = "";
        Timer = GameObject.Find("WerBietetMehr/Outline/Timer");
        Timer.SetActive(false);
        Anzahl = GameObject.Find("WerBietetMehr/Outline/Anzahl");
        Anzahl.SetActive(false);
        Elemente = new GameObject[30];
        for (int i = 1; i <= 30; i++)
        {
            Elemente[i - 1] = GameObject.Find("WerBietetMehr/Outline/Elemente/Element (" + i + ")");
            Elemente[i - 1].transform.GetChild(0).gameObject.SetActive(false);
            Elemente[i - 1].transform.GetChild(2).gameObject.GetComponent<TMP_Text>().text = "";
            Elemente[i - 1].transform.GetChild(3).gameObject.SetActive(false);
            Elemente[i - 1].SetActive(false);
        }
        Kreuz1 = GameObject.Find("WerBietetMehr/Outline/Kreuze/Kreuz 1");
        Kreuz1.SetActive(false);
        Kreuz2 = GameObject.Find("WerBietetMehr/Outline/Kreuze/Kreuz 2");
        Kreuz2.SetActive(false);
        Kreuz3 = GameObject.Find("WerBietetMehr/Outline/Kreuze/Kreuz 3");
        Kreuz3.SetActive(false);
    }
    #region
    /// <summary>
    /// Aktualisiert die Spieler Anzeigen
    /// </summary>
    /// <param name="data">#UpdateSpieler <...></param>
    private void UpdateSpieler(string data)
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrClient", "UpdateSpieler", "Aktualisiert Spieleranzeigen: " + data);
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
                bool connected = bool.Parse(sp.Replace("[ONLINE]", "|").Split('|')[1]);
                if (Config.PLAYERLIST[pos].name != "" && connected)
                {
                    SpielerAnzeige[pos, 0].SetActive(true);
                }
                else
                {
                    if (SpielerAnzeige[pos, 0].activeInHierarchy && !connected)
                    {
                        Config.PLAYERLIST[pos].name = "";
                        PlayDisconnectSound();
                    }

                    SpielerAnzeige[pos, 0].SetActive(false);
                }
            }
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
    /// Sendet eine Buzzer Anfrage an den Server
    /// </summary>
    private void SpielerBuzzered()
    {
        ClientUtils.SendToServer("#SpielerBuzzered");
    }
    /// <summary>
    /// Gibt den Buzzer frei
    /// </summary>
    private void BuzzerFreigeben()
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrClient", "BuzzerFreigeben", "Gibt den Buzzer frei.");
        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
            SpielerAnzeige[i, 1].SetActive(false);
    }
    /// <summary>
    /// Spielt Sound des Buzzers ab und zeigt welcher Spieler diesen gedrückt hat
    /// </summary>
    /// <param name="data">Spieler</param>
    private void AudioBuzzerPressed(string data)
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrClient", "AudioBuzzerPressed", "Spieler bekommt Buzzer: " + data);
        BuzzerSound.Play();
        int pId = Int32.Parse(data);
        SpielerAnzeige[Player.getPosInLists(pId), 1].SetActive(true);
    }
    /// <summary>
    /// Zeigt an, welcher Spieler dran ist. Blendet den roten Kreis eines Spielers ein
    /// </summary>
    /// <param name="data">Spielers</param>
    private void SpielerIstDran(string data)
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrClient", "SpielerIstDran", "Blendet roten Kreis ein: " + data);
        int pId = Int32.Parse(data);
        SpielerAnzeige[Player.getPosInLists(pId), 1].SetActive(true);
    }
    /// <summary>
    /// Deaktiviert die Spieler ist nicht dran anzeige. Blendet den roten Kreis eines Spielers aus
    /// </summary>
    /// <param name="data"> Spielers</param>
    private void SpielerIstNichtDran(string data)
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrClient", "SpielerIstNichtDran", "Blendet roten Kreis aus: " + data);
        int pId = Int32.Parse(data);
        SpielerAnzeige[Player.getPosInLists(pId), 1].SetActive(false);
    }
    /// <summary>
    /// Spielt den Sound für eine richtige Antwort ab
    /// </summary>
    private void AudioRichtigeAntwort()
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrClient", "AudioRichtigeAntwort", "Spielt den Sound für eine richtige Antwort ab");
        RichtigeAntwortSound.Play();
    }
    /// <summary>
    /// Spielt den Sound für eine falsche Antwort ab
    /// </summary>
    private void AudioFalscheAntwort()
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrClient", "AudioFalscheAntwort", "Spielt den Sound für eine falsche Antwort ab");
        FalscheAntwortSound.Play();
    }
    /// <summary>
    /// Zeigt an, ob ein Spieler austabt
    /// </summary>
    /// <param name="data">Aus-/Eingetabbte Spieler</param>
    private void SpielerAusgetabt(string data)
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrClient", "SpielerAusgetabt", "Zeigt an ob ein Spieler ein-/austabbt: " + data);
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
    /// <param name="data">bool</param>
    private void TexteingabeAnzeigen(string data)
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrClient", "TexteingabeAnzeigen", "Blendet das Texteingabefeld ein/aus: " + data);
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
    /// <param name="data">Blendet die Textantworten der Spieler ein</param>
    private void TextantwortenAnzeigen(string data)
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrClient", "TextantwortenAnzeigen", "Textantworten aller Spieler werden angezeigt: " + data);
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
    /// <param name="input">Texteingabefeld</param>
    public void SpielerAntwortEingabeInput(TMP_InputField input)
    {
        ClientUtils.SendToServer("#SpielerAntwortEingabe "+input.text);
    }
#endregion
    /// <summary>
    /// Blendet alle Kreuze ein/aus
    /// </summary>
    /// <param name="data">bool</param>
    private void WMBKreuzeEinblenden(string data)
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrClient", "WMBKreuzeEinblenden", "Blendet alle Kreuze ein/aus: " + data);
        bool toggle = bool.Parse(data);
        Kreuz1.SetActive(toggle);
        Kreuz1.transform.GetChild(0).gameObject.SetActive(toggle);
        Kreuz2.SetActive(toggle);
        Kreuz2.transform.GetChild(0).gameObject.SetActive(toggle);
        Kreuz3.SetActive(toggle);
        Kreuz3.transform.GetChild(0).gameObject.SetActive(toggle);
    }
    /// <summary>
    /// Zeigt an, wie viele Elemente vorhanden sind. 
    /// Blendet aber Texte aus.
    /// </summary>
    /// <param name="data">Liste der Elemente</param>
    private void WBMAnzahl(string data)
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrClient", "WBMAnzahl", data);
        string[] elemente = data.Replace("<>", "|").Split('|');
        for (int i = 0; i < elemente.Length; i++)
        {
            Elemente[i].transform.GetChild(0).gameObject.SetActive(false); // Grüner Rahmen
            Elemente[i].transform.GetChild(2).gameObject.GetComponent<TMP_Text>().text = elemente[i];
            Elemente[i].transform.GetChild(2).gameObject.SetActive(false);
            Elemente[i].transform.GetChild(3).gameObject.SetActive(false); // Blauer Rahmen
            Elemente[i].SetActive(true);
        }
    }
    /// <summary>
    /// Blendet ein, wie viele Elemente aufgezählt werden müssen
    /// </summary>
    /// <param name="data">Anzahl der aufzuzählenden Elemente</param>
    private void AnsagenElementeEinblenden(string data)
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrClient", "AnsagenElementeEinblenden", "Blendet ein, wie viele Elemente auszuzählen sind. " + data);
        Anzahl.SetActive(true);
        Anzahl.transform.GetChild(0).gameObject.GetComponent<TMP_Text>().text = "0";
        Anzahl.transform.GetChild(1).gameObject.GetComponent<TMP_Text>().text = "von " + data;
    }
    /// <summary>
    /// Blendet den Titel des Games für alle ein
    /// </summary>
    /// <param name="data">Titel</param>
    private void TitelEinblenden(string data)
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrClient", "TitelEinblenden", "Titel wird eingeblendet: " + data);
        Titel.GetComponent<TMP_Text>().text = data;
        Titel.SetActive(true);
    }
    /// <summary>
    /// Startet den Timer mit Anzeige
    /// </summary>
    /// <param name="data">Sekunden</param>
    private void TimerStarten(string data)
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrClient", "TimerStarten", "Starte den Timer mit " + data + " Sekunden.");
        int sekunden = Int32.Parse(data);

        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);
        StartCoroutine(RunTimer(sekunden));
    }
    /// <summary>
    /// Zeigt/versteckt Kreuze bei Fehlern
    /// </summary>
    /// <param name="data">Kreuz <1-3> ein-/ausblenden</param>
    private void KreuzeAusgrauen(string data)
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrClient", "KreuzeAusgrauen", data);
        if (data == "1")
        {
            Kreuz1.transform.GetChild(0).gameObject.SetActive(false);
        }
        else if (data == "2")
        {
            Kreuz2.transform.GetChild(0).gameObject.SetActive(false);
        }
        else if (data == "3")
        {
            Kreuz3.transform.GetChild(0).gameObject.SetActive(false);
        }
        else if (data == "-1")
        {
            Kreuz1.transform.GetChild(0).gameObject.SetActive(true);
        }
        else if (data == "-2")
        {
            Kreuz2.transform.GetChild(0).gameObject.SetActive(true);
        }
        else if (data == "-3")
        {
            Kreuz3.transform.GetChild(0).gameObject.SetActive(true);
        }
        else
        {
            Logging.log(Logging.LogType.Warning, "WerBietetMehrClient", "KreuzeAusgrauen", "Kreuz nicht gefunden. " + data);
        }
    }
    /// <summary>
    /// Ändert die Punktanzahl manuell
    /// </summary>
    /// <param name="data">Punkteingabe</param>
    private void AnzahlManuellAendern(string data)
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrClient", "AnzahlManuellAendern", "Ändert die Anzahl der aufgezählten Elemente. Anzahl: " + data);
        Anzahl.transform.GetChild(0).gameObject.GetComponent<TMP_Text>().text = data;
    }
    /// <summary>
    /// Blendet alte Anzeigen aus und wechselt das ausgewählte Spiel
    /// </summary>
    private void ChangeListe()
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrClient", "ChangeListe", "Wechselt die Liste und blendet alle Anzeigen aus.");
        Titel.GetComponent<TMP_Text>().text = "";
        Timer.SetActive(false);
        Anzahl.SetActive(false);
        for (int i = 1; i <= 30; i++)
        {
            Elemente[i - 1] = GameObject.Find("WerBietetMehr/Outline/Elemente/Element (" + i + ")");
            Elemente[i - 1].transform.GetChild(0).gameObject.SetActive(false);
            Elemente[i - 1].transform.GetChild(2).gameObject.GetComponent<TMP_Text>().text = "";
            Elemente[i - 1].transform.GetChild(2).gameObject.SetActive(false);
            Elemente[i - 1].transform.GetChild(3).gameObject.SetActive(false);
            Elemente[i - 1].SetActive(false);
        }
        Kreuz1.SetActive(false);
        Kreuz2.SetActive(false);
        Kreuz3.SetActive(false);
    }
    /// <summary>
    /// Löst ein Element der Liste auf
    /// </summary>
    /// <param name="data">Element Daten</param>
    private void ElementAuflösen(string data)
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrClient", "ElementAuflösen", "Element wird aufgelöst. Element: " + data);
        bool aufloesen = bool.Parse(data.Replace("[BOOL]", "|").Split('|')[1]);
        string anz = data.Replace("[ANZ]", "|").Split('|')[1];
        int index = Int32.Parse(data.Replace("[INDEX]", "|").Split('|')[1])-1;
        if (!aufloesen)
        {
            Elemente[index].transform.GetChild(0).gameObject.SetActive(true); // Grün Anzeigen
        }
        Anzahl.transform.GetChild(0).gameObject.GetComponent<TMP_Text>().text = anz;
        Elemente[index].transform.GetChild(2).gameObject.SetActive(true);
    }
}