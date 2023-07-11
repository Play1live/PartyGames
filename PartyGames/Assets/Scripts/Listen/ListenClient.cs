using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ListenClient : MonoBehaviour
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
    #region Client
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
    #endregion
    #endregion

    [SerializeField] AudioSource BuzzerSound;
    [SerializeField] AudioSource RichtigeAntwortSound;
    [SerializeField] AudioSource FalscheAntwortSound;

    void OnEnable()
    {
        InitAnzeigen();

        if (!Config.CLIENT_STARTED)
            return;
        ClientUtils.SendToServer("#JoinListen");

        StartCoroutine(TestConnectionToServer());
    }

    void Update()
    {
        #region Prüft auf Nachrichten vom Server
        if (Config.CLIENT_STARTED)
        {
            NetworkStream stream = Config.CLIENT_TCP.GetStream();
            if (stream.DataAvailable)
            {
                StreamReader reader = new StreamReader(stream);
                string data = reader.ReadLine();
                if (data != null)
                    OnIncomingData(data);
            }
        }
        #endregion
    }

    private void OnApplicationFocus(bool focus)
    {
        ClientUtils.SendToServer("#ClientFocusChange " + focus);
    }

    private void OnApplicationQuit()
    {
        Logging.log(Logging.LogType.Normal, "ListenClient", "OnApplicationQuit", "Client wird geschlossen.");
        ClientUtils.SendToServer("#ClientClosed");
        CloseSocket();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    /// <summary>
    /// Testet die Verbindung zum Server
    /// </summary>
    IEnumerator TestConnectionToServer()
    {
        while (Config.CLIENT_STARTED)
        {
            ClientUtils.SendToServer("#TestConnection");
            yield return new WaitForSeconds(10);
        }
        yield break;
    }
    #region Verbindungen
    /// <summary>
    /// Trennt die Verbindung zum Server
    /// </summary>
    private void CloseSocket()
    {
        if (!Config.CLIENT_STARTED)
            return;

        Config.CLIENT_TCP.Close();
        Config.CLIENT_STARTED = false;

        Logging.log(Logging.LogType.Normal, "ListenClient", "CloseSocket", "Verbindung zum Server wurde getrennt. Client wird in das Hauptmenü geladen.");
    }
    #endregion
    #region Kommunikation
    /// <summary>
    /// Einkommende Nachrichten die vom Sever
    /// </summary>
    /// <param name="data">Nachricht</param>
    private void OnIncomingData(string data)
    {
        if (data.StartsWith(Config.GAME_TITLE + "#"))
            data = data.Substring(Config.GAME_TITLE.Length);
        else
            Logging.log(Logging.LogType.Error, "ListenClient", "OnIncommingData", "Wrong Command format: " + data);

        string cmd;
        if (data.Contains(" "))
        {
            cmd = data.Split(' ')[0];
            data = data.Substring(cmd.Length + 1);
        }
        else
            cmd = data;

        Commands(data, cmd);
    }
    #endregion
    /// <summary>
    /// Eingehende Commands vom Server
    /// </summary>
    /// <param name="data">Befehlsargumente</param>
    /// <param name="cmd">Befehl</param>
    private void Commands(string data, string cmd)
    {
        Logging.log(Logging.LogType.Debug, "ListenClient", "Commands", "Eingehende Nachricht: " + cmd + " -> " + data);
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "ListenClient", "Commands", "Unkown Command: " + cmd + " -> " + data);
                break;

            #region Universal Commands
            case "#ServerClosed":
                Logging.log(Logging.LogType.Normal, "ListenClient", "Commands", "Verbindung zum Server getrennt. Lade ins Hauptmenü.");
                CloseSocket();
                SceneManager.LoadSceneAsync("Startup");
                break;
            case "#UpdateRemoteConfig":
                Logging.log(Logging.LogType.Normal, "ListenClient", "Commands", "Aktualisiere die RemoteConfig.");
                LoadConfigs.FetchRemoteConfig();
                break;
            case "#ZurueckInsHauptmenue":
                Logging.log(Logging.LogType.Normal, "ListenClient", "Commands", "Beende das Spiel und lade ins Hauptmenü.");
                SceneManager.LoadSceneAsync("Startup");
                break;
            #endregion

            case "#UpdateSpieler":
                UpdateSpieler(data);
                break;
            #region Listen
            case "#ListenStart":
                ListenStart();
                break;
            case "#ListenTitel":
                ListenTitel(data);
                break;
            case "#ListenAuswahl":
                ListenAuswahl(data);
                break;
            case "#ListenGrenzen":
                ListenGrenzen(data);
                break;
            case "#ListenAufloesen":
                ListenAufloesen(data);
                break;
            case "#ListenElementEinfuegen":
                ListenElementEinfuegen(data);
                break;
            case "#ListenElementShowDisplay":
                ListenShowSortDisplay(data);
                break;
            #endregion

            case "#HerzenEinblenden":
                HerzenEinblenden(data);
                break;
            case "#HerzenFuellen":
                HerzenFuellen(data);
                break;
            case "#HerzenAbziehen":
                HerzenAbziehen(data);
                break;

            case "#UpdateSpielerPunkte":
                UpdateSpielerPunkte(data);
                break;
            case "#SpielerAusgetabt":
                SpielerAusgetabt(data);
                break;
            case "#SpielerIstDran":
                SpielerIstDran(data);
                break;
            case "#ZurueckZurTeamWahl":
                ZurueckZurTeamWahl();
                break;

        }
    }
    /// <summary>
    /// Initialisiert die Anzeigen der Scene
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

        GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Zuschauer/Spacer").transform.GetChild(0).gameObject.SetActive(false);
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
        #region Client
        KeinTeamSpiel = GameObject.Find("InGameAnzeige/Client/KeinTeam");
        KeinTeamSpielGrid = new GameObject[8];
        for (int j = 1; j <= 4; j++)
        {
            for (int i = 1; i <= 2; i++)
            {
                KeinTeamSpielGrid[(j - 1) * 2 + (i - 1)] = GameObject.Find("InGameAnzeige/Client/KeinTeam/Team " + i + "/Spieler (" + j + ")");
                KeinTeamSpielGrid[(j - 1) * 2 + (i - 1)].transform.GetChild(0).gameObject.SetActive(false);
                KeinTeamSpielGrid[(j - 1) * 2 + (i - 1)].transform.GetChild(1).gameObject.SetActive(false);
                KeinTeamSpielGrid[(j - 1) * 2 + (i - 1)].transform.GetChild(3).gameObject.SetActive(false);
                KeinTeamSpielGrid[(j - 1) * 2 + (i - 1)].transform.GetChild(5).gameObject.SetActive(false);
                KeinTeamSpielGrid[(j - 1) * 2 + (i - 1)].SetActive(false);
            }
        }
        KeinTeamSpiel.SetActive(false);
        TeamRotSpiel = GameObject.Find("InGameAnzeige/Client/Team Rot");
        TeamRotSpielGrid = new GameObject[4];
        for (int i = 1; i <= 4; i++)
        {
            TeamRotSpielGrid[i - 1] = GameObject.Find("InGameAnzeige/Client/Team Rot/Spieler (" + i + ")");
            TeamRotSpielGrid[i - 1].transform.GetChild(0).gameObject.SetActive(false);
            TeamRotSpielGrid[i - 1].transform.GetChild(1).gameObject.SetActive(false);
            TeamRotSpielGrid[i - 1].transform.GetChild(3).gameObject.SetActive(false);
            TeamRotSpielGrid[i - 1].transform.GetChild(5).gameObject.SetActive(false);
            TeamRotSpielGrid[i - 1].SetActive(false);
        }
        TeamRotSpiel.SetActive(false);
        TeamBlauSpiel = GameObject.Find("InGameAnzeige/Client/Team Blau");
        TeamBlauSpielGrid = new GameObject[4];
        for (int i = 1; i <= 4; i++)
        {
            TeamBlauSpielGrid[i - 1] = GameObject.Find("InGameAnzeige/Client/Team Blau/Spieler (" + i + ")");
            TeamBlauSpielGrid[i - 1].transform.GetChild(0).gameObject.SetActive(false);
            TeamBlauSpielGrid[i - 1].transform.GetChild(1).gameObject.SetActive(false);
            TeamBlauSpielGrid[i - 1].transform.GetChild(3).gameObject.SetActive(false);
            TeamBlauSpielGrid[i - 1].transform.GetChild(5).gameObject.SetActive(false);
            TeamBlauSpielGrid[i - 1].SetActive(false);
        }
        TeamBlauSpiel.SetActive(false);
        TeamGruenSpiel = GameObject.Find("InGameAnzeige/Client/Team Gruen");
        TeamGruenSpielGrid = new GameObject[2];
        for (int i = 1; i <= 2; i++)
        {
            TeamGruenSpielGrid[i - 1] = GameObject.Find("InGameAnzeige/Client/Team Gruen/Spieler (" + i + ")");
            TeamGruenSpielGrid[i - 1].transform.GetChild(0).gameObject.SetActive(false);
            TeamGruenSpielGrid[i - 1].transform.GetChild(1).gameObject.SetActive(false);
            TeamGruenSpielGrid[i - 1].transform.GetChild(3).gameObject.SetActive(false);
            TeamGruenSpielGrid[i - 1].transform.GetChild(5).gameObject.SetActive(false);
            TeamGruenSpielGrid[i - 1].SetActive(false);
        }
        TeamGruenSpiel.SetActive(false);
        TeamLilaSpiel = GameObject.Find("InGameAnzeige/Client/Team Lila");
        TeamLilaSpielGrid = new GameObject[2];
        for (int i = 1; i <= 2; i++)
        {
            TeamLilaSpielGrid[i - 1] = GameObject.Find("InGameAnzeige/Client/Team Lila/Spieler (" + i + ")");
            TeamLilaSpielGrid[i - 1].transform.GetChild(0).gameObject.SetActive(false);
            TeamLilaSpielGrid[i - 1].transform.GetChild(1).gameObject.SetActive(false);
            TeamLilaSpielGrid[i - 1].transform.GetChild(3).gameObject.SetActive(false);
            TeamLilaSpielGrid[i - 1].transform.GetChild(5).gameObject.SetActive(false);
            TeamLilaSpielGrid[i - 1].SetActive(false);
        }
        TeamLilaSpiel.SetActive(false);
        #endregion
        #endregion
    }
    /// <summary>
    /// Aktualisiert die Spieler Anzeigen
    /// </summary>
    /// <param name="data"></param>
    private void UpdateSpieler(string data)
    {
        // LobbyTeamWahl
        if (data.Contains("[ANZEIGE]"))
            if (data.Replace("[ANZEIGE]", "|").Split('|')[1].Equals("LobbyTeamWahl"))
            {
                UpdateSpielerLobby(data);
            }
        // Ingame
        if (data.Contains("[ANZEIGE]"))
            if (data.Replace("[ANZEIGE]", "|").Split('|')[1].Equals("InGame"))
            {
                UpdateSpielerInGame(data);
            }
    }
    /// <summary>
    /// Aktualisiert die Spieler Anzeigen in der Lobby
    /// </summary>
    /// <param name="data"></param>
    private void UpdateSpielerLobby(string data)
    {
        /// Zuschauer
        string team = data.Replace("[ZUSCHAUER]", "|").Split("|")[1];
        bool boolean = bool.Parse(team.Replace("[BOOL]", "|").Split("|")[1]);
        TeamZuschauerGrid.SetActive(boolean);
        string ids = team.Replace("[IDS]", "|").Split("|")[1];
        for (int i = 1; i <= 8; i++)
        {
            TeamZuschauer[i - 1, 0].SetActive(false);
        }
        if (ids.Length > 0)
            for (int i = 0; i < ids.Split(',').Length; i++)
            {
                int id = Int32.Parse(ids.Split(',')[i]);
                int index = Player.getPosInLists(id);
                TeamZuschauer[i, 0].SetActive(true);
                TeamZuschauer[i, 1].SetActive(false);
                TeamZuschauer[i, 2].SetActive(false);
                TeamZuschauer[i, 3].GetComponent<Image>().sprite = Config.PLAYERLIST[Player.getPosInLists(id)].icon;
                TeamZuschauer[i, 4].SetActive(false);
                TeamZuschauer[i, 5].GetComponent<TMP_Text>().text = Config.PLAYERLIST[Player.getPosInLists(id)].name;
                TeamZuschauer[i, 6].GetComponent<TMP_Text>().text = Config.PLAYERLIST[Player.getPosInLists(id)].points+"";
            }
        /// Team Rot
        team = data.Replace("[TEAMROT]", "|").Split("|")[1];
        boolean = bool.Parse(team.Replace("[BOOL]", "|").Split("|")[1]);
        TeamRotGrid.SetActive(boolean);
        if (boolean)
            GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Zuschauer/Spacer").transform.GetChild(0).gameObject.SetActive(true);
        else
            GameObject.Find("LobbyTeamWahl/SpielerAnzeige Server/Team Zuschauer/Spacer").transform.GetChild(0).gameObject.SetActive(false);
        ids = team.Replace("[IDS]", "|").Split("|")[1];
        for (int i = 1; i <= 4; i++)
        {
            TeamRot[i - 1, 0].SetActive(false);
        }
        if (ids.Length > 0)
            for (int i = 0; i < ids.Split(',').Length; i++)
            {
                int id = Int32.Parse(ids.Split(',')[i]);
                int index = Player.getPosInLists(id);
                TeamRot[i, 0].SetActive(true);
                TeamRot[i, 1].SetActive(false);
                TeamRot[i, 2].SetActive(false);
                TeamRot[i, 3].GetComponent<Image>().sprite = Config.PLAYERLIST[Player.getPosInLists(id)].icon;
                TeamRot[i, 4].SetActive(false);
                TeamRot[i, 5].GetComponent<TMP_Text>().text = Config.PLAYERLIST[Player.getPosInLists(id)].name;
                TeamRot[i, 6].GetComponent<TMP_Text>().text = Config.PLAYERLIST[Player.getPosInLists(id)].points + "";
            }
        /// Team Blau
        team = data.Replace("[TEAMBLAU]", "|").Split("|")[1];
        boolean = bool.Parse(team.Replace("[BOOL]", "|").Split("|")[1]);
        TeamBlauGrid.SetActive(boolean);
        ids = team.Replace("[IDS]", "|").Split("|")[1];
        for (int i = 1; i <= 4; i++)
        {
            TeamBlau[i - 1, 0].SetActive(false);
        }
        if (ids.Length > 0)
            for (int i = 0; i < ids.Split(',').Length; i++)
            {
                int id = Int32.Parse(ids.Split(',')[i]);
                int index = Player.getPosInLists(id);
                TeamBlau[i, 0].SetActive(true);
                TeamBlau[i, 1].SetActive(false);
                TeamBlau[i, 2].SetActive(false);
                TeamBlau[i, 3].GetComponent<Image>().sprite = Config.PLAYERLIST[Player.getPosInLists(id)].icon;
                TeamBlau[i, 4].SetActive(false);
                TeamBlau[i, 5].GetComponent<TMP_Text>().text = Config.PLAYERLIST[Player.getPosInLists(id)].name;
                TeamBlau[i, 6].GetComponent<TMP_Text>().text = Config.PLAYERLIST[Player.getPosInLists(id)].points + "";
            }
        /// Team Gruen
        team = data.Replace("[TEAMGRUEN]", "|").Split("|")[1];
        boolean = bool.Parse(team.Replace("[BOOL]", "|").Split("|")[1]);
        TeamGruenGrid.SetActive(boolean);
        ids = team.Replace("[IDS]", "|").Split("|")[1];
        for (int i = 1; i <= 2; i++)
        {
            TeamGruen[i - 1, 0].SetActive(false);
        }
        if (ids.Length > 0)
            for (int i = 0; i < ids.Split(',').Length; i++)
            {
                int id = Int32.Parse(ids.Split(',')[i]);
                int index = Player.getPosInLists(id);
                TeamGruen[i, 0].SetActive(true);
                TeamGruen[i, 1].SetActive(false);
                TeamGruen[i, 2].SetActive(false);
                TeamGruen[i, 3].GetComponent<Image>().sprite = Config.PLAYERLIST[Player.getPosInLists(id)].icon;
                TeamGruen[i, 4].SetActive(false);
                TeamGruen[i, 5].GetComponent<TMP_Text>().text = Config.PLAYERLIST[Player.getPosInLists(id)].name;
                TeamGruen[i, 6].GetComponent<TMP_Text>().text = Config.PLAYERLIST[Player.getPosInLists(id)].points + "";
            }
        /// Team Lila
        team = data.Replace("[TEAMLILA]", "|").Split("|")[1];
        boolean = bool.Parse(team.Replace("[BOOL]", "|").Split("|")[1]);
        TeamLilaGrid.SetActive(boolean);
        ids = team.Replace("[IDS]", "|").Split("|")[1];
        for (int i = 1; i <= 2; i++)
        {
            TeamLila[i - 1, 0].SetActive(false);
        }
        if (ids.Length > 0)
            for (int i = 0; i < ids.Split(',').Length; i++)
            {
                int id = Int32.Parse(ids.Split(',')[i]);
                int index = Player.getPosInLists(id);
                TeamLila[i, 0].SetActive(true);
                TeamLila[i, 1].SetActive(false);
                TeamLila[i, 2].SetActive(false);
                TeamLila[i, 3].GetComponent<Image>().sprite = Config.PLAYERLIST[Player.getPosInLists(id)].icon;
                TeamLila[i, 4].SetActive(false);
                TeamLila[i, 5].GetComponent<TMP_Text>().text = Config.PLAYERLIST[Player.getPosInLists(id)].name;
                TeamLila[i, 6].GetComponent<TMP_Text>().text = Config.PLAYERLIST[Player.getPosInLists(id)].points + "";
            }
    }
    /// <summary>
    /// Aktualisiert die Spieler Anzeigen InGame
    /// </summary>
    /// <param name="data"></param>
    private void UpdateSpielerInGame(string data)
    {
        /// Zuschauer
        string team = data.Replace("[KEINTEAM]", "|").Split("|")[1];
        bool boolean = bool.Parse(team.Replace("[BOOL]", "|").Split("|")[1]);
        KeinTeamSpiel.SetActive(boolean);
        string ids = team.Replace("[IDS]", "|").Split("|")[1];
        for (int i = 0; i < 8; i++)
        {
            KeinTeamSpielGrid[i].SetActive(false);
        }
        if (ids.Length > 0)
            for (int i = 0; i < ids.Split(',').Length; i++)
            {
                int id = Int32.Parse(ids.Split(',')[i]);
                KeinTeamSpielGrid[i].SetActive(true);
                KeinTeamSpielGrid[i].transform.GetChild(2).GetComponent<Image>().sprite = Config.PLAYERLIST[Player.getPosInLists(id)].icon;
                KeinTeamSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text = Config.PLAYERLIST[Player.getPosInLists(id)].name;
                KeinTeamSpielGrid[i].transform.GetChild(4).GetChild(2).GetComponent<TMP_Text>().text = Config.PLAYERLIST[Player.getPosInLists(id)].points + "";
            }
        /// Team Rot
        team = data.Replace("[TEAMROT]", "|").Split("|")[1];
        boolean = bool.Parse(team.Replace("[BOOL]", "|").Split("|")[1]);
        TeamRotSpiel.SetActive(boolean);
        ids = team.Replace("[IDS]", "|").Split("|")[1];
        for (int i = 0; i < 4; i++)
        {
            TeamRotSpielGrid[i].SetActive(false);
        }
        if (ids.Length > 0)
            for (int i = 0; i < ids.Split(',').Length; i++)
            {
                int id = Int32.Parse(ids.Split(',')[i]);
                TeamRotSpielGrid[i].SetActive(true);
                TeamRotSpielGrid[i].transform.GetChild(2).GetComponent<Image>().sprite = Config.PLAYERLIST[Player.getPosInLists(id)].icon;
                TeamRotSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text = Config.PLAYERLIST[Player.getPosInLists(id)].name;
                TeamRotSpielGrid[i].transform.GetChild(4).GetChild(2).GetComponent<TMP_Text>().text = Config.PLAYERLIST[Player.getPosInLists(id)].points + "";
            }
        /// Team Blau
        team = data.Replace("[TEAMBLAU]", "|").Split("|")[1];
        boolean = bool.Parse(team.Replace("[BOOL]", "|").Split("|")[1]);
        TeamBlauSpiel.SetActive(boolean);
        ids = team.Replace("[IDS]", "|").Split("|")[1];
        for (int i = 0; i < 4; i++)
        {
            TeamBlauSpielGrid[i].SetActive(false);
        }
        if (ids.Length > 0)
            for (int i = 0; i < ids.Split(',').Length; i++)
            {
                int id = Int32.Parse(ids.Split(',')[i]);
                TeamBlauSpielGrid[i].SetActive(true);
                TeamBlauSpielGrid[i].transform.GetChild(2).GetComponent<Image>().sprite = Config.PLAYERLIST[Player.getPosInLists(id)].icon;
                TeamBlauSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text = Config.PLAYERLIST[Player.getPosInLists(id)].name;
                TeamBlauSpielGrid[i].transform.GetChild(4).GetChild(2).GetComponent<TMP_Text>().text = Config.PLAYERLIST[Player.getPosInLists(id)].points + "";
            }
        /// Team Gruen
        team = data.Replace("[TEAMGRUEN]", "|").Split("|")[1];
        boolean = bool.Parse(team.Replace("[BOOL]", "|").Split("|")[1]);
        TeamGruenSpiel.SetActive(boolean);
        ids = team.Replace("[IDS]", "|").Split("|")[1];
        for (int i = 0; i < 2; i++)
        {
            TeamGruenSpielGrid[i].SetActive(false);
        }
        if (ids.Length > 0)
            for (int i = 0; i < ids.Split(',').Length; i++)
            {
                int id = Int32.Parse(ids.Split(',')[i]);
                TeamGruenSpielGrid[i].SetActive(true);
                TeamGruenSpielGrid[i].transform.GetChild(2).GetComponent<Image>().sprite = Config.PLAYERLIST[Player.getPosInLists(id)].icon;
                TeamGruenSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text = Config.PLAYERLIST[Player.getPosInLists(id)].name;
                TeamGruenSpielGrid[i].transform.GetChild(4).GetChild(2).GetComponent<TMP_Text>().text = Config.PLAYERLIST[Player.getPosInLists(id)].points + "";
            }
        /// Team Lila
        team = data.Replace("[TEAMLILA]", "|").Split("|")[1];
        boolean = bool.Parse(team.Replace("[BOOL]", "|").Split("|")[1]);
        TeamLilaSpiel.SetActive(boolean);
        ids = team.Replace("[IDS]", "|").Split("|")[1];
        for (int i = 0; i < 2; i++)
        {
            TeamLilaSpielGrid[i].SetActive(false);
        }
        if (ids.Length > 0)
            for (int i = 0; i < ids.Split(',').Length; i++)
            {
                int id = Int32.Parse(ids.Split(',')[i]);
                TeamLilaSpielGrid[i].SetActive(true);
                TeamLilaSpielGrid[i].transform.GetChild(2).GetComponent<Image>().sprite = Config.PLAYERLIST[Player.getPosInLists(id)].icon;
                TeamLilaSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text = Config.PLAYERLIST[Player.getPosInLists(id)].name;
                TeamLilaSpielGrid[i].transform.GetChild(4).GetChild(2).GetComponent<TMP_Text>().text = Config.PLAYERLIST[Player.getPosInLists(id)].points + "";
            }
    }
    /// <summary>
    /// Aktualisiert die Punkte bei den Spielern
    /// </summary>
    /// <param name="data"></param>
    private void UpdateSpielerPunkte(string data)
    {
        string spielername = data.Replace("[]", "|").Split('|')[0];
        int punkte = Int32.Parse(data.Replace("[]", "|").Split('|')[1]);
        int id = Player.getIdByName(spielername);
        int index = Player.getPosInLists(id);
        Config.PLAYERLIST[index].points = punkte;

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
    /// Zeigt an ob ein Spieler ausgetabt ist
    /// </summary>
    /// <param name="data">id bool</param>
    private void SpielerAusgetabt(string data)
    {
        if (data.Equals("0"))
            return;

        string name = data.Replace("[]", "|").Split('|')[0];
        bool ausgetabt = bool.Parse(data.Replace("[]", "|").Split('|')[1]);

        // KeinTeam
        for (int i = 0; i < 8; i++)
        {
            if (KeinTeamSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text == name)
            {
                KeinTeamSpielGrid[i].transform.GetChild(3).gameObject.SetActive(ausgetabt);
                return;
            }
        }
        // Rot Team
        for (int i = 0; i < 4; i++)
        {
            if (TeamRotSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text == name)
            {
                TeamRotSpielGrid[i].transform.GetChild(3).gameObject.SetActive(ausgetabt);
                return;
            }
        }
        // Blau Team
        for (int i = 0; i < 4; i++)
        {
            if (TeamBlauSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text == name)
            {
                TeamBlauSpielGrid[i].transform.GetChild(3).gameObject.SetActive(ausgetabt);
                return;
            }
        }
        // Gruen Team
        for (int i = 0; i < 2; i++)
        {
            if (TeamGruenSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text == name)
            {
                TeamGruenSpielGrid[i].transform.GetChild(3).gameObject.SetActive(ausgetabt);
                return;
            }
        }
        // Lila Team
        for (int i = 0; i < 2; i++)
        {
            if (TeamLilaSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text == name)
            {
                TeamLilaSpielGrid[i].transform.GetChild(3).gameObject.SetActive(ausgetabt);
                return;
            }
        }
    }
    /// <summary>
    /// Versucht einem Team beizutreten
    /// </summary>
    /// <param name="teambutton">Tritt einem Team bei</param>
    public void TeamBeitreten(GameObject teambutton)
    {
        ClientUtils.SendToServer("#LobbyTeamWaehlen " + teambutton.name.Replace("Team",""));
    }
    #region InGameAnzeige
    /// <summary>
    /// Wechselt ins ListenSpiel
    /// </summary>
    private void ListenStart()
    {
        LobbyTeamWahl.SetActive(false);
        InGameAnzeige.SetActive(true);

        HerzenEinblenden("0"); // Blendet Herzen aus
    }
    /// <summary>
    /// Zeigt den Listen Titel an
    /// </summary>
    /// <param name="data">Titel</param>
    private void ListenTitel(string data)
    {
        ListenAnzeigen.SetActive(true);
        Titel.SetActive(true);
        Titel.GetComponent<TMP_Text>().text = data;
    }
    /// <summary>
    /// Zeigt die Grenzen der Liste an
    /// </summary>
    /// <param name="data">Grenzen</param>
    private void ListenGrenzen(string data)
    {
        ListenAnzeigen.SetActive(true);
        SortierungOben.SetActive(true);
        SortierungOben.GetComponentInChildren<TMP_Text>().text = "▲ " + data.Replace("[OBEN]","|").Split('|')[1];
        SortierungUnten.SetActive(true);
        SortierungUnten.GetComponentInChildren<TMP_Text>().text = "▼ " + data.Replace("[UNTEN]", "|").Split('|')[1];
    }
    /// <summary>
    /// Zeigt die Auswahlelemente der Liste an
    /// </summary>
    /// <param name="data">Auswahlelemente</param>
    private void ListenAuswahl(string data)
    {
        ListenAnzeigen.SetActive(true);
        AuswahlTitel.SetActive(true);
        int zahl = Int32.Parse(data.Replace("<#!#>", "|").Split('|')[0]);
        for (int i = 0; i < 30; i++)
        {
            if (i < zahl)
            {
                string element = data.Replace("[" + (i + 1) + "]", "|").Split('|')[1];
                AuswahlElemente[i].SetActive(true);
                AuswahlElemente[i].GetComponentInChildren<TMP_Text>().text = element;
                AuswahlElemente[i].transform.GetChild(2).GetComponentInChildren<TMP_Text>().text = "" + (i + 1);
            }
        }
    }
    /// <summary>
    /// Zeigt die aufgelösten Elemente an
    /// </summary>
    /// <param name="data">Inhalte der Elemente</param>
    private void ListenAufloesen(string data)
    {
        // Bereits eingefügtes mit Infos versehen
        string[] msg = data.Replace("<#!#>", "|").Split('|');
        for (int i = 0; i < msg.Length; i++)
        {
            int index = Int32.Parse(msg[i].Replace("[TRENNER]", "|").Split('|')[0]);
            GridElemente[index].SetActive(true);
            GridElemente[index].GetComponentInChildren<TMP_Text>().text = msg[i].Replace("[TRENNER]","|").Split('|')[1];
            GridElemente[index].transform.GetChild(0).gameObject.SetActive(true);
        }
    }
    /// <summary>
    /// Fügt ein Auswahlelement in die Liste ein
    /// </summary>
    /// <param name="data">Element</param>
    private void ListenElementEinfuegen(string data)
    {
        int index = Int32.Parse(data.Replace("[INDEX]", "|").Split('|')[1]);
        string element = data.Replace("[ELEMENT]", "|").Split('|')[1];
        int auswahlindex = Int32.Parse(data.Replace("[AUSWAHLINDEX]", "|").Split('|')[1]);

        GridElemente[index].SetActive(true); // Für alle Anzeigen
        GridElemente[index].GetComponentInChildren<TMP_Text>().text = element;

		AuswahlElemente[auswahlindex].GetComponentInChildren<TMP_Text>().text = "";
        ListenElementIdsAktualisieren();
    }
    /// <summary>
    /// Gibt bei einem Eingefügen Element die Sortdisplay an
    /// </summary>
    /// <param name="data"></param>
    private void ListenShowSortDisplay(string data)
    {
        int elementindex = Int32.Parse(data.Replace("[!#!]", "|").Split('|')[0]);
        string display = data.Replace("[!#!]", "|").Split('|')[1];

        GridElemente[elementindex].transform.GetChild(0).gameObject.SetActive(true);
        GridElemente[elementindex].transform.GetChild(1).GetComponentInChildren<TMP_Text>().text = display;
    }
    /// <summary>
    /// Aktualisiert die Ids der Elemente
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
    }
    #endregion
    #region Herzen
    /// <summary>
    /// Blendet die Herzen ein
    /// </summary>
    /// <param name="data">Anzahl</param>
    private void HerzenEinblenden(string data)
    {
        if (data.Equals("0"))
        {
            for (int i = 0; i < 8; i++)
            {
                KeinTeamSpielGrid[i].transform.GetChild(5).gameObject.SetActive(false);
                KeinTeamSpielGrid[i].transform.GetChild(5).GetChild(0).gameObject.SetActive(false);
                KeinTeamSpielGrid[i].transform.GetChild(5).GetChild(1).gameObject.SetActive(false);
                KeinTeamSpielGrid[i].transform.GetChild(5).GetChild(2).gameObject.SetActive(false);
                KeinTeamSpielGrid[i].transform.GetChild(5).GetChild(3).gameObject.SetActive(false);
            }
            for (int i = 0; i < 4; i++)
            {
                TeamRotSpielGrid[i].transform.GetChild(5).gameObject.SetActive(false);
                TeamRotSpielGrid[i].transform.GetChild(5).GetChild(0).gameObject.SetActive(false);
                TeamRotSpielGrid[i].transform.GetChild(5).GetChild(1).gameObject.SetActive(false);
                TeamRotSpielGrid[i].transform.GetChild(5).GetChild(2).gameObject.SetActive(false);
                TeamRotSpielGrid[i].transform.GetChild(5).GetChild(3).gameObject.SetActive(false);
            }
            for (int i = 0; i < 4; i++)
            {
                TeamBlauSpielGrid[i].transform.GetChild(5).gameObject.SetActive(false);
                TeamBlauSpielGrid[i].transform.GetChild(5).GetChild(0).gameObject.SetActive(false);
                TeamBlauSpielGrid[i].transform.GetChild(5).GetChild(1).gameObject.SetActive(false);
                TeamBlauSpielGrid[i].transform.GetChild(5).GetChild(2).gameObject.SetActive(false);
                TeamBlauSpielGrid[i].transform.GetChild(5).GetChild(3).gameObject.SetActive(false);
            }
            for (int i = 0; i < 2; i++)
            {
                TeamGruenSpielGrid[i].transform.GetChild(5).gameObject.SetActive(false);
                TeamGruenSpielGrid[i].transform.GetChild(5).GetChild(0).gameObject.SetActive(false);
                TeamGruenSpielGrid[i].transform.GetChild(5).GetChild(1).gameObject.SetActive(false);
                TeamGruenSpielGrid[i].transform.GetChild(5).GetChild(2).gameObject.SetActive(false);
                TeamGruenSpielGrid[i].transform.GetChild(5).GetChild(3).gameObject.SetActive(false);
            }
            for (int i = 0; i < 2; i++)
            {
                TeamLilaSpielGrid[i].transform.GetChild(5).gameObject.SetActive(false);
                TeamLilaSpielGrid[i].transform.GetChild(5).GetChild(0).gameObject.SetActive(false);
                TeamLilaSpielGrid[i].transform.GetChild(5).GetChild(1).gameObject.SetActive(false);
                TeamLilaSpielGrid[i].transform.GetChild(5).GetChild(2).gameObject.SetActive(false);
                TeamLilaSpielGrid[i].transform.GetChild(5).GetChild(3).gameObject.SetActive(false);
            }
            return;
        }
        bool anzeigen = bool.Parse(data.Replace("[BOOL]", "|").Split('|')[1]);
        int anzahl = Int32.Parse(data.Replace("[ZAHL]", "|").Split('|')[1]);
        List<int> idlist = new List<int>();

        /// Kein Team
        string keinteam = data.Replace("[KEINTEAM]", "|").Split('|')[1];
        if (keinteam.Length > 1)
            for (int i = 0; i < keinteam.Replace("[]", "|").Split('|').Length; i++)
                idlist.Add(Int32.Parse(keinteam.Replace("[]", "|").Split('|')[i]));
        else if (keinteam.Length == 1)
            idlist.Add(Int32.Parse(keinteam));

        for (int i = 0; i < idlist.Count; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (Config.PLAYERLIST[Player.getPosInLists(idlist[i])].name == KeinTeamSpielGrid[j].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text)
                {
                    KeinTeamSpielGrid[j].SetActive(true);
                    KeinTeamSpielGrid[j].transform.GetChild(5).gameObject.SetActive(anzeigen);
                    for (int k = 0; k < 4; k++)
                    {
                        // Ausblenden
                        KeinTeamSpielGrid[i].transform.GetChild(5).GetChild(k).gameObject.SetActive(false);
                        // Anzahl einblenden
                        if (anzahl >= (k + 1))
                        {
                            KeinTeamSpielGrid[j].transform.GetChild(5).GetChild(k).gameObject.SetActive(true);
                            KeinTeamSpielGrid[j].transform.GetChild(5).GetChild(k).GetChild(0).GetChild(0).gameObject.SetActive(true);
                        }
                    }
                    break;
                }
            }
        }
        idlist.Clear();
        /// Team Rot
        string teamrot = data.Replace("[TEAMROT]", "|").Split('|')[1];
        if (teamrot.Length > 1)
            for (int i = 0; i < teamrot.Replace("[]", "|").Split('|').Length; i++)
                idlist.Add(Int32.Parse(teamrot.Replace("[]", "|").Split('|')[i]));
        else if (teamrot.Length == 1)
            idlist.Add(Int32.Parse(teamrot));

        for (int i = 0; i < idlist.Count; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                if (Config.PLAYERLIST[Player.getPosInLists(idlist[i])].name == TeamRotSpielGrid[j].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text)
                {
                    TeamRotSpielGrid[j].SetActive(true);
                    TeamRotSpielGrid[j].transform.GetChild(5).gameObject.SetActive(anzeigen);
                    for (int k = 0; k < 4; k++)
                    {
                        // Ausblenden
                        TeamRotSpielGrid[i].transform.GetChild(5).GetChild(k).gameObject.SetActive(false);
                        // Anzahl einblenden
                        if (anzahl >= (k + 1))
                        {
                            TeamRotSpielGrid[j].transform.GetChild(5).GetChild(k).gameObject.SetActive(true);
                            TeamRotSpielGrid[j].transform.GetChild(5).GetChild(k).GetChild(0).GetChild(0).gameObject.SetActive(true);
                        }
                    }
                    break;
                }
            }
        }
        idlist.Clear();
        /// Team Blau
        string teamblau = data.Replace("[TEAMBLAU]", "|").Split('|')[1];
        if (teamblau.Length > 1)
            for (int i = 0; i < teamblau.Replace("[]", "|").Split('|').Length; i++)
                idlist.Add(Int32.Parse(teamblau.Replace("[]", "|").Split('|')[i]));
        else if (teamblau.Length == 1)
            idlist.Add(Int32.Parse(teamblau));

        for (int i = 0; i < idlist.Count; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                if (Config.PLAYERLIST[Player.getPosInLists(idlist[i])].name == TeamBlauSpielGrid[j].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text)
                {
                    TeamBlauSpielGrid[j].SetActive(true);
                    TeamBlauSpielGrid[j].transform.GetChild(5).gameObject.SetActive(anzeigen);
                    for (int k = 0; k < 4; k++)
                    {
                        // Ausblenden
                        TeamBlauSpielGrid[i].transform.GetChild(5).GetChild(k).gameObject.SetActive(false);
                        // Anzahl einblenden
                        if (anzahl >= (k + 1))
                        {
                            TeamBlauSpielGrid[j].transform.GetChild(5).GetChild(k).gameObject.SetActive(true);
                            TeamBlauSpielGrid[j].transform.GetChild(5).GetChild(k).GetChild(0).GetChild(0).gameObject.SetActive(true);
                        }
                    }
                    break;
                }
            }
        }
        idlist.Clear();
        /// Team Gruen
        string teamgruen = data.Replace("[TEAMGRUEN]", "|").Split('|')[1];
        if (teamgruen.Length > 1)
            for (int i = 0; i < teamgruen.Replace("[]", "|").Split('|').Length; i++)
                idlist.Add(Int32.Parse(teamgruen.Replace("[]", "|").Split('|')[i]));
        else if (teamgruen.Length == 1)
            idlist.Add(Int32.Parse(teamgruen));

        for (int i = 0; i < idlist.Count; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                if (Config.PLAYERLIST[Player.getPosInLists(idlist[i])].name == TeamGruenSpielGrid[j].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text)
                {
                    TeamGruenSpielGrid[j].SetActive(true);
                    TeamGruenSpielGrid[j].transform.GetChild(5).gameObject.SetActive(anzeigen);
                    for (int k = 0; k < 4; k++)
                    {
                        // Ausblenden
                        TeamGruenSpielGrid[i].transform.GetChild(5).GetChild(k).gameObject.SetActive(false);
                        // Anzahl einblenden
                        if (anzahl >= (k + 1))
                        {
                            TeamGruenSpielGrid[j].transform.GetChild(5).GetChild(k).gameObject.SetActive(true);
                            TeamGruenSpielGrid[j].transform.GetChild(5).GetChild(k).GetChild(0).GetChild(0).gameObject.SetActive(true);
                        }
                    }
                    break;
                }
            }
        }
        idlist.Clear();
        /// Team Lila
        string teamlila = data.Replace("[TEAMLILA]", "|").Split('|')[1];
        if (teamlila.Length > 1)
            for (int i = 0; i < teamlila.Replace("[]", "|").Split('|').Length; i++)
                idlist.Add(Int32.Parse(teamlila.Replace("[]", "|").Split('|')[i]));
        else if (teamlila.Length == 1)
            idlist.Add(Int32.Parse(teamlila));

        for (int i = 0; i < idlist.Count; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                if (Config.PLAYERLIST[Player.getPosInLists(idlist[i])].name == TeamLilaSpielGrid[j].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text)
                {
                    TeamLilaSpielGrid[j].SetActive(true);
                    TeamLilaSpielGrid[j].transform.GetChild(5).gameObject.SetActive(anzeigen);
                    for (int k = 0; k < 4; k++)
                    {
                        // Ausblenden
                        TeamLilaSpielGrid[i].transform.GetChild(5).GetChild(k).gameObject.SetActive(false);
                        // Anzahl einblenden
                        if (anzahl >= (k + 1))
                        {
                            TeamLilaSpielGrid[j].transform.GetChild(5).GetChild(k).gameObject.SetActive(true);
                            TeamLilaSpielGrid[j].transform.GetChild(5).GetChild(k).GetChild(0).GetChild(0).gameObject.SetActive(true);
                        }
                    }
                    break;
                }
            }
        }
    }
    /// <summary>
    /// Füllt die Herzen wieder auf
    /// </summary>
    /// <param name="data"></param>
    private void HerzenFuellen(string data)
    {
        //[TEAM]KEINTEAM[TEAM][ID]"+ playerid + "[ID][ZAHL]"+spielerherzenzahl[Player.getPosInLists(playerid)]+"[ZAHL]
        string team = data.Replace("[TEAM]", "|").Split('|')[1];
        int id = Int32.Parse(data.Replace("[ID]", "|").Split('|')[1]);
        int herzen = Int32.Parse(data.Replace("[ZAHL]", "|").Split('|')[1]);
        if (team.Equals("KEINTEAM"))
        {
            for (int i = 0; i < 8; i++)
            {
                if (Config.PLAYERLIST[Player.getPosInLists(id)].name == KeinTeamSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        // Ausblenden
                        KeinTeamSpielGrid[i].transform.GetChild(5).GetChild(j).gameObject.SetActive(false);
                        // Anzahl einblenden
                        if (herzen >= (j + 1))
                        {
                            KeinTeamSpielGrid[i].transform.GetChild(5).GetChild(j).gameObject.SetActive(true);
                            KeinTeamSpielGrid[i].transform.GetChild(5).GetChild(j).GetChild(0).GetChild(0).gameObject.SetActive(true);
                        }
                    }
                    return;
                }
            }
        }
        else if (team.Equals("TEAMROT"))
        {
            for (int i = 0; i < 4; i++)
            {
                if (Config.PLAYERLIST[Player.getPosInLists(id)].name == TeamRotSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        // Ausblenden
                        TeamRotSpielGrid[i].transform.GetChild(5).GetChild(j).gameObject.SetActive(false);
                        // Anzahl einblenden
                        if (herzen >= (j + 1))
                        {
                            TeamRotSpielGrid[i].transform.GetChild(5).GetChild(j).gameObject.SetActive(true);
                            TeamRotSpielGrid[i].transform.GetChild(5).GetChild(j).GetChild(0).GetChild(0).gameObject.SetActive(true);
                        }
                    }
                    return;
                }
            }
        }
        else if (team.Equals("TEAMBLAU"))
        {
            for (int i = 0; i < 4; i++)
            {
                if (Config.PLAYERLIST[Player.getPosInLists(id)].name == TeamBlauSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        // Ausblenden
                        TeamBlauSpielGrid[i].transform.GetChild(5).GetChild(j).gameObject.SetActive(false);
                        // Anzahl einblenden
                        if (herzen >= (j + 1))
                        {
                            TeamBlauSpielGrid[i].transform.GetChild(5).GetChild(j).gameObject.SetActive(true);
                            TeamBlauSpielGrid[i].transform.GetChild(5).GetChild(j).GetChild(0).GetChild(0).gameObject.SetActive(true);
                        }
                    }
                    return;
                }
            }
        }
        else if (team.Equals("TEAMGRUEN"))
        {
            for (int i = 0; i < 2; i++)
            {
                if (Config.PLAYERLIST[Player.getPosInLists(id)].name == TeamGruenSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        // Ausblenden
                        TeamGruenSpielGrid[i].transform.GetChild(5).GetChild(j).gameObject.SetActive(false);
                        // Anzahl einblenden
                        if (herzen >= (j + 1))
                        {
                            TeamGruenSpielGrid[i].transform.GetChild(5).GetChild(j).gameObject.SetActive(true);
                            TeamGruenSpielGrid[i].transform.GetChild(5).GetChild(j).GetChild(0).GetChild(0).gameObject.SetActive(true);
                        }
                    }
                    return;
                }
            }
        }
        else if (team.Equals("TEAMLILA"))
        {
            for (int i = 0; i < 2; i++)
            {
                if (Config.PLAYERLIST[Player.getPosInLists(id)].name == TeamLilaSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        // Ausblenden
                        TeamLilaSpielGrid[i].transform.GetChild(5).GetChild(j).gameObject.SetActive(false);
                        // Anzahl einblenden
                        if (herzen >= (j + 1))
                        {
                            TeamLilaSpielGrid[i].transform.GetChild(5).GetChild(j).gameObject.SetActive(true);
                            TeamLilaSpielGrid[i].transform.GetChild(5).GetChild(j).GetChild(0).GetChild(0).gameObject.SetActive(true);
                        }
                    }
                    return;
                }
            }
        }
    }
    /// <summary>
    /// Zieht Herzen ab
    /// </summary>
    /// <param name="data">id</param>
    private void HerzenAbziehen(string data)
    {
        string team = data.Replace("[TEAM]", "|").Split('|')[1];
        string name = data.Replace("[NAME]", "|").Split('|')[1];
        int herzid = Int32.Parse(data.Replace("[HERZID]", "|").Split('|')[1]);

        if (team.Equals("KeinTeam"))
        {
            for (int i = 0; i < 8; i++)
            {
                if (name == KeinTeamSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text)
                {
                    KeinTeamSpielGrid[i].transform.GetChild(5).GetChild(herzid).GetChild(0).GetChild(0).gameObject.SetActive(false);
                    return;
                }
            }
        }
        else if (team.Equals("Team Rot"))
        {
            for (int i = 0; i < 4; i++)
            {
                TeamRotSpielGrid[i].transform.GetChild(5).GetChild(herzid).GetChild(0).GetChild(0).gameObject.SetActive(false);
                /*if (name == TeamRotSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text)
                {
                    TeamRotSpielGrid[i].transform.GetChild(5).GetChild(herzid).GetChild(0).GetChild(0).gameObject.SetActive(false);
                    return;
                }*/
            }
        }
        else if (team.Equals("Team Blau"))
        {
            for (int i = 0; i < 4; i++)
            {
                TeamBlauSpielGrid[i].transform.GetChild(5).GetChild(herzid).GetChild(0).GetChild(0).gameObject.SetActive(false);
                /*if (name == TeamBlauSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text)
                {
                    TeamBlauSpielGrid[i].transform.GetChild(5).GetChild(herzid).GetChild(0).GetChild(0).gameObject.SetActive(false);
                    return;
                }*/
            }
        }
        else if (team.Equals("Team Gruen"))
        {
            for (int i = 0; i < 2; i++)
            {
                TeamGruenSpielGrid[i].transform.GetChild(5).GetChild(herzid).GetChild(0).GetChild(0).gameObject.SetActive(false);
                /*if (name == TeamGruenSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text)
                {
                    TeamGruenSpielGrid[i].transform.GetChild(5).GetChild(herzid).GetChild(0).GetChild(0).gameObject.SetActive(false);
                    return;
                }*/
            }
        }
        else if (team.Equals("Team Lila"))
        {
            for (int i = 0; i < 2; i++)
            {
                TeamLilaSpielGrid[i].transform.GetChild(5).GetChild(herzid).GetChild(0).GetChild(0).gameObject.SetActive(false);
                /*if (name == TeamLilaSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text)
                {
                    TeamLilaSpielGrid[i].transform.GetChild(5).GetChild(herzid).GetChild(0).GetChild(0).gameObject.SetActive(false);
                    return;
                }*/
            }
        }
    }
    #endregion
    /// <summary>
    /// Spieler/Team ist dran anzeige
    /// </summary>
    /// <param name="data">Spielerid</param>
    private void SpielerIstDran(string data)
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
        if (data.Equals("0"))
            return;

        /// Einblendem
        string team = data.Replace("[TEAM]", "|").Split('|')[1];
        if (team.Equals("KeinTeam"))
        {
            string name = data.Replace("[NAME]", "|").Split('|')[1];
            for (int i = 0; i < 8; i++)
            {
                if (name == KeinTeamSpielGrid[i].transform.GetChild(4).GetChild(1).GetComponent<TMP_Text>().text)
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
    /// <summary>
    /// Geht von der Listenanzeige zurück zur Teamwahl
    /// </summary>
    private void ZurueckZurTeamWahl()
    {
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
    }
}
