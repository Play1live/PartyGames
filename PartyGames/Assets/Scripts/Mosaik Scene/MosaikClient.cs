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

public class MosaikClient : MonoBehaviour
{
    Vector2 BildRect;
    GameObject[] Bild;
    List<int> coverlist;

    List<string> geladeneURls;
    List<Sprite> geladeneBilder;

    GameObject[,] SpielerAnzeige;
    bool pressingbuzzer = false;

    [SerializeField] AudioSource BuzzerSound;
    [SerializeField] AudioSource RichtigeAntwortSound;
    [SerializeField] AudioSource FalscheAntwortSound;

    void OnEnable()
    {
        InitAnzeigen();

        if (!Config.CLIENT_STARTED)
            return;
        SendToServer("#JoinMosaik");

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

        Logging.add(Logging.Type.Normal, "Client", "CloseSocket", "Verbindung zum Server wurde getrennt. Client wird in das Hauptmenü geladen.");
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

            case "#MosaikEinblendenAusblenden":
                MosaikEinblendenAusblenden(data);
                break;
            case "#MosaikCoverAuflösen":
                MosaikCoverAuflösen(data);
                break;
            case "#MosaikAllesAuflösen":
                MosaikAllesAuflösen();
                break;
            case "#DownloadCustom":
                DownloadCustom(data);
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
                SpielerAnzeige[pos, 2].GetComponent<Image>().sprite = Config.PLAYERLIST[pos].icon;
                SpielerAnzeige[pos, 4].GetComponent<TMP_Text>().text = Config.PLAYERLIST[pos].name;
                SpielerAnzeige[pos, 5].GetComponent<TMP_Text>().text = Config.PLAYERLIST[pos].points+"";
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
    }
    /**
     * Sendet eine Buzzer Anfrage an den Server
     */
    public void SpielerBuzzered()
    {
        SendToServer("#SpielerBuzzered");
    }
    /**
     * Gibt den Buzzer frei
     */
    private void BuzzerFreigeben()
    {
        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
            SpielerAnzeige[i, 1].SetActive(false);
    }
    /**
     * Spielt Sound des Buzzers ab und zeigt welcher Spieler diesen gedrückt hat
     */
    private void AudioBuzzerPressed(string data)
    {
        BuzzerSound.Play();
        int pId = Int32.Parse(data);
        SpielerAnzeige[Player.getPosInLists(pId), 1].SetActive(true);
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
     * Spielt den Sound für eine richtige Antwort ab
     */
    private void AudioRichtigeAntwort()
    {
        RichtigeAntwortSound.Play();
    }
    /**
     * Spielt den Sound für eine falsche Antwort ab
     */
    private void AudioFalscheAntwort()
    {
        FalscheAntwortSound.Play();
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
        // Austaben für Spieler anzeigen
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
        geladeneURls = new List<string>();
        geladeneBilder = new List<Sprite>();
        // ImageAnzeige
        Bild = new GameObject[49];
        coverlist = new List<int>();
        for (int i = 0; i < 49; i++)
        {
            Bild[i] = GameObject.Find("MosaikAnzeige/BildImage/Cover (" + i + ")");
            Bild[i].GetComponent<Animator>().enabled = false;
            Bild[i].GetComponent<RectTransform>().sizeDelta = new Vector2(100, 100);
            Bild[i].GetComponent<RectTransform>().eulerAngles = new Vector3(0, 0, 0);
            Bild[i].GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
            coverlist.Add(i);
        }
        BildRect = new Vector2(Bild[0].transform.parent.GetComponent<RectTransform>().rect.width, Bild[0].transform.parent.GetComponent<RectTransform>().rect.height);
        Bild[0].transform.parent.gameObject.SetActive(false);

        // Spieler Anzeige
        SpielerAnzeige = new GameObject[Config.SERVER_MAX_CONNECTIONS, 7]; // Anzahl benötigter Elemente
        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
        {
            SpielerAnzeige[i, 0] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")"); // Spieler Anzeige
            SpielerAnzeige[i, 1] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/BuzzerPressed"); // BuzzerPressed Umrandung
            SpielerAnzeige[i, 2] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Icon"); // Spieler Icon
            SpielerAnzeige[i, 3] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Ausgetabt"); // Ausgetabt Einblednung
            SpielerAnzeige[i, 4] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Infobar/Name"); // Spieler Name
            SpielerAnzeige[i, 5] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Infobar/Punkte"); // Spieler Punkte
            SpielerAnzeige[i, 6] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/ServerControl"); // Server Settings

            SpielerAnzeige[i, 0].SetActive(false); // Spieler Anzeige
            SpielerAnzeige[i, 1].SetActive(false); // BuzzerPressed Umrandung
            SpielerAnzeige[i, 3].SetActive(false); // Ausgetabt Einblendung
            SpielerAnzeige[i, 6].SetActive(false); // Server Settings
        }
    }
    /**
     * Blendet Bild ein/aus
     */
    private void MosaikEinblendenAusblenden(string data)
    {
        bool einblenden = Boolean.Parse(data.Replace("[!#!]", "|").Split('|')[0]);
        string url = data.Replace("[!#!]", "|").Split('|')[1];

        // Ausblenden
        if (einblenden == false)
        {
            Bild[0].transform.parent.gameObject.SetActive(einblenden);
        }
        // Einblenden
        else
        {
            // Beispiel Bild
            if (url.Equals("Beispiel"))
            {
                Bild[0].transform.parent.gameObject.GetComponent<Image>().sprite = Config.MOSAIK_SPIEL.getBeispiel();
                Bild[0].transform.parent.gameObject.SetActive(einblenden);
                SendToServer("#SpielerHatBildGeladen success");
            }
            // Unbekanntes Bild
            else
            {
                // Schauen ob Bild bereits vorhanden
                if (geladeneURls.Contains(url))
                {
                    Bild[0].transform.parent.gameObject.GetComponent<Image>().sprite = geladeneBilder[geladeneURls.IndexOf(url)];
                    Bild[0].transform.parent.gameObject.SetActive(einblenden);
                    SendToServer("#SpielerHatBildGeladen success");
                }
                // Sonst Bild herunterladen
                else
                {
                    StartCoroutine(LoadImageFromWeb(url));
                }
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
                Bild[i].GetComponent<RectTransform>().sizeDelta = new Vector2(100, 100);
                Bild[i].GetComponent<RectTransform>().eulerAngles = new Vector3(0, 0, 0);
                Bild[i].GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
                Bild[i].GetComponent<Animator>().enabled = false;
                Bild[i].SetActive(true);
            }
        }
    }
    IEnumerator LoadImageFromWeb(string imageUrl)
    {
        Debug.Log("Loading Image: " + imageUrl);
        // TEIL 1: Download des Bildes
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUrl);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
            SendToServer("#SpielerHatBildGeladen error");
        }
        else
        {
            Texture2D myTexture1 = ((DownloadHandlerTexture)www.downloadHandler).texture;
            Sprite sprite1 = Sprite.Create(myTexture1, new Rect(0, 0, myTexture1.width, myTexture1.height), new Vector2(0.5f, 0.5f), 100);
            geladeneURls.Add(imageUrl);
            geladeneBilder.Add(sprite1);
            SendToServer("#SpielerHatBildGeladen success");

            Bild[0].transform.parent.gameObject.GetComponent<Image>().sprite = geladeneBilder[geladeneURls.IndexOf(imageUrl)];
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
            Bild[i].GetComponent<RectTransform>().sizeDelta = new Vector2(100, 100);
            Bild[i].GetComponent<RectTransform>().eulerAngles = new Vector3(0, 0, 0);
            Bild[i].GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
            Bild[i].GetComponent<Animator>().enabled = false;
            Bild[i].SetActive(true);
        }

        yield return null;
    }
    /**
     * Löst bestimmtes Cover auf
     */
    private void MosaikCoverAuflösen(string data)
    {
        int index = Int32.Parse(data);

        Bild[index].SetActive(false);
        Bild[index].GetComponent<Animator>().enabled = false;
        Bild[index].GetComponent<Animator>().enabled = true;
        Bild[index].SetActive(true);
    }
    /**
     * Löst alle Cover auf
     */
    private void MosaikAllesAuflösen()
    {
        for (int i = 0; i < 49; i++)
        {
            Bild[i].SetActive(false);
            Bild[i].GetComponent<Animator>().enabled = false;
            Bild[i].GetComponent<Animator>().enabled = true;
            Bild[i].SetActive(true);
        }
    }

    public void DownloadCustom(string data)
    {
        StartCoroutine(LoadImageFromWebIntoScene(data));
    }
    IEnumerator LoadImageFromWebIntoScene(string imageUrl)
    {
        // Blendet Cover ein
        for (int i = 0; i < 49; i++)
        {
            Bild[i].GetComponent<RectTransform>().sizeDelta = new Vector2(100, 100);
            Bild[i].GetComponent<RectTransform>().eulerAngles = new Vector3(0, 0, 0);
            Bild[i].GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
            Bild[i].GetComponent<Animator>().enabled = false;
            Bild[i].SetActive(true);
        }
        Debug.Log("Loading Image: " + imageUrl);
        // TEIL 1: Download des Bildes
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUrl);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
            SendToServer("#SpielerHatBildGeladen error");
        }
        else
        {
            Texture2D myTexture2 = ((DownloadHandlerTexture)www.downloadHandler).texture;
            Sprite sprite2 = Sprite.Create(myTexture2, new Rect(0, 0, myTexture2.width, myTexture2.height), new Vector2(0.5f, 0.5f), 100);

            SendToServer("#SpielerHatBildGeladen success");

            Bild[0].transform.parent.gameObject.GetComponent<Image>().sprite = sprite2;
        }
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
        Bild[0].transform.parent.gameObject.SetActive(true);
        yield return null;
    }
}