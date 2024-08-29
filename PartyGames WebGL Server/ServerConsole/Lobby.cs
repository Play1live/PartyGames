using Fleck;
using ServerConsole.Games;

namespace ServerConsole
{
    internal class Lobby
    {
        public static void StartLobby()
        {
            Config.game_title = "Lobby";
            BroadcastSpielerUpdate();
        }
        public static void OnCommand(IWebSocketConnection socket, string cmd, string data)
        {
            Player player = Player.getPlayerBySocket(socket);
            switch (cmd)
            {
                default:
                    Utils.Log(LogType.Warning, "Unbekannter Befehl: " + cmd + " " + data);
                    return;
                case "GetSpielerUpdate": BroadcastSpielerUpdate(); break;
                case "ClientSetName": SetName(socket, data); break;
                case "SetIcon": SetIcon(player, data); break;
                case "ChangeModerator": ChangeModerator(player, data); break;
                case "GetPingTime": ServerUtils.SendMessage(player, "Lobby", "SetPingTime", ""); break;
                case "PingTime": ServerUtils.BroadcastMessage("Lobby", "PingUpdate", player.name + "#" + data); break;
                case "GetSpielData": ServerUtils.SendMessage(player, "Lobby", "SetSpielData", GetSpielData()); break;
                case "StartGame": StartGame(player, data); break;
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
        private static void SetName(IWebSocketConnection socket, string data)
        {
            Utils.Log(LogType.Debug, socket.ConnectionInfo.ClientIpAddress + " Name: " + data);
            // Spieler existiert noch nicht
            Player p1 = Player.getPlayerBySocket(socket);
            Guid uuid = Guid.Parse(data.Split('#')[0]);
            string name = data.Split('#')[1];
            if (p1 != null)
            {
                return;
            }
            else
            {
                if (Config.players.Count >= 20)
                {
                    Utils.Log(LogType.Info, "Maximale Anzahl an Clients bereits verbunden. Client wird abgelehnt.");
                    socket.Close();
                    return;
                }

                // Spieler name ist
                Player player = new Player(uuid, socket);
                player.name = name;
                for (int i = 0; i < Config.players.Count + 20; i++)
                {
                    if (Player.getPlayerByName(player.name) == null)
                        break;
                    player.name += new Random().Next(0, 10);
                }
                Config.players.Add(player);
                if (Config.players.Count == 1 || Config.moderator == null)
                    Config.moderator = player;
                ServerUtils.SendMessage(player, "Startup", "ClientSetName", player.ToString());
                ServerUtils.SendMessage(player, "Startup", "HideCommunication", "" + Config.hide_communication);
                BroadcastSpielerUpdate();
                if (player == Config.moderator)
                    ServerUtils.SendMessage(Config.moderator, "Lobby", "ClientSetModerator", "");
                ServerUtils.BroadcastMessage("Lobby", "PlayConnectSound", "");
            }
        }
        private static void SetIcon(Player p, string data)
        {
            Utils.Log(LogType.Debug, p.name + " wants new Icon: " + data);
            bool unused_icon = false;
            int selected = int.Parse(data.Split('#')[0]);
            List<string> all_icons = new List<string>();
            all_icons.AddRange(data.Split('#')[1].Split(','));

            for (int i = 0; i < all_icons.Count; i++)
            {
                unused_icon = true;
                foreach (var item in Config.players)
                {
                    if (item.icon_id == selected)
                    {
                        selected = (selected + 1) % all_icons.Count;
                        unused_icon = false;
                        break;
                    }
                }
                if (unused_icon)
                    break;
            }

            p.icon_id = selected;
            BroadcastSpielerUpdate();
        }
        private static void ChangeModerator(Player p, string data)
        {
            if (Config.moderator != p)
                return;
            Player new_mod = Player.getPlayerByName(data);
            if (new_mod != null)
            {
                Utils.Log(LogType.Info, new_mod.name + " " + Config.moderator.name);
                Config.moderator = new_mod;
                ServerUtils.SendMessage(p, "Lobby", "ClientUnSetModerator", "");
                ServerUtils.SendMessage(Config.moderator, "Lobby", "ClientSetModerator", "");
            }
        }
        // TODO: Wenn Host spielauswahl öffnet
        // schicke ich ihm was bei mir alles da ist 
        // das wird bei ihm aktualisiert (ob nun mit 1 oder 2 auswahlfeldern)
        // der soll die möglichkeit haben eine datei hochzuladen
        private static string GetSpielData()
        {
            //gamedata.Add("[TYPE]Select_1,Upload[TYPE][SELECTION_1]...[TRENNER]...[SELECTION_1]");
            //gamedata.Add("[TYPE]Select_1[TYPE][SELECTION_1]...[TRENNER]...[SELECTION_1]");
            //gamedata.Add("[TYPE]Upload[TYPE]");

            List<string> gamedata = new List<string>();
            // Moderierte Spiele
            gamedata.Add("[TYPE]Text[TYPE][TITLE]<b><i>Moderierte Spiele</i></b>[TITLE]");
            // Flaggen
            // Quiz
            gamedata.Add("[TYPE]Select_1,Upload[TYPE][TITLE]Quiz[TITLE][SPIELER_ANZ]" + Quiz.min_player + "-" + Quiz.max_player + "[SPIELER_ANZ][SELECTION_1]" + Config.quiz.GetSetsAsString() + "[SELECTION_1]");
            // Listen
            // Mosaik
            // WerBietetMehr
            // Geheimwörter
            // Auktion
            // Sloxikon
            // Jeopardy
            // Sabotage
            // Unmoderierte Spiele
            gamedata.Add("[TYPE]Text[TYPE][TITLE]<b><i>Unmoderierte Spiele</i></b>[TITLE]");
            // MenschÄrgerDichNicht
            // Kniffel
            // Tabu
            gamedata.Add("[TYPE]Select_2[TYPE][TITLE]Tabu[TITLE][SPIELER_ANZ]" + Tabu.min_player + "-" + Tabu.max_player + "[SPIELER_ANZ][SELECTION_1]" + Config.tabu.GetTypesAsString() + "[SELECTION_1][SELECTION_2]" + Config.tabu.GetSetsAsString() + "[SELECTION_2]");
            // WerBinIch
            return string.Join("~", gamedata);
        }
        private static void StartGame(Player p, string data)
        {
            Utils.Log(LogType.Debug, p.name + " > " + data);
            List<string> temp = data.Split('#').ToList();
            string game = temp[0];
            temp.RemoveAt(0);
            List<int> values = temp.ConvertAll(int.Parse);

            switch (game)
            {
                default: Utils.Log(LogType.Warning, "Unbekanntes Spiel: " + game); break;
                case "Tabu":
                    Config.tabu.SetType(values[0]);
                    Config.tabu.SetSelected(values[1]);
                    TabuHandler.StartGame();
                    ServerUtils.BroadcastMessage("Lobby", "StartGame", game);
                    break;
                case "Quiz":
                    Config.quiz.SetSelected(values[0]);
                    QuizHandler.StartGame();
                    ServerUtils.BroadcastMessage("Lobby", "StartGame", game);
                    break;
            }
        }
    }
}
