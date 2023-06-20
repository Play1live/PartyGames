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

    Coroutine timerCoroutine;

    [SerializeField] AudioSource BuzzerSound;
    [SerializeField] AudioSource RichtigeAntwortSound;
    [SerializeField] AudioSource FalscheAntwortSound;
    [SerializeField] AudioSource Moeoep;
    [SerializeField] AudioSource Beeep;
    [SerializeField] AudioSource DisconnectSound;

    void OnEnable()
    {
        StartCoroutine(ServerUtils.Broadcast());
        PlayerConnected = new bool[Config.SERVER_MAX_CONNECTIONS];
        InitAnzeigen();
        InitWerBietetMehr();
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

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private void OnApplicationQuit()
    {
        ServerUtils.BroadcastImmediate("#ServerClosed");
        Logging.log(Logging.LogType.Normal, "WerBietetMehrServer", "OnApplicationQuit", "Server wird geschlossen");
        Config.SERVER_TCP.Server.Close();
    }

    /// <summary>
    /// Timer läuft
    /// </summary>
    /// <param name="seconds">Dauer</param>
    IEnumerator RunTimer(int seconds)
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrServer", "RunTimer", "Timer läuft: " + seconds);
        Timer.SetActive(true);

        while (seconds >= 0)
        {
            Timer.GetComponent<TMP_Text>().text = "" + seconds;

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
    #region Server Stuff
    #region Kommunikation
    /*/// <summary>
    /// Spieler beendet das Spiel
    /// </summary>
    /// <param name="player">Spieler</param>
    private void ClientClosed(Player player)
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrServer", "ClientClosed", "Spielerdaten werden gelöscht. " + player.name);
        player.icon = Resources.Load<Sprite>("Images/ProfileIcons/empty");
        player.name = "";
        player.points = 0;
        player.isConnected = false;
        player.isDisconnected = true;
    }
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
            Logging.log(Logging.LogType.Warning, "WerBietetMehrServer", "SendMSG", "Nachricht an Client: " + sc.id + " (" + sc.name + ") konnte nicht gesendet werden.", e);
            // Verbindung zum Client wird getrennt
            ClientClosed(sc);
        }
    }
    /// <summary>
    /// Sendet eine Nachricht an alle verbundenen Spieler
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
    }*/
    /// <summary>
    /// Einkommende Nachrichten die von Spielern an den Server gesendet werden.
    /// </summary>
    /// <param name="spieler">Spieler</param>
    /// <param name="data">Argumente</param>
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
        Logging.log(Logging.LogType.Debug, "WerBietetMehrServer", "Commands", "Eingehende Nachricht: " + player.name + " " + player.id + " -> " + cmd + "   ---   " + data);
        // Sucht nach Command
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "WerBietetMehrServer", "Commands", "Unkown Command: " + cmd + " -> " + data);
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
    /// <summary>
    /// Sendet aktualisierte Spielerinfos an alle Spieler
    /// </summary>
    private void UpdateSpielerBroadcast()
    {
        ServerUtils.AddBroadcast(UpdateSpieler());
    }
    /// <summary>
    /// Aktualisiert die Spieler Anzeige Informationen & gibt diese als Text zurück
    /// </summary>
    /// <returns>#Update Spieler ...</returns>
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
                SpielerAnzeige[i, 2].GetComponent<Image>().sprite = p.icon;
                SpielerAnzeige[i, 4].GetComponent<TMP_Text>().text = p.name;
                SpielerAnzeige[i, 5].GetComponent<TMP_Text>().text = p.points + "";
            }
            else
                SpielerAnzeige[i, 0].SetActive(false);

        }
        Logging.log(Logging.LogType.Debug, "WerBietetMehrServer", "UpdateSpieler", "Spieler werden aktualisiert: " + msg);
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
    /// Spiel Verlassen & Zurück in die Lobby laden
    /// </summary>
    public void SpielVerlassenButton()
    {
        Logging.log(Logging.LogType.Normal, "WerBietetMehrServer", "SpielVerlassenButton", "Spiel wird verlassen. Lade ins Hauptmenü.");
        //SceneManager.LoadScene("Startup");
        ServerUtils.AddBroadcast("#ZurueckInsHauptmenue");
    }
    /// <summary>
    /// Initialisiert die Anzeigen zu beginn
    /// </summary>
    private void InitAnzeigen()
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrServer", "InitAnzeigen", "Anzeigen werden initialisiert.");
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
        ChangeQuiz.GetComponent<TMP_Dropdown>().AddOptions(Config.WERBIETETMEHR_SPIEL.getGamesAsList());
        ChangeQuiz.GetComponent<TMP_Dropdown>().value = Config.WERBIETETMEHR_SPIEL.getIndex(Config.WERBIETETMEHR_SPIEL.getSelected());
    }
    #region Buzzer
    /// <summary>
    /// Aktiviert/Deaktiviert den Buzzer für alle Spieler
    /// </summary>
    /// <param name="toggle">Toggle</param>
    public void BuzzerAktivierenToggle(Toggle toggle)
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrServer", "BuzzerAktivierenToggle", "Buzzer wird aktiviert: " + toggle.isOn);
        buzzerIsOn = toggle.isOn;
        BuzzerAnzeige.SetActive(toggle.isOn);
    }
    /// <summary>
    /// Spielt Sound ab, sperrt den Buzzer und zeigt den Spieler an
    /// </summary>
    /// <param name="p">Spieler</param>
    private void SpielerBuzzered(Player p)
    {
        if (!buzzerIsOn)
        {
            Logging.log(Logging.LogType.Normal, "WerBietetMehrServer", "SpielerBuzzered", p.name + " - " + DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second + ":" + DateTime.Now.Millisecond);
            return;
        }
        Logging.log(Logging.LogType.Warning, "WerBietetMehrServer", "SpielerBuzzered", "B: " + p.name + " - " + DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second + ":" + DateTime.Now.Millisecond);
        buzzerIsOn = false;
        ServerUtils.AddBroadcast("#AudioBuzzerPressed " + p.id);
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
        Logging.log(Logging.LogType.Warning, "WerBietetMehrServer", "SpielerBuzzerFreigeben", "Buzzer freigegeben");
        ServerUtils.AddBroadcast("#BuzzerFreigeben");
    }
    #endregion
    #region Spieler Ausgetabt Anzeige
    /// <summary>
    /// Austaben wird allen/keinen Spielern angezeigt
    /// </summary>
    /// <param name="toggle">Toggle</param>
    public void AustabenAllenZeigenToggle(Toggle toggle)
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrServer", "AustabenAllenZeigenToggle", "Angeige: " + toggle.isOn);
        AustabbenAnzeigen.SetActive(toggle.isOn);
        if (toggle.isOn == false)
            ServerUtils.AddBroadcast("#SpielerAusgetabt 0");
    }
    /// <summary>
    /// Spieler Tabt aus, wird ggf allen gezeigt
    /// </summary>
    /// <param name="player">Spieler</param>
    /// <param name="data">Ein-/Ausgetabt</param>
    private void ClientFocusChange(Player player, string data)
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrServer", "ClientFocusChange", "Spieler " + player.name + " ist ausgetabt: " + data);
        bool ausgetabt = !Boolean.Parse(data);
        SpielerAnzeige[(player.id - 1), 3].SetActive(ausgetabt); // Ausgetabt Einblednung
        if (AustabbenAnzeigen.activeInHierarchy)
            ServerUtils.AddBroadcast("#SpielerAusgetabt " + player.id + " " + ausgetabt);
    }
    #endregion
    #region Textantworten der Spieler
    /// <summary>
    /// Blendet die Texteingabe für die Spieler ein
    /// </summary>
    /// <param name="toggle">Toggle</param>
    public void TexteingabeAnzeigenToggle(Toggle toggle)
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrServer", "TexteingabeAnzeigenToggle", "Blendet Texteingabefeld ein: " + toggle.isOn);
        TextEingabeAnzeige.SetActive(toggle.isOn);
        ServerUtils.AddBroadcast("#TexteingabeAnzeigen " + toggle.isOn);
    }
    /// <summary>
    /// Aktualisiert die Antwort die der Spieler eingibt
    /// </summary>
    /// <param name="p">Spieler</param>
    /// <param name="data">Texteingabe</param>
    private void SpielerAntwortEingabe(Player p, string data)
    {
        SpielerAnzeige[p.id - 1, 6].GetComponentInChildren<TMP_InputField>().text = data;
    }
    #endregion
    #region Punkte
    /// <summary>
    /// Punkte Pro Richtige Antworten Anzeigen
    /// </summary>
    /// <param name="input">Eingabefeld</param>
    public void ChangePunkteProRichtigeAntwort(TMP_InputField input)
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrServer", "ChangePunkteProRichtigeAntwort", "Neue Punkte pro richtige Antwort: " + input.text);
        PunkteProRichtige = Int32.Parse(input.text);
    }
    /// <summary>
    /// Punkte pro falsche Antwort ändern
    /// </summary>
    /// <param name="input">Eingabefeld</param>
    public void ChangePunkteProFalscheAntwort(TMP_InputField input)
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrServer", "ChangePunkteProFalscheAntwort", "Neue Punkte pro falsche Antwort: " + input.text);
        PunkteProFalsche = Int32.Parse(input.text);
    }
    /// <summary>
    /// Vergibt an den Spieler Punkte für eine richtige Antwort
    /// </summary>
    /// <param name="player"></param>
    public void PunkteRichtigeAntwort(GameObject player)
    {
        ServerUtils.AddBroadcast("#AudioRichtigeAntwort");
        RichtigeAntwortSound.Play();
        int pId = Int32.Parse(player.transform.parent.parent.name.Replace("Player (", "").Replace(")", ""));
        int pIndex = Player.getPosInLists(pId);

        Logging.log(Logging.LogType.Debug, "WerBietetMehrServer", "PunkteRichtigeAntwort", "Spieler " + Config.PLAYERLIST[pIndex].name + " hat richtig geantwortet.");
        Config.PLAYERLIST[pIndex].points += PunkteProRichtige;
        UpdateSpielerBroadcast();
    }
    /// <summary>
    /// Vergibt an alle anderen Spieler Punkte bei einer falschen Antwort
    /// </summary>
    /// <param name="player">Spieler, der keine Punkte bekommen soll</param>
    public void PunkteFalscheAntwort(GameObject player)
    {
        ServerUtils.AddBroadcast("#AudioFalscheAntwort");
        FalscheAntwortSound.Play();
        int pId = Int32.Parse(player.transform.parent.parent.name.Replace("Player (", "").Replace(")", ""));

        Logging.log(Logging.LogType.Debug, "WerBietetMehrServer", "PunkteFalscheAntwort", "Spieler " + Config.PLAYERLIST[Player.getPosInLists(pId)].name + " hat falsch geantwortet.");
        foreach (Player p in Config.PLAYERLIST)
        {
            if (pId != p.id && p.isConnected)
                p.points += PunkteProFalsche;
        }
        Config.SERVER_PLAYER.points += PunkteProFalsche;
        UpdateSpielerBroadcast();
    }
    /// <summary>
    /// Ändert die Punkte des Spielers (+-1)
    /// </summary>
    /// <param name="button"></param>
    public void PunkteManuellAendern(GameObject button)
    {
        int pId = Int32.Parse(button.transform.parent.parent.name.Replace("Player (", "").Replace(")", ""));
        int pIndex = Player.getPosInLists(pId);

        Logging.log(Logging.LogType.Debug, "WerBietetMehrServer", "PunkteManuellAendern", "Spieler " + Config.PLAYERLIST[pIndex].name + " erhält " + button.name.Replace("+","") + " Punkte.");
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
        Logging.log(Logging.LogType.Debug, "WerBietetMehrServer", "PunkteManuellAendern", "Spieler " + Config.PLAYERLIST[pIndex].name + " erhält " + punkte + " Punkte.");

        Config.PLAYERLIST[pIndex].points += punkte;
        UpdateSpielerBroadcast();
    }
    #endregion
    #region Spieler ist (Nicht-)Dran
    /// <summary>
    /// Aktiviert den Icon Rand beim Spieler
    /// </summary>
    /// <param name="button">Spielerbutton</param>
    public void SpielerIstDran(GameObject button)
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrServer", "SpielerIstNichtDran", "Spieler ist dran.");
        int pId = Int32.Parse(button.transform.parent.parent.name.Replace("Player (", "").Replace(")", ""));
        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
            SpielerAnzeige[(pId - 1), 1].SetActive(false);
        SpielerAnzeige[(pId - 1), 1].SetActive(true);
        buzzerIsOn = false;
        ServerUtils.AddBroadcast("#SpielerIstDran " + pId);
    }
    /// <summary>
    /// Versteckt den Icon Rand beim Spieler
    /// </summary>
    /// <param name="button">Spielerbutton</param>
    public void SpielerIstNichtDran(GameObject button)
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrServer", "SpielerIstNichtDran", "Spieler ist nicht mehr dran.");
        int pId = Int32.Parse(button.transform.parent.parent.name.Replace("Player (", "").Replace(")", ""));
        SpielerAnzeige[(pId - 1), 1].SetActive(false);

        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
            if (SpielerAnzeige[i, 1].activeInHierarchy)
                return;
        buzzerIsOn = BuzzerAnzeige.activeInHierarchy; // Buzzer wird erst aktiviert wenn keiner mehr dran ist
        ServerUtils.AddBroadcast("#SpielerIstNichtDran " + pId);
    }
    #endregion
    #region WerBietetMehr
    /// <summary>
    /// Initialisiert die Anzeigen von diesem Game
    /// </summary>
    private void InitWerBietetMehr()
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrServer", "InitWerBietetMehr", "Anzeigen werden initialisiert.");
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
    /// <summary>
    /// Blendet alle Kreuze ein
    /// </summary>
    /// <param name="toggle">Toggle</param>
    public void KreuzeEinblenden(Toggle toggle)
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrServer", "KreuzeEinblenden", "Blendet alle Kreuze ein");
        ServerUtils.AddBroadcast("#WMBKreuzeEinblenden " + toggle.isOn);
        Kreuz1.SetActive(toggle.isOn);
        Kreuz1.transform.GetChild(0).gameObject.SetActive(toggle.isOn);
        Kreuz2.SetActive(toggle.isOn);
        Kreuz2.transform.GetChild(0).gameObject.SetActive(toggle.isOn);
        Kreuz3.SetActive(toggle.isOn);
        Kreuz3.transform.GetChild(0).gameObject.SetActive(toggle.isOn);
    }
    /// <summary>
    /// Blendet ein, wie viele Elemente in der Liste enthalten sind
    /// </summary>
    public void ElementAnzahlEinblenden()
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrServer", "ElementAnzahlEinblenden", "Anzahl: " + Config.WERBIETETMEHR_SPIEL.getSelected().getElemente().Count);
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
        ServerUtils.AddBroadcast("#WBMAnzahl " + msg);
    }
    /// <summary>
    /// Blendet die Anzeige an, die besagt, wieviele Elemente aufgezählt werden müssen
    /// </summary>
    public void AnsagenElementeEinblenden()
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrServer", "AnsagenElementeEinblenden", "wird angezeigt");
        Anzahl.SetActive(true);
        Anzahl.transform.GetChild(0).gameObject.GetComponent<TMP_Text>().text = "" + aufgezähleElemente;
        Anzahl.transform.GetChild(1).gameObject.GetComponent<TMP_Text>().text = "von " + GameObject.Find("WerBietetMehr/Outline/Server/AnsagenAnzahl").GetComponent<TMP_InputField>().text;

        ServerUtils.AddBroadcast("#WBMAnsagen "+ GameObject.Find("WerBietetMehr/Outline/Server/AnsagenAnzahl").GetComponent<TMP_InputField>().text);
    }
    /// <summary>
    /// Blendet den Titel des Spiels ein
    /// </summary>
    public void TitelEinblenden()
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrServer", "Titel Einblenden", "Titel: " + Config.WERBIETETMEHR_SPIEL.getSelected().getTitel());
        Titel.GetComponent<TMP_Text>().text = Config.WERBIETETMEHR_SPIEL.getSelected().getTitel();
        Titel.SetActive(true);

        ServerUtils.AddBroadcast("#WBMTitel " + Config.WERBIETETMEHR_SPIEL.getSelected().getTitel());
    }
    /// <summary>
    /// Startet den Timer und bricht den alten, falls dieser noch läuft, ab
    /// </summary>
    public void TimerStarten()
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrServer", "TimerStarten", "Startet den Timer");
        if (TimerSekunden.text.Length == 0)
            return;
        int sekunden = Int32.Parse(TimerSekunden.text);
        ServerUtils.AddBroadcast("#WBMTimerStarten " + (sekunden));

        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(RunTimer(sekunden));
    }
    /// <summary>
    /// Wechselt die Anzeige, welche Kreuze ein-/ausgeblendet werden
    /// </summary>
    /// <param name="go">Auswahl</param>
    public void KreuzeAusgrauen(GameObject go)
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrServer", "KreuzeAusgrauen", "Wechselt die Anzeige der Kreuze: " + go.GetComponentInChildren<TMP_Text>().text);
        string txt = go.GetComponentInChildren<TMP_Text>().text;
        ServerUtils.AddBroadcast("#WBMKreuzAusgrauen " + txt);

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
    /// <summary>
    /// Ändert manuell die Anzahl der Elemente die aufgezählt werden müssen
    /// </summary>
    /// <param name="anz"></param>
    public void AnzahlManuellAendern(int anz)
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrServer", "AnzahlManuellAendern", "Anzahl: " + anz);
        aufgezähleElemente += anz;
        Anzahl.transform.GetChild(0).gameObject.GetComponent<TMP_Text>().text = aufgezähleElemente + "";

        ServerUtils.AddBroadcast("#WBMAnsagenAnzahl " + aufgezähleElemente);
    }
    /// <summary>
    /// Wechselt das Spiel und blendet alte Anzeigen aus und aktualisiert diese
    /// </summary>
    /// <param name="drop">Spielauswahl</param>
    public void ChangeListe(TMP_Dropdown drop)
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrServer", "ChangeListe", "Spiel wird gewechselt: " + drop.options[drop.value]);
        // Wählt neues Quiz aus
        Config.WERBIETETMEHR_SPIEL.setSelected(Config.WERBIETETMEHR_SPIEL.getQuizByIndex(drop.value));
        Logging.log(Logging.LogType.Normal, "WerBietetMehrServer", "ChangeListe", "WerBietetMehr starts: " + Config.WERBIETETMEHR_SPIEL.getSelected().getTitel());
        // Aktualisiert die Anzeigen
        ServerUtils.AddBroadcast("#WBMNew");
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
    /// <summary>
    /// Löst ein Element auf und schickt Infos dazu an die Spieler
    /// </summary>
    /// <param name="go">Element</param>
    public void ElementAuflösen(GameObject go)
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrServer", "ElementAuflösen", "Element wird aufgelöst " + go.name);
        if (!aufloesen.isOn)
        {
            aufgezähleElemente++;
            go.transform.GetChild(0).gameObject.SetActive(true); // Grün Anzeigen
            Anzahl.transform.GetChild(0).gameObject.GetComponent<TMP_Text>().text = aufgezähleElemente+"";
        }
        go.transform.GetChild(3).gameObject.SetActive(true); // Blau Anzeigen

        ServerUtils.AddBroadcast("#WBMElement [ANZ]"+aufgezähleElemente+"[ANZ][INDEX]" + go.name.Replace("Element (","").Replace(")","") + "[INDEX][BOOL]" + aufloesen.isOn+"[BOOL]");
    }
    #endregion
}
