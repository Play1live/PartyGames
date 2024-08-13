using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Tabu : MonoBehaviour
{
    private bool lockcmds;
    private int team_green_points;
    [SerializeField] Transform team_green_grid;
    private int team_blue_points;
    [SerializeField] Transform team_blue_grid;
    [SerializeField] GameObject time;
    [SerializeField] GameObject startround;
    [SerializeField] GameObject correct;
    [SerializeField] GameObject wrong;
    [SerializeField] GameObject skip;
    [SerializeField] Transform karte;
    [SerializeField] GameObject historie;

    private Coroutine timer_coroutine;
    private string erklaerer_name;
    private bool started;
    private string tabu_type;
    private string team_turn;
    [SerializeField] GameObject moderator_menue;
    private int max_skip_int;
    private int round_skip_int;
    [SerializeField] TMP_InputField max_skip;
    private int skip_delay_int;
    [SerializeField] TMP_InputField skip_delay;
    private int timer_sec_int;
    [SerializeField] TMP_InputField timer_sec;

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
        time.SetActive(false);
        startround.SetActive(false);
        correct.SetActive(false);
        wrong.SetActive(false);
        skip.SetActive(false);
        karte.gameObject.SetActive(false);
        //ClearHistory(); // TODO: 
        GameStartSound.Play();
        started = false;
        erklaerer_name = "";
        if (Config.spieler.isModerator)
            ClientUtils.SendMessage("Tabu", "GetGameInfo", "");
        ClientUtils.SendMessage("Tabu", "GetUpdate", "");
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

        switch (cmd)
        {
            default: Utils.Log(LogType.Warning, "Unbekannter Befehl: " + cmd + " " + data); return;
            case "Pong": break;
            case "SetGameInfo": GameObject.Find("Moderator/GameTypeAndPack").GetComponent<TMP_Text>().text = data; break;
            case "SpielVerlassen": lockcmds = true; SceneManager.LoadScene("Lobby"); break;
            case "SpielerUpdate": UpdateSpieler(data); break;
            case "PlayConnectSound": ConnectSound.Play(); break;
            case "PlayDisconnectSound": DisconnectSound.Play(); break;
            case "PlayErratenSound": ErratenSound.Play(); break;
            case "PlayFalschSound": FalschSound.Play(); break;
            case "InitModeratorView": UpdateModeratorView(data); break;
            case "ChangeMaxSkip": max_skip_int = int.Parse(data); max_skip.text = "" + max_skip_int; break;
            case "ChangeSkipDelay": skip_delay_int = int.Parse(data); skip_delay.text = "" + skip_delay_int; break;
            case "ChangeTimerSec": timer_sec_int = int.Parse(data); timer_sec.text = "" + timer_sec_int; break;
            case "UpdatePoints": UpdatePoints(data); break;
            case "AddHistory": AddHistory(data); break;
            case "StartRound": StartRound(data); break;
            case "RoundEnd": RoundEnd(data); break;
        }
    }
    private IEnumerator SendPingUpdate()
    {
        while (true)
        {
            ClientUtils.SendMessage("ALLE", "Ping", "");
            yield return new WaitForSeconds(new System.Random().Next(10, 15));
        }
        yield break;
    }
    private void UpdateSpieler(string data)
    {
        string[] green = data.Split("[GREEN_LIST]")[1].Split('*');
        for (int i = 0; i < green.Length; i++)
        {
            Player p = Player.getPlayerById(Guid.Parse(green[i]));
            team_green_grid.GetChild(2).GetChild(i).gameObject.SetActive(true);
            team_green_grid.GetChild(2).GetChild(i).GetChild(0).GetComponent<Image>().sprite = p.icon;
            team_green_grid.GetChild(2).GetChild(i).GetChild(1).GetComponent<TMP_Text>().text = p.name;
        }
        for (int i = green.Length; i < 10; i++)
            team_green_grid.GetChild(2).GetChild(i).gameObject.SetActive(false);
        team_green_points = int.Parse(data.Split("[GREEN_POINTS]")[1]);
        team_green_grid.GetChild(1).GetChild(1).GetComponent<TMP_Text>().text = "" + team_green_points;

        string[] blue = data.Split("[BLUE_LIST]")[1].Split('*');
        for (int i = 0; i < blue.Length; i++)
        {
            Player p = Player.getPlayerById(Guid.Parse(blue[i]));
            team_blue_grid.GetChild(2).GetChild(i).gameObject.SetActive(true);
            team_blue_grid.GetChild(2).GetChild(i).GetChild(0).GetComponent<Image>().sprite = p.icon;
            team_blue_grid.GetChild(2).GetChild(i).GetChild(1).GetComponent<TMP_Text>().text = p.name;
        }
        for (int i = blue.Length; i < 10; i++)
            team_blue_grid.GetChild(2).GetChild(i).gameObject.SetActive(false);
        team_blue_points = int.Parse(data.Split("[BLUE_POINTS]")[1]);
        team_blue_grid.GetChild(1).GetChild(1).GetComponent<TMP_Text>().text = "" + team_blue_points;

        team_turn = data.Split("[TURN]")[1];

        MarkAlsErklaerer();

        if (!started)
        {
            if (team_turn.Equals("Green") && data.Split("[GREEN_LIST]")[1].Contains(Config.spieler.uuid.ToString()))
            {
                startround.SetActive(true);
            }
            else if (team_turn.Equals("Blue") && data.Split("[BLUE_LIST]")[1].Contains(Config.spieler.uuid.ToString()))
            {
                startround.SetActive(true);
            }
            else
                startround.SetActive(false);
        }        
    }
    #region Moderator
    private void UpdateModeratorView(string data)
    {
        max_skip_int = int.Parse(data.Split('#')[0]);
        skip_delay_int = int.Parse(data.Split('#')[1]);
        timer_sec_int = int.Parse(data.Split('#')[2]);
        if (!Config.spieler.isModerator)
        {
            moderator_menue.SetActive(false);
            return;
        }
        moderator_menue.SetActive(true);

        max_skip.text = "" + max_skip_int;
        skip_delay.text = "" + skip_delay_int;
        timer_sec.text = "" + timer_sec_int;
    }
    public void ModZufaelligeTeams()
    {
        if (started)
            return;
        ClientUtils.SendMessage("Tabu", "RandomTeams", "");
    }
    public void ModResetGame() { ClientUtils.SendMessage("Tabu", "ResetGame", ""); }
    public void ModChangeMaxSkip(TMP_InputField input) { ClientUtils.SendMessage("Tabu", "ChangeMaxSkip", input.text); }
    public void ModChangeSkipDelay(TMP_InputField input) { ClientUtils.SendMessage("Tabu", "ChangeSkipDelay", input.text); }
    public void ModChangeTimerSec(TMP_InputField input) { ClientUtils.SendMessage("Tabu", "ChangeTimerSec", input.text); }
    public void ModChangePointsGreen(int count)
    {
        ClientUtils.SendMessage("Tabu", "ChangePoints", "Green*" + (team_green_points + count));
    }
    public void ModChangePointsGreen(TMP_InputField input)
    {
        ClientUtils.SendMessage("Tabu", "ChangePoints", "Green*" + (team_green_points + int.Parse(input.text)));
        input.text = "";
    }
    public void ModChangePointsBlue(int count) 
    {
        ClientUtils.SendMessage("Tabu", "ChangePoints", "Blue*" + (team_blue_points + count));
    }
    public void ModChangePointsBlue(TMP_InputField input)
    {
        ClientUtils.SendMessage("Tabu", "ChangePoints", "Blue*" + (team_blue_points + int.Parse(input.text)));
        input.text = "";
    }
    public void ModSpielVerlassen()
    {
        ClientUtils.SendMessage("ALLE", "SpielVerlassen", "");
    }
    #endregion
    private void ClearHistory()
    {
        while (historie.transform.parent.childCount > 2)
        {
            GameObject go = historie.transform.parent.GetChild(2).gameObject;
            if (go != historie)
            {
                try
                {
                    Destroy(go);
                }
                catch {}
            }
        }
    }
    private void UpdatePoints(string data)
    {
        team_green_points = int.Parse(data.Split('*')[0]);
        team_green_grid.GetChild(1).GetChild(1).GetComponent<TMP_Text>().text = "" + team_green_points;
        team_blue_points = int.Parse(data.Split('*')[1]);
        team_blue_grid.GetChild(1).GetChild(1).GetComponent<TMP_Text>().text = "" + team_blue_points;
    }
    public void JoinTeam(string team)
    {
        if (started)
            return;
        if (team == "Green")
        {
            ClientUtils.SendMessage("Tabu", "JoinTeam", "Green");
        }
        else if (team == "Blue")
        {
            ClientUtils.SendMessage("Tabu", "JoinTeam", "Blue");
        }
        else
            Utils.Log(LogType.Warning, "Fehlerhaftes Team" + team);
    }
    public void StartAlsErklaerer()
    {
        if (started)
            startround.SetActive(false);
        ClientUtils.SendMessage("Tabu", "StarteAlsErklaerer", "");
    }
    public void ClickRichtig()
    {
        if (!started)
            correct.SetActive(false);
        ClientUtils.SendMessage("Tabu", "ClickRichtig", "");
    }
    public void ClickFalsch()
    {
        if (!started)
            wrong.SetActive(false);
        ClientUtils.SendMessage("Tabu", "ClickFalsch", "");
    }
    public void ClickSkip()
    {
        if (!started)
            skip.SetActive(false);
        if (round_skip_int == -1 || round_skip_int > 0)
        {
            ClientUtils.SendMessage("Tabu", "ClickSkip", "");
            if (round_skip_int > 0)
                round_skip_int--;
            StartCoroutine(SkipWortCoro());
        }
    }
    private IEnumerator SkipWortCoro()
    {
        skip.SetActive(false);
        yield return new WaitForSeconds(skip_delay_int);
        if (erklaerer_name.Equals(Config.spieler.name))
            if (round_skip_int == -1 || round_skip_int > 0)
                skip.SetActive(true);
        yield break;
    }
    private void DisplayKarte(bool show, string erklaerer, string team_turn, string team_green_s, string team_blue_s, string karte_data, bool showAll = false)
    {
        if (!show)
        {
            karte.gameObject.SetActive(show);
            karte.GetChild(0).GetChild(1).GetComponent<TMP_Text>().text = "Leer";
            karte.GetChild(0).GetChild(3).GetComponent<TMP_Text>().text = "Leer";
            return;
        }
        if (showAll)
        {
            karte.GetChild(0).GetChild(1).GetComponent<TMP_Text>().text = karte_data.Split('#')[0];
            karte.GetChild(0).GetChild(3).GetComponent<TMP_Text>().text = karte_data.Split('#')[1].Replace("-", "\n");
            karte.gameObject.SetActive(show);
            StartCoroutine(DisplayKarteEnumerator());
            return;
        }
        karte.gameObject.SetActive(show);

        //Player erkl = Player.getPlayerById(Guid.Parse(erklaerer));
        Player erkl = Player.getPlayerByName(erklaerer);
        string erkl_id = erkl?.uuid.ToString();
        // Team Green
        if (team_green_s.Contains(Config.spieler.uuid.ToString())) 
        {
            // Erklärer ist im Team Green
            if (team_green_s.Contains(erkl_id))
            {
                karte.GetChild(0).GetChild(1).GetComponent<TMP_Text>().text = erklaerer;
                karte.GetChild(0).GetChild(3).GetComponent<TMP_Text>().text = "Du musst das Wort erraten";
            }
            // Erklärer ist im Team Blue
            else
            {
                karte.GetChild(0).GetChild(1).GetComponent<TMP_Text>().text = karte_data.Split('#')[0];
                karte.GetChild(0).GetChild(3).GetComponent<TMP_Text>().text = karte_data.Split('#')[1].Replace("-", "\n");
            }
        }
        // Team Blue
        else if (team_blue_s.Contains(Config.spieler.uuid.ToString()))
        {
            // Erklärer ist im Team Blue
            if (team_blue_s.Contains(erkl_id))
            {
                karte.GetChild(0).GetChild(1).GetComponent<TMP_Text>().text = erklaerer;
                karte.GetChild(0).GetChild(3).GetComponent<TMP_Text>().text = "Du musst das Wort erraten";
            }
            // Erklärer ist im Team Green
            else
            {
                karte.GetChild(0).GetChild(1).GetComponent<TMP_Text>().text = karte_data.Split('#')[0];
                karte.GetChild(0).GetChild(3).GetComponent<TMP_Text>().text = karte_data.Split('#')[1].Replace("-", "\n");
            }
        }
        // Zeige dem Erklärer was er sehen muss
        if (Config.spieler.uuid.ToString() == erkl_id)
        {
            karte.GetChild(0).GetChild(1).GetComponent<TMP_Text>().text = karte_data.Split('#')[0];
            karte.GetChild(0).GetChild(3).GetComponent<TMP_Text>().text = karte_data.Split('#')[1].Replace("-", "\n");
        }
        StartCoroutine(DisplayKarteEnumerator());
    }
    private IEnumerator DisplayKarteEnumerator()
    {
        yield return new WaitForSeconds(0.0001f);
        karte.transform.GetChild(0).GetChild(5).gameObject.SetActive(false);
        yield return new WaitForSeconds(0.0001f);
        karte.transform.GetChild(0).GetChild(5).gameObject.SetActive(true);
    }
    private void AddHistory(string data_s)
    {
        string[] data = data_s.Split('*');
        Player erkl = Player.getPlayerById(Guid.Parse(data[0]));
        string indicator = data[1];
        string karte = data[2];
        GameObject new_item = Instantiate(historie);
        new_item.transform.SetParent(historie.transform.parent, false);
        new_item.name = historie.transform.parent.childCount + "*" + karte.Split('#')[0];
        new_item.transform.GetChild(0).GetComponent<Image>().sprite = erkl.icon;
        new_item.transform.GetChild(1).GetComponent<TMP_Text>().text = karte.Split('#')[0];
        new_item.transform.GetChild(1).GetChild(0).GetComponent<TMP_Text>().text = karte.Split('#')[1].Replace("-","\n");
        if (indicator.Equals("Correct"))
            new_item.transform.GetChild(2).GetChild(1).gameObject.SetActive(true);
        else if (indicator.Equals("Wrong"))
            new_item.transform.GetChild(2).GetChild(2).gameObject.SetActive(true);
        else if (indicator.Equals("Skip"))
            new_item.transform.GetChild(2).GetChild(0).gameObject.SetActive(true);
        new_item.SetActive(true);
        StartCoroutine(HistoryEnumerator(new_item));
    }
    private IEnumerator HistoryEnumerator(GameObject go)
    {
        yield return new WaitForSeconds(0.0001f);
        historie.transform.parent.GetChild(0).gameObject.SetActive(false);
        yield return new WaitForSeconds(0.0001f);
        historie.transform.parent.GetChild(0).gameObject.SetActive(true);
        yield return new WaitForSeconds(0.0001f);
        historie.transform.parent.GetChild(0).gameObject.SetActive(false);
        yield return new WaitForSeconds(0.0001f);
        historie.transform.parent.GetChild(0).gameObject.SetActive(true);
    }
    private void ZeigeCorrektWrongSkip(string erklaerer, string team_green_s, string team_blue_s)
    {
        // Du bist Erklärer
        if (Config.spieler.name.Equals(erklaerer))
        {
            correct.SetActive(true);
            wrong.SetActive(true);
            if (round_skip_int > 0 || round_skip_int == -1)
                skip.SetActive(true);
            //SpielerIstDran.Play();
        }
        // Team Green
        else if (team_turn.Equals("Green"))
        {
            if (team_green_s.Contains(Config.spieler.uuid.ToString()))
            {
                correct.SetActive(false);
                wrong.SetActive(false);
                skip.SetActive(false);
            }
            else
            {
                correct.SetActive(false);
                wrong.SetActive(true);
                skip.SetActive(false);
            }
        }
        // Team Blue
        else
        {
            if (team_blue_s.Contains(Config.spieler.uuid.ToString()))
            {
                correct.SetActive(false);
                wrong.SetActive(false);
                skip.SetActive(false);
            }
            else
            {
                correct.SetActive(false);
                wrong.SetActive(true);
                skip.SetActive(false);
            }
        }
    }
    private void ZeigeCorrektWrongSkip(bool started)
    {
        if (started)
            return;
        correct.SetActive(false);
        wrong.SetActive(false);
        skip.SetActive(false);
    }
    private void MarkAlsErklaerer()
    { // Markiere Erklärer & entmarkiere alten
        // Team Green
        Transform list = team_green_grid.GetChild(2);
        for (int i = 0; i < list.childCount; i++)
        {
            if (list.GetChild(i).GetChild(1).GetComponent<TMP_Text>().text.Equals(erklaerer_name) ||
                list.GetChild(i).GetChild(1).GetComponent<TMP_Text>().text.Equals("<b><color=green>" + erklaerer_name))
                list.GetChild(i).GetChild(1).GetComponent<TMP_Text>().text =
                    "<b><color=green>" + erklaerer_name;
            else
                list.GetChild(i).GetChild(1).GetComponent<TMP_Text>().text =
                    list.GetChild(i).GetChild(1).GetComponent<TMP_Text>().text.Replace("<b><color=green>", "");
        }
        // Team Blue
        list = team_blue_grid.GetChild(2);
        for (int i = 0; i < list.childCount; i++)
        {
            if (list.GetChild(i).GetChild(1).GetComponent<TMP_Text>().text.Equals(erklaerer_name) ||
                list.GetChild(i).GetChild(1).GetComponent<TMP_Text>().text.Equals("<b><color=green>" + erklaerer_name))
                list.GetChild(i).GetChild(1).GetComponent<TMP_Text>().text =
                    "<b><color=green>" + erklaerer_name;
            else
                list.GetChild(i).GetChild(1).GetComponent<TMP_Text>().text =
                    list.GetChild(i).GetChild(1).GetComponent<TMP_Text>().text.Replace("<b><color=green>", "");
        }
    }
    private void StartTimer(int sec)
    {
        if (timer_coroutine != null)
            StopCoroutine(timer_coroutine);
        timer_coroutine = StartCoroutine(RunTimer(sec));
    }
    private IEnumerator RunTimer(int seconds)
    {
        bool show_timer = true;
        bool decrease_points = false;
        if (tabu_type.Equals("battle_royale"))
        {
            show_timer = false;
            decrease_points = true;
        }
        time.SetActive(show_timer);

        while (seconds >= 0 || decrease_points)
        {
            if (show_timer)
            {
                time.GetComponentInChildren<TMP_Text>().text = "" + seconds;

                if (seconds == 0)
                {
                    Beeep.Play();
                }
                // Moep Sound bei sekunden
                else if (seconds == 1 || seconds == 2 || seconds == 3)
                {
                    Moeoop.Play();
                }
                seconds--;
                yield return new WaitForSecondsRealtime(1);
            }
            if (decrease_points)
            {
                if (team_turn.Equals("Green"))
                    team_green_points = team_green_points - 1;
                else if (team_turn.Equals("Blue"))
                    team_blue_points = team_blue_points - 1;

                team_green_grid.GetChild(1).GetChild(1).GetComponent<TMP_Text>().text = "" + team_green_points;
                team_blue_grid.GetChild(1).GetChild(1).GetComponent<TMP_Text>().text = "" + team_blue_points;

                yield return new WaitForSecondsRealtime(1);

                if (team_green_points <= 0 || team_blue_points <= 0)
                    yield break;
            }
        }
        time.SetActive(false);
        yield break;
    }
    private void StartRound(string data_string)
    {
        startround.SetActive(false);
        started = true;
        string[] data = data_string.Split('*');
        tabu_type = data[0];
        string erklaerer = data[1];
        erklaerer_name = Player.getPlayerByName(erklaerer)?.name;
        team_turn = data[2];
        string team_green_s = data[3];
        string team_blue_s = data[4];
        UpdatePoints(data[5] + "*" + data[6]);
        string karte = data[7];
        DisplayKarte(true, erklaerer, team_turn, team_green_s, team_blue_s, karte);
        max_skip_int = int.Parse(data[8]);
        max_skip.text = "" + max_skip_int;
        round_skip_int = max_skip_int;
        skip_delay_int = int.Parse(data[9]);
        skip_delay.text = "" + skip_delay_int;
        timer_sec_int = int.Parse(data[10]);
        timer_sec.text = "" + timer_sec_int;

        ZeigeCorrektWrongSkip(erklaerer, team_green_s, team_blue_s);
        MarkAlsErklaerer();
        StartTimer(timer_sec_int);

        if (Config.spieler.name.Equals(erklaerer_name))
            SpielerIstDran.Play();
    }

    private void RoundEnd(string data_s)
    {
        string[] data = data_s.Split('*');
        string press_spieler_name = data[0];
        string indicator = data[1];
        string karte = data[2];
        string green_points = data[3];
        string blue_points = data[4];
        string erklaerer = data[5];
        string new_team_turn = data[6];
        string green_team = data[7];
        string blue_team = data[8];
        int round_skip = int.Parse(data[9]);
        int skip_delay = int.Parse(data[10]);
        int timer_sec = int.Parse(data[11]);

        // Play sound nach typ
        if (indicator.Equals("Correct"))
            ErratenSound.Play();
        else if (indicator.Equals("Wrong"))
            FalschSound.Play();

        UpdatePoints(green_points + "*" + blue_points);
        erklaerer_name = erklaerer;
        team_turn = new_team_turn;
        MarkAlsErklaerer();
        round_skip_int = round_skip;
        timer_sec_int = timer_sec;

        if (tabu_type == "normal")
        {
            HandleRoundEnd_Normal(indicator, karte, green_team, blue_team);
        }
        else if (tabu_type == "one_word_use")
        {
            HandleRoundEnd_OneWordUse(indicator, karte, green_team, blue_team);
        }
        else if (tabu_type == "one_word_goal")
        {
            HandleRoundEnd_OneWordGoal(indicator, karte, green_team, blue_team);
        }
        else if (tabu_type == "neandertaler")
        {
            HandleRoundEnd_Neandertaler(indicator, karte, green_team, blue_team);
        }
        else if (tabu_type == "battle_royale")
        {
            HandleRoundEnd_BattleRoyale(indicator, karte, green_team, blue_team);
        }
        else
            Utils.Log(LogType.Error, "Unbekannter Typ: " + tabu_type, true);
        ZeigeCorrektWrongSkip(erklaerer, green_team, blue_team);
        //ZeigeCorrektWrongSkip(started);
        ClientUtils.SendMessage("Tabu", "GetUpdate", "");
    }

    private void HandleRoundEnd_Normal(string indicator, string karte, string team_green_s, string team_blue_s)
    {
        if (indicator.Equals("Correct"))
        {
            DisplayKarte(true, erklaerer_name, team_turn, team_green_s, team_blue_s, karte);
        }
        else if (indicator.Equals("Wrong"))
        {
            DisplayKarte(true, erklaerer_name, team_turn, team_green_s, team_blue_s, karte);
        }
        else if (indicator.Equals("Skip"))
        {
            DisplayKarte(true, erklaerer_name, team_turn, team_green_s, team_blue_s, karte);
        }
        else if (indicator.Equals("Time"))
        {
            DisplayKarte(true, erklaerer_name, team_turn, team_green_s, team_blue_s, karte, true);
            StopCoroutine(timer_coroutine);
            time.SetActive(false);
            started = false;
        }
        else
            Utils.Log(LogType.Error, "Unbekannter Typ: " + tabu_type, true);
    }
    private void HandleRoundEnd_OneWordUse(string indicator, string karte, string team_green_s, string team_blue_s)
    {
        if (indicator.Equals("Correct"))
        {
            DisplayKarte(true, erklaerer_name, team_turn, team_green_s, team_blue_s, karte);
        }
        else if (indicator.Equals("Wrong"))
        {
            DisplayKarte(true, erklaerer_name, team_turn, team_green_s, team_blue_s, karte);
        }
        else if (indicator.Equals("Skip"))
        {
            DisplayKarte(true, erklaerer_name, team_turn, team_green_s, team_blue_s, karte);
        }
        else if (indicator.Equals("Time"))
        {
            DisplayKarte(true, erklaerer_name, team_turn, team_green_s, team_blue_s, karte, true);
            StopCoroutine(timer_coroutine);
            time.SetActive(false);
            started = false;
        }
        else
            Utils.Log(LogType.Error, "Unbekannter Typ: " + tabu_type, true);
    }
    private void HandleRoundEnd_OneWordGoal(string indicator, string karte, string team_green_s, string team_blue_s)
    {
        if (indicator.Equals("Correct"))
        {
            DisplayKarte(true, erklaerer_name, team_turn, team_green_s, team_blue_s, karte, true);
            StopCoroutine(timer_coroutine);
            time.SetActive(false);
            started = false;
        }
        else if (indicator.Equals("Wrong"))
        {
            DisplayKarte(true, erklaerer_name, team_turn, team_green_s, team_blue_s, karte, true);
            StopCoroutine(timer_coroutine);
            time.SetActive(false);
            started = false;
        }
        else if (indicator.Equals("Skip"))
        {
            DisplayKarte(true, erklaerer_name, team_turn, team_green_s, team_blue_s, karte);
        }
        else if (indicator.Equals("Time"))
        {
            DisplayKarte(true, erklaerer_name, team_turn, team_green_s, team_blue_s, karte, true);
            StopCoroutine(timer_coroutine);
            time.SetActive(false);
            started = false;
        }
        else
            Utils.Log(LogType.Error, "Unbekannter Typ: " + tabu_type, true);
    }
    private void HandleRoundEnd_Neandertaler(string indicator, string karte, string team_green_s, string team_blue_s)
    {
        if (indicator.Equals("Correct"))
        {
            DisplayKarte(true, erklaerer_name, team_turn, team_green_s, team_blue_s, karte, true);
            StopCoroutine(timer_coroutine);
            time.SetActive(false);
            started = false;
        }
        else if (indicator.Equals("Wrong"))
        {
            DisplayKarte(true, erklaerer_name, team_turn, team_green_s, team_blue_s, karte, true);
            StopCoroutine(timer_coroutine);
            time.SetActive(false);
            started = false;
        }
        else if (indicator.Equals("Skip"))
        {
            DisplayKarte(true, erklaerer_name, team_turn, team_green_s, team_blue_s, karte);
        }
        else if (indicator.Equals("Time"))
        {
            DisplayKarte(true, erklaerer_name, team_turn, team_green_s, team_blue_s, karte, true);
            StopCoroutine(timer_coroutine);
            time.SetActive(false);
            started = false;
        }
        else
            Utils.Log(LogType.Error, "Unbekannter Typ: " + tabu_type, true);
    }
    private void HandleRoundEnd_BattleRoyale(string indicator, string karte, string team_green_s, string team_blue_s)
    {
        if (indicator.Equals("Correct"))
        {
            DisplayKarte(true, erklaerer_name, team_turn, team_green_s, team_blue_s, karte);
            if (Config.spieler.name.Equals(erklaerer_name))
                SpielerIstDran.Play();
        }
        else if (indicator.Equals("Wrong"))
        {
            DisplayKarte(true, erklaerer_name, team_turn, team_green_s, team_blue_s, karte);
        }
        else if (indicator.Equals("Skip"))
        {
            DisplayKarte(true, erklaerer_name, team_turn, team_green_s, team_blue_s, karte);
        }
        else if (indicator.Equals("Time"))
        {
            DisplayKarte(true, erklaerer_name, team_turn, team_green_s, team_blue_s, karte, true);
            StopCoroutine(timer_coroutine);
            time.SetActive(false);
            started = false;
        }
        else
            Utils.Log(LogType.Error, "Unbekannter Typ: " + tabu_type, true);
    }
}
