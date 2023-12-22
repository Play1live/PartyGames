using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Audio;
using UnityEngine.UI;
using System.Net.Sockets;
using static TMPro.TMP_Dropdown;
using System.Runtime.Serialization;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System.Net;

public class Utils
{
    #region Einstellungsmenü
    #region Allgemeine Kategorie Einstellungen
    public enum EinstellungsKategorien
    {
        ContentObject,
        Audio,
        Grafik,
        Sonstiges,
        Server
    }
    /// <summary>
    /// Blendet beliebige Kategorien ein und aktualisiert deren Werte
    /// </summary>
    /// <param name="EinstellungsParent"></param>
    /// <param name="audiomixer"></param>
    /// <param name="kategorien"></param>
    public static void EinstellungenStartSzene(GameObject EinstellungsParent, AudioMixer audiomixer, params EinstellungsKategorien[] kategorien)
    {
        EinstellungenDeaktiviereAlle(EinstellungsParent);
        for (int i = 0; i < kategorien.Length; i++)
        {
            EinstellungenAktiviere(EinstellungsParent, kategorien[i], true);
        }

        EinstellungenGrafikUpdate(EinstellungsParent);
        EinstellungenAudioUpdateVolume(EinstellungsParent, audiomixer);
        EinstellungenVersionUpdate(EinstellungsParent);
    }
    /// <summary>
    /// Blendet beliebig viele Kategorien ein, die nicht genannten, werden ausgeblendet
    /// </summary>
    /// <param name="EinstellungsParent"></param>
    /// <param name="kategorien"></param>
    public static void EinstellungenToggle(GameObject EinstellungsParent, params EinstellungsKategorien[] kategorien)
    {
        EinstellungenDeaktiviereAlle(EinstellungsParent);
        for (int i = 0; i < kategorien.Length; i++)
        {
            EinstellungenAktiviere(EinstellungsParent, kategorien[i], true);
        }
    }
    /// <summary>
    /// Gibt mit dem Einstellungs Gameobject das jeweilig passende für die Kategorie zurück
    /// </summary>
    /// <param name="EinstellungsParent"></param>
    /// <param name="kategorie"></param>
    /// <returns></returns>
    private static GameObject EinstellungenGetKategorie(GameObject EinstellungsParent, EinstellungsKategorien kategorie)
    {
        Transform EinstellungsContent = EinstellungsParent.transform.GetChild(2).GetChild(3).GetChild(0).GetChild(0);
        switch (kategorie)
        {
            default:
                Logging.log(Logging.LogType.Error, "Utils_Einstellungen", "EinstellungsKategorie", "Unbekannte Kategorie.");
                return null;
            case EinstellungsKategorien.ContentObject:
                return EinstellungsContent.gameObject;
            case EinstellungsKategorien.Audio:
                return EinstellungsContent.GetChild(0).gameObject;
            case EinstellungsKategorien.Grafik:
                return EinstellungsContent.GetChild(1).gameObject;
            case EinstellungsKategorien.Sonstiges:
                return EinstellungsContent.GetChild(2).gameObject;
            case EinstellungsKategorien.Server:
                return EinstellungsContent.GetChild(3).gameObject;
        }
    }
    /// <summary>
    /// Deaktiviert alle Kategorien
    /// </summary>
    /// <param name="EinstellungsParent"></param>
    private static void EinstellungenDeaktiviereAlle(GameObject EinstellungsParent)
    {
        GameObject content = EinstellungenGetKategorie(EinstellungsParent, EinstellungsKategorien.ContentObject);
        for (int i = 0; i < content.transform.childCount; i++)
            content.transform.GetChild(i).gameObject.SetActive(false);
    }
    /// <summary>
    /// Aktiviert/Deaktiviert bestimmte Kategorien
    /// </summary>
    /// <param name="EinstellungsParent"></param>
    /// <param name="kategorie"></param>
    /// <param name="toggle"></param>
    public static void EinstellungenAktiviere(GameObject EinstellungsParent, EinstellungsKategorien kategorie, bool toggle)
    {
        EinstellungenGetKategorie(EinstellungsParent, kategorie).SetActive(toggle);
    }
    #endregion
    #region weitere Einstellungen
    /// <summary>
    /// Aktualisiert die Angezeigte Version
    /// </summary>
    /// <param name="EinstellungsParent"></param>
    public static void EinstellungenVersionUpdate(GameObject EinstellungsParent)
    {
        EinstellungsParent.transform.GetChild(2).GetChild(1).GetComponent<TMP_Text>().text = "v" + Config.APPLICATION_VERSION;
    }
    #endregion
    #region Audio Einstellungen
    /// <summary>
    /// Aktualisiert die Audiovolume Anzeige
    /// </summary>
    /// <param name="EinstellungsParent"></param>
    /// <param name="audiomixer"></param>
    public static void EinstellungenAudioUpdateVolume(GameObject EinstellungsParent, AudioMixer audiomixer)
    {
        if (Config.APPLICATION_CONFIG == null)
            return;

        Transform AudioContent = EinstellungenGetKategorie(EinstellungsParent, EinstellungsKategorien.Audio).transform;

        float master = Config.APPLICATION_CONFIG.GetFloat("GAME_MASTER_VOLUME", 0f);
        audiomixer.SetFloat("MASTER", master);
        AudioContent.GetChild(1).GetChild(1).GetComponent<Slider>().value = master / 10;
        AudioContent.GetChild(1).GetChild(1).GetChild(3).GetComponentInChildren<TMP_Text>().text = ((master * 3) + 100) + "%";

        float sfx = Config.APPLICATION_CONFIG.GetFloat("GAME_SFX_VOLUME", 0f);
        audiomixer.SetFloat("SFX", sfx);
        AudioContent.GetChild(2).GetChild(1).GetComponent<Slider>().value = sfx / 10;
        AudioContent.GetChild(2).GetChild(1).GetChild(3).GetComponentInChildren<TMP_Text>().text = ((sfx * 3) + 100) + "%";

        float bgm = Config.APPLICATION_CONFIG.GetFloat("GAME_BGM_VOLUME", 0f);
        audiomixer.SetFloat("BGM", bgm);
        AudioContent.GetChild(3).GetChild(1).GetComponent<Slider>().value = bgm / 10;
        AudioContent.GetChild(3).GetChild(1).GetChild(3).GetComponentInChildren<TMP_Text>().text = ((bgm * 3) + 100) + "%";
    }
    #endregion
    #region Server Einstellungen
    /// <summary>
    /// Aktualisiert die IP und Port Anzeige
    /// </summary>
    /// <param name="EinstellungsParent"></param>
    /// <param name="isServer"></param>
    /// <param name="ip"></param>
    /// <param name="port"></param>
    public static void EinstelungenServerUpdatePortIP(GameObject EinstellungsParent, bool isServer, string ip, string port)
    {
        Transform ServerContent = EinstellungenGetKategorie(EinstellungsParent, EinstellungsKategorien.Server).transform;

        ServerContent.GetChild(1).GetChild(1).GetComponent<Toggle>().isOn = isServer;
        ServerContent.GetChild(2).GetChild(1).GetChild(0).GetComponent<TMP_InputField>().text = ip;
        ServerContent.GetChild(3).GetChild(1).GetChild(0).GetComponent<TMP_InputField>().text = port;
    }
    /// <summary>
    /// Aktualisiert die NoIP Anzeige
    /// </summary>
    /// <param name="EinstellungsParent"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <param name="hostname"></param>
    public static void EinstellungenServerNoIpUpdate(GameObject EinstellungsParent, string username, string password, string hostname)
    {
        Transform ServerContent = EinstellungenGetKategorie(EinstellungsParent, EinstellungsKategorien.Server).transform;

        ServerContent.GetChild(5).GetChild(1).GetComponentInChildren<TMP_InputField>().text = username;
        ServerContent.GetChild(6).GetChild(1).GetComponentInChildren<TMP_InputField>().text = password;
        ServerContent.GetChild(7).GetChild(1).GetComponentInChildren<TMP_InputField>().text = hostname;
    }
    #endregion
    #region Grafik Einstellungen
    /// <summary>
    /// Aktualisiert die Anzeige für die Grafikeinstellungs
    /// </summary>
    /// <param name="EinstellungsParent"></param>
    public static void EinstellungenGrafikUpdate(GameObject EinstellungsParent)
    {
        if (Config.APPLICATION_CONFIG == null)
            return;

        Transform GrafikContent = EinstellungenGetKategorie(EinstellungsParent, EinstellungsKategorien.Grafik).transform;

        EinstellungsmenueUtils.blockDebugMode = true;
        TMP_Dropdown BildschirmVollbildDrop = GrafikContent.GetChild(4).GetChild(1).GetComponent<TMP_Dropdown>();
        List<DisplayInfo> BildschirmeListe = new List<DisplayInfo>();
        Screen.GetDisplayLayout(BildschirmeListe);
        List<OptionData> BildschirmeDropOptions = new List<OptionData>();
        foreach (var item in BildschirmeListe)
            BildschirmeDropOptions.Add(new OptionData(item.name + " - " + item.refreshRate.value + "Hz"));
        BildschirmVollbildDrop.options = BildschirmeDropOptions;
        string BildschirmWanted = Config.APPLICATION_CONFIG.GetString("GAME_DISPLAY_SELECTION", "*<#>*~ERROR~*<#>");

        for (int i = 0; i < BildschirmVollbildDrop.options.Count; i++)
            if (BildschirmWanted.Equals(BildschirmeListe[i].name))
            {
                BildschirmVollbildDrop.value = i;
                break;
            }
        EinstellungsmenueUtils.blockDebugMode = false;
        GrafikContent.GetChild(2).GetChild(1).GetComponent<TMP_Dropdown>().value = Config.APPLICATION_CONFIG.GetInt("GAME_DISPLAY_RESOLUTION", 2);
        GrafikContent.GetChild(3).GetChild(1).GetComponent<Toggle>().isOn = Config.APPLICATION_CONFIG.GetBool("GAME_DISPLAY_FULLSCREEN", true);
    }
    /// <summary>
    /// Wendet die Grafik Einstellungen an, mit der Bedingung ob vollbild pflicht ist
    /// </summary>
    public static void EinstellungenGrafikApply(bool forceFullscreen)
    {
        if (Config.APPLICATION_CONFIG == null)
            return;

        if (forceFullscreen == true)
        {
            if (Config.FULLSCREEN == true)
            {
                if (Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen)
                {
                    return;
                }
                else
                {
                    List<DisplayInfo> displays = new List<DisplayInfo>();
                    Screen.GetDisplayLayout(displays);
                    if (displays?.Count > 1) // don't bother running if only one display exists...
                    {
                        string wantedDisplay = Config.APPLICATION_CONFIG.GetString("GAME_DISPLAY_SELECTION", "*<#>*~ERROR~*<#>*|*").Replace(" - ", "|").Split('|')[0];
                        foreach (var item in displays)
                            if (wantedDisplay.Equals(item.name))
                            {
                                Screen.MoveMainWindowTo(item, new Vector2Int(item.width / 2, item.height / 2));
                                Screen.SetResolution(item.width, item.height, true);
                                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                                return;
                            }
                        Screen.MoveMainWindowTo(displays[0], new Vector2Int(displays[0].width / 2, displays[0].height / 2));
                    }

                    Screen.SetResolution(Display.displays[0].systemWidth, Display.displays[0].systemHeight, true);
                    Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                    return;
                }
            }
        }

        string[] ress = new string[] { "2560x1440", "1920x1080", "1280x720" };
        int width = Int32.Parse(ress[Config.APPLICATION_CONFIG.GetInt("GAME_DISPLAY_RESOLUTION", 2)].Split('x')[0]);
        int height = Int32.Parse(ress[Config.APPLICATION_CONFIG.GetInt("GAME_DISPLAY_RESOLUTION", 2)].Split('x')[1]);
        bool full = Config.APPLICATION_CONFIG.GetBool("GAME_DISPLAY_FULLSCREEN", true);

        if (full)
        {
            if (Screen.fullScreenMode != FullScreenMode.ExclusiveFullScreen)
            {
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                List<DisplayInfo> displays = new List<DisplayInfo>();
                Screen.GetDisplayLayout(displays);
                string wantedDisplay = Config.APPLICATION_CONFIG.GetString("GAME_DISPLAY_SELECTION", "*<#>*~ERROR~*<#>*|*").Replace(" - ", "|").Split('|')[0];
                foreach (var item in displays)
                    if (wantedDisplay.Equals(item.name))
                    {
                        Screen.MoveMainWindowTo(item, new Vector2Int(item.width / 2, item.height / 2));
                        Screen.SetResolution(item.width, item.height, true);
                        Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                        return;
                    }
                Screen.SetResolution(Display.displays[0].systemWidth, Display.displays[0].systemHeight, true);
            }
            else
            {
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                List<DisplayInfo> displays = new List<DisplayInfo>();
                Screen.GetDisplayLayout(displays);
                string wantedDisplay = Config.APPLICATION_CONFIG.GetString("GAME_DISPLAY_SELECTION", "*<#>*~ERROR~*<#>*|*").Replace(" - ", "|").Split('|')[0];
                foreach (var item in displays)
                    if (wantedDisplay.Equals(item.name))
                    {
                        Screen.MoveMainWindowTo(item, new Vector2Int(item.width / 2, item.height / 2));
                        Screen.SetResolution(item.width, item.height, true);
                        Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                        return;
                    }
            }
        }
        else
        {
            if (Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen)
                Screen.fullScreenMode = FullScreenMode.Windowed;
            Screen.SetResolution(width, height, false);
        }
    }
    #endregion
    #region Sonstige Einstellungen
    /// <summary>
    /// Aktualisiert die Anzeige für die Grafikeinstellungs
    /// </summary>
    /// <param name="EinstellungsParent"></param>
    public static void EinstellungenSonstigeUpdate(GameObject EinstellungsParent)
    {
        Transform GrafikContent = EinstellungenGetKategorie(EinstellungsParent, EinstellungsKategorien.Sonstiges).transform;

        EinstellungsmenueUtils.blockDebugMode = true;
        GrafikContent.GetChild(3).GetChild(1).GetComponent<Toggle>().isOn = Config.APPLICATION_CONFIG.GetBool("APPLICATION_DEBUGMODE", true);
        EinstellungsmenueUtils.blockDebugMode = false;
    }
    #endregion
    #endregion
    /// <summary>
    /// Generiert einen String mit allen Elementen der Liste
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    public static string ListToString(List<string> list)
    {
        string output = "";
        foreach (string item in list)
            output += "," + item;
        if (output.Length > 0)
            output = output.Substring(1);
        return "[" + output + "]";
    }
    /// <summary>
    /// Lässt nur Commands durch die sich im selben Game befinden.
    /// Oder mit Allgemeinem Title. Sonst zurück zur Lobby oder so
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static string ParseCMDGameTitle(string data, bool isServer)
    {
        if (data.StartsWith(Config.GAME_TITLE + "#"))
            data = data.Substring(Config.GAME_TITLE.Length);
        else if (data.StartsWith(Config.GLOBAL_TITLE + "#"))
            data = data.Substring(Config.GLOBAL_TITLE.Length);
        else
        {
            Logging.log(Logging.LogType.Warning, "Utils", "ParseCMDGameTitle", "Server: " + isServer + " Command format: " + data);
            /*TODO
            if (isServer)
                ServerUtils.BroadcastImmediate("");
            else
                ClientUtils.SendToServer("");*/
        }

        return data;
    }
    /// <summary>
    /// Initialisiert die Spiele mit FPS und Images
    /// </summary>
    /// <param name="background"></param>
    public static void LoadStartGameInitiations(Image background, bool checkInited = true)
    {
        if (checkInited)
        {
            if (!Config.CLIENT_STARTED && !Config.SERVER_STARTED)
            {
                SceneManager.LoadScene("Startup");
                return;
            }
            Application.targetFrameRate = 120;
#if UNITY_EDITOR
            Application.targetFrameRate = 200;
#endif
        }
        // Saisonale Backgrounds werden geladen
        foreach (var item in Config.SAISONAL_BACKGROUNDS)
        {
            if (item.IsNow(Config.GAME_TITLE))
                background.sprite = item.GetImage();
        }
        // Temporärer Background wird geladen
        if (Config.TEMP_BACKGROUND.IsNow(Config.GAME_TITLE))
            background.sprite = Config.TEMP_BACKGROUND.GetImage();
    }
    /// <summary>
    /// Lädt alle Saisonalen Hintergründe
    /// lade die bilder und entscheide anhand des Namens klasse_start_end
    /// </summary>
    public static void LoadSaisonalBackgrounds()
    {
        Logging.log(Logging.LogType.Normal, "Utils", "LoadSaisonalBackgrounds", "Bilder werden geladen.");
        if (Config.TEMP_BACKGROUND == null)
            Config.TEMP_BACKGROUND = new RemoteBackground();
        Config.SAISONAL_BACKGROUNDS = new List<RemoteBackground>();
        foreach (var item in Resources.LoadAll<Sprite>("Images/SaisonalBackgrounds/"))
        {
            try
            {
                string notiz = item.name.Split('_')[0];
                string classname = item.name.Split('_')[1];
                int start = int.Parse(item.name.Split('_')[2]);
                int end = int.Parse(item.name.Split('_')[3]);

                Config.SAISONAL_BACKGROUNDS.Add(new RemoteBackground(start, end, classname, item));
            }
            catch (Exception e)
            {
                Logging.log(Logging.LogType.Warning, "Utils", "LoadSaisonalBackgrounds", "Fehler beim Laden eines Images", e);
            }
        }
        Logging.log(Logging.LogType.Normal, "Utils", "LoadSaisonalBackgrounds", "Es wurde Bilder geladen: " + Config.SAISONAL_BACKGROUNDS.Count);
    }
}

public class ServerUtils
    {
        #region Kommunikation
        /// Liste mit zusendenden Nachrichten an die Clients
        public static List<string> broadcastmsgs;
        public static bool blockBroadcastMsgs;
        /// <summary>
        /// Fügt eine Nachricht auf die Broadcast liste hinzu
        /// </summary>
        /// <param name="msg"></param>
        public static void AddBroadcast(string msg)
        {
            if (msg.StartsWith(Config.GAME_TITLE))
                broadcastmsgs.Add(msg);
            else
                broadcastmsgs.Add(Config.GAME_TITLE + msg);
            if (broadcastmsgs.Count >= 10)
                Logging.log(Logging.LogType.Warning, "ServerUtils", "AddBroadcast", "Zu viele MSGs im Puffer: " + broadcastmsgs.Count + " \n" + Utils.ListToString(broadcastmsgs).Replace(",", "\n"));
        }
        /// <summary>
        /// Sendet eine Nachticht an alle verbundenen Spieler. (Config.PLAYLIST)
        /// </summary>
        /// <param name="data">Nachricht</param>
        public static IEnumerator Broadcast()
        {
            broadcastmsgs = new List<string>();
            while (true)
            {
                // Broadcastet alle MSGs nacheinander
                if (broadcastmsgs.Count != 0 && !blockBroadcastMsgs)
                {
                    blockBroadcastMsgs = true;
                    string msg = broadcastmsgs[0];
                    broadcastmsgs.RemoveAt(0);
                    BroadcastImmediate(msg);
                    blockBroadcastMsgs = false;
                }
                yield return new WaitForSeconds(0.01f);
            }
        }
        /// <summary>
        /// Sendet eine Nachticht an alle verbundenen Spieler. (Config.PLAYLIST)
        /// </summary>
        /// <param name="data">Nachricht</param>
        public static void BroadcastImmediate(string msg)
        {
            blockBroadcastMsgs = true;
            if (!msg.StartsWith(Config.GAME_TITLE) && !msg.StartsWith(Config.GLOBAL_TITLE))
                msg = Config.GAME_TITLE + msg;
            foreach (Player sc in Config.PLAYERLIST)
                if (sc.isConnected)
                    SendMSG(msg, sc);

            // Lädt zurück ins Hauptmenü
            if (msg.Equals(Config.GAME_TITLE + "#ZurueckInsHauptmenue") || msg.Equals(Config.GLOBAL_TITLE + "#ZurueckInsHauptmenue"))
                SceneManager.LoadScene("Startup"); //Async?
                                                   // Startet ein Spiel
            else if (msg.StartsWith(Config.GAME_TITLE + "#StarteSpiel "))
            {
                Config.GAME_TITLE = msg.Split(' ')[1];
                SceneManager.LoadScene(msg.Split(' ')[1]);
            }
            blockBroadcastMsgs = false;
        }
        /// <summary>
        /// Vergibt Kronen je nach Platzierung der Spieler
        /// </summary>
        public static void LoadKronen(Player[] playerlist)
        {
            Player p1 = new Player(-2);
            Player p2 = new Player(-3);
            Player p3 = new Player(-4);

            foreach (var item in playerlist)
            {
                if (item.points > p1.points)
                    p1 = item;
            }

            foreach (var item in playerlist)
            {
                if (item.points > p2.points && item.points <= p1.points && item != p1)
                    p2 = item;
            }

            foreach (var item in playerlist)
            {
                if (item.points > p3.points && item.points <= p2.points && item != p1 && item != p2)
                    p3 = item;
            }

            p1.crowns += 3;

            if (p2.points == p1.points)
                p2.crowns += 3;
            else
                p2.crowns += 2;

            if (p3.points == p1.points)
                p3.crowns += 3;
            else if (p3.points == p2.points)
                p3.crowns += 2;
            else
                p3.crowns += 1;

            foreach (var item in Config.PLAYERLIST)
                item.points = 0;
        }
        /// <summary>
        /// Sendet eine Nachricht an den angegebenen Spieler.
        /// </summary>
        /// <param name="data">Nachricht</param>
        /// <param name="sc">Spieler</param>
        private static void SendMSG(string data, Player sc)
        {
            try
            {
                StreamWriter writer = new StreamWriter(sc.tcp.GetStream());
                writer.WriteLine(data);
                writer.Flush();
            }
            catch (Exception e)
            {
                Logging.log(Logging.LogType.Warning, "ServerUtils", "SendMSG", "Nachricht an Client: " + sc.id + " (" + sc.name + ") konnte nicht gesendet werden.", e);
                // Verbindung zum Client wird getrennt
                ClientClosed(sc);
            }
        }
        /// <summary>
        /// Sendet eine Nachricht an den angegebenen Spieler und schreibt einen Prefix dafor
        /// </summary>
        /// <param name="data"></param>
        /// <param name="sc"></param>
        /// <param name="globalPrefix"></param>
        public static void SendMSG(string data, Player sc, bool globalPrefix)
        {
            if (globalPrefix)
                SendMSG(Config.GLOBAL_TITLE + data, sc);
            else
                SendMSG(Config.GAME_TITLE + data, sc);
        }
        #endregion
        /// <summary>
        /// Löscht Daten des Spielers von dem die Verbindung getrennt wurde
        /// </summary>
        /// <param name="player">Spieler</param>
        public static void ClientClosed(Player player)
        {
            player.icon2 = new PlayerIcon();
            player.name = "";
            player.points = 0;
            player.crowns = 0;
            player.isConnected = false;
            player.isDisconnected = false;
            Config.SERVER_ALL_CONNECTED = false;
        }
        /// <summary>
        /// Startet das empfangen von Nachrichten von Clients
        /// </summary>
        public static void startListening()
        {
            Config.SERVER_TCP.BeginAcceptTcpClient(AcceptTcpClient, Config.SERVER_TCP);
        }
        /// <summary>
        /// Fügt Client der Empfangsliste hinzu (Config.PLAYLIST)
        /// </summary>
        /// <param name="ar"></param>
        public static void AcceptTcpClient(IAsyncResult ar)
        {
            Logging.log(Logging.LogType.Debug, "ServerUtils", "AcceptTcpClient", "Ein neuer Spieler verbindet sich...");
            // Sucht freien Spieler Platz
            Player freierS = null;
            foreach (Player sp in Config.PLAYERLIST)
            {
                if (sp.isConnected == false && sp.isDisconnected == false)
                {
                    freierS = sp;
                    break;
                }
            }
            // Spieler sind voll
            if (freierS == null)
            {
                Logging.log(Logging.LogType.Warning, "ServerUtils", "AcceptTcpClient", "Server ist voll. Spieler wird abgelehnt.");
                Player temp = new Player(-1);
                temp.name = "full";
                TcpListener ll = (TcpListener)ar.AsyncState;
                temp.tcp = ll.EndAcceptTcpClient(ar);
                // Log ausgabe und Clientseite testen weil es nicht geht
                ServerUtils.SendMSG("#ServerFull", temp, true);
                startListening();
                return;
            }

            TcpListener listener = (TcpListener)ar.AsyncState;
            freierS.isConnected = true;
            freierS.tcp = listener.EndAcceptTcpClient(ar);
            StartupServer.connectedPlayer++;
            Logging.log(Logging.LogType.Debug, "ServerUtils", "AcceptTcpClient", "Ein neuer Spieler verbindet sich: " + freierS.id);
            // Prüft ob der Server voll ist
            bool tempAllConnected = true;
            for (int i = 0; i < Config.PLAYERLIST.Length; i++)
            {
                if (!Config.PLAYERLIST[i].isConnected)
                {
                    tempAllConnected = false;
                    break;
                }
            }

            Config.SERVER_ALL_CONNECTED = tempAllConnected;
            Logging.log(Logging.LogType.Debug, "ServerUtils", "AcceptTcpClient", "Server ist voll: " + Config.SERVER_ALL_CONNECTED);
            startListening();

            // Sendet neuem Spieler zugehörige ID
            ServerUtils.SendMSG("#SetID [ID]" + freierS.id + "[ID][GAMEFILES]" + StartupServer.UpdateClientGameVorschau, freierS, true);
            // Dazu die GameFiles
            Logging.log(Logging.LogType.Normal, "ServerUtils", "AcceptTcpClient", "Spieler: " + freierS.id + " ist jetzt verbunden. IP: " + freierS.tcp.Client.RemoteEndPoint);
        }
    }

public class ClientUtils
    {
        #region Kommunikation
        /// <summary>
        /// Sendet einen Befehl zum Server, falls die Verbindung fehlerhaft ist, dann wird diese getrennt.
        /// </summary>
        /// <param name="data"></param>
        public static void SendToServer(string data)
        {
            if (!Config.CLIENT_STARTED)
                return;

            data = Config.GAME_TITLE + data;

            try
            {
                NetworkStream stream = Config.CLIENT_TCP.GetStream();
                StreamWriter writer = new StreamWriter(stream);
                writer.WriteLine(data);
                writer.Flush();
            }
            catch (Exception e)
            {
                Logging.log(Logging.LogType.Warning, "ClientUtils", "SendToServer", "Nachricht an Server konnte nicht gesendet werden. Nachricht: " + data, e);
                Config.HAUPTMENUE_FEHLERMELDUNG = "Verbindung zum Server wurde verloren.";
                CloseSocket();
            }
        }
        #endregion
        /// <summary>
        /// Trennt die Verbindung zum Server
        /// </summary>
        public static void CloseSocket()
        {
            if (!Config.CLIENT_STARTED)
                return;
            Logging.log(Logging.LogType.Normal, "ClientUtils", "CloseSocket", "Verbindung zum Server wurde getrennt. Client wird in das Hauptmenü geladen.");
            try
            {
                Config.CLIENT_TCP.Close();
                Config.CLIENT_STARTED = false;
            }
            catch
            {
                Config.CLIENT_TCP = null;
            }
            SceneManager.LoadSceneAsync("Startup");
        }
    }

[DataContract]
public class RemoteBackgroundList
{
    [DataMember]
    private List<RemoteBackground> backgrounds;

    public RemoteBackgroundList()
    {
        backgrounds = new List<RemoteBackground>();
    }

    public List<RemoteBackground> GetList() { return backgrounds; }
    public RemoteBackground Get(string className)
    {
        foreach (var item in backgrounds)
        {
            if (item.IsNow(className))
                return item;
        }
        return null;
    }
}

[DataContract]
public class RemoteBackground
{
    [DataMember]
    private int start; // Monat Tag 1203
    [DataMember]
    private int end; // Monat Tag 1204
    [DataMember]
    private List<string> klassen;
    [DataMember]
    private string url;
    [IgnoreDataMember]
    private Sprite image;

    public RemoteBackground()
    {
        this.start = -1;
        this.end = -1;
        this.klassen = new List<string>();
        this.url = "";
        this.image = null;
    }
    public RemoteBackground(string content)
    {
        try
        {
            this.start = int.Parse(content.Split('|')[0]);
            this.end = int.Parse(content.Split('|')[1]);
            this.klassen = new List<string>();
            this.klassen.AddRange(content.Split('|')[2].Split('*'));
            this.url = content.Split('|')[3];
            if (this.url.Length == 0)
                this.start = -1;
        }
        catch (Exception e)
        {
            Logging.log(Logging.LogType.Warning, "Utils", "LoadTemporaryBackground", "Bild konnte nicht geladen werden.", e);
            this.start = -1;
        }
    }
    public RemoteBackground(int start, int end, string klasse, Sprite sprite)
    {
        this.start = start;
        this.end = end;
        this.klassen = new List<string>();
        this.url = "";
        this.image = sprite;
    }

    public int GetStart() { return start; }
    public int GetEnd() { return end; }
    public bool IsNow(string classname)
    {
        if (this.klassen.Count == 0 || (this.image == null && this.url.Length == 0))
            return false;
        if (!this.klassen.Contains(classname) && !this.klassen.Contains("ALL"))
            return false;

        int today = DateTime.Now.Month * 100 + DateTime.Now.Day;
        if ((today >= start && today <= end) || (start > end && (today >= start || today <= end)))
            return true;
        else
            return false;
    }
    public string GetUrl() { return this.url; }
    public List<string> GetClass() { return klassen; }
    public Sprite GetImage() { return image; }
    public void SetImage(Sprite sprite) { image = sprite; } 
    public void ClearImage()
    {
        this.start = -1;
        this.klassen = new List<string>();
        this.url = "";
        this.image = null;
    }
}
