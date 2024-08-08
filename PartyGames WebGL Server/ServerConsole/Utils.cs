using Fleck;
using ServerConsole.Games;

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
        // TODO: Überarbeiten, mit ForceShow, und Traceback mit chatgpt mit klasse und methode
        public static void Log(string text)
        {
            if (!Config.hide_communication)
                Console.WriteLine(text);
        }
    }

    internal class ServerUtils
    {
        public static void OnSocketOpened(IWebSocketConnection socket)
        {
            Utils.Log("Connection opened");
            Utils.Log($"Client ID: {socket.ConnectionInfo.Id}");
            Utils.Log($"Client IP: {socket.ConnectionInfo.ClientIpAddress}");
            Utils.Log($"Port: {socket.ConnectionInfo.ClientPort}");
            Utils.Log($"Is Available: {socket.IsAvailable}");
        }
        public static void OnErrorHandle(Exception error, IWebSocketConnection socket)
        {
            // Log the error details and the ID of the client that caused the error
            Utils.Log($"Error from {socket.ConnectionInfo.Id}: {error.Message}");

            // Du könntest hier auch entscheiden, ob du zusätzliche Schritte unternehmen willst,
            // wie das Senden einer Fehlermeldung an den Client oder das Schließen der Verbindung.
            if (!socket.IsAvailable)
            {
                Utils.Log("Socket is not available, closing connection.");
                socket.Close();
            }
        }
        public static void OnSocketClose(IWebSocketConnection socket)
        {
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
                }
                ServerUtils.SendMessage(Config.moderator, "Lobby", "ClientSetModerator", "");
            }

            if (Config.game_title.Equals("Lobby"))
            {
                Lobby.BroadcastSpielerUpdate();
                ServerUtils.BroadcastMessage("Lobby", "PlayDisconnectSound", "");
            }
        }
        public static void OnSocketMessage(string message, IWebSocketConnection socket)
        {
            Utils.Log($"Received message: {message}");

            if (message.Split('|').Length < 3)
                return;
            string gametitle = message.Split('|')[0];
            string command = message.Split('|')[1];
            string data = message.Split('|')[2];

            // in die Einzelnen Klassen laden
            switch (gametitle)
            {
                default:
                    Utils.Log("ERROR unknown gametitle");
                    break;
                case "ALLE": Universell.OnCommand(socket, command, data); break;
                case "Lobby": Lobby.OnCommand(socket, command, data); break;

                case "Tabu": TabuHandler.OnCommand(socket, command, data); break;
            }
        }


        // Methode zum Senden einer Nachricht an alle Clients
        public static void BroadcastMessage(string gametitle, string cmd, string message)
        {
            List<Player> deadclients = new List<Player>();
            foreach (var client in Config.players)
            {
                if (client.websocket.IsAvailable)
                    client.websocket.Send(gametitle + "|" + cmd + "|" + message);
            }
        }
        // Methode zum Senden einer Nachricht an einen Client
        public static void SendMessage(Player player, string gametitle, string cmd, string message)
        {
            if (player != null)
            {
                Utils.Log("Send message: " + player.name + ">" + gametitle + "|" + cmd + "|" + message);
                player.websocket.Send(gametitle + "|" + cmd + "|" + message);
            }
            else
                Utils.Log("Send message: " + "unknown" + ">" + gametitle + "|" + cmd + "|" + message);
        }
    }
}
