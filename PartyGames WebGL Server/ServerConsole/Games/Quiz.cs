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

        public static void StartGame()
        {
            Utils.Log(LogType.Trace, "StartGame");
            Config.game_title = "Quiz";
            qplist = new List<QuizPlayer>();
            foreach (Player p in Config.players)
                qplist.Add(new QuizPlayer(p));
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
                case "GetUpdate": ServerUtils.SendMessage(player, "Tabu", "SpielerUpdate", SpielerUpdate()); break;
            }
        }

        public static void BroadcastSpielerUpdate()
        {
            ServerUtils.BroadcastMessage("Quiz", "SpielerUpdate", SpielerUpdate());
        }
        private static string SpielerUpdate()
        {
            string msg = "";
            foreach (QuizPlayer p in qplist)
            {
                if (!Config.moderator.id.Equals(p.p.id))
                    msg += "[TRENNER][ID]" + p.p.id + "[ID][PUNKTE]" + p.points + "[PUNKTE]";
            }
            if (msg.Length > 0)
                msg = msg.Substring("[TRENNER]".Length);
            return msg;
        }
        #region Moderator
        #endregion
    }

    internal class QuizPlayer
    {
        public Player p;
        public int points;

        public QuizPlayer(Player p)
        {
            this.p = p;
            this.points = 0;
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
                this.sets.Add(new QuizSet(Path.GetFileName(item), File.ReadAllText(item)));

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
        public string GetAvailableSets() { return this.available_sets.ToString("N", new CultureInfo("de-DE")); }
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

        public QuizSet(string name, string inhalt)
        {
            this.name = name;
            fragen = new List<QuizItem>();
            string[] temp = inhalt.Split('~');
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
            this.frage = frage;
            this.antwort = antwort;
            this.info = info;
        }

        public string getFrage() { return this.frage; }
        public void setFrage(string frage) { this.frage = frage; }
        public string getAntwort() { return this.antwort; }
        public void setAntwort(string antwort) { this.antwort = antwort; }
        public string getInfo() { return this.info; }
        public void setInfo(string info) { this.info = info; }
    }
}
