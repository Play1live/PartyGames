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
    [SerializeField] AudioSource DisconnectSound;

    void OnEnable()
    {
        InitAnzeigen();

        if (!Config.CLIENT_STARTED)
            return;
        ClientUtils.SendToServer("#JoinQuiz");

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
        Logging.log(Logging.LogType.Normal, "QuizClient", "OnApplicationQuit", "Client wird geschlossen.");
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
        Logging.log(Logging.LogType.Debug, "QuizClient", "TestConnectionToServer", "Testet die Verbindumg zum Server.");
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
        Logging.log(Logging.LogType.Debug, "QuizClient", "Commands", "Eingehende Nachricht: " + cmd + " -> " + data);
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "QuizClient", "Commands", "Unkown Command: " + cmd + " -> " + data);
                break;

            #region Universal Commands
            case "#ServerClosed":
                Logging.log(Logging.LogType.Normal, "QuizClient", "Commands", "Verbindumg zum Server wurde beendet. Lade ins Hauptmenü.");
                CloseSocket();
                SceneManager.LoadSceneAsync("Startup");
                break;
            case "#UpdateRemoteConfig":
                Logging.log(Logging.LogType.Normal, "QuizClient", "Commands", "RemoteConfig wird neugeladen");
                LoadConfigs.FetchRemoteConfig();
                break;
            case "#ZurueckInsHauptmenue":
                Logging.log(Logging.LogType.Normal, "QuizClient", "Commands", "Spiel wird beendet. Lade ins Hauptmenü");
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
                AudioRichtigeAntwort(data);
                break;
            case "#AudioFalscheAntwort":
                AudioFalscheAntwort(data);
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
    /// <summary>
    /// Initialisiert die Anzeigen der Scene
    /// </summary>
    private void InitAnzeigen()
    {
        Logging.log(Logging.LogType.Debug, "QuizClient", "InitAnzeigen", "Anzeigen werden initialisiert.");
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
            catch {}
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
    /// <summary>
    /// Aktualisiert die Spieler Anzeigen
    /// </summary>
    /// <param name="data">#UpdateSpieler ...</param>
    private void UpdateSpieler(string data)
    {
        Logging.log(Logging.LogType.Debug, "QuizClient", "UpdateSpieler", data);
        string[] player = data.Replace("[TRENNER]", "|").Split('|');
        foreach (string sp in player)
        {
            int pId = Int32.Parse(sp.Replace("[ID]", "|").Split('|')[1]);
            // Display ServerInfos
            if (pId == 0)
            {}
            // Display Client Infos
            else
            {
                int pos = Player.getPosInLists(pId);
                // Update PlayerInfos
                //Config.PLAYERLIST[pos].name = sp.Replace("[NAME]", "|").Split('|')[1];
                Config.PLAYERLIST[pos].points = Int32.Parse(sp.Replace("[PUNKTE]", "|").Split('|')[1]);
                //Config.PLAYERLIST[pos].icon = Resources.Load<Sprite>("Images/ProfileIcons/" + sp.Replace("[ICON]", "|").Split('|')[1]);
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
        Logging.log(Logging.LogType.Debug, "QuizClient", "BuzzerFreigeben", "Buzzer wurde freigegeben");
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
    /// <param name="data">Spielerid</param>
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
    /// <param name="data">Spielerid</param>
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
    /// Zeigt die gestellte Frage an
    /// </summary>
    /// <param name="data">Frage & bool</param>
    private void FragenAnzeige(string data)
    {
        bool anzeigen = Boolean.Parse(data.Replace("[BOOL]", "|").Split('|')[1]);
        if (anzeigen)
        {
            Frage.SetActive(true);
            Frage.GetComponentInChildren<TMP_Text>().text = data.Replace("[FRAGE]", "|").Split('|')[1].Replace("\\n", "\n");
        }
        else
        {
            Frage.SetActive(false);
            Frage.GetComponentInChildren<TMP_Text>().text = "";
        }
    }
    /// <summary>
    /// Zeigt das Texteingabefeld an
    /// </summary>
    /// <param name="data">bool</param>
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
    /// <param name="data">Textantworten der SPieler</param>
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
    /// <param name="input">Texteingabefeld</param>
    public void SpielerAntwortEingabeInput(TMP_InputField input)
    {
        ClientUtils.SendToServer("#SpielerAntwortEingabe "+input.text);
    }
    #region SchaetzAnimation
    /// <summary>
    /// Zeigt das Ziel der Schätzfrage an
    /// </summary>
    /// <param name="data"></param>
    private void AnimationZiel(string data)
    {
        SchaetzfragenAnzeige[2].SetActive(bool.Parse(data));
    }
    /// <summary>
    /// Zeigt die Schätzfragenanimation an
    /// </summary>
    /// <param name="data"></param>
    private void AnimationAnzeigen(string data)
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
                SchaetzfragenAnzeige[(4 + 2 * (p.id - 1))].GetComponent<Image>().sprite = p.icon2.icon;
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
        double spielerwert = 0;

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
    /// <summary>
    /// Startet die Schätzfragenanimation
    /// </summary>
    private void AnimationStarten()
    {
        SchaetzfragenAnimationController.SetActive(true);
    }
    /// <summary>
    /// Beendet die Schätzfragenanimation
    /// </summary>
    private void AnimationBeenden()
    {
        SchaetzfragenAnimationController.SetActive(false);
        SchaetzfragenAnzeige[0].SetActive(false);
    }
    #endregion
}