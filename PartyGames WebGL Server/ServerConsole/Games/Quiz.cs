using Fleck;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace ServerConsole.Games
{
    internal class QuizHandler
    {
        private static List<QuizPlayer> qplist;
        private static int pointsprorichtig;
        private static int pointsprofalsch;
        private static bool buzzerisactive;
        private static bool buzzerisfreigegeben;
        private static bool showtabbedout;
        private static int fragenindex;
        private static int wronganswers;

        public static void StartGame()
        {
            Utils.Log(LogType.Trace, "StartGame");
            Config.game_title = "Quiz";
            qplist = new List<QuizPlayer>();
            foreach (Player p in Config.players)
                qplist.Add(new QuizPlayer(p));
            pointsprofalsch = 1;
            pointsprorichtig = Math.Max(2, qplist.Count);
            buzzerisactive = false;
            buzzerisfreigegeben = true;
            showtabbedout = false;
            fragenindex = 0;
            wronganswers = 0;
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
                case "GetGameInfo": SendModeratorInit(player); break;
                case "GetUpdate": ServerUtils.SendMessage(player, "Quiz", "SpielerUpdate", SpielerUpdate()); break;
                case "GetFragePreview": GetFragePreview(player, data); break;
                case "GetAntwortPreview": SendAntwortPreview(player); break;
                case "ChangePunkteprorichtig": ChangePunkteprorichtig(data); break;
                case "ChangePunkteprofalsche": ChangePunkteprofalsche(data); break;
                case "SetBuzzer": buzzerisactive = bool.Parse(data); if (buzzerisactive) buzzerisfreigegeben = true; ServerUtils.BroadcastMessage("Quiz", "BuzzerIsActive", buzzerisactive.ToString()); break;
                case "SetTabbedout": showtabbedout = bool.Parse(data); break;
                case "PlayerInputAntwort": PlayerInputAntwort(player, data);  break;
                case "PlayerRichtig": PlayerRichtig(data); break;
                case "PlayerFalsch": PlayerFalsch(data); break;
                case "PlayerIstDran": PlayerIstDran(data); break;
                case "PlayerBuzzerFreigeben": PlayerBuzzerFreigeben(); break;
                case "PressBuzzer": PressBuzzer(player); break;
                case "PlayerPointsAdd": PlayerPointsAdd(data); break;
            }
        }

        public static void BroadcastSpielerUpdate()
        {
            ServerUtils.BroadcastMessage("Quiz", "SpielerUpdate", SpielerUpdate());
        }
        private static string SpielerUpdate()
        {
            string msg = "";
            qplist.RemoveAll(p => p.p == null);
            foreach (QuizPlayer p in qplist)
            {
                if (p.p == null)
                    continue;
                if (!Config.moderator.id.Equals(p.p.id))
                    msg += "[TRENNER][ID]" + p.p.id + "[ID][PUNKTE]" + p.points + "[PUNKTE]";
            }
            if (msg.Length > 0)
                msg = msg.Substring("[TRENNER]".Length);
            return msg;
        }
        private static void PressBuzzer(Player p)
        {
            if (!buzzerisactive)
                return;
            if (!buzzerisfreigegeben)
                return;
            buzzerisfreigegeben = false;
            QuizPlayer.GetByID(p.id.ToString(), qplist).istdran = true;
            ServerUtils.BroadcastMessage("Quiz", "PlayerPressedBuzzer", p.id.ToString());
        }
        #region Moderator
        // TODO: wenn moderator quittet das dann ein spieler joint und aus der liste unten verschwindet
        private static void SendModeratorInit(Player p)
        {
            string msg = pointsprorichtig + "*" + pointsprofalsch + "*" +
                (fragenindex + 1) + "/" + Config.quiz.GetSelected().getFragenCount() + "*" + wronganswers;
            ServerUtils.SendMessage(p, "Quiz", "SetGameInfo", msg);
            SendFragenPreview(p, fragenindex);
        }
        private static void SendFragenPreview(Player p, int index)
        {
            ServerUtils.SendMessage(p, "Quiz", "FragenPreview",
                "Frage:\\n" + Config.quiz.GetSelected().getFrage(index).getFrage() +
                "\\n\\nInfo:\\n" + Config.quiz.GetSelected().getFrage(index).getInfo());
        }
        private static void SendAntwortPreview(Player p)
        {
            ServerUtils.SendMessage(p, "Quiz", "AntwortPreview",
                "Antwort:\n" + Config.quiz.GetSelected().getFrage(fragenindex).getAntwort());
        }
        private static void GetFragePreview(Player p, string data)
        {
            sbyte type = sbyte.Parse(data);
            if ((fragenindex + type) >= 0 && (fragenindex + type) < Config.quiz.GetSelected().getFragenCount())
            {
                fragenindex += type;
            }
            ServerUtils.SendMessage(p, "Quiz", "FragenIndex", (fragenindex + 1) + "/" + Config.quiz.GetSelected().getFragenCount());
            SendFragenPreview(p, fragenindex);
        }
        private static void ChangePunkteprorichtig(string data)
        {
            pointsprorichtig = int.Parse(data);
        }
        private static void ChangePunkteprofalsche(string data)
        {
            pointsprofalsch = int.Parse(data);
        }

        private static void PlayerPointsAdd(string data_s)
        {
            string uuid = data_s.Split('~')[0];
            int type = int.Parse(data_s.Split("~")[1]);
            QuizPlayer.GetByID(uuid, qplist).points += type;
            BroadcastSpielerUpdate();
        }
        private static void PlayerRichtig(string data_s)
        {
            string uuid = data_s;
            QuizPlayer.GetByID(uuid, qplist).points += pointsprorichtig;
            ServerUtils.BroadcastMessage("Quiz", "PlayRichtig", "");
            BroadcastSpielerUpdate();
        }
        private static void PlayerFalsch(string data_s)
        {
            string uuid = data_s;
            foreach (var item in qplist)
                if (!item.p.id.ToString().Equals(uuid))
                    item.points += pointsprofalsch;
            ServerUtils.BroadcastMessage("Quiz", "PlayFalsch", "");
            BroadcastSpielerUpdate();
        }
        private static void PlayerIstDran(string data_s)
        {
            string uuid = data_s.Split('~')[0];
            bool type = bool.Parse(data_s.Split('~')[1]);
            QuizPlayer.GetByID(uuid, qplist).istdran = type;
            if (type)
                buzzerisfreigegeben = false;
            else
            {
                bool allenichtdran = true;
                foreach (var item in qplist)
                {
                    if (item.istdran)
                        allenichtdran = false;
                }
                if (allenichtdran)
                    buzzerisfreigegeben = true;
            }
            ServerUtils.BroadcastMessage("Quiz", "PlayerIstDran", data_s);
        }
        private static void PlayerBuzzerFreigeben()
        {
            buzzerisfreigegeben = true;
            foreach (var item in qplist)
            {
                item.istdran = false;
            }
            ServerUtils.BroadcastMessage("Quiz", "PlayerBuzzerFreigeben", "");
        }
        private static void PlayerInputAntwort(Player p, string data)
        {
            QuizPlayer.GetByID(p.id.ToString(), qplist).antwort = data;
            ServerUtils.SendMessage(Config.moderator, "Quiz", "ModShowPlayerInputAntwort", p.id + "~" + data);
        }
        #endregion
    }

    internal class QuizPlayer
    {
        public Player p;
        public int points;
        public bool istdran;
        public string antwort;

        public QuizPlayer(Player p)
        {
            this.p = p;
            this.points = 0;
            istdran = false;
            antwort = "";
        }

        public static QuizPlayer GetByID(string uuid, List<QuizPlayer> list)
        {
            foreach (var item in list)
            {
                if (item.p.id.ToString().Equals(uuid))
                    return item;
            }
            return null;
        }
    }

    internal class Quiz
    {
        public const byte min_player = 3;
        public const byte max_player = 11; // inklusive Host
        private List<QuizSet> sets;
        private QuizSet selected;
        private int available_sets;

        public Quiz()
        {
            this.sets = new List<QuizSet>();
            this.sets.Add(new QuizSet());
            foreach (var item in Directory.EnumerateFiles("Resources/Quiz/"))
                this.sets.Add(new QuizSet(Path.GetFileName(item), File.ReadAllLines(item)));

            if (sets.Count > 0)
                selected = sets[0];

            this.available_sets = this.sets.Count;

            Utils.Log(LogType.Info, "Quiz wurde initialisiert.");
        }

        public List<QuizSet> GetSets() { return this.sets; }
        public string GetSetsAsString() { return string.Join("[TRENNER]", GetSets()); }
        public QuizSet GetSet(int index) { return this.sets[index]; }
        public QuizSet GetSelected() { return this.selected; }
        public void SetSelected(QuizSet set) { this.selected = set; }
        public void SetSelected(int set) { SetSelected(GetSets()[set]); }
        public string GetAvailableSets() { return this.available_sets.ToString("N0", new CultureInfo("de-DE")); }
        public void AddAvailableSets(int counter) { this.available_sets += counter; }
    }

    internal class QuizSet
    {
        public string name;
        public List<QuizItem> fragen;

        public QuizSet()
        {
            name = "Freestyle";
            fragen = new List<QuizItem>();
            fragen.Add(new QuizItem("Freestyle", "Freestyle", "Freestyle"));
        }

        public QuizSet(string name, string[] inhalt)
        {
            this.name = name;
            Utils.Log(LogType.Debug, name);
            fragen = new List<QuizItem>();
            //string[] temp = inhalt.Split('~');
            string[] temp = inhalt;
            for (int i = 0; i < temp.Length;)
            {
                string frage = temp[i];
                i++;
                string antwort = temp[i].Replace("\\n", "\n");
                i++;
                string info = temp[i].Replace("\\n", "\n");
                i++;

                if (frage.StartsWith("Frage"))
                    frage = frage.Substring("Frage".Length);
                if (frage.StartsWith(":"))
                    frage = frage.Substring(":".Length);
                if (frage.StartsWith(" "))
                    frage = frage.Substring(" ".Length);

                if (antwort.StartsWith("Antwort"))
                    antwort = antwort.Substring("Antwort".Length);
                if (antwort.StartsWith(":"))
                    antwort = antwort.Substring(":".Length);
                if (antwort.StartsWith(" "))
                    antwort = antwort.Substring(" ".Length);

                if (info.StartsWith("Info"))
                    info = info.Substring("Info".Length);
                if (info.StartsWith(":"))
                    info = info.Substring(":".Length);
                if (info.StartsWith(" "))
                    info = info.Substring(" ".Length);

                fragen.Add(new QuizItem(frage, antwort, info));
            }
        }

        public override string ToString() { return name; }
        public string getName() { return this.name; }
        public List<QuizItem> getFragen() { return this.fragen; }
        public int getFragenCount() { return this.fragen.Count; }
        public QuizItem getFrage(int index) { return this.fragen[index]; }
    }

    internal class QuizItem
    {
        private string frage;
        private string antwort;
        private string info;

        public QuizItem(string frage, string antwort, string info)
        {
            this.frage = frage.Replace("\n", "\\n");
            this.antwort = antwort.Replace("\n", "\\n");
            this.info = info.Replace("\n", "\\n");
        }

        public string getFrage() { return this.frage; }
        public void setFrage(string frage) { this.frage = frage; }
        public string getAntwort() { return this.antwort; }
        public void setAntwort(string antwort) { this.antwort = antwort; }
        public string getInfo() { return this.info; }
        public void setInfo(string info) { this.info = info; }
    }
}
