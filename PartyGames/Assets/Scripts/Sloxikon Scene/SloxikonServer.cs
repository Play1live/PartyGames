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

    int serverantwort = 0;

    GameObject AustabbenAnzeigen;
    GameObject TextEingabeAnzeige;
    GameObject TextAntwortenAnzeige;
    GameObject[,] SpielerAnzeige;
    bool[] PlayerConnected;
    int PunkteProRichtige = 3;
    int PunkteProFalsche = 1;

    GameObject Thema;
    GameObject[] Antworten;

    [SerializeField] AudioSource BuzzerSound;
    [SerializeField] AudioSource RichtigeAntwortSound;
    [SerializeField] AudioSource FalscheAntwortSound;

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
            #region Spieler Disconnected Message
            for (int i = 0; i < Config.PLAYERLIST.Length; i++)
            {
                if (Config.PLAYERLIST[i].isConnected == false)
                {
                    if (Config.PLAYERLIST[i].isDisconnected == true)
                    {
                        Logging.log(Logging.LogType.Normal, "QuizServer", "Update", "Spieler hat die Verbindung getrennt. ID: " + Config.PLAYERLIST[i].id);
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
        Logging.log(Logging.LogType.Normal, "Server", "OnApplicationQuit", "Server wird geschlossen");
        Config.SERVER_TCP.Server.Close();
    }

    #region Server Stuff
    #region Kommunikation
    /// <summary>
    /// Sendet eine Nachricht an den übergebenen Spieler
    /// </summary>
    /// <param name="data"></param>
    /// <param name="sc"></param>
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
            Logging.log(Logging.LogType.Warning, "SloxikonServer", "SendMSG", "Nachricht an Client: " + sc.id + " (" + sc.name + ") konnte nicht gesendet werden.", e);
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
    }
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
        Logging.log(Logging.LogType.Debug, "SloxikonServer", "Commands", "Eingehende Nachricht: " + cmd + " -> " + data);
        // Sucht nach Command
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "SloxikonServer", "Commands", "Unkown Command: " + cmd + " -> " + data);
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
    /// Fordert alle Clients auf die RemoteConfig neuzuladen
    /// </summary>
    public void UpdateRemoteConfig()
    {
        Broadcast("#UpdateRemoteConfig");
        LoadConfigs.FetchRemoteConfig();
    }
    /// <summary>
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
    /// Spiel Verlassen & Zurück in die Lobby laden
    /// </summary>
    public void SpielVerlassenButton()
    {
        SceneManager.LoadScene("Startup");
        Broadcast("#ZurueckInsHauptmenue");
    }
    /// <summary>
    /// Sendet aktualisierte Spielerinfos an alle Spieler
    /// </summary>
    private void UpdateSpielerBroadcast()
    {
        Broadcast(UpdateSpieler(), Config.PLAYERLIST);
    }
    /// <summary>
    /// Aktualisiert die Spieler Anzeige Informationen & gibt diese als Text zurück
    /// </summary>
    /// <returns></returns>
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
        return msg;
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
    }
    #region Sloxikon Anzeige
    /// <summary>
    /// Initialisiert die Anzeigen des Quizzes
    /// </summary>
    private void InitSloxikon()
    {
        Logging.log(Logging.LogType.Debug, "InitSloxikon", "InitAnzeigen", "Initialisiert Sloxikonanzeigen");
        GameObject.Find("Sloxikon/Titel").GetComponent<TMP_Text>().text = "";//Config.SLOXIKON_SPIEL.getSelected().getTitel();
        GameObject.Find("Server/ThemenVorschau").GetComponent<TMP_Text>().text = Config.SLOXIKON_SPIEL.getSelected().getThemenListe();
        aktuellesThema = 0;
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
                    child.GetComponent<Image>().sprite = Config.PLAYERLIST[j].icon;
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
    }

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
        Broadcast("SloxikonShowTitel " + Config.SLOXIKON_SPIEL.getSelected().getTitel());
        GameObject.Find("Sloxikon/Titel").GetComponent<TMP_Text>().text = Config.SLOXIKON_SPIEL.getSelected().getTitel();
    }
    public void HideAll()
    {

    }
    public void ShowThema()
    {
        Broadcast("SloxikonShowThema " + Config.SLOXIKON_SPIEL.getSelected().getThemen()[aktuellesThema]);
        Thema.SetActive(true);
        Thema.GetComponentInChildren<TMP_InputField>().text = Config.SLOXIKON_SPIEL.getSelected().getThemen()[aktuellesThema];


        // Zuweisung wer welche antwort gibt
        List<int> list = new List<int>();
        list.Add(0);
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            if (Config.PLAYERLIST[i].isConnected)
                list.Add(list.Count);
        }
        int anzahl = list.Count;
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            if (Config.PLAYERLIST[i].isConnected)
            {
                int random = UnityEngine.Random.Range(0, list.Count);
                Antworten[random].transform.GetChild(1).GetComponent<Image>().sprite = Config.PLAYERLIST[i].icon;
                Antworten[random].transform.GetChild(1).GetComponentInChildren<TMP_Text>().text = "" + Config.PLAYERLIST[i].id;
                Antworten[random].transform.GetChild(1).GetComponent<Image>().color = new Color(255, 255, 255, 0.5f);
                //Antworten[random].transform.GetChild(1).GetComponentInChildren<TMP_Text>().gameObject.SetActive(false);
                // eher unsichtbar machen sonst error
                list.Remove(random);
            }
        }
        // Blendet spielerselect aus
        for (int j = 0; j < Config.PLAYERLIST.Length; j++)
        {
            if (Config.PLAYERLIST[j].isConnected)
            {
                for (int i = 0; i < 8; i++)
                {
                    Antworten[i].transform.GetChild(3).GetComponent<Image>().sprite = Config.PLAYERLIST[j].icon;
                    Antworten[i].transform.GetChild(3).GetComponent<Image>().color = new Color(255, 255, 255, 0.5f);
                    Antworten[i].transform.GetChild(3).gameObject.SetActive(true);
                }
            }
        }
        // Serverantwort
        Antworten[list[0]].transform.GetChild(1).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/ProfileIcons/empty");
        Antworten[list[0]].transform.GetChild(1).GetComponentInChildren<TMP_Text>().text = "0";
        Antworten[list[0]].transform.GetChild(1).GetComponent<Image>().color = new Color(255, 255, 255, 0.5f);
        //Antworten[list[0]].transform.GetChild(1).GetComponentInChildren<TMP_Text>().gameObject.SetActive(false);
        // eher unsichtbar machen sonst error
        serverantwort = list[0];
        // Vorgefertigte Antwort zeigen
        Antworten[serverantwort].transform.GetChild(2).GetComponentInChildren<TMP_InputField>().text = Config.SLOXIKON_SPIEL.getSelected().getAntwort()[aktuellesThema];
    }
    public void ShowAllAntworten()
    {
        // für den Server vorher schon anzeigen

        string msg = "";
        for (int i = 0; i < 9; i++)
        {
            msg += "[" + i + "]" + Antworten[i].transform.GetChild(2).GetComponentInChildren<TMP_InputField>().text + "[" + i + "]";
            // Blendet ShowTXT button aus
            Antworten[i].transform.GetChild(0).GetChild(0).GetComponent<Button>().enabled = false;
        }
        Broadcast("SloxikonShowAllAntworten " + msg);
    }
    public void ShowAntwort(int antwortindex)
    {
        string msg = "";
        msg += "[" + antwortindex + "]" + Antworten[antwortindex].transform.GetChild(2).GetComponentInChildren<TMP_InputField>().text + "[" + antwortindex + "]";
        // Blendet ShowTXT button aus
        Antworten[antwortindex].transform.GetChild(0).GetChild(0).GetComponent<Button>().enabled = false;
        Broadcast("SloxikonShowAllAntworten " + msg);       
    }
    public void ShowOwner(int ownerindex)
    {

    }
    public void PlayerSelectAnswer(GameObject Player)
    {

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
        Broadcast("#AudioBuzzerPressed " + p.id);
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
        Broadcast("#BuzzerFreigeben");
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
            Broadcast("#SpielerAusgetabt 0");
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
            Broadcast("#SpielerAusgetabt " + player.id + " " + ausgetabt);
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
        Broadcast("#TexteingabeAnzeigen "+ toggle.isOn);
    }
    /// <summary>
    /// Aktualisiert die Antwort die der Spieler eingibt
    /// </summary>
    /// <param name="p"></param>
    /// <param name="data"></param>
    private void SpielerAntwortEingabe(Player p, string data)
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
        Broadcast("#AudioRichtigeAntwort");
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
        Broadcast("#SpielerIstDran "+pId);
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
        Broadcast("#SpielerIstNichtDran " + pId);
    }
    #endregion
}
