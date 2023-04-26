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

public class AuktionServer : MonoBehaviour
{
    GameObject AustabbenAnzeigen;
    bool initReady = false;
    GameObject[,] SpielerAnzeige;
    bool[] PlayerConnected;

    Image BildAnzeige;
    //Server
    GameObject[,] AuktionsElemente;
    TMP_InputField SummeAllerPreise;
    

    [SerializeField] AudioSource BuzzerSound;
    [SerializeField] AudioSource RichtigeAntwortSound;
    [SerializeField] AudioSource FalscheAntwortSound;

    void OnEnable()
    {
        initReady = false;
        PlayerConnected = new bool[Config.SERVER_MAX_CONNECTIONS];
        InitAnzeigen();
        StartCoroutine(LoadAllAuktionImages());
    }

    void Update()
    {
        #region Server
        if (!Config.SERVER_STARTED)
        {
            SceneManager.LoadSceneAsync("Startup");
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
                        Logging.log(Logging.LogType.Normal, "AuktionServer", "Update", "Spieler hat die Verbindung getrennt. ID: " + Config.PLAYERLIST[i].id);
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
        Logging.log(Logging.LogType.Normal, "AuktionServer", "OnApplicationQuit", "Server wird geschlossen");
        Config.SERVER_TCP.Server.Close();
    }

    /// <summary>
    /// Lädt alle Bilder die für das Game notwendig sind herunter
    /// </summary>
    IEnumerator LoadAllAuktionImages()
    {
        if (Config.AUKTION_SPIEL.getSelected() == null)
            yield break;

        foreach (AuktionElement Elemente in Config.AUKTION_SPIEL.getSelected().getElemente())
        {
            for (int i = 0; i < Elemente.getBilderURL().Length; i++)
            {
                string url = Elemente.getBilderURL()[i];
                UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    Logging.log(Logging.LogType.Warning, "AuktionServer", "LoadAllAuktionImages", "Bild konnte nicht geladen werden: " + Elemente.getName() + "-> " + url + " << " + www.error);
                }
                else
                {
                    Texture2D texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
                    Elemente.getBilder()[i] = sprite;
                }
                yield return null;
            }
        }

        // Anzeigen
        yield return null;
        InitAuktion();
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
            Logging.log(Logging.LogType.Warning, "AuktionServer", "SendMSG", "Nachricht an Client: " + sc.id + " (" + sc.name + ") konnte nicht gesendet werden.", e);
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
        Logging.log(Logging.LogType.Debug, "AuktionServer", "Commands", "Eingehende Nachricht: " + cmd + " -> " + data);
        // Sucht nach Command
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "AuktionServer", "Commands", "Unkown Command: " + cmd + " -> " + data);
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

            case "#JoinAuktion":
                PlayerConnected[player.id - 1] = true;
                SendImageURLs(player);
                UpdateSpielerBroadcast();
                break;
            case "#ImageDownloadError":
                Logging.log(Logging.LogType.Warning, "AuktionServer", "Commands", "ImageDownloadError: (" + player.id + ") " + player.name + "-> " + data);
                break;
            case "#ImageDownloadedSuccessful":
                Logging.log(Logging.LogType.Warning, "AuktionServer", "Commands", "ImageDownloadedSuccessful: (" + player.id + ") " + player.name);
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
        if (!initReady)
            return;
        Broadcast(UpdateSpieler(), Config.PLAYERLIST);
    }
    /// <summary>
    /// Aktualisiert die Spieler Anzeige Informationen & gibt diese als Text zurück
    /// </summary>
    /// <returns></returns>
    private string UpdateSpieler()
    {
        string msg = "";
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            
            Player p = Config.PLAYERLIST[i];
            string buyeditems = "";
            for (int j = 0; j < Config.AUKTION_SPIEL.getSelected().getElemente().Count; j++)
            {
                if (Config.AUKTION_SPIEL.getSelected().getElemente()[j].getWurdeverkauft())
                {
                    if (Config.AUKTION_SPIEL.getSelected().getElemente()[j].getKaueferId() == p.id)
                    {
                        buyeditems += "," + j;
                    }
                }
            }
            if (buyeditems.Length > 1)
                buyeditems = buyeditems.Substring(1);

            msg += "[TRENNER][ID]" + p.id + "[ID][PUNKTE]" + p.points + "[PUNKTE][KONTO]"+ SpielerAnzeige[p.id - 1, 7].GetComponent<TMP_InputField>().text + "[KONTO][GUV]"+ SpielerAnzeige[p.id - 1, 8].GetComponent<TMP_InputField>().text + "[GUV][ITEMS]" + buyeditems + "[ITEMS]";
            if (p.isConnected && PlayerConnected[i])
            {
                SpielerAnzeige[i, 0].SetActive(true);
                SpielerAnzeige[i, 2].GetComponent<Image>().sprite = p.icon;
                SpielerAnzeige[i, 4].GetComponent<TMP_Text>().text = p.name;
                SpielerAnzeige[i, 5].GetComponent<TMP_Text>().text = p.points+"";

                
                for (int j = 0; j < 10; j++)
                {
                    SpielerAnzeige[i, 6].transform.GetChild(j).gameObject.SetActive(false);
                }
                if (buyeditems.Length == 1)
                {
                    SpielerAnzeige[i, 6].transform.GetChild(Int32.Parse(buyeditems)).gameObject.SetActive(true);
                }
                else if (buyeditems.Length > 1)
                {
                    string[] items = buyeditems.Split(',');
                    for (int j = 0; j < items.Length; j++)
                    {
                        SpielerAnzeige[i, 6].transform.GetChild(Int32.Parse(items[j])).gameObject.SetActive(true);
                    }
                }
            }
            else
                SpielerAnzeige[i, 0].SetActive(false);

        }
        if (msg.StartsWith("[TRENNER]"))
            msg = msg.Substring("[TRENNER]".Length);
        return "#UpdateSpieler " +msg;
    }
    /// <summary>
    /// Initialisiert die Anzeigen zu beginn
    /// </summary>
    private void InitAnzeigen()
    {
        GameObject.Find("Auktion/Server/KontoGuthaben").GetComponent<TMP_InputField>().text = "0";
        // Austabben wird gezeigt
        GameObject.Find("Einstellungen/AusgetabtSpielernZeigenToggle").GetComponent<Toggle>().isOn = false;
        AustabbenAnzeigen = GameObject.Find("Einstellungen/AusgetabtWirdSpielernGezeigen");
        AustabbenAnzeigen.SetActive(false);
        // Spieler Anzeige
        SpielerAnzeige = new GameObject[Config.SERVER_MAX_CONNECTIONS, 9]; // Anzahl benötigter Elemente
        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
        {
            PlayerConnected[i] = false;
            SpielerAnzeige[i, 0] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")"); // Spieler Anzeige
            SpielerAnzeige[i, 1] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/BuzzerPressed"); // BuzzerPressed Umrandung
            SpielerAnzeige[i, 2] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Icon"); // Spieler Icon
            SpielerAnzeige[i, 3] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Ausgetabt"); // Ausgetabt Einblednung
            SpielerAnzeige[i, 4] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Infobar/Name"); // Spieler Name
            SpielerAnzeige[i, 5] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Infobar/Punkte"); // Spieler Punkte

            SpielerAnzeige[i, 6] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Elemente");
            SpielerAnzeige[i, 7] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Konto");
            SpielerAnzeige[i, 7].GetComponent<TMP_InputField>().text = "0";
            SpielerAnzeige[i, 8] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/GUV");
            SpielerAnzeige[i, 8].GetComponent<TMP_InputField>().text = "0";


            SpielerAnzeige[i, 0].SetActive(false); // Spieler Anzeige
            SpielerAnzeige[i, 1].SetActive(false); // BuzzerPressed Umrandung
            SpielerAnzeige[i, 3].SetActive(false); // Ausgetabt Einblendung
        }

        //Auktion
        BildAnzeige = GameObject.Find("Auktion/Anzeige/Bild").GetComponent<Image>();
        BildAnzeige.gameObject.SetActive(false);
        //Server
        SummeAllerPreise = GameObject.Find("Auktion/Server/SummePreise").GetComponent<TMP_InputField>();
        AuktionsElemente = new GameObject[10, 11];
        for (int i = 0; i < 10; i++)
        {
            // Element An Sich falls weniger enthalten sind
            AuktionsElemente[i, 0] = GameObject.Find("Auktion/Server/Vorschau/Element (" + (i+1) + ")");
            // Name
            AuktionsElemente[i, 1] = GameObject.Find("Auktion/Server/Vorschau/Element (" + (i + 1) + ")/Name");
            // Preis
            AuktionsElemente[i, 2] = GameObject.Find("Auktion/Server/Vorschau/Element (" + (i + 1) + ")/Preis");
            // URL
            AuktionsElemente[i, 3] = GameObject.Find("Auktion/Server/Vorschau/Element (" + (i + 1) + ")/URL");
            // Verkaufspreis
            AuktionsElemente[i, 4] = GameObject.Find("Auktion/Server/Vorschau/Element (" + (i + 1) + ")/Verkaufspreis");
            // Käufer
            AuktionsElemente[i, 5] = GameObject.Find("Auktion/Server/Vorschau/Element (" + (i + 1) + ")/Käufer");
            // Bild (1)
            AuktionsElemente[i, 6] = GameObject.Find("Auktion/Server/Vorschau/Element (" + (i + 1) + ")/Bild (1)");
            // Bild (2)
            AuktionsElemente[i, 7] = GameObject.Find("Auktion/Server/Vorschau/Element (" + (i + 1) + ")/Bild (2)");
            // Bild (3)
            AuktionsElemente[i, 8] = GameObject.Find("Auktion/Server/Vorschau/Element (" + (i + 1) + ")/Bild (3)");
            // Bild (4)
            AuktionsElemente[i, 9] = GameObject.Find("Auktion/Server/Vorschau/Element (" + (i + 1) + ")/Bild (4)");
            // Bild (5)
            AuktionsElemente[i, 10] = GameObject.Find("Auktion/Server/Vorschau/Element (" + (i + 1) + ")/Bild (5)");

            AuktionsElemente[i, 0].SetActive(false);
        }
    }
    /// <summary>
    /// Initialisiert die Anzeigen der Auktion
    /// </summary>
    private void InitAuktion()
    {
        //Auktion
        BildAnzeige.gameObject.SetActive(false);
        //Server
        for (int i = 0; i < Config.AUKTION_SPIEL.getSelected().getElemente().Count; i++)
        {
            // Element An Sich falls weniger enthalten sind
            AuktionsElemente[i, 0].SetActive(true);
            // Name
            AuktionsElemente[i, 1].GetComponent<TMP_Text>().text = Config.AUKTION_SPIEL.getSelected().getElemente()[i].getName();
            // Preis
            AuktionsElemente[i, 2].GetComponent<TMP_InputField>().text = Config.AUKTION_SPIEL.getSelected().getElemente()[i].getPreis() + "";
            // URL
            AuktionsElemente[i, 3].GetComponent<TMP_InputField>().text = Config.AUKTION_SPIEL.getSelected().getElemente()[i].getURL();
            // Verkaufspreis
            AuktionsElemente[i, 4].GetComponent<TMP_InputField>().text = "";
            Config.AUKTION_SPIEL.getSelected().getElemente()[i].setVerkaufspreis(0);
            // Käufer
            List<string> spieler = new List<string>();
            spieler.Add("zurücknehmen");
            foreach (Player p in Config.PLAYERLIST)
            {
                if (p.isConnected)
                    spieler.Add(p.id + " - " + p.name);
            }
            AuktionsElemente[i, 5].GetComponent<TMP_Dropdown>().ClearOptions();
            AuktionsElemente[i, 5].GetComponent<TMP_Dropdown>().AddOptions(spieler);
            // Bild (1)
            AuktionsElemente[i, 6].GetComponent<Image>().sprite = Config.AUKTION_SPIEL.getSelected().getElemente()[i].getBilder()[0];
            // Bild (2)
            AuktionsElemente[i, 7].GetComponent<Image>().sprite = Config.AUKTION_SPIEL.getSelected().getElemente()[i].getBilder()[1];
            // Bild (3)
            AuktionsElemente[i, 8].GetComponent<Image>().sprite = Config.AUKTION_SPIEL.getSelected().getElemente()[i].getBilder()[2];
            // Bild (4)
            AuktionsElemente[i, 9].GetComponent<Image>().sprite = Config.AUKTION_SPIEL.getSelected().getElemente()[i].getBilder()[3];
            // Bild (5)
            AuktionsElemente[i, 10].GetComponent<Image>().sprite = Config.AUKTION_SPIEL.getSelected().getElemente()[i].getBilder()[4];
        }
        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                SpielerAnzeige[i, 6].transform.GetChild(j).gameObject.SetActive(false);
            }
            
            for (int j = 0; j < Config.AUKTION_SPIEL.getSelected().getElemente().Count; j++)
            {
                SpielerAnzeige[i, 6].transform.GetChild(j).GetComponent<Image>().sprite = Config.AUKTION_SPIEL.getSelected().getElemente()[j].getBilder()[0];
            }
        }

        initReady = true;
        UpdateKonten();
    }
    /// <summary>
    /// Aktualisiert die Konten der Spieler
    /// </summary>
    private void UpdateKonten()
    {
        float preissumme = 0;
        foreach (AuktionElement element in Config.AUKTION_SPIEL.getSelected().getElemente())
        {
            preissumme += element.getPreis();
        }
        SummeAllerPreise.text = preissumme + "";

        foreach (Player p in Config.PLAYERLIST)
        {
            if (!p.isConnected)
                continue;
            float konto = 0;
            try
            {
                konto = float.Parse(GameObject.Find("Auktion/Server/KontoGuthaben").GetComponent<TMP_InputField>().text);
            }
            catch (Exception e)
            {
                Logging.log(Logging.LogType.Warning, "AuktionServer", "UpdateKonten", "Guthaben konnte nicht gelesen werden.", e);
                return;
            }
            float guv = 0;
            foreach (AuktionElement element in Config.AUKTION_SPIEL.getSelected().getElemente())
            {
                if (!element.getWurdeverkauft())
                    continue;
                if (element.getKaueferId() == p.id)
                {
                    guv += element.getPreis() - element.getVerkaufspreis();
                    konto -= element.getVerkaufspreis();
                }
            }
            // Konto
            SpielerAnzeige[Player.getPosInLists(p.id), 7].GetComponent<TMP_InputField>().text = konto + " €";
            // GUV
            SpielerAnzeige[Player.getPosInLists(p.id), 8].GetComponent<TMP_InputField>().text = guv+" €";

        }
        UpdateSpielerBroadcast();
    }
    /// <summary>
    /// Sendet die URLs für die notwendigen Bilder
    /// </summary>
    /// <param name="p"></param>
    private void SendImageURLs(Player p)
    {
        string msg = "[ANZ]" + Config.AUKTION_SPIEL.getSelected().getElemente().Count + "[ANZ]";
        for (int j = 0; j < Config.AUKTION_SPIEL.getSelected().getElemente().Count; j++)
        {
            string temp = "";
            AuktionElement Elemente = Config.AUKTION_SPIEL.getSelected().getElemente()[j];
            for (int i = 0; i < Elemente.getBilderURL().Length; i++)
            {
                temp += "<#>" + Elemente.getBilderURL()[i];
            }
            if (temp.Length > 3)
                temp = temp.Substring(3);
            msg += "[" + j + "]" + temp + "[" + j + "]";
        }
        Logging.log(Logging.LogType.Warning, "AuktionServer", "SendImageURLs", "#AuktionDownloadImages " + msg + p.name);
        SendMSG("#AuktionDownloadImages "+msg, p);
    }
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
    #region Punkte
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
        Broadcast("#SpielerIstDran " + pId);
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

        Broadcast("#SpielerIstNichtDran " + pId);
    }
    #endregion
    #region Auktion Anzeige
    /// <summary>
    /// Blendet vom Server eingegebenes Bild ein
    /// </summary>
    /// <param name="input"></param>
    public void ShowCustomImage(TMP_InputField input)
    {
        if (input.text.Length == 0)
            return;
        Broadcast("#ShowCustomImage "+input.text);
        StartCoroutine(LoadImageIntoScene(input.text));
    }
    /// <summary>
    /// Blendet die Bildanzeige aus
    /// </summary>
    public void HideImage()
    {
        Broadcast("#HideImage");
        BildAnzeige.gameObject.SetActive(false);
    }
    /// <summary>
    /// Lädt ein Bild per URL herunter und zeigt dieses in der Scene direkt an
    /// </summary>
    /// <param name="url"></param>
    IEnumerator LoadImageIntoScene(string url)
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Logging.log(Logging.LogType.Warning, "AuktionServer", "LoadImageIntoScene", "Bild konnte nicht geladen werden: " + url + " << " + www.error);
        }
        else
        {
            try
            {
                Texture2D texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
                BildAnzeige.sprite = sprite;
                BildAnzeige.gameObject.SetActive(true);
            }
            catch (Exception e)
            {
                Logging.log(Logging.LogType.Warning, "AuktionServer", "LoadImageIntoScene", "Custombild konnte nicht geladen werden: " + url + " << ", e);
            }
        }
        yield return null;
    }
    /// <summary>
    /// Aktualisiert die Spielerkonten (legt den Wert fest)
    /// </summary>
    /// <param name="input"></param>
    public void SpielerKontoFestlegen(TMP_InputField input)
    {
        if (input.text.Length == 0)
            return;
        UpdateKonten();
        Broadcast("#SpielerKonto "+ input.text);
    }
    /// <summary>
    /// Ändert den Preis eines Produktes
    /// </summary>
    /// <param name="input"></param>
    public void ChangePreis(TMP_InputField input)
    {
        if (input.text.Length == 0)
            return;
        int item = Int32.Parse(input.transform.parent.name.Replace("Element (", "").Replace(")", "")) - 1;
        Config.AUKTION_SPIEL.getSelected().getElemente()[item].setPreis(float.Parse(input.text));

        UpdateKonten();
    }
    /// <summary>
    /// Legt/Ändert den Verkaufspreis eines Produktes
    /// </summary>
    /// <param name="input"></param>
    public void SetVerkaufspreis(TMP_InputField input)
    {
        if (input.text.Length == 0)
            return;
        int item = Int32.Parse(input.transform.parent.name.Replace("Element (", "").Replace(")", "")) - 1;
        Config.AUKTION_SPIEL.getSelected().getElemente()[item].setVerkaufspreis(float.Parse(input.text));

        UpdateKonten();
    }
    /// <summary>
    /// Zeigt ein Bild eines Produktes an
    /// </summary>
    /// <param name="go"></param>
    public void ShowItemImage(GameObject go)
    {
        int item = Int32.Parse(go.transform.parent.name.Replace("Element (", "").Replace(")", "")) -1;
        int bild = Int32.Parse(go.name.Replace("Bild (", "").Replace(")", "")) - 1;
        BildAnzeige.sprite = Config.AUKTION_SPIEL.getSelected().getElemente()[item].getBilder()[bild];
        BildAnzeige.gameObject.SetActive(true);
        Broadcast("#ShowItemImage " + item + "|" + bild);
    }
    /// <summary>
    /// Verkauft ein Produkt an einen Spieler
    /// </summary>
    /// <param name="drop"></param>
    public void SellItemToPlayer(TMP_Dropdown drop)
    {
        int item = Int32.Parse(drop.transform.parent.name.Replace("Element (", "").Replace(")", "")) - 1;
        //Broadcast("#SellItemToPlayer " + drop.value + "|" + item);
        // Verkauf wird zurückgenommen
        if (drop.value == 0)
        {
            Config.AUKTION_SPIEL.getSelected().getElemente()[item].setWurdeverkauft(false);
            Config.AUKTION_SPIEL.getSelected().getElemente()[item].setKaueferId(0);
        }
        // Item wird verkauft
        else
        {
            int id = drop.value;
            Config.AUKTION_SPIEL.getSelected().getElemente()[item].setWurdeverkauft(true);
            Config.AUKTION_SPIEL.getSelected().getElemente()[item].setKaueferId(id);
        }

        UpdateKonten();
    }
    /// <summary>
    /// Zeigt allen Spielern alle Konten an
    /// </summary>
    /// <param name="toggle"></param>
    public void ShowAllKonten(Toggle toggle)
    {
        Broadcast("#ShowAllKonten " + toggle.isOn);
    }
    /// <summary>
    /// Zeigt allen Spielern alle GUVs an
    /// </summary>
    /// <param name="toggle"></param>
    public void ShowAllGUV(Toggle toggle)
    {
        Broadcast("#ShowAllGUV " + toggle.isOn);
    }
    #endregion
}
