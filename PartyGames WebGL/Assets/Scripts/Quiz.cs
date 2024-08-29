using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Toggle = UnityEngine.UI.Toggle;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;

public class Quiz : MonoBehaviour
{
    private bool lockcmds;
    public TMP_InputField SpielerantwortEingabe;
    public Button Buzzer;
    public GameObject SpieleranzeigeListe;
    public List<QuizPlayer> quizplayer;


    public GameObject moderator_menue;

    [SerializeField] AudioSource ConnectSound;
    [SerializeField] AudioSource DisconnectSound;
    [SerializeField] AudioSource GameStartSound;
    [SerializeField] AudioSource ErratenSound;
    [SerializeField] AudioSource FalschSound;
    [SerializeField] AudioSource SpielerIstDran;
    [SerializeField] AudioSource Beeep;
    [SerializeField] AudioSource Moeoop;

    // Start is called before the first frame update
    void Start()
    {
        Utils.Log(LogType.Info, "Starting Lobby", true);
        lockcmds = false;
        moderator_menue.SetActive(false);
        if (Config.spieler.isModerator)
            moderator_menue.SetActive(true);
        StartCoroutine(SendPingUpdate());
        GameStartSound.Play();
        ClientUtils.SendMessage("Quiz", "GetUpdate", "");

        InitScene();
    }

    // Update is called once per frame
    void Update()
    {
        // Verarbeite alle Nachrichten, die seit dem letzten Frame empfangen wurden
        while (Config.msg_queue.Count > 0)
        {
            if (lockcmds)
                return;
            string message = null;
            lock (Config.msg_queue)
            {
                message = Config.msg_queue.Dequeue();
            }
            Utils.Log(LogType.Trace, message);
            OnCommand(message);
        }
    }
    private void OnDisable()
    {
        StopAllCoroutines();
    }

    public void OnCommand(string message)
    {
        if (message.Split('|').Length < 3)
            return;
        string gametitle = message.Split('|')[0];
        string cmd = message.Split('|')[1];
        string data = message.Split('|')[2];
        if (!gametitle.Equals(this.GetType().Name) && !gameObject.Equals("ALLE"))
            Utils.Log(LogType.Warning, "Befehl kann in dieser Klasse nicht ausgeführt werden: " + message);

        switch (cmd)
        {
            default: Utils.Log(LogType.Warning, "Unbekannter Befehl: " + cmd + " " + data); return;
            case "Pong": break;
            case "SpielVerlassen": lockcmds = true; SceneManager.LoadScene("Lobby"); break;
            case "ClientSetModerator": Config.spieler.isModerator = true; if (Config.spieler.isModerator) moderator_menue.SetActive(true); break;
            case "UnknownPlayerSetData": UnknownPlayerSet(data); break;
            case "SpielerUpdate": UpdateSpieler(data); break;
        }
    }
    private IEnumerator SendPingUpdate()
    {
        while (true)
        {
            ClientUtils.SendMessage("ALLE", "Ping", "");
            yield return new WaitForSeconds(new System.Random().Next(10, 15));
        }
    }
    private void UnknownPlayerSet(string data_s)
    { // listpos, uuid, name, iconid
        string[] data = data_s.Split('*');
        Config.players.Insert(int.Parse(data[0]),
            new Player(Guid.Parse(data[1]), data[2], int.Parse(data[3])));
        ClientUtils.SendMessage("Quiz", "GetSpielerUpdate", "");
    }
    private void InitScene()
    {
        UpdateModeratorView();
        ToggleBuzzer(false);
        ToggleSpielerantwortEingabe(false);
        if (Config.spieler.isModerator)
        {
            // TODO iwi mit bools lösen
            GameObject.Find("Moderator/BuzzerAktivierenToggle").GetComponent<Toggle>().isOn = false;
            GameObject.Find("Moderator/AusgetabbtAnzeigen").GetComponent<Toggle>().isOn = false;
        }
        for (int i = 0; i < SpieleranzeigeListe.transform.childCount; i++)
        {
            SpieleranzeigeListe.transform.GetChild(i).gameObject.SetActive(false);
        }
    }
    private void UpdateSpieler(string data_s)
    {
        string[] data = data_s.Split("[TRENNER]");
        if (quizplayer == null)
        {
            for (int i = 0; i < data.Length; i++)
            {
                quizplayer = new List<QuizPlayer>();
                Player p = Player.getPlayerById(Guid.Parse(data[i].Split("[ID]")[1]));
                QuizPlayer qp_ = new QuizPlayer(SpieleranzeigeListe.transform.GetChild(i).gameObject, p);
                quizplayer.Add(qp_);
                qp_.SetPoints(int.Parse(data[i].Split("[PUNKTE]")[1]));
            }
            return;
        }
        for (int i = 0; i < data.Length; i++)
        {
            QuizPlayer qp = QuizPlayer.GetPlayer(data[i].Split("[ID]")[1], quizplayer);
            if (qp == null)
            {
                for (int j = 0; j < quizplayer.Count; j++)
                {
                    if (quizplayer[j].parent.name.Equals(data[i].Split("[ID]")[1]))
                    {
                        quizplayer[j].parent.name = "Spieler (" + i + ")";
                        quizplayer[j].parent.SetActive(false);
                        quizplayer.RemoveAt(j);
                        break;
                    }
                }
            }
            else 
                qp.SetPoints(int.Parse(data[i].Split("[POINTS]")[1]));
        }
    }
    private void ToggleSpielerantwortEingabe(bool toggle)
    {
        SpielerantwortEingabe.transform.parent.gameObject.SetActive(toggle);
        SpielerantwortEingabe.text = "";
    }
    private void ToggleBuzzer(bool toggle)
    {
        Buzzer.interactable = toggle;
    }

    #region Moderator
    // TODO: wenn mod die settings öffnet muss das auch eine möglichkeit haben das zu schließen
    public void SetBuzzer(Toggle toggle)
    {
        ToggleBuzzer(toggle.isOn);
    }
    public void SetSpielerantwortEingabe(Toggle toggle)
    {
        ToggleSpielerantwortEingabe(toggle.isOn);
    }
    private void UpdateModeratorView()
    {
        if (!Config.spieler.isModerator)
        {
            moderator_menue.SetActive(false);
            return;
        }
        moderator_menue.SetActive(true);
        Buzzer.gameObject.SetActive(false);
        SpielerantwortEingabe.gameObject.SetActive(false);
        SpielerantwortEingabe.transform.parent.gameObject.SetActive(false);


    }
    #endregion
}

public class QuizPlayer
{
    public Player p;
    public GameObject parent;
    public GameObject buzzered;
    public GameObject tabbedout;
    public int points;
    public TMP_Text pointsTXT;
    public TMP_InputField answerTXT;
    
    public QuizPlayer(GameObject parent, Player p)
    {
        this.p = p;
        this.parent = parent;
        this.parent.name = p.uuid.ToString();
        this.parent.SetActive(true);
        this.buzzered = parent.transform.GetChild(0).gameObject;
        ToggleBuzzered(false);
        parent.transform.GetChild(1).GetChild(0).GetComponent<Image>().sprite = p.icon;
        this.tabbedout = parent.transform.GetChild(2).gameObject;
        this.tabbedout.SetActive(false);
        parent.transform.GetChild(3).GetChild(1).GetComponent<TMP_Text>().text = p.name;
        this.pointsTXT = parent.transform.GetChild(3).GetChild(2).GetComponent<TMP_Text>();
        SetPoints(0);
        this.answerTXT = parent.transform.GetChild(4).GetChild(0).GetComponent<TMP_InputField>();
        UpdateModerator();
    }

    public void UpdateModerator()
    {
        this.parent.transform.GetChild(1).GetChild(0).GetComponent<Button>().interactable = Config.spieler.isModerator;
    }
    public void ToggleBuzzered(bool toggle)
    {
        this.buzzered.SetActive(toggle);
    }
    public void ToggleTabbedOut(bool toggle)
    {
        this.tabbedout.SetActive(toggle);
    }
    public void ToggleAnswer(bool toggle)
    {
        this.answerTXT.transform.parent.gameObject.SetActive(toggle);
        this.answerTXT.text = "";
    }
    public string GetAnswer()
    {
        return this.answerTXT.text;
    }
    public void SetPoints(int points)
    {
        this.points = points;
        this.pointsTXT.text = this.points.ToString("N", new CultureInfo("de-DE"));
    }
    public void AddPoints(int points)
    {
        this.points += points;
        this.pointsTXT.text = this.points.ToString("N", new CultureInfo("de-DE"));
    }

    public static QuizPlayer GetPlayer(string guid, List<QuizPlayer> list)
    {
        foreach (QuizPlayer player in list) 
        {
            if (player.p.uuid.Equals(Guid.Parse(guid)))
                return player;
        }
        return null;
    }
} 
