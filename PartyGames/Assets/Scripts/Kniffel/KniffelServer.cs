using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class KniffelServer : MonoBehaviour
{
    private GameObject[] Playerlist;
    bool[] PlayerConnected;

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
        StartCoroutine(ServerUtils.Broadcast());
        PlayerConnected = new bool[Config.SERVER_MAX_CONNECTIONS];
        InitGame();
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
        Logging.log(Logging.LogType.Normal, "MenschƒrgerDichNichtServer", "OnApplicationQuit", "Server wird geschlossen.");
        Config.SERVER_TCP.Server.Close();
    }

    #region Server Stuff  
    #region Kommunikation
    /// <summary>
    /// Einkommende Nachrichten die von Spielern an den Server gesendet werden
    /// </summary>
    /// <param name="spieler">Spieler</param>
    /// <param name="data">Nachricht</param>
    private void OnIncommingData(Player spieler, string data)
    {
        if (data.StartsWith(Config.GAME_TITLE + "#"))
            data = data.Substring(Config.GAME_TITLE.Length);
        else
            Logging.log(Logging.LogType.Error, "KniffelServer", "OnIncommingData", "Wrong Command format: " + data);

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
        Logging.log(Logging.LogType.Debug, "MenschƒrgerDichNichtServer", "Commands", "Eingehende Nachricht: " + player.name + " " + player.id + " -> " + cmd + "   ---   " + data);
        // Sucht nach Command
        switch (cmd)
        {
            default:
                Logging.log(Logging.LogType.Warning, "MenschƒrgerDichNichtServer", "Commands", "Unkown Command: " + cmd + " -> " + data);
                break;

            case "#ClientClosed":
                if (board.GetPlayerTurn().name == player.name)
                    StartTurn();
                PlayDisconnectSound();
                StartCoroutine(ClientClosedDelayed(player, 2));
                ServerUtils.ClientClosed(player);
                break;
            case "#TestConnection":
                break;
            case "#ClientFocusChange":
                break;

            case "#JoinKniffel":
                PlayerConnected[player.id - 1] = true;

                if (board == null)
                    return;
                foreach (Player p in Config.PLAYERLIST)
                    if (p.isConnected && p.name.Length > 0 && !PlayerConnected[p.id - 1])
                        return;
                string plist = "";
                foreach (KniffelPlayer item in board.GetPlayerList())
                {
                    plist += "[#]" + item.gamerid + "*" + item.name + "*" + item.PlayerImage.name;
                }
                if (plist.Length > 3)
                    plist = plist.Substring("[#]".Length);

                ServerUtils.AddBroadcast("#InitGame " + plist);
                break;

            case "#ClickPlayerKategorie":
                ClickPlayerKategorie(player, data);
                break;
            case "#WuerfelnClient":
                WuerfelnClient(player, data);
                break;
            case "#SafeUnsafe":
                ClientSafeUnsafeWuerfel(player, data);
                break;
        }
    }
    #endregion
    /// <summary>
    /// Spieler beendet das Spiel
    /// </summary>
    /// <param name="player">Spieler</param>
    private void ClientClosed(Player player)
    {
        string playername = player.name;

        player.icon = Resources.Load<Sprite>("Images/ProfileIcons/empty");
        player.name = "";
        player.points = 0;
        player.isConnected = false;
        player.isDisconnected = true;

        if (board != null)
        {
            ServerUtils.AddBroadcast("#DeleteClient " + playername);
            foreach (KniffelPlayer item in board.GetPlayerList())
            {
                if (item.name == playername)
                {
                    item.Punkteliste.SetActive(false);
                    Punkteliste.transform.GetChild(2 + item.gamerid).gameObject.SetActive(false);
                    board.GetPlayerList().Remove(item);
                    break;
                }
            }
        }
    }
    private IEnumerator ClientClosedDelayed(Player player, int seconds)
    {
        yield return new WaitForSeconds(seconds);
        ClientClosed(player);
        yield break;
    }
    /// <summary>
    /// Spiel Verlassen & Zur¸ck in die Lobby laden
    /// </summary>
    public void SpielVerlassenButton()
    {
        Logging.log(Logging.LogType.Debug, "MenschƒrgerDichNichtServer", "SpielVerlassenButton", "Spiel wird beendet. L‰dt ins Hauptmen¸.");
        //SceneManager.LoadScene("Startup");
        ServerUtils.AddBroadcast("#ZurueckInsHauptmenue");
    }
    /// <summary>
    /// Spielt den Disconnect Sound ab
    /// </summary>
    private void PlayDisconnectSound()
    {
        DisconnectSound.Play();
    }
    #region GameLogic
    private void InitGame()
    {
        List<KniffelPlayer> player = new List<KniffelPlayer>();
        int playercounter = 0;
        player.Add(new KniffelPlayer(playercounter++, Config.PLAYER_NAME, Config.SERVER_PLAYER.icon, Punkteliste.transform.GetChild(2 + player.Count).gameObject));
        foreach (Player p in Config.PLAYERLIST)
        {
            if (p.isConnected && p.name.Length > 0)
            {
                player.Add(new KniffelPlayer(playercounter++, p.name, p.icon, Punkteliste.transform.GetChild(2 + player.Count).gameObject));
            }
        }
        string plist = "";
        foreach (KniffelPlayer item in player)
        {
            plist += "[#]" + item.gamerid + "*" + item.name + "*" + item.PlayerImage.name;
        }
        if (plist.Length > 3)
            plist = plist.Substring("[#]".Length);
        ServerUtils.AddBroadcast("#InitGame " + plist);

        SafeWuerfel = new List<Image>();
        SafeWuerfel.Add(WuerfelBoard.transform.GetChild(1).GetComponent<Image>());
        SafeWuerfel.Add(WuerfelBoard.transform.GetChild(2).GetComponent<Image>());
        SafeWuerfel.Add(WuerfelBoard.transform.GetChild(3).GetComponent<Image>());
        SafeWuerfel.Add(WuerfelBoard.transform.GetChild(4).GetComponent<Image>());
        SafeWuerfel.Add(WuerfelBoard.transform.GetChild(5).GetComponent<Image>());
        board = new KniffelBoard(Punkteliste, WuerfelBoard, player);

        WuerfelBoard.transform.GetChild(6).gameObject.SetActive(false);
        StartTurn();
    }
    private void UpdatePunkteliste()
    {
        string msg = "";
        foreach (KniffelPlayer player in board.GetPlayerList())
        {
            msg += "[#]" + player.ToString();
        }
        if (msg.Length > 3)
            msg = msg.Substring("[#]".Length);

        ServerUtils.AddBroadcast("#UpdatePunkteliste " + msg);
    }
    private void StartTurn()
    {
        // Check for end
        bool gameend = true;
        foreach (KniffelPlayer player in board.GetPlayerList())
        {
            if (!player.GetPlayerFinished())
            {
                gameend = false;
                break;
            }
        }
        if (gameend)
        {
            foreach (KniffelPlayer player in board.GetPlayerList())
            {
                player.Punkteliste.transform.GetChild(0).GetComponent<Image>().enabled = false;
            }
            int siegerpunkte = 0;
            foreach (KniffelPlayer item in board.GetPlayerList())
                if (siegerpunkte < item.EndSumme.getDisplay())
                    siegerpunkte = item.EndSumme.getDisplay();
            foreach (KniffelPlayer item in board.GetPlayerList())
                if (item.EndSumme.getDisplay() == siegerpunkte)
                    item.Punkteliste.transform.GetChild(0).GetComponent<Image>().enabled = true;

            ServerUtils.AddBroadcast("#GameEnded " + siegerpunkte);
            SpielIstVorbei.Play();
            return;
        }

        if (StartTurnDelayedCoroutine != null)
            StopCoroutine(StartTurnDelayedCoroutine);
        StartTurnDelayedCoroutine = StartCoroutine(StartTurnDelayed());
    }
    IEnumerator StartTurnDelayed()
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

        KniffelPlayer player = board.PlayerTurnSelect();
        Logging.log(Logging.LogType.Debug, "KniffelServer", "StartTurn", "Der Spieler " + player.name + " ist dran.");
        player.safewuerfel = new List<int>();
        player.unsafewuerfe = new List<int>();
        player.availablewuerfe = 3;
        ServerUtils.AddBroadcast("#StartTurn " + player.gamerid);

        // wenn Server dran ist, dann w¸rfel aktivieren
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
    public void WuerfelnServer()
    {
        if (!Config.SERVER_STARTED)
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
        ServerUtils.AddBroadcast("#PlayerWuerfel " + board.GetPlayerTurn().gamerid + "*" + msgZahlenUnsafe + "[#]" + msgZahlenSafe);

        // Starte Animation
        if (StartWuerfelAnimationCoroutine != null)
            StopCoroutine(StartWuerfelAnimation());
        StartWuerfelAnimationCoroutine = StartCoroutine(StartWuerfelAnimation());
    }
    private void WuerfelnClient(Player p, string data)
    {
        if (board.GetPlayerTurn().name != p.name)
            return;
        if (board.GetPlayerTurn().availablewuerfe <= 0)
            return;
        board.GetPlayerTurn().availablewuerfe--;
        if (board.GetPlayerTurn().availablewuerfe <= 0)
            WuerfelBoard.transform.GetChild(6).gameObject.SetActive(false);

        // Generiere Ergebnis
        board.GetPlayerTurn().unsafewuerfe.Clear();
        board.GetPlayerTurn().safewuerfel.Clear();
        data = data.Split('*')[1];
        string unsafeZ = data.Replace("[#]", "|").Split('|')[0];
        if (unsafeZ.Length != 0)
            for (int i = 0; i < unsafeZ.Split('+').Length; i++)
                board.GetPlayerTurn().unsafewuerfe.Add(Int32.Parse(unsafeZ.Split('+')[i]));
        string safeZ = data.Replace("[#]", "|").Split('|')[1];
        if (safeZ.Length != 0)
            for (int i = 0; i < safeZ.Split('+').Length; i++)
                board.GetPlayerTurn().safewuerfel.Add(Int32.Parse(safeZ.Split('+')[i]));

        ServerUtils.AddBroadcast("#PlayerWuerfel " + board.GetPlayerTurn().gamerid + "*" + data);

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
            int temp  = 0;
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
    private void SafeUnsafeWuerfel(string type, KniffelPlayer p, int zahl)
    {
        ServerUtils.AddBroadcast("#SafeUnsafe " + type + "|" + p.gamerid + "|" + zahl);
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
    public void ServerSafeUnsafeWuerfel(GameObject wuerfel)
    {
        if (!Config.SERVER_STARTED)
            return;
        if (board.GetPlayerTurn().name != Config.PLAYER_NAME)
            return;
        string spritename = wuerfel.GetComponent<Image>().sprite.name.Replace("w¸rfel", "");
        if (spritename.Length == 0)
            return;
        int zahl = Int32.Parse(wuerfel.GetComponent<Image>().sprite.name.Replace("w¸rfel ", ""));

        if (wuerfel.name.StartsWith("SafeWuerfel"))
            SafeUnsafeWuerfel("Unsafe", KniffelPlayer.GetPlayerByName(board.GetPlayerList(), Config.PLAYER_NAME), zahl);
        else
            SafeUnsafeWuerfel("Safe", KniffelPlayer.GetPlayerByName(board.GetPlayerList(), Config.PLAYER_NAME), zahl);
    }
    private void ClientSafeUnsafeWuerfel(Player p, string data)
    {
        if (board.GetPlayerTurn().name != p.name)
            return;
        SafeUnsafeWuerfel(data.Split('|')[0], KniffelPlayer.GetPlayerById(board.GetPlayerList(), Int32.Parse(data.Split('|')[1])), Int32.Parse(data.Split('|')[2]));
    }
    private void ClickKategorie(KniffelPlayer player, KniffelKategorie kategorie)
    {
        if (!kategorie.clickable || kategorie.used)
            return;
        if ((player.safewuerfel.Count + player.unsafewuerfe.Count) == 0)
            return;
        if (player != board.GetPlayerTurn())
            return;

        WuerfelBoard.transform.GetChild(6).gameObject.SetActive(false);

        string safewuerfel = "";
        foreach (int item in player.safewuerfel)
            safewuerfel += "+" + item;
        if (safewuerfel.Length > 0)
            safewuerfel = safewuerfel.Substring("+".Length);
        string unsafewuerfel = "";
        foreach (int item in player.unsafewuerfe)
            unsafewuerfel += "+" + item;
        if (unsafewuerfel.Length > 0)
            unsafewuerfel = unsafewuerfel.Substring("+".Length);

        ServerUtils.AddBroadcast("#ClickKategorie " + player.gamerid + "|" + kategorie.name + "|" + safewuerfel + "|" + unsafewuerfel);

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
        // lˆsche test vorschau und zug changen
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

        StartTurn();
    }
    private void ClickPlayerKategorie(Player p, string data)
    {
        KniffelPlayer kp = KniffelPlayer.GetPlayerByName(board.GetPlayerList(), p.name);
        KniffelKategorie kk = kp.GetKategorie(data.Split("*")[0]);
        data = data.Split('*')[1];
        string safew = data.Split("-")[1];
        List<int> safewlist = new List<int>();
        if (safew.Length != 0)
            foreach (string ww in safew.Split('+'))
                safewlist.Add(Int32.Parse(ww));
        string unsafew = data.Split("-")[0];
        List<int> unsafewlist = new List<int>();
        if (unsafew.Length != 0)
            foreach (string ww in unsafew.Split('+'))
                unsafewlist.Add(Int32.Parse(ww));

        kp.safewuerfel = safewlist;
        kp.unsafewuerfe = unsafewlist;
        ClickKategorie(kp, kk);
    }
    public void ClickEinsen(GameObject Spieler)
    {
        if (!Config.SERVER_STARTED)
            return;
        KniffelPlayer p = KniffelPlayer.GetPlayerByName(board.GetPlayerList(), Config.PLAYER_NAME);
        ClickKategorie(p, p.Einsen);
    }
    public void ClickZweien(GameObject Spieler)
    {
        if (!Config.SERVER_STARTED)
            return;
        KniffelPlayer p = KniffelPlayer.GetPlayerByName(board.GetPlayerList(), Config.PLAYER_NAME);
        ClickKategorie(p, p.Zweien);
    }
    public void ClickDreien(GameObject Spieler)
    {
        if (!Config.SERVER_STARTED)
            return;
        KniffelPlayer p = KniffelPlayer.GetPlayerByName(board.GetPlayerList(), Config.PLAYER_NAME);
        ClickKategorie(p, p.Dreien);
    }
    public void ClickVieren(GameObject Spieler)
    {
        if (!Config.SERVER_STARTED)
            return;
        KniffelPlayer p = KniffelPlayer.GetPlayerByName(board.GetPlayerList(), Config.PLAYER_NAME);
        ClickKategorie(p, p.Vieren);
    }
    public void ClickFuenfen(GameObject Spieler)
    {
        if (!Config.SERVER_STARTED)
            return;
        KniffelPlayer p = KniffelPlayer.GetPlayerByName(board.GetPlayerList(), Config.PLAYER_NAME);
        ClickKategorie(p, p.Fuenfen);
    }
    public void ClickSechsen(GameObject Spieler)
    {
        if (!Config.SERVER_STARTED)
            return;
        KniffelPlayer p = KniffelPlayer.GetPlayerByName(board.GetPlayerList(), Config.PLAYER_NAME);
        ClickKategorie(p, p.Sechsen);
    }
    public void ClickDreierpasch(GameObject Spieler)
    {
        if (!Config.SERVER_STARTED)
            return;
        KniffelPlayer p = KniffelPlayer.GetPlayerByName(board.GetPlayerList(), Config.PLAYER_NAME);
        ClickKategorie(p, p.Dreierpasch);
    }
    public void ClickViererpasch(GameObject Spieler)
    {
        if (!Config.SERVER_STARTED)
            return;
        KniffelPlayer p = KniffelPlayer.GetPlayerByName(board.GetPlayerList(), Config.PLAYER_NAME);
        ClickKategorie(p, p.Viererpasch);
    }
    public void ClickFullHouse(GameObject Spieler)
    {
        if (!Config.SERVER_STARTED)
            return;
        KniffelPlayer p = KniffelPlayer.GetPlayerByName(board.GetPlayerList(), Config.PLAYER_NAME);
        ClickKategorie(p, p.FullHouse);
    }
    public void ClickKleineStraﬂe(GameObject Spieler)
    {
        if (!Config.SERVER_STARTED)
            return;
        KniffelPlayer p = KniffelPlayer.GetPlayerByName(board.GetPlayerList(), Config.PLAYER_NAME);
        ClickKategorie(p, p.KleineStraﬂe);
    }
    public void ClickGroﬂeStraﬂe(GameObject Spieler)
    {
        if (!Config.SERVER_STARTED)
            return;
        KniffelPlayer p = KniffelPlayer.GetPlayerByName(board.GetPlayerList(), Config.PLAYER_NAME);
        ClickKategorie(p, p.GroﬂeStraﬂe);
    }
    public void ClickKniffel(GameObject Spieler)
    {
        if (!Config.SERVER_STARTED)
            return;
        KniffelPlayer p = KniffelPlayer.GetPlayerByName(board.GetPlayerList(), Config.PLAYER_NAME);
        ClickKategorie(p, p.Kniffel);
    }
    public void ClickChance(GameObject Spieler)
    {
        if (!Config.SERVER_STARTED)
            return;
        KniffelPlayer p = KniffelPlayer.GetPlayerByName(board.GetPlayerList(), Config.PLAYER_NAME);
        ClickKategorie(p, p.Chance);
    }
    #endregion
}