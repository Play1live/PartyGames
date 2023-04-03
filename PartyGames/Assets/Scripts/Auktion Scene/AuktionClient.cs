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

    void OnEnable()
    {
        if (!Config.CLIENT_STARTED)
            return;
        InitAnzeigen();

        SendToServer("#JoinAuktion");

        StartCoroutine(TestConnectionToServer());
    }
    IEnumerator TestConnectionToServer()
    {
        while (Config.CLIENT_STARTED)
        {
            SendToServer("#TestConnection");
            yield return new WaitForSeconds(10);
        }
    }

    void Update()
    {
        #region Pr�ft auf Nachrichten vom Server
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
        SendToServer("#ClientFocusChange " + focus);
    }

    private void OnApplicationQuit()
    {
        Logging.add(Logging.Type.Normal, "Client", "OnApplicationQuit", "Client wird geschlossen.");
        SendToServer("#ClientClosed");
        CloseSocket();
    }

    #region Verbindungen
    /**
     * Trennt die Verbindung zum Server
     */
    private void CloseSocket()
    {
        if (!Config.CLIENT_STARTED)
            return;

        Config.CLIENT_TCP.Close();
        Config.CLIENT_STARTED = false;

        Logging.add(Logging.Type.Normal, "Client", "CloseSocket", "Verbindung zum Server wurde getrennt. Client wird in das Hauptmen� geladen.");
    }
    #endregion
    #region Kommunikation
    /**
     * Sendet eine Nachricht an den Server.
     */
    public void SendToServer(string data)
    {
        if (!Config.CLIENT_STARTED)
            return;

        try
        {
            NetworkStream stream = Config.CLIENT_TCP.GetStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.WriteLine(data);
            writer.Flush();
        }
        catch (Exception e)
        {
            Logging.add(Logging.Type.Error, "Client", "SendToServer", "Nachricht an Server konnte nicht gesendet werden." + e);
            Config.HAUPTMENUE_FEHLERMELDUNG = "Verbindung zum Server wurde verloren.";
            CloseSocket();
            SceneManager.LoadSceneAsync("StartUp");
        }
    }
    /**
     * Einkommende Nachrichten die vom Sever
     */
    private void OnIncomingData(string data)
    {
        string cmd;
        if (data.Contains(" "))
            cmd = data.Split(' ')[0];
        else
            cmd = data;
        data = data.Replace(cmd + " ", "");

        Commands(data, cmd);
    }
    #endregion
    /**
     * Eingehende Commands vom Server
     */
    public void Commands(string data, string cmd)
    {
        //Debug.Log("Eingehend: " + cmd + " -> " + data);
        switch (cmd)
        {
            default:
                Logging.add(Logging.Type.Warning, "QuizClient", "Commands", "Unkown Command -> " + cmd + " - " + data);
                break;

            #region Universal Commands
            case "#ServerClosed":
                CloseSocket();
                SceneManager.LoadScene("Startup");
                break;
            case "#UpdateRemoteConfig":
                LoadConfigs.FetchRemoteConfig();
                break;
            case "#ZurueckInsHauptmenue":
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
                Debug.LogWarning(data);
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

    /**
     * Aktualisiert die Spieler Anzeigen
     */
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
            if (Config.PLAYERLIST[pos].name != "")
            {
                SpielerAnzeige[pos, 0].SetActive(true);
            }
            else
            {
                SpielerAnzeige[pos, 0].SetActive(false);
            }
            
        }
    }
    /**
     * Zeigt an, welcher Spieler dran ist
     */
    private void SpielerIstDran(string data)
    {
        int pId = Int32.Parse(data);
        SpielerAnzeige[Player.getPosInLists(pId), 1].SetActive(true);
    }
    /**
     * Deaktiviert die Spieler ist dran anzeige
     */
    private void SpielerIstNichtDran(string data)
    {
        int pId = Int32.Parse(data);
        SpielerAnzeige[Player.getPosInLists(pId), 1].SetActive(false);
    }
    /**
     * Zeigt an, ob ein Spieler austabt
     */
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
        // Austaben f�r Spieler anzeigen
        else
        {
            int id = Int32.Parse(data.Split(' ')[0]);
            int pos = Player.getPosInLists(id);
            bool type = Boolean.Parse(data.Split(' ')[1]);

            SpielerAnzeige[pos, 3].SetActive(type);
        }
    }

    /**
     * Initialisiert die Anzeigen der Scene
     */
    private void InitAnzeigen()
    {
        // Spieler Anzeige
        SpielerAnzeige = new GameObject[Config.SERVER_MAX_CONNECTIONS, 9]; // Anzahl ben�tigter Elemente
        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
        {
            SpielerAnzeige[i, 0] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")"); // Spieler Anzeige
            SpielerAnzeige[i, 1] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/BuzzerPressed"); // BuzzerPressed Umrandung
            SpielerAnzeige[i, 2] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Icon"); // Spieler Icon
            SpielerAnzeige[i, 3] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Ausgetabt"); // Ausgetabt Einblednung
            SpielerAnzeige[i, 4] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Infobar/Name"); // Spieler Name
            SpielerAnzeige[i, 5] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Infobar/Punkte"); // Spieler Punkte

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
            SpielerAnzeige[i, 8].SetActive(false); // GUV nur f�r Server
        }
        int pos = Player.getPosInLists(Config.PLAYER_ID);
        SpielerAnzeige[pos, 7].SetActive(true);

        //Auktion
        BildAnzeige = GameObject.Find("Auktion/Anzeige/Bild").GetComponent<Image>();
        BildAnzeige.gameObject.SetActive(false);

    }
 
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
    IEnumerator DownloadAllImages()
    {
        for (int i = 0; i < urls.GetLength(0); i++)
        {
            for (int j = 0; j < urls.GetLength(1); j++)
            {
                string url = urls[i, j];
                UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
                yield return www.SendWebRequest();
                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError("Auktion: Bild konnte nicht geladen werden: " + url + " << " + www.error);
                    SendToServer("#ImageDownloadError "+ url);
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
        // F�r Spieler einf�gen
        for (int k = 0; k < Config.PLAYERLIST.Length; k++)
        {
            for (int i = 0; i < bilder.GetLength(0); i++)
            {
                SpielerAnzeige[k, 6].transform.GetChild(i).GetComponent<Image>().sprite = bilder[i, 0];
            }
        }

        // Server senden
        SendToServer("#ImageDownloadedSuccessful");
        yield return null;
    }

    private void ShowCustomImage(string data)
    {
        StartCoroutine(LoadImageIntoScene(data));
    }
    private void HideImage()
    {
        BildAnzeige.gameObject.SetActive(false);
    }
    IEnumerator LoadImageIntoScene(string url)
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Auktion: Bild konnte nicht geladen werden: " + url + " << " + www.error);
            SendToServer("#LoadImageIntoSceneError");
        }
        else
        {
            Texture2D texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
            BildAnzeige.sprite = sprite;
            BildAnzeige.gameObject.SetActive(true);
            SendToServer("#LoadImageIntoSceneSuccess");
        }
        yield return null;
    }
    private void SpielerKonto(string data)
    {
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            SpielerAnzeige[i, 7].GetComponent<TMP_InputField>().text = data;
        }
    }
    private void ShowItemImage(string data)
    {
        int item = Int32.Parse(data.Split('|')[0]);
        int bild = Int32.Parse(data.Split('|')[1]);
        BildAnzeige.sprite = bilder[item, bild];
        BildAnzeige.gameObject.SetActive(true);
    }
    private void ShowAllKonten(string data)
    {
        bool toggle = bool.Parse(data);
        
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            SpielerAnzeige[i, 7].SetActive(toggle);
        }
        SpielerAnzeige[Player.getPosInLists(Config.PLAYER_ID), 7].SetActive(true);
    }
    private void ShowAllGuv(string data)
    {
        bool toggle = bool.Parse(data);

        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            SpielerAnzeige[i, 8].SetActive(toggle);
        }
    }
}