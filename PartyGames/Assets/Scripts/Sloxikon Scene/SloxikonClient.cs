using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SloxikonClient : MonoBehaviour
{
    GameObject SpielerAntwortEingabe;
    GameObject[,] SpielerAnzeige;
    bool pressingbuzzer = false;

    [SerializeField] AudioSource BuzzerSound;
    [SerializeField] AudioSource RichtigeAntwortSound;
    [SerializeField] AudioSource FalscheAntwortSound;

    void OnEnable()
    {
        if (!Config.CLIENT_STARTED)
            return;

        InitAnzeigen();
        SendToServer("#JoinSloxikon");

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
        SendToServer("#ClientFocusChange " + focus);
    }

    private void OnApplicationQuit()
    {
        Logging.log(Logging.LogType.Normal, "SloxikonClient", "OnApplicationQuit", "Client wird geschlossen.");
        SendToServer("#ClientClosed");
        CloseSocket();
    }

    /// <summary>
    /// Testet die Verbindung zum Server
    /// </summary>
    /// <returns></returns>
    IEnumerator TestConnectionToServer()
    {
        while (Config.CLIENT_STARTED)
        {
            SendToServer("#TestConnection");
            yield return new WaitForSeconds(10);
        }
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

        Logging.log(Logging.LogType.Normal, "SloxikonClient", "CloseSocket", "Verbindung zum Server wurde getrennt. Client wird in das Hauptmenü geladen.");
    }
    #endregion
    #region Kommunikation
    /// <summary>
    /// Sendet eine Nachricht an den Server.
    /// </summary>
    /// <param name="data"></param>
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
            Logging.log(Logging.LogType.Warning, "SloxikonClient", "SendToServer", "Nachricht an Server konnte nicht gesendet werden.", e);
            Config.HAUPTMENUE_FEHLERMELDUNG = "Verbindung zum Server wurde verloren.";
            CloseSocket();
            SceneManager.LoadSceneAsync("StartUp");
        }
    }
    /// <summary>
    /// Einkommende Nachrichten die vom Sever
    /// </summary>
    /// <param name="data"></param>
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
    /// <summary>
    /// Eingehende Commands vom Server
    /// </summary>
    /// <param name="data"></param>
    /// <param name="cmd"></param>
    public void Commands(string data, string cmd)
    {
        Logging.log(Logging.LogType.Debug, "SloxikonClient", "Commands", "Eingehende Nachricht: " + cmd + " -> " + data);
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "SloxikonClient", "Commands", "Unkown Command: " + cmd + " -> " + data);
                break;

            #region Universal Commands
            case "#ServerClosed":
                Logging.log(Logging.LogType.Normal, "SloxikonClient", "Commands", "Verbindung zum Server wurde getrennt");
                CloseSocket();
                SceneManager.LoadSceneAsync("Startup");
                break;
            case "#UpdateRemoteConfig":
                Logging.log(Logging.LogType.Normal, "SloxikonClient", "Commands", "RemoteConfig wird aktualisiert");
                LoadConfigs.FetchRemoteConfig();
                break;
            case "#ZurueckInsHauptmenue":
                Logging.log(Logging.LogType.Debug, "SloxikonClient", "Commands", "Spiel wurde beendet, lade ins Hauptmenü.");
                SceneManager.LoadSceneAsync("Startup");
                break;
            #endregion

            case "#UpdateSpieler":
                UpdateSpieler(data);
                break;
            case "#SpielerAusgetabt":
                SpielerAusgetabt(data);
                break;
            case "#TexteingabeAnzeigen":
                TexteingabeAnzeigen(data);
                break;
            case "#TextantwortenAnzeigen":
                TextantwortenAnzeigen(data);
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
        } 
    }
    /// <summary>
    /// Initialisiert die Anzeigen der Scene
    /// </summary>
    private void InitAnzeigen()
    {
        Logging.log(Logging.LogType.Debug, "SloxikonClient", "InitAnzeigen", "Initialisiert die Anzeigen");
        // Spieler Texteingabe
        SpielerAntwortEingabe = GameObject.Find("SpielerAntwortEingabe");
        SpielerAntwortEingabe.SetActive(false);
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
            SpielerAnzeige[i, 6] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/SpielerAntwort"); // Spieler Antwort

            try
            {
                GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/ServerControl").SetActive(false); // Server Control für Spieler ausblenden
            }
            catch {}
            SpielerAnzeige[i, 0].SetActive(false);
            SpielerAnzeige[i, 1].SetActive(false);
            SpielerAnzeige[i, 3].SetActive(false);
            SpielerAnzeige[i, 6].SetActive(false);
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

            // Display ServerInfos
            if (pId == 0)
            {
            }
            // Display Client Infos
            else
            {
                int pos = Player.getPosInLists(pId);
                // Update PlayerInfos
                //Config.PLAYERLIST[pos].name = sp.Replace("[NAME]", "|").Split('|')[1];
                Config.PLAYERLIST[pos].points = Int32.Parse(sp.Replace("[PUNKTE]", "|").Split('|')[1]);
                //Config.PLAYERLIST[pos].icon = Resources.Load<Sprite>("Images/ProfileIcons/" + sp.Replace("[ICON]", "|").Split('|')[1]);
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
    /// <summary>
    /// Sendet eine Buzzer Anfrage an den Server
    /// </summary>
    public void SpielerBuzzered()
    {
        SendToServer("#SpielerBuzzered");
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
    /// <param name="data"></param>
    private void AudioBuzzerPressed(string data)
    {
        BuzzerSound.Play();
        int pId = Int32.Parse(data);
        SpielerAnzeige[Player.getPosInLists(pId), 1].SetActive(true);
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
    /// Zeigt das Texteingabefeld an
    /// </summary>
    /// <param name="data"></param>
    private void TexteingabeAnzeigen(string data)
    {
        bool anzeigen = Boolean.Parse(data);
        if (anzeigen) {
            SpielerAntwortEingabe.SetActive(true);
            SpielerAntwortEingabe.GetComponentInChildren<TMP_InputField>().text = "";
        }
        else
            SpielerAntwortEingabe.SetActive(false);
    }
    /// <summary>
    /// Zeigt die Textantworten aller Spieler an
    /// </summary>
    /// <param name="data"></param>
    private void TextantwortenAnzeigen(string data)
    {
        bool anzeigen = Boolean.Parse(data.Replace("[BOOL]", "|").Split('|')[1]);
        if (!anzeigen)
        {
            for (int i = 1; i <= Config.SERVER_MAX_CONNECTIONS; i++)
            {
                SpielerAnzeige[Player.getPosInLists(i), 6].SetActive(false);
            }
            return;
        }
        string text = data.Replace("[TEXT]", "|").Split('|')[1];
        for (int i = 1; i <= Config.SERVER_MAX_CONNECTIONS; i++)
        {
            SpielerAnzeige[Player.getPosInLists(i), 6].SetActive(true);
            SpielerAnzeige[Player.getPosInLists(i), 6].GetComponentInChildren<TMP_InputField>().text = text.Replace("[ID"+i+"]","|").Split('|')[1];
        }
    }
    /// <summary>
    /// Sendet die Antworteingabe an den Server
    /// </summary>
    /// <param name="input"></param>
    public void SpielerAntwortEingabeInput(TMP_InputField input)
    {
        SendToServer("#SpielerAntwortEingabe "+input.text);
    }
}