using Fleck;

namespace ServerConsole
{
    internal class Player
    {
        public Guid id;
        public string name;
        public IWebSocketConnection websocket;
        public int icon_id;

        public Player(Guid id, IWebSocketConnection websocket)
        {
            this.id = id;
            this.name = string.Empty;
            this.websocket = websocket;
            this.icon_id = 0;
        }

        public override string ToString()
        {
            return id.ToString() + "#" + name + "#" + icon_id;
        }

        public static Player getPlayerById(Guid id)
        {
            foreach (var player in Config.players)
                if (player.id == id)
                    return player;
            return null;
        }
        public static Player getPlayerById(Guid id, List<Player> players)
        {
            foreach (var player in players)
                if (player.id == id)
                    return player;
            return null;
        }
        public static Player getPlayerByName(string name)
        {
            foreach (var player in Config.players)
                if (player.name == name)
                    return player;
            return null;
        }
        public static Player getPlayerByName(string name, List<Player> players)
        {
            foreach (var player in players)
                if (player.name == name)
                    return player;
            return null;
        }
        public static Player getPlayerBySocket(IWebSocketConnection socket)
        {
            foreach (var player in Config.players)
                if (player.websocket == socket)
                    return player;
            return null;
        }
        public static Player getPlayerBySocket(IWebSocketConnection socket, List<Player> players)
        {
            foreach (var player in players)
                if (player.websocket == socket)
                    return player;
            return null;
        }
    }
}
