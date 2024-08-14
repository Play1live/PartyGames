using Fleck;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerConsole
{
    internal class Universell
    {
        public static void OnCommand(IWebSocketConnection socket, string cmd, string data)
        {
            Player player = Player.getPlayerBySocket(socket);

            switch (cmd)
            {
                default:
                    Utils.Log(LogType.Warning, "Unbekannter Befehl: " + cmd + " " + data);
                    return;
                case "Ping": ServerUtils.SendMessage(player, "ALLE", "Pong", ""); break;
                case "SpielVerlassen": ServerUtils.BroadcastMessage("ALLE", "SpielVerlassen", ""); Lobby.StartLobby(); break;
                case "GetActiveGame": ServerUtils.SendMessage(player, "ALLE", "SetActiveGame", Config.game_title); break;
                case "UnknownPlayerGetData": UnknownPlayerGetData(player, data); break;
            }
        }
        
        private static void UnknownPlayerGetData(Player p, string data)
        {
            Player needed_p = null;
            if (data.Split('*')[0].Equals("guid"))
                needed_p = Player.getPlayerById(Guid.Parse(data.Split('*')[1]));
            else if (data.Split('*')[0].Equals("name"))
                needed_p = Player.getPlayerByName(data.Split('*')[1]);
            else
            {
                Utils.Log(LogType.Error, "Art der Spielersuche unbekannt: " + data);
                return;
            }

            if (needed_p == null)
            {
                Utils.Log(LogType.Error, "Spieler nicht gefunden unbekannt: " + data);
                return;
            }

            ServerUtils.SendMessage(p, "ALLE", "UnknownPlayerSetData", 
                Config.players.IndexOf(needed_p) + "*" + needed_p.id.ToString() + "*" + needed_p.name + "*" + needed_p.icon_id);
        }
    }
}
