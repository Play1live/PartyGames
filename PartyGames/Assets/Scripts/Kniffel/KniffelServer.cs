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


    private List<string> broadcastmsgs;

    void OnEnable()
    {
        broadcastmsgs = new List<string>();
        PlayerConnected = new bool[Config.SERVER_MAX_CONNECTIONS];
        InitGame();
        StartCoroutine(NewBroadcast());
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

            #region Spieler Disconnected Message
            for (int i = 0; i < Config.PLAYERLIST.Length; i++)
            {
                if (Config.PLAYERLIST[i].isConnected == false)
                {
                    if (Config.PLAYERLIST[i].isDisconnected == true)
                    {
                        Logging.log(Logging.LogType.Normal, "MenschƒrgerDichNichtServer", "Update", "Spieler hat die Verbindung getrennt. ID: " + Config.PLAYERLIST[i].id);
                        Broadcast(Config.PLAYERLIST[i].name + " has disconnected", Config.PLAYERLIST);
                        Config.PLAYERLIST[i].isConnected = false;
                        Config.PLAYERLIST[i].isDisconnected = false;
                        Config.SERVER_ALL_CONNECTED = false;
                        Config.PLAYERLIST[i].name = "";
                    }
                }
            }
            #endregion
        }
        #endregion
    }

    private void OnApplicationQuit()
    {
        Broadcast("#ServerClosed", Config.PLAYERLIST);
        Logging.log(Logging.LogType.Normal, "MenschƒrgerDichNichtServer", "OnApplicationQuit", "Server wird geschlossen.");
        Config.SERVER_TCP.Server.Close();
    }

    IEnumerator NewBroadcast()
    {
        while (true)
        {
            // Broadcastet alle MSGs nacheinander
            if (broadcastmsgs.Count != 0)
            {
                string msg = broadcastmsgs[0];
                broadcastmsgs.RemoveAt(0);
                Broadcast(msg);
                yield return null;
            }
            //yield return new WaitForSeconds(0.005f);
            yield return new WaitForSeconds(0.01f);
        }
        yield break;
    }

    #region Server Stuff  
    #region Kommunikation
    /// <summary>
    /// Sendet eine Nachricht an den ¸bergebenen Spieler
    /// </summary>
    /// <param name="data">Nachricht</param>
    /// <param name="sc">Spieler</param>
    private void SendMSG(string data, Player sc)
    {
        try
        {
            StreamWriter writer = new StreamWriter(sc.tcp.GetStream());
            writer.WriteLine(data);
            writer.Flush();
        }
        catch (Exception e)
        {
            Logging.log(Logging.LogType.Warning, "MenschƒrgerDichNichtServer", "SendMSG", "Nachricht an Client: " + sc.id + " (" + sc.name + ") konnte nicht gesendet werden.", e);
            // Verbindung zum Client wird getrennt
            ClientClosed(sc);
        }
    }
    /// <summary>
    /// Sendet eine Nachricht an alle Spieler der liste
    /// </summary>
    /// <param name="data">Nachricht</param>
    /// <param name="spieler">Spielerliste</param>
    private void Broadcast(string data, Player[] spieler)
    {
        foreach (Player sc in spieler)
        {
            if (sc.isConnected)
                SendMSG(data, sc);
        }
    }
    /// <summary>
    /// Sendet eine Nachricht an alle verbundenen Spieler
    /// </summary>
    /// <param name="data">Nachricht</param>
    private void Broadcast(string data)
    {
        foreach (Player sc in Config.PLAYERLIST)
        {
            if (sc.isConnected)
                SendMSG(data, sc);
        }
    }
    private void BroadcastNew(string data)
    {
        broadcastmsgs.Add(data);
    }
    /// <summary>
    /// Einkommende Nachrichten die von Spielern an den Server gesendet werden
    /// </summary>
    /// <param name="spieler">Spieler</param>
    /// <param name="data">Nachricht</param>
    private void OnIncommingData(Player spieler, string data)
    {
        string cmd;
        if (data.Contains(" "))
            cmd = data.Split(' ')[0];
        else
            cmd = data;
        data = data.Replace(cmd + " ", "");

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
                // TODO;: SpielerWirdZumBot(player);
                PlayDisconnectSound();
                ClientClosed(player);
                break;
            case "#TestConnection":
                break;
            case "#ClientFocusChange":
                break;

            case "#JoinMenschAergerDichNicht":
                PlayerConnected[player.id - 1] = true;
                // TODO: UpdateLobby();
                break;

            case "#ClickPlayerKategorie":
                ClickPlayerKategorie(player, data);
                break;
            case "#WuerfelnClient":
                WuerfelnClient(player, data);
                break;
            case "#ClientSafeUnsafeWuerfel":
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
        player.icon = Resources.Load<Sprite>("Images/ProfileIcons/empty");
        player.name = "";
        player.points = 0;
        player.isConnected = false;
        player.isDisconnected = true;
    }
    /// <summary>
    /// Spiel Verlassen & Zur¸ck in die Lobby laden
    /// </summary>
    public void SpielVerlassenButton()
    {
        Logging.log(Logging.LogType.Debug, "MenschƒrgerDichNichtServer", "SpielVerlassenButton", "Spiel wird beendet. L‰dt ins Hauptmen¸.");
        SceneManager.LoadScene("Startup");
        BroadcastNew("#ZurueckInsHauptmenue");
    }
    /// <summary>
    /// Spielt den Disconnect Sound ab
    /// </summary>
    private void PlayDisconnectSound()
    {
        DisconnectSound.Play();
    }
    #region GameLogic
    private void InitGame() // TODO send p list mit namen bildern und id
    {
        List<KniffelPlayer> player = new List<KniffelPlayer>();
        int playercounter = 0;
        player.Add(new KniffelPlayer(playercounter++, Config.PLAYER_NAME, Config.SERVER_ICON, Punkteliste.transform.GetChild(1 + playercounter++).gameObject));
        foreach (Player p in Config.PLAYERLIST)
        {
            if (p.isConnected && p.name.Length > 0)
            {
                player.Add(new KniffelPlayer(playercounter++, p.name, p.icon, Punkteliste.transform.GetChild(1 + player.Count).gameObject));
            }
        }
        string plist = "";
        foreach (KniffelPlayer item in player)
        {
            plist += "[#]" + item.gamerid + "*" + item.name + "*" + item.PlayerImage.name;
        }
        if (plist.Length > 3)
            plist = plist.Substring("[#]".Length);
        BroadcastNew("#InitGame " + plist);

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

        BroadcastNew("#UpdatePunkteliste " + msg);
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
            BroadcastNew("#GameEnded");
            SpielIstVorbei.Play();
            foreach (KniffelPlayer player in board.GetPlayerList())
            {
                player.Punkteliste.transform.GetChild(0).GetComponent<Outline>().enabled = false;
            }
            return;
        }


        if (StartTurnDelayedCoroutine != null)
            StopCoroutine(StartTurnDelayedCoroutine);
        StartTurnDelayedCoroutine = StartCoroutine(StartTurnDelayed());
    }
    IEnumerator StartTurnDelayed()
    {
        yield return new WaitForSeconds(1f);

        KniffelPlayer player = board.PlayerTurnSelect();
        Logging.log(Logging.LogType.Debug, "KniffelServer", "StartTurn", "Der Spieler " + player.name + " ist dran.");
        player.safewuerfel = new List<int>();
        player.unsafewuerfe = new List<int>();

        BroadcastNew("#StartTurn " + player.gamerid);

        // wenn Server dran ist, dann w¸rfel aktivieren
        if (player.name == Config.PLAYER_NAME)
        {
            SpielerIstDran.Play();

            WuerfelBoard.transform.GetChild(6).gameObject.SetActive(true);
        }
        else
        {
            WuerfelBoard.transform.GetChild(6).gameObject.SetActive(false);
        }
        yield break;
    }
    public void WuerfelnServer()
    {
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
        BroadcastNew("#PlayerWuerfel " + board.GetPlayerTurn().gamerid + "*" + msgZahlenUnsafe + "[#]" + msgZahlenSafe);

        // Starte Animation
        if (StartWuerfelAnimationCoroutine != null)
            StopCoroutine(StartWuerfelAnimation());
        StartWuerfelAnimationCoroutine = StartCoroutine(StartWuerfelAnimation());
    }
    private void WuerfelnClient(Player p, string data)
    {
        if (board.GetPlayerTurn().name != p.name)
            return;
        // Generiere Ergebnis
        board.GetPlayerTurn().unsafewuerfe.Clear();
        board.GetPlayerTurn().safewuerfel.Clear();
        string[] unsafeZ = data.Replace("[#]", "|").Split('|')[0].Split('+');
        for (int i = 0; i < unsafeZ.Length; i++)
            board.GetPlayerTurn().unsafewuerfe.Add(Int32.Parse(unsafeZ[i]));
        string[] safeZ = data.Replace("[#]", "|").Split('|')[1].Split('+');
        for (int i = 0; i < safeZ.Length; i++)
            board.GetPlayerTurn().safewuerfel.Add(Int32.Parse(unsafeZ[i]));

        //old
        // Generiere Ergebnis
        BroadcastNew("#PlayerWuerfel " + board.GetPlayerTurn().gamerid + "*" + data);

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
            p.Einsen.button.GetComponent<Button>().interactable = true;
        }
        if (!p.Zweien.used)
        {
            int temp = 0;
            foreach (int item in zahlen)
                if (item == 2)
                    temp += item;
            p.Zweien.DisplayTest(temp);
            p.Zweien.button.GetComponent<Button>().interactable = true;
        }
        if (!p.Dreien.used)
        {
            int temp = 0;
            foreach (int item in zahlen)
                if (item == 3)
                    temp += item;
            p.Dreien.DisplayTest(temp);
            p.Dreien.button.GetComponent<Button>().interactable = true;
        }
        if (!p.Vieren.used)
        {
            int temp = 0;
            foreach (int item in zahlen)
                if (item == 4)
                    temp += item;
            p.Vieren.DisplayTest(temp);
            p.Vieren.button.GetComponent<Button>().interactable = true;
        }
        if (!p.Fuenfen.used)
        {
            int temp = 0;
            foreach (int item in zahlen)
                if (item == 5)
                    temp += item;
            p.Fuenfen.DisplayTest(temp);
            p.Fuenfen.button.GetComponent<Button>().interactable = true;
        }
        if (!p.Sechsen.used)
        {
            int temp = 0;
            foreach (int item in zahlen)
                if (item == 6)
                    temp += item;
            p.Sechsen.DisplayTest(temp);
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
            p.KleineStraﬂe.button.GetComponent<Button>().interactable = true;
        }
        if (!p.GroﬂeStraﬂe.used)
        {
            if (zahlen.Contains(1) && zahlen.Contains(2) && zahlen.Contains(3) && zahlen.Contains(4) && zahlen.Contains(5) ||
            zahlen.Contains(2) && zahlen.Contains(3) && zahlen.Contains(4) && zahlen.Contains(5) && zahlen.Contains(6))
                p.GroﬂeStraﬂe.DisplayTest(40);
            else
                p.GroﬂeStraﬂe.DisplayTest(0);
            p.GroﬂeStraﬂe.button.GetComponent<Button>().interactable = true;
        }
        if (!p.Kniffel.used)
        {
            if ((zahlen[0] == zahlen[1]) && (zahlen[0] == zahlen[2]) && (zahlen[0] == zahlen[3]) && (zahlen[0] == zahlen[4]))
                p.Kniffel.DisplayTest(50);
            else
                p.Kniffel.DisplayTest(0);
            p.Kniffel.button.GetComponent<Button>().interactable = true;
        }
        if (!p.Chance.used)
        {
            int temp = 0;
            foreach (int item in zahlen)
                temp += item;
            p.Chance.DisplayTest(temp);
            p.Chance.button.GetComponent<Button>().interactable = true;
        }
    }
    private void SafeUnsafeWuerfel(string type, KniffelPlayer p, int zahl)
    {
        BroadcastNew("#SafeUnsafe " + type + "|" + p.gamerid + "|" + zahl);
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
        SafeUnsafeWuerfel(data.Split('|')[0], KniffelPlayer.GetPlayerByName(board.GetPlayerList(), data.Split('|')[1]), Int32.Parse(data.Split('|')[2]));
    }
    private void ClickKategorie(KniffelPlayer player, KniffelKategorie kategorie)
    {
        if (!kategorie.clickable || kategorie.used)
            return;
        if ((player.safewuerfel.Count + player.unsafewuerfe.Count) == 0)
            return;

        WuerfelBoard.transform.GetChild(6).gameObject.SetActive(false);

        string safewuerfel = "";
        foreach (int item in player.safewuerfel)
            safewuerfel += "+" + item;
        if (safewuerfel.Length > 1)
            safewuerfel = safewuerfel.Substring("+".Length);
        string unsafewuerfel = "";
        foreach (int item in player.unsafewuerfe)
            unsafewuerfel += "+" + item;
        if (unsafewuerfel.Length > 1)
            unsafewuerfel = unsafewuerfel.Substring("+".Length);

        BroadcastNew("#ClickKategorie " + player.gamerid + "|" + kategorie.name + "|" + safewuerfel + "|" + unsafewuerfel);

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
        // TODO lˆsche test vorschau und zug changen
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
        string[] safew = data.Split("|")[1].Split("-")[0].Split('+');
        List<int> safewlist = new List<int>();
        foreach (string ww in safew)
            safewlist.Add(Int32.Parse(ww));
        string[] unsafew = data.Split("|")[1].Split("-")[0].Split('+');
        List<int> unsafewlist = new List<int>();
        foreach (string ww in unsafew)
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