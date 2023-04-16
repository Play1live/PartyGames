using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WerBietetMehrServer : MonoBehaviour
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

    GameObject[,] SpielerAnzeige;

    // Server
    TMP_InputField ElementAnzahl;
    TMP_InputField AnsagenAnzahl;
    TMP_InputField TimerSekunden;
    int maxelemente;
    int aufgezähleElemente;
    Toggle aufloesen;
    GameObject TextEingabeAnzeige;
    GameObject BuzzerAnzeige;
    GameObject AustabbenAnzeigen;
    bool buzzerIsOn;

    bool[] PlayerConnected;
    int PunkteProRichtige = 4;
    int PunkteProFalsche = 1;

    [SerializeField] AudioSource BuzzerSound;
    [SerializeField] AudioSource RichtigeAntwortSound;
    [SerializeField] AudioSource FalscheAntwortSound;
    [SerializeField] AudioSource Moeoep;
    [SerializeField] AudioSource Beeep;

    void OnEnable()
    {
        PlayerConnected = new bool[Config.SERVER_MAX_CONNECTIONS];
        InitAnzeigen();
        InitWerBietetMehr();
    }
    void Update()
    {
        // Timer
        /*  TODO: testweise mit Coroutines
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
                    Debug.Log(diff);
                    Moeoep.Play();
                }
            }
        }*/

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
            Logging.add(new Logging(Logging.Type.Error, "Server", "SendMessage", "Nachricht an Client: " + sc.id + " (" + sc.name + ") konnte nicht gesendet werden." + e));
            // Verbindung zum Client wird getrennt
            ClientClosed(sc);
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
                SendMSG(data, sc);
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
                SendMSG(data, sc);
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
        //Debug.Log(player.name + " " + player.id + " -> " + cmd + "   ---   " + data);
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
            case "#TestConnection":
                break;
            case "#ClientFocusChange":
                ClientFocusChange(player, data);
                break;

            case "#JoinWerBietetMehr":
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
                SpielerAnzeige[i, 5].GetComponent<TMP_Text>().text = p.points + "";
            }
            else
                SpielerAnzeige[i, 0].SetActive(false);

        }
        return msg;
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
     * Initialisiert die Anzeigen zu beginn
     */
    private void InitAnzeigen()
    {
        // Buzzer Deaktivieren
        GameObject.Find("Einstellungen/BuzzerAktivierenToggle").GetComponent<Toggle>().isOn = false;
        BuzzerAnzeige = GameObject.Find("Einstellungen/BuzzerIstAktiviert");
        BuzzerAnzeige.SetActive(false);
        buzzerIsOn = false;
        // Austabben wird gezeigt
        GameObject.Find("Einstellungen/AusgetabtSpielernZeigenToggle").GetComponent<Toggle>().isOn = false;
        AustabbenAnzeigen = GameObject.Find("Einstellungen/AusgetabtWirdSpielernGezeigen");
        AustabbenAnzeigen.SetActive(false);
        // Spieler Texteingabe
        GameObject.Find("Einstellungen/TexteingabeAnzeigenToggle").GetComponent<Toggle>().isOn = false;
        TextEingabeAnzeige = GameObject.Find("Einstellungen/TexteingabeWirdAngezeigt");
        TextEingabeAnzeige.SetActive(false);
        // Punkte Pro Richtige Antwort
        GameObject.Find("Einstellungen/PunkteProRichtigeAntwort").GetComponent<TMP_InputField>().text = "" + PunkteProRichtige;
        // Punkte Pro Falsche Antwort
        GameObject.Find("Einstellungen/PunkteProFalscheAntwort").GetComponent<TMP_InputField>().text = "" + PunkteProFalsche;
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
        TMP_Dropdown ChangeQuiz = GameObject.Find("Einstellungen/ChangeQuiz").GetComponent<TMP_Dropdown>();
        ChangeQuiz.ClearOptions();
        ChangeQuiz.GetComponent<TMP_Dropdown>().AddOptions(Config.WERBIETETMEHR_SPIEL.getQuizzeAsStringList());
        ChangeQuiz.GetComponent<TMP_Dropdown>().value = Config.WERBIETETMEHR_SPIEL.getIndex(Config.WERBIETETMEHR_SPIEL.getSelected());
    }

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
        {
            Debug.Log(p.name + " - " + DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second + ":" + DateTime.Now.Millisecond);
            return;
        }
        Debug.LogWarning("B: " + p.name + " - " + DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second + ":" + DateTime.Now.Millisecond);
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
        Debug.LogWarning("Buzzer freigegeben.");
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
    #region Textantworten der Spieler
    /**
     * Blendet die Texteingabe für die Spieler ein
     */
    public void TexteingabeAnzeigenToggle(Toggle toggle)
    {
        TextEingabeAnzeige.SetActive(toggle.isOn);
        Broadcast("#TexteingabeAnzeigen " + toggle.isOn);
    }
    /**
    * Aktualisiert die Antwort die der Spieler eingibt
    */
    public void SpielerAntwortEingabe(Player p, string data)
    {
        SpielerAnzeige[p.id - 1, 6].GetComponentInChildren<TMP_InputField>().text = data;
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
        Broadcast("#SpielerIstDran " + pId);
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
    #region WerBietetMehr
    private void InitWerBietetMehr()
    {
        zieltimer = DateTime.MinValue;
        aufgezähleElemente = 0;
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
        GameObject.Find("WerBietetMehr/Outline/Server/KreuzeAnzeigen").GetComponent<Toggle>().isOn = false;

        // Server
        maxelemente = Config.WERBIETETMEHR_SPIEL.getSelected().getElemente().Count;
        ElementAnzahl = GameObject.Find("WerBietetMehr/Outline/Server/ElementAnzahl").GetComponent<TMP_InputField>();
        ElementAnzahl.text = "" + maxelemente;
        AnsagenAnzahl = GameObject.Find("WerBietetMehr/Outline/Server/AnsagenAnzahl").GetComponent<TMP_InputField>();
        TimerSekunden = GameObject.Find("WerBietetMehr/Outline/Server/TimerInput").GetComponent<TMP_InputField>();
        aufloesen = GameObject.Find("WerBietetMehr/Outline/Server/Auflösen").GetComponent<Toggle>();
        aufloesen.isOn = false;
        GameObject.Find("WerBietetMehr/Outline/Server/Quelle").GetComponent<TMP_InputField>().text = Config.WERBIETETMEHR_SPIEL.getSelected().getQuelle();
    }
    public void KreuzeEinblenden(Toggle toggle)
    {
        Broadcast("#WMBKreuzeEinblenden " + toggle.isOn);
        Kreuz1.SetActive(toggle.isOn);
        Kreuz1.transform.GetChild(0).gameObject.SetActive(toggle.isOn);
        Kreuz2.SetActive(toggle.isOn);
        Kreuz2.transform.GetChild(0).gameObject.SetActive(toggle.isOn);
        Kreuz3.SetActive(toggle.isOn);
        Kreuz3.transform.GetChild(0).gameObject.SetActive(toggle.isOn);
    }
    public void ElementAnzahlEinblenden()
    {
        // Blendet ElementAnzahl ein
        int anzahl = Config.WERBIETETMEHR_SPIEL.getSelected().getElemente().Count;
        string msg = "";
        for (int i = 0; i < anzahl; i++)
        {
            Elemente[i].transform.GetChild(0).gameObject.SetActive(false); // Grüner Rahmen
            Elemente[i].transform.GetChild(2).gameObject.GetComponent<TMP_Text>().text = Config.WERBIETETMEHR_SPIEL.getSelected().getElement(i);
            Elemente[i].transform.GetChild(3).gameObject.SetActive(false); // Blauer Rahmen
            Elemente[i].SetActive(true);
            msg += "<>" + Config.WERBIETETMEHR_SPIEL.getSelected().getElement(i);
        }
        if (msg.Length > 2)
            msg = msg.Substring("<>".Length);
        Broadcast("#WBMAnzahl " + msg); // TODO: für Server elemente anzeigen, bei clients erst nach andrücken
    }
    public void AnsagenElementeEinblenden()
    {
        Anzahl.SetActive(true);
        Anzahl.transform.GetChild(0).gameObject.GetComponent<TMP_Text>().text = "" + aufgezähleElemente;
        Anzahl.transform.GetChild(1).gameObject.GetComponent<TMP_Text>().text = "von " + GameObject.Find("WerBietetMehr/Outline/Server/AnsagenAnzahl").GetComponent<TMP_InputField>().text;

        Broadcast("#WBMAnsagen "+ GameObject.Find("WerBietetMehr/Outline/Server/AnsagenAnzahl").GetComponent<TMP_InputField>().text);
    }
    public void TitelEinblenden()
    {
        Titel.GetComponent<TMP_Text>().text = Config.WERBIETETMEHR_SPIEL.getSelected().getTitel();
        Titel.SetActive(true);

        Broadcast("#WBMTitel " + Config.WERBIETETMEHR_SPIEL.getSelected().getTitel());
    }
    public void TimerStarten()
    {
        if (TimerSekunden.text.Length == 0)
            return;
        int sekunden = Int32.Parse(TimerSekunden.text);
        Broadcast("#WBMTimerStarten " + (sekunden));

        StopCoroutine(RunTimer(0));
        StartCoroutine(RunTimer(sekunden));

        //zieltimer = DateTime.Now.AddSeconds(sekunden);
        //Timer.SetActive(true);
        //Timer.GetComponent<TMP_Text>().text = "" + getDiffInSeconds(zieltimer);
    }
    public void KreuzeAusgrauen(GameObject go)
    {
        string txt = go.GetComponentInChildren<TMP_Text>().text;
        Broadcast("#WBMKreuzAusgrauen " + txt);

        if (txt == "1")
        {
            Kreuz1.transform.GetChild(0).gameObject.SetActive(false);
        }
        else if (txt == "2")
        {
            Kreuz2.transform.GetChild(0).gameObject.SetActive(false);
        }
        else if (txt == "3")
        {
            Kreuz3.transform.GetChild(0).gameObject.SetActive(false);
        }
        else if (txt == "-1")
        {
            Kreuz1.transform.GetChild(0).gameObject.SetActive(true);
        }
        else if (txt == "-2")
        {
            Kreuz2.transform.GetChild(0).gameObject.SetActive(true);
        }
        else if (txt == "-3")
        {
            Kreuz3.transform.GetChild(0).gameObject.SetActive(true);
        }
    }
    public void AnzahlManuellAendern(int anz)
    {
        aufgezähleElemente += anz;
        Anzahl.transform.GetChild(0).gameObject.GetComponent<TMP_Text>().text = aufgezähleElemente + "";

        Broadcast("#WBMAnsagenAnzahl " + aufgezähleElemente);
    }
    public void ChangeListe(TMP_Dropdown drop)
    {
        // Wählt neues Quiz aus
        Config.WERBIETETMEHR_SPIEL.setSelected(Config.WERBIETETMEHR_SPIEL.getQuizByIndex(drop.value));
        Logging.add(Logging.Type.Normal, "WerBietetMehrServer", "ChangeListe", "WerBietetMehr starts: " + Config.WERBIETETMEHR_SPIEL.getSelected().getTitel());
        // Aktualisiert die Anzeigen
        Broadcast("#WBMNew");
        zieltimer = DateTime.MinValue;
        aufgezähleElemente = 0;
        Titel.GetComponent<TMP_Text>().text = "";
        Timer.SetActive(false);
        Anzahl.SetActive(false);
        for (int i = 1; i <= 30; i++)
        {
            Elemente[i - 1] = GameObject.Find("WerBietetMehr/Outline/Elemente/Element (" + i + ")");
            Elemente[i - 1].transform.GetChild(0).gameObject.SetActive(false);
            Elemente[i - 1].transform.GetChild(2).gameObject.GetComponent<TMP_Text>().text = "";
            Elemente[i - 1].transform.GetChild(3).gameObject.SetActive(false);
            Elemente[i - 1].SetActive(false);
        }
        Kreuz1.SetActive(false);
        Kreuz2.SetActive(false);
        Kreuz3.SetActive(false);
        GameObject.Find("WerBietetMehr/Outline/Server/KreuzeAnzeigen").GetComponent<Toggle>().isOn = false;

        // Server
        maxelemente = Config.WERBIETETMEHR_SPIEL.getSelected().getElemente().Count;
        ElementAnzahl.text = "" + maxelemente;
        aufloesen.isOn = false;
        GameObject.Find("WerBietetMehr/Outline/Server/Quelle").GetComponent<TMP_InputField>().text = Config.WERBIETETMEHR_SPIEL.getSelected().getQuelle();
        AnsagenAnzahl.text = "";
    }
    public void ElementAuflösen(GameObject go)
    {
        // TODO: wenn auflösen an, wird element nicht grün
        if (!aufloesen.isOn)
        {
            aufgezähleElemente++;
            go.transform.GetChild(0).gameObject.SetActive(true); // Grün Anzeigen
            Anzahl.transform.GetChild(0).gameObject.GetComponent<TMP_Text>().text = aufgezähleElemente+"";
        }
        go.transform.GetChild(3).gameObject.SetActive(true); // Blau Anzeigen

        Broadcast("#WBMElement [ANZ]"+aufgezähleElemente+"[ANZ][INDEX]" + go.name.Replace("Element (","").Replace(")","") + "[INDEX][BOOL]" + aufloesen.isOn+"[BOOL]");
    }
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
    #endregion

}
