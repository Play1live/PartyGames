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
                    Utils.Log("Unbekannter Befehl: " + cmd + " " + data);
                    return;
                case "Ping": ServerUtils.SendMessage(player, "ALLE", "Pong", ""); break;
                case "SpielVerlassen": ServerUtils.BroadcastMessage("ALLE", "SpielVerlassen", ""); Lobby.StartLobby(); break;
            }
        }
    }
}
