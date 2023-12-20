using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WerBinIchServer : MonoBehaviour
{
    int connectedcount;
    bool[] PlayerConnected;
    [SerializeField] AudioSource DisconnectSound;

    private GameObject[,] PlayerAnzeige;
    private string[] names;

    // Start is called before the first frame update
    void Start()
    {
        PlayerConnected = new bool[Config.SERVER_MAX_CONNECTIONS + 1];
        PlayerConnected[0] = true;
        InitGame();
    }

    // Update is called once per frame
    void Update()
    {
        #region Server
        if (!Config.SERVER_STARTED)
        {
            SceneManager.LoadSceneAsync("Startup");
            return;
        }
        foreach (Player spieler in Config.PLAYERLIST)
        {
            if (spieler.isConnected == false)
                continue;

            #region Sucht nach neuen Nachrichten
            if (spieler.isConnected == true)
            {
                NetworkStream stream = spieler.tcp.GetStream();
                if (stream.DataAvailable)
                {
                    StreamReader reader = new StreamReader(stream);
                    string data = reader.ReadLine();

                    if (data != null)
                        OnIncommingData(spieler, data);
                }
            }
            #endregion
        }
        #endregion
    }

    private void OnApplicationQuit()
    {
        ServerUtils.BroadcastImmediate(Config.GLOBAL_TITLE + "#ServerClosed");
        Logging.log(Logging.LogType.Normal, "WerBinIchServer", "OnApplicationQuit", "Server wird geschlossen.");
        Config.SERVER_TCP.Server.Close();
    }

    private void OnApplicationFocus(bool focus)
    {
        ServerUtils.BroadcastImmediate("#ClientFocus 0~" + focus);
    }

    #region Server Stuff
    /// <summary>
    /// Einkommende Nachrichten die von Spielern an den Server gesendet werden
    /// </summary>
    /// <param name="spieler">Spieler</param>
    /// <param name="data">Nachricht</param>
    private void OnIncommingData(Player spieler, string data)
    {
        if (!data.StartsWith(Config.GAME_TITLE) && !data.StartsWith(Config.GLOBAL_TITLE))
        {
            Logging.log(Logging.LogType.Warning, "", "OnIncommingData", "Wrong Prefix: " + data);
            return;
        }
        data = Utils.ParseCMDGameTitle(data, Config.isServer);

        string cmd;
        if (data.Contains(" "))
        {
            cmd = data.Split(' ')[0];
            data = data.Substring(cmd.Length + 1);
        }
        else
            cmd = data;

        Commands(spieler, data, cmd);
    }
    /// <summary>
    /// Einkommende Befehle von Spielern
    /// </summary>
    /// <param name="player">Spieler</param>
    /// <param name="data">Befehlsargumente</param>
    /// <param name="cmd">Befehl</param>
    private void Commands(Player player, string data, string cmd)
    {
        // Zeigt alle einkommenden Nachrichten an
        Logging.log(Logging.LogType.Debug, "WerBinIchServer", "Commands", "Eingehende Nachricht: " + player.name + " " + player.id + " -> " + cmd + "   ---   " + data);
        // Sucht nach Command
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "WerBinIchServer", "Commands", "Unkown Command: " + cmd + " -> " + data);
                break;

            case "#ClientClosed":
                DisconnectSound.Play();
                connectedcount = 0;
                ServerUtils.ClientClosed(player);
                UpdateSpielerBroadcast();
                break;
            case "#TestConnection":
                break;
            case "#ClientFocusChange":
                ServerUtils.BroadcastImmediate("#ClientFocus " + player.id + "~" + data);
                PlayerAnzeige[player.id, 2].SetActive(!bool.Parse(data));
                break;

            case "#JoinWerBinIch":
                PlayerConnected[player.id] = true;
                UpdateSpielerBroadcast();
                break;
            case "#ClientEnterText":
                ClientEnterText(data);
                break;
        }
    }
    #endregion
    /// <summary>
    /// Spiel Verlassen & Zurück in die Lobby laden
    /// </summary>
    public void SpielVerlassenButton()
    {
        Logging.log(Logging.LogType.Debug, "WerBinIchServer", "SpielVerlassenButton", "Spiel wird beendet. Lädt ins Hauptmenü.");
        Player[] plist = new Player[Config.PLAYERLIST.Length + 1];
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
            plist[i] = Config.PLAYERLIST[i];
        plist[Config.PLAYERLIST.Length] = Config.SERVER_PLAYER;
        ServerUtils.LoadKronen(plist);
        ServerUtils.BroadcastImmediate("#ZurueckInsHauptmenue");
    }

    #region GameLogic
    /// <summary>
    /// Initialisiert die Game Elemente
    /// </summary>
    private void InitGame()
    {
        PlayerAnzeige = new GameObject[9, 4];
        names = new string[9];
        for (int i = 0; i < 9; i++)
        {
            names[i] = "";
            GameObject p = GameObject.Find("Player (" + i + ")");
            PlayerAnzeige[i, 0] = p.transform.GetChild(0).gameObject; // GesuchterName
            PlayerAnzeige[i, 0].SetActive(true);
            PlayerAnzeige[i, 1] = p.transform.GetChild(1).gameObject; // Icon
            PlayerAnzeige[i, 1].SetActive(true);
            PlayerAnzeige[i, 2] = p.transform.GetChild(2).gameObject; // Ausgetabbt
            PlayerAnzeige[i, 2].SetActive(false);
            PlayerAnzeige[i, 3] = p.transform.GetChild(3).GetChild(1).gameObject; // Name
            p.SetActive(false);
        }
        PlayerAnzeige[Config.SERVER_PLAYER.id, 0].SetActive(false);

        UpdateSpieler();
        StartCoroutine(UpdateTextAsync());
    }
    /// <summary>
    /// Sendet aktualisierte Spielerinfos an alle Spieler
    /// </summary>
    private void UpdateSpielerBroadcast()
    {
        ServerUtils.BroadcastImmediate(Config.GAME_TITLE + UpdateSpieler());
    }
    /// <summary>
    /// Aktualisiert die Spieler Anzeige Informationen & gibt diese als Text zurück
    /// </summary>
    /// <returns>#UpdateSpieler ...</returns>
    private string UpdateSpieler()
    {
        string msg = "#UpdateSpieler [ID]0[ID][PUNKTE]" + Config.SERVER_PLAYER.points + "[PUNKTE][ONLINE]true[ONLINE][NAME]"+ Config.SERVER_PLAYER.name + "[NAME]";
        int connectedplayer = 0;
        PlayerAnzeige[0, 0].transform.parent.gameObject.SetActive(true);
        PlayerAnzeige[0, 1].GetComponent<Image>().sprite = Config.SERVER_PLAYER.icon2.icon;
        PlayerAnzeige[0, 3].GetComponent<TMP_Text>().text = Config.SERVER_PLAYER.name;

        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            Player p = Config.PLAYERLIST[i];
            msg += "[TRENNER][ID]" + p.id + "[ID][PUNKTE]" + p.points + "[PUNKTE][ONLINE]" + p.isConnected + "[ONLINE][NAME]"+p.name+"[NAME]";
            if (p.isConnected && PlayerConnected[i])
            {
                connectedplayer++;
                PlayerAnzeige[i+1, 0].transform.parent.gameObject.SetActive(true);
                PlayerAnzeige[i+1, 1].GetComponent<Image>().sprite = p.icon2.icon;
                PlayerAnzeige[i+1, 3].GetComponent<TMP_Text>().text = p.name;
            }
            else
                PlayerAnzeige[i+1, 0].transform.parent.gameObject.SetActive(false);
        }

        if (connectedplayer < connectedcount)
        {
            msg = msg + "[DISCONNECT]";
            DisconnectSound.Play();
        }
        connectedcount = connectedplayer;
        Logging.log(Logging.LogType.Debug, "WerBinIchServer", "UpdateSpieler", msg);
        return msg;
    }
    /// <summary>
    /// Ein Client gibt Inhalte an, die an alle weiter geleitet werden müssen
    /// </summary>
    /// <param name="data"></param>
    private void ClientEnterText(string data)
    {
        names[int.Parse(data.Split('|')[0])] = data.Split('|')[1];
        UpdateText();
    }
    /// <summary>
    /// Server gibt Inhalte ein
    /// </summary>
    /// <param name="input"></param>
    public void ServerEnterText(TMP_InputField input)
    {
        if (!Config.SERVER_STARTED)
            return;
        names[int.Parse(input.transform.parent.name.Replace("Player (", "").Replace(")", ""))] = input.text;
        UpdateText();
    }
    /// <summary>
    /// Aktualisiert die Texteingaben, die erraten werden müssen
    /// </summary>
    private string UpdateTextAusgabe;
    private void UpdateText()
    {
        UpdateTextAusgabe = "";
        for (int i = 0; i < names.Length; i++)
        {
            UpdateTextAusgabe += "[" + i + "]" + names[i] + "[" + i + "]";
            PlayerAnzeige[i, 0].GetComponent<TMP_InputField>().text = names[i];
        }
        //ServerUtils.BroadcastImmediate("#UpdateText " + UpdateTextAusgabe);
    }

    /// <summary>
    /// Aktualisiert die TextAnzeige aller Spieler nach X Sekunden, 
    /// damit nicht zu viele Anfragen gesendet werden
    /// und die Clients das nicht mehr verarbeiten können
    /// </summary>
    /// <returns></returns>
    private IEnumerator UpdateTextAsync()
    {
        string tempText = "";
        while (true) 
        {
            yield return new WaitForSeconds(0.01f);
            if (tempText != UpdateTextAusgabe)
            {
                tempText = UpdateTextAusgabe;
                ServerUtils.BroadcastImmediate("#UpdateText " + tempText);
            }
        }
    }
    
    #endregion
}
