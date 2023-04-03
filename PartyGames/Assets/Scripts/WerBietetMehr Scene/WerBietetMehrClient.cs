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
    DateTime zieltimer;
    int timersecond = 0;

    GameObject SpielerAntwortEingabe;
    GameObject[,] SpielerAnzeige;
    bool pressingbuzzer = false;

    [SerializeField] AudioSource BuzzerSound;
    [SerializeField] AudioSource RichtigeAntwortSound;
    [SerializeField] AudioSource FalscheAntwortSound;
    [SerializeField] AudioSource Moeoep;
    [SerializeField] AudioSource Beeep;

    void OnEnable()
    {
        InitAnzeigen();

        if (!Config.CLIENT_STARTED)
            return;
        SendToServer("#JoinWerBietetMehr");

        StartCoroutine(TestConnectionToServer());
    }
    IEnumerator TestConnectionToServer()
    {
        while (Config.CLIENT_STARTED)
        {
            SendToServer("#TestConnection");
            yield return new WaitForSeconds(10);
        }
    }

    void Update()
    {
        // Timer
        /* TODO: testweise mit Coroutines
        if (zieltimer != null && zieltimer != DateTime.MinValue)
        {
            if (DateTime.Now.Second != timersecond)
            {
                timersecond = DateTime.Now.Second;
                int diff = getDiffInSeconds(zieltimer);
                Timer.GetComponent<TMP_Text>().text = "" + diff;
                if (diff == 0)
                {
                    Beeep.Play();
                    Timer.SetActive(false);
                    zieltimer = DateTime.MinValue;
                    return;
                }
                // Moep Sound bei sekunden
                if (diff == 1 || diff == 2 || diff == 3 || diff == 10 || diff == 30 || diff == 60) // 10-0
                {
                    Moeoep.Play();
                }
            }
        }*/

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

    IEnumerator RunTimer(int seconds)
    {
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

    private void OnApplicationFocus(bool focus)
    {
        SendToServer("#ClientFocusChange " + focus);
    }

    private void OnApplicationQuit()
    {
        Logging.add(Logging.Type.Normal, "Client", "OnApplicationQuit", "Client wird geschlossen.");
        SendToServer("#ClientClosed");
        CloseSocket();
    }

    #region Verbindungen
    /**
     * Trennt die Verbindung zum Server
     */
    private void CloseSocket()
    {
        if (!Config.CLIENT_STARTED)
            return;

        Config.CLIENT_TCP.Close();
        Config.CLIENT_STARTED = false;

        Logging.add(Logging.Type.Normal, "Client", "CloseSocket", "Verbindung zum Server wurde getrennt. Client wird in das Hauptmenü geladen.");
    }
    #endregion
    #region Kommunikation
    /**
     * Sendet eine Nachricht an den Server.
     */
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
            Logging.add(Logging.Type.Error, "Client", "SendToServer", "Nachricht an Server konnte nicht gesendet werden." + e);
            Config.HAUPTMENUE_FEHLERMELDUNG = "Verbindung zum Server wurde verloren.";
            CloseSocket();
            SceneManager.LoadSceneAsync("StartUp");
        }
    }
    /**
     * Einkommende Nachrichten die vom Sever
     */
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
    /**
     * Eingehende Commands vom Server
     */
    public void Commands(string data, string cmd)
    {
        //Debug.Log("Eingehend: " + cmd + " -> " + data);
        switch (cmd)
        {
            default:
                Logging.add(Logging.Type.Warning, "WerBietetMehrClient", "Commands", "Unkown Command -> " + cmd + " - " + data);
                break;

            #region Universal Commands
            case "#ServerClosed":
                CloseSocket();
                SceneManager.LoadSceneAsync("Startup");
                break;
            case "#UpdateRemoteConfig":
                LoadConfigs.FetchRemoteConfig();
                break;
            case "#ZurueckInsHauptmenue":
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

    /**
     * Initialisiert die Anzeigen der Scene
     */
    private void InitAnzeigen()
    {
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

        zieltimer = DateTime.MinValue;
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
    /**
     * Aktualisiert die Spieler Anzeigen
     */
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
    /**
     * Sendet eine Buzzer Anfrage an den Server
     */
    public void SpielerBuzzered()
    {
        SendToServer("#SpielerBuzzered");
    }
    /**
     * Gibt den Buzzer frei
     */
    private void BuzzerFreigeben()
    {
        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
            SpielerAnzeige[i, 1].SetActive(false);
    }
    /**
     * Spielt Sound des Buzzers ab und zeigt welcher Spieler diesen gedrückt hat
     */
    private void AudioBuzzerPressed(string data)
    {
        BuzzerSound.Play();
        int pId = Int32.Parse(data);
        SpielerAnzeige[Player.getPosInLists(pId), 1].SetActive(true);
    }
    /**
     * Zeigt an, welcher Spieler dran ist
     */
    private void SpielerIstDran(string data)
    {
        int pId = Int32.Parse(data);
        SpielerAnzeige[Player.getPosInLists(pId), 1].SetActive(true);
    }
    /**
     * Deaktiviert die Spieler ist dran anzeige
     */
    private void SpielerIstNichtDran(string data)
    {
        int pId = Int32.Parse(data);
        SpielerAnzeige[Player.getPosInLists(pId), 1].SetActive(false);
    }
    /**
     * Spielt den Sound für eine richtige Antwort ab
     */
    private void AudioRichtigeAntwort()
    {
        RichtigeAntwortSound.Play();
    }
    /**
     * Spielt den Sound für eine falsche Antwort ab
     */
    private void AudioFalscheAntwort()
    {
        FalscheAntwortSound.Play();
    }
    /**
     * Zeigt an, ob ein Spieler austabt
     */
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
    /**
     * Zeigt das Texteingabefeld an
     */
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
    /**
     * Zeigt die Textantworten aller Spieler an
     */
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
    /**
     * Sendet die Antworteingabe an den Server
     */
    public void SpielerAntwortEingabeInput(TMP_InputField input)
    {
        SendToServer("#SpielerAntwortEingabe "+input.text);
    }
#endregion
    private int getDiffInSeconds(DateTime time)
    {
        if (DateTime.Compare(DateTime.Now, time) > 0)
        {
            return 0;
        }
        int sekunden = time.Second - DateTime.Now.Second;
        sekunden += (time.Minute - DateTime.Now.Minute) * 60;
        sekunden += (time.Hour - DateTime.Now.Hour) * 60 * 60;
        return sekunden;
    }
    private void WMBKreuzeEinblenden(string data)
    {
        bool toggle = bool.Parse(data);
        Kreuz1.SetActive(toggle);
        Kreuz1.transform.GetChild(0).gameObject.SetActive(toggle);
        Kreuz2.SetActive(toggle);
        Kreuz2.transform.GetChild(0).gameObject.SetActive(toggle);
        Kreuz3.SetActive(toggle);
        Kreuz3.transform.GetChild(0).gameObject.SetActive(toggle);
    }
    public void WBMAnzahl(string data)
    {
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
    public void AnsagenElementeEinblenden(string data)
    {
        Anzahl.SetActive(true);
        Anzahl.transform.GetChild(0).gameObject.GetComponent<TMP_Text>().text = "0";
        Anzahl.transform.GetChild(1).gameObject.GetComponent<TMP_Text>().text = "von " + data;
    }
    public void TitelEinblenden(string data)
    {
        Titel.GetComponent<TMP_Text>().text = data;
        Titel.SetActive(true);
    }
    public void TimerStarten(string data)
    {
        int sekunden = Int32.Parse(data);

        StopCoroutine(RunTimer(0));
        StartCoroutine(RunTimer(sekunden));
        //zieltimer = DateTime.Now.AddSeconds(sekunden);
        //Timer.SetActive(true);
        //Timer.GetComponent<TMP_Text>().text = "" + getDiffInSeconds(zieltimer);
    }
    private void KreuzeAusgrauen(string data)
    {
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
    }
    private void AnzahlManuellAendern(string data)
    {
        Anzahl.transform.GetChild(0).gameObject.GetComponent<TMP_Text>().text = data;
    }
    private void ChangeListe()
    {
        zieltimer = DateTime.MinValue;
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
    public void ElementAuflösen(string data)
    {
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