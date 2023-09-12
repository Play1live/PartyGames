using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GeheimwörterServer : MonoBehaviour
{
    TMP_Text GeheimTitel;
    TMP_Text GeheimIndex;
    TMP_Text GeheimLoesung;
    int GeheimIndexInt;

    TMP_Text WoerterAnzeige;
    TMP_Text KategorieAnzeige;
    TMP_Text LoesungsWortAnzeige;
    GameObject[] Liste;

    bool buzzerIsOn = false;
    GameObject BuzzerAnzeige;
    GameObject TextEingabeAnzeige;
    GameObject TextAntwortenAnzeige;
    GameObject AustabbenAnzeigen;
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
        PlayerConnected = new bool[Config.SERVER_MAX_CONNECTIONS];
        InitAnzeigen();
        InitGeheimwörter();
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
        Logging.log(Logging.LogType.Normal, "GeheimwörterServer", "OnApplicationQuit", "Server wird geschlossen");
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
    /// <param name="player">Spieler</param>
    /// <param name="data">Befehlsargumente</param>
    /// <param name="cmd">Befehl</param>
    private void Commands(Player player, string data, string cmd)
    {
        // Zeigt alle einkommenden Nachrichten an
        Logging.log(Logging.LogType.Debug, "QuizServer", "Commands", "Eingehende Nachricht: " + cmd + " -> " + data);
        // Sucht nach Command
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "GeheimwörterServer", "Commands", "Unkown Command: " + cmd + " -> " + data);
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

            case "#JoinGeheimwörter":
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
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            Player p = Config.PLAYERLIST[i];
            msg += "[TRENNER][ID]" + p.id + "[ID][PUNKTE]" + p.points + "[PUNKTE][ONLINE]"+p.isConnected+"[ONLINE]";
            if (p.isConnected && PlayerConnected[i])
            {
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
    /// Initialisiert die Anzeigen zu beginn
    /// </summary>
    private void InitAnzeigen()
    {
        // Buzzer Deaktivieren
        GameObject.Find("Einstellungen/BuzzerAktivierenToggle").GetComponent<Toggle>().isOn = false;
        BuzzerAnzeige = GameObject.Find("Einstellungen/BuzzerIstAktiviert");
        BuzzerAnzeige.SetActive(false);
        buzzerIsOn = false;
        // Spieler Antworten
        GameObject.Find("Einstellungen/TexteingabeAnzeigenToggle").GetComponent<Toggle>().isOn = false;
        TextEingabeAnzeige = GameObject.Find("Einstellungen/TexteingabeWirdAngezeigt");
        TextEingabeAnzeige.SetActive(false);
        GameObject.Find("Einstellungen/TextantwortenAnzeigenToggle").GetComponent<Toggle>().isOn = false;
        TextAntwortenAnzeige = GameObject.Find("Einstellungen/TextantwortenWerdenAngezeigt");
        TextAntwortenAnzeige.SetActive(false);
        // Austabben wird gezeigt
        GameObject.Find("Einstellungen/AusgetabtSpielernZeigenToggle").GetComponent<Toggle>().isOn = false;
        AustabbenAnzeigen = GameObject.Find("Einstellungen/AusgetabtWirdSpielernGezeigen");
        AustabbenAnzeigen.SetActive(false);
        // Punkte Pro Richtige Antwort
        GameObject.Find("Einstellungen/PunkteProRichtigeAntwort").GetComponent<TMP_InputField>().text = ""+PunkteProRichtige;
        // Punkte Pro Falsche Antwort
        GameObject.Find("Einstellungen/PunkteProFalscheAntwort").GetComponent<TMP_InputField>().text = ""+PunkteProFalsche;
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
            SpielerAnzeige[i, 6] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/SpielerAntwort"); // Textantworten
            
            SpielerAnzeige[i, 0].SetActive(false); // Spieler Anzeige
            SpielerAnzeige[i, 1].SetActive(false); // BuzzerPressed Umrandung
            SpielerAnzeige[i, 3].SetActive(false); // Ausgetabt Einblendung
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
            Logging.log(Logging.LogType.Normal, "GeheimwörterServer", "SpielerBuzzered", p.name + " - " + DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second + ":" + DateTime.Now.Millisecond);
            return;
        }
        Logging.log(Logging.LogType.Warning, "GeheimwörterServer", "SpielerBuzzered", "B: " + p.name + " - " + DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second + ":" + DateTime.Now.Millisecond);
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
        Logging.log(Logging.LogType.Warning, "GeheimwörterServer", "SpielerBuzzerFreigeben", "Buzzer wurde freigegeben.");
        ServerUtils.BroadcastImmediate("#BuzzerFreigeben");
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
        ServerUtils.BroadcastImmediate("#TexteingabeAnzeigen " + toggle.isOn);
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
            ServerUtils.BroadcastImmediate("#TextantwortenAnzeigen [BOOL]" + toggle.isOn + "[BOOL]");
            return;
        }
        string msg = "";
        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
        {
            msg = msg + "[ID" + (i + 1) + "]" + SpielerAnzeige[i, 6].GetComponentInChildren<TMP_InputField>().text + "[ID" + (i + 1) + "]";
        }
        ServerUtils.BroadcastImmediate("#TextantwortenAnzeigen [BOOL]" + toggle.isOn + "[BOOL][TEXT]" + msg);
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
    /// <param name="player">Spieler</param>
    /// <param name="data">bool</param>
    private void ClientFocusChange(Player player, string data)
    {
        bool ausgetabt = !Boolean.Parse(data);
        SpielerAnzeige[(player.id - 1), 3].SetActive(ausgetabt); // Ausgetabt Einblednung
        if (AustabbenAnzeigen.activeInHierarchy)
            ServerUtils.BroadcastImmediate("#SpielerAusgetabt " + player.id + " " + ausgetabt);
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
        int pId = Int32.Parse(player.transform.parent.parent.name.Replace("Player (", "").Replace(")", ""));
        ServerUtils.BroadcastImmediate(Config.GAME_TITLE + "#AudioRichtigeAntwort " + pId + "*" + PunkteProRichtige);
        RichtigeAntwortSound.Play();
        int pIndex = Player.getPosInLists(pId);
        Config.PLAYERLIST[pIndex].points += PunkteProRichtige;
        UpdateSpieler();
    }
    /// <summary>
    /// Vergibt an alle anderen Spieler Punkte bei einer falschen Antwort
    /// </summary>
    /// <param name="player"></param>
    public void PunkteFalscheAntwort(GameObject player)
    {
        int pId = Int32.Parse(player.transform.parent.parent.name.Replace("Player (", "").Replace(")", ""));
        ServerUtils.BroadcastImmediate(Config.GAME_TITLE + "#AudioFalscheAntwort " + pId + "*" + PunkteProFalsche);
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
    /// <param name="button">Spieler</param>
    public void SpielerIstDran(GameObject button)
    {
        int pId = Int32.Parse(button.transform.parent.parent.name.Replace("Player (", "").Replace(")", ""));
        SpielerAnzeige[(pId - 1), 1].SetActive(true);
        buzzerIsOn = false;
        ServerUtils.BroadcastImmediate("#SpielerIstDran " + pId);
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
        ServerUtils.BroadcastImmediate("#SpielerIstNichtDran " + pId);
    }
    #endregion
    #region Geheimwörter Anzeige
    /// <summary>
    /// Initialisiert die Anzeigen
    /// </summary>
    private void InitGeheimwörter()
    {
        GeheimTitel = GameObject.Find("GeheimwörterAnzeige/Server/Titel").GetComponent<TMP_Text>();
        GeheimTitel.text = Config.GEHEIMWOERTER_SPIEL.getSelected().getTitel();
        GeheimIndexInt = 0;
        GeheimIndex = GameObject.Find("GeheimwörterAnzeige/Server/Index").GetComponent<TMP_Text>();
        GeheimIndex.text = (GeheimIndexInt+1)+"/" + Config.GEHEIMWOERTER_SPIEL.getSelected().getGeheimwörter().Count;
        GeheimLoesung = GameObject.Find("GeheimwörterAnzeige/Server/Lösung").GetComponent<TMP_Text>();
        GeheimLoesung.text = Config.GEHEIMWOERTER_SPIEL.getSelected().getGeheimwörter()[GeheimIndexInt].getLoesung();

        GameObject.Find("Einstellungen/ChangeGeheimwörter").GetComponent<TMP_Dropdown>().ClearOptions();
        GameObject.Find("Einstellungen/ChangeGeheimwörter").GetComponent<TMP_Dropdown>().AddOptions(Config.GEHEIMWOERTER_SPIEL.getGamesAsStringList());
        GameObject.Find("Einstellungen/ChangeGeheimwörter").GetComponent<TMP_Dropdown>().value = Config.GEHEIMWOERTER_SPIEL.getIndex(Config.GEHEIMWOERTER_SPIEL.getSelected());

        WoerterAnzeige = GameObject.Find("GeheimwörterAnzeige/Outline/Woerter").GetComponent<TMP_Text>();
        WoerterAnzeige.text = "";
        KategorieAnzeige = GameObject.Find("GeheimwörterAnzeige/Outline/Kategorien").GetComponent<TMP_Text>();
        KategorieAnzeige.text = "";
        LoesungsWortAnzeige = GameObject.Find("GeheimwörterAnzeige/Outline/Loesungswort/Text").GetComponent<TMP_Text>();
        LoesungsWortAnzeige.text = "";
        LoesungsWortAnzeige.transform.parent.gameObject.SetActive(false);
        Liste = new GameObject[30];
        for (int i = 0; i < 30; i++)
        {
            Liste[i] = GameObject.Find("GeheimwörterAnzeige/Outline/Liste/Element (" + i + ")");
            Liste[i].SetActive(false);
        }
    }
    /// <summary>
    /// Wechselt das Geheimwörter Game
    /// </summary>
    /// <param name="drop">Spielauswahl</param>
    public void ChangeGeheimwörter(TMP_Dropdown drop)
    {
        Config.GEHEIMWOERTER_SPIEL.setSelected(Config.GEHEIMWOERTER_SPIEL.getListe(drop.value));

        GeheimTitel.text = Config.GEHEIMWOERTER_SPIEL.getSelected().getTitel();
        GeheimIndexInt = 0;
        GeheimIndex.text = (GeheimIndexInt + 1) + "/" + Config.GEHEIMWOERTER_SPIEL.getSelected().getGeheimwörter().Count;
        GeheimLoesung.text = Config.GEHEIMWOERTER_SPIEL.getSelected().getGeheimwörter()[GeheimIndexInt].getLoesung();

        WoerterAnzeige.text = "";
        KategorieAnzeige.text = "";
        LoesungsWortAnzeige.text = "";
        LoesungsWortAnzeige.transform.parent.gameObject.SetActive(false);
        for (int i = 0; i < 30; i++)
        {
            Liste[i].SetActive(false);
        }
        ServerUtils.BroadcastImmediate("#GeheimwoerterHide");
    }
    /// <summary>
    /// Blendet den Lösungscode ein
    /// </summary>
    public void CodeZeigen()
    {
        string msg = "";
        foreach (string c in Config.GEHEIMWOERTER_SPIEL.getSelected().getCode())
        {
            msg += "<#>" + c;
        }
        if (msg.Length > 3)
            msg = msg.Substring("<#>".Length);
        ServerUtils.BroadcastImmediate("#GeheimwoerterCode " + msg);

        for (int i = 0; i < Config.GEHEIMWOERTER_SPIEL.getSelected().getCode().Count; i++)
        {
            Liste[i].transform.GetChild(0).gameObject.GetComponent<TMP_Text>().text = Config.GEHEIMWOERTER_SPIEL.getSelected().getCode()[i].Split('=')[1];
            Liste[i].transform.GetChild(1).gameObject.GetComponent<TMP_Text>().text = Config.GEHEIMWOERTER_SPIEL.getSelected().getCode()[i].Split('=')[0];
            Liste[i].SetActive(true);
        }
    }
    /// <summary>
    /// Navigiert durch die Runden
    /// </summary>
    /// <param name="bewegen"></param>
    public void NaechstesVorherigesElement(int bewegen)
    {
        if ((GeheimIndexInt + bewegen) < 0 || (GeheimIndexInt + bewegen) >= Config.GEHEIMWOERTER_SPIEL.getSelected().getGeheimwörter().Count)
            return;
        GeheimIndexInt += bewegen;

        ServerUtils.BroadcastImmediate("#GeheimwoerterNeue");

        WoerterAnzeige.text = "";
        KategorieAnzeige.text = "";
        LoesungsWortAnzeige.text = "";
        LoesungsWortAnzeige.transform.parent.gameObject.SetActive(false);

        GeheimIndex.text = (GeheimIndexInt+1)+"/"+Config.GEHEIMWOERTER_SPIEL.getSelected().getGeheimwörter().Count;
        GeheimLoesung.text = Config.GEHEIMWOERTER_SPIEL.getSelected().getGeheimwörter()[GeheimIndexInt].getLoesung();
    }
    /// <summary>
    /// Zeigt die Worte der Runde an
    /// </summary>
    public void WorteZeigen()
    {
        WoerterAnzeige.text = Config.GEHEIMWOERTER_SPIEL.getSelected().getGeheimwörter()[GeheimIndexInt].getWorte();
        KategorieAnzeige.text = Config.GEHEIMWOERTER_SPIEL.getSelected().getGeheimwörter()[GeheimIndexInt].getKategorien();

        ServerUtils.BroadcastImmediate("#GeheimwoerterWorteZeigen " + WoerterAnzeige.text.Replace("\n", "<>"));
    }
    /// <summary>
    /// Blendet die Lösung der Runde ein
    /// </summary>
    public void LoesungZeigen()
    {
        LoesungsWortAnzeige.text = Config.GEHEIMWOERTER_SPIEL.getSelected().getGeheimwörter()[GeheimIndexInt].getLoesung();
        LoesungsWortAnzeige.transform.parent.gameObject.SetActive(true);

        ServerUtils.BroadcastImmediate("#GeheimwoerterLoesung " + LoesungsWortAnzeige.text + "[TRENNER]" + KategorieAnzeige.text.Replace("\n", "<>"));
    }
    #endregion
}
