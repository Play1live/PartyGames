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

public class JeopardyClient : MonoBehaviour
{
    // JeopardyAuswahl
    GameObject JeopardyAuswahlAnzeige;
    GameObject AuswahlGrid;
    // JeopardyElementAnzeige
    GameObject JeopardyElementAnzeige;
    GameObject AnzeigeFrage;
    Vector2 BildRect;
    GameObject Bild;

    GameObject SpielerAntwortEingabe;
    GameObject[,] SpielerAnzeige;
    bool pressingbuzzer = false;

    [SerializeField] AudioSource BuzzerSound;
    [SerializeField] AudioSource RichtigeAntwortSound;
    [SerializeField] AudioSource FalscheAntwortSound;
    [SerializeField] AudioSource DisconnectSound;

    void OnEnable()
    {
        InitAnzeigen();

        if (!Config.CLIENT_STARTED)
            return;
        ClientUtils.SendToServer("#JoinJeopardy");

        StartCoroutine(TestConnectionToServer());
    }

    void Update()
    {
        // Leertaste kann Buzzern
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!pressingbuzzer)
            {
                pressingbuzzer = true;
                SpielerBuzzered();
            }
        }
        else if (Input.GetKeyUp(KeyCode.Space) && pressingbuzzer)
        {
            pressingbuzzer = false;
        }


        #region Prüft auf Nachrichten vom Server
        if (Config.CLIENT_STARTED)
        {
            NetworkStream stream = Config.CLIENT_TCP.GetStream();
            if (stream.DataAvailable)
            {
                StreamReader reader = new StreamReader(stream);
                string data = reader.ReadLine();
                if (data != null)
                    OnIncomingData(data);
            }
        }
        #endregion
    }

    private void OnApplicationFocus(bool focus)
    {
        ClientUtils.SendToServer("#ClientFocusChange " + focus);
    }

    private void OnApplicationQuit()
    {
        Logging.log(Logging.LogType.Normal, "MosaikClient", "OnApplicationQuit", "Client wird geschlossen.");
        ClientUtils.SendToServer("#ClientClosed");
        CloseSocket();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    /// <summary>
    /// Testet die Verbindung zum Server
    /// </summary>
    IEnumerator TestConnectionToServer()
    {
        while (Config.CLIENT_STARTED)
        {
            ClientUtils.SendToServer("#TestConnection");
            yield return new WaitForSeconds(10);
        }
        yield break;
    }
    #region Verbindungen
    /// <summary>
    /// Trennt die Verbindung zum Server
    /// </summary>
    private void CloseSocket()
    {
        if (!Config.CLIENT_STARTED)
            return;

        Config.CLIENT_TCP.Close();
        Config.CLIENT_STARTED = false;

        Logging.log(Logging.LogType.Normal, "MosaikClient", "CloseSocket", "Verbindung zum Server wurde getrennt. Client wird in das Hauptmenü geladen.");
    }
    #endregion
    #region Kommunikation
    /// <summary>
    /// Einkommende Nachrichten die vom Sever
    /// </summary>
    /// <param name="data">Nachricht</param>
    private void OnIncomingData(string data)
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

        Commands(data, cmd);
    }
    #endregion
    /// <summary>
    /// Eingehende Commands vom Server
    /// </summary>
    /// <param name="data">Befehlsargumente</param>
    /// <param name="cmd">Befehl</param>
    private void Commands(string data, string cmd)
    {
        Logging.log(Logging.LogType.Debug, "MosaikClient", "Commands", "Eingehende Nachricht: " + cmd + " -> " + data);
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "MosaikClient", "Commands", "Unkown Command: " + cmd + " -> " + data);
                break;

            #region Universal Commands
            case "#ServerClosed":
                Logging.log(Logging.LogType.Normal, "MosaikClient", "Commands", "Verbindung zum Server wurde beendet. Lade zurück ins Hauptmenü.");
                CloseSocket();
                SceneManager.LoadScene("Startup");
                break;
            case "#UpdateRemoteConfig":
                Logging.log(Logging.LogType.Normal, "MosaikClient", "Commands", "Aktualisiere die RemoteConfig.");
                LoadConfigs.FetchRemoteConfig();
                break;
            case "#ZurueckInsHauptmenue":
                Logging.log(Logging.LogType.Normal, "MosaikClient", "Commands", "Spiel wurde beendet. Lade ins Hauptmenü");
                SceneManager.LoadScene("Startup");
                break;
            #endregion
            #region BuzzerSpieler Anzeigen
            case "#UpdateSpieler":
                UpdateSpieler(data);
                break;
            case "#SpielerAusgetabt":
                SpielerAusgetabt(data);
                break;
             case "#SpielerIstDran":
                SpielerIstDran(data);
                break;
            case "#SpielerIstNichtDran":
                SpielerIstNichtDran(data);
                break;
            case "#AudioBuzzerPressed":
                AudioBuzzerPressed(data);
                break;
            case "#AudioRichtigeAntwort":
                AudioRichtigeAntwort();
                break;
            case "#AudioFalscheAntwort":
                AudioFalscheAntwort();
                break;
            case "#BuzzerFreigeben":
                BuzzerFreigeben();
                break;
            #endregion

            case "#AuswahlUebersicht":
                LoadAuswahlGrid(data);
                break;
            case "#SelectElement":
                SelectElement(data);
                break;
            case "#ElementFrage":
                ElementFrage(data);
                break;
            case "#ZurueckZurAuswahl":
                ZurueckZurAuswahl();
                break;
            case "#ElementBildLaden":
                ElementBildLaden(data);
                break;
            case "#ElementBildEinblenden":
                ElementBildEinblenden();
                break;
            case "#EingabefeldToggle":
                EingabefeldToggle(data);
                break;
        }
    }
    /// <summary>
    /// Aktualisiert die Spieler Anzeigen
    /// </summary>
    /// <param name="data">#UpdateSpieler ...</param>
    private void UpdateSpieler(string data)
    {
        string[] player = data.Replace("[TRENNER]", "|").Split('|');
        foreach (string sp in player)
        {
            int pId = Int32.Parse(sp.Replace("[ID]", "|").Split('|')[1]);

            // Display ServerInfos
            if (pId == 0)
            {
            }
            // Display Client Infos
            else
            {
                int pos = Player.getPosInLists(pId);
                // Update PlayerInfos
                Config.PLAYERLIST[pos].points = Int32.Parse(sp.Replace("[PUNKTE]", "|").Split('|')[1]);
                // Display PlayerInfos                
                SpielerAnzeige[pos, 2].GetComponent<Image>().sprite = Config.PLAYERLIST[pos].icon2.icon;
                SpielerAnzeige[pos, 4].GetComponent<TMP_Text>().text = Config.PLAYERLIST[pos].name;
                SpielerAnzeige[pos, 5].GetComponent<TMP_Text>().text = Config.PLAYERLIST[pos].points+"";
                // Verbundene Spieler anzeigen
                bool connected = bool.Parse(sp.Replace("[ONLINE]", "|").Split('|')[1]);
                if (Config.PLAYERLIST[pos].name != "" && connected)
                {
                    SpielerAnzeige[pos, 0].SetActive(true);
                }
                else
                {
                    if (SpielerAnzeige[pos, 0].activeInHierarchy && !connected)
                    {
                        Config.PLAYERLIST[pos].name = "";
                        PlayDisconnectSound();
                    }

                    SpielerAnzeige[pos, 0].SetActive(false);
                }
            }
        }
    }
    /// <summary>
    /// Spielt den Disconnect Sound ab
    /// </summary>
    private void PlayDisconnectSound()
    {
        DisconnectSound.Play();
    }
    /// <summary>
    /// Sendet eine Buzzer Anfrage an den Server
    /// </summary>
    public void SpielerBuzzered()
    {
        ClientUtils.SendToServer("#SpielerBuzzered");
    }
    /// <summary>
    /// Gibt den Buzzer frei
    /// </summary>
    private void BuzzerFreigeben()
    {
        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
            SpielerAnzeige[i, 1].SetActive(false);
    }
    /// <summary>
    /// Spielt Sound des Buzzers ab und zeigt welcher Spieler diesen gedrückt hat
    /// </summary>
    /// <param name="data">Spielerid</param>
    private void AudioBuzzerPressed(string data)
    {
        BuzzerSound.Play();
        int pId = Int32.Parse(data);
        SpielerAnzeige[Player.getPosInLists(pId), 1].SetActive(true);
    }
    /// <summary>
    /// Zeigt an, welcher Spieler dran ist
    /// </summary>
    /// <param name="data">Spielerid</param>
    private void SpielerIstDran(string data)
    {
        int pId = Int32.Parse(data);
        SpielerAnzeige[Player.getPosInLists(pId), 1].SetActive(true);
    }
    /// <summary>
    /// Deaktiviert die Spieler ist dran anzeige
    /// </summary>
    /// <param name="data"></param>
    private void SpielerIstNichtDran(string data)
    {
        int pId = Int32.Parse(data);
        SpielerAnzeige[Player.getPosInLists(pId), 1].SetActive(false);
    }
    /// <summary>
    /// Spielt den Sound für eine richtige Antwort ab
    /// </summary>
    private void AudioRichtigeAntwort()
    {
        RichtigeAntwortSound.Play();
    }
    /// <summary>
    /// Spielt den Sound für eine falsche Antwort ab
    /// </summary>
    private void AudioFalscheAntwort()
    {
        FalscheAntwortSound.Play();
    }
    /// <summary>
    /// Zeigt an, ob ein Spieler austabt
    /// </summary>
    /// <param name="data">Spielerid bool</param>
    private void SpielerAusgetabt(string data)
    {
        // AustabenAnzeigen wird deaktiviert
        if (data == "0")
        {
            for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
            {
                SpielerAnzeige[i, 3].SetActive(false);
            }
        }
        // Austaben für Spieler anzeigen
        else
        {
            int id = Int32.Parse(data.Split(' ')[0]);
            int pos = Player.getPosInLists(id);
            bool type = Boolean.Parse(data.Split(' ')[1]);

            SpielerAnzeige[pos, 3].SetActive(type);
        }
    }
    /// <summary>
    /// Initialisiert die Anzeigen der Scene
    /// </summary>
    private void InitAnzeigen()
    {
        Logging.log(Logging.LogType.Debug, "JeopardyClient", "InitAnzeigen", "Initialisiert die Anzeigen");
        // Spieler Antworteingabe Deaktivieren
        SpielerAntwortEingabe = GameObject.Find("SpielerAntwortEingabe");
        SpielerAntwortEingabe.SetActive(false);
        // Spieler Anzeige
        SpielerAnzeige = new GameObject[Config.SERVER_MAX_CONNECTIONS, 6]; // Anzahl benötigter Elemente
        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
        {
            GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/ServerControl").SetActive(false); // Spieler Anzeige
            SpielerAnzeige[i, 0] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")"); // Spieler Anzeige
            SpielerAnzeige[i, 1] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/BuzzerPressed"); // BuzzerPressed Umrandung
            SpielerAnzeige[i, 2] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Icon"); // Spieler Icon
            SpielerAnzeige[i, 3] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Ausgetabt"); // Ausgetabt Einblednung
            SpielerAnzeige[i, 4] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Infobar/Name"); // Spieler Name
            SpielerAnzeige[i, 5] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Infobar/Punkte"); // Spieler Punkte
            GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/SpielerAntwort").gameObject.SetActive(false);

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
        
        JeopardyAuswahlAnzeige = GameObject.Find("JeopardyAuswahl");
        JeopardyAuswahlAnzeige.SetActive(true);

        // AnzeigeFrage
        AnzeigeFrage = GameObject.Find("JeopardyElementAnzeige/Grid/Frage");
        // AnzeigeBild   
        Bild = GameObject.Find("JeopardyElementAnzeige/Grid/BildImage");
        Bild.SetActive(false);
        BildRect = new Vector2(Bild.GetComponent<RectTransform>().rect.width, Bild.GetComponent<RectTransform>().rect.height);
        JeopardyElementAnzeige = GameObject.Find("JeopardyElementAnzeige");
        JeopardyElementAnzeige.SetActive(false);
    }
    private void LoadAuswahlGrid(string data)
    {
        for (int i = 0; i < data.Split('|').Length; i++)
        {
            AuswahlGrid.transform.GetChild(i).gameObject.SetActive(true);
            AuswahlGrid.transform.GetChild(i).GetChild(0).gameObject.SetActive(true);
            AuswahlGrid.transform.GetChild(i).GetChild(0).GetComponent<Button>().enabled = false;
            AuswahlGrid.transform.GetChild(i).GetChild(0).GetComponentInChildren<TMP_Text>().text = data.Split('|')[i].Split('~')[0];
            for (int j = 2; j < data.Split('|')[i].Split('~').Length; j++)
            {
                AuswahlGrid.transform.GetChild(i).GetChild(j).gameObject.SetActive(true);
                AuswahlGrid.transform.GetChild(i).GetChild(j).GetComponentInChildren<TMP_Text>().text = data.Split('|')[i].Split('~')[j];
            }
        }

        ClientUtils.SendToServer("#GetPlayerUpdate");
    }
    private void SelectElement(string data)
    {
        AnzeigeFrage.GetComponentInChildren<TMP_Text>().text = "";
        Bild.SetActive(false);
        JeopardyAuswahlAnzeige.SetActive(false);
        JeopardyElementAnzeige.SetActive(true);

        AuswahlGrid.transform.GetChild(Int32.Parse(data.Split('|')[0])).GetChild(2 + Int32.Parse(data.Split('|')[1])).GetComponent<Button>().interactable = false;
    }
    private void ElementFrage(string data)
    {
        AnzeigeFrage.GetComponentInChildren<TMP_Text>().text = data;
    }
    private void ZurueckZurAuswahl()
    {
        JeopardyAuswahlAnzeige.SetActive(true);
        JeopardyElementAnzeige.SetActive(false);
    }
    private void ElementBildLaden(string data)
    {
        if (data.Length == 0)
        {
            Bild.SetActive(false);
            return;
        }
        Bild.SetActive(false);
        StartCoroutine(LoadImageFromWeb(data));
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
    private void ElementBildEinblenden()
    {
        Bild.SetActive(true);
    }
    private void ClientHatBildGeladen(Sprite sprite)
    {
        LoadImageIntoPreview(sprite);
        ClientUtils.SendToServer("#ClientHatBildGeladen");
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
            ClientHatBildGeladen(sprite);
        }
        yield return null;
    }
    private void EingabefeldToggle(string data)
    {
        bool toggle = bool.Parse(data);
        SpielerAntwortEingabe.SetActive(toggle);
    }
    public void SendEingabeUpdate(TMP_InputField input)
    {
        ClientUtils.SendToServer("#AntwortEingabeUpdate " + input.text);
    }
}