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
        InitAnzeigen(); // Crasht wenn spieler mit in der Lobby sind
        //InitAuktion();
        StartCoroutine(LoadAllAuktionImages());
    }

    IEnumerator LoadAllAuktionImages()
    {
        if (Config.AUKTION_SPIEL.getSelected() == null)
            yield break;
        //yield return new WaitForSeconds(2);

        foreach (AuktionElement Elemente in Config.AUKTION_SPIEL.getSelected().getElemente())
        {
            for (int i = 0; i < Elemente.getBilderURL().Length; i++)
            {
                string url = Elemente.getBilderURL()[i];
                UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError("Auktion: Bild konnte nicht geladen werden: " + Elemente.getName() + " -> " + url + " << " + www.error);
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
    private void SendMessage(string data, Player sc)
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
                SendMessage(data, sc);
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
                SendMessage(data, sc);
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

            case "#JoinAuktion":
                PlayerConnected[player.id - 1] = true;
                SendImageURLs(player);
                UpdateSpielerBroadcast();
                break;
            case "#ImageDownloadError":
                Debug.LogError("ImageDownloadError: (" + player.id + ") " + player.name + " -> " + data);
                break;
            case "#ImageDownloadedSuccessful":
                Debug.LogError("ImageDownloadedSuccessful: (" + player.id + ") " + player.name);
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
     *  Spiel Verlassen & Zurück in die Lobby laden
     */
    public void SpielVerlassenButton()
    {
        SceneManager.LoadScene("Startup");
        Broadcast("#ZurueckInsHauptmenue");
    }
    /**
     * Sendet aktualisierte Spielerinfos an alle Spieler
     */
    private void UpdateSpielerBroadcast()
    {
        if (!initReady)
            return;
        Broadcast(UpdateSpieler(), Config.PLAYERLIST);
    }
    /**
     * Aktualisiert die Spieler Anzeige Informationen & gibt diese als Text zurück
     */
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
    /**
     * Initialisiert die Anzeigen zu beginn
     */
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

        TMP_Dropdown drop = GameObject.Find("Einstellungen/ChangeAuktion").GetComponent<TMP_Dropdown>();
        drop.ClearOptions();
        List<string> gamelist = new List<string>();
        foreach (Auktion liste in Config.AUKTION_SPIEL.getAuktionen())
        {
            gamelist.Add(liste.getTitel());
        }
        drop.AddOptions(gamelist);
        drop.value = Config.AUKTION_SPIEL.getIndex(Config.AUKTION_SPIEL.getSelected());
    }
    /**
    * Initialisiert die Anzeigen der Auktion
    */
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
    /**
     * Wechselt das Mosaik Game
     */
    public void ChangeAuktion(TMP_Dropdown drop)
    {
        // TODO: not working
        /*Broadcast("#HideImage");
        Config.AUKTION_SPIEL.setSelected(Config.AUKTION_SPIEL.getAuktion(drop.value));

        for (int i = 0; i < AuktionsElemente.GetLength(0); i++)
        {
            AuktionsElemente[i, 0].SetActive(false);
        }

        InitAuktion();
        StartCoroutine(LoadAllAuktionImages());

        foreach (Player p in Config.PLAYERLIST)
        {
            if (p.isConnected)
                SendImageURLs(p);
        }*/
    }

   

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
        Debug.LogError("#AuktionDownloadImages "+ p.name);
        SendMessage("#AuktionDownloadImages "+msg, p);
    }
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
    #region Punkte
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
        SpielerAnzeige[(pId - 1), 1].SetActive(true);
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

        Broadcast("#SpielerIstNichtDran " + pId);
    }
    #endregion

    #region Auktion Anzeige
    public void ShowCustomImage(TMP_InputField input)
    {
        if (input.text.Length == 0)
            return;
        Broadcast("#ShowCustomImage "+input.text);
        StartCoroutine(LoadImageIntoScene(input.text));
    }
    public void HideImage()
    {
        Broadcast("#HideImage");
        BildAnzeige.gameObject.SetActive(false);
    }
    IEnumerator LoadImageIntoScene(string url)
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Auktion: Bild konnte nicht geladen werden: " + url + " << " + www.error);
        }
        else
        {
            Texture2D texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
            BildAnzeige.sprite = sprite;
            BildAnzeige.gameObject.SetActive(true);
        }
        yield return null;
    }

    public void SpielerKontoFestlegen(TMP_InputField input)
    {
        if (input.text.Length == 0)
            return;
        UpdateKonten();
        Broadcast("#SpielerKonto "+ input.text);
    }
    public void ChangePreis(TMP_InputField input)
    {
        if (input.text.Length == 0)
            return;
        int item = Int32.Parse(input.transform.parent.name.Replace("Element (", "").Replace(")", "")) - 1;
        Config.AUKTION_SPIEL.getSelected().getElemente()[item].setPreis(float.Parse(input.text));

        UpdateKonten();
    }
    public void SetVerkaufspreis(TMP_InputField input)
    {
        if (input.text.Length == 0)
            return;
        int item = Int32.Parse(input.transform.parent.name.Replace("Element (", "").Replace(")", "")) - 1;
        Config.AUKTION_SPIEL.getSelected().getElemente()[item].setVerkaufspreis(float.Parse(input.text));

        UpdateKonten();
    }
    public void ShowItemImage(GameObject go)
    {
        int item = Int32.Parse(go.transform.parent.name.Replace("Element (", "").Replace(")", "")) -1;
        int bild = Int32.Parse(go.name.Replace("Bild (", "").Replace(")", "")) - 1;
        BildAnzeige.sprite = Config.AUKTION_SPIEL.getSelected().getElemente()[item].getBilder()[bild];
        BildAnzeige.gameObject.SetActive(true);
        Broadcast("#ShowItemImage " + item + "|" + bild);
    }
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
    public void ShowAllKonten(Toggle toggle)
    {
        Broadcast("#ShowAllKonten " + toggle.isOn);
    }
    public void ShowAllGUV(Toggle toggle)
    {
        Broadcast("#ShowAllGUV " + toggle.isOn);
    }
    #endregion
}
