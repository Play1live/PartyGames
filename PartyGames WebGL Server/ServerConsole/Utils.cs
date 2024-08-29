using Fleck;
using ServerConsole.Games;
using System.Runtime.CompilerServices;

namespace ServerConsole
{
    internal class Utils
    {
        public static string EncryptDecrypt(string text, int key)
        {
            char[] chars = text.ToCharArray();  // Konvertiere den Text in ein Array von Zeichen
            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = (char)(chars[i] ^ key);  // Verwende XOR auf jedes Zeichen mit dem Schlüssel
            }
            return new string(chars);  // Konvertiere das Array zurück in einen String
        }
        public static void Log(LogType type, object text,
            [CallerMemberName] string methodName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (type >= Config.logtype)
            {
                // Setze die Farbe basierend auf dem LogType
                switch (type)
                {
                    case LogType.Trace:
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        break;
                    case LogType.Debug:
                        Console.ForegroundColor = ConsoleColor.Gray;
                        break;
                    case LogType.Info:
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case LogType.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case LogType.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    case LogType.Fatal:
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        break;
                }

                string className = System.IO.Path.GetFileNameWithoutExtension(filePath);
                Console.WriteLine($"[{className}.{methodName} (Zeile {lineNumber})]: {text.ToString()}");
            }
        }
    }
    public enum LogType
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warning = 3,
        Error = 4,
        Fatal = 5,
        None = 6,
    }
    internal class ServerUtils
    {
        public static void OnSocketOpened(IWebSocketConnection socket)
        {
            Utils.Log(LogType.Info, $"Connection opened Client ID: {socket.ConnectionInfo.Id} Client IP: {socket.ConnectionInfo.ClientIpAddress} Port: {socket.ConnectionInfo.ClientPort}");
        }
        public static void OnErrorHandle(Exception error, IWebSocketConnection socket)
        {
            // Log the error details and the ID of the client that caused the error
            Utils.Log(LogType.Error, $"Error from {socket.ConnectionInfo.Id}: {error.Message}");

            // Du könntest hier auch entscheiden, ob du zusätzliche Schritte unternehmen willst,
            // wie das Senden einer Fehlermeldung an den Client oder das Schließen der Verbindung.
            if (!socket.IsAvailable)
            {
                Utils.Log(LogType.Info, "Socket is not available, closing connection.");
                socket.Close();
            }
        }
        public static void OnSocketClose(IWebSocketConnection socket)
        {
            Utils.Log(LogType.Info, $"Socket closed: {socket.ConnectionInfo.Id}");
            Player deadplayer = Player.getPlayerBySocket(socket);
            if (deadplayer == null)
                return;
            BroadcastMessage("ALLE", "#DEADCLIENT", deadplayer.id.ToString());
            Config.players.Remove(deadplayer);
            if (Config.moderator == deadplayer)
            {
                if (Config.players.Count == 0)
                {
                    Config.moderator = null;
                }
                else
                {
                    Config.moderator = Config.players[new Random().Next(0, Config.players.Count)];
                    ServerUtils.SendMessage(Config.moderator, "Lobby", "ClientSetModerator", "");
                }
            }

            if (Config.game_title.Equals("Lobby"))
            {
                Lobby.BroadcastSpielerUpdate();
                ServerUtils.BroadcastMessage("Lobby", "PlayDisconnectSound", "");
            }
            else if (Config.game_title.Equals("Tabu"))
            {
                TabuHandler.BroadcastSpielerUpdate();
                ServerUtils.BroadcastMessage("Tabu", "PlayDisconnectSound", "");
            }
            else if (Config.game_title.Equals("Quiz"))
            {
                QuizHandler.BroadcastSpielerUpdate();
                ServerUtils.BroadcastMessage("Quiz", "PlayDisconnectSound", "");
            }

            if (Config.players.Count == 0)
                Config.game_title = "Lobby";
        }
        public static void OnSocketMessage(string message, IWebSocketConnection socket)
        {
            Utils.Log(LogType.Debug, $"Received message: {message}");

            if (message.Split('|').Length < 3)
                return;
            string gametitle = message.Split('|')[0];
            string command = message.Split('|')[1];
            string data = message.Split('|')[2];

            // in die Einzelnen Klassen laden
            switch (gametitle)
            {
                default:
                    Utils.Log(LogType.Warning, "Unknown Gametitle: " + gametitle);
                    break;
                case "ALLE": Universell.OnCommand(socket, command, data); break;
                case "Lobby": Lobby.OnCommand(socket, command, data); break;

                case "Quiz": QuizHandler.OnCommand(socket, command, data); break;
                case "Tabu": TabuHandler.OnCommand(socket, command, data); break;
            }
        }


        // Methode zum Senden einer Nachricht an alle Clients
        public static void BroadcastMessage(string gametitle, string cmd, string message)
        {
            Utils.Log(LogType.Debug, "Broadcast message: " + gametitle + "|" + cmd + "|" + message);
            List<Player> deadclients = new List<Player>();
            foreach (var client in Config.players)
            {
                if (client.websocket.IsAvailable)
                {
                    client.websocket.Send(gametitle + "|" + cmd + "|" + message);
                }
            }
        }
        // Methode zum Senden einer Nachricht an einen Client
        public static void SendMessage(Player player, string gametitle, string cmd, string message)
        {
            if (player != null)
            {
                Utils.Log(LogType.Debug, "Send message: " + player.name + ">" + gametitle + "|" + cmd + "|" + message);
                player.websocket.Send(gametitle + "|" + cmd + "|" + message);
            }
            else
                Utils.Log(LogType.Debug, "Send message: " + "unknown" + ">" + gametitle + "|" + cmd + "|" + message);
        }
    }
}
