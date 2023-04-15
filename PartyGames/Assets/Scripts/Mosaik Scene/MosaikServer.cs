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
        Broadcast(UpdateSpieler(), Config.PLAYERLIST);
    }
    /**
     * Aktualisiert die Spieler Anzeige Informationen & gibt diese als Text zurück
     */
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
    /**
     * Wechselt das Mosaik Game
     */
    public void ChangeMosaik(TMP_Dropdown drop)
    {
        Config.MOSAIK_SPIEL.setSelected(Config.MOSAIK_SPIEL.getMosaik(drop.value));
        bildIndex = 0;

        Bild[0].transform.parent.gameObject.GetComponent<Image>().sprite = Config.MOSAIK_SPIEL.getBeispiel();
        Bild[0].transform.parent.gameObject.SetActive(false);
        BildTitel = GameObject.Find("MosaikAnzeige/Server/Titel").GetComponent<TMP_Text>();
        BildTitel.text = Config.MOSAIK_SPIEL.getBeispiel().name;
        
        bildIndex = 0;
        bildListeText.GetComponent<TMP_Text>().text = "- Beispiel";
        for (int i = 0; i < Config.MOSAIK_SPIEL.getSelected().getNames().Count; i++)
        {
            bildListeText.GetComponent<TMP_Text>().text += "\n- " + Config.MOSAIK_SPIEL.getSelected().getNames()[i];
        }

        BildVorschau[0].GetComponent<Image>().sprite = Config.MOSAIK_SPIEL.getBeispiel();
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

    #region Mosaik Anzeige
    /**
     * Initialisiert die Anzeigen des Quizzes
     */
    private void InitMosaik()
    {
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

        List<string> games = new List<string>();
        foreach (Mosaik m in Config.MOSAIK_SPIEL.getMosaike())
        {
            games.Add(m.getTitel());
        }
        GameObject.Find("Einstellungen/ChangeMosaik").GetComponent<TMP_Dropdown>().ClearOptions();
        GameObject.Find("Einstellungen/ChangeMosaik").GetComponent<TMP_Dropdown>().AddOptions(games);
        GameObject.Find("Einstellungen/ChangeMosaik").GetComponent<TMP_Dropdown>().value = Config.MOSAIK_SPIEL.getIndex(Config.MOSAIK_SPIEL.getSelected());

    }
    /**
     * Blendet das Nächste/Vorherige Element in der Vorschau ein
     */
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
    // TODO:  TEIL 5: Lösung dafür finden das ich im Overlay sehe wenn alle fertig geladen sind

    IEnumerator LoadImageFromWeb(string imageUrl)
    {
        Debug.Log("Loading Image: "+ imageUrl);
        // TEIL 1: Download des Bildes
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUrl);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
            // TODO: Handling, nachricht an server, der erneut befehl geben kann/ oder custom bild einblenden
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
    private void SpielerHatBildGeladen(Player p, string data)
    {
        if (data.Equals("error"))
        {
            Debug.LogError(p.name + " konnte das Bild nicht laden.");
            return;
        }
        GameObject SpielerGeladenAnzeige = GameObject.Find("SpielerGeladenAnzeige");
        SpielerGeladenAnzeige.transform.GetChild(p.id - 1).gameObject.SetActive(true);
    }
    /**
     * Zeigt allen das ausgewählte Element
     */
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
    /**
     * Löst zufällige Cover auf
     */
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
    /**
     * Löst bestimmtes Cover auf
     */
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
    /**
     * Löst alle Cover auf
     */
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
    IEnumerator ToggleGridLayoutGroup(GameObject ob)
    {
        yield return new WaitForSeconds(0.01f);
        ob.GetComponent<GridLayoutGroup>().enabled = true;
        yield return new WaitForSeconds(0.01f);
        ob.GetComponent<GridLayoutGroup>().enabled = false;
        yield return null;
    }


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

    IEnumerator LoadImageFromWebIntoScene(string imageUrl)
    {
        Debug.Log("Loading Image: " + imageUrl);
        // TEIL 1: Download des Bildes
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUrl);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(www.error);
            yield break;
            // TODO: Handling, nachricht an server, der erneut befehl geben kann/ oder custom bild einblenden
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
