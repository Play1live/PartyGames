using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FlaggenClient : MonoBehaviour
{
    GameObject FlaggenOutline;
    GameObject FlaggenImage;
    GameObject Antwort;

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
        ClientUtils.SendToServer("#JoinFlaggen");

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
        Logging.log(Logging.LogType.Normal, "FlaggenClient", "OnApplicationQuit", "Client wird geschlossen.");
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

        Logging.log(Logging.LogType.Normal, "FlaggenClient", "CloseSocket", "Verbindung zum Server wurde getrennt. Client wird in das Hauptmenü geladen.");
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
        Logging.log(Logging.LogType.Debug, "FlaggenClient", "Commands", "Eingehende Nachricht: " + cmd + " -> " + data);
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "FlaggenClient", "Commands", "Unkown Command: " + cmd + " -> " + data);
                break;

            #region Universal Commands
            case "#ServerClosed":
                Logging.log(Logging.LogType.Normal, "FlaggenClient", "Commands", "Verbindung zum Server wurde getrennt. Lade ins Hauptmenü.");
                CloseSocket();
                SceneManager.LoadScene("Startup");
                break;
            case "#UpdateRemoteConfig":
                Logging.log(Logging.LogType.Warning, "FlaggenClient", "Commands", "Aktualisiere RemoteConfig");
                LoadConfigs.FetchRemoteConfig();
                break;
            case "#ZurueckInsHauptmenue":
                Logging.log(Logging.LogType.Warning, "FlaggenClient", "Commands", "Spiel wurde beendet, lade ins Hauptmenü.");
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
                AudioRichtigeAntwort(data);
                break;
            case "#AudioFalscheAntwort":
                AudioFalscheAntwort(data);
                break;
            case "#BuzzerFreigeben":
                BuzzerFreigeben();
                break;
            #endregion

            case "#FlaggenSpielAnzeige":
                FlaggenAnzeige(data);
                break;
            case "#FlaggenSpielShowAntwort":
                FlaggenShowAntwort(data);
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
    private void SpielerBuzzered()
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
    /// Deaktiviert die Spieler ist dran Anzeige
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
    private void AudioRichtigeAntwort(string data)
    {
        RichtigeAntwortSound.Play();
        int pIndex = Player.getPosInLists(Int32.Parse(data.Split('*')[0]));
        Config.PLAYERLIST[pIndex].points += Int32.Parse(data.Split('*')[1]);
        SpielerAnzeige[pIndex, 5].GetComponent<TMP_Text>().text = Config.PLAYERLIST[pIndex].points + "";
    }
    /// <summary>
    /// Spielt den Sound für eine falsche Antwort ab
    /// </summary>
    private void AudioFalscheAntwort(string data)
    {
        FalscheAntwortSound.Play();
        int pId = Int32.Parse(data.Split('*')[0]);
        int pPunkte = Int32.Parse(data.Split('*')[1]);
        foreach (Player p in Config.PLAYERLIST)
        {
            if (pId != p.id)
            {
                p.points += pPunkte;
                SpielerAnzeige[Player.getPosInLists(p.id), 5].GetComponent<TMP_Text>().text = p.points + "";
            }
        }
        Config.SERVER_PLAYER.points += pPunkte;
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
        ClientUtils.SendToServer("#SpielerAntwortEingabe "+input.text);
    }
    /// <summary>
    /// Initialisiert die Anzeigen der Scene
    /// </summary>
    private void InitAnzeigen()
    {
        Logging.log(Logging.LogType.Debug, "FlaggenClient", "InitAnzeigen", "Anzeigen werden initialisiert.");
        // Anzeigen für alle
        FlaggenOutline = GameObject.Find("FlaggenAnzeigen/FlaggenImageOutline");
        FlaggenOutline.SetActive(false);
        FlaggenImage = GameObject.Find("FlaggenAnzeigen/FlaggenImage");
        FlaggenImage.SetActive(false);
        Antwort = GameObject.Find("FlaggenAnzeigen/Antwort");
        Antwort.SetActive(false);
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
    /// Zeigt die Flagge an
    /// </summary>
    /// <param name="data"></param>
    private void FlaggenAnzeige(string data)
    {
        FlaggenOutline.SetActive(true); // Blendet Outline ein
        FlaggenImage.SetActive(true); // Blendet Flagge ein
        if (data.Equals("#Fragezeichen"))
        {
            FlaggenImage.GetComponent<Image>().sprite = Resources.Load<Sprite>("Spiele/Flaggen/#Fragezeichen");
        }
        else
        {
            FlaggenImage.GetComponent<Image>().sprite = Resources.Load<Sprite>("Spiele/Flaggen/" + data + "_Flagge");
        }
        Antwort.SetActive(false); // Blendet Antwort aus
    }
    /// <summary>
    /// Zeigt die gesuchte Antwort an
    /// </summary>
    /// <param name="data"></param>
    private void FlaggenShowAntwort(string data)
    {
        Antwort.SetActive(true);
        Antwort.GetComponent<TMP_Text>().text = data;
    }
}