using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ListenServer : MonoBehaviour
{
    #region TeamWahl
    GameObject LobbyTeamWahl;
    GameObject[] ServerTeamAnzahl;
    #region SpielerAnzeige
    List<int> teamZuschauerIds;
    GameObject TeamZuschauerGrid;
    GameObject[,] TeamZuschauer;
    List<int> teamRotIds;
    GameObject TeamRotGrid;
    GameObject[,] TeamRot;
    List<int> teamBlauIds;
    GameObject TeamBlauGrid;
    GameObject[,] TeamBlau;
    List<int> teamGruenIds;
    GameObject TeamGruenGrid;
    GameObject[,] TeamGruen;
    List<int> teamLilaIds;
    GameObject TeamLilaGrid;
    GameObject[,] TeamLila;
    #endregion
    #endregion
    #region InGameAnzeige
    GameObject InGameAnzeige;
    #region ListenAnzeigen
    GameObject ListenAnzeigen;
    GameObject Titel;
    GameObject SortierungOben;
    GameObject SortierungUnten;
    GameObject[] GridElemente;
    GameObject AuswahlTitel;
    GameObject[] AuswahlElemente;
    #endregion
    #region Server
    GameObject[] LoesungGrid;
    GameObject KeinTeamSpiel;
    GameObject[] KeinTeamSpielGrid;
    GameObject TeamRotSpiel;
    GameObject[] TeamRotSpielGrid;
    GameObject TeamBlauSpiel;
    GameObject[] TeamBlauSpielGrid;
    GameObject TeamGruenSpiel;
    GameObject[] TeamGruenSpielGrid;
    GameObject TeamLilaSpiel;
    GameObject[] TeamLilaSpielGrid;
    bool listewirdaufgeloest;
    #endregion
    #region Einstellungen
    int[] spielerherzenzahl;
    GameObject AusgetabtAnzeige;
    #endregion
    #endregion
    bool[] PlayerConnected;

    [SerializeField] AudioSource BuzzerSound;
    [SerializeField] AudioSource RichtigeAntwortSound;
    [SerializeField] AudioSource FalscheAntwortSound;
    [SerializeField] AudioSource DisconnectSound;

    void OnEnable()
    {
        StartCoroutine(ServerUtils.Broadcast());
        PlayerConnected = new bool[Config.SERVER_MAX_CONNECTIONS];
        InitAnzeigen();
    }

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
        Logging.log(Logging.LogType.Normal, "ListenServer", "OnApplicationQuit", "Server wird geschlossen");
        Config.SERVER_TCP.Server.Close();
    }

    #region Server Stuff
    #region Kommunikation
    /// <summary>
    /// Einkommende Nachrichten die von Spielern an den Server gesendet werden.
    /// </summary>
    /// <param name="spieler">Spieler</param>
    /// <param name="data">Nachricht</param>
    private void OnIncommingData(Player spieler, string data)
    {
        if (data.StartsWith(Config.GAME_TITLE + "#"))
            data = data.Substring(Config.GAME_TITLE.Length);
        else
            Logging.log(Logging.LogType.Error, "ListenServer", "OnIncommingData", "Wrong Command format: " + data);

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
    #endregion
    /// <summary>
    /// Einkommende Befehle von Spielern
    /// </summary>
    /// <param name="player">Spieler</param>
    /// <param name="data">Befehlsargumente</param>
    /// <param name="cmd">Befehl</param>
    private void Commands(Player player, string data, string cmd)
    {
        // Zeigt alle einkommenden Nachrichten an
        Logging.log(Logging.LogType.Debug, "ListenServer", "Commands", "Eingehende Nachricht: " + cmd + " -> " + data);
        // Sucht nach Command
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "ListenServer", "Commands", "Unkown Command: " + cmd + " -> " + data);
                break;

            case "#ClientClosed":
                ServerUtils.ClientClosed(player);
                UpdateSpielerBroadcast();
                PlayDisconnectSound();
                break;
            case "#TestConnection":
                break;
            case "#JoinListen":
                JoinTeam("Zuschauer", player.id);
                PlayerConnected[player.id - 1] = true;
                UpdateSpielerBroadcast();
                break;
            case "#ClientFocusChange":
                ClientFocusChange(player, data);
                break;
            case "#LobbyTeamWaehlen":
                if (GameObject.Find("LobbyTeamWahl/Server/SpielerDuerfenTeamWaehlen").GetComponent<Toggle>().isOn)
                {
                    JoinTeam(data, player.id);
                    UpdateSpielerBroadcast();
                }
                break;
        }
    }
    #endregion
    /// <summary>
    /// Initialisiert die Anzeigen zu beginn
    /// </summary>
    private void InitAnzeigen()
    {
        #region LobbyTeamWahl
        LobbyTeamWahl = GameObject.Find("Canvas/LobbyTeamWahl");

        ServerTeamAnzahl = new GameObject[4];
        ServerTeamAnzahl[0] = GameObject.Find("LobbyTeamWahl/Server/0_Teams");
        ServerTeamAnzahl[0].transform.GetChild(0).gameObject.SetActive(true);
        ServerTeamAnzahl[1] = GameObject.Find("LobbyTeamWahl/Server/2_Teams");
        ServerTeamAnzahl[1].transform.GetChild(0).gameObject.SetActive(false);
        ServerTeamAnzahl[2] = GameObject.Find("LobbyTeamWahl/Server/3_Teams");
        ServerTeamAnzahl[2].transform.GetChild(0).gameObject.SetActive(false);
        ServerTeamAnzahl[3] = GameObject.Find("LobbyTeamWahl/Server/4_Teams");
        ServerTeamAnzahl[3].transform.GetChild(0).gameObject.SetActive(false);

        teamRotIds = new List<int>();
        TeamRotGrid = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Rot");
        TeamRot = new GameObject[4, 7];
        for (int i = 1; i <= 4; i++)
        {
            TeamRot[(i - 1), 0] = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Rot/Spieler (" + i + ")");
            TeamRot[(i - 1), 1] = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Rot/Spieler (" + i + ")/ServerControl");
            TeamRot[(i - 1), 2] = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Rot/Spieler (" + i + ")/BuzzerPressed");
            TeamRot[(i - 1), 3] = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Rot/Spieler (" + i + ")/Icon");
            TeamRot[(i - 1), 4] = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Rot/Spieler (" + i + ")/Ausgetabt");
            TeamRot[(i - 1), 5] = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Rot/Spieler (" + i + ")/Infobar/Name");
            TeamRot[(i - 1), 6] = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Rot/Spieler (" + i + ")/Infobar/Punkte");

            TeamRot[(i - 1), 0].SetActive(false);
            TeamRot[(i - 1), 2].SetActive(false);
            TeamRot[(i - 1), 4].SetActive(false);
            TeamRot[(i - 1), 5].GetComponent<TMP_Text>().text = "";
            TeamRot[(i - 1), 6].GetComponent<TMP_Text>().text = "";
        }
        TeamRotGrid.SetActive(false);

        teamBlauIds = new List<int>();
        TeamBlauGrid = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Blau");
        TeamBlau = new GameObject[4, 7];
        for (int i = 1; i <= 4; i++)
        {
            TeamBlau[(i - 1), 0] = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Blau/Spieler (" + i + ")");
            TeamBlau[(i - 1), 1] = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Blau/Spieler (" + i + ")/ServerControl");
            TeamBlau[(i - 1), 2] = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Blau/Spieler (" + i + ")/BuzzerPressed");
            TeamBlau[(i - 1), 3] = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Blau/Spieler (" + i + ")/Icon");
            TeamBlau[(i - 1), 4] = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Blau/Spieler (" + i + ")/Ausgetabt");
            TeamBlau[(i - 1), 5] = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Blau/Spieler (" + i + ")/Infobar/Name");
            TeamBlau[(i - 1), 6] = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Blau/Spieler (" + i + ")/Infobar/Punkte");

            TeamBlau[(i - 1), 0].SetActive(false);
            TeamBlau[(i - 1), 2].SetActive(false);
            TeamBlau[(i - 1), 4].SetActive(false);
            TeamBlau[(i - 1), 5].GetComponent<TMP_Text>().text = "";
            TeamBlau[(i - 1), 6].GetComponent<TMP_Text>().text = "";
        }
        TeamBlauGrid.SetActive(false);

        teamGruenIds = new List<int>();
        TeamGruenGrid = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Gruen");
        TeamGruen = new GameObject[2, 7];
        for (int i = 1; i <= 2; i++)
        {
            TeamGruen[(i - 1), 0] = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Gruen/Spieler (" + i + ")");
            TeamGruen[(i - 1), 1] = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Gruen/Spieler (" + i + ")/ServerControl");
            TeamGruen[(i - 1), 2] = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Gruen/Spieler (" + i + ")/BuzzerPressed");
            TeamGruen[(i - 1), 3] = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Gruen/Spieler (" + i + ")/Icon");
            TeamGruen[(i - 1), 4] = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Gruen/Spieler (" + i + ")/Ausgetabt");
            TeamGruen[(i - 1), 5] = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Gruen/Spieler (" + i + ")/Infobar/Name");
            TeamGruen[(i - 1), 6] = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Gruen/Spieler (" + i + ")/Infobar/Punkte");

            TeamGruen[(i - 1), 0].SetActive(false);
            TeamGruen[(i - 1), 2].SetActive(false);
            TeamGruen[(i - 1), 4].SetActive(false);
            TeamGruen[(i - 1), 5].GetComponent<TMP_Text>().text = "";
            TeamGruen[(i - 1), 6].GetComponent<TMP_Text>().text = "";
        }
        TeamGruenGrid.SetActive(false);

        teamLilaIds = new List<int>();
        TeamLilaGrid = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Lila");
        TeamLila = new GameObject[2, 7];
        for (int i = 1; i <= 2; i++)
        {
            TeamLila[(i - 1), 0] = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Lila/Spieler (" + i + ")");
            TeamLila[(i - 1), 1] = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Lila/Spieler (" + i + ")/ServerControl");
            TeamLila[(i - 1), 2] = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Lila/Spieler (" + i + ")/BuzzerPressed");
            TeamLila[(i - 1), 3] = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Lila/Spieler (" + i + ")/Icon");
            TeamLila[(i - 1), 4] = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Lila/Spieler (" + i + ")/Ausgetabt");
            TeamLila[(i - 1), 5] = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Lila/Spieler (" + i + ")/Infobar/Name");
            TeamLila[(i - 1), 6] = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Lila/Spieler (" + i + ")/Infobar/Punkte");

            TeamLila[(i - 1), 0].SetActive(false);
            TeamLila[(i - 1), 2].SetActive(false);
            TeamLila[(i - 1), 4].SetActive(false);
            TeamLila[(i - 1), 5].GetComponent<TMP_Text>().text = "";
            TeamLila[(i - 1), 6].GetComponent<TMP_Text>().text = "";
        }
        TeamLilaGrid.SetActive(false);

        teamZuschauerIds = new List<int>();
        TeamZuschauerGrid = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Zuschauer");
        TeamZuschauer = new GameObject[8, 7];
        for (int j = 1; j <= 4; j++)
        {
            for (int i = 1; i <= 2; i++)
            {
                TeamZuschauer[(j - 1) * 2 + (i - 1), 0] = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Zuschauer/GameObject (" + i + ")/Spieler (" + j + ")");
                TeamZuschauer[(j - 1) * 2 + (i - 1), 1] = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Zuschauer/GameObject (" + i + ")/Spieler (" + j + ")/ServerControl");
                TeamZuschauer[(j - 1) * 2 + (i - 1), 2] = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Zuschauer/GameObject (" + i + ")/Spieler (" + j + ")/BuzzerPressed");
                TeamZuschauer[(j - 1) * 2 + (i - 1), 3] = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Zuschauer/GameObject (" + i + ")/Spieler (" + j + ")/Icon");
                TeamZuschauer[(j - 1) * 2 + (i - 1), 4] = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Zuschauer/GameObject (" + i + ")/Spieler (" + j + ")/Ausgetabt");
                TeamZuschauer[(j - 1) * 2 + (i - 1), 5] = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Zuschauer/GameObject (" + i + ")/Spieler (" + j + ")/Infobar/Name");
                TeamZuschauer[(j - 1) * 2 + (i - 1), 6] = GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Zuschauer/GameObject (" + i + ")/Spieler (" + j + ")/Infobar/Punkte");

                TeamZuschauer[(j - 1) * 2 + (i - 1), 0].SetActive(false);
                TeamZuschauer[(j - 1) * 2 + (i - 1), 2].SetActive(false);
                TeamZuschauer[(j - 1) * 2 + (i - 1), 4].SetActive(false);
                TeamZuschauer[(j - 1) * 2 + (i - 1), 5].GetComponent<TMP_Text>().text = "";
                TeamZuschauer[(j - 1) * 2 + (i - 1), 6].GetComponent<TMP_Text>().text = "";
            }
        }
        TeamZuschauerGrid.SetActive(true);

        // joint alle Spieler in Zuschauer
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            if (Config.PLAYERLIST[i].name.Length > 1)
                JoinTeam("Zuschauer", Config.PLAYERLIST[i].id);
        }

        GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Zuschauer/Spacer").transform.GetChild(0).gameObject.SetActive(false);

        // Server
        GameObject.Find("LobbyTeamWahl/Server/SpielerDuerfenTeamWaehlen").GetComponent<Toggle>().isOn = false;
        #endregion

        #region InGameSpielerAnzeige
        #region ListenAnzeigen
        InGameAnzeige = GameObject.Find("Canvas/InGameAnzeige");

        ListenAnzeigen = GameObject.Find("InGameAnzeige/ListenAnzeigen");
        Titel = GameObject.Find("InGameAnzeige/ListenAnzeigen/Spielfeld/Titel");
        Titel.SetActive(false);
        SortierungOben = GameObject.Find("ListenAnzeigen/Spielfeld/Grid/SortierungOben");
        SortierungOben.SetActive(false);
        SortierungUnten = GameObject.Find("ListenAnzeigen/Spielfeld/Grid/SortierungUnten");
        SortierungUnten.SetActive(false);
        GridElemente = new GameObject[30];
        for (int i = 1; i <= 30; i++)
        {
            GridElemente[i - 1] = GameObject.Find("ListenAnzeigen/Spielfeld/Grid/Element (" + i + ")");
            GridElemente[i - 1].SetActive(false);
        }
        AuswahlTitel = GameObject.Find("ListenAnzeigen/Spielfeld/AuswahlGrid/AuswahlTitel");
        AuswahlTitel.SetActive(false);
        AuswahlElemente = new GameObject[30];
        for (int i = 1; i <= 30; i++)
        {
            AuswahlElemente[i - 1] = GameObject.Find("ListenAnzeigen/Spielfeld/AuswahlGrid/Element (" + i + ")");
            AuswahlElemente[i - 1].SetActive(false);
        }
        ListenAnzeigen.SetActive(false);
        #endregion
        #region Server
        LoesungGrid = new GameObject[32];
        LoesungGrid[0] = GameObject.Find("InGameAnzeige/Server/Grid/SortierungOben");
        LoesungGrid[0].SetActive(false);
        LoesungGrid[1] = GameObject.Find("InGameAnzeige/Server/Grid/SortierungUnten");
        LoesungGrid[1].SetActive(false);
        for (int i = 1; i <= 30; i++)
        {
            LoesungGrid[i + 1] = GameObject.Find("InGameAnzeige/Server/Grid/Element (" + i + ")");
            LoesungGrid[i + 1].SetActive(false);
        }
        
        KeinTeamSpiel = GameObject.Find("InGameAnzeige/Server/KeinTeam");
        KeinTeamSpielGrid = new GameObject[8];
        for (int j = 1; j <= 4; j++)
        {
            for (int i = 1; i <= 2; i++)
            {
                KeinTeamSpielGrid[(j - 1) * 2 + (i - 1)] = GameObject.Find("InGameAnzeige/Server/KeinTeam/Team "+i+"/Spieler ("+j+")");
                KeinTeamSpielGrid[(j - 1) * 2 + (i - 1)].SetActive(false);
            }
        }
        KeinTeamSpiel.SetActive(false);
        TeamRotSpiel = GameObject.Find("InGameAnzeige/Server/Team Rot");
        TeamRotSpielGrid = new GameObject[4];
        for (int i = 1; i <= 4; i++)
        {
            TeamRotSpielGrid[i-1] = GameObject.Find("InGameAnzeige/Server/Team Rot/Spieler (" + i + ")");
            TeamRotSpielGrid[i-1].SetActive(false);
        }
        TeamRotSpiel.SetActive(false);
        TeamBlauSpiel = GameObject.Find("InGameAnzeige/Server/Team Blau");
        TeamBlauSpielGrid = new GameObject[4];
        for (int i = 1; i <= 4; i++)
        {
            TeamBlauSpielGrid[i - 1] = GameObject.Find("InGameAnzeige/Server/Team Blau/Spieler (" + i + ")");
            TeamBlauSpielGrid[i - 1].SetActive(false);
        }
        TeamBlauSpiel.SetActive(false);
        TeamGruenSpiel = GameObject.Find("InGameAnzeige/Server/Team Gruen");
        TeamGruenSpielGrid = new GameObject[2];
        for (int i = 1; i <= 2; i++)
        {
            TeamGruenSpielGrid[i - 1] = GameObject.Find("InGameAnzeige/Server/Team Gruen/Spieler (" + i + ")");
            TeamGruenSpielGrid[i - 1].SetActive(false);
        }
        TeamGruenSpiel.SetActive(false);
        TeamLilaSpiel = GameObject.Find("InGameAnzeige/Server/Team Lila");
        TeamLilaSpielGrid = new GameObject[2];
        for (int i = 1; i <= 2; i++)
        {
            TeamLilaSpielGrid[i - 1] = GameObject.Find("InGameAnzeige/Server/Team Lila/Spieler (" + i + ")");
            TeamLilaSpielGrid[i - 1].SetActive(false);
        }
        TeamLilaSpiel.SetActive(false);
        #endregion
        #region Einstellungen
        spielerherzenzahl = new int[Config.SERVER_MAX_CONNECTIONS];
        for (int i = 0; i < spielerherzenzahl.Length; i++)
        {
            spielerherzenzahl[i] = 0;
        }
        listewirdaufgeloest = false;
        GameObject.Find("Einstellungen/AusgetabtSpielernZeigenToggle").GetComponent<Toggle>().isOn = false;
        AusgetabtAnzeige = GameObject.Find("Einstellungen/AusgetabtWirdSpielernGezeigen");
        AusgetabtAnzeige.SetActive(false);
        GameObject.Find("Einstellungen/HerzenAnzeigen").GetComponent<Toggle>().isOn = false;
        GameObject.Find("Einstellungen/HerzenAnzahl").GetComponent<TMP_InputField>().text = "4";
        TMP_Dropdown drop = GameObject.Find("Einstellungen/ChangeListenWahl").GetComponent<TMP_Dropdown>();
        drop.ClearOptions();
        List<string> gamelist = new List<string>();
        foreach (Listen liste in Config.LISTEN_SPIEL.getListen())
        {
            gamelist.Add(liste.getTitel());
        }
        drop.AddOptions(gamelist);
        drop.value = Config.LISTEN_SPIEL.getIndex(Config.LISTEN_SPIEL.getSelected());
        InGameAnzeige.SetActive(false);
        #endregion
        #endregion

        GameObject.Find("Einstellungen/Quelle").GetComponent<TMP_InputField>().text = Config.LISTEN_SPIEL.getSelected().getQuelle();
    }
    #region Update Spieler
    /// <summary>
    /// Sendet aktualisierte Spielerinfos an alle Spieler
    /// </summary>
    private void UpdateSpielerBroadcast() 
    {
        ServerUtils.AddBroadcast(UpdateSpieler());
    }
    /// <summary>
    /// Aktualisiert die Spieler Anzeige Informationen & gibt diese als Text zurück
    /// </summary>
    /// <returns>#UpdateSpieler ...</returns>
    private string UpdateSpieler()
    {
        if (LobbyTeamWahl.activeInHierarchy)
        {
            UpdateDisplaysLobby();
            return UpdateSpielerLobby();
        }
        else if (InGameAnzeige.activeInHierarchy)
        {
            UpdateDisplaysInGame();
            return UpdateSpielerInGame();
        }
        return "";
    }
    /// <summary>
    /// Spielt den Disconnect Sound ab
    /// </summary>
    private void PlayDisconnectSound()
    {
        DisconnectSound.Play();
    }
    /// <summary>
    /// Aktualisiert die Spieler Anzeige in der Lobby & gibt diese als Text zurück
    /// </summary>
    /// <returns>#UpdateSpieler ...</returns>
    private string UpdateSpielerLobby()
    {
        string msg = "#UpdateSpieler [ANZEIGE]LobbyTeamWahl[ANZEIGE]";

        // Zuschauer
        string zuschauer = "";
        foreach (int id in teamZuschauerIds)
        {
            zuschauer += "," + id;
        }
        if (zuschauer.StartsWith(","))
            zuschauer = zuschauer.Substring(1);
        // Team Rot
        string teamrot = "";
        foreach (int id in teamRotIds)
        {
            teamrot += "," + id;
        }
        if (teamrot.StartsWith(","))
            teamrot = teamrot.Substring(1);
        // Team Blau
        string teamblau = "";
        foreach (int id in teamBlauIds)
        {
            teamblau += "," + id;
        }
        if (teamblau.StartsWith(","))
            teamblau = teamblau.Substring(1);
        // Team Gruen
        string teamgruen = "";
        foreach (int id in teamGruenIds)
        {
            teamgruen += "," + id;
        }
        if (teamgruen.StartsWith(","))
            teamgruen = teamgruen.Substring(1);
        // Team Lila
        string teamlila = "";
        foreach (int id in teamLilaIds)
        {
            teamlila+= "," + id;
        }
        if (teamlila.StartsWith(","))
            teamlila = teamlila.Substring(1);
        msg += "[ZUSCHAUER][BOOL]" + TeamZuschauerGrid.activeInHierarchy + "[BOOL][IDS]" + zuschauer+ "[IDS][ZUSCHAUER]" +
            "[TEAMROT][BOOL]" + TeamRotGrid.activeInHierarchy + "[BOOL][IDS]" + teamrot+ "[IDS][TEAMROT]" +
            "[TEAMBLAU][BOOL]" + TeamBlauGrid.activeInHierarchy + "[BOOL][IDS]" + teamblau+ "[IDS][TEAMBLAU]" +
            "[TEAMGRUEN][BOOL]" + TeamGruenGrid.activeInHierarchy + "[BOOL][IDS]" + teamgruen+ "[IDS][TEAMGRUEN]" +
            "[TEAMLILA][BOOL]" + TeamLilaGrid.activeInHierarchy + "[BOOL][IDS]" + teamlila+"[IDS][TEAMLILA]";
        return msg;
    }
    /// <summary>
    /// Aktualisiert die Spieler Anzeige Informationen
    /// </summary>
    private void UpdateDisplaysLobby()
    {
        /// Zuschauer
        if (TeamZuschauerGrid.activeInHierarchy)
        {
            for (int i = 1; i <= 8; i++)
            {
                TeamZuschauer[i - 1, 0].SetActive(false);
            }
            for (int i = 0; i < teamZuschauerIds.Count; i++)
            {
                int id = teamZuschauerIds[i];
                TeamZuschauer[i, 0].SetActive(true);
                TeamZuschauer[i, 1].SetActive(true);
                TeamZuschauer[i, 2].SetActive(false);
                TeamZuschauer[i, 3].GetComponent<Image>().sprite = Config.PLAYERLIST[Player.getPosInLists(id)].icon;
                TeamZuschauer[i, 4].SetActive(false);
                TeamZuschauer[i, 5].GetComponent<TMP_Text>().text = Config.PLAYERLIST[Player.getPosInLists(id)].name;
                TeamZuschauer[i, 6].GetComponent<TMP_Text>().text = Config.PLAYERLIST[Player.getPosInLists(id)].points + "";
            }
        }
        /// Team Rot
        if (TeamRotGrid.activeInHierarchy)
        {
            for (int i = 1; i <= 4; i++)
            {
                TeamRot[i - 1, 0].SetActive(false);
            }
            for (int i = 0; i < teamRotIds.Count; i++)
            {
                int id = teamRotIds[i];
                TeamRot[i, 0].SetActive(true);
                TeamRot[i, 1].SetActive(true);
                TeamRot[i, 2].SetActive(false);
                TeamRot[i, 3].GetComponent<Image>().sprite = Config.PLAYERLIST[Player.getPosInLists(id)].icon;
                TeamRot[i, 4].SetActive(false);
                TeamRot[i, 5].GetComponent<TMP_Text>().text = Config.PLAYERLIST[Player.getPosInLists(id)].name;
                TeamRot[i, 6].GetComponent<TMP_Text>().text = Config.PLAYERLIST[Player.getPosInLists(id)].points + "";
            }
        }
        /// Team Blau
        if (TeamBlauGrid.activeInHierarchy)
        {
            for (int i = 1; i <= 4; i++)
            {
                TeamBlau[i - 1, 0].SetActive(false);
            }
            for (int i = 0; i < teamBlauIds.Count; i++)
            {
                int id = teamBlauIds[i];
                TeamBlau[i, 0].SetActive(true);
                TeamBlau[i, 1].SetActive(true);
                TeamBlau[i, 2].SetActive(false);
                TeamBlau[i, 3].GetComponent<Image>().sprite = Config.PLAYERLIST[Player.getPosInLists(id)].icon;
                TeamBlau[i, 4].SetActive(false);
                TeamBlau[i, 5].GetComponent<TMP_Text>().text = Config.PLAYERLIST[Player.getPosInLists(id)].name;
                TeamBlau[i, 6].GetComponent<TMP_Text>().text = Config.PLAYERLIST[Player.getPosInLists(id)].points + "";
            }
        }
        /// Team Gruen
        if (TeamGruenGrid.activeInHierarchy)
        {
            for (int i = 1; i <= 2; i++)
            {
                TeamGruen[i - 1, 0].SetActive(false);
            }
            for (int i = 0; i < teamGruenIds.Count; i++)
            {
                int id = teamGruenIds[i];
                TeamGruen[i, 0].SetActive(true);
                TeamGruen[i, 1].SetActive(true);
                TeamGruen[i, 2].SetActive(false);
                TeamGruen[i, 3].GetComponent<Image>().sprite = Config.PLAYERLIST[Player.getPosInLists(id)].icon;
                TeamGruen[i, 4].SetActive(false);
                TeamGruen[i, 5].GetComponent<TMP_Text>().text = Config.PLAYERLIST[Player.getPosInLists(id)].name;
                TeamGruen[i, 6].GetComponent<TMP_Text>().text = Config.PLAYERLIST[Player.getPosInLists(id)].points + "";
            }
        }
        /// Team Lila
        if (TeamLilaGrid.activeInHierarchy)
        {
            for (int i = 1; i <= 2; i++)
            {
                TeamLila[i - 1, 0].SetActive(false);
            }
            for (int i = 0; i < teamLilaIds.Count; i++)
            {
                int id = teamLilaIds[i];
                TeamLila[i, 0].SetActive(true);
                TeamLila[i, 1].SetActive(true);
                TeamLila[i, 2].SetActive(false);
                TeamLila[i, 3].GetComponent<Image>().sprite = Config.PLAYERLIST[Player.getPosInLists(id)].icon;
                TeamLila[i, 4].SetActive(false);
                TeamLila[i, 5].GetComponent<TMP_Text>().text = Config.PLAYERLIST[Player.getPosInLists(id)].name;
                TeamLila[i, 6].GetComponent<TMP_Text>().text = Config.PLAYERLIST[Player.getPosInLists(id)].points + "";
            }
        }


    }
    /// <summary>
    /// Aktualisiert die Spieler Anzeige InGame & gibt diese als Text zurück
    /// </summary>
    /// <returns>#UpdateSpieler ...</returns>
    private string UpdateSpielerInGame()
    {
        string msg = "#UpdateSpieler [ANZEIGE]InGame[ANZEIGE]";

        // Zuschauer
        string zuschauer = "";
        foreach (int id in teamZuschauerIds)
        {
            zuschauer += "," + id;
        }
        if (zuschauer.StartsWith(","))
            zuschauer = zuschauer.Substring(1);
        // Team Rot
        string teamrot = "";
        foreach (int id in teamRotIds)
        {
            teamrot += "," + id;
        }
        if (teamrot.StartsWith(","))
            teamrot = teamrot.Substring(1);
        // Team Blau
        string teamblau = "";
        foreach (int id in teamBlauIds)
        {
            teamblau += "," + id;
        }
        if (teamblau.StartsWith(","))
            teamblau = teamblau.Substring(1);
        // Team Gruen
        string teamgruen = "";
        foreach (int id in teamGruenIds)
        {
            teamgruen += "," + id;
        }
        if (teamgruen.StartsWith(","))
            teamgruen = teamgruen.Substring(1);
        // Team Lila
        string teamlila = "";
        foreach (int id in teamLilaIds)
        {
            teamlila += "," + id;
        }
        if (teamlila.StartsWith(","))
            teamlila = teamlila.Substring(1);
        msg += "[KEINTEAM][BOOL]" + KeinTeamSpiel.activeInHierarchy + "[BOOL][IDS]" + zuschauer + "[IDS][KEINTEAM]" +
            "[TEAMROT][BOOL]" + TeamRotSpiel.activeInHierarchy + "[BOOL][IDS]" + teamrot + "[IDS][TEAMROT]" +
            "[TEAMBLAU][BOOL]" + TeamBlauSpiel.activeInHierarchy + "[BOOL][IDS]" + teamblau + "[IDS][TEAMBLAU]" +
            "[TEAMGRUEN][BOOL]" + TeamGruenSpiel.activeInHierarchy + "[BOOL][IDS]" + teamgruen + "[IDS][TEAMGRUEN]" +
            "[TEAMLILA][BOOL]" + TeamLilaSpiel.activeInHierarchy + "[BOOL][IDS]" + teamlila + "[IDS][TEAMLILA]";
        Logging.log(Logging.LogType.Debug, "ListenServer", "UpdateSpielerInGame", msg);
        return msg;
    }
    /// <summary>
    /// Aktualisiert die Spieler Anzeige Informationen
    /// </summary>
    private void UpdateDisplaysInGame()
    {
        /// Zuschauer
        if (KeinTeamSpiel.activeInHierarchy)
        {
            for (int i = 1; i <= 8; i++)
            {
                KeinTeamSpielGrid[i - 1].SetActive(false);
            }
            for (int i = 0; i < teamZuschauerIds.Count; i++)
            {
                int id = teamZuschauerIds[i];
                KeinTeamSpielGrid[i].SetActive(true);
                KeinTeamSpielGrid[i].transform.GetChild(2).GetComponent<Image>().sprite = Config.PLAYERLIST[id - 1].icon;
                KeinTeamSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text = Config.PLAYERLIST[id - 1].name;
                KeinTeamSpielGrid[i].transform.GetChild(4).GetChild(2).GetComponent<TMP_Text>().text = Config.PLAYERLIST[id - 1].points + "";
            }
        }
        /// Team Rot
        if (TeamRotSpiel.activeInHierarchy)
        {
            for (int i = 1; i <= 4; i++)
            {
                TeamRotSpielGrid[i - 1].SetActive(false);
            }
            for (int i = 0; i < teamRotIds.Count; i++)
            {
                int id = teamRotIds[i];
                TeamRotSpielGrid[i].SetActive(true);
                TeamRotSpielGrid[i].transform.GetChild(2).GetComponent<Image>().sprite = Config.PLAYERLIST[id - 1].icon;
                TeamRotSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text = Config.PLAYERLIST[id - 1].name;
                TeamRotSpielGrid[i].transform.GetChild(4).GetChild(2).GetComponent<TMP_Text>().text = Config.PLAYERLIST[id - 1].points + "";
            }
        }
        /// Team Blau
        if (TeamBlauSpiel.activeInHierarchy)
        {
            for (int i = 1; i <= 4; i++)
            {
                TeamBlauSpielGrid[i - 1].SetActive(false);
            }
            for (int i = 0; i < teamBlauIds.Count; i++)
            {
                int id = teamBlauIds[i];
                TeamBlauSpielGrid[i].SetActive(true);
                TeamBlauSpielGrid[i].transform.GetChild(2).GetComponent<Image>().sprite = Config.PLAYERLIST[id - 1].icon;
                TeamBlauSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text = Config.PLAYERLIST[id - 1].name;
                TeamBlauSpielGrid[i].transform.GetChild(4).GetChild(2).GetComponent<TMP_Text>().text = Config.PLAYERLIST[id - 1].points + "";
            }
        }
        /// Team Gruen
        if (TeamGruenSpiel.activeInHierarchy)
        {
            for (int i = 1; i <= 2; i++)
            {
                TeamGruenSpielGrid[i - 1].SetActive(false);
            }
            for (int i = 0; i < teamGruenIds.Count; i++)
            {
                int id = teamGruenIds[i];
                TeamGruenSpielGrid[i].SetActive(true);
                TeamGruenSpielGrid[i].transform.GetChild(2).GetComponent<Image>().sprite = Config.PLAYERLIST[id - 1].icon;
                TeamGruenSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text = Config.PLAYERLIST[id - 1].name;
                TeamGruenSpielGrid[i].transform.GetChild(4).GetChild(2).GetComponent<TMP_Text>().text = Config.PLAYERLIST[id - 1].points + "";
            }
        }
        /// Team Lila
        if (TeamLilaSpiel.activeInHierarchy)
        {
            for (int i = 1; i <= 2; i++)
            {
                TeamLilaSpielGrid[i - 1].SetActive(false);
            }
            for (int i = 0; i < teamLilaIds.Count; i++)
            {
                int id = teamLilaIds[i];
                TeamLilaSpielGrid[i].SetActive(true);
                TeamLilaSpielGrid[i].transform.GetChild(2).GetComponent<Image>().sprite = Config.PLAYERLIST[id - 1].icon;
                TeamLilaSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text = Config.PLAYERLIST[id - 1].name;
                TeamLilaSpielGrid[i].transform.GetChild(4).GetChild(2).GetComponent<TMP_Text>().text = Config.PLAYERLIST[id - 1].points + "";
            }
        }
    }
    #endregion
    #region LobbyTeamWahl
    /// <summary>
    /// Ändert die Anzahl der möglichen Teams
    /// </summary>
    /// <param name="button">Teamanzahl</param>
    public void TeamAnzahlAendern(GameObject button)
    {
        GameObject.Find("LobbyTeamWahl/Server/0_Teams/Image").SetActive(false);
        GameObject.Find("LobbyTeamWahl/Server/2_Teams/Image").SetActive(false);
        GameObject.Find("LobbyTeamWahl/Server/3_Teams/Image").SetActive(false);
        GameObject.Find("LobbyTeamWahl/Server/4_Teams/Image").SetActive(false);
        int nummer = Int32.Parse(button.GetComponentInChildren<TMP_Text>().text);
        button.transform.GetChild(0).gameObject.SetActive(true);


        GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Zuschauer/Spacer").transform.GetChild(0).gameObject.SetActive(true);
        if (nummer == 0)
            GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Zuschauer/Spacer").transform.GetChild(0).gameObject.SetActive(false);

        /// Zuschauer
        if (nummer >= 0)
        {
            /// Team freischalten
            TeamZuschauerGrid.SetActive(true);
        }
        else
        {
            /// Team schließen -> Spieler in Zuschauer werfen
            TeamZuschauerGrid.SetActive(false);
            List<int> temp = new List<int>();
            temp.AddRange(teamZuschauerIds);
            foreach (int id in temp)
            {
                JoinTeam("Zuschauer", id);
            }
        }
        /// Team Rot
        if (nummer >= 2)
        {
            /// Team freischalten
            TeamRotGrid.SetActive(true);
        }
        else
        {
            /// Team schließen -> Spieler in Zuschauer werfen
            TeamRotGrid.SetActive(false);
            List<int> temp = new List<int>();
            temp.AddRange(teamRotIds);
            foreach (int id in temp)
            {
                JoinTeam("Zuschauer", id);
            }
        }
        /// Team Blau
        if (nummer >= 2)
        {
            /// Team freischalten
            TeamBlauGrid.SetActive(true);
        }
        else
        {
            /// Team schließen -> Spieler in Zuschauer werfen
            TeamBlauGrid.SetActive(false);
            List<int> temp = new List<int>();
            temp.AddRange(teamBlauIds);
            foreach (int id in temp)
            {
                JoinTeam("Zuschauer", id);
            }
        }
        /// Team Gruen
        if (nummer >= 3)
        {
            /// Team freischalten
            TeamGruenGrid.SetActive(true);
        }
        else
        {
            /// Team schließen -> Spieler in Zuschauer werfen
            TeamGruenGrid.SetActive(false);
            List<int> temp = new List<int>();
            temp.AddRange(teamGruenIds);
            foreach (int id in temp)
            {
                JoinTeam("Zuschauer", id);
            }
        }
        /// Team Lila
        if (nummer >= 4)
        {
            /// Team freischalten
            TeamLilaGrid.SetActive(true);
        }
        else
        {
            /// Team schließen -> Spieler in Zuschauer werfen
            TeamLilaGrid.SetActive(false);
            List<int> temp = new List<int>();
            temp.AddRange(teamLilaIds);
            foreach (int id in temp)
            {
                JoinTeam("Zuschauer", id);
            }
        }
        UpdateSpielerBroadcast();
    }
    /// <summary>
    /// Spieler tritt einem Team bei
    /// </summary>
    /// <param name="team">Zuschauer, Rot, Blau, Gruen, Lila</param>
    /// <param name="playerId"><1-9></param>
    private void JoinTeam(string team, int playerId)
    {
        // Check ob Team voll oder deaktiviert ist
        if (team.Equals("Zuschauer") && teamZuschauerIds.Count >= 8)
            return;
        if (team.Equals("Rot") && teamRotIds.Count >= 4 || (team.Equals("Rot") && !TeamRotGrid.activeInHierarchy))
            return;
        if (team.Equals("Blau") && teamBlauIds.Count >= 4 || (team.Equals("Blau") && !TeamBlauGrid.activeInHierarchy))
            return;
        if (team.Equals("Gruen") && teamGruenIds.Count >= 2 || (team.Equals("Gruen") && !TeamGruenGrid.activeInHierarchy))
            return;
        if (team.Equals("Lila") && teamLilaIds.Count >= 2 || (team.Equals("Lila") && !TeamLilaGrid.activeInHierarchy))
            return;

        // Spieler aus altem Team löschen
        if (teamZuschauerIds.Contains(playerId))
            teamZuschauerIds.Remove(playerId);
        if (teamRotIds.Contains(playerId))
            teamRotIds.Remove(playerId);
        if (teamBlauIds.Contains(playerId))
            teamBlauIds.Remove(playerId);
        if (teamGruenIds.Contains(playerId))
            teamGruenIds.Remove(playerId);
        if (teamLilaIds.Contains(playerId))
            teamLilaIds.Remove(playerId);

        // Neuem Team beitreten
        if (team.Equals("Zuschauer"))
            teamZuschauerIds.Add(playerId);
        if (team.Equals("Rot"))
            teamRotIds.Add(playerId);
        if (team.Equals("Blau"))
            teamBlauIds.Add(playerId);
        if (team.Equals("Gruen"))
            teamGruenIds.Add(playerId);
        if (team.Equals("Lila"))
            teamLilaIds.Add(playerId);
    }
    /// <summary>
    /// Zufällige Teams mischen
    /// </summary>
    public void ZufaelligeTeamsFestlegen()
    {
        // teams sammeln
        int teams = 0;
        if (TeamRotGrid.activeInHierarchy)
            teams++;
        if (TeamBlauGrid.activeInHierarchy)
            teams++;
        if (TeamGruenGrid.activeInHierarchy)
            teams++;
        if (TeamLilaGrid.activeInHierarchy)
            teams++;
        if (teams == 0)
            return;

        // Ids sammeln
        List<int> ids = new List<int>();
        foreach (int id in teamZuschauerIds)
            ids.Add(id);
        foreach (int id in teamRotIds)
            ids.Add(id);
        foreach (int id in teamBlauIds)
            ids.Add(id);
        foreach (int id in teamGruenIds)
            ids.Add(id);
        foreach (int id in teamLilaIds)
            ids.Add(id);

        List<string> freieplaetze = new List<string>();
        if (teams == 4)
        {
            if (ids.Count >= 1)
                freieplaetze.Add("Rot");
            if (ids.Count >= 2)
                freieplaetze.Add("Blau");
            if (ids.Count >= 3)
                freieplaetze.Add("Gruen");
            if (ids.Count >= 4)
                freieplaetze.Add("Lila");
            if (ids.Count >= 5)
                freieplaetze.Add("Rot");
            if (ids.Count >= 6)
                freieplaetze.Add("Blau");
            if (ids.Count >= 7)
                freieplaetze.Add("Gruen");
            if (ids.Count >= 8)
                freieplaetze.Add("Lila");
            
        }
        else if (teams == 3)
        {
            if (ids.Count >= 1)
                freieplaetze.Add("Rot");
            if (ids.Count >= 2)
                freieplaetze.Add("Blau");
            if (ids.Count >= 3)
                freieplaetze.Add("Gruen");
            if (ids.Count >= 4)
                freieplaetze.Add("Rot");
            if (ids.Count >= 5)
                freieplaetze.Add("Blau");
            if (ids.Count >= 6)
                freieplaetze.Add("Gruen");
        }
        else if (teams == 2)
        {
            if (ids.Count >= 1)
                freieplaetze.Add("Rot");
            if (ids.Count >= 2)
                freieplaetze.Add("Blau");
            if (ids.Count >= 3)
                freieplaetze.Add("Rot");
            if (ids.Count >= 4)
                freieplaetze.Add("Blau");
            if (ids.Count >= 5)
                freieplaetze.Add("Rot");
            if (ids.Count >= 6)
                freieplaetze.Add("Blau");
            if (ids.Count >= 7)
                freieplaetze.Add("Rot");
            if (ids.Count >= 8)
                freieplaetze.Add("Blau");
        }

        /// 3 Teams nur für 6 Spieler
        if (ids.Count > 6 && teams == 3)
            return;

        foreach (int id in ids)
            JoinTeam("Zuschauer", id);

        while (ids.Count > 0)
        {
            string team = freieplaetze[UnityEngine.Random.Range(0, freieplaetze.Count)];
            int id = ids[UnityEngine.Random.Range(0, ids.Count)];

            JoinTeam(team, id);
            freieplaetze.Remove(team);
            ids.Remove(id);
        }
        UpdateSpielerBroadcast();
    }
    /// <summary>
    /// Spieler wird einem Team zugewiesen
    /// </summary>
    /// <param name="button">Team</param>
    public void SpielerTeamZuweisen(GameObject button)
    {
        string newteam = button.name.Replace("Team", "");
        string spielername = button.transform.parent.parent.GetChild(4).GetChild(1).gameObject.GetComponent<TMP_Text>().text;
        JoinTeam(newteam, Player.getIdByName(spielername));
        UpdateSpielerBroadcast();
    }
    #endregion
    #region InGameAnzeige
    /// <summary>
    /// Spiel wird gestartet, benötigte Elemente ausgeblendet/eingeblendet
    /// </summary>
    public void StarteListenSpiel()
    {
        /// Nur Starten wenn Zuschauer leer oder keine Teams
        if (TeamRotGrid.activeInHierarchy)
            if (teamZuschauerIds.Count > 0)
                return;

        LobbyTeamWahl.SetActive(false);
        InGameAnzeige.SetActive(true);
        listewirdaufgeloest = false;
        GameObject.Find("Einstellungen/HerzenAnzeigen").GetComponent<Toggle>().isOn = false;

        // TeamsAnzeigen BuzzerPress & Ausgetabt ausblenden
        KeinTeamSpiel.SetActive(false);
        TeamRotSpiel.SetActive(false);
        TeamBlauSpiel.SetActive(false);
        TeamGruenSpiel.SetActive(false);
        TeamLilaSpiel.SetActive(false);
        if (teamZuschauerIds.Count > 0)
        {
            KeinTeamSpiel.SetActive(true);
            for (int i = 0; i < 8; i++)
            {
                KeinTeamSpielGrid[i].transform.GetChild(1).gameObject.SetActive(false);
                KeinTeamSpielGrid[i].transform.GetChild(3).gameObject.SetActive(false);
                KeinTeamSpielGrid[i].transform.GetChild(5).gameObject.SetActive(false);
            }
        }
        if (teamRotIds.Count > 0)
        {
            TeamRotSpiel.SetActive(true);
            for (int i = 0; i < 4; i++)
            {
                TeamRotSpielGrid[i].transform.GetChild(1).gameObject.SetActive(false);
                TeamRotSpielGrid[i].transform.GetChild(3).gameObject.SetActive(false);
                TeamRotSpielGrid[i].transform.GetChild(5).gameObject.SetActive(false);
            }
        }
        if (teamBlauIds.Count > 0)
        {
            TeamBlauSpiel.SetActive(true);
            for (int i = 0; i < 4; i++)
            {
                TeamBlauSpielGrid[i].transform.GetChild(1).gameObject.SetActive(false);
                TeamBlauSpielGrid[i].transform.GetChild(3).gameObject.SetActive(false);
                TeamBlauSpielGrid[i].transform.GetChild(5).gameObject.SetActive(false);
            }
        }
        if (teamGruenIds.Count > 0)
        {
            TeamGruenSpiel.SetActive(true);
            for (int i = 0; i < 2; i++)
            {
                TeamGruenSpielGrid[i].transform.GetChild(1).gameObject.SetActive(false);
                TeamGruenSpielGrid[i].transform.GetChild(3).gameObject.SetActive(false);
                TeamGruenSpielGrid[i].transform.GetChild(5).gameObject.SetActive(false);
            }
        }
        if (teamLilaIds.Count > 0)
        {
            TeamLilaSpiel.SetActive(true);
            for (int i = 0; i < 2; i++)
            {
                TeamLilaSpielGrid[i].transform.GetChild(1).gameObject.SetActive(false);
                TeamLilaSpielGrid[i].transform.GetChild(3).gameObject.SetActive(false);
                TeamLilaSpielGrid[i].transform.GetChild(5).gameObject.SetActive(false);
            }
        }

        UpdateSpielerBroadcast();

        // Zeigt Lösung
        LoesungGrid[0].SetActive(true);
        LoesungGrid[0].GetComponentInChildren<TMP_Text>().text = "▲ "+ Config.LISTEN_SPIEL.getSelected().getSortByDisplay().Replace(" - ","|").Split('|')[0];
        LoesungGrid[1].SetActive(true);
        LoesungGrid[1].GetComponentInChildren<TMP_Text>().text = "▼ " + Config.LISTEN_SPIEL.getSelected().getSortByDisplay().Replace(" - ", "|").Split('|')[1];

        for (int i = 0; i < 30; i++)
        {
            if (i < Config.LISTEN_SPIEL.getSelected().getAlleElemente().Count) {
                Element element = Config.LISTEN_SPIEL.getSelected().getAlleElemente()[i];
                LoesungGrid[2 + i].SetActive(true);
                LoesungGrid[2 + i].transform.GetChild(1).GetComponent<TMP_Text>().text = element.getItem() + " - " + element.getDisplay();
                LoesungGrid[2 + i].transform.GetChild(2).GetComponentInChildren<TMP_Text>().text = "";// + (i+1);
            }
        }
        ServerUtils.AddBroadcast("#ListenStart");
    }
    /// <summary>
    /// Blendet Titel bei allen ein
    /// </summary>
    public void ListenTitelAnzeigen()
    {
        ListenAnzeigen.SetActive(true);
        Titel.SetActive(true);
        Titel.GetComponent<TMP_Text>().text = Config.LISTEN_SPIEL.getSelected().getTitel();
        ServerUtils.AddBroadcast("#ListenTitel "+ Config.LISTEN_SPIEL.getSelected().getTitel());
    }
    /// <summary>
    /// Blendet Grenzen bei allen ein
    /// </summary>
    public void ListenGrenzenAnzeigen()
    {
        ListenAnzeigen.SetActive(true);
        SortierungOben.SetActive(true);
        SortierungOben.GetComponentInChildren<TMP_Text>().text = "▲ " + Config.LISTEN_SPIEL.getSelected().getSortByDisplay().Replace(" - ", "|").Split('|')[0];
        SortierungUnten.SetActive(true);
        SortierungUnten.GetComponentInChildren<TMP_Text>().text = "▼ " + Config.LISTEN_SPIEL.getSelected().getSortByDisplay().Replace(" - ", "|").Split('|')[1];

        ServerUtils.AddBroadcast("#ListenGrenzen [OBEN]"+ Config.LISTEN_SPIEL.getSelected().getSortByDisplay().Replace(" - ", "|").Split('|')[0] + "[OBEN][UNTEN]"+ Config.LISTEN_SPIEL.getSelected().getSortByDisplay().Replace(" - ", "|").Split('|')[1]+"[UNTEN]");
    }
    /// <summary>
    /// Blendet die Auswahlelemente bei allen ein
    /// </summary>
    public void ListenAuswahlAnzeigen()
    {
        string msg = "";
        ListenAnzeigen.SetActive(true);
        AuswahlTitel.SetActive(true);
        for (int i = 0; i < 30; i++)
        {
            if (i < Config.LISTEN_SPIEL.getSelected().getAuswahlElemente().Count)
            {
                Element element = Config.LISTEN_SPIEL.getSelected().getAuswahlElemente()[i];
                msg += "[" + (i + 1) + "]" + element.getItem() + "[" + (i + 1) + "]";
                AuswahlElemente[i].SetActive(true);
                AuswahlElemente[i].GetComponentInChildren<TMP_Text>().text = element.getItem();
                AuswahlElemente[i].transform.GetChild(2).GetComponentInChildren<TMP_Text>().text = "" + (i + 1);
            }
        }

        ServerUtils.AddBroadcast("#ListenAuswahl "+ Config.LISTEN_SPIEL.getSelected().getAuswahlElemente().Count + "<#!#>" + msg);
    }
    /// <summary>
    /// Löst die bereits gewählten Elemente mit details auf
    /// </summary>
    public void ListenAuflösen()
    {
        listewirdaufgeloest = true;
        // Bereits eingefügtes mit Infos versehen
        string msg = "";
        for (int i = 0; i < 30; i++)
        {
            if (GridElemente[i].activeInHierarchy)
            {
                Element element = Config.LISTEN_SPIEL.getSelected().getAlleElemente()[i];
                GridElemente[i].GetComponentInChildren<TMP_Text>().text = element.getItem() + " - " + element.getDisplay();
                GridElemente[i].transform.GetChild(0).gameObject.SetActive(true);

                msg += "[]"+i+"[TRENNER]" + element.getItem() + " - " + element.getDisplay();
            }
        }
        if (msg.Length > 2)
            msg = msg.Substring(2);
        ServerUtils.AddBroadcast("#ListenAufloesen " + msg);
    }
    /// <summary>
    /// Fügt ein Element aus der Auswahl in die Sortierung ein
    /// </summary>
    /// <param name="element">Element</param>
    public void ListenElementEinfuegen(GameObject element)
    {
        Logging.log(Logging.LogType.Debug, "ListenServer", "ListenElementEinfügen", "Element wird eingefügt: " + element.name);
        int auswahlIndex = Int32.Parse(element.transform.GetChild(2).GetComponentInChildren<TMP_Text>().text)-1;
        Element item = Config.LISTEN_SPIEL.getSelected().getAuswahlElemente()[auswahlIndex];
        int loesIndex = Config.LISTEN_SPIEL.getSelected().getAlleFromAuswahl(item);

        AuswahlElemente[auswahlIndex].GetComponentInChildren<TMP_Text>().text = ""; // Auswahl Ausblenden
        LoesungGrid[loesIndex+2].transform.GetChild(0).gameObject.SetActive(true); // in Lösung markieren
        GridElemente[loesIndex].SetActive(true); // Für alle Anzeigen
        Element loesElement = Config.LISTEN_SPIEL.getSelected().getAlleElemente()[loesIndex];
        if (listewirdaufgeloest)
            GridElemente[loesIndex].GetComponentInChildren<TMP_Text>().text = loesElement.getItem() +" - "+loesElement.getDisplay();
        else
            GridElemente[loesIndex].GetComponentInChildren<TMP_Text>().text = loesElement.getItem();
        ListenElementIdsAktualisieren();

        if (listewirdaufgeloest)
            ServerUtils.AddBroadcast("#ListenElementEinfuegen [INDEX]" + loesIndex + "[INDEX][ELEMENT]" + loesElement.getItem() + " - " + loesElement.getDisplay() + "[ELEMENT][AUSWAHLINDEX]"+ auswahlIndex+"[AUSWAHLINDEX]");
        else
            ServerUtils.AddBroadcast("#ListenElementEinfuegen [INDEX]" + loesIndex + "[INDEX][ELEMENT]" + loesElement.getItem() + "[ELEMENT][AUSWAHLINDEX]"+ auswahlIndex + "[AUSWAHLINDEX]");
    }
    /// <summary>
    /// Gibt bei einem Eingefügen Element die Sortdisplay an
    /// </summary>
    /// <param name="element">Element</param>
    public void ListenShowSortDisplay(GameObject element)
    {
        if (!Config.isServer)
            return;

        string ele = element.transform.GetChild(1).GetComponentInChildren<TMP_Text>().text;
        Element e = null;
        int index = 0;
        for (int i = 0; i < Config.LISTEN_SPIEL.getSelected().getAlleElemente().Count; i++)
        {
            if (Config.LISTEN_SPIEL.getSelected().getAlleElemente()[i].getItem() == ele)
            {
                e = Config.LISTEN_SPIEL.getSelected().getAlleElemente()[i];
                index = i;
                break;
            }
        }
        if (e == null)
            return;

        element.transform.GetChild(0).gameObject.SetActive(true);
        element.transform.GetChild(1).GetComponentInChildren<TMP_Text>().text = e.getItem() + " - " + e.getDisplay();

        ServerUtils.AddBroadcast("#ListenElementShowDisplay "+index+"[!#!]"+ e.getItem() + " - " + e.getDisplay());
    }
    /// <summary>
    /// ID der Listen werden aktualisiert
    /// </summary>
    private void ListenElementIdsAktualisieren()
    {
        /// Nummeriere Grid Anzeige
        int nummer = 1;
        for (int i = 0; i < 30; i++)
        {
            if (GridElemente[i].activeInHierarchy)
            {
                GridElemente[i].transform.GetChild(2).GetComponentInChildren<TMP_Text>().text = "" + nummer;
                nummer++;
            }
        }

        /// Nummeriere Grid Lösung
        nummer = 1;
        for (int i = 0; i < 30; i++)
        {
            if (LoesungGrid[i+2].transform.GetChild(0).gameObject.activeInHierarchy)
            {
                LoesungGrid[i+2].transform.GetChild(2).GetComponentInChildren<TMP_Text>().text = "" + nummer;
                nummer++;
            }
        }
    }
    #endregion
    /// <summary>
    /// Aktiviert den Icon Rand beim Spieler
    /// </summary>
    /// <param name="button"></param>
    public void SpielerIstDran(GameObject button)
    {
        /// Alle ausblenden
        for (int i = 0; i < 8; i++)
        {
            KeinTeamSpielGrid[i].transform.GetChild(1).gameObject.SetActive(false);
        }
        for (int i = 0; i < 4; i++)
        {
            TeamRotSpielGrid[i].transform.GetChild(1).gameObject.SetActive(false);
        }
        for (int i = 0; i < 4; i++)
        {
            TeamBlauSpielGrid[i].transform.GetChild(1).gameObject.SetActive(false);
        }
        for (int i = 0; i < 2; i++)
        {
            TeamGruenSpielGrid[i].transform.GetChild(1).gameObject.SetActive(false);
        }
        for (int i = 0; i < 2; i++)
        {
            TeamLilaSpielGrid[i].transform.GetChild(1).gameObject.SetActive(false);
        }

        if (button.name.Equals("IstNichtDran"))
        {
            ServerUtils.AddBroadcast("#SpielerIstDran 0");
            return;
        }


        string team = button.transform.parent.parent.parent.name;
        if (team.Equals("Team 1") || team.Equals("Team 2"))
            team = button.transform.parent.parent.parent.parent.name;
        string spielername = button.transform.parent.parent.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text;
        int index = Player.getPosInLists(Player.getIdByName(spielername));

        ServerUtils.AddBroadcast("#SpielerIstDran [TEAM]"+team+"[TEAM][NAME]"+spielername);

        if (team.Equals("KeinTeam"))
        {
            for (int i = 0; i < 8; i++)
            {
                if (spielername == KeinTeamSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text)
                {
                    KeinTeamSpielGrid[i].transform.GetChild(1).gameObject.SetActive(true);
                    return;
                }
            }
        }
        else if (team.Equals("Team Rot"))
        {
            for (int i = 0; i < 4; i++)
            {
                TeamRotSpielGrid[i].transform.GetChild(1).gameObject.SetActive(true);
            }
            return;
        }
        else if (team.Equals("Team Blau"))
        {
            for (int i = 0; i < 4; i++)
            {
                TeamBlauSpielGrid[i].transform.GetChild(1).gameObject.SetActive(true);
            }
            return;
        }
        else if (team.Equals("Team Gruen"))
        {
            for (int i = 0; i < 2; i++)
            {
                TeamGruenSpielGrid[i].transform.GetChild(1).gameObject.SetActive(true);
            }
            return;
        }
        else if (team.Equals("Team Lila"))
        {
            for (int i = 0; i < 2; i++)
            {
                TeamLilaSpielGrid[i].transform.GetChild(1).gameObject.SetActive(true);
            }
            return;
        }
    }
    #region Einstellungen
    /// <summary>
    /// Spiel Verlassen & Zurück in die Lobby laden
    /// </summary>
    public void ZurueckZurTeamWahl()
    {
        LobbyTeamWahl.SetActive(true);
        InGameAnzeige.SetActive(false);

        ServerUtils.AddBroadcast("#ZurueckZurTeamWahl");
    }
    #region Herzen
    /// <summary>
    /// Toggelt die Herzen für alle
    /// </summary>
    /// <param name="toggle">Toggle</param>
    public void ShowHerzenAnzeige(Toggle toggle)
    {
        if (toggle.isOn)
        {
            for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
            {
                spielerherzenzahl[i] = Int32.Parse(GameObject.Find("Einstellungen/HerzenAnzahl").GetComponent<TMP_InputField>().text);
            }
        }
        else
        {
            for (int i = 0; i < Config.SERVER_MAX_CONNECTIONS; i++)
            {
                spielerherzenzahl[i] = 0;
            }
        }

        /// Server Anzeige Aktualisieren
        /// Zuschauer
        string keinteam = "";
        if (KeinTeamSpiel.activeInHierarchy)
        {
            for (int i = 0; i < teamZuschauerIds.Count; i++)
            {
                int id = teamZuschauerIds[i];
                KeinTeamSpielGrid[i].SetActive(true);
                KeinTeamSpielGrid[i].transform.GetChild(5).gameObject.SetActive(toggle.isOn);
                for (int j = 0; j < 4; j++)
                {
                    // Ausblenden
                    KeinTeamSpielGrid[i].transform.GetChild(5).GetChild(j).gameObject.SetActive(false);
                    // Anzahl einblenden
                    if (spielerherzenzahl[Player.getPosInLists(id)] >= (j+1))
                    {
                        KeinTeamSpielGrid[i].transform.GetChild(5).GetChild(j).gameObject.SetActive(true);
                        KeinTeamSpielGrid[i].transform.GetChild(5).GetChild(j).GetChild(0).GetChild(0).gameObject.SetActive(true);
                    }
                }
                keinteam += "[]" + id;
            }
        }
        if (keinteam.Length > 1)
            keinteam = keinteam.Substring("[]".Length);
        /// Team Rot
        string rotteam = "";
        if (TeamRotSpiel.activeInHierarchy)
        {
            for (int i = 0; i < teamRotIds.Count; i++)
            {
                int id = teamRotIds[i];
                TeamRotSpielGrid[i].SetActive(true);
                TeamRotSpielGrid[i].transform.GetChild(5).gameObject.SetActive(toggle.isOn);
                for (int j = 0; j < 4; j++)
                {
                    // Ausblenden
                    TeamRotSpielGrid[i].transform.GetChild(5).GetChild(j).gameObject.SetActive(false);
                    // Anzahl einblenden
                    if (spielerherzenzahl[Player.getPosInLists(id)] >= (j + 1))
                    {
                        TeamRotSpielGrid[i].transform.GetChild(5).GetChild(j).gameObject.SetActive(true);
                        TeamRotSpielGrid[i].transform.GetChild(5).GetChild(j).GetChild(0).GetChild(0).gameObject.SetActive(true);
                    }
                }
                rotteam += "[]" + id;
            }
        }
        if (rotteam.Length > 1)
            rotteam = rotteam.Substring("[]".Length);
        /// Team Blau
        string blauteam = "";
        if (TeamBlauSpiel.activeInHierarchy)
        {
            for (int i = 0; i < teamBlauIds.Count; i++)
            {
                int id = teamBlauIds[i];
                TeamBlauSpielGrid[i].SetActive(true);
                TeamBlauSpielGrid[i].transform.GetChild(5).gameObject.SetActive(toggle.isOn);
                for (int j = 0; j < 4; j++)
                {
                    // Ausblenden
                    TeamBlauSpielGrid[i].transform.GetChild(5).GetChild(j).gameObject.SetActive(false);
                    // Anzahl einblenden
                    if (spielerherzenzahl[Player.getPosInLists(id)] >= (j + 1))
                    {
                        TeamBlauSpielGrid[i].transform.GetChild(5).GetChild(j).gameObject.SetActive(true);
                        TeamBlauSpielGrid[i].transform.GetChild(5).GetChild(j).GetChild(0).GetChild(0).gameObject.SetActive(true);
                    }
                }
                blauteam += "[]" + id;
            }
        }
        if (blauteam.Length > 1)
            blauteam = blauteam.Substring("[]".Length);
        /// Team Gruen
        string gruenteam = "";
        if (TeamGruenSpiel.activeInHierarchy)
        {
            for (int i = 0; i < teamGruenIds.Count; i++)
            {
                int id = teamGruenIds[i];
                TeamGruenSpielGrid[i].SetActive(true);
                TeamGruenSpielGrid[i].transform.GetChild(5).gameObject.SetActive(toggle.isOn);
                for (int j = 0; j < 4; j++)
                {
                    // Ausblenden
                    TeamGruenSpielGrid[i].transform.GetChild(5).GetChild(j).gameObject.SetActive(false);
                    // Anzahl einblenden
                    if (spielerherzenzahl[Player.getPosInLists(id)] >= (j + 1))
                    {
                        TeamGruenSpielGrid[i].transform.GetChild(5).GetChild(j).gameObject.SetActive(true);
                        TeamGruenSpielGrid[i].transform.GetChild(5).GetChild(j).GetChild(0).GetChild(0).gameObject.SetActive(true);
                    }
                }
                gruenteam += "[]" + id;
            }
        }
        if (gruenteam.Length > 1)
            gruenteam = gruenteam.Substring("[]".Length);
        /// Team Lila
        string lilateam = "";
        if (TeamLilaSpiel.activeInHierarchy)
        {
            for (int i = 0; i < teamLilaIds.Count; i++)
            {
                int id = teamLilaIds[i];
                TeamLilaSpielGrid[i].SetActive(true);
                TeamLilaSpielGrid[i].transform.GetChild(5).gameObject.SetActive(toggle.isOn);
                for (int j = 0; j < 4; j++)
                {
                    // Ausblenden
                    TeamLilaSpielGrid[i].transform.GetChild(5).GetChild(j).gameObject.SetActive(false);
                    // Anzahl einblenden
                    if (spielerherzenzahl[Player.getPosInLists(id)] >= (j + 1))
                    {
                        TeamLilaSpielGrid[i].transform.GetChild(5).GetChild(j).gameObject.SetActive(true);
                        TeamLilaSpielGrid[i].transform.GetChild(5).GetChild(j).GetChild(0).GetChild(0).gameObject.SetActive(true);
                    }
                }
                lilateam += "[]" + id;
            }
        }
        if (lilateam.Length > 1)
            lilateam = lilateam.Substring("[]".Length);

        ServerUtils.AddBroadcast("#HerzenEinblenden [BOOL]" + toggle.isOn + "[BOOL][ZAHL]"+ GameObject.Find("Einstellungen/HerzenAnzahl").GetComponent<TMP_InputField>().text + "[ZAHL][KEINTEAM]"+keinteam+"[KEINTEAM][TEAMROT]"+rotteam+"[TEAMROT][TEAMBLAU]"+blauteam+"[TEAMBLAU][TEAMGRUEN]"+gruenteam+"[TEAMGRUEN][TEAMLILA]"+lilateam+"[TEAMLILA]");
    }
    /// <summary>
    /// Ändert die Anzahl der Herzen
    /// </summary>
    /// <param name="input"></param>
    public void ChangeHerzenAnzahl(TMP_InputField input)
    {
        int neu = Int32.Parse(input.text);
        if (neu >= 0 && neu <= 4)
            input.text = neu + "";
        else
        {
            if (neu > 4)
                input.text = "4";
            else
                input.text = "0";
        }    
    }
    /// <summary>
    /// Füllt bereits verlorene Herzen komplett auf
    /// </summary>
    /// <param name="button"></param>
    public void HerzenFuellen(GameObject button)
    {
        string team = button.transform.parent.parent.parent.name;
        if (team.Equals("Team 1") || team.Equals("Team 2"))
            team = button.transform.parent.parent.parent.parent.name;
        string spielername = button.transform.parent.parent.GetChild(4).GetChild(1).gameObject.GetComponent<TMP_Text>().text;
        int playerid = Player.getIdByName(spielername);
        int index = Player.getPosInLists(playerid);
        spielerherzenzahl[index] = Int32.Parse(GameObject.Find("Einstellungen/HerzenAnzahl").GetComponent<TMP_InputField>().text);

        // Spieler aktualisieren
        if (teamZuschauerIds.Contains(playerid))
        {
            for (int i = 0; i < teamZuschauerIds.Count; i++)
            {
                if (teamZuschauerIds[i] == playerid)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        // Ausblenden
                        KeinTeamSpielGrid[i].transform.GetChild(5).GetChild(j).gameObject.SetActive(false);
                        // Anzahl einblenden
                        if (spielerherzenzahl[Player.getPosInLists(playerid)] >= (j + 1))
                        {
                            KeinTeamSpielGrid[i].transform.GetChild(5).GetChild(j).gameObject.SetActive(true);
                            KeinTeamSpielGrid[i].transform.GetChild(5).GetChild(j).GetChild(0).GetChild(0).gameObject.SetActive(true);
                        }
                    }
                    ServerUtils.AddBroadcast("#HerzenFuellen [TEAM]KEINTEAM[TEAM][ID]"+ playerid + "[ID][ZAHL]"+spielerherzenzahl[Player.getPosInLists(playerid)]+"[ZAHL]");
                    break;
                }
            }
        }
        else if (teamRotIds.Contains(playerid))
        {
            for (int i = 0; i < teamRotIds.Count; i++)
            {
                if (teamRotIds[i] == playerid)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        // Ausblenden
                        TeamRotSpielGrid[i].transform.GetChild(5).GetChild(j).gameObject.SetActive(false);
                        // Anzahl einblenden
                        if (spielerherzenzahl[Player.getPosInLists(playerid)] >= (j + 1))
                        {
                            TeamRotSpielGrid[i].transform.GetChild(5).GetChild(j).gameObject.SetActive(true);
                            TeamRotSpielGrid[i].transform.GetChild(5).GetChild(j).GetChild(0).GetChild(0).gameObject.SetActive(true);
                        }
                    }
                    ServerUtils.AddBroadcast("#HerzenFuellen [TEAM]TEAMROT[TEAM][ID]" + playerid + "[ID][ZAHL]" + spielerherzenzahl[Player.getPosInLists(playerid)] + "[ZAHL]");
                    break;
                }
            }
        }
        else if (teamBlauIds.Contains(playerid))
        {
            for (int i = 0; i < teamBlauIds.Count; i++)
            {
                if (teamBlauIds[i] == playerid)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        // Ausblenden
                        TeamBlauSpielGrid[i].transform.GetChild(5).GetChild(j).gameObject.SetActive(false);
                        // Anzahl einblenden
                        if (spielerherzenzahl[Player.getPosInLists(playerid)] >= (j + 1))
                        {
                            TeamBlauSpielGrid[i].transform.GetChild(5).GetChild(j).gameObject.SetActive(true);
                            TeamBlauSpielGrid[i].transform.GetChild(5).GetChild(j).GetChild(0).GetChild(0).gameObject.SetActive(true);
                        }
                    }
                    ServerUtils.AddBroadcast("#HerzenFuellen [TEAM]TEAMBLAU[TEAM][ID]" + playerid + "[ID][ZAHL]" + spielerherzenzahl[Player.getPosInLists(playerid)] + "[ZAHL]");
                    break;
                }
            }
        }
        else if (teamGruenIds.Contains(playerid))
        {
            for (int i = 0; i < teamGruenIds.Count; i++)
            {
                if (teamGruenIds[i] == playerid)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        // Ausblenden
                        TeamGruenSpielGrid[i].transform.GetChild(5).GetChild(j).gameObject.SetActive(false);
                        // Anzahl einblenden
                        if (spielerherzenzahl[Player.getPosInLists(playerid)] >= (j + 1))
                        {
                            TeamGruenSpielGrid[i].transform.GetChild(5).GetChild(j).gameObject.SetActive(true);
                            TeamGruenSpielGrid[i].transform.GetChild(5).GetChild(j).GetChild(0).GetChild(0).gameObject.SetActive(true);
                        }
                    }
                    ServerUtils.AddBroadcast("#HerzenFuellen [TEAM]TEAMGRUEN[TEAM][ID]" + playerid + "[ID][ZAHL]" + spielerherzenzahl[Player.getPosInLists(playerid)] + "[ZAHL]");
                    break;
                }
            }
        }
        else if (teamLilaIds.Contains(playerid))
        {
            for (int i = 0; i < teamLilaIds.Count; i++)
            {
                if (teamLilaIds[i] == playerid)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        // Ausblenden
                        TeamLilaSpielGrid[i].transform.GetChild(5).GetChild(j).gameObject.SetActive(false);
                        // Anzahl einblenden
                        if (spielerherzenzahl[Player.getPosInLists(playerid)] >= (j + 1))
                        {
                            TeamLilaSpielGrid[i].transform.GetChild(5).GetChild(j).gameObject.SetActive(true);
                            TeamLilaSpielGrid[i].transform.GetChild(5).GetChild(j).GetChild(0).GetChild(0).gameObject.SetActive(true);
                        }
                    }
                    ServerUtils.AddBroadcast("#HerzenFuellen [TEAM]TEAMLILA[TEAM][ID]" + playerid + "[ID][ZAHL]" + spielerherzenzahl[Player.getPosInLists(playerid)] + "[ZAHL]");
                    break;
                }
            }
        }
        else
        {
            Logging.log(Logging.LogType.Error, "ListenServer", "HerzenFuellen", "Fehler beim Füllen der Herzen " + button.name);
        }
    }
    /// <summary>
    /// Nimmt einem Spieler ein herz ab
    /// </summary>
    /// <param name="button"></param>
    public void HerzenAbziehen(GameObject button) 
    {
        string team = button.transform.parent.parent.parent.name;
        if (team.Equals("Team 1") || team.Equals("Team 2"))
            team = button.transform.parent.parent.parent.parent.name;
        string spielername = button.transform.parent.parent.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text;
        int index = Player.getPosInLists(Player.getIdByName(spielername));
        spielerherzenzahl[index]--;
        ServerUtils.AddBroadcast("#HerzenAbziehen [TEAM]"+team+"[TEAM][NAME]"+spielername+"[NAME][HERZID]"+spielerherzenzahl[index]+"[HERZID]");

        if (team.Equals("KeinTeam"))
        {
            for (int i = 0; i < 8; i++)
            {
                if (spielername == KeinTeamSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text)
                {
                    KeinTeamSpielGrid[i].transform.GetChild(5).GetChild(spielerherzenzahl[index]).GetChild(0).GetChild(0).gameObject.SetActive(false);
                    return;
                }
            }
        }
        else if (team.Equals("Team Rot"))
        {
            for (int i = 0; i < 4; i++)
            {
                if (i < teamRotIds.Count)
                    TeamRotSpielGrid[i].transform.GetChild(5).GetChild(spielerherzenzahl[index]).GetChild(0).GetChild(0).gameObject.SetActive(false);
            }
        }
        else if (team.Equals("Team Blau"))
        {
            for (int i = 0; i < 4; i++)
            {
                if (i < teamBlauIds.Count)
                    TeamBlauSpielGrid[i].transform.GetChild(5).GetChild(spielerherzenzahl[index]).GetChild(0).GetChild(0).gameObject.SetActive(false);
            }
        }
        else if (team.Equals("Team Gruen"))
        {
            for (int i = 0; i < 2; i++)
            {
                if (i < teamGruenIds.Count)
                    TeamGruenSpielGrid[i].transform.GetChild(5).GetChild(spielerherzenzahl[index]).GetChild(0).GetChild(0).gameObject.SetActive(false);
            }
        }
        else if (team.Equals("Team Lila"))
        {
            for (int i = 0; i < 2; i++)
            {
                if (i < teamLilaIds.Count)
                    TeamLilaSpielGrid[i].transform.GetChild(5).GetChild(spielerherzenzahl[index]).GetChild(0).GetChild(0).gameObject.SetActive(false);
            }
        }
    }
    #endregion
    /// <summary>
    /// Spiel wird verlassen - Zurück ins hauptmenü
    /// </summary>
    public void SpielVerlassenButton()
    {
        //SceneManager.LoadScene("Startup");
        ServerUtils.AddBroadcast("#ZurueckInsHauptmenue");
    }
    /// <summary>
    /// Toggelt die Anzeige für alle ob jemand austabt
    /// </summary>
    /// <param name="toggle">Toggle</param>
    public void SpielerAusgetabtAllenAnzeige(Toggle toggle)
    {
        AusgetabtAnzeige.SetActive(toggle.isOn);
        if (toggle.isOn == false)
            ServerUtils.AddBroadcast("#SpielerAusgetabt 0");
    }
    /// <summary>
    /// Spieler Tabt aus, wird ggf allen gezeigt
    /// </summary>
    /// <param name="player">Spieler</param>
    /// <param name="data">bool</param>
    private void ClientFocusChange(Player player, string data)
    {
        bool ausgetabt = !Boolean.Parse(data);
        // Update Client Anzeige
        if (GameObject.Find("Einstellungen/AusgetabtSpielernZeigenToggle").GetComponent<Toggle>().isOn)
            ServerUtils.AddBroadcast("#SpielerAusgetabt " + player.name + "[]" + ausgetabt);
        // Update Server Anzeige
        // KeinTeam
        for (int i = 0; i < 8; i++)
        {
            if (KeinTeamSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text == player.name)
            {
                KeinTeamSpielGrid[i].transform.GetChild(3).gameObject.SetActive(ausgetabt);
                return;
            }
        }
        // Rot Team
        for (int i = 0; i < 4; i++)
        {
            if (TeamRotSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text == player.name)
            {
                TeamRotSpielGrid[i].transform.GetChild(3).gameObject.SetActive(ausgetabt);
                return;
            }
        }
        // Blau Team
        for (int i = 0; i < 4; i++)
        {
            if (TeamBlauSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text == player.name)
            {
                TeamBlauSpielGrid[i].transform.GetChild(3).gameObject.SetActive(ausgetabt);
                return;
            }
        }
        // Gruen Team
        for (int i = 0; i < 2; i++)
        {
            if (TeamGruenSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text == player.name)
            {
                TeamGruenSpielGrid[i].transform.GetChild(3).gameObject.SetActive(ausgetabt);
                return;
            }
        }
        // Lila Team
        for (int i = 0; i < 2; i++)
        {
            if (TeamLilaSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text == player.name)
            {
                TeamLilaSpielGrid[i].transform.GetChild(3).gameObject.SetActive(ausgetabt);
                return;
            }
        }
    }
    /// <summary>
    /// Ändert die Punkte des Spielers, variable Punkte
    /// </summary>
    /// <param name="input"></param>
    public void PunkteManuellAendern(TMP_InputField input)
    {
        string spielername = input.transform.parent.parent.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text;
        int punkte = Int32.Parse(input.text);
        int tindex = Player.getPosInLists(Player.getIdByName(spielername));
        Config.PLAYERLIST[tindex].points += punkte;
        input.text = "";
        ServerUtils.AddBroadcast("#UpdateSpielerPunkte " + spielername +"[]"+ Config.PLAYERLIST[tindex].points);

        // KeinTeam InGame
        for (int i = 0; i < 8; i++)
        {
            int pindex = Player.getPosInLists(Player.getIdByName(KeinTeamSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text));
            if (pindex == -1)
                continue;
            KeinTeamSpielGrid[i].transform.GetChild(4).GetChild(2).GetComponent<TMP_Text>().text = Config.PLAYERLIST[pindex].points + "";
        }
        // Team Rot InGame
        for (int i = 0; i < 4; i++)
        {
            int pindex = Player.getPosInLists(Player.getIdByName(TeamRotSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text));
            if (pindex == -1)
                continue;
            TeamRotSpielGrid[i].transform.GetChild(4).GetChild(2).GetComponent<TMP_Text>().text = Config.PLAYERLIST[pindex].points + "";
        }
        // Team Blau InGame
        for (int i = 0; i < 4; i++)
        {
            int pindex = Player.getPosInLists(Player.getIdByName(TeamBlauSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text));
            if (pindex == -1)
                continue;
            TeamBlauSpielGrid[i].transform.GetChild(4).GetChild(2).GetComponent<TMP_Text>().text = Config.PLAYERLIST[pindex].points + "";
        }
        // Team Gruen InGame
        for (int i = 0; i < 2; i++)
        {
            int pindex = Player.getPosInLists(Player.getIdByName(TeamGruenSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text));
            if (pindex == -1)
                continue;
            TeamGruenSpielGrid[i].transform.GetChild(4).GetChild(2).GetComponent<TMP_Text>().text = Config.PLAYERLIST[pindex].points + "";
        }
        // Team Lila InGame
        for (int i = 0; i < 2; i++)
        {
            int pindex = Player.getPosInLists(Player.getIdByName(TeamLilaSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text));
            if (pindex == -1)
                continue;
            TeamLilaSpielGrid[i].transform.GetChild(4).GetChild(2).GetComponent<TMP_Text>().text = Config.PLAYERLIST[pindex].points + "";
        }
        // Zuschauer Lobby
        for (int i = 0; i < 8; i++)
        {
            int pindex = Player.getPosInLists(Player.getIdByName(TeamZuschauer[i, 5].GetComponent<TMP_Text>().text));
            if (pindex == -1)
                continue;
            TeamZuschauer[i, 6].GetComponent<TMP_Text>().text = Config.PLAYERLIST[pindex].points + "";
        }
        // Team Rot Lobby
        for (int i = 0; i < 4; i++)
        {
            int pindex = Player.getPosInLists(Player.getIdByName(TeamRot[i, 5].GetComponent<TMP_Text>().text));
            if (pindex == -1)
                continue;
            TeamRot[i, 6].GetComponent<TMP_Text>().text = Config.PLAYERLIST[pindex].points + "";
        }
        // Team Blau Lobby
        for (int i = 0; i < 4; i++)
        {
            int pindex = Player.getPosInLists(Player.getIdByName(TeamBlau[i, 5].GetComponent<TMP_Text>().text));
            if (pindex == -1)
                continue;
            TeamBlau[i, 6].GetComponent<TMP_Text>().text = Config.PLAYERLIST[pindex].points + "";
        }
        // Team Gruen Lobby
        for (int i = 0; i < 2; i++)
        {
            int pindex = Player.getPosInLists(Player.getIdByName(TeamGruen[i, 5].GetComponent<TMP_Text>().text));
            if (pindex == -1)
                continue;
            TeamGruen[i, 6].GetComponent<TMP_Text>().text = Config.PLAYERLIST[pindex].points + "";
        }
        // Team Lila Lobby
        for (int i = 0; i < 2; i++)
        {
            int pindex = Player.getPosInLists(Player.getIdByName(TeamLila[i, 5].GetComponent<TMP_Text>().text));
            if (pindex == -1)
                continue;
            TeamLila[i, 6].GetComponent<TMP_Text>().text = Config.PLAYERLIST[pindex].points + "";
        }
    }
    /// <summary>
    /// Spielt den Sound für richtige Antworten ab
    /// </summary>
    public void PlayRichtigeAntwort()
    {
        ServerUtils.AddBroadcast("#AudioRichtigeAntwort");
        RichtigeAntwortSound.Play();
    }
    /// <summary>
    /// Spielt den Sound für falsche Antworten ab
    /// </summary>
    public void PlayFalscheAntwort()
    {
        ServerUtils.AddBroadcast("#AudioFalscheAntwort");
        FalscheAntwortSound.Play();
    }
    #endregion
    /// <summary>
    /// Ändert die ausgewählte Liste
    /// </summary>
    /// <param name="drop">Dropauswahl</param>
    public void ChangeList(TMP_Dropdown drop)
    {
        Config.LISTEN_SPIEL.setSelected(Config.LISTEN_SPIEL.getListe(drop.value));
        drop.value = Config.LISTEN_SPIEL.getIndex(Config.LISTEN_SPIEL.getSelected());

        GameObject.Find("Einstellungen/Quelle").GetComponent<TMP_InputField>().text = Config.LISTEN_SPIEL.getSelected().getQuelle();

        ServerUtils.AddBroadcast("#ZurueckZurTeamWahl");

        LobbyTeamWahl.SetActive(true);
        InGameAnzeige.SetActive(false);
        // Anzeigen Leeren
        Titel.SetActive(false);
        SortierungOben.SetActive(false);
        SortierungUnten.SetActive(false);
        for (int i = 0; i < GridElemente.Length; i++)
        {
            GridElemente[i].transform.GetChild(0).gameObject.SetActive(false);
            GridElemente[i].transform.GetChild(1).GetComponent<TMP_Text>().text = "";
            GridElemente[i].transform.GetChild(2).GetComponentInChildren<TMP_Text>().text = "";
            GridElemente[i].SetActive(false);
        }
        AuswahlTitel.SetActive(false);
        for (int i = 0; i < AuswahlElemente.Length; i++)
        {
            AuswahlElemente[i].transform.GetChild(0).gameObject.SetActive(false);
            AuswahlElemente[i].transform.GetChild(1).GetComponent<TMP_Text>().text = "";
            AuswahlElemente[i].transform.GetChild(2).GetComponentInChildren<TMP_Text>().text = "";
            AuswahlElemente[i].SetActive(false);
        }
        for (int i = 2; i < LoesungGrid.Length; i++)
        {
            LoesungGrid[i].transform.GetChild(0).gameObject.SetActive(false);
            LoesungGrid[i].transform.GetChild(1).GetComponent<TMP_Text>().text = "";
            LoesungGrid[i].transform.GetChild(2).GetComponentInChildren<TMP_Text>().text = "";
            LoesungGrid[i].SetActive(false);
        }
    }
}