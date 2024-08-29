using Fleck;
using ServerConsole;
using ServerConsole.Games;

class Program
{
    static void Main(string[] args)
    {
        Config.players = new List<Player>();
        Config.game_title = "Lobby";

        Config.tabu = new Tabu();
        Config.quiz = new Quiz();

        Config.server = new WebSocketServer("ws://0.0.0.0:14002");
        Config.server.Start(socket =>
        {
            socket.OnOpen = () => ServerUtils.OnSocketOpened(socket);
            socket.OnMessage = message => ServerUtils.OnSocketMessage(message, socket);
            socket.OnError = error => ServerUtils.OnErrorHandle(error, socket);
            socket.OnClose = () => ServerUtils.OnSocketClose(socket);

            socket.OnPing = (data) => { socket.SendPong(data); };
            socket.OnPong = (data) => { };
        });

        Utils.Log(LogType.Info, "WebSocket server started at ws://0.0.0.0:14002");
        Lobby.StartLobby();
        Console.ReadKey();  // Hält die Konsole offen
    }
}