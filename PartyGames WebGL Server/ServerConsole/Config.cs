using Fleck;
using ServerConsole.Games;

namespace ServerConsole
{
    internal class Config
    {
        public static LogType logtype = LogType.Trace;
        public static bool hide_communication = false;

        public static WebSocketServer server;
        public static List<Player> players;
        public static Player moderator;
        public static string game_title;

        public static Tabu tabu;
        public static Quiz quiz;
    }
}
