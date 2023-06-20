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
        SendToServer("#JoinKniffel");

        StartCoroutine(TestConnectionToServer());
    }

    void Update()
    {
        #region Pr¸ft auf Nachrichten vom Server
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
        Logging.log(Logging.LogType.Normal, "KniffelClient", "OnApplicationQuit", "Client wird geschlossen.");
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
        Logging.log(Logging.LogType.Debug, "KniffelClient", "TestConnectionToServer", "Testet die Verbindumg zum Server.");
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
            Logging.log(Logging.LogType.Warning, "KniffelClient", "SendToServer", "Nachricht an Server konnte nicht gesendet werden.", e);
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
        Logging.log(Logging.LogType.Debug, "KniffelClient", "Commands", "Eingehende Nachricht: " + cmd + " -> " + data);
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "KniffelClient", "Commands", "Unkown Command: " + cmd + " -> " + data);
                break;

            #region Universal Commands
            case "#ServerClosed":
                Logging.log(Logging.LogType.Normal, "KniffelClient", "Commands", "Verbindumg zum Server wurde beendet. Lade ins Hauptmen¸.");
                CloseSocket();
                SceneManager.LoadSceneAsync("Startup");
                break;
            case "#UpdateRemoteConfig":
                Logging.log(Logging.LogType.Normal, "KniffelClient", "Commands", "RemoteConfig wird neugeladen");
                LoadConfigs.FetchRemoteConfig();
                break;
            case "#ZurueckInsHauptmenue":
                Logging.log(Logging.LogType.Normal, "KniffelClient", "Commands", "Spiel wird beendet. Lade ins Hauptmen¸");
                SceneManager.LoadSceneAsync("Startup");
                break;
            #endregion

            case "#InitGame":
                InitGame(data);
                break;
            case "#StartTurn":
                StartTurn(data);
                break;
            case "#UpdateLobby":
                UpdateLobby(data);
                break;
            case "#UpdatePunkteliste":
                UpdatePunkteliste(data);
                break;
            case "#PlayerWuerfel":
                SpielerWuerfelt(data);
                break;
            case "#GameEnded":
                GameEnded(data);
                break;
            case "#ClickKategorie":
                ClickKategorie(data);
                break;
            case "#SafeUnsafe":
                SafeUnsafeWuerfel(data);
                break;
            case "#DeleteClient":
                foreach (KniffelPlayer item in board.GetPlayerList())
                {
                    if (item.name == data)
                    {
                        item.Punkteliste.SetActive(false);
                        Punkteliste.transform.GetChild(2 + item.gamerid).gameObject.SetActive(false);
                        board.GetPlayerList().Remove(item);
                        break;
                    }
                }
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
            players.Add(new KniffelPlayer(Int32.Parse(item.Split('*')[0]), item.Split('*')[1], Resources.Load<Sprite>("Images/ProfileIcons/" + item.Split('*')[2]), Punkteliste.transform.GetChild(2 + players.Count).gameObject));
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
        foreach (string player in data.Replace("[#]","|").Split('|'))
        {
            string[] infos = player.Split('*');
            KniffelPlayer p = KniffelPlayer.GetPlayerById(board.GetPlayerList(), Int32.Parse(infos[0]));
            p.name = infos[1];
            p.PlayerImage = Resources.Load<Sprite>("Images/ProfileIcons/" + infos[2]);
            p.Punkteliste.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = p.PlayerImage;
            p.Einsen.SetPoints(Int32.Parse(infos[3].Split('~')[1]), p.PlayerColor);
            p.Einsen.used = bool.Parse(infos[3].Split('~')[0]);
            p.Zweien.SetPoints(Int32.Parse(infos[4].Split('~')[1]), p.PlayerColor);
            p.Zweien.used = bool.Parse(infos[4].Split('~')[0]);
            p.Dreien.SetPoints(Int32.Parse(infos[5].Split('~')[1]), p.PlayerColor);
            p.Dreien.used = bool.Parse(infos[5].Split('~')[0]);
            p.Vieren.SetPoints(Int32.Parse(infos[6].Split('~')[1]), p.PlayerColor);
            p.Vieren.used = bool.Parse(infos[6].Split('~')[0]);
            p.Fuenfen.SetPoints(Int32.Parse(infos[7].Split('~')[1]), p.PlayerColor);
            p.Fuenfen.used = bool.Parse(infos[7].Split('~')[0]);
            p.Sechsen.SetPoints(Int32.Parse(infos[8].Split('~')[1]), p.PlayerColor);
            p.Sechsen.used = bool.Parse(infos[9].Split('~')[0]);
            p.ObenSummeOhneBonus.SetPoints(Int32.Parse(infos[10].Split('~')[1]), p.PlayerColor);
            p.ObenSummeOhneBonus.used = bool.Parse(infos[10].Split('~')[0]);
            p.Bonus.SetPoints(Int32.Parse(infos[11].Split('~')[1]), p.PlayerColor);
            p.Bonus.used = bool.Parse(infos[11].Split('~')[0]);
            p.ObenSumme.SetPoints(Int32.Parse(infos[12].Split('~')[1]), p.PlayerColor);
            p.ObenSumme.used = bool.Parse(infos[12].Split('~')[0]);
            p.Dreierpasch.SetPoints(Int32.Parse(infos[13].Split('~')[1]), p.PlayerColor);
            p.Dreierpasch.used = bool.Parse(infos[13].Split('~')[0]);
            p.Viererpasch.SetPoints(Int32.Parse(infos[14].Split('~')[1]), p.PlayerColor);
            p.Viererpasch.used = bool.Parse(infos[14].Split('~')[0]);
            p.FullHouse.SetPoints(Int32.Parse(infos[15].Split('~')[1]), p.PlayerColor);
            p.FullHouse.used = bool.Parse(infos[15].Split('~')[0]);
            p.KleineStraﬂe.SetPoints(Int32.Parse(infos[16].Split('~')[1]), p.PlayerColor);
            p.KleineStraﬂe.used = bool.Parse(infos[16].Split('~')[0]);
            p.GroﬂeStraﬂe.SetPoints(Int32.Parse(infos[17].Split('~')[1]), p.PlayerColor);
            p.GroﬂeStraﬂe.used = bool.Parse(infos[17].Split('~')[0]);
            p.Kniffel.SetPoints(Int32.Parse(infos[18].Split('~')[1]), p.PlayerColor);
            p.Kniffel.used = bool.Parse(infos[18].Split('~')[0]);
            p.Chance.SetPoints(Int32.Parse(infos[19].Split('~')[1]), p.PlayerColor);
            p.Chance.used = bool.Parse(infos[19].Split('~')[0]);
            p.SummeUntererTeil.SetPoints(Int32.Parse(infos[20].Split('~')[1]), p.PlayerColor);
            p.SummeUntererTeil.used = bool.Parse(infos[20].Split('~')[0]);
            p.SummeObererTeil.SetPoints(Int32.Parse(infos[21].Split('~')[1]), p.PlayerColor);
            p.SummeObererTeil.used = bool.Parse(infos[21].Split('~')[0]);
            p.EndSumme.SetPoints(Int32.Parse(infos[22].Split('~')[1]), p.PlayerColor);
            p.EndSumme.used = bool.Parse(infos[22].Split('~')[0]);
        }
    }
    private void GameEnded(string data)
    {
        SpielIstVorbei.Play();
        foreach (KniffelPlayer player in board.GetPlayerList())
        {
            player.Punkteliste.transform.GetChild(0).GetComponent<Image>().enabled = false;
        }

        int siegerpunkte = Int32.Parse(data);
        foreach (KniffelPlayer item in board.GetPlayerList())
            if (item.EndSumme.getDisplay() == siegerpunkte)
                item.Punkteliste.transform.GetChild(0).GetComponent<Image>().enabled = true;
    }
    private void StartTurn(string data)
    {
        int id = Int32.Parse(data);
        if (StartTurnDelayedCoroutine != null)
            StopCoroutine(StartTurnDelayedCoroutine);
        StartTurnDelayedCoroutine = StartCoroutine(StartTurnDelayed(id));
    }
    IEnumerator StartTurnDelayed(int id)
    {
        // Deaktiviere buttons
        if (board.GetPlayerTurn() != null)
            for (int i = 1; i < board.GetPlayerTurn().Punkteliste.transform.childCount; i++)
            {
                GameObject btn = board.GetPlayerTurn().Punkteliste.transform.GetChild(i).gameObject;
                if (btn.name.Equals("Spacer"))
                    continue;
                btn.GetComponent<Button>().interactable = false;
            }

        yield return new WaitForSeconds(1f);

        board.playersTurn = KniffelPlayer.GetPlayerById(board.GetPlayerList(), id);
        KniffelPlayer player = board.GetPlayerTurn();
        player.availablewuerfe = 3;
        board.BlendeIstDranOutlineEin(player);
        Logging.log(Logging.LogType.Debug, "KniffelServer", "StartTurn", "Der Spieler " + player.name + " ist dran.");
        player.safewuerfel = new List<int>();
        player.unsafewuerfe = new List<int>();

        // wenn Client dran ist, dann w¸rfel aktivieren
        if (player.name == Config.PLAYER_NAME)
        {
            SpielerIstDran.Play();

            WuerfelBoard.transform.GetChild(6).gameObject.SetActive(true);

            // Aktiviere Buttons
            for (int i = 1; i < board.GetPlayerTurn().Punkteliste.transform.childCount; i++)
            {
                GameObject btn = board.GetPlayerTurn().Punkteliste.transform.GetChild(i).gameObject;
                if (btn.name.Equals("Spacer"))
                    continue;
                KniffelKategorie kategorie = player.GetKategorie(btn.gameObject.name);
                if (kategorie != null)
                    if (!kategorie.used && kategorie.clickable)
                        btn.GetComponent<Button>().interactable = true;
            }
        }
        else
        {
            WuerfelBoard.transform.GetChild(6).gameObject.SetActive(false);
        }
        yield break;
    }
    private void SpielerWuerfelt(string data)
    {
        int gamerid = Int32.Parse(data.Split('*')[0]);
        data = data.Split('*')[1];
        // Nachricht ist von einem selber
        if (gamerid == KniffelPlayer.GetPlayerByName(board.GetPlayerList(), Config.PLAYER_NAME).gamerid)
            return;
        // Spieler war noch nicht dran
        if (board.GetPlayerTurn() != null)
            if (board.GetPlayerTurn().gamerid != gamerid)
            {
                StartTurn("" + gamerid);
            }
        // Generiere Ergebnis
        board.GetPlayerTurn().unsafewuerfe.Clear();
        board.GetPlayerTurn().safewuerfel.Clear();
        if (data.Replace("[#]", "|").Split('|')[0].Length != 0)
            foreach (string item in data.Replace("[#]", "|").Split('|')[0].Split('+'))
                board.GetPlayerTurn().unsafewuerfe.Add(Int32.Parse(item));
        if (data.Replace("[#]", "|").Split('|')[1].Length != 0)
            foreach (string item in data.Replace("[#]", "|").Split('|')[1].Split('+'))
            board.GetPlayerTurn().safewuerfel.Add(Int32.Parse(item));

        // Starte Animation
        if (StartWuerfelAnimationCoroutine != null)
            StopCoroutine(StartWuerfelAnimation());
        StartWuerfelAnimationCoroutine = StartCoroutine(StartWuerfelAnimation());
    }
    public void ClientWuerfelt()
    {
        if (!Config.CLIENT_STARTED)
            return;
        if (board.GetPlayerTurn().name != Config.PLAYER_NAME)
            return;
        if (board.GetPlayerTurn().availablewuerfe <= 0)
            return;
        board.GetPlayerTurn().availablewuerfe--;
        if (board.GetPlayerTurn().availablewuerfe <= 0)
            WuerfelBoard.transform.GetChild(6).gameObject.SetActive(false);

        // Generiere Ergebnis
        string msgZahlenUnsafe = "";
        board.GetPlayerTurn().unsafewuerfe.Clear();
        for (int i = 0; i < 5 - board.GetPlayerTurn().safewuerfel.Count; i++)
        {
            int randomzahl = UnityEngine.Random.Range(1, 7);
            msgZahlenUnsafe += "+" + randomzahl;
            board.GetPlayerTurn().unsafewuerfe.Add(randomzahl);
        }
        if (msgZahlenUnsafe.Length > 1)
            msgZahlenUnsafe = msgZahlenUnsafe.Substring("+".Length);

        string msgZahlenSafe = "";
        foreach (int safeZ in board.GetPlayerTurn().safewuerfel)
        {
            msgZahlenSafe += "+" + safeZ;
        }
        if (msgZahlenSafe.Length > 1)
            msgZahlenSafe = msgZahlenSafe.Substring("+".Length);

        SendToServer("#WuerfelnClient " + board.GetPlayerTurn().gamerid + "*" + msgZahlenUnsafe + "[#]" + msgZahlenSafe);

        // Starte Animation
        if (StartWuerfelAnimationCoroutine != null)
            StopCoroutine(StartWuerfelAnimation());
        StartWuerfelAnimationCoroutine = StartCoroutine(StartWuerfelAnimation());
    }
    IEnumerator StartWuerfelAnimation()
    {
        List<GameObject> unsafes = new List<GameObject>();
        for (int i = 0; i < WuerfelBoard.transform.GetChild(0).GetChild(0).childCount; i++)
        {
            unsafes.Add(WuerfelBoard.transform.GetChild(0).GetChild(0).GetChild(i).gameObject);
            WuerfelBoard.transform.GetChild(0).GetChild(0).GetChild(i).gameObject.SetActive(false);
        }
        List<GameObject> safes = new List<GameObject>();
        for (int i = 1; i <= 5; i++)
        {
            safes.Add(WuerfelBoard.transform.GetChild(i).gameObject);
            safes[i - 1].GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/GUI/w¸rfel");
        }

        for (int i = 0; i < board.GetPlayerTurn().safewuerfel.Count; i++)
            safes[i].GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/GUI/w¸rfel " + board.GetPlayerTurn().safewuerfel[i]);

        yield return new WaitForSeconds(0.01f);

        for (int i = 0; i < board.GetPlayerTurn().unsafewuerfe.Count; i++)
        {
            int randomwuerfel = UnityEngine.Random.Range(0, unsafes.Count);
            unsafes[randomwuerfel].GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/GUI/w¸rfel " + board.GetPlayerTurn().unsafewuerfe[i]);
            unsafes[randomwuerfel].gameObject.SetActive(true);
            unsafes.RemoveAt(randomwuerfel);
            yield return new WaitForSeconds(0.02f);
        }
        AktualisiereKategorien();
        yield break;
    }
    private void AktualisiereKategorien()
    {
        KniffelPlayer p = board.GetPlayerTurn();
        List<int> zahlen = new List<int>();
        zahlen.AddRange(p.safewuerfel);
        zahlen.AddRange(p.unsafewuerfe);

        if (!p.Einsen.used)
        {
            int temp = 0;
            foreach (int item in zahlen)
                if (item == 1)
                    temp += item;
            p.Einsen.DisplayTest(temp);
            if (board.GetPlayerTurn().name == Config.PLAYER_NAME)
                p.Einsen.button.GetComponent<Button>().interactable = true;
        }
        if (!p.Zweien.used)
        {
            int temp = 0;
            foreach (int item in zahlen)
                if (item == 2)
                    temp += item;
            p.Zweien.DisplayTest(temp);
            if (board.GetPlayerTurn().name == Config.PLAYER_NAME)
                p.Zweien.button.GetComponent<Button>().interactable = true;
        }
        if (!p.Dreien.used)
        {
            int temp = 0;
            foreach (int item in zahlen)
                if (item == 3)
                    temp += item;
            p.Dreien.DisplayTest(temp);
            if (board.GetPlayerTurn().name == Config.PLAYER_NAME)
                p.Dreien.button.GetComponent<Button>().interactable = true;
        }
        if (!p.Vieren.used)
        {
            int temp = 0;
            foreach (int item in zahlen)
                if (item == 4)
                    temp += item;
            p.Vieren.DisplayTest(temp);
            if (board.GetPlayerTurn().name == Config.PLAYER_NAME)
                p.Vieren.button.GetComponent<Button>().interactable = true;
        }
        if (!p.Fuenfen.used)
        {
            int temp = 0;
            foreach (int item in zahlen)
                if (item == 5)
                    temp += item;
            p.Fuenfen.DisplayTest(temp);
            if (board.GetPlayerTurn().name == Config.PLAYER_NAME)
                p.Fuenfen.button.GetComponent<Button>().interactable = true;
        }
        if (!p.Sechsen.used)
        {
            int temp = 0;
            foreach (int item in zahlen)
                if (item == 6)
                    temp += item;
            p.Sechsen.DisplayTest(temp);
            if (board.GetPlayerTurn().name == Config.PLAYER_NAME)
                p.Sechsen.button.GetComponent<Button>().interactable = true;
        }
        if (!p.Dreierpasch.used)
        {
            int[] temp1 = new int[6];
            int temp = 0;
            foreach (int item in zahlen)
            {
                temp1[item - 1]++;
                temp += item;
            }
            foreach (int item in temp1)
            {
                if (item >= 3)
                {
                    p.Dreierpasch.DisplayTest(temp);
                    temp = 0;
                    break;
                }
            }
            if (temp != 0)
                p.Dreierpasch.DisplayTest(0);
            if (board.GetPlayerTurn().name == Config.PLAYER_NAME)
                p.Dreierpasch.button.GetComponent<Button>().interactable = true;
        }
        if (!p.Viererpasch.used)
        {
            int[] temp2 = new int[6];
            int temp = 0;
            foreach (int item in zahlen)
            {
                temp2[item - 1]++;
                temp += item;
            }
            foreach (int item in temp2)
            {
                if (item >= 4)
                {
                    p.Viererpasch.DisplayTest(temp);
                    temp = 0;
                    break;
                }
            }
            if (temp != 0)
                p.Viererpasch.DisplayTest(0);
            if (board.GetPlayerTurn().name == Config.PLAYER_NAME)
                p.Viererpasch.button.GetComponent<Button>().interactable = true;
        }
        if (!p.FullHouse.used)
        {
            int[] temp3 = new int[6];
            int temp = 0;
            foreach (int item in zahlen)
            {
                temp3[item - 1]++;
            }
            foreach (int item in temp3)
            {
                if (item == 3)
                {
                    temp++;
                    break;
                }
            }
            foreach (int item in temp3)
            {
                if (item == 2)
                {
                    temp++;
                    break;
                }
            }
            if (temp == 2)
                p.FullHouse.DisplayTest(25);
            else
                p.FullHouse.DisplayTest(0);
            if (board.GetPlayerTurn().name == Config.PLAYER_NAME)
                p.FullHouse.button.GetComponent<Button>().interactable = true;
        }
        if (!p.KleineStraﬂe.used)
        {
            if (zahlen.Contains(1) && zahlen.Contains(2) && zahlen.Contains(3) && zahlen.Contains(4) ||
            zahlen.Contains(2) && zahlen.Contains(3) && zahlen.Contains(4) && zahlen.Contains(5) ||
            zahlen.Contains(3) && zahlen.Contains(4) && zahlen.Contains(5) && zahlen.Contains(6))
                p.KleineStraﬂe.DisplayTest(30);
            else
                p.KleineStraﬂe.DisplayTest(0);
            if (board.GetPlayerTurn().name == Config.PLAYER_NAME)
                p.KleineStraﬂe.button.GetComponent<Button>().interactable = true;
        }
        if (!p.GroﬂeStraﬂe.used)
        {
            if (zahlen.Contains(1) && zahlen.Contains(2) && zahlen.Contains(3) && zahlen.Contains(4) && zahlen.Contains(5) ||
            zahlen.Contains(2) && zahlen.Contains(3) && zahlen.Contains(4) && zahlen.Contains(5) && zahlen.Contains(6))
                p.GroﬂeStraﬂe.DisplayTest(40);
            else
                p.GroﬂeStraﬂe.DisplayTest(0);
            if (board.GetPlayerTurn().name == Config.PLAYER_NAME)
                p.GroﬂeStraﬂe.button.GetComponent<Button>().interactable = true;
        }
        if (!p.Kniffel.used)
        {
            if ((zahlen[0] == zahlen[1]) && (zahlen[0] == zahlen[2]) && (zahlen[0] == zahlen[3]) && (zahlen[0] == zahlen[4]))
                p.Kniffel.DisplayTest(50);
            else
                p.Kniffel.DisplayTest(0);
            if (board.GetPlayerTurn().name == Config.PLAYER_NAME)
                p.Kniffel.button.GetComponent<Button>().interactable = true;
        }
        if (!p.Chance.used)
        {
            int temp = 0;
            foreach (int item in zahlen)
                temp += item;
            p.Chance.DisplayTest(temp);
            if (board.GetPlayerTurn().name == Config.PLAYER_NAME)
                p.Chance.button.GetComponent<Button>().interactable = true;
        }
    }
    private void SafeUnsafeWuerfel(string data)
    {
        string type = data.Split('|')[0];
        KniffelPlayer p = KniffelPlayer.GetPlayerById(board.GetPlayerList(), Int32.Parse(data.Split('|')[1]));
        if (p.name == Config.PLAYER_NAME)
            return;
        int zahl = Int32.Parse(data.Split('|')[2]);

        if (type == "Safe")
        {
            for (int i = 0; i < WuerfelBoard.transform.GetChild(0).GetChild(0).childCount; i++)
            {
                Image wuerfel = WuerfelBoard.transform.GetChild(0).GetChild(0).GetChild(i).GetComponent<Image>();
                if (wuerfel.sprite.name.EndsWith("" + zahl) && wuerfel.gameObject.activeInHierarchy)
                {
                    board.GetPlayerTurn().unsafewuerfe.Remove(zahl);
                    board.GetPlayerTurn().safewuerfel.Add(zahl);
                    // Aus Brett ausblenden
                    wuerfel.gameObject.SetActive(false);
                    wuerfel.sprite = Resources.Load<Sprite>("Images/GUI/w¸rfel");

                    for (int j = 0; j < SafeWuerfel.Count; j++)
                    {
                        if (SafeWuerfel[j].sprite.name.Equals("w¸rfel"))
                        {
                            SafeWuerfel[j].sprite = Resources.Load<Sprite>("Images/GUI/w¸rfel " + zahl);
                            break;
                        }
                    }
                    break;
                }
            }
        }
        else if (type == "Unsafe")
        {
            for (int i = 0; i < SafeWuerfel.Count; i++)
            {
                if (SafeWuerfel[i].sprite.name.Replace("w¸rfel ", "").Equals("" + zahl))
                {
                    board.GetPlayerTurn().safewuerfel.Remove(zahl);
                    board.GetPlayerTurn().unsafewuerfe.Add(zahl);
                    SafeWuerfel[i].sprite = Resources.Load<Sprite>("Images/GUI/w¸rfel");

                    for (int j = 0; j < WuerfelBoard.transform.GetChild(0).GetChild(0).childCount; j++)
                    {
                        Image boardImage = WuerfelBoard.transform.GetChild(0).GetChild(0).GetChild(j).GetComponent<Image>();
                        if (!boardImage.gameObject.activeInHierarchy)
                        {
                            boardImage.sprite = Resources.Load<Sprite>("Images/GUI/w¸rfel " + zahl);
                            boardImage.gameObject.SetActive(true);
                            break;
                        }
                    }
                    break;
                }
            }
        }
    }
    public void ClientSafeUnsafeWuerfel(GameObject wuerfel)
    {
        if (!Config.CLIENT_STARTED)
            return;
        if (board.GetPlayerTurn().name != Config.PLAYER_NAME)
            return;
        string spritename = wuerfel.GetComponent<Image>().sprite.name.Replace("w¸rfel", "");
        if (spritename.Length == 0)
            return;
        int zahl = Int32.Parse(wuerfel.GetComponent<Image>().sprite.name.Replace("w¸rfel ", ""));
        string type = "";
        if (wuerfel.name.StartsWith("SafeWuerfel"))
        {
            type = "Unsafe";
            SendToServer("#SafeUnsafe Unsafe|" + board.GetPlayerTurn().gamerid + "|" + zahl);
            //SafeUnsafeWuerfel("Unsafe|" + board.GetPlayerTurn().gamerid + "|" + zahl);
        }
        else
        {
            type = "Safe";
            SendToServer("#SafeUnsafe Safe|" + board.GetPlayerTurn().gamerid + "|" + zahl);
            //SafeUnsafeWuerfel("Safe|" + board.GetPlayerTurn().gamerid + "|" + zahl);
        }

        if (type == "Safe")
        {
            for (int i = 0; i < WuerfelBoard.transform.GetChild(0).GetChild(0).childCount; i++)
            {
                Image wuerfelImage = WuerfelBoard.transform.GetChild(0).GetChild(0).GetChild(i).GetComponent<Image>();
                if (wuerfelImage.sprite.name.EndsWith("" + zahl) && wuerfelImage.gameObject.activeInHierarchy)
                {
                    board.GetPlayerTurn().unsafewuerfe.Remove(zahl);
                    board.GetPlayerTurn().safewuerfel.Add(zahl);
                    // Aus Brett ausblenden
                    wuerfelImage.gameObject.SetActive(false);
                    wuerfelImage.sprite = Resources.Load<Sprite>("Images/GUI/w¸rfel");

                    for (int j = 0; j < SafeWuerfel.Count; j++)
                    {
                        if (SafeWuerfel[j].sprite.name.Equals("w¸rfel"))
                        {
                            SafeWuerfel[j].sprite = Resources.Load<Sprite>("Images/GUI/w¸rfel " + zahl);
                            break;
                        }
                    }
                    break;
                }
            }
        }
        else if (type == "Unsafe")
        {
            for (int i = 0; i < SafeWuerfel.Count; i++)
            {
                if (SafeWuerfel[i].sprite.name.Replace("w¸rfel ", "").Equals("" + zahl))
                {
                    board.GetPlayerTurn().safewuerfel.Remove(zahl);
                    board.GetPlayerTurn().unsafewuerfe.Add(zahl);
                    SafeWuerfel[i].sprite = Resources.Load<Sprite>("Images/GUI/w¸rfel");

                    for (int j = 0; j < WuerfelBoard.transform.GetChild(0).GetChild(0).childCount; j++)
                    {
                        Image boardImage = WuerfelBoard.transform.GetChild(0).GetChild(0).GetChild(j).GetComponent<Image>();
                        if (!boardImage.gameObject.activeInHierarchy)
                        {
                            boardImage.sprite = Resources.Load<Sprite>("Images/GUI/w¸rfel " + zahl);
                            boardImage.gameObject.SetActive(true);
                            break;
                        }
                    }
                    break;
                }
            }
        }
    }
    private void ClickKategorie(string data)
    {
        // TODO falsche spalte wird eingeblendet mit vorschau (nur wenn zuschnell gew‰hlt wird)

        KniffelPlayer player = KniffelPlayer.GetPlayerById(board.GetPlayerList(), Int32.Parse(data.Split('|')[0]));
        KniffelKategorie kategorie = player.GetKategorie(data.Split('|')[1]);
        
        WuerfelBoard.transform.GetChild(6).gameObject.SetActive(false);

        player.safewuerfel.Clear();
        player.unsafewuerfe.Clear();
        if (data.Split('|')[2].Length != 0)
            foreach (string item in data.Split('|')[2].Split('+'))
                player.safewuerfel.Add(Int32.Parse(item));
        if (data.Split('|')[3].Length != 0)
            foreach (string item in data.Split('|')[3].Split('+'))
                player.unsafewuerfe.Add(Int32.Parse(item));

        List<int> zahlen = new List<int>();
        zahlen.AddRange(player.safewuerfel);
        zahlen.AddRange(player.unsafewuerfe);
        switch (kategorie.name)
        {
            default:
                break;
            case "Einsen":
                int temp = 0;
                foreach (int item in zahlen)
                    if (item == 1)
                        temp += item;
                player.Einsen.SetPoints(temp, player.PlayerColor);
                break;
            case "Zweien":
                temp = 0;
                foreach (int item in zahlen)
                    if (item == 2)
                        temp += item;
                player.Zweien.SetPoints(temp, player.PlayerColor);
                break;
            case "Dreien":
                temp = 0;
                foreach (int item in zahlen)
                    if (item == 3)
                        temp += item;
                player.Dreien.SetPoints(temp, player.PlayerColor);
                break;
            case "Vieren":
                temp = 0;
                foreach (int item in zahlen)
                    if (item == 4)
                        temp += item;
                player.Vieren.SetPoints(temp, player.PlayerColor);
                break;
            case "Fuenfen":
                temp = 0;
                foreach (int item in zahlen)
                    if (item == 5)
                        temp += item;
                player.Fuenfen.SetPoints(temp, player.PlayerColor);
                break;
            case "Sechsen":
                temp = 0;
                foreach (int item in zahlen)
                    if (item == 6)
                        temp += item;
                player.Sechsen.SetPoints(temp, player.PlayerColor);
                break;
            case "Dreierpasch":
                int[] temp1 = new int[6];
                temp = 0;
                foreach (int item in zahlen)
                {
                    temp1[item - 1]++;
                    temp += item;
                }
                foreach (int item in temp1)
                {
                    if (item >= 3)
                    {
                        player.Dreierpasch.SetPoints(temp, player.PlayerColor);
                        temp = 0;
                        break;
                    }
                }
                if (temp != 0)
                    player.Dreierpasch.SetPoints(0, player.PlayerColor);
                break;
            case "Viererpasch":
                temp1 = new int[6];
                temp = 0;
                foreach (int item in zahlen)
                {
                    temp1[item - 1]++;
                    temp += item;
                }
                foreach (int item in temp1)
                {
                    if (item >= 4)
                    {
                        player.Viererpasch.SetPoints(temp, player.PlayerColor);
                        temp = 0;
                        break;
                    }
                }
                if (temp != 0)
                    player.Viererpasch.SetPoints(0, player.PlayerColor);
                break;
            case "FullHouse":
                int[] temp3 = new int[6];
                temp = 0;
                foreach (int item in zahlen)
                {
                    temp3[item - 1]++;
                }
                foreach (int item in temp3)
                {
                    if (item == 3)
                    {
                        temp++;
                        break;
                    }
                }
                foreach (int item in temp3)
                {
                    if (item == 2)
                    {
                        temp++;
                        break;
                    }
                }
                if (temp == 2)
                    player.FullHouse.SetPoints(25, player.PlayerColor);
                else
                    player.FullHouse.SetPoints(0, player.PlayerColor);
                break;
            case "KleineStraﬂe":
                if (zahlen.Contains(1) && zahlen.Contains(2) && zahlen.Contains(3) && zahlen.Contains(4) ||
                    zahlen.Contains(2) && zahlen.Contains(3) && zahlen.Contains(4) && zahlen.Contains(5) ||
                    zahlen.Contains(3) && zahlen.Contains(4) && zahlen.Contains(5) && zahlen.Contains(6))
                    player.KleineStraﬂe.SetPoints(30, player.PlayerColor);
                else
                    player.KleineStraﬂe.SetPoints(0, player.PlayerColor);
                break;
            case "GroﬂeStraﬂe":
                if (zahlen.Contains(1) && zahlen.Contains(2) && zahlen.Contains(3) && zahlen.Contains(4) && zahlen.Contains(5) ||
                    zahlen.Contains(2) && zahlen.Contains(3) && zahlen.Contains(4) && zahlen.Contains(5) && zahlen.Contains(6))
                    player.GroﬂeStraﬂe.SetPoints(40, player.PlayerColor);
                else
                    player.GroﬂeStraﬂe.SetPoints(0, player.PlayerColor);
                break;
            case "Kniffel":
                if ((zahlen[0] == zahlen[1]) && (zahlen[0] == zahlen[2]) && (zahlen[0] == zahlen[3]) && (zahlen[0] == zahlen[4]))
                    player.Kniffel.SetPoints(50, player.PlayerColor);
                else
                    player.Kniffel.SetPoints(0, player.PlayerColor);
                break;
            case "Chance":
                temp = 0;
                foreach (int item in zahlen)
                    temp += item;
                player.Chance.SetPoints(temp, player.PlayerColor);
                break;
        }

        AktualisierePointsKategorien(player);
    }
    private void AktualisierePointsKategorien(KniffelPlayer p)
    {
        // lˆsche test vorschau
        if (!p.Einsen.used)
            p.Einsen.button.GetComponentInChildren<TMP_Text>().text = "";
        if (!p.Zweien.used)
            p.Zweien.button.GetComponentInChildren<TMP_Text>().text = "";
        if (!p.Dreien.used)
            p.Dreien.button.GetComponentInChildren<TMP_Text>().text = "";
        if (!p.Vieren.used)
            p.Vieren.button.GetComponentInChildren<TMP_Text>().text = "";
        if (!p.Fuenfen.used)
            p.Fuenfen.button.GetComponentInChildren<TMP_Text>().text = "";
        if (!p.Sechsen.used)
            p.Sechsen.button.GetComponentInChildren<TMP_Text>().text = "";
        if (!p.Dreierpasch.used)
            p.Dreierpasch.button.GetComponentInChildren<TMP_Text>().text = "";
        if (!p.Viererpasch.used)
            p.Viererpasch.button.GetComponentInChildren<TMP_Text>().text = "";
        if (!p.FullHouse.used)
            p.FullHouse.button.GetComponentInChildren<TMP_Text>().text = "";
        if (!p.KleineStraﬂe.used)
            p.KleineStraﬂe.button.GetComponentInChildren<TMP_Text>().text = "";
        if (!p.GroﬂeStraﬂe.used)
            p.GroﬂeStraﬂe.button.GetComponentInChildren<TMP_Text>().text = "";
        if (!p.Kniffel.used)
            p.Kniffel.button.GetComponentInChildren<TMP_Text>().text = "";
        if (!p.Chance.used)
            p.Chance.button.GetComponentInChildren<TMP_Text>().text = "";

        // Summen aktualisieren
        p.ObenSummeOhneBonus.SetPoints(p.Einsen.getDisplay() + p.Zweien.getDisplay() + p.Dreien.getDisplay() + p.Vieren.getDisplay() + p.Fuenfen.getDisplay() + p.Sechsen.getDisplay(), p.PlayerColor);
        if (p.ObenSummeOhneBonus.getDisplay() >= 63)
            p.Bonus.SetPoints(35, p.PlayerColor);
        p.ObenSumme.SetPoints(p.ObenSummeOhneBonus.getDisplay() + p.Bonus.getDisplay(), p.PlayerColor);

        p.SummeUntererTeil.SetPoints(p.Dreierpasch.getDisplay() + p.Viererpasch.getDisplay() + p.FullHouse.getDisplay() + p.KleineStraﬂe.getDisplay() + p.GroﬂeStraﬂe.getDisplay() + p.Kniffel.getDisplay() + p.Chance.getDisplay(), p.PlayerColor);
        p.SummeObererTeil.SetPoints(p.ObenSumme.getDisplay(), p.PlayerColor);

        p.EndSumme.SetPoints(p.SummeObererTeil.getDisplay() + p.SummeUntererTeil.getDisplay(), p.PlayerColor);

        for (int i = 0; i < WuerfelBoard.transform.GetChild(0).GetChild(0).childCount; i++)
        {
            Image wuerfel = WuerfelBoard.transform.GetChild(0).GetChild(0).GetChild(i).GetComponent<Image>();
            wuerfel.gameObject.SetActive(false);
            wuerfel.sprite = Resources.Load<Sprite>("Images/GUI/w¸rfel");
        }
        for (int i = 0; i < SafeWuerfel.Count; i++)
        {
            SafeWuerfel[i].sprite = Resources.Load<Sprite>("Images/GUI/w¸rfel");
        }

        //StartTurn();
    }

    private string GetWuerfelString()
    {
        string unsavewuerfel = "";
        for (int i = 0; i < board.GetPlayerTurn().unsafewuerfe.Count; i++)
        {
            unsavewuerfel += "+" + board.GetPlayerTurn().unsafewuerfe[i];
        }
        if (unsavewuerfel.Length > 1)
            unsavewuerfel = unsavewuerfel.Substring("+".Length);
        string savewuerfel = "";
        for (int i = 0; i < board.GetPlayerTurn().safewuerfel.Count; i++)
        {
            savewuerfel += "+" + board.GetPlayerTurn().safewuerfel[i];
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
    public void ClickKleineStraﬂe(GameObject Spieler)
    {
        if (!Config.CLIENT_STARTED)
            return;
        SendToServer("#ClickPlayerKategorie KleineStraﬂe*" + GetWuerfelString());
    }
    public void ClickGroﬂeStraﬂe(GameObject Spieler)
    {
        if (!Config.CLIENT_STARTED)
            return;
        SendToServer("#ClickPlayerKategorie GroﬂeStraﬂe*" + GetWuerfelString());
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