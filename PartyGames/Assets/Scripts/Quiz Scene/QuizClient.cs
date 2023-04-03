using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class QuizClient : MonoBehaviour
{
    GameObject Frage;
    GameObject SpielerAntwortEingabe;
    GameObject[,] SpielerAnzeige;
    GameObject[] SchaetzfragenAnzeige;
    [SerializeField] GameObject SchaetzfragenAnimationController;
    bool pressingbuzzer = false;

    [SerializeField] AudioSource BuzzerSound;
    [SerializeField] AudioSource RichtigeAntwortSound;
    [SerializeField] AudioSource FalscheAntwortSound;

    void OnEnable()
    {
        InitAnzeigen();

        if (!Config.CLIENT_STARTED)
            return;
        SendToServer("#JoinQuiz");

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
                SceneManager.LoadSceneAsync("Startup");
                break;
            case "#UpdateRemoteConfig":
                LoadConfigs.FetchRemoteConfig();
                break;
            case "#ZurueckInsHauptmenue":
                SceneManager.LoadSceneAsync("Startup");
                break;
            #endregion

            case "#UpdateSpieler":
                UpdateSpieler(data);
                break;
            case "#SpielerAusgetabt":
                SpielerAusgetabt(data);
                break;
            case "#FragenAnzeige":
                FragenAnzeige(data);
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

            case "#AnimationInfo":
                AnimationAnzeigen(data);
                break;
            case "#AnimationStart":
                AnimationStarten();
                break;
            case "#AnimationBeenden":
                AnimationBeenden();
                break;
            case "#AnimationZiel":
                AnimationZiel(data);
                break;
        } 
    }

    /**
     * Initialisiert die Anzeigen der Scene
     */
    private void InitAnzeigen()
    {
        // Fragen Anzeige
        Frage = GameObject.Find("Frage");
        Frage.SetActive(false);
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
            catch (Exception e)
            {
            }
            SpielerAnzeige[i, 0].SetActive(false);
            SpielerAnzeige[i, 1].SetActive(false);
            SpielerAnzeige[i, 3].SetActive(false);
            SpielerAnzeige[i, 6].SetActive(false);
        }

        // Schätzfragen
        //if (GameObject.Find("SchaetzfragenAnimation") != null)
        //GameObject.Find("SchaetzfragenAnimation").SetActive(false);
        SchaetzfragenAnzeige = new GameObject[20];
        SchaetzfragenAnzeige[0] = GameObject.Find("SchaetzfragenAnimation/Grid");
        SchaetzfragenAnzeige[1] = GameObject.Find("SchaetzfragenAnimation/Grid/MinGrenze");
        SchaetzfragenAnzeige[2] = GameObject.Find("SchaetzfragenAnimation/Grid/ZielGrenze");
        SchaetzfragenAnzeige[3] = GameObject.Find("SchaetzfragenAnimation/Grid/MaxGrenze");

        SchaetzfragenAnzeige[4] = GameObject.Find("SchaetzfragenAnimation/Grid/Icon (1)");
        SchaetzfragenAnzeige[5] = GameObject.Find("SchaetzfragenAnimation/Grid/Icon (1)/Data");
        SchaetzfragenAnzeige[5].SetActive(false);
        SchaetzfragenAnzeige[6] = GameObject.Find("SchaetzfragenAnimation/Grid/Icon (2)");
        SchaetzfragenAnzeige[7] = GameObject.Find("SchaetzfragenAnimation/Grid/Icon (2)/Data");
        SchaetzfragenAnzeige[7].SetActive(false);
        SchaetzfragenAnzeige[8] = GameObject.Find("SchaetzfragenAnimation/Grid/Icon (3)");
        SchaetzfragenAnzeige[9] = GameObject.Find("SchaetzfragenAnimation/Grid/Icon (3)/Data");
        SchaetzfragenAnzeige[9].SetActive(false);
        SchaetzfragenAnzeige[10] = GameObject.Find("SchaetzfragenAnimation/Grid/Icon (4)");
        SchaetzfragenAnzeige[11] = GameObject.Find("SchaetzfragenAnimation/Grid/Icon (4)/Data");
        SchaetzfragenAnzeige[11].SetActive(false);
        SchaetzfragenAnzeige[12] = GameObject.Find("SchaetzfragenAnimation/Grid/Icon (5)");
        SchaetzfragenAnzeige[13] = GameObject.Find("SchaetzfragenAnimation/Grid/Icon (5)/Data");
        SchaetzfragenAnzeige[13].SetActive(false);
        SchaetzfragenAnzeige[14] = GameObject.Find("SchaetzfragenAnimation/Grid/Icon (6)");
        SchaetzfragenAnzeige[15] = GameObject.Find("SchaetzfragenAnimation/Grid/Icon (6)/Data");
        SchaetzfragenAnzeige[15].SetActive(false);
        SchaetzfragenAnzeige[16] = GameObject.Find("SchaetzfragenAnimation/Grid/Icon (7)");
        SchaetzfragenAnzeige[17] = GameObject.Find("SchaetzfragenAnimation/Grid/Icon (7)/Data");
        SchaetzfragenAnzeige[17].SetActive(false);
        SchaetzfragenAnzeige[18] = GameObject.Find("SchaetzfragenAnimation/Grid/Icon (8)");
        SchaetzfragenAnzeige[19] = GameObject.Find("SchaetzfragenAnimation/Grid/Icon (8)/Data");
        SchaetzfragenAnzeige[19].SetActive(false);
        SchaetzfragenAnzeige[0].SetActive(false);
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
     * Zeigt die gestellte Frage an
     */
    private void FragenAnzeige(string data)
    {
        bool anzeigen = Boolean.Parse(data.Replace("[BOOL]", "|").Split('|')[1]);
        if (anzeigen)
        {
            Frage.SetActive(true);
            Frage.GetComponentInChildren<TMP_Text>().text = data.Replace("[FRAGE]", "|").Split('|')[1];
        }
        else
        {
            Frage.SetActive(false);
            Frage.GetComponentInChildren<TMP_Text>().text = "";
        }
    }
    /**
     * Zeigt das Texteingabefeld an
     */
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
    /**
     * Zeigt die Textantworten aller Spieler an
     */
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
    /**
     * Sendet die Antworteingabe an den Server
     */
    public void SpielerAntwortEingabeInput(TMP_InputField input)
    {
        SendToServer("#SpielerAntwortEingabe "+input.text);
    }

    #region SchaetzAnimation
    public void AnimationZiel(string data)
    {
        SchaetzfragenAnzeige[2].SetActive(bool.Parse(data));
    }
    public void AnimationAnzeigen(string data)
    {
        SchaetzfragenAnzeige[0].SetActive(true);
        SchaetzfragenAnimationController.SetActive(false);
        float startwert = float.Parse(data.Replace("[START_WERT]", "|").Split('|')[1]);
        float zielwert = float.Parse(data.Replace("[ZIEL_WERT]", "|").Split('|')[1]);
        float maxwert = float.Parse(data.Replace("[MAX_WERT]", "|").Split('|')[1]);
        float startpos = float.Parse(data.Replace("[START_POSITION]", "|").Split('|')[1]);
        float endpos = float.Parse(data.Replace("[MAX_POSITION]", "|").Split('|')[1]);
        float difftonull = float.Parse(data.Replace("[DIFF_NULL]", "|").Split('|')[1]);
        float diffmax = float.Parse(data.Replace("[DIFF_MAX]", "|").Split('|')[1]);
        float werttomax = float.Parse(data.Replace("[DISTANCE_PER_MOVE]", "|").Split('|')[1]);
        string einheit = data.Replace("[EINHEIT]", "|").Split('|')[1];
        int stellen = Int32.Parse(data.Replace("[KOMMASTELLEN]", "|").Split('|')[1]);

        // Ziel Bewegen
        SchaetzfragenAnzeige[2].transform.localPosition = new Vector3(werttomax * (zielwert - startwert) - difftonull, SchaetzfragenAnzeige[2].transform.localPosition.y, 0);
        
        // Versteckt alte Schätzungen
        foreach (Player p in Config.PLAYERLIST)
        {
            if (!p.name.Equals(""))
            {
                SchaetzfragenAnzeige[(4 + 2 * (p.id - 1))].transform.GetChild(1).GetComponent<TMP_Text>().text = "";
                SchaetzfragenAnzeige[(4 + 2 * (p.id - 1))].transform.GetChild(3).gameObject.SetActive(false);
                SchaetzfragenAnzeige[(4 + 2 * (p.id - 1))].GetComponent<Image>().sprite = p.icon;
            }
        }


        // SpielerData berechnen
        string data_text = "";
        data_text += "[START_WERT]" + startwert + "[START_WERT]";
        data_text += "[ZIEL_WERT]" + zielwert + "[ZIEL_WERT]";
        data_text += "[MAX_WERT]" + maxwert + "[MAX_WERT]";
        data_text += "[START_POSITION]" + startpos + "[START_POSITION]";
        data_text += "[MAX_POSITION]" + endpos + "[MAX_POSITION]";
        data_text += "[DIFF_NULL]" + difftonull + "[DIFF_NULL]";
        data_text += "[DIFF_MAX]" + diffmax + "[DIFF_MAX]";
        data_text += "[DISTANCE_PER_MOVE]" + werttomax+ "[DISTANCE_PER_MOVE]";
        data_text += "[EINHEIT]" + einheit+ "[EINHEIT]";
        data_text += "[KOMMASTELLEN]" + stellen + "[KOMMASTELLEN]";
        data_text += "[SPIELER_WERT]0[SPIELER_WERT]";
        float spielerwert = 0;

        // Spieler
        foreach (Player p in Config.PLAYERLIST)
        {
            SchaetzfragenAnzeige[(4 + 2 * (p.id - 1))].transform.localPosition = new Vector3(startpos, SchaetzfragenAnzeige[(4 + 2 * (p.id - 1))].transform.localPosition.y, 0);
            if (!p.name.Equals(""))
            {
                spielerwert = float.Parse(data.Replace("["+p.id+"]", "|").Split('|')[1]);
                data_text = data_text.Replace("[SPIELER_WERT]", "|").Split('|')[0] + "[SPIELER_WERT]" + spielerwert + "[SPIELER_WERT]";
                SchaetzfragenAnzeige[(5 + 2 * (p.id - 1))].GetComponent<TMP_Text>().text = data_text;
            }
            else
            {
                // Hide disconnected
                SchaetzfragenAnzeige[(4 + 2 * (p.id - 1))].SetActive(false);
            }
        }
    }

    public void AnimationStarten()
    {
        SchaetzfragenAnimationController.SetActive(true);
    }
    public void AnimationBeenden()
    {
        SchaetzfragenAnimationController.SetActive(false);
        SchaetzfragenAnzeige[0].SetActive(false);
    }
    #endregion

}