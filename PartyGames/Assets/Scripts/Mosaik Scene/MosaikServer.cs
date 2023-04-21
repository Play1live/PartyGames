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

public class MosaikServer : MonoBehaviour
{
    Vector2 BildRect;
    GameObject[] Bild;

    TMP_Text BildTitel;
    Vector2 VorschauRect;
    GameObject[] BildVorschau;
    GameObject bildListeText;
    List<int> coverlist;
    int bildIndex;

    bool buzzerIsOn = false;
    GameObject BuzzerAnzeige;
    GameObject AustabbenAnzeigen;

    GameObject[,] SpielerAnzeige;
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
        InitMosaik();
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
                        Logging.log(Logging.LogType.Normal, "MosaikServer", "Update", "Spieler hat die Verbindung getrennt. ID: " + Config.PLAYERLIST[i].id);
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
            Logging.log(Logging.LogType.Warning, "MosaikServer", "SendMSG", "Nachricht an Client: " + sc.id + " (" + sc.name + ") konnte nicht gesendet werden.", e);
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
    }
    /// <summary>
    /// Einkommende Nachrichten die von Spielern an den Server gesendet werden.
    /// </summary>
    /// <param name="spieler">Spieler</param>
    /// <param name="data">Nachricht</param>
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
        Logging.log(Logging.LogType.Debug, "MosaikServer", "Commands", "Eingehende Nachricht: " + player.name + " " + player.id + " -> " + cmd + "   ---   " + data);
        // Sucht nach Command
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "MosaikServer", "Commands", "Unkown Command: " + cmd + " -> " + data);
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

            case "#JoinMosaik":
                PlayerConnected[player.id - 1] = true;
                UpdateSpielerBroadcast();
                break;
            case "#SpielerBuzzered":
                SpielerBuzzered(player);
                break;
            case "#SpielerHatBildGeladen":
                SpielerHatBildGeladen(player, data);
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
    /// <param name="player">Spieler</param>
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
    /// <returns>#UpdateSpieler ...</returns>
    private string UpdateSpieler()
    {
        string msg = "#UpdateSpieler [ID]0[ID][PUNKTE]" + Config.SERVER_PLAYER_POINTS + "[PUNKTE]";
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            Player p = Config.PLAYERLIST[i];
            msg += "[TRENNER][ID]" + p.id + "[ID][PUNKTE]" + p.points + "[PUNKTE]";
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
        Logging.log(Logging.LogType.Debug, "MosaikServer", "InitAnzeigen", "Initialisiert die Anzeigen");
        // Buzzer Deaktivieren
        GameObject.Find("Einstellungen/BuzzerAktivierenToggle").GetComponent<Toggle>().isOn = false;
        BuzzerAnzeige = GameObject.Find("Einstellungen/BuzzerIstAktiviert");
        BuzzerAnzeige.SetActive(false);
        buzzerIsOn = false;
        // Austabben wird gezeigt
        GameObject.Find("Einstellungen/AusgetabtSpielernZeigenToggle").GetComponent<Toggle>().isOn = false;
        AustabbenAnzeigen = GameObject.Find("Einstellungen/AusgetabtWirdSpielernGezeigen");
        AustabbenAnzeigen.SetActive(false);
        // Punkte Pro Richtige Antwort
        GameObject.Find("Einstellungen/PunkteProRichtigeAntwort").GetComponent<TMP_InputField>().text = "" + PunkteProRichtige;
        // Punkte Pro Falsche Antwort
        GameObject.Find("Einstellungen/PunkteProFalscheAntwort").GetComponent<TMP_InputField>().text = "" + PunkteProFalsche;
        // Spieler Anzeige
        SpielerAnzeige = new GameObject[Config.SERVER_MAX_CONNECTIONS, 6]; // Anzahl benötigter Elemente
        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
        {
            PlayerConnected[i] = false;
            SpielerAnzeige[i, 0] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")"); // Spieler Anzeige
            SpielerAnzeige[i, 1] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/BuzzerPressed"); // BuzzerPressed Umrandung
            SpielerAnzeige[i, 2] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Icon"); // Spieler Icon
            SpielerAnzeige[i, 3] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Ausgetabt"); // Ausgetabt Einblednung
            SpielerAnzeige[i, 4] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Infobar/Name"); // Spieler Name
            SpielerAnzeige[i, 5] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Infobar/Punkte"); // Spieler Punkte

            SpielerAnzeige[i, 0].SetActive(false); // Spieler Anzeige
            SpielerAnzeige[i, 1].SetActive(false); // BuzzerPressed Umrandung
            SpielerAnzeige[i, 3].SetActive(false); // Ausgetabt Einblendung
        }
        GameObject SpielerGeladenAnzeige = GameObject.Find("SpielerGeladenAnzeige");
        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
        {
            SpielerGeladenAnzeige.transform.GetChild(i).GetComponent<Image>().sprite = Config.PLAYERLIST[i].icon;
            SpielerGeladenAnzeige.transform.GetChild(i).gameObject.SetActive(false);
        }
    }
    #region Buzzer
    /// <summary>
    /// Aktiviert/Deaktiviert den Buzzer für alle Spieler
    /// </summary>
    /// <param name="toggle">Toggle</param>
    public void BuzzerAktivierenToggle(Toggle toggle)
    {
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
            Logging.log(Logging.LogType.Normal, "MosaikServer", "SpielerBuzzered", p.name + " - " + DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second + ":" + DateTime.Now.Millisecond);
            return;
        }
        Logging.log(Logging.LogType.Warning, "MosaikServer", "SpielerBuzzered", "B: " + p.name + " - " + DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second + ":" + DateTime.Now.Millisecond);
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
        Logging.log(Logging.LogType.Warning, "MosaikServer", "SpielerBuzzerFreigeben", "Buzzer wird freigegeben");
        Broadcast("#BuzzerFreigeben");
    }
    #endregion
    #region Spieler Ausgetabt Anzeige
    /// <summary>
    /// Austaben wird allen/keinen Spielern angezeigt
    /// </summary>
    /// <param name="toggle">Toogle</param>
    public void AustabenAllenZeigenToggle(Toggle toggle)
    {
        AustabbenAnzeigen.SetActive(toggle.isOn);
        if (toggle.isOn == false)
            Broadcast("#SpielerAusgetabt 0");
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
            Broadcast("#SpielerAusgetabt " + player.id + " " + ausgetabt);
    }
    #endregion
    #region Punkte
    /// <summary>
    /// Punkte Pro Richtige Antworten Anzeigen
    /// </summary>
    /// <param name="input">Punkteeingabe</param>
    public void ChangePunkteProRichtigeAntwort(TMP_InputField input)
    {
        PunkteProRichtige = Int32.Parse(input.text);
    }
    /// <summary>
    /// Punkte Pro Falsche Antworten Anzeigen
    /// </summary>
    /// <param name="input">Punkteeingabe</param>
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
    /// <param name="player">Spieler</param>
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
    /// <param name="button">Spielerauswahl</param>
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
    /// <param name="input">Punkteeingabe</param>
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
        Broadcast("#SpielerIstDran " + pId);
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
        Broadcast("#SpielerIstNichtDran " + pId);
    }
    #endregion
    #region Mosaik Anzeige
    /// <summary>
    /// Initialisiert die Anzeigen des Quizzes
    /// </summary>
    private void InitMosaik()
    {
        Logging.log(Logging.LogType.Debug, "MosaikServer", "InitMosaik", "Anzeigen werden initialisiert");
        BildTitel = GameObject.Find("MosaikAnzeige/Server/Titel").GetComponent<TMP_Text>();
        BildTitel.text = "(0/"+Config.MOSAIK_SPIEL.getSelected().getSprites().Count + ") " + Config.MOSAIK_SPIEL.getBeispiel().name;
        // ImageAnzeige
        Bild = new GameObject[49];
        BildVorschau = new GameObject[49];
        coverlist = new List<int>();
        for (int i = 0; i < 49; i++)
        {
            Bild[i] = GameObject.Find("MosaikAnzeige/BildImage/Cover (" + i + ")");
            Bild[i].GetComponent<Animator>().enabled = false;
            Bild[i].GetComponent<RectTransform>().sizeDelta = new Vector2(100, 100);
            Bild[i].GetComponent<RectTransform>().eulerAngles = new Vector3(0, 0, 0);
            Bild[i].GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
            BildVorschau[i] = GameObject.Find("MosaikAnzeige/Server/Vorschau/Cover (" + i + ")");
            coverlist.Add(i);
        }
        BildRect = new Vector2(Bild[0].transform.parent.GetComponent<RectTransform>().rect.width, Bild[0].transform.parent.GetComponent<RectTransform>().rect.height);
        Bild[0].transform.parent.gameObject.GetComponent<Image>().sprite = Config.MOSAIK_SPIEL.getBeispiel();
        Bild[0].transform.parent.gameObject.SetActive(false);

        BildVorschau[0].transform.parent.GetComponent<Image>().sprite = Config.MOSAIK_SPIEL.getBeispiel();
        VorschauRect = new Vector2(BildVorschau[0].transform.parent.GetComponent<RectTransform>().rect.width, BildVorschau[0].transform.parent.GetComponent<RectTransform>().rect.height);

        bildIndex = 0;
        bildListeText = GameObject.Find("Server/TextVorschau");
        bildListeText.GetComponent<TMP_Text>().text = "- Beispiel";
        for (int i = 0; i < Config.MOSAIK_SPIEL.getSelected().getNames().Count; i++)
        {
            bildListeText.GetComponent<TMP_Text>().text += "\n- " + Config.MOSAIK_SPIEL.getSelected().getNames()[i];
        }
    }
    /// <summary>
    /// Blendet das Nächste/Vorherige Element in der Vorschau ein
    /// </summary>
    /// <param name="vor">+-1</param>
    public void MosaikNächstesElement(int vor)
    {
        if ((bildIndex + vor) < 0 || (bildIndex + vor) > Config.MOSAIK_SPIEL.getSelected().getURLs().Count)
        {
            return;
        }
        bildIndex += vor;

        if (bildIndex == 0)
        {
            BildTitel.text = "("+ bildIndex + "/"+ Config.MOSAIK_SPIEL.getSelected().getURLs().Count+") "+ Config.MOSAIK_SPIEL.getBeispiel().name;
            LoadImageIntoPreview();
        }
        else
        {
            BildTitel.text = "(" + bildIndex + "/" + Config.MOSAIK_SPIEL.getSelected().getURLs().Count + ") " + Config.MOSAIK_SPIEL.getSelected().getNames()[bildIndex - 1];

            // Schauen ob das Bild bereits geladen wurde
            if (Config.MOSAIK_SPIEL.getSelected().getIstGeladen()[bildIndex - 1])
            {
                LoadImageIntoPreview();
            }
            // Bild Herunterladen
            else
            {
                StartCoroutine(LoadImageFromWeb(Config.MOSAIK_SPIEL.getSelected().getURLs()[bildIndex - 1]));
            }
        }

        // Blendet Cover ein
        for (int i = 0; i < 49; i++)
        {
            BildVorschau[i].GetComponent<RectTransform>().sizeDelta = new Vector2(70, 70);
            BildVorschau[i].GetComponent<RectTransform>().eulerAngles = new Vector3(0, 0, 0);
            BildVorschau[i].GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
            BildVorschau[i].SetActive(true);
        }
    }
    /// <summary>
    /// Lädt ein Bild aus dem Internet und zeigt dieses an
    /// </summary>
    /// <param name="imageUrl">URL</param>
    IEnumerator LoadImageFromWeb(string imageUrl)
    {
        Logging.log(Logging.LogType.Normal, "MosaikServer", "LoadImageFromWeb", "Lädt Bild herunter: " + imageUrl);
        // TEIL 1: Download des Bildes
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUrl);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Logging.log(Logging.LogType.Warning, "MosaikServer", "LoadImageFromWeb", "Bild konnte nicht herunter geladen werden: " + www.error);
        }
        else
        {
            Texture2D myTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;
            Sprite sprite = Sprite.Create(myTexture, new Rect(0, 0, myTexture.width, myTexture.height), new Vector2(0.5f, 0.5f), 100);
            Config.MOSAIK_SPIEL.getSelected().getSprites()[bildIndex - 1] = sprite;
            Config.MOSAIK_SPIEL.getSelected().getIstGeladen()[bildIndex - 1] = true;

            LoadImageIntoPreview();
        }
        yield return null;
    }
    /// <summary>
    /// Zeigt an, wenn ein Spieler das Bild geladen hat
    /// </summary>
    /// <param name="p">Spieler</param>
    /// <param name="data"></param>
    private void SpielerHatBildGeladen(Player p, string data)
    {
        if (data.Equals("error"))
        {
            Logging.log(Logging.LogType.Warning, "MosaikServer", "SpielerHatBildGeladen", "Spieler konnte Bild nicht laden.");
            return;
        }
        GameObject SpielerGeladenAnzeige = GameObject.Find("SpielerGeladenAnzeige");
        SpielerGeladenAnzeige.transform.GetChild(p.id - 1).gameObject.SetActive(true);
    }
    /// <summary>
    /// Zeigt allen das ausgewählte Element
    /// </summary>
    /// <param name="einblenden">bool</param>
    public void MosaikEinblendenAusblenden(bool einblenden)
    {
        if (einblenden == true && bildIndex > 0 && Config.MOSAIK_SPIEL.getSelected().getIstGeladen()[bildIndex - 1] == false)
            return;

        if (bildIndex == 0)
            Broadcast("#MosaikEinblendenAusblenden " + einblenden + "[!#!]Beispiel");
        else
            Broadcast("#MosaikEinblendenAusblenden " + einblenden + "[!#!]" + Config.MOSAIK_SPIEL.getSelected().getURLs()[bildIndex - 1]);

        if (einblenden == true)
        {
            // Setzt SpielerGeladenAnzeige zurück
            GameObject SpielerGeladenAnzeige = GameObject.Find("SpielerGeladenAnzeige");
            for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
            {
                SpielerGeladenAnzeige.transform.GetChild(i).gameObject.SetActive(false);
            }

            coverlist = new List<int>();
            if (bildIndex == 0)
            {
                Bild[0].transform.parent.gameObject.GetComponent<Image>().sprite = Config.MOSAIK_SPIEL.getBeispiel();
                Bild[0].transform.parent.gameObject.SetActive(true);
            }
            else
            {
                Bild[0].transform.parent.gameObject.GetComponent<Image>().sprite = Config.MOSAIK_SPIEL.getSelected().getSprites()[bildIndex - 1];
                Bild[0].transform.parent.gameObject.SetActive(true);
            }
            // TEIL 2: Zeigt Bild in der Szene an und behält die Seitenverhältnisse bei
            #region Teil 2
            Bild[0].transform.parent.GetComponent<RectTransform>().sizeDelta = BildRect;
            Texture2D myTexture = Bild[0].transform.parent.gameObject.GetComponent<Image>().sprite.texture;
            Sprite sprite = Sprite.Create(myTexture, new Rect(0, 0, myTexture.width, myTexture.height), new Vector2(0.5f, 0.5f), 100);
            GameObject imageObject = Bild[0].transform.parent.gameObject;
            imageObject.GetComponent<Image>().sprite = sprite;

            // Skalierung des Bildes, um das Seitenverhältnis beizubehalten und um sicherzustellen, dass das Bild nicht größer als das Image ist
            float imageWidth = imageObject.GetComponent<RectTransform>().rect.width;
            float imageHeight = imageObject.GetComponent<RectTransform>().rect.height;
            float textureWidth = myTexture.width;
            float textureHeight = myTexture.height;
            float widthRatio = imageWidth / textureWidth;
            float heightRatio = imageHeight / textureHeight;
            float ratio = Mathf.Min(widthRatio, heightRatio);
            float newWidth = textureWidth * ratio;
            float newHeight = textureHeight * ratio;

            // Anpassung der Größe des Image-GameObjects und des Sprite-Components
            RectTransform imageRectTransform = imageObject.GetComponent<RectTransform>();
            imageRectTransform.sizeDelta = new Vector2(newWidth, newHeight);
            imageObject.GetComponent<Image>().sprite = sprite;
            #endregion
            // TEIL 3: Passt die Überlagerten Images an die größe an
            #region Teil 3
            float cellWidth = newWidth / 7;
            float cellHeight = newHeight / 7;
            imageObject.GetComponent<GridLayoutGroup>().cellSize = new Vector2(cellWidth, cellHeight);
            #endregion
            // TEIL 4: Überlagerten Images muster geben
            #region Teil 4
            string[] himmelrichtungen = new string[] { "E", "N", "NE", "NW", "S", "SE", "SW", "W" };
            for (int i = 0; i < 49; i++)
            {
                int random = UnityEngine.Random.Range(0, himmelrichtungen.Length);
                imageObject.transform.GetChild(i).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/GUI/Arrow " + himmelrichtungen[random]);
            }
            #endregion

            // Blendet Cover ein
            for (int i = 0; i < 49; i++)
            {
                coverlist.Add(i);
                Bild[i].GetComponent<RectTransform>().sizeDelta = new Vector2(100, 100);
                Bild[i].GetComponent<RectTransform>().eulerAngles = new Vector3(0, 0, 0);
                Bild[i].GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
                Bild[i].GetComponent<Animator>().enabled = false;
                Bild[i].SetActive(true);
            }
            // Blendet Cover ein
            for (int i = 0; i < 49; i++)
            {
                BildVorschau[i].SetActive(true);
            }
        }
        else
        {
            Bild[0].transform.parent.gameObject.SetActive(false);
            // Setzt SpielerGeladenAnzeige zurück
            GameObject SpielerGeladenAnzeige = GameObject.Find("SpielerGeladenAnzeige");
            for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
            {
                SpielerGeladenAnzeige.transform.GetChild(i).gameObject.SetActive(false);
            }
        }

    }
    /// <summary>
    /// Löst zufällige Cover auf
    /// </summary>
    public void MosaikZufälligesAuflösen()
    {
        if (coverlist.Count == 0)
            return;
        int random = coverlist[UnityEngine.Random.Range(0, coverlist.Count)];
        coverlist.Remove(random);

        Broadcast("#MosaikCoverAuflösen " + random);

        Bild[random].SetActive(false);
        Bild[random].GetComponent<Animator>().enabled = false;
        Bild[random].GetComponent<Animator>().enabled = true;
        Bild[random].SetActive(true);
        BildVorschau[random].SetActive(false);
    }
    /// <summary>
    /// Löst bestimmtes Cover auf
    /// </summary>
    /// <param name="go">Coverauswahl</param>
    public void MosaikBestimmtesAuflösen(Button go)
    {
        int index = Int32.Parse(go.gameObject.name.Replace("Cover (", "").Replace(")", ""));
        Broadcast("#MosaikCoverAuflösen " + index);

        Bild[index].SetActive(false);
        Bild[index].GetComponent<Animator>().enabled = false;
        Bild[index].GetComponent<Animator>().enabled = true;
        Bild[index].SetActive(true);
        go.gameObject.SetActive(false);
        coverlist.Remove(index);
    }
    /// <summary>
    /// Löst alle Cover auf
    /// </summary>
    public void MosaikAllesAuflösen()
    {
        if (coverlist.Count == 0)
            return;
        Broadcast("#MosaikAllesAuflösen");

        while (coverlist.Count > 0)
        {
            Bild[coverlist[0]].SetActive(false);
            Bild[coverlist[0]].GetComponent<Animator>().enabled = false;
            Bild[coverlist[0]].GetComponent<Animator>().enabled = true;
            Bild[coverlist[0]].SetActive(true);
            BildVorschau[coverlist[0]].SetActive(false);
            coverlist.RemoveAt(0);
        }
    }
    #endregion
    /// <summary>
    /// Lädt Bild aus dem Internet und zeigt dieses
    /// </summary>
    private void LoadImageIntoPreview()
    {
        BildVorschau[0].transform.parent.GetComponent<RectTransform>().sizeDelta = VorschauRect;
        GameObject imageObject = BildVorschau[0].transform.parent.gameObject;
        Texture2D myTexture;
        Sprite sprite;

        if (bildIndex == 0)
        {
            myTexture = Config.MOSAIK_SPIEL.getBeispiel().texture;
            sprite = Sprite.Create(myTexture, new Rect(0, 0, myTexture.width, myTexture.height), new Vector2(0.5f, 0.5f), 100);
        }
        else
        {
            myTexture = Config.MOSAIK_SPIEL.getSelected().getSprites()[bildIndex -1].texture;
            sprite = Sprite.Create(myTexture, new Rect(0, 0, myTexture.width, myTexture.height), new Vector2(0.5f, 0.5f), 100);
        }

        // Skalierung des Bildes, um das Seitenverhältnis beizubehalten und um sicherzustellen, dass das Bild nicht größer als das Image ist
        float imageWidth = imageObject.GetComponent<RectTransform>().rect.width;
        float imageHeight = imageObject.GetComponent<RectTransform>().rect.height;
        float textureWidth = myTexture.width;
        float textureHeight = myTexture.height;
        float widthRatio = imageWidth / textureWidth;
        float heightRatio = imageHeight / textureHeight;
        float ratio = Mathf.Min(widthRatio, heightRatio);
        float newWidth = textureWidth * ratio;
        float newHeight = textureHeight * ratio;

        // Anpassung der Größe des Image-GameObjects und des Sprite-Components
        RectTransform imageRectTransform = imageObject.GetComponent<RectTransform>();
        imageRectTransform.sizeDelta = new Vector2(newWidth, newHeight);
        imageObject.GetComponent<Image>().sprite = sprite;

        // TEIL 3: Passt die Überlagerten Images an die größe an
        #region Teil 3
        float cellWidth = newWidth / 7;
        float cellHeight = newHeight / 7;
        imageObject.GetComponent<GridLayoutGroup>().cellSize = new Vector2(cellWidth, cellHeight);
        StartCoroutine(ToggleGridLayoutGroup(imageObject));
        #endregion
        // TEIL 4: Überlagerten Images muster geben
        #region Teil 4
        string[] himmelrichtungen = new string[] { "E", "N", "NE", "NW", "S", "SE", "SW", "W" };
        for (int i = 0; i < 49; i++)
        {
            int random = UnityEngine.Random.Range(0, himmelrichtungen.Length);
            imageObject.transform.GetChild(i).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/GUI/Arrow " + himmelrichtungen[random]);
        }
        #endregion
    }
    /// <summary>
    /// Toggle GridLayoutGroup
    /// </summary>
    /// <param name="ob"></param>
    IEnumerator ToggleGridLayoutGroup(GameObject ob)
    {
        yield return new WaitForSeconds(0.01f);
        ob.GetComponent<GridLayoutGroup>().enabled = true;
        yield return new WaitForSeconds(0.01f);
        ob.GetComponent<GridLayoutGroup>().enabled = false;
        yield return null;
    }
    /// <summary>
    /// Lädt ein Bild per Url aus dem Netz
    /// </summary>
    /// <param name="input">Urleingabe</param>
    public void DownloadCustom(TMP_InputField input)
    {
        if (input.text.Length == 0)
            return;
        Broadcast("#DownloadCustom "+ input.text);

        // Setzt SpielerGeladenAnzeige zurück
        GameObject SpielerGeladenAnzeige = GameObject.Find("SpielerGeladenAnzeige");
        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
        {
            SpielerGeladenAnzeige.transform.GetChild(i).gameObject.SetActive(false);
        }

        StartCoroutine(LoadImageFromWebIntoScene(input.text));

        // Blendet Cover ein
        for (int i = 0; i < 49; i++)
        {
            BildVorschau[i].GetComponent<RectTransform>().sizeDelta = new Vector2(70, 70);
            BildVorschau[i].GetComponent<RectTransform>().eulerAngles = new Vector3(0, 0, 0);
            BildVorschau[i].GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
            BildVorschau[i].SetActive(true);
        }
    }
    /// <summary>
    /// Lädt ein Bild aus dem Netz direkt in die Scene
    /// </summary>
    /// <param name="imageUrl">URL</param>
    IEnumerator LoadImageFromWebIntoScene(string imageUrl)
    {
        Logging.log(Logging.LogType.Normal, "MosaikServer", "LoadImageFromWebIntoScene", "Lädt Bild herunter: " + imageUrl);
        // TEIL 1: Download des Bildes
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUrl);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Logging.log(Logging.LogType.Warning, "MosaikServer", "LoadImageFromWebIntoScene", "Bild konnte nicht heruntergeladen werden: " + www.error);
            yield break;
        }
        else
        {
            Texture2D myTexture2 = ((DownloadHandlerTexture)www.downloadHandler).texture;
            Sprite sprite2 = Sprite.Create(myTexture2, new Rect(0, 0, myTexture2.width, myTexture2.height), new Vector2(0.5f, 0.5f), 100);

            Bild[0].transform.parent.gameObject.GetComponent<Image>().sprite = sprite2;
            Bild[0].transform.parent.gameObject.SetActive(true);

            LoadImageIntoPreview();
        }
        yield return null;

        BildVorschau[0].transform.parent.GetComponent<RectTransform>().sizeDelta = VorschauRect;
        GameObject imageObject1 = BildVorschau[0].transform.parent.gameObject;
        Texture2D myTexture1 = Bild[0].transform.parent.gameObject.GetComponent<Image>().sprite.texture;
        Sprite sprite1 = Sprite.Create(myTexture1, new Rect(0, 0, myTexture1.width, myTexture1.height), new Vector2(0.5f, 0.5f), 100);

        // Skalierung des Bildes, um das Seitenverhältnis beizubehalten und um sicherzustellen, dass das Bild nicht größer als das Image ist
        float imageWidth1 = imageObject1.GetComponent<RectTransform>().rect.width;
        float imageHeight1 = imageObject1.GetComponent<RectTransform>().rect.height;
        float textureWidth1 = myTexture1.width;
        float textureHeight1 = myTexture1.height;
        float widthRatio1 = imageWidth1 / textureWidth1;
        float heightRatio1 = imageHeight1 / textureHeight1;
        float ratio1 = Mathf.Min(widthRatio1, heightRatio1);
        float newWidth1 = textureWidth1 * ratio1;
        float newHeight1 = textureHeight1 * ratio1;

        // Anpassung der Größe des Image-GameObjects und des Sprite-Components
        RectTransform imageRectTransform1 = imageObject1.GetComponent<RectTransform>();
        imageRectTransform1.sizeDelta = new Vector2(newWidth1, newHeight1);
        imageObject1.GetComponent<Image>().sprite = sprite1;

        // TEIL 3: Passt die Überlagerten Images an die größe an
        #region Teil 3
        float cellWidth1 = newWidth1 / 7;
        float cellHeight1 = newHeight1 / 7;
        imageObject1.GetComponent<GridLayoutGroup>().cellSize = new Vector2(cellWidth1, cellHeight1);
        StartCoroutine(ToggleGridLayoutGroup(imageObject1));
        #endregion
        // TEIL 4: Überlagerten Images muster geben
        #region Teil 4
        string[] himmelrichtungen1 = new string[] { "E", "N", "NE", "NW", "S", "SE", "SW", "W" };
        for (int i = 0; i < 49; i++)
        {
            int random = UnityEngine.Random.Range(0, himmelrichtungen1.Length);
            imageObject1.transform.GetChild(i).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/GUI/Arrow " + himmelrichtungen1[random]);
        }
        #endregion
        yield return null;
        // TEIL 2: Zeigt Bild in der Szene an und behält die Seitenverhältnisse bei
        #region Teil 2
        Bild[0].transform.parent.GetComponent<RectTransform>().sizeDelta = BildRect;
        Texture2D myTexture = Bild[0].transform.parent.gameObject.GetComponent<Image>().sprite.texture;
        Sprite sprite = Sprite.Create(myTexture, new Rect(0, 0, myTexture.width, myTexture.height), new Vector2(0.5f, 0.5f), 100);
        GameObject imageObject = Bild[0].transform.parent.gameObject;
        imageObject.GetComponent<Image>().sprite = sprite;

        // Skalierung des Bildes, um das Seitenverhältnis beizubehalten und um sicherzustellen, dass das Bild nicht größer als das Image ist
        float imageWidth = imageObject.GetComponent<RectTransform>().rect.width;
        float imageHeight = imageObject.GetComponent<RectTransform>().rect.height;
        float textureWidth = myTexture.width;
        float textureHeight = myTexture.height;
        float widthRatio = imageWidth / textureWidth;
        float heightRatio = imageHeight / textureHeight;
        float ratio = Mathf.Min(widthRatio, heightRatio);
        float newWidth = textureWidth * ratio;
        float newHeight = textureHeight * ratio;

        // Anpassung der Größe des Image-GameObjects und des Sprite-Components
        RectTransform imageRectTransform = imageObject.GetComponent<RectTransform>();
        imageRectTransform.sizeDelta = new Vector2(newWidth, newHeight);
        imageObject.GetComponent<Image>().sprite = sprite;
        #endregion
        // TEIL 3: Passt die Überlagerten Images an die größe an
        #region Teil 3
        float cellWidth = newWidth / 7;
        float cellHeight = newHeight / 7;
        imageObject.GetComponent<GridLayoutGroup>().cellSize = new Vector2(cellWidth, cellHeight);
        #endregion
        // TEIL 4: Überlagerten Images muster geben
        #region Teil 4
        string[] himmelrichtungen = new string[] { "E", "N", "NE", "NW", "S", "SE", "SW", "W" };
        for (int i = 0; i < 49; i++)
        {
            int random = UnityEngine.Random.Range(0, himmelrichtungen.Length);
            imageObject.transform.GetChild(i).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/GUI/Arrow " + himmelrichtungen[random]);
        }
        #endregion

        // Blendet Cover ein
        for (int i = 0; i < 49; i++)
        {
            coverlist.Add(i);
            Bild[i].GetComponent<RectTransform>().sizeDelta = new Vector2(100, 100);
            Bild[i].GetComponent<RectTransform>().eulerAngles = new Vector3(0, 0, 0);
            Bild[i].GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
            Bild[i].GetComponent<Animator>().enabled = false;
            Bild[i].SetActive(true);
        }
        yield return null;
    }
}
