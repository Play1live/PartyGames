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

public class AuktionClient : MonoBehaviour
{
    GameObject[,] SpielerAnzeige;
    Image BildAnzeige;
    Sprite[,] bilder;
    string[,] urls;

    [SerializeField] AudioSource BuzzerSound;
    [SerializeField] AudioSource RichtigeAntwortSound;
    [SerializeField] AudioSource FalscheAntwortSound;
    [SerializeField] AudioSource DisconnectSound;

    void OnEnable()
    {
        if (!Config.CLIENT_STARTED)
            return;
        InitAnzeigen();

        ClientUtils.SendToServer("#JoinAuktion");

        StartCoroutine(TestConnectionToServer());
    }

    void Update()
    {
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
        Logging.log(Logging.LogType.Normal, "AuktionClient", "OnApplicationQuit", "Client wird geschlossen.");
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

        Logging.log(Logging.LogType.Normal, "AuktionClient", "CloseSocket", "Verbindung zum Server wurde getrennt. Client wird in das Hauptmenü geladen.");
    }
    #endregion
    #region Kommunikation
    /// <summary>
    /// Einkommende Nachrichten die vom Sever
    /// </summary>
    /// <param name="data"></param>
    private void OnIncomingData(string data)
    {
        if (data.StartsWith(Config.GAME_TITLE + "#"))
            data = data.Substring(Config.GAME_TITLE.Length);
        else
            Logging.log(Logging.LogType.Error, "AuktionClient", "OnIncommingData", "Wrong Command format: " + data);

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
    /// <param name="data"></param>
    /// <param name="cmd"></param>
    public void Commands(string data, string cmd)
    {
        Logging.log(Logging.LogType.Debug, "AuktionClient", "Commands", "Eingehende Nachricht: " + cmd + " -> " + data);
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "AuktionClient", "Commands", "Unkown Command: " + cmd + " -> " + data);
                break;

            #region Universal Commands
            case "#ServerClosed":
                Logging.log(Logging.LogType.Normal, "AuktionClient", "Commands", "Verbindung zum Server wurde getrennt. Lade zurück ins Hauptmenü");
                CloseSocket();
                SceneManager.LoadScene("Startup");
                break;
            case "#UpdateRemoteConfig":
                Logging.log(Logging.LogType.Debug, "AuktionClient", "Commands", "RemoteConfig wird aktualisiert");
                LoadConfigs.FetchRemoteConfig();
                break;
            case "#ZurueckInsHauptmenue":
                Logging.log(Logging.LogType.Debug, "AuktionClient", "Commands", "Spiel wird beendet, lade ins Hauptmenü.");
                SceneManager.LoadScene("Startup");
                break;
            #endregion
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

            case "#AuktionDownloadImages":
                Logging.log(Logging.LogType.Normal, "AuktionClient", "Commands", "Client soll alle Bilder laden: " + data);
                AuktionDownloadImages(data);
                break;
            case "#ShowCustomImage":
                ShowCustomImage(data);
                break;
            case "#HideImage":
                HideImage();
                break;
            case "#SpielerKonto":
                SpielerKonto(data);
                break;
            case "#ShowItemImage":
                ShowItemImage(data);
                break;
            case "#ShowAllKonten":
                ShowAllKonten(data);
                break;
            case "#ShowAllGUV":
                ShowAllGuv(data);
                break;
        }
    }
    /// <summary>
    /// Aktualisiert die Spieler Anzeigen
    /// </summary>
    /// <param name="data"></param>
    private void UpdateSpieler(string data)
    {
        string[] player = data.Replace("[TRENNER]", "|").Split('|');
        foreach (string sp in player)
        {
            int pId = Int32.Parse(sp.Replace("[ID]", "|").Split('|')[1]);
            int pos = Player.getPosInLists(pId);
            // Update PlayerInfos
            Config.PLAYERLIST[pos].points = Int32.Parse(sp.Replace("[PUNKTE]", "|").Split('|')[1]);
            // Display PlayerInfos                
            SpielerAnzeige[pos, 2].GetComponent<Image>().sprite = Config.PLAYERLIST[pos].icon;
            SpielerAnzeige[pos, 4].GetComponent<TMP_Text>().text = Config.PLAYERLIST[pos].name;
            SpielerAnzeige[pos, 5].GetComponent<TMP_Text>().text = Config.PLAYERLIST[pos].points+"";
            SpielerAnzeige[pos, 7].GetComponent<TMP_InputField>().text = sp.Replace("[KONTO]", "|").Split('|')[1];
            SpielerAnzeige[pos, 8].GetComponent<TMP_InputField>().text = sp.Replace("[GUV]", "|").Split('|')[1];

            // Items anzeigen
            string buyeditems = sp.Replace("[ITEMS]", "|").Split('|')[1];
            for (int j = 0; j < 10; j++)
            {
                SpielerAnzeige[pos, 6].transform.GetChild(j).gameObject.SetActive(false);
            }
            if (buyeditems.Length == 1)
            {
                SpielerAnzeige[pos, 6].transform.GetChild(Int32.Parse(buyeditems)).gameObject.SetActive(true);
            }
            else if (buyeditems.Length > 1)
            {
                string[] items = buyeditems.Split(',');
                for (int j = 0; j < items.Length; j++)
                {
                    SpielerAnzeige[pos, 6].transform.GetChild(Int32.Parse(items[j])).gameObject.SetActive(true);
                }
            }

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
    /// <summary>
    /// Zeigt an, welcher Spieler dran ist
    /// </summary>
    /// <param name="data"></param>
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
    /// Zeigt an, ob ein Spieler austabt
    /// </summary>
    /// <param name="data"></param>
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
        // Spieler Anzeige
        SpielerAnzeige = new GameObject[Config.SERVER_MAX_CONNECTIONS, 9]; // Anzahl benötigter Elemente
        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
        {
            SpielerAnzeige[i, 0] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")"); // Spieler Anzeige
            SpielerAnzeige[i, 1] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/BuzzerPressed"); // BuzzerPressed Umrandung
            SpielerAnzeige[i, 2] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Icon"); // Spieler Icon
            SpielerAnzeige[i, 3] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Ausgetabt"); // Ausgetabt Einblednung
            SpielerAnzeige[i, 4] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Infobar/Name"); // Spieler Name
            SpielerAnzeige[i, 5] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Infobar/Punkte"); // Spieler Punkte

            GameObject servercontrol = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/ServerControl");
            if (servercontrol != null)
                servercontrol.SetActive(false);

            SpielerAnzeige[i, 6] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Elemente");
            for (int j = 0; j < 10; j++)
            {
                SpielerAnzeige[i, 6].transform.GetChild(j).gameObject.SetActive(false);
            }
            SpielerAnzeige[i, 7] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Konto");
            SpielerAnzeige[i, 7].GetComponent<TMP_InputField>().text = "0";
            SpielerAnzeige[i, 8] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/GUV");
            SpielerAnzeige[i, 8].GetComponent<TMP_InputField>().text = "0";


            SpielerAnzeige[i, 0].SetActive(false); // Spieler Anzeige
            SpielerAnzeige[i, 1].SetActive(false); // BuzzerPressed Umrandung
            SpielerAnzeige[i, 3].SetActive(false); // Ausgetabt Einblendung
            SpielerAnzeige[i, 7].SetActive(false); // Konto ausblenden
            SpielerAnzeige[i, 8].SetActive(false); // GUV nur für Server
        }
        int pos = Player.getPosInLists(Config.PLAYER_ID);
        SpielerAnzeige[pos, 7].SetActive(true);

        //Auktion
        BildAnzeige = GameObject.Find("Auktion/Anzeige/Bild").GetComponent<Image>();
        BildAnzeige.gameObject.SetActive(false);
    }
    /// <summary>
    /// Spielt den Disconnect Sound ab
    /// </summary>
    private void PlayDisconnectSound()
    {
        DisconnectSound.Play();
    }
    /// <summary>
    /// Lädt Bilder per URL herunter
    /// </summary>
    /// <param name="data"></param>
    private void AuktionDownloadImages(string data)
    {
        int anz = Int32.Parse(data.Replace("[ANZ]", "|").Split('|')[1]);
        bilder = new Sprite[anz, 5];
        urls = new string[anz, 5];
        for (int j = 0; j < anz; j++)
        {
            string[] elemente = data.Replace("["+j+"]","|").Split('|')[1].Replace("<#>","|").Split('|');
            for (int i = 0; i < elemente.Length; i++)
            {
                urls[j, i] = elemente[i];
            }
        }
        StartCoroutine(DownloadAllImages());
    }
    /// <summary>
    /// Lädt die benötigten Bilder herunter
    /// </summary>
    /// <returns></returns>
    IEnumerator DownloadAllImages()
    {
        Logging.log(Logging.LogType.Normal, "AuktionServer", "DownloadAllImages", "Alle Bilder werden heruntergeladen.");
        for (int i = 0; i < urls.GetLength(0); i++)
        {
            for (int j = 0; j < urls.GetLength(1); j++)
            {
                string url = urls[i, j];
                UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
                yield return www.SendWebRequest();
                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    Logging.log(Logging.LogType.Warning, "AukitonClient", "DownloadAllImages", "Bild konnte nicht geladen werden: " + url + " << " + www.error);
                    ClientUtils.SendToServer("#ImageDownloadError "+ url);
                }
                else
                {
                    Texture2D texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);

                    bilder[i, j] = sprite;
                }
                yield return null;
            }
        }
        // Für Spieler einfügen
        for (int k = 0; k < Config.PLAYERLIST.Length; k++)
        {
            for (int i = 0; i < bilder.GetLength(0); i++)
            {
                SpielerAnzeige[k, 6].transform.GetChild(i).GetComponent<Image>().sprite = bilder[i, 0];
            }
        }

        // Server senden
        ClientUtils.SendToServer("#ImageDownloadedSuccessful");
        yield break;
    }
    /// <summary>
    /// Zeigt ein per URL eingefügtes Bild an
    /// </summary>
    /// <param name="data"></param>
    private void ShowCustomImage(string data)
    {
        StartCoroutine(LoadImageIntoScene(data));
    }
    /// <summary>
    /// Blendet die Bildanzeige aus
    /// </summary>
    private void HideImage()
    {
        BildAnzeige.gameObject.SetActive(false);
    }
    /// <summary>
    /// Lädt ein Bild in die Scene und zeigt dieses dann an
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    IEnumerator LoadImageIntoScene(string url)
    {
        Logging.log(Logging.LogType.Normal, "AuktionServer", "LoadImageIntoScene", "Bild wird heruntergeladen.");
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Logging.log(Logging.LogType.Warning, "AuktionClient", "LoadImageIntoScene", "Bild konnte nicht geladen werden: " + url + " << " + www.error);
            ClientUtils.SendToServer("#LoadImageIntoSceneError");
        }
        else
        {
            try
            {
                Texture2D texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
                BildAnzeige.sprite = sprite;
                BildAnzeige.gameObject.SetActive(true);
                ClientUtils.SendToServer("#LoadImageIntoSceneSuccess");
            }
            catch (Exception e)
            {
                Logging.log(Logging.LogType.Warning, "AuktionServer", "LoadImageIntoScene", "Custombild konnte nicht geladen werden: " + url + " << ", e);
                ClientUtils.SendToServer("#ImageDownloadError");
            }
        }
        yield break;
    }
    /// <summary>
    /// Aktualisiert das Guthaben des Spielerkontos
    /// </summary>
    /// <param name="data"></param>
    private void SpielerKonto(string data)
    {
        if (!data.EndsWith("€"))
        {
            if (!data.EndsWith(" "))
                data = data + " ";
            data = data + "€";
        }


        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            SpielerAnzeige[i, 7].GetComponent<TMP_InputField>().text = data;
        }
    }
    /// <summary>
    /// Zeigt Verkaufselement an
    /// </summary>
    /// <param name="data"></param>
    private void ShowItemImage(string data)
    {
        int item = Int32.Parse(data.Split('|')[0]);
        int bild = Int32.Parse(data.Split('|')[1]);
        BildAnzeige.sprite = bilder[item, bild];
        BildAnzeige.gameObject.SetActive(true);
    }
    /// <summary>
    /// Zeigt die Konten der anderen Spieler an
    /// </summary>
    /// <param name="data"></param>
    private void ShowAllKonten(string data)
    {
        bool toggle = bool.Parse(data);
        
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            SpielerAnzeige[i, 7].SetActive(toggle);
        }
        SpielerAnzeige[Player.getPosInLists(Config.PLAYER_ID), 7].SetActive(true);
    }
    /// <summary>
    /// Zeigt die GUV aller Spieler an
    /// </summary>
    /// <param name="data"></param>
    private void ShowAllGuv(string data)
    {
        bool toggle = bool.Parse(data);

        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            SpielerAnzeige[i, 8].SetActive(toggle);
        }
    }
}