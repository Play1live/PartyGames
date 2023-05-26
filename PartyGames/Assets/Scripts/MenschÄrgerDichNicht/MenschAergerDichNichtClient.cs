using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenschAergerDichNichtClient : MonoBehaviour
{
    [SerializeField] GameObject Lobby;
    private GameObject[] Playerlist;
    [SerializeField] GameObject Games;
    [SerializeField] GameObject[] Maps;
    [SerializeField] GameObject SpielprotokollContent;
    [SerializeField] GameObject Würfel;
    [SerializeField] GameObject InfoBoard;

    [SerializeField] AudioSource DisconnectSound;

    MenschAegerDichNichtBoard board;

    void OnEnable()
    {
        Lobby.SetActive(true);
        Games.SetActive(false);
        InitLobby();

        if (!Config.CLIENT_STARTED)
            return;
        SendToServer("#JoinMenschAergerDichNicht");

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
        SendToServer("#ClientFocusChange " + focus);
    }

    private void OnApplicationQuit()
    {
        Logging.log(Logging.LogType.Normal, "MenschÄrgerDichNichtClient", "OnApplicationQuit", "Client wird geschlossen.");
        SendToServer("#ClientClosed");
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
        Logging.log(Logging.LogType.Debug, "MenschÄrgerDichNichtClient", "TestConnectionToServer", "Testet die Verbindumg zum Server.");
        while (Config.CLIENT_STARTED)
        {
            SendToServer("#TestConnection");
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
    }
    #endregion
    #region Kommunikation
    /// <summary>
    /// Sendet eine Nachricht an den Server.
    /// </summary>
    /// <param name="data">Nachricht</param>
    private void SendToServer(string data)
    {
        if (!Config.CLIENT_STARTED)
            return;

        try
        {
            NetworkStream stream = Config.CLIENT_TCP.GetStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.WriteLine(data);
            writer.Flush();
        }
        catch (Exception e)
        {
            Logging.log(Logging.LogType.Warning, "MenschÄrgerDichNichtClient", "SendToServer", "Nachricht an Server konnte nicht gesendet werden.", e);
            Config.HAUPTMENUE_FEHLERMELDUNG = "Verbindung zum Server wurde verloren.";
            CloseSocket();
            SceneManager.LoadSceneAsync("StartUp");
        }
    }
    /// <summary>
    /// Einkommende Nachrichten die vom Sever
    /// </summary>
    /// <param name="data">Nachricht</param>
    private void OnIncomingData(string data)
    {
        string cmd;
        if (data.Contains(" "))
            cmd = data.Split(' ')[0];
        else
            cmd = data;
        data = data.Replace(cmd + " ", "");

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
        Logging.log(Logging.LogType.Debug, "MenschÄrgerDichNichtClient", "Commands", "Eingehende Nachricht: " + cmd + " -> " + data);
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "MenschÄrgerDichNichtClient", "Commands", "Unkown Command: " + cmd + " -> " + data);
                break;

            #region Universal Commands
            case "#ServerClosed":
                Logging.log(Logging.LogType.Normal, "MenschÄrgerDichNichtClient", "Commands", "Verbindumg zum Server wurde beendet. Lade ins Hauptmenü.");
                CloseSocket();
                SceneManager.LoadSceneAsync("Startup");
                break;
            case "#UpdateRemoteConfig":
                Logging.log(Logging.LogType.Normal, "MenschÄrgerDichNichtClient", "Commands", "RemoteConfig wird neugeladen");
                LoadConfigs.FetchRemoteConfig();
                break;
            case "#ZurueckInsHauptmenue":
                Logging.log(Logging.LogType.Normal, "MenschÄrgerDichNichtClient", "Commands", "Spiel wird beendet. Lade ins Hauptmenü");
                SceneManager.LoadSceneAsync("Startup");
                break;
            #endregion

            case "#UpdateLobby":
                UpdateLobby(data);
                break;
            case "#StartGame":
                StartGame(data);
                break;
        }
    }
    /// <summary>
    /// Spielt den Disconnect Sound ab
    /// </summary>
    private void PlayDisconnectSound()
    {
        DisconnectSound.Play();
    }
    /// <summary>
    /// Initialisiert die Anzeigen der Scene
    /// </summary>
    private void InitLobby()
    {
        Logging.log(Logging.LogType.Debug, "MenschÄrgerDichNichtClient", "InitLobby", "Lobby wird initialisiert.");
        Playerlist = new GameObject[Lobby.transform.GetChild(1).childCount];
        for (int i = 0; i < Lobby.transform.GetChild(1).childCount; i++)
        {
            Playerlist[i] = Lobby.transform.GetChild(1).GetChild(i).gameObject;
            Playerlist[i].SetActive(false);
        }
        // Blendet Maps aus
        foreach (GameObject go in Maps)
        {
            go.SetActive(false);
        }
    }
    private int ingameSpieler;
    private void UpdateLobby(string data)
    {
        Logging.log(Logging.LogType.Debug, "MenschAergerDichNichtClient", "UpdateLobby", "LobbyAnzeigen werden aktualisiert: " + data);
        for (int i = 0; i < Playerlist.Length; i++)
        {
            Playerlist[i].SetActive(false);
        }
        string[] elemente = data.Split('|');
        if (elemente.Length < ingameSpieler)
            PlayDisconnectSound();
        ingameSpieler = elemente.Length;
        for (int i = 0; i < elemente.Length; i++)
        {
            Playerlist[i].GetComponentInChildren<TMP_Text>().text = elemente[i];
            Playerlist[i].SetActive(true);
        }
    }
    private void StartGame(string data)
    {
        // PlayerInit
        List<MenschAergerDichNichtPlayer> randomplayer = new List<MenschAergerDichNichtPlayer>();
        string[] playerdata = data.Replace("[PLAYER]", "|").Split('|')[1].Replace("[#]", "|").Split('|');
        for (int i = 0; i < playerdata.Length; i++)
        {
            string name = playerdata[i].Split('*')[0];
            bool isbot = bool.Parse(playerdata[i].Split('*')[1]);
            Sprite sprite = Resources.Load<Sprite>("Images/Icons/" + playerdata[i].Split('*')[2]);

            randomplayer.Add(new MenschAergerDichNichtPlayer(i, name, isbot, sprite));
        }
        // LoadMap
        GameObject selectedMap = null;
        string mapstring = data.Replace("[MAP]", "|").Split('|')[1];
        foreach (GameObject go in Maps)
        {
            go.SetActive(false);
            if (go.name.Equals(mapstring))
                selectedMap = go;
        }
        if (selectedMap == null)
        {
            Logging.log(Logging.LogType.Warning, "MenschAergerDichNichtClient", "StartGame", "Ausgewählte Map konnte nicht gefunden werden.");
            Lobby.SetActive(true);
            Games.SetActive(false);
            return;
        }
        selectedMap.SetActive(true);
        int teamsize = Int32.Parse(data.Replace("[TEAMSIZE]", "|").Split('|')[1]);
        int runwaysize = Int32.Parse(data.Replace("[RUNWAY]", "|").Split('|')[1]); 
        Lobby.SetActive(false);
        Games.SetActive(true);

        board = new MenschAegerDichNichtBoard(selectedMap, runwaysize, teamsize, randomplayer);

        // Hide PlayerAnimation
        selectedMap.transform.GetChild(4).gameObject.SetActive(false);

        AddMSGToProtokoll("Spiel wurde gestartet.");
        DisplayMSGInfoBoard("Spiel wird geladen.");
    }
    public void AddMSGToProtokoll(string msg)
    {
        GameObject go = Instantiate(SpielprotokollContent.transform.GetChild(0).gameObject, SpielprotokollContent.transform.GetChild(0).position, SpielprotokollContent.transform.GetChild(0).rotation);
        go.name = "MSG_" + SpielprotokollContent.transform.childCount;
        go.transform.SetParent(SpielprotokollContent.transform);
        go.transform.GetComponentInChildren<TMP_Text>().text = msg;
        go.transform.localScale = new Vector3(1, 1, 1);
        go.SetActive(true);
    }
    public void DisplayMSGInfoBoard(string msg)
    {
        InfoBoard.GetComponentInChildren<TMP_Text>().text = msg;
    }
    public void StartWuerfelAnimation()
    {

    }
}