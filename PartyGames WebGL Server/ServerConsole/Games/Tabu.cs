using Fleck;
using System.Globalization;

namespace ServerConsole.Games
{
    internal class TabuHandler
    {
        public static void StartGame()
        {

        }

        public static void OnCommand(IWebSocketConnection socket, string cmd, string data)
        {
            Player player = Player.getPlayerBySocket(socket);
            switch (cmd)
            {
                default:
                    Utils.Log("Unbekannter Befehl: " + cmd + " " + data);
                    return;
                case "GetSpielerUpdate": BroadcastSpielerUpdate(); break;
            }
        }

        public static void BroadcastSpielerUpdate()
        {
            ServerUtils.BroadcastMessage("Lobby", "SpielerUpdate", SpielerUpdate());
        }
        private static string SpielerUpdate()
        {
            string ausgabe = "";
            foreach (var item in Config.players)
            {
                ausgabe += "*" + item.ToString();
            }
            if (ausgabe.Length > 0)
                ausgabe = ausgabe.Substring(1);
            return ausgabe;
        }

        #region Moderator
        #endregion
    }

    internal class Tabu
    {
        public const byte min_player = 4;
        public const byte max_player = 20;
        public const byte tabu_word_count = 6;
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

            Utils.Log("Tabu wurde initialisiert.");
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

        public TabuSet(string name, string inhalt, Tabu parent)
        {
            this.name = name;
            this.need_to_safe = false;
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
                    Utils.Log("TabuSet - Wort kommt doppelt vor: " + word);
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
                Utils.Log("TabuSet - File: " + name + " wurde gespeichert. " + 
                    Path.GetFullPath(@"Resources\Tabu\" + this.name + ".txt") + " " + this.worte.Count);
            }
#endif
        }
        public override string ToString()
        {
            return name;
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
                    Utils.Log("TabuItem - Wort enthält # " + item);
                    set.need_to_safe = true;
                    this.tabus.Add(item.Replace("#", ""));
                }
                else
                {
                    Utils.Log("TabuItem - Dopplung bei Wort: " + this.word + " >> " + item);
                    set.need_to_safe = true;
                }
            }
        }
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
        public string GenTabuListAsString() { return string.Join(".", GenTabuList()); }

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
