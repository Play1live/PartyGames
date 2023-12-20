using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WerBinIchClient : MonoBehaviour
{
    [SerializeField] AudioSource DisconnectSound;

    private GameObject[,] PlayerAnzeige;

    // Start is called before the first frame update
    void Start()
    {
        if (!Config.CLIENT_STARTED)
            return;
        ClientUtils.SendToServer("#JoinWerBinIch");
        InitGame();
        StartCoroutine(TestConnectionToServer());
    }

    // Update is called once per frame
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
        Logging.log(Logging.LogType.Normal, "TabuClient", "OnApplicationQuit", "Client wird geschlossen.");
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
        Logging.log(Logging.LogType.Debug, "TabuClient", "TestConnectionToServer", "Testet die Verbindumg zum Server.");
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
    }
    #endregion
    #region Kommunikation
    /// <summary>
    /// Einkommende Nachrichten die vom Sever
    /// </summary>
    /// <param name="data">Nachricht</param>
    private void OnIncomingData(string data)
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
        //Debug.LogWarning(cmd + "  ->  " + data);
        Logging.log(Logging.LogType.Debug, "TabuClient", "Commands", "Eingehende Nachricht: " + cmd + " -> " + data);
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "TabuClient", "Commands", "Unkown Command: " + cmd + " -> " + data);
                break;

            #region Universal Commands
            case "#ServerClosed":
                Logging.log(Logging.LogType.Normal, "TabuClient", "Commands", "Verbindumg zum Server wurde beendet. Lade ins Hauptmenü.");
                CloseSocket();
                SceneManager.LoadSceneAsync("Startup");
                break;
            case "#UpdateRemoteConfig":
                Logging.log(Logging.LogType.Normal, "TabuClient", "Commands", "RemoteConfig wird neugeladen");
                LoadConfigs.FetchRemoteConfig();
                break;
            case "#ZurueckInsHauptmenue":
                Logging.log(Logging.LogType.Normal, "TabuClient", "Commands", "Spiel wird beendet. Lade ins Hauptmenü");
                SceneManager.LoadSceneAsync("Startup");
                break;
            #endregion

            case "#ClientFocus":
                PlayerAnzeige[int.Parse(data.Split('~')[0]), 2].SetActive(!bool.Parse(data.Split('~')[1]));
                break;
            case "#UpdateSpieler":
                UpdateSpieler(data);
                break;
            case "#UpdateText":
                UpdateText(data);
                break;
        }
    }
    /// <summary>
    /// Initialisiert die Game Elemente
    /// </summary>
    private void InitGame()
    {
        PlayerAnzeige = new GameObject[9, 4];
        for (int i = 0; i < 9; i++)
        {
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
        PlayerAnzeige[Config.PLAYER_ID, 0].SetActive(false);
    }
    /// <summary>
    /// Aktualisiert die Spieleranzeigen
    /// </summary>
    /// <param name="data"></param>
    private void UpdateSpieler(string data)
    {
        if (data.EndsWith("[DISCONNECT]"))
            DisconnectSound.Play();
        for (int i = 0; i < 9; i++)
            PlayerAnzeige[i, 0].transform.parent.gameObject.SetActive(false);
        string[] stuff = data.Replace("[TRENNER]", "|").Split('|');
        foreach (var item in stuff)
        {
            int id = Int32.Parse(item.Replace("[ID]", "|").Split('|')[1]);
            string name = item.Replace("[NAME]", "|").Split('|')[1];
            string punkte = item.Replace("[PUNKTE]", "|").Split('|')[1];
            bool online = bool.Parse(item.Replace("[ONLINE]", "|").Split('|')[1]);

            PlayerAnzeige[id, 0].transform.parent.gameObject.SetActive(online);
            PlayerAnzeige[id, 3].GetComponent<TMP_Text>().text = name;
            PlayerAnzeige[id, 1].GetComponent<Image>().sprite = Player.getPlayerIconByPlayerName(name).icon;
        }
        Logging.log(Logging.LogType.Debug, "WerBinIchClient", "UpdateSpieler", data);
    }
    /// <summary>
    /// Sendet das Eingegebene an den Server
    /// </summary>
    /// <param name="input"></param>
    public void ClientEnterText(TMP_InputField input)
    {
        if (!Config.CLIENT_STARTED)
            return;
        ClientUtils.SendToServer("#ClientEnterText " + input.transform.parent.name.Replace("Player (", "").Replace(")", "") + "|" + input.text);
    }
    /// <summary>
    /// Aktualisiert die Texteingaben, der Worte die erraten werden sollen
    /// </summary>
    /// <param name="data"></param>
    private void UpdateText(string data)
    {
        for (int i = 0; i < 9; i++)
            PlayerAnzeige[i, 0].GetComponent<TMP_InputField>().text = data.Replace("["+i+"]", "|").Split('|')[1];
    }
}
