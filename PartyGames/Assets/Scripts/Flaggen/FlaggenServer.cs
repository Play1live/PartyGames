using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FlaggenServer : MonoBehaviour
{
    GameObject FlaggenOutline;
    GameObject FlaggenImage;
    GameObject Antwort;

    GameObject FlaggenEinstellungen;
    GameObject FlaggenName;
    GameObject FlaggenVorschauAnzeige;
    GameObject InhaltVorschau;
    GameObject FlaggenAuswahl;
    GameObject KategorieAuswahl;

    GameObject[] SearchBar;

    bool buzzerIsOn = false;

    GameObject BuzzerAnzeige;
    GameObject AustabbenAnzeigen;
    GameObject TextEingabeAnzeige;
    GameObject TextAntwortenAnzeige;

    GameObject[,] SpielerAnzeige;
    bool[] PlayerConnected;
    int PunkteProRichtige = 4;
    int PunkteProFalsche = 1;

    [SerializeField] AudioSource BuzzerSound;
    [SerializeField] AudioSource RichtigeAntwortSound;
    [SerializeField] AudioSource FalscheAntwortSound;
    [SerializeField] AudioSource DisconnectSound;

    void OnEnable()
    {
        StartCoroutine(ServerUtils.Broadcast());
        PlayerConnected = new bool[Config.SERVER_MAX_CONNECTIONS];
        InitAnzeigen();
        InitFlaggen();
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
        ServerUtils.BroadcastImmediate("#ServerClosed");
        Logging.log(Logging.LogType.Normal, "FlaggenServer", "OnApplicationQuit", "Server wird geschlossen");
        Config.SERVER_TCP.Server.Close();
    }

    #region Server Stuff
    #region Kommunikation
    /*/// <summary>
    /// Spieler beendet das Spiel
    /// </summary>
    /// <param name="player"></param>
    private void ClientClosed(Player player)
    {
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
            Logging.log(Logging.LogType.Warning, "FlaggenServer", "SendMSG", "Nachricht an Client: " + sc.id + " (" + sc.name + ") konnte nicht gesendet werden.", e);
            // Verbindung zum Client wird getrennt
            ClientClosed(sc);
        }
    }
    /// <summary>
    /// Sendet eine Nachricht an alle verbundenen Spieler
    /// </summary>
    /// <param name="data"></param>
    /// <param name="spieler"></param>
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
    /// <param name="data"></param>
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
    /// <param name="spieler"></param>
    /// <param name="data"></param>
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
    /// <param name="player"></param>
    /// <param name="data"></param>
    /// <param name="cmd"></param>
    private void Commands(Player player, string data, string cmd)
    {
        // Zeigt alle einkommenden Nachrichten an
        Logging.log(Logging.LogType.Debug, "FlaggenServer", "Commands", "Eingehende Nachricht: " + cmd + " -> " + data);
        // Sucht nach Command
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "FlaggenServer", "Commands", "Unkown Command: " + cmd + " -> " + data);
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

            case "#JoinFlaggen":
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
        //SceneManager.LoadScene("Startup");
        ServerUtils.AddBroadcast("#ZurueckInsHauptmenue");
    }
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
    /// <returns></returns>
    private string UpdateSpieler()
    {
        string msg = "#UpdateSpieler [ID]0[ID][PUNKTE]" + Config.SERVER_PLAYER.points + "[PUNKTE]";
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            Player p = Config.PLAYERLIST[i];
            msg += "[TRENNER][ID]" + p.id + "[ID][PUNKTE]" + p.points + "[PUNKTE][ONLINE]"+p.isConnected+"[ONLINE]";
            if (p.isConnected && PlayerConnected[i])
            {
                SpielerAnzeige[i, 0].SetActive(true);
                SpielerAnzeige[i, 2].GetComponent<Image>().sprite = p.icon;
                SpielerAnzeige[i, 4].GetComponent<TMP_Text>().text = p.name;
                SpielerAnzeige[i, 5].GetComponent<TMP_Text>().text = p.points+"";
            }
            else
                SpielerAnzeige[i, 0].SetActive(false);

        }
        return msg;
    }
    /// <summary>
    /// Initialisiert die Anzeigen zu beginn
    /// </summary>
    private void InitAnzeigen()
    {
        Logging.log(Logging.LogType.Debug, "FlaggenServer", "InitAnzeigen", "Initialisiert Anzeigen");
        // Anzeigen für alle
        FlaggenOutline = GameObject.Find("FlaggenAnzeigen/FlaggenImageOutline");
        FlaggenOutline.SetActive(false);
        FlaggenImage = GameObject.Find("FlaggenAnzeigen/FlaggenImage");
        FlaggenImage.SetActive(false);
        Antwort = GameObject.Find("FlaggenAnzeigen/Antwort");
        Antwort.SetActive(false);
        // Server Anzeigen
        FlaggenEinstellungen = GameObject.Find("FlaggenAnzeigen/EinstellungenLinks");
        FlaggenName = GameObject.Find("FlaggenAnzeigen/EinstellungenLinks/FlaggenName");
        FlaggenVorschauAnzeige = GameObject.Find("FlaggenAnzeigen/EinstellungenLinks/FlaggenVorschau");
        InhaltVorschau = GameObject.Find("FlaggenAnzeigen/EinstellungenLinks/InhaltVorschau");
        FlaggenAuswahl = GameObject.Find("FlaggenAnzeigen/EinstellungenLinks/FlaggenAuswahl");
        KategorieAuswahl = GameObject.Find("FlaggenAnzeigen/EinstellungenLinks/KategorieAuswahl");
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

        //SearchBar
        SearchBar = new GameObject[21];
        for (int i = 0; i < 21; i++)
        {
            SearchBar[i] = GameObject.Find("SearchBar/Land (" + (i) + ")");
            SearchBar[i].SetActive(false);
        }
    }
    /// <summary>
    /// Spielt den Disconnect Sound ab
    /// </summary>
    private void PlayDisconnectSound()
    {
        DisconnectSound.Play();
    }
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
            Logging.log(Logging.LogType.Normal, "FlaggenServer", "SpielerBuzzered", p.name + " - " + DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second + ":" + DateTime.Now.Millisecond);
            return;
        }
        Logging.log(Logging.LogType.Warning, "FlaggenServer", "SpielerBuzzered", "B: " + p.name + " - " + DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second + ":" + DateTime.Now.Millisecond);
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
        Logging.log(Logging.LogType.Warning, "FlaggenServer", "SpielerBuzzerFreigeben", "Buzzer wurde freigegeben");
        ServerUtils.AddBroadcast("#BuzzerFreigeben");
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
            ServerUtils.AddBroadcast("#SpielerAusgetabt 0");
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
            ServerUtils.AddBroadcast("#SpielerAusgetabt " + player.id + " " + ausgetabt);
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
        ServerUtils.AddBroadcast("#TexteingabeAnzeigen " + toggle.isOn);
    }
    /// <summary>
    /// Aktualisiert die Antwort die der Spieler eingibt
    /// </summary>
    /// <param name="p"></param>
    /// <param name="data"></param>
    public void SpielerAntwortEingabe(Player p, string data)
    {
        SpielerAnzeige[p.id - 1, 6].GetComponentInChildren<TMP_InputField>().text = data;
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
            ServerUtils.AddBroadcast("#TextantwortenAnzeigen [BOOL]" + toggle.isOn + "[BOOL]");
            return;
        }
        string msg = "";
        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
        {
            msg = msg + "[ID" + (i + 1) + "]" + SpielerAnzeige[i, 6].GetComponentInChildren<TMP_InputField>().text + "[ID" + (i + 1) + "]";
        }
        ServerUtils.AddBroadcast("#TextantwortenAnzeigen [BOOL]" + toggle.isOn + "[BOOL][TEXT]" + msg);
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
    /// Vergibt an den Spieler Punkte für eine richtige Antwort
    /// </summary>
    /// <param name="player"></param>
    public void PunkteRichtigeAntwort(GameObject player)
    {
        ServerUtils.AddBroadcast("#AudioRichtigeAntwort");
        RichtigeAntwortSound.Play();
        int pId = Int32.Parse(player.transform.parent.parent.name.Replace("Player (", "").Replace(")", ""));
        int pIndex = Player.getPosInLists(pId);
        Config.PLAYERLIST[pIndex].points += PunkteProRichtige;
        UpdateSpielerBroadcast();
    }
    /// <summary>
    /// Vergibt an alle anderen Spieler Punkte bei einer falschen Antwort
    /// </summary>
    /// <param name="player"></param>
    public void PunkteFalscheAntwort(GameObject player)
    {
        ServerUtils.AddBroadcast("#AudioFalscheAntwort");
        FalscheAntwortSound.Play();
        int pId = Int32.Parse(player.transform.parent.parent.name.Replace("Player (", "").Replace(")", ""));
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
        SpielerAnzeige[(pId - 1), 1].SetActive(true);
        buzzerIsOn = false;
        ServerUtils.AddBroadcast("#SpielerIstDran " + pId);
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
        ServerUtils.AddBroadcast("#SpielerIstNichtDran " + pId);
    }
    #endregion
    #region Flaggen Anzeige
    /// <summary>
    /// Initialisiert die Anzeigen
    /// </summary>
    private void InitFlaggen()
    {
        Logging.log(Logging.LogType.Debug, "FlaggenServer", "InitFlaggen", "Initialisiert die Flaggenanzeigen");
        FlaggenName.GetComponent<TMP_Text>().text = "#Fragezeichen";
        FlaggenVorschauAnzeige.GetComponent<Image>().sprite = Config.FLAGGEN_SPIEL.getFragezeichenFlagge();
        InhaltVorschau.GetComponent<TMP_Text>().text = "Farben: \nHauptstadt: \nFläche: \nEinwohnerzahl:";
        List<string> namen = new List<string>();
        foreach (Flagge flagge in Config.FLAGGEN_SPIEL.getFlaggen())
        {
            namen.Add(flagge.getName());
        }
        FlaggenAuswahl.GetComponent<TMP_Dropdown>().ClearOptions();
        FlaggenAuswahl.GetComponent<TMP_Dropdown>().AddOptions(namen);
    }
    /// <summary>
    /// Wählt eine zufällige Flagge aus.
    /// </summary>
    public void FlaggenZufaelligeFlaggeVorschau()
    {
        Flagge flagge = Config.FLAGGEN_SPIEL.getRandomFlagge();
        FlaggenVorschau(flagge);
    }
    /// <summary>
    /// Wählt eine bestimmte Landesflagge aus.
    /// </summary>
    /// <param name="drop"></param>
    public void FlaggenBestimmteFlaggeVorschau(TMP_Dropdown drop)
    {
        Flagge flagge = Config.FLAGGEN_SPIEL.getFlagge(drop.value);
        FlaggenVorschau(flagge);
    }
    /// <summary>
    /// Wählt eine zufällige Kategorie aus.
    /// </summary>
    /// <param name="drop"></param>
    public void FlaggenZufaelligeKategorie(TMP_Dropdown drop)
    {
        drop.value = UnityEngine.Random.Range(0, drop.options.Count);
    }
    /// <summary>
    /// Zeigt Flagge in Vorschau an.
    /// </summary>
    /// <param name="flagge"></param>
    private void FlaggenVorschau(Flagge flagge)
    {
        FlaggenName.GetComponent<TMP_Text>().text = flagge.getName();
        FlaggenVorschauAnzeige.GetComponent<Image>().sprite = flagge.getBild();
        string farben = "";
        foreach (string f in flagge.getFarben())
        {
            farben += ", " + f;
        }
        farben = farben.Substring(2);
        string einwohner = "";
        char[] carray = flagge.getEinwohner().ToString().ToCharArray();
        int temp = 0;
        for (int i = carray.Length - 1; i >= 0; i--)
        {
            if (temp % 3 == 0)
                einwohner = "." + einwohner;
            einwohner = carray[i] + einwohner;
            temp++;
        }
        einwohner = einwohner.Substring(0, einwohner.Length - 1);
        string flaeche = "";
        char[] carray2 = flagge.getFlaeche().ToString().ToCharArray();
        int temp2 = 0;
        for (int i = carray2.Length - 1; i >= 0; i--)
        {
            if (temp2 % 3 == 0)
                flaeche = "." + flaeche;
            flaeche = carray2[i] + flaeche;
            temp2++;
        }
        flaeche = flaeche.Substring(0, flaeche.Length - 1);
        InhaltVorschau.GetComponent<TMP_Text>().text = "Farben: " + farben + "\nHauptstadt: " + flagge.getHauptstadt() + "\nFläche: " + flaeche + " km²\nEinwohnerzahl: " + einwohner;
        FlaggenAuswahl.GetComponent<TMP_Dropdown>().value = Config.FLAGGEN_SPIEL.getIndex(flagge);
    }
    /// <summary>
    /// Wählt Flagge aus und zeigt diese den Spielern an.
    /// </summary>
    public void FlaggenFlaggeAuswaehlen()
    {
        FlaggenOutline.SetActive(true);
        FlaggenImage.SetActive(true);
        if (KategorieAuswahl.GetComponent<TMP_Dropdown>().value == 1 || FlaggenName.GetComponent<TMP_Text>().text.Equals("#Fragezeichen"))
        {
            // Fragezeichen Flagge zeigen
            ServerUtils.AddBroadcast("#FlaggenSpielAnzeige #Fragezeichen");
            FlaggenImage.GetComponent<Image>().sprite = Config.FLAGGEN_SPIEL.getFragezeichenFlagge();
        }
        else
        {
            ServerUtils.AddBroadcast("#FlaggenSpielAnzeige " + Config.FLAGGEN_SPIEL.getFlagge(FlaggenName.GetComponent<TMP_Text>().text).getName());
            FlaggenImage.GetComponent<Image>().sprite = Config.FLAGGEN_SPIEL.getFlagge(FlaggenName.GetComponent<TMP_Text>().text).getBild();
        }
        Antwort.SetActive(false); // Antwort ausblenden
    }
    /// <summary>
    /// Zeigt den Spielern die gegebene Antwort an.
    /// </summary>
    public void FlaggenShowAntwort()
    {
        string antwort = "";
        Flagge flagge = Config.FLAGGEN_SPIEL.getFlagge(FlaggenAuswahl.GetComponent<TMP_Dropdown>().value);
        if (KategorieAuswahl.GetComponent<TMP_Dropdown>().value == 0) // Namen
        {
            antwort = flagge.getName();
        }
        else if (KategorieAuswahl.GetComponent<TMP_Dropdown>().value == 1) // Farben
        {
            string farben = "";
            for (int i = 0; i < flagge.getFarben().Length; i++)
            {
                farben += ", " + flagge.getFarben()[i];
            }
            farben = farben.Substring(2);
            antwort = farben;
        }
        else if (KategorieAuswahl.GetComponent<TMP_Dropdown>().value == 2) // Hauptstadt
        {
            antwort = flagge.getHauptstadt();
        }
        else if (KategorieAuswahl.GetComponent<TMP_Dropdown>().value == 3) // Fläche
        {
            string flaeche = "";
            char[] carray = flagge.getFlaeche().ToString().ToCharArray();
            int temp = 0;
            for (int i = carray.Length - 1; i >= 0; i--)
            {
                if (temp % 3 == 0)
                    flaeche = "." + flaeche;
                flaeche = carray[i] + flaeche;
                temp++;
            }
            flaeche = flaeche.Substring(0, flaeche.Length - 1);
            antwort = flaeche + " km²";
        }
        else if (KategorieAuswahl.GetComponent<TMP_Dropdown>().value == 4) // Einwohnerzahl
        {
            string einwohner = "";
            char[] carray = flagge.getEinwohner().ToString().ToCharArray();
            int temp = 0;
            for (int i = carray.Length - 1; i >= 0; i--)
            {
                if (temp % 3 == 0)
                    einwohner = "." + einwohner;
                einwohner = carray[i] + einwohner;
                temp++;
            }
            einwohner = einwohner.Substring(0, einwohner.Length - 1);
            antwort = einwohner + " Einwohner";
        }
        else
        {
            Logging.log(Logging.LogType.Error, "FlaggenServer", "FlaggenShowAntwort", "Unbekannte Kategorie: " + KategorieAuswahl.GetComponent<TMP_Dropdown>().options[KategorieAuswahl.GetComponent<TMP_Dropdown>().value]);
        }

        ServerUtils.AddBroadcast("#FlaggenSpielShowAntwort " + antwort);
        Antwort.GetComponent<TMP_Text>().text = antwort;
        Antwort.SetActive(true);
    }
    #endregion
    #region SearchBar
    /// <summary>
    /// Aktualisiert die Suchvorschau
    /// </summary>
    /// <param name="input"></param>
    public void OnSearch(TMP_InputField input)
    {
        if (input.text.Length == 0)
        {
            for (int i = 0; i < 21; i++)
            {
                SearchBar[i].SetActive(false);
            }
            return;
        }
        
        int bar = 0;
        foreach (Flagge flagge in Config.FLAGGEN_SPIEL.getFlaggen())
        {
            if (bar == 21)
                break;

            if (flagge.getName().ToLower().StartsWith(input.text.ToLower()))
            {
                SearchBar[bar].SetActive(true);
                SearchBar[bar].transform.GetChild(0).GetComponent<TMP_Text>().text = flagge.getName();
                bar++;
            }
        }

        // Blendet restliches aus
        for (int i = bar; i < 21; i++)
        {
            SearchBar[i].SetActive(false);
        }
    }
    /// <summary>
    /// Wählt ein Flaggenelement aus der Suche aus
    /// </summary>
    /// <param name="btn"></param>
    public void SelectSearchItem(Button btn)
    {
        Flagge flag = Config.FLAGGEN_SPIEL.getFlagge(btn.transform.parent.transform.GetChild(0).GetComponent<TMP_Text>().text);
        if (flag == null)
            return;
        FlaggenVorschau(flag);
    }
    #endregion
}
