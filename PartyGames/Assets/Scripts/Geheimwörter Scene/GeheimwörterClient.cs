using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GeheimwörterClient : MonoBehaviour
{
    TMP_Text WoerterAnzeige;
    TMP_Text KategorieAnzeige;
    TMP_Text LoesungsWortAnzeige;
    GameObject[] Liste;

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
        SendToServer("#JoinGeheimwörter");

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
            case "#TexteingabeAnzeigen":
                TexteingabeAnzeigen(data);
                break;
            case "#TextantwortenAnzeigen":
                TextantwortenAnzeigen(data);
                break;

            #endregion
            case "#GeheimwoerterHide":
                GeheimwoerterHide();
                break;
            case "#GeheimwoerterNeue":
                GeheimwoerterNeue();
                break;
            case "#GeheimwoerterCode":
                GeheimwoerterCode(data);
                break;
            case "#GeheimwoerterWorteZeigen":
                GeheimwoerterWorteZeigen(data);
                break;
            case "#GeheimwoerterLoesung":
                GeheimwoerterLoesung(data);
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
     * Sendet die Antworteingabe an den Server
     */
    public void SpielerAntwortEingabeInput(TMP_InputField input)
    {
        SendToServer("#SpielerAntwortEingabe " + input.text);
    }
    /**
     * Zeigt das Texteingabefeld an
     */
    private void TexteingabeAnzeigen(string data)
    {
        bool anzeigen = Boolean.Parse(data);
        if (anzeigen)
        {
            SpielerAntwortEingabe.SetActive(true);
            SpielerAntwortEingabe.GetComponentInChildren<TMP_InputField>().text = "";
        }
        else
            SpielerAntwortEingabe.SetActive(false);
    }
    /**
     * Zeigt die Textantworten aller Spieler an
     */
    private void TextantwortenAnzeigen(string data)
    {
        bool boolean = bool.Parse(data.Replace("[BOOL]", "|").Split('|')[1]);
        if (boolean == false)
        {
            for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
            {
                SpielerAnzeige[i, 7].SetActive(false);
                SpielerAnzeige[i, 7].GetComponentInChildren<TMP_InputField>().text = "";
            }
            return;
        }
        string text = data.Replace("[TEXT]", "|").Split('|')[1];
        for (int i = 1; i <= Config.SERVER_MAX_CONNECTIONS; i++)
        {
            SpielerAnzeige[Player.getPosInLists(i), 7].SetActive(true);
            SpielerAnzeige[Player.getPosInLists(i), 7].GetComponentInChildren<TMP_InputField>().text = text.Replace("[ID" + i + "]", "|").Split('|')[1];
        }
    }
    /**
     * Initialisiert die Anzeigen der Scene
     */
    private void InitAnzeigen()
    {
        WoerterAnzeige = GameObject.Find("GeheimwörterAnzeige/Outline/Woerter").GetComponent<TMP_Text>();
        WoerterAnzeige.text = "";
        KategorieAnzeige = GameObject.Find("GeheimwörterAnzeige/Outline/Kategorien").GetComponent<TMP_Text>();
        KategorieAnzeige.text = "";
        LoesungsWortAnzeige = GameObject.Find("GeheimwörterAnzeige/Outline/Loesungswort/Text").GetComponent<TMP_Text>();
        LoesungsWortAnzeige.text = "";
        LoesungsWortAnzeige.transform.parent.gameObject.SetActive(false);
        Liste = new GameObject[30];
        for (int i = 0; i < 30; i++)
        {
            Liste[i] = GameObject.Find("GeheimwörterAnzeige/Outline/Liste/Element (" + i + ")");
            Liste[i].SetActive(false);
        }
        // Spieler Texteingabe
        SpielerAntwortEingabe = GameObject.Find("SpielerAntwortEingabe");
        SpielerAntwortEingabe.SetActive(false);
        // Spieler Anzeige
        SpielerAnzeige = new GameObject[Config.SERVER_MAX_CONNECTIONS, 8]; // Anzahl benötigter Elemente
        for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
        {
            SpielerAnzeige[i, 0] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")"); // Spieler Anzeige
            SpielerAnzeige[i, 1] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/BuzzerPressed"); // BuzzerPressed Umrandung
            SpielerAnzeige[i, 2] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Icon"); // Spieler Icon
            SpielerAnzeige[i, 3] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Ausgetabt"); // Ausgetabt Einblednung
            SpielerAnzeige[i, 4] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Infobar/Name"); // Spieler Name
            SpielerAnzeige[i, 5] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/Infobar/Punkte"); // Spieler Punkte
            SpielerAnzeige[i, 6] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/ServerControl"); // Server Settings
            SpielerAnzeige[i, 7] = GameObject.Find("SpielerAnzeige/Player (" + (i + 1) + ")/SpielerAntwort"); // Server Settings

            SpielerAnzeige[i, 0].SetActive(false); // Spieler Anzeige
            SpielerAnzeige[i, 1].SetActive(false); // BuzzerPressed Umrandung
            SpielerAnzeige[i, 3].SetActive(false); // Ausgetabt Einblendung
            SpielerAnzeige[i, 6].SetActive(false); // Server Settings
            SpielerAnzeige[i, 7].SetActive(false);
        }
    } 
    
    private void GeheimwoerterHide()
    {
        WoerterAnzeige.text = "";
        KategorieAnzeige.text = "";
        LoesungsWortAnzeige.text = "";
        LoesungsWortAnzeige.transform.parent.gameObject.SetActive(false);
        for (int i = 0; i < 30; i++)
        {
            Liste[i].SetActive(false);
        }
    }
    private void GeheimwoerterNeue()
    {
        WoerterAnzeige.text = "";
        KategorieAnzeige.text = "";
        LoesungsWortAnzeige.text = "";
        LoesungsWortAnzeige.transform.parent.gameObject.SetActive(false);
    }
    private void GeheimwoerterCode(string data)
    {
        string[] code = data.Replace("<#>", "|").Split('|');
        for (int i = 0; i < code.Length; i++)
        {
            Liste[i].transform.GetChild(0).gameObject.GetComponent<TMP_Text>().text = code[i].Split('=')[1];
            Liste[i].transform.GetChild(1).gameObject.GetComponent<TMP_Text>().text = code[i].Split('=')[0];
            Liste[i].SetActive(true);
        }
    }
    private void GeheimwoerterWorteZeigen(string data)
    {
        WoerterAnzeige.text = data.Replace("<>", "\n");
        KategorieAnzeige.text = "";
    }
    private void GeheimwoerterLoesung(string data)
    {
        LoesungsWortAnzeige.text = data.Replace("[TRENNER]", "|").Split('|')[0];
        LoesungsWortAnzeige.transform.parent.gameObject.SetActive(true);
        KategorieAnzeige.text = data.Replace("[TRENNER]", "|").Split('|')[1].Replace("<>", "\n");
    }


}