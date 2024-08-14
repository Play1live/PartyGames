using Fleck;
using System.Globalization;

namespace ServerConsole.Games
{
    internal class TabuHandler
    {
        private static int max_skip;
        private static int round_skip;
        private static int skip_delay;
        private static int timer_sec;
        private static int round_sec;
        private static CancellationTokenSource cancellationTokenSource;

        private static int team_green_points;
        private static List<Player> team_green;
        private static Player team_green_last_turn;
        private static int team_blue_points;
        private static List<Player> team_blue;
        private static Player team_blue_last_turn;
        private static Player erklaerer;
        private static string team_turn;
        private static TabuItem karte;

        public static void StartGame()
        {
            Utils.Log(LogType.Trace, "StartGame");
            Config.game_title = "Tabu";
            team_green = new List<Player>();
            team_blue = new List<Player>();
            InitTeams();
            InitMaxSkip();
            InitSkipDelay();
            InitTimerSec();
            InitTeamPoints();
        }

        public static void OnCommand(IWebSocketConnection socket, string cmd, string data)
        {
            Player player = Player.getPlayerBySocket(socket);
            Utils.Log(LogType.Trace, player.name + " > " + cmd + " " + data);
            switch (cmd)
            {
                default:
                    Utils.Log(LogType.Warning, "Unbekannter Befehl: " + cmd + " " + data);
                    return;
                case "GetSpielerUpdate": BroadcastSpielerUpdate(); break;
                case "RandomTeams": RandomTeams(player); break;
                case "ResetGame": ResetGame(player); break;
                case "ChangePoints": ChangePoints(player, data); break;
                case "ChangeMaxSkip": ChangeMaxSkip(player, data); break;
                case "ChangeSkipDelay": ChangeSkipDelay(player, data); break;
                case "ChangeTimerSec": ChangeTimerSec(player, data); break;
                case "GetGameInfo": 
                    ServerUtils.SendMessage(player, "Tabu", "SetGameInfo", Config.tabu.GetTabuType() + " " + Config.tabu.GetSelected().name); 
                    InitModeratorView(); 
                    break;

                case "GetUpdate": ServerUtils.SendMessage(player, "Tabu", "SpielerUpdate", SpielerUpdate()); break;
                case "JoinTeam": JoinTeam(player, data); break;
                case "StarteAlsErklaerer": StartRound(player); break;
                case "ClickRichtig": RoundEnd(player, "Correct"); break;
                case "ClickFalsch": RoundEnd(player, "Wrong"); break;
                case "ClickSkip": 
                    if (round_skip == -1 || round_skip > 0)
                    {
                        if (round_skip > 0)
                            round_skip--;
                        RoundEnd(player, "Skip");
                    }
                    break;
            }
        }

        public static void BroadcastSpielerUpdate()
        {
            ServerUtils.BroadcastMessage("Lobby", "SpielerUpdate", SpielerUpdate());
        }
        private static string SpielerUpdate()
        {
            string green = "";
            foreach (var item in team_green)
                green += "*" + item.id;
            if (green.Length > 0)
                green = green.Substring(1);
            green = "[GREEN_LIST]" + green + "[GREEN_LIST][GREEN_POINTS]" + team_green_points + "[GREEN_POINTS]";
            
            string blue = "";
            foreach (var item in team_blue)
                blue += "*" + item.id;
            if (blue.Length > 0)
                blue = blue.Substring(1);
            blue = "[BLUE_LIST]" + blue + "[BLUE_LIST][BLUE_POINTS]" + team_blue_points + "[BLUE_POINTS]";

            return green + blue + "[TURN]" + team_turn + "[TURN]";
        }
        #region Moderator
        private static void InitModeratorView()
        {        //                                  MaxSkip SkipDelay TimerSec, TeamPoints
            if (Config.tabu.GetTabuType().Equals(TabuType.normal))
            {
                max_skip = Tabu.settings_normal[0];
                skip_delay = Tabu.settings_normal[1];
                timer_sec = Tabu.settings_normal[2];
            }
            else if (Config.tabu.GetTabuType().Equals(TabuType.one_word_use))
            {
                max_skip = Tabu.settings_one_word_use[0];
                skip_delay = Tabu.settings_one_word_use[1];
                timer_sec = Tabu.settings_one_word_use[2];
            }
            else if (Config.tabu.GetTabuType().Equals(TabuType.one_word_goal))
            {
                max_skip = Tabu.settings_one_word_goal[0];
                skip_delay = Tabu.settings_one_word_goal[1];
                timer_sec = Tabu.settings_one_word_goal[2];
            }
            else if (Config.tabu.GetTabuType().Equals(TabuType.neandertaler))
            {
                max_skip = Tabu.settings_neandertaler[0];
                skip_delay = Tabu.settings_neandertaler[1];
                timer_sec = Tabu.settings_neandertaler[2];
            }
            else if (Config.tabu.GetTabuType().Equals(TabuType.battle_royale))
            {
                max_skip = Tabu.settings_battle_royale[0];
                skip_delay = Tabu.settings_battle_royale[1];
                timer_sec = Tabu.settings_battle_royale[2];
            }

            ServerUtils.BroadcastMessage("Tabu", "InitModeratorView", max_skip + "#" + skip_delay + "#" + timer_sec);
        }
        private static void RandomTeams(Player p)
        {
            if (Config.moderator != p)
            {
                return;
            }
            InitTeams();
            BroadcastSpielerUpdate();
        }
        private static void ResetGame(Player p)
        {
            if (Config.moderator != p)
            {
                return;
            }
            InitTeams();
            InitMaxSkip();
            InitSkipDelay();
            InitTimerSec();
            InitTeamPoints();
            InitModeratorView();
        }
        private static void ChangeMaxSkip(Player p, string data)
        {
            if (Config.moderator != p)
            {
                return;
            }
            try
            {
                max_skip = int.Parse(data);
                if (!(max_skip >= 0 || max_skip == -1))
                    max_skip = 0;
            }
            catch (Exception e) 
            {
                Utils.Log(LogType.Warning, "Fehlerhafte Eingabe: " + e);
            }
            ServerUtils.BroadcastMessage("Tabu", "ChangeMaxSkip", "" + max_skip);
        }
        private static void ChangeSkipDelay(Player p, string data)
        {
            if (Config.moderator != p)
            {
                return;
            }
            try
            {
                skip_delay = int.Parse(data);
                if (skip_delay <= 0)
                    skip_delay = 1;
            }
            catch (Exception e)
            {
                Utils.Log(LogType.Warning, "Fehlerhafte Eingabe: " + e);
            }
            ServerUtils.BroadcastMessage("Tabu", "ChangeSkipDelay", "" + skip_delay);
        }
        private static void ChangeTimerSec(Player p, string data)
        {
            if (Config.moderator != p)
            {
                return;
            }
            try
            {
                timer_sec = int.Parse(data);
                if (timer_sec <= 0)
                    timer_sec = 1;
            }
            catch (Exception e)
            {
                Utils.Log(LogType.Warning, "Fehlerhafte Eingabe: " + e);
            }
            ServerUtils.BroadcastMessage("Tabu", "ChangeTimerSec", "" + timer_sec);
        }
        private static void ChangePoints(Player p, string data)
        {
            string team = data.Split('*')[0];
            int points = int.Parse(data.Split('*')[1]);

            if (team.Equals("Green"))
                team_green_points = points;
            else if (team.Equals("Blue"))
                team_blue_points = points;

            ServerUtils.BroadcastMessage("Tabu", "UpdatePoints", team_green_points + "*" + team_blue_points);
        }
        #endregion

        #region Logic
        private static void InitTeams()
        {
            team_green.Clear();
            team_green_last_turn = null;
            team_blue.Clear();
            team_blue_last_turn = null;
            team_turn = "Green";
            erklaerer = null;
            List<Player> temp_list = new List<Player>();
            temp_list.AddRange(Config.players);
            while (temp_list.Count > 0)
            {
                int random = new Random().Next(0, temp_list.Count);
                if (team_green.Count > team_blue.Count)
                    team_blue.Add(temp_list[random]);
                else
                    team_green.Add(temp_list[random]);
                temp_list.RemoveAt(random);
            }

            if (Config.tabu.GetTabuType().Equals(TabuType.normal))
            {
                team_green_points = Tabu.settings_normal[3];
                team_blue_points = Tabu.settings_normal[3];
            }
            else if (Config.tabu.GetTabuType().Equals(TabuType.one_word_use))
            {
                team_green_points = Tabu.settings_one_word_use[3];
                team_blue_points = Tabu.settings_one_word_use[3];
            }
            else if (Config.tabu.GetTabuType().Equals(TabuType.one_word_goal))
            {
                team_green_points = Tabu.settings_one_word_goal[3];
                team_blue_points = Tabu.settings_one_word_goal[3];
            }
            else if (Config.tabu.GetTabuType().Equals(TabuType.neandertaler))
            {
                team_green_points = Tabu.settings_neandertaler[3];
                team_blue_points = Tabu.settings_neandertaler[3];
            }
            else if (Config.tabu.GetTabuType().Equals(TabuType.battle_royale))
            {
                team_green_points = Tabu.settings_battle_royale[3];
                team_blue_points = Tabu.settings_battle_royale[3];
            }
        }
        private static void InitMaxSkip()
        {
            if (Config.tabu.GetTabuType() == TabuType.normal)
                max_skip = Tabu.settings_normal[0];
            else if (Config.tabu.GetTabuType() == TabuType.one_word_use)
                max_skip = Tabu.settings_one_word_use[0];
            else if (Config.tabu.GetTabuType() == TabuType.one_word_goal)
                max_skip = Tabu.settings_one_word_goal[0];
            else if (Config.tabu.GetTabuType() == TabuType.neandertaler)
                max_skip = Tabu.settings_neandertaler[0];
            else if (Config.tabu.GetTabuType() == TabuType.battle_royale)
                max_skip = Tabu.settings_battle_royale[0];
            else
                Utils.Log(LogType.Error, "Unbekannter Tabu Typ: " + Config.tabu.GetTabuType());
        }
        private static void InitSkipDelay()
        {
            if (Config.tabu.GetTabuType() == TabuType.normal)
                skip_delay = Tabu.settings_normal[1];
            else if (Config.tabu.GetTabuType() == TabuType.one_word_use)
                skip_delay = Tabu.settings_one_word_use[1];
            else if (Config.tabu.GetTabuType() == TabuType.one_word_goal)
                skip_delay = Tabu.settings_one_word_goal[1];
            else if (Config.tabu.GetTabuType() == TabuType.neandertaler)
                skip_delay = Tabu.settings_neandertaler[1];
            else if (Config.tabu.GetTabuType() == TabuType.battle_royale)
                skip_delay = Tabu.settings_battle_royale[1];
            else
                Utils.Log(LogType.Error, "Unbekannter Tabu Typ: " + Config.tabu.GetTabuType());
        }
        private static void InitTimerSec()
        {
            if (Config.tabu.GetTabuType() == TabuType.normal)
                timer_sec = Tabu.settings_normal[2];
            else if (Config.tabu.GetTabuType() == TabuType.one_word_use)
                timer_sec = Tabu.settings_one_word_use[2];
            else if (Config.tabu.GetTabuType() == TabuType.one_word_goal)
                timer_sec = Tabu.settings_one_word_goal[2];
            else if (Config.tabu.GetTabuType() == TabuType.neandertaler)
                timer_sec = Tabu.settings_neandertaler[2];
            else if (Config.tabu.GetTabuType() == TabuType.battle_royale)
                timer_sec = Tabu.settings_battle_royale[2];
            else
                Utils.Log(LogType.Error, "Unbekannter Tabu Typ: " + Config.tabu.GetTabuType());
        }
        private static void InitTeamPoints()
        {
            if (Config.tabu.GetTabuType() == TabuType.normal)
            {
                team_green_points = Tabu.settings_normal[3];
                team_blue_points = Tabu.settings_normal[3];
            }
            else if (Config.tabu.GetTabuType() == TabuType.one_word_use)
            {
                team_green_points = Tabu.settings_one_word_use[3];
                team_blue_points = Tabu.settings_one_word_use[3];
            }
            else if (Config.tabu.GetTabuType() == TabuType.one_word_goal)
            {
                team_green_points = Tabu.settings_one_word_goal[3];
                team_blue_points = Tabu.settings_one_word_goal[3];
            }
            else if (Config.tabu.GetTabuType() == TabuType.neandertaler)
            {
                team_green_points = Tabu.settings_neandertaler[3];
                team_blue_points = Tabu.settings_neandertaler[3];
            }
            else if (Config.tabu.GetTabuType() == TabuType.battle_royale)
            {
                team_green_points = Tabu.settings_battle_royale[3];
                team_blue_points = Tabu.settings_battle_royale[3];
            }
            else
            {
                team_green_points = 0;
                team_blue_points = 0;
            }
        }
        private static void JoinTeamIfSpectator(Player p)
        {
            if (team_green.Contains(p) || team_blue.Contains(p))
                return;

            if (team_green.Count > team_blue.Count)
                team_green.Add(p);
            else
                team_blue.Add(p);

            BroadcastSpielerUpdate();
        }
        private static void JoinTeam(Player p, string data)
        {
            if (data.Equals("Green"))
            {
                if (!team_green.Contains(p))
                {
                    if (team_blue.Contains(p))
                        team_blue.Remove(p);
                    team_green.Add(p);
                }
                BroadcastSpielerUpdate();
            }
            else if (data.Equals("Blue"))
            {
                if (!team_blue.Contains(p))
                {
                    if (team_green.Contains(p))
                        team_green.Remove(p);
                    team_blue.Add(p);
                }
                BroadcastSpielerUpdate();
            }
            else
                Utils.Log(LogType.Error, "Unbekanntes Team " + data);
        }
        private static string GetTeamOfPlayer(Player p)
        {
            if (team_green.Contains(p))
                return "Green";
            else if (team_blue.Contains(p))
                return "Blue";
            Utils.Log(LogType.Error, "Unbekannter Spieler: " + p.ToString());
            return "ERROR";
        }
        private static void StartTimer()
        {
            cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => StartCountdown(cancellationTokenSource.Token));
        }
        private static void StopTimer()
        {
            cancellationTokenSource.Cancel(); // Timer stoppen
        }
        private static void StartCountdown(CancellationToken cancellationToken)
        {
            bool decrease_points = false;
            if (Config.tabu.GetTabuType().Equals(TabuType.battle_royale))
                decrease_points = true;

            while (round_sec >= 0 || decrease_points)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return; // Timer wurde abgebrochen
                }

                if (!decrease_points)
                {
                    Utils.Log(LogType.Trace, $"Verbleibende Zeit: {round_sec} Sekunden");
                    round_sec--;
                    Thread.Sleep(1000); // Eine Sekunde warten
                }
                else
                {
                    if (team_turn.Equals("Green"))
                        team_green_points--;
                    else if (team_turn.Equals("Blue"))
                        team_blue_points--;

                    Utils.Log(LogType.Trace, $"Verbleibende Zeit: {team_green_points} {team_blue_points} Sekunden");
                    Thread.Sleep(1000); // Eine Sekunde warten

                    if (team_green_points <= 0 || team_blue_points <= 0)
                        break;
                }

            }

            Utils.Log(LogType.Trace, "Timer abgelaufen");
            RoundEnd(null, "Time");
        }
        private static void StartRound(Player spieler)
        {
            erklaerer = spieler;
            team_turn = GetTeamOfPlayer(spieler);
            round_skip = max_skip;
            round_sec = timer_sec;
            if (team_turn.Equals("Green"))
                team_green_last_turn = spieler;
            else
                team_blue_last_turn = spieler;
            karte = Config.tabu.GetSelected().GetRandom();
            string team_green_s = "";
            foreach (var item in team_green)
                team_green_s += "+" + item.id;
            if (team_green_s.Length > 0)
                team_green_s = team_green_s.Substring(1);
            string team_blue_s = "";
            foreach (var item in team_blue)
                team_blue_s += "+" + item.id;
            if (team_blue_s.Length > 0)
                team_blue_s = team_blue_s.Substring(1);
            ServerUtils.BroadcastMessage("Tabu", "StartRound",
                Config.tabu.GetTabuType()
                + "*" + erklaerer.name
                + "*" + team_turn
                + "*" + team_green_s
                + "*" + team_blue_s
                + "*" + team_green_points
                + "*" + team_blue_points
                + "*" + karte.ToString()
                + "*" + max_skip
                + "*" + skip_delay
                + "*" + timer_sec);
            StartTimer();
        }
        private static void RoundEnd(Player p, string indicator)
        {
            if (erklaerer == null)
                return;
            ServerUtils.BroadcastMessage("Tabu", "AddHistory", 
                erklaerer.id.ToString() + "*" + indicator + "*" + karte.ToString());

            if (indicator.Equals("Correct"))
                UpdatePointsAfterRound(team_turn, 0);
            else if (indicator.Equals("Wrong"))
                UpdatePointsAfterRound(team_turn, 1);
            else if (indicator.Equals("Skip"))
                UpdatePointsAfterRound(team_turn, 2);

            if (Config.tabu.GetTabuType() == TabuType.normal)
            {
                HandleRoundEnd_Normal(p, indicator);
            }
            else if (Config.tabu.GetTabuType() == TabuType.one_word_use)
            {
                HandleRoundEnd_OneWordUse(p, indicator);
            }
            else if (Config.tabu.GetTabuType() == TabuType.one_word_goal)
            {
                HandleRoundEnd_OneWordGoal(p, indicator);
            }
            else if (Config.tabu.GetTabuType() == TabuType.neandertaler)
            {
                HandleRoundEnd_Neandertaler(p, indicator);
            }
            else if (Config.tabu.GetTabuType() == TabuType.battle_royale)
            {
                HandleRoundEnd_BattleRoyale(p, indicator);
            }
            else
                Utils.Log(LogType.Error, "Unbekannter Typ: " + Config.tabu.GetTabuType());

            string team_green_s = "";
            foreach (var item in team_green)
                team_green_s += "+" + item.id;
            if (team_green_s.Length > 0)
                team_green_s = team_green_s.Substring(1);
            string team_blue_s = "";
            foreach (var item in team_blue)
                team_blue_s += "+" + item.id;
            if (team_blue_s.Length > 0)
                team_blue_s = team_blue_s.Substring(1);
            
            ServerUtils.BroadcastMessage("Tabu", "RoundEnd", 
                p?.name + "*" + 
                indicator + "*" + 
                karte.ToString() + "*" +
                team_green_points + "*" +
                team_blue_points + "*" + 
                erklaerer?.name + "*" +
                team_turn + "*" +
                team_green_s + "*" + 
                team_blue_s + "*" +
                round_skip + "*" +
                skip_delay + "*" + 
                round_sec
                );
        }
        private static void HandleRoundEnd_Normal(Player p, string indicator)
        {
            if (indicator.Equals("Correct"))
            {
                NeueKarte();
            }
            else if (indicator.Equals("Wrong"))
            {
                NeueKarte();
            }
            else if (indicator.Equals("Skip"))
            {
                NeueKarte();
            }
            else if (indicator.Equals("Time"))
            {
                erklaerer = null;
                if (team_turn.Equals("Green"))
                    team_turn = "Blue";
                else
                    team_turn = "Green";
                round_skip = max_skip;
                round_sec = timer_sec;
            }
            else
                Utils.Log(LogType.Error, "Unbekannter Typ: " + Config.tabu.GetTabuType());
        }
        private static void HandleRoundEnd_OneWordUse(Player p, string indicator)
        {
            if (indicator.Equals("Correct"))
            {
                NeueKarte();
            }
            else if (indicator.Equals("Wrong"))
            {
                NeueKarte();
            }
            else if (indicator.Equals("Skip"))
            {
                NeueKarte();
            }
            else if (indicator.Equals("Time"))
            {
                erklaerer = null;
                if (team_turn.Equals("Green"))
                    team_turn = "Blue";
                else
                    team_turn = "Green";
                round_skip = max_skip;
                round_sec = timer_sec;
            }
            else
                Utils.Log(LogType.Error, "Unbekannter Typ: " + Config.tabu.GetTabuType());
        }
        private static void HandleRoundEnd_OneWordGoal(Player p, string indicator)
        {
            if (indicator.Equals("Correct"))
            {
                erklaerer = null;
                if (team_turn.Equals("Green"))
                    team_turn = "Blue";
                else
                    team_turn = "Green";
                round_skip = max_skip;
                round_sec = timer_sec;
            }
            else if (indicator.Equals("Wrong"))
            {
                erklaerer = null;
                if (team_turn.Equals("Green"))
                    team_turn = "Blue";
                else
                    team_turn = "Green";
                round_skip = max_skip;
                round_sec = timer_sec;
            }
            else if (indicator.Equals("Skip"))
            {
                NeueKarte();
            }
            else if (indicator.Equals("Time"))
            {
                erklaerer = null;
                if (team_turn.Equals("Green"))
                    team_turn = "Blue";
                else
                    team_turn = "Green";
                round_skip = max_skip;
                round_sec = timer_sec;
            }
            else
                Utils.Log(LogType.Error, "Unbekannter Typ: " + Config.tabu.GetTabuType());
        }
        private static void HandleRoundEnd_Neandertaler(Player p, string indicator)
        {
            if (indicator.Equals("Correct"))
            {
                erklaerer = null;
                if (team_turn.Equals("Green"))
                    team_turn = "Blue";
                else
                    team_turn = "Green";
                round_skip = max_skip;
                round_sec = timer_sec;
            }
            else if (indicator.Equals("Wrong"))
            {
                erklaerer = null;
                if (team_turn.Equals("Green"))
                    team_turn = "Blue";
                else
                    team_turn = "Green";
                round_skip = max_skip;
                round_sec = timer_sec;
            }
            else if (indicator.Equals("Skip"))
            {
                NeueKarte();
            }
            else if (indicator.Equals("Time"))
            {
                erklaerer = null;
                if (team_turn.Equals("Green"))
                    team_turn = "Blue";
                else
                    team_turn = "Green";
                round_skip = max_skip;
                round_sec = timer_sec;
            }
            else
                Utils.Log(LogType.Error, "Unbekannter Typ: " + Config.tabu.GetTabuType());
        }
        private static void HandleRoundEnd_BattleRoyale(Player p, string indicator)
        {
            if (indicator.Equals("Correct"))
            {
                round_skip = max_skip;
                round_sec = timer_sec;
                if (team_turn.Equals("Green"))
                    team_turn = "Blue";
                else
                    team_turn = "Green";
                //Erklärer wählen
                List<Player> anderesTeam = new List<Player>();
                if (team_turn.Equals("Green"))
                    anderesTeam.AddRange(team_green);
                else
                    anderesTeam.AddRange(team_blue);
                if (team_green_last_turn != null)
                    anderesTeam.Remove(team_green_last_turn);
                if (team_blue_last_turn != null)
                    anderesTeam.Remove(team_blue_last_turn);

                erklaerer = anderesTeam[new Random().Next(0, anderesTeam.Count)];
                if (team_turn.Equals("Green"))
                    team_green_last_turn = erklaerer;
                else
                    team_blue_last_turn = erklaerer;
                NeueKarte();
            }
            else if (indicator.Equals("Wrong"))
            {
                NeueKarte();
            }
            else if (indicator.Equals("Skip"))
            {
                NeueKarte();
            }
            else if (indicator.Equals("Time"))
            {
                erklaerer = null;
                if (team_turn.Equals("Green"))
                    team_turn = "Blue";
                else
                    team_turn = "Green";
                round_skip = max_skip;
                round_sec = timer_sec;
            }
            else
                Utils.Log(LogType.Error, "Unbekannter Typ: " + Config.tabu.GetTabuType());
        }
        private static void NeueKarte()
        {
            TabuItem temp = karte;
            karte = Config.tabu.GetSelected().GetRandom();
            Utils.Log(LogType.Trace, karte.ToString());
            if (karte == temp)
            {
                Utils.Log(LogType.Trace, "Neue Karte wäre eine Dopplung. Generiere neu");
                NeueKarte();
                return;
            }
        }
        private static void UpdatePointsAfterRound(string team, int indicator)
        { // Indicator: Correct: 0, Wrong: 1, Skip: 2
            if (Config.tabu.GetTabuType() == TabuType.normal)
            {
                if (team.Equals("Green"))
                    team_green_points += Tabu.points_normal[indicator];
                else if (team.Equals("Blue"))
                    team_blue_points += Tabu.points_normal[indicator];
            }
            else if (Config.tabu.GetTabuType() == TabuType.one_word_use)
            {
                if (team.Equals("Green"))
                    team_green_points += Tabu.points_one_word_use[indicator];
                else if (team.Equals("Blue"))
                    team_blue_points += Tabu.points_one_word_use[indicator];
            }
            else if (Config.tabu.GetTabuType() == TabuType.one_word_goal)
            {
                if (team.Equals("Green"))
                    team_green_points += Tabu.points_one_word_goal[indicator];
                else if (team.Equals("Blue"))
                    team_blue_points += Tabu.points_one_word_goal[indicator];
            }
            else if (Config.tabu.GetTabuType() == TabuType.neandertaler)
            {
                if (team.Equals("Green"))
                    team_green_points += Tabu.points_neandertaler[indicator];
                else if (team.Equals("Blue"))
                    team_blue_points += Tabu.points_neandertaler[indicator];
            }
            else if (Config.tabu.GetTabuType() == TabuType.battle_royale)
            {
                if (team.Equals("Green"))
                    team_green_points += Tabu.points_battle_royale[indicator];
                else if (team.Equals("Blue"))
                    team_blue_points += Tabu.points_battle_royale[indicator];
            }
            else
            {
                Utils.Log(LogType.Error, "Unbekannter Typ: " + Config.tabu.GetTabuType());
            }
        }
        #endregion
    }

    internal class Tabu
    {
        public const byte min_player = 4;
        public const byte max_player = 20;
        public const byte tabu_word_count = 6;
        //                                  Correct, Wrong, Skip
        public static int[] points_normal = { 1, -1, 0};
        public static int[] points_one_word_use = { 1, 0, 0};
        public static int[] points_one_word_goal = { 1, 0, 0 };
        public static int[] points_neandertaler = { 1, 0, 0 };
        public static int[] points_battle_royale = { 15, -20, -5 };
        //                                  MaxSkip SkipDelay TimerSec, TeamPoints
        public static int[] settings_normal = { -1, 1, 60, 0 };
        public static int[] settings_one_word_use = { -1, 1, 60, 0 };
        public static int[] settings_one_word_goal = { 0, 1, 60, 0 };
        public static int[] settings_neandertaler = { 2, 0, 60, 0 };
        public static int[] settings_battle_royale= { -1, 5, -1, 600 };
        private TabuType type = TabuType.normal;
        private List<TabuSet> sets;
        private TabuSet selected;
        private long available_words;
        public Tabu()
        {
            this.sets = new List<TabuSet>();
            foreach (var item in Directory.EnumerateFiles("Resources/Tabu/"))
                this.sets.Add(new TabuSet(Path.GetFileName(item), File.ReadAllText(item), this));
            
            if (sets.Count > 0)
                selected = sets[0];

            Utils.Log(LogType.Info, "Tabu wurde initialisiert.");
        }
        public TabuType GetTabuType() { return this.type; }
        public string GetTypeAsString() { return this.type.ToString(); }
        public string GetTypesAsString() { return string.Join("[TRENNER]", Enum.GetNames(typeof(TabuType))); }
        public void SetType(TabuType type) { this.type = type; }
        public void SetType(int type) { this.type = (TabuType)type; }
        public List<TabuSet> GetSets() { return this.sets; }
        public string GetSetsAsString() { return string.Join("[TRENNER]", GetSets()); }
        public TabuSet GetSet(int index) { return this.sets[index]; }
        public TabuSet GetSelected() { return this.selected; }
        public void SetSelected(TabuSet set) { this.selected = set; }
        public void SetSelected(int set) { SetSelected(GetSets()[set]); }
        public string GetAvailableWords() { return this.available_words.ToString("N", new CultureInfo("de-DE")); }
        public void AddAvailableWords(long counter) { this.available_words += counter; }
    }
    internal class TabuSet
    {
        public bool need_to_safe = false;
        public string name;
        public List<TabuItem> worte;
        private Queue<TabuItem> last_recent_items;

        public TabuSet(string name, string inhalt, Tabu parent)
        {
            this.name = name;
            this.need_to_safe = false;
            this.last_recent_items = new Queue<TabuItem>(15);
            this.worte = new List<TabuItem>();
            List<string> words = new List<string>();
            foreach (string item in inhalt.Split('~'))
            {
                if (item.Length <= 3)
                    continue;
                string temp = item;
                temp = temp.Replace("\n", "").Replace("\r", "")
                .Replace("##ss##", "ß").Replace("#ss#", "ß")
                .Replace("#ue#", "ü").Replace("#UE#", "Ü")
                .Replace("#oe#", "ö").Replace("#OE#", "Ö")
                .Replace("#ae#", "ä").Replace("#AE#", "Ä");

                string word = temp.Split('|')[0];
                string tabus = temp.Split("|")[1];

                if (!words.Contains(word.ToLower()))
                {
                    words.Add(word.ToLower());
                    this.worte.Add(new TabuItem(word, tabus, this));
                }
                else
                {
                    Utils.Log(LogType.Warning, "Wort kommt doppelt vor: " + word);
                    this.need_to_safe = true;
                }
            }
            parent.AddAvailableWords(this.worte.Count);
#if DEBUG
            if (need_to_safe)
            {
                List<string> newFile = new List<string>();
                foreach (TabuItem item in this.worte)
                {
                    newFile.Add("~" + item.GetWord().Replace("ß", "#ss#")
                    .Replace("Ü", "#UE#").Replace("ü", "#ue#")
                    .Replace("Ö", "#OE#").Replace("ö", "#oe#")
                    .Replace("Ä", "#AE#").Replace("ä", "#ae#")
                    + "|" + string.Join("-", item.GetTabus())
                    .Replace("ß", "#ss#")
                    .Replace("Ü", "#UE#").Replace("ü", "#ue#")
                    .Replace("Ö", "#OE#").Replace("ö", "#oe#")
                    .Replace("Ä", "#AE#").Replace("ä", "#ae#"));
                }
                File.WriteAllLines(@"Resources\Tabu\" + this.name, newFile);
                Utils.Log(LogType.Info, "File: " + name + " wurde gespeichert. " + 
                    Path.GetFullPath(@"Resources\Tabu\" + this.name + ".txt") + " " + this.worte.Count);
            }
#endif
        }
        public override string ToString() { return name; }
        public TabuItem GetRandom()
        {
            TabuItem item = this.worte[new Random().Next(0, this.worte.Count)];
            for (int i = 0; i < 15; i++)
            {
                if (!last_recent_items.Contains(item))
                    break;
                item = this.worte[new Random().Next(0, this.worte.Count)];
            }
            this.last_recent_items.Enqueue(item);

            if (this.last_recent_items.Count > 15)
                this.last_recent_items.Dequeue();

            return this.worte[new Random().Next(0, this.worte.Count)]; 
        }
    }
    internal class TabuItem
    {
        private string word;
        private List<string> tabus;

        public TabuItem(string word, string tabus, TabuSet set)
        {
            this.word = word;
            this.tabus = new List<string>();
            foreach (string item in tabus.Split('-'))
            {
                if (!this.tabus.Contains(item.ToLower(), StringComparer.OrdinalIgnoreCase) && item.ToLower() != this.word.ToLower())
                {
                    this.tabus.Add(item);
                }
                else if (item.Contains('#'))
                {
                    Utils.Log(LogType.Warning, "Wort enthält # " + item);
                    set.need_to_safe = true;
                    this.tabus.Add(item.Replace("#", ""));
                }
                else
                {
                    Utils.Log(LogType.Warning, "Dopplung bei Wort: " + this.word + " >> " + item);
                    set.need_to_safe = true;
                }
            }
        }
        public override string ToString() { return this.word + "#" + this.GenTabuListAsString(); }
        public string GetWord() { return this.word; }
        public List<string> GetTabus() { return this.tabus; }
        public List<string> GenTabuList()
        {
            if (this.tabus.Count > Tabu.tabu_word_count)
            {
                List<string> temp_tabus = new List<string>();
                temp_tabus.AddRange(this.tabus);
                List<string> list = new List<string>();
                for (int i = 0; i < Tabu.tabu_word_count; i++)
                {
                    int random = new Random().Next(0, temp_tabus.Count);
                    list.Add(temp_tabus[random]);
                    temp_tabus.RemoveAt(random);
                }
                return list;
            }
            else
                return GetTabus();
        }
        public string GenTabuListAsString() { return string.Join("-", GenTabuList()); }

    }
    enum TabuType
    {
        // Erklärer versucht so viele Worte wie möglich zu erklären.
        // Zeitbegrenzung meist 1 min. (Unendlicher Skip möglich, delay 1 sek)
        normal = 0,
        // Erklärer darf die zu erklärenden Worte nur mit einzelnen Worten beschreiben (Nomen, Adjektive) keine Ganzen Sätze
        // Zeitbegrenzung meist 1 min. (Unendlicher Skip möglich, delay 1 sek)
        one_word_use = 1,
        // Erklärer muss nur 1 Wort erklären hat dafür 1 min Zeit, kein Skip
        one_word_goal = 2,
        // Erklärer darf die zu erklärenden Worte nur Einsilbigen Worten erklären
        // Zeitbegrenzung meist 1 min. (2 Skip möglich, delay 1 sek)
        neandertaler = 3,
        // Spieler haben Zeitkonto das runterläuft wenn die dran sind
        // Wird 1 Wort erraten gibts Bonuspunkte
        // Wird 1 Wort geskippt gibts Abzug
        // Wird 1 Wort falsch gemacht gibts Abzug
        battle_royale = 4,
    }
}
