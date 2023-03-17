using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MosaikClient : MonoBehaviour
{
    GameObject[] Bild;
    List<int> coverlist;
    int bildIndex;

    GameObject SpielerAntwortEingabe;
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

        NetworkStream stream = Config.CLIENT_TCP.GetStream();
        StreamWriter writer = new StreamWriter(stream);
        writer.WriteLine(data);
        writer.Flush();
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
        Debug.Log("Eingehend: " + cmd + " -> " + data);
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
        // ImageAnzeige
        Bild = new GameObject[49];
        coverlist = new List<int>();
        for (int i = 0; i < 49; i++)
        {
            Bild[i] = GameObject.Find("MosaikAnzeige/Image/Cover (" + i + ")");
            Bild[i].GetComponent<Animator>().enabled = false;
            Bild[i].GetComponent<RectTransform>().sizeDelta = new Vector2(100, 100);
            Bild[i].GetComponent<RectTransform>().eulerAngles = new Vector3(0, 0, 0);
            Bild[i].GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
            coverlist.Add(i);
        }
        Bild[0].transform.parent.gameObject.GetComponent<Image>().sprite = Config.MOSAIK_SPIEL.getBeispiel();
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
        bool einblenden = Boolean.Parse(data.Replace("[BOOL]", "|").Split('|')[1]);
        int index = Int32.Parse(data.Replace("[BILD]", "|").Split('|')[1]);
        int gameIndex = Int32.Parse(data.Replace("[GAME]", "|").Split('|')[1]);
        Config.MOSAIK_SPIEL.setSelected(Config.MOSAIK_SPIEL.getMosaik(gameIndex));

        if (einblenden == true)
        {
            if (index == 0)
            {
                Bild[0].transform.parent.gameObject.GetComponent<Image>().sprite = Config.MOSAIK_SPIEL.getBeispiel();
                Bild[0].transform.parent.gameObject.SetActive(true);
            }
            else
            {
                Bild[0].transform.parent.gameObject.GetComponent<Image>().sprite = Config.MOSAIK_SPIEL.getSelected().getSprites()[index-1];
                Bild[0].transform.parent.gameObject.SetActive(true);
            }

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
        else
        {
            Bild[0].transform.parent.gameObject.SetActive(false);
        }
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
}