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

public class JeopardyServer : MonoBehaviour
{
    // JeopardyAuswahl
    GameObject JeopardyAuswahlAnzeige;
    GameObject AuswahlGrid;
    // JeopardyElementAnzeige
    GameObject JeopardyElementAnzeige;
    GameObject AnzeigeFrage;
    Vector2 BildRect;
    GameObject Bild;
    // Server
    Image SchaetzSiegerImage;
    TMP_InputField SchaetzZiel;
    GameObject SchaetzSpielerAnzeige;
    TMP_InputField Frage;
    TMP_InputField Antwort;
    TMP_InputField Punkte;
    TMP_Text Thema;
    TMP_InputField BildUrl;
    GameObject SpielerHabenGeladen;


    bool buzzerIsOn = false;
    GameObject BuzzerAnzeige;
    GameObject AustabbenAnzeigen;
    GameObject EingabeAnzeige;

    GameObject[,] SpielerAnzeige;
    bool[] PlayerConnected;
    int PunkteProRichtige = 0;
    int PunkteProFalsche = 0;

    [SerializeField] AudioSource BuzzerSound;
    [SerializeField] AudioSource RichtigeAntwortSound;
    [SerializeField] AudioSource FalscheAntwortSound;
    [SerializeField] AudioSource DisconnectSound;

    void OnEnable()
    {
        PlayerConnected = new bool[Config.SERVER_MAX_CONNECTIONS];
        InitAnzeigen();
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
        }
        #endregion
    }

    private void OnApplicationQuit()
    {
        ServerUtils.BroadcastImmediate(Config.GLOBAL_TITLE + "#ServerClosed");
        Logging.log(Logging.LogType.Normal, "Server", "OnApplicationQuit", "Server wird geschlossen");
        Config.SERVER_TCP.Server.Close();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    #region Server Stuff
    #region Kommunikation
    /// <summary>
    /// Einkommende Nachrichten die von Spielern an den Server gesendet werden.
    /// </summary>
    /// <param name="spieler">Spieler</param>
    /// <param name="data">Nachricht</param>
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
        Logging.log(Logging.LogType.Debug, "JeopardyServer", "Commands", "Eingehende Nachricht: " + player.name + " " + player.id + " -> " + cmd + "   ---   " + data);
        // Sucht nach Command
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "JeopardyServer", "Commands", "Unkown Command: " + cmd + " -> " + data);
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

            case "#JoinJeopardy":
                PlayerConnected[player.id - 1] = true;
                //UpdateSpielerBroadcast();
                string msg = "";
                foreach (var themen in Config.JEOPARDY_SPIEL.getSelected().getThemen())
                {
                    msg += "|" + themen.thema + "~";
                    foreach (var item in themen.items)
                    {
                        msg += "~" + item.points;
                    }
                }
                if (msg.Length > 0)
                    msg = msg.Substring(1);
                ServerUtils.SendMSG("#AuswahlUebersicht " + msg, player, false);
                break;
            case "#GetPlayerUpdate":
                ServerUtils.SendMSG(UpdateSpieler(), player, false);
                break;
            case "#SpielerBuzzered":
                SpielerBuzzered(player);
                break;
            case "#ClientHatBildGeladen":
                ClientBildGeladen(player);
                break;
            case "#AntwortEingabeUpdate":
                SpielerAnzeige[Player.getPosInLists(player.id), 6].GetComponentInChildren<TMP_InputField>().text = data;
                ParseSchaetzfrage(player, data);
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
    /// <returns>#UpdateSpieler ...</returns>
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
    /// Spielt den Disconnect Sound ab
    /// </summary>
    private void PlayDisconnectSound()
    {
        DisconnectSound.Play();
    }
    /// <summary>
    /// Initialisiert die Anzeigen zu beginn
    /// </summary>
    private void InitAnzeigen()
    {
        Logging.log(Logging.LogType.Debug, "JeopardyServer", "InitAnzeigen", "Initialisiert die Anzeigen");
        // Buzzer Deaktivieren
        GameObject.Find("ServerSide/BuzzerAktivierenToggle").GetComponent<Toggle>().isOn = false;
        BuzzerAnzeige = GameObject.Find("ServerSide/BuzzerIstAktiviert");
        BuzzerAnzeige.SetActive(false);
        buzzerIsOn = false;
        // Austabben wird gezeigt Deaktivieren
        GameObject.Find("ServerSide/AusgetabtSpielernZeigenToggle").GetComponent<Toggle>().isOn = false;
        AustabbenAnzeigen = GameObject.Find("ServerSide/AusgetabtWirdSpielernGezeigen");
        AustabbenAnzeigen.SetActive(false);
        // Spieler Antworteingabe Deaktivieren
        GameObject.Find("ServerSide/SpielerEingabeAktivierenToggle").GetComponent<Toggle>().isOn = false;
        EingabeAnzeige = GameObject.Find("ServerSide/SpielerEingabeIstAktiviert");
        EingabeAnzeige.SetActive(false);
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
        }
        // AuswahlGrid Laden
        AuswahlGrid = GameObject.Find("JeopardyAuswahl/Grid");
        for (int i = 0; i < AuswahlGrid.transform.childCount; i++)
        {
            for (int j = 0; j < AuswahlGrid.transform.GetChild(i).childCount; j++)
            {
                if (!AuswahlGrid.transform.GetChild(i).GetChild(j).name.Equals("Spacer"))
                    AuswahlGrid.transform.GetChild(i).GetChild(j).gameObject.SetActive(false);
            }
            AuswahlGrid.transform.GetChild(i).gameObject.SetActive(false);
        }
        for (int i = 0; i < Config.JEOPARDY_SPIEL.getSelected().getThemen().Count; i++)
        {
            AuswahlGrid.transform.GetChild(i).gameObject.SetActive(true);
            AuswahlGrid.transform.GetChild(i).GetChild(0).gameObject.SetActive(true);
            AuswahlGrid.transform.GetChild(i).GetChild(0).GetComponent<Button>().enabled = false;
            AuswahlGrid.transform.GetChild(i).GetChild(0).GetComponentInChildren<TMP_Text>().text = Config.JEOPARDY_SPIEL.getSelected().getThemen()[i].thema;
            for (int j = 2; j < Config.JEOPARDY_SPIEL.getSelected().getThemen()[i].items.Count + 2; j++)
            {
                AuswahlGrid.transform.GetChild(i).GetChild(j).gameObject.SetActive(true);
                AuswahlGrid.transform.GetChild(i).GetChild(j).GetComponentInChildren<TMP_Text>().text = Config.JEOPARDY_SPIEL.getSelected().getThemen()[i].items[j-2].points + "";
            }
        }
        JeopardyAuswahlAnzeige = GameObject.Find("JeopardyAuswahl");
        JeopardyAuswahlAnzeige.SetActive(true);

        // AnzeigeFrage
        AnzeigeFrage = GameObject.Find("JeopardyElementAnzeige/Grid/Frage");
        // AnzeigeBild   
        Bild = GameObject.Find("JeopardyElementAnzeige/Grid/BildImage");
        Bild.SetActive(false);
        BildRect = new Vector2(Bild.GetComponent<RectTransform>().rect.width, Bild.GetComponent<RectTransform>().rect.height);
        BildUrl = GameObject.Find("JeopardyElementAnzeige/ServerSide/BildUrl").GetComponent<TMP_InputField>();

        // Schätzfrage
        SchaetzSiegerImage = GameObject.Find("JeopardyElementAnzeige/ServerSide/Sieger/Image").GetComponent<Image>();
        SchaetzSiegerImage.sprite = Resources.Load<Sprite>("Images/ProfileIcons/empty");
        SchaetzSpielerAnzeige = GameObject.Find("JeopardyElementAnzeige/ServerSide/Spieler"); 
        SchaetzZiel = GameObject.Find("ServerSide/Ziel").GetComponent<TMP_InputField>();
        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
        {
            if (Config.PLAYERLIST[i].name.Length > 2)
            {
                SchaetzSpielerAnzeige.transform.GetChild(i).GetChild(1).GetComponent<Image>().sprite = Config.PLAYERLIST[i].icon2.icon;
                SchaetzSpielerAnzeige.transform.GetChild(i).GetChild(0).GetComponent<TMP_InputField>().text = "";
                SchaetzSpielerAnzeige.transform.GetChild(i).gameObject.SetActive(true);
            }
            else
                SchaetzSpielerAnzeige.transform.GetChild(i).gameObject.SetActive(false);
        }

        // ElementAnzeigen
        Frage = GameObject.Find("JeopardyElementAnzeige/ServerSide/Frage").GetComponent<TMP_InputField>();
        Frage.text = "";
        Antwort = GameObject.Find("JeopardyElementAnzeige/ServerSide/Antwort").GetComponent<TMP_InputField>();
        Punkte = GameObject.Find("JeopardyElementAnzeige/ServerSide/Punkte").GetComponent<TMP_InputField>();
        Thema = GameObject.Find("JeopardyElementAnzeige/ServerSide/Thema").GetComponent<TMP_Text>();

        SpielerHabenGeladen = GameObject.Find("JeopardyElementAnzeige/ServerSide/SpielerHabenGeladen");
        for (int i = -1; i < Config.SERVER_MAX_CONNECTIONS; i++)
        {
            if (i == -1)
            {
                SpielerHabenGeladen.transform.GetChild(i+1).GetComponent<Image>().sprite = Config.SERVER_PLAYER.icon2.icon;
                SpielerHabenGeladen.transform.GetChild(i+1).gameObject.SetActive(false);
                continue;
            }
            SpielerHabenGeladen.transform.GetChild(i+1).GetComponent<Image>().sprite = Config.PLAYERLIST[i].icon2.icon;
            SpielerHabenGeladen.transform.GetChild(i+1).gameObject.SetActive(false);
        }
        JeopardyElementAnzeige = GameObject.Find("JeopardyElementAnzeige");
        JeopardyElementAnzeige.SetActive(false);

        
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
            Logging.log(Logging.LogType.Normal, "JeopardyServer", "SpielerBuzzered", p.name + " - " + DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second + ":" + DateTime.Now.Millisecond);
            return;
        }
        Logging.log(Logging.LogType.Warning, "JeopardyServer", "SpielerBuzzered", "B: " + p.name + " - " + DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second + ":" + DateTime.Now.Millisecond);
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
        Logging.log(Logging.LogType.Warning, "JeopardyServer", "SpielerBuzzerFreigeben", "Buzzer wird freigegeben");
        ServerUtils.BroadcastImmediate("#BuzzerFreigeben");
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
    /// Vergibt an den Spieler Punkte für eine richtige Antwort
    /// </summary>
    /// <param name="player">Spieler</param>
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
    /// <param name="player">Spieler</param>
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
    #region Jeopardy Anzeige
    JeopardyItem selectedItem;
    public void SelectJeopardyElement(GameObject go)
    {
        if (!Config.SERVER_STARTED)
            return;
        int thema = Int32.Parse(go.transform.parent.name);
        int element = Int32.Parse(go.name);

        go.GetComponent<Button>().interactable = false;
        selectedItem = Config.JEOPARDY_SPIEL.getSelected().getThemen()[thema].items[element];
        ServerUtils.BroadcastImmediate("#SelectElement " + thema + "|" + element + "|" + selectedItem.points + "|" + selectedItem.thema.thema);

        Frage.text = selectedItem.frage;
        Antwort.text = selectedItem.antwort;
        Punkte.text = selectedItem.points + "";
        Thema.text = selectedItem.thema.thema;
        BildUrl.text = selectedItem.imageurl;
        Bild.SetActive(false);

        AnzeigeFrage.GetComponentInChildren<TMP_Text>().text = "";

        JeopardyAuswahlAnzeige.SetActive(false);
        JeopardyElementAnzeige.SetActive(true);
    }
    public void ElementFrageEinblenden(TMP_InputField input)
    {
        ServerUtils.BroadcastImmediate("#ElementFrage " + input.text);
        AnzeigeFrage.GetComponentInChildren<TMP_Text>().text = input.text;
    }
    public void ZurueckZurAuswahl()
    {
        ServerUtils.BroadcastImmediate("#ZurueckZurAuswahl");
        JeopardyAuswahlAnzeige.SetActive(true);
        JeopardyElementAnzeige.SetActive(false);
    }
    public void ElementBildLaden(TMP_InputField input)
    {
        ServerUtils.BroadcastImmediate("#ElementBildLaden " + input.text);
        if (input.text.Length == 0)
        {
            Bild.SetActive(false);
            return;
        }
        for (int i = -1; i < Config.SERVER_MAX_CONNECTIONS; i++)
        {
            if (i == -1)
            {
                SpielerHabenGeladen.transform.GetChild(i + 1).gameObject.SetActive(false);
                continue;
            }
            SpielerHabenGeladen.transform.GetChild(i + 1).gameObject.SetActive(false);
        }
        Bild.SetActive(false);
        StartCoroutine(LoadImageFromWeb(input.text));
    }
    private void ServerBildGeladen(Sprite sprite)
    {
        LoadImageIntoPreview(sprite);
        SpielerHabenGeladen.transform.GetChild(0).GetComponent<Image>().sprite = Config.SERVER_PLAYER.icon2.icon;
        SpielerHabenGeladen.transform.GetChild(0).gameObject.SetActive(true);
    }
    private void LoadImageIntoPreview(Sprite s)
    {
        Bild.GetComponent<RectTransform>().sizeDelta = BildRect;
        GameObject imageObject = Bild;
        Texture2D myTexture = s.texture;
        Sprite sprite = Sprite.Create(myTexture, new Rect(0, 0, myTexture.width, myTexture.height), new Vector2(0.5f, 0.5f), 100);

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
    }
    private void ClientBildGeladen(Player p)
    {
        SpielerHabenGeladen.transform.GetChild(p.id).GetComponent<Image>().sprite = p.icon2.icon;
        SpielerHabenGeladen.transform.GetChild(p.id).gameObject.SetActive(true);
    }
    public void ElementBildEinblenden()
    {
        ServerUtils.BroadcastImmediate("#ElementBildEinblenden");
        Bild.SetActive(true);
    }
    IEnumerator LoadImageFromWeb(string imageUrl)
    {
        Logging.log(Logging.LogType.Normal, "JeopardyServer", "LoadImageFromWeb", "Lädt Bild herunter: " + imageUrl);
        // TEIL 1: Download des Bildes
        UnityWebRequest www;
        try
        {
            www = UnityWebRequestTexture.GetTexture(imageUrl);
        }
        catch
        {
            www = null;
        }
        if (www == null)
            yield break;

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Logging.log(Logging.LogType.Warning, "JeopardyServer", "LoadImageFromWeb", "Bild konnte nicht herunter geladen werden: " + www.error);
        }
        else
        {
            Texture2D myTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;
            Sprite sprite = Sprite.Create(myTexture, new Rect(0, 0, myTexture.width, myTexture.height), new Vector2(0.5f, 0.5f), 100);
            ServerBildGeladen(sprite);
        }
        yield return null;
    }
    /// <summary>
    /// Aktiviert/Deaktiviert das Eingabefeld für alle Spieler
    /// </summary>
    /// <param name="toggle">Toggle</param>
    public void EingabeFeldAktivierenToggle(Toggle toggle)
    {
        EingabeAnzeige.SetActive(toggle.isOn);
        ServerUtils.BroadcastImmediate("#EingabefeldToggle " + toggle.isOn);
    }
    char[] moeglicheEingaben = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ',', '.' };
    private void ParseSchaetzfrage(Player p, string data)
    {
        int zahlstart = -1;
        int zahlende = -1;
        for (int i = 0; i < data.Length; i++)
        {
            // Buchstabe an Index i ist nicht in Liste enthalten
            if (Array.IndexOf(moeglicheEingaben, data[i]) > -1)
            {
                if (zahlstart == -1)
                    zahlstart = i;

                zahlende = i;
            }
            // Wenn Zahl Vorbei ist, abbrechen
            if (zahlende != i && zahlende > -1)
                break;
            try
            {
                string antwort = data.Substring(zahlstart, zahlende - zahlstart + 1).Replace(".", "");
                // maximiert komma trennung
                if (antwort.Contains(","))
                {
                    if (antwort.Split(',').Length > 2)
                        antwort = antwort.Split(',')[0] + "," + antwort.Split(',')[1];
                }
                SchaetzSpielerAnzeige.transform.GetChild(Player.getPosInLists(p.id)).GetChild(0).GetComponent<TMP_InputField>().text = antwort + "";
            }
            catch { }
        }
        if (data.Length == 0)
            SchaetzSpielerAnzeige.transform.GetChild(Player.getPosInLists(p.id)).GetChild(0).GetComponent<TMP_InputField>().text = "";

        UpdateSieger();
    }
    public void UpdateSieger()
    {
        // Sieger finden
        if (SchaetzZiel.text.Length == 0)
        {
            SchaetzSiegerImage.sprite = Resources.Load<Sprite>("Images/ProfileIcons/empty");
            return;
        }

        double siegzahl = double.Parse(SchaetzZiel.text);
        double closest = double.MaxValue;
        for (int i = 0; i < SchaetzSpielerAnzeige.transform.childCount; i++)
        {
            if (!SchaetzSpielerAnzeige.transform.GetChild(i).gameObject.activeInHierarchy)
                continue;
            double temp;
            if (SchaetzSpielerAnzeige.transform.GetChild(i).GetChild(0).GetComponent<TMP_InputField>().text.Length == 0)
                temp = 0;
            else
                temp = double.Parse(SchaetzSpielerAnzeige.transform.GetChild(i).GetChild(0).GetComponent<TMP_InputField>().text);
            if (Math.Abs(siegzahl - temp) < Math.Abs(siegzahl - closest))
                closest = Math.Abs(siegzahl - temp);
        }
        Logging.log(Logging.LogType.Warning, "JeopardyServer", "UpdateSieger", "Bestimme neue Sieger");
        for (int i = 0; i < SchaetzSpielerAnzeige.transform.childCount; i++)
        {
            if (!SchaetzSpielerAnzeige.transform.GetChild(i).gameObject.activeInHierarchy)
                continue;
            if (SchaetzSpielerAnzeige.transform.GetChild(i).GetChild(0).GetComponent<TMP_InputField>().text.Length == 0)
                continue;
            if (Math.Abs(siegzahl - double.Parse(SchaetzSpielerAnzeige.transform.GetChild(i).GetChild(0).GetComponent<TMP_InputField>().text)) == closest)
            {
                SchaetzSiegerImage.sprite = SchaetzSpielerAnzeige.transform.GetChild(i).GetChild(1).GetComponent<Image>().sprite;
                Logging.log(Logging.LogType.Normal, "JeopardyServer", "UpdateSieger", "Sieger: Spieler" + i + "Tipp: " + SchaetzSpielerAnzeige.transform.GetChild(i).GetChild(0).GetComponent<TMP_InputField>().text + " Differenz: " + Math.Abs(siegzahl - double.Parse(SchaetzSpielerAnzeige.transform.GetChild(i).GetChild(0).GetComponent<TMP_InputField>().text)));
            }
        }
    }
    #endregion
}
