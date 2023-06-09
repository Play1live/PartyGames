using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class KniffelClient : MonoBehaviour
{
    private GameObject[] Playerlist;

    [SerializeField] AudioSource DisconnectSound;
    [SerializeField] AudioSource SpielerIstDran;
    [SerializeField] AudioSource SpielIstVorbei;
    [SerializeField] GameObject Punkteliste;
    [SerializeField] GameObject WuerfelBoard;
    [SerializeField] GameObject WuerfelnButton;
    List<Image> SafeWuerfel;

    KniffelBoard board;
    Coroutine StartTurnDelayedCoroutine;
    Coroutine StartWuerfelAnimationCoroutine;

    void OnEnable()
    {
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

            case "#InitGame":
                InitGame(data);
                break;
            case "#UpdateLobby":
                UpdateLobby(data);
                break;
            case "#PlayerWuerfel":
                SpielerWuerfelt(data);
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
    /// Aktualisiert die Lobby
    /// </summary>
    int ingameSpieler = 0;
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

    #region Kniffel
    private void InitGame(string data)
    {
        List<KniffelPlayer> players = new List<KniffelPlayer>();
        foreach (string item in data.Replace("[#]", "|").Split('|'))
        {
            players.Add(new KniffelPlayer(Int32.Parse(item.Split('*')[0]), item.Split('*')[1], Resources.Load<Sprite>("Images/ProfileIcons/" + item.Split('*')[2]), Punkteliste.transform.GetChild(1 + players.Count).gameObject));
        }

        SafeWuerfel = new List<Image>();
        SafeWuerfel.Add(WuerfelBoard.transform.GetChild(1).GetComponent<Image>());
        SafeWuerfel.Add(WuerfelBoard.transform.GetChild(2).GetComponent<Image>());
        SafeWuerfel.Add(WuerfelBoard.transform.GetChild(3).GetComponent<Image>());
        SafeWuerfel.Add(WuerfelBoard.transform.GetChild(4).GetComponent<Image>());
        SafeWuerfel.Add(WuerfelBoard.transform.GetChild(5).GetComponent<Image>());
        board = new KniffelBoard(Punkteliste, WuerfelBoard, players);

        WuerfelBoard.transform.GetChild(6).gameObject.SetActive(false);
    }
    private void UpdatePunkteliste(string data)
    {
        /*this.gamerid + "*" + this.name + "*" + this.PlayerImage.name + "*" + this.Einsen.ToString() + "*" + this.Zweien.ToString() + "*" +
            this.Dreien.ToString() + "*" + this.Vieren.ToString() + "*" + this.Fuenfen.ToString() + "*" + this.Sechsen.ToString() + "*" +
            this.ObenSummeOhneBonus.ToString() + "*" + this.Bonus.ToString() + "*" + this.ObenSumme.ToString() + "*" + this.Dreierpasch.ToString() + "*" +
            this.Viererpasch.ToString() + "*" + this.FullHouse.ToString() + "*" + this.KleineStraße.ToString() + "*" + this.GroßeStraße.ToString() + "*" +
            this.Kniffel.ToString() + "*" + this.Chance.ToString() + "*" + this.SummeUntererTeil.ToString() + "*" + this.SummeObererTeil.ToString() + "*" +
            this.EndSumme.ToString();*/

        foreach (string player in data.Replace("[#]","|").Split('|'))
        {
            string[] infos = player.Split('*');
            KniffelPlayer p = KniffelPlayer.GetPlayerById(board.GetPlayerList(), Int32.Parse(infos[0]));

        }
    }
    private void SpielerWuerfelt(string data)
    {

    }
    private string GetWuerfelString()
    {
        string unsavewuerfel = "";
        for (int i = 0; i < wuerfelUnsave.Count; i++)
        {
            unsavewuerfel += "+" + wuerfelUnsave[i];
        }
        if (unsavewuerfel.Length > 1)
            unsavewuerfel = unsavewuerfel.Substring("+".Length);
        string savewuerfel = "";
        for (int i = 0; i < wuerfelSave.Count; i++)
        {
            savewuerfel += "+" + wuerfelSave[i];
        }
        if (savewuerfel.Length > 1)
            savewuerfel = savewuerfel.Substring("+".Length);
        return unsavewuerfel + "-" + savewuerfel;
    }
    public void ClickEinsen(GameObject Spieler)
    {
        if (!Config.CLIENT_STARTED)
            return;
        SendToServer("#ClickPlayerKategorie Einsen*" + GetWuerfelString());
    }
    public void ClickZweien(GameObject Spieler)
    {
        if (!Config.CLIENT_STARTED)
            return;
        SendToServer("#ClickPlayerKategorie Zweien*" + GetWuerfelString());
    }
    public void ClickDreien(GameObject Spieler)
    {
        if (!Config.CLIENT_STARTED)
            return;
        SendToServer("#ClickPlayerKategorie Dreien*" + GetWuerfelString());
    }
    public void ClickVieren(GameObject Spieler)
    {
        if (!Config.CLIENT_STARTED)
            return;
        SendToServer("#ClickPlayerKategorie Vieren*" + GetWuerfelString());
    }
    public void ClickFuenfen(GameObject Spieler)
    {
        if (!Config.CLIENT_STARTED)
            return;
        SendToServer("#ClickPlayerKategorie Fuenfen*" + GetWuerfelString());
    }
    public void ClickSechsen(GameObject Spieler)
    {
        if (!Config.CLIENT_STARTED)
            return;
        SendToServer("#ClickPlayerKategorie Sechsen*" + GetWuerfelString());
    }
    public void ClickDreierpasch(GameObject Spieler)
    {
        if (!Config.CLIENT_STARTED)
            return;
        SendToServer("#ClickPlayerKategorie Dreierpasch*" + GetWuerfelString());
    }
    public void ClickViererpasch(GameObject Spieler)
    {
        if (!Config.CLIENT_STARTED)
            return;
        SendToServer("#ClickPlayerKategorie Viererpasch*" + GetWuerfelString());
    }
    public void ClickFullHouse(GameObject Spieler)
    {
        if (!Config.CLIENT_STARTED)
            return;
        SendToServer("#ClickPlayerKategorie FullHouse*" + GetWuerfelString());
    }
    public void ClickKleineStraße(GameObject Spieler)
    {
        if (!Config.CLIENT_STARTED)
            return;
        SendToServer("#ClickPlayerKategorie KleineStraße*" + GetWuerfelString());
    }
    public void ClickGroßeStraße(GameObject Spieler)
    {
        if (!Config.CLIENT_STARTED)
            return;
        SendToServer("#ClickPlayerKategorie GroßeStraße*" + GetWuerfelString());
    }
    public void ClickKniffel(GameObject Spieler)
    {
        if (!Config.CLIENT_STARTED)
            return;
        SendToServer("#ClickPlayerKategorie Kniffel*" + GetWuerfelString());
    }
    public void ClickChance(GameObject Spieler)
    {
        if (!Config.CLIENT_STARTED)
            return;
        SendToServer("#ClickPlayerKategorie Chance*" + GetWuerfelString());
    }
    #endregion
}