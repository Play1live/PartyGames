using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartupScene : MonoBehaviour
{
    [SerializeField] GameObject Client;
    [SerializeField] GameObject Server;
    [SerializeField] GameObject Hauptmenue;
    [SerializeField] GameObject Lobby;
    [SerializeField] GameObject ServerControl;

    [SerializeField] GameObject LautstärkeEinstellung;
    [SerializeField] GameObject ServerEinstellungen;

    [SerializeField] GameObject ModeratedGamesSFX;
    [SerializeField] AudioMixer audiomixer;

    private bool UpdaterIsUpToDate = false;

    void Start()
    {
#if UNITY_EDITOR
        Config.isServer = true;
        Config.PLAYER_NAME = "Henryk";
#endif
        /*Testzwecke*/
        // Config.DEBUG_MODE = true;
        //Config.isServer = !Config.isServer;
        //Config.isServer = true;
        Debug.LogWarning(Config.SERVER_CONNECTION_IP);
        Config.GAME_TITLE = "Startup";
        if (!Config.APPLICATION_INIT)
        {
            Logging.log(Logging.LogType.Normal, "StartupScene", "Start", "Application Version: " + Config.APPLICATION_VERSION);
            Logging.log(Logging.LogType.Normal, "StartupScene", "Start", "Debugmode: " + Config.DEBUG_MODE);
            WriteGameVersionFile();
            LoadConfigs.FetchRemoteConfig();    // Lädt Config
            MedienUtil.CreateMediaDirectory();
            StartCoroutine(UpdateGameUpdater());

            // Lädt die applicationConfig
            Config.APPLICATION_CONFIG = new ConfigFile(Application.persistentDataPath + "/", "application.config");
            StartCoroutine(UpdateSoundsVolume());


            if (Config.isServer)
                StartCoroutine(LoadGameFilesAsync());
            StartCoroutine(EnableConnectionButton());

            // Init PlayerlistZeigt die geladenen Spiele in der GameÜbersicht an
            if (Config.PLAYERLIST == null)
            {
                InitPlayerlist();
                Hauptmenue.SetActive(true);
                Lobby.SetActive(false);
                ServerControl.SetActive(false);
            }
            if (Config.isServer)
                GameObject.Find("Version_LBL").gameObject.GetComponent<TMP_Text>().text = "Version: " + Config.APPLICATION_VERSION + "    Medien: " + Config.MedienPath;
            else
                GameObject.Find("Version_LBL").gameObject.GetComponent<TMP_Text>().text = "Version: " + Config.APPLICATION_VERSION;

            GameObject.Find("ApplicationEinstellungen/Hintergrund/Version").GetComponent<TMP_Text>().text = Config.APPLICATION_VERSION;
            Config.APPLICATION_INIT = true;
        }

        ServerEinstellungen.transform.GetChild(2).GetChild(1).GetChild(0).GetComponent<TMP_InputField>().text = Config.SERVER_CONNECTION_IP;
        ServerEinstellungen.transform.GetChild(3).GetChild(1).GetChild(0).GetComponent<TMP_InputField>().text = "" + Config.SERVER_CONNECTION_PORT;

        if (!Config.CLIENT_STARTED && !Config.SERVER_STARTED)
        {
            Client.SetActive(false);
            Server.SetActive(false);
            ServerEinstellungen.SetActive(true);
        }
        // Zeigt den temporären Spielernamen an
        if (Hauptmenue.activeInHierarchy)
            Hauptmenue.transform.GetChild(3).GetComponent<TMP_InputField>().text = Config.PLAYER_NAME;

        StartCoroutine(AutostartServer());
    }
        
    private void OnEnable()
    {
        Hauptmenue.SetActive(true);
        Lobby.SetActive(false);
        ServerControl.SetActive(false);

        if (Config.isServer)
        {
            Application.targetFrameRate = 120;
            if (Config.SERVER_STARTED)
                Server.SetActive(true);
        }
        else
        {
            Application.targetFrameRate = 60;
            if (Config.CLIENT_STARTED)
                Client.SetActive(true);
        }
    }

    void Update()
    {
        if (Hauptmenue.activeInHierarchy)
            GameObject.Find("ErrorMessage").gameObject.GetComponent<TMP_Text>().text = Config.HAUPTMENUE_FEHLERMELDUNG;
    }

    private void OnApplicationQuit()
    {
        Logging.log(Logging.LogType.Debug, "StartupScene", "OnApplicationQuit", "Spiel wird beendet.");
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        Config.APPLICATION_CONFIG.Save();
    }

    /// <summary>
    /// Updates SoundVolume (Master, SFX, BGM)
    /// </summary>
    IEnumerator UpdateSoundsVolume()
    {
        float master = Config.APPLICATION_CONFIG.GetFloat("GAME_MASTER_VOLUME", 0f);
        audiomixer.SetFloat("MASTER", master);
        LautstärkeEinstellung.transform.GetChild(1).GetChild(1).GetComponent<Slider>().value = master / 10;
        LautstärkeEinstellung.transform.GetChild(1).GetChild(1).GetChild(3).GetComponentInChildren<TMP_Text>().text = ((master * 3) + 100) + "%";

        float sfx = Config.APPLICATION_CONFIG.GetFloat("GAME_SFX_VOLUME", 0f);
        audiomixer.SetFloat("SFX", sfx);
        LautstärkeEinstellung.transform.GetChild(2).GetChild(1).GetComponent<Slider>().value = sfx / 10;
        LautstärkeEinstellung.transform.GetChild(2).GetChild(1).GetChild(3).GetComponentInChildren<TMP_Text>().text = ((sfx * 3) + 100) + "%";

        float bgm = Config.APPLICATION_CONFIG.GetFloat("GAME_BGM_VOLUME", 0f);
        audiomixer.SetFloat("BGM", bgm);
        LautstärkeEinstellung.transform.GetChild(3).GetChild(1).GetComponent<Slider>().value = bgm / 10;
        LautstärkeEinstellung.transform.GetChild(3).GetChild(1).GetChild(3).GetComponentInChildren<TMP_Text>().text = ((bgm * 3) + 100) + "%";
        yield return null;
    }
    /// <summary>
    /// Aktualisiert den Updater sofern dieser veraltet ist
    /// </summary>
    IEnumerator UpdateGameUpdater()
    {
        Config.HAUPTMENUE_FEHLERMELDUNG = "Initialisiere Spieldateien...";
        yield return new WaitForSeconds(3);
        yield return new WaitUntil(() => Config.SERVER_CONNECTION_IP != "localhost"); // RemoteConfig wurd geladen
#if UNITY_EDITOR
        UpdaterIsUpToDate = true;
        Config.HAUPTMENUE_FEHLERMELDUNG = "";
        yield break;
#endif
#pragma warning disable CS0162 // Unerreichbarer Code wurde entdeckt. (ist aber erreichbar)
        yield return null;
#pragma warning restore CS0162 // Unerreichbarer Code wurde entdeckt. (ist aber erreichbar)
        // Erstelle den Path zur Versionsdatei des Updaters
        Logging.log(Logging.LogType.Debug, "StartupScene", "UpdateGameUpdater", "Starte die Aktualisierung des Updaters.");
        string datapath = Application.dataPath;
        string GameFiles = datapath.Split('/')[datapath.Split('/').Length - 1];
        string UpdaterFiles = datapath.Split('/')[datapath.Split('/').Length - 2];
        string UpdaterVersionPath = datapath.Replace("/" + GameFiles, "").Replace("\\" + GameFiles, "").Replace("/" + UpdaterFiles, "").Replace("\\" + UpdaterFiles, "");
        Logging.log(Logging.LogType.Debug, "StartupScene", "UpdateGameUpdater", "Updaterversion File: " + UpdaterVersionPath + "/Version.txt");
        
        // Lösche alten UpdateZip
        if (File.Exists(UpdaterVersionPath + @"/Updater.zip"))
            File.Delete(UpdaterVersionPath + @"/Updater.zip");

        // Lade Version des Updaters
        string updaterVersion = "";
        if (File.Exists(UpdaterVersionPath + "/Version.txt"))
        {
            updaterVersion = File.ReadAllText(UpdaterVersionPath + "/Version.txt");
        }
        else
        {
            Logging.log(Logging.LogType.Error, "StartupScene", "UpdateGameUpdater", "Updaterversion konnte nicht gefunden werden. Path: " + UpdaterVersionPath + "/Version.txt");
            if (!File.Exists(UpdaterVersionPath + "/PartyGamesUpdater.exe"))
            {
                Config.HAUPTMENUE_FEHLERMELDUNG = "";
                UpdaterIsUpToDate = true;
                yield break;
            }
        }

        // Prüfe Versionen
        if (Config.UPDATER_LATEST_VERSION != updaterVersion)
        {
            Config.HAUPTMENUE_FEHLERMELDUNG = "Updater ist veraltet und wird nun aktualisiert...";
            Logging.log(Logging.LogType.Normal, "StartupScene", "UpdateGameUpdater", "Updater ist veraltet.");
            UnityWebRequest dlreq = new UnityWebRequest(Config.UPDATER_DOWNLOAD_URL);
            dlreq.downloadHandler = new DownloadHandlerFile(UpdaterVersionPath + @"/Updater.zip");
            UnityWebRequestAsyncOperation op = dlreq.SendWebRequest();
            while (!op.isDone)
            {
                yield return null;
            }
            if (dlreq.result.Equals(dlreq.error))
            {
                Config.HAUPTMENUE_FEHLERMELDUNG = "Updater konnte nicht aktualisiert werden, bitte starte das Spiel einmal neu.";
                Logging.log(Logging.LogType.Error, "StartupScene", "DownloadUpdater", "Updater konnte nicht heruntergeladen werden. " + dlreq.error);
                UpdaterIsUpToDate = true;
                yield break;
            }
            else
            {
                Config.HAUPTMENUE_FEHLERMELDUNG = "Updater ist veraltet und wird nun aktualisiert..";
                Logging.log(Logging.LogType.Normal, "StartupScene", "DownloadUpdater", "Updater wurde erfolgreich heruntergeladen.");
            }
            dlreq.Dispose();

            yield return new WaitForSeconds(0.2f);
            #region Unzip File
            Config.HAUPTMENUE_FEHLERMELDUNG = "Updater ist veraltet und wird nun aktualisiert.";
            Logging.log(Logging.LogType.Normal, "StartupScene", "DownloadUpdater", "Updaterdateien werden entpackt.");
            try
            {
                ZipFile.ExtractToDirectory(UpdaterVersionPath + @"/Updater.zip", UpdaterVersionPath, true);
            }
            catch (Exception e)
            {
                Config.HAUPTMENUE_FEHLERMELDUNG = "Updater konnte nicht aktualisiert werden!\n" +
                    "Bitte starte das Programm neu und versuche es erneut.\n\n" +
                    "Falls der Fehler bestehen bleibt, entpacke die \"Updater.zip\" im Ordner: \n\"" + UpdaterVersionPath + "\"";
                Logging.log(Logging.LogType.Warning, "StartupScene", "DownloadUpdater", "Updaterdateien konnten nicht entpackt werden.", e);
                yield break;
            }
            Logging.log(Logging.LogType.Normal, "StartupScene", "DownloadUpdater", "Updaterdateien wurden erfolgreich entpackt.");
            #endregion

            Config.HAUPTMENUE_FEHLERMELDUNG = "Updater wurde erfolgreich aktualisiert.";

            // Lösche alten Version File
            if (File.Exists(UpdaterVersionPath + "/Version.txt"))
                File.Delete(UpdaterVersionPath + "/Version.txt");

            // Lösche alten UpdateZip
            if (File.Exists(UpdaterVersionPath + @"/Updater.zip"))
                File.Delete(UpdaterVersionPath + @"/Updater.zip");

            UpdaterIsUpToDate = true;
            yield return null;
        }
        else
        {
            Config.HAUPTMENUE_FEHLERMELDUNG = "";
            Logging.log(Logging.LogType.Normal, "StartupScene", "UpdateGameUpdater", "Updater ist bereits aktuell.");
            UpdaterIsUpToDate = true;
            yield break;
        }
    }
    /// <summary>
    /// Aktiviert den Verbinden Button, nachdem die RemoteConfig erfolgreich geladen wurde, damit die Spieler nicht 
    /// </summary>
    /// <returns></returns>
    IEnumerator EnableConnectionButton()
    {
        Hauptmenue.transform.GetChild(4).gameObject.SetActive(false);
        yield return new WaitUntil(() => Config.SERVER_CONNECTION_IP != "localhost"); // RemoteConfig wurd geladen
        ServerEinstellungen.transform.GetChild(2).GetChild(1).GetChild(0).GetComponent<TMP_InputField>().text = Config.SERVER_CONNECTION_IP;
        ServerEinstellungen.transform.GetChild(3).GetChild(1).GetChild(0).GetComponent<TMP_InputField>().text = "" + Config.SERVER_CONNECTION_PORT;
        yield return new WaitUntil(() => UpdaterIsUpToDate == true); // Warte bis die Version des Updater aktualisiert wurde
        Logging.log(Logging.LogType.Debug, "StartupScene", "EnableConnectionButton", "Spieler darf sich nun verbinden.");
        Hauptmenue.transform.GetChild(4).gameObject.SetActive(true);
    }
    /// <summary>
    /// Startet den Server automatisch.
    /// Wenn die IP-Adresse nicht aktualisiert werden konnte, dann wird abgebrochen.
    /// </summary>
    IEnumerator AutostartServer()
    {
        if (Config.isServer && !Config.SERVER_STARTED)
        {
            // Updatet die IP-Adresse nachdem die RemoteConfig geladen wurde
            yield return new WaitUntil(() => Config.SERVER_CONNECTION_IP != "localhost");
            Hauptmenue.transform.GetChild(0).GetComponent<TMP_Text>().text = Config.SERVER_CONNECTION_IP + ":" + Config.SERVER_CONNECTION_PORT;
            bool updatedSuccessful = new UpdateIpAddress().UpdateNoIP_DNS();

            yield return new WaitForSeconds(1);

            // Startet den Server, wenn IP Update erfolgreich war
            if (updatedSuccessful)
                StartConnection();
        }
        yield break;
    }
    /// <summary>
    /// Lädt die vorbereiteten Spieldateien
    /// </summary>
    IEnumerator LoadGameFilesAsync()
    {
        SetupSpiele.LoadGameFiles();
        yield break;
    }
    /// <summary>
    /// Initialisiert die Config.PLAYERLIST
    /// </summary>
    private void InitPlayerlist()
    {
        Logging.log(Logging.LogType.Debug, "StartupScene", "Start", "Initialisiert Spieler & Icons");
        Config.PLAYERLIST = new Player[] { new Player(1), new Player(2), new Player(3), new Player(4), new Player(5), new Player(6), new Player(7), new Player(8) };
        Config.SERVER_MAX_CONNECTIONS = Config.PLAYERLIST.Length;
        Config.PLAYER_ICONS = new List<Sprite>();
        foreach (Sprite sprite in Resources.LoadAll<Sprite>("Images/ProfileIcons/"))
        {
            if (sprite.name.Equals("empty"))
                continue;
            Config.PLAYER_ICONS.Add(sprite);
        }
    }
    /// <summary>
    /// Begrenzt die Länge des Namens
    /// </summary>
    /// <param name="input">Eingabefeld für den Spielernamen</param>
    public void OnChangeName(TMP_InputField input) 
    {
        if (input.text.Length > Config.MAX_PLAYER_NAME_LENGTH)
            input.text = input.text.Substring(0, Config.MAX_PLAYER_NAME_LENGTH);

        if (input.text.Length < 3)
            Hauptmenue.transform.GetChild(4).GetComponent<Button>().interactable = false;
        else
            Hauptmenue.transform.GetChild(4).GetComponent<Button>().interactable = true;
    }
    /// <summary>
    /// Beendet das Spiel auf Button
    /// </summary>
    public void SpielBeenden()
    {
        Application.Quit();
    }
    /// <summary>
    /// Startet den Server/Client
    /// </summary>
    public void StartConnection()
    {
        if (Config.CLIENT_STARTED || Config.SERVER_STARTED)
            return;

        ServerEinstellungen.SetActive(false);

        Config.PLAYER_NAME = GameObject.Find("ChooseYourName_TXT").gameObject.GetComponent<TMP_InputField>().text;
        Config.APPLICATION_CONFIG.SetString("PLAYER_DISPLAY_NAME", Config.PLAYER_NAME);
        // Game is Player
        if (!Config.isServer)
        {
            // Initiate Gameobjects
            Config.HAUPTMENUE_FEHLERMELDUNG = "Verbindung zum Server wird aufgebaut...";
            Logging.log(Logging.LogType.Normal, "StartupScene", "StartConnection", "Verbindung zum Server wird aufgebaut...");
            Client.SetActive(true);
        }
        // Game is Server
        else
        {
            // Initiate Gameobjects
            Config.HAUPTMENUE_FEHLERMELDUNG = "Server wird gestartet...";
            Logging.log(Logging.LogType.Normal, "StartupScene", "StartConnection", "Server wird gestartet...");
            Server.SetActive(true);
        }
    }
    /// <summary>
    /// Speichert Spiel-Version in einer TXT-Datei
    /// </summary>
    private void WriteGameVersionFile()
    {
        if (!File.Exists(Application.dataPath.Replace("\\PartyGames_Data", "").Replace("/PartyGames_Data", "") + @"/Version.txt"))
        {
            using (FileStream fs = File.Create(Application.dataPath.Replace("\\PartyGames_Data", "").Replace("/PartyGames_Data", "") + @"/Version.txt"))
            {
                byte[] info = new UTF8Encoding(true).GetBytes(Config.APPLICATION_VERSION);
                // Add some information to the file.
                fs.Write(info, 0, info.Length);
            }
        }
        else
        {
            string version = File.ReadAllText(Application.dataPath.Replace("\\PartyGames_Data", "").Replace("/PartyGames_Data", "") + @"/Version.txt");
            if (version != Config.APPLICATION_VERSION)
            {
                File.Delete(Application.dataPath.Replace("\\PartyGames_Data", "").Replace("/PartyGames_Data", "") + @"/Version.txt");
                WriteGameVersionFile();
            }
        }
        Logging.log(Logging.LogType.Debug, "StartupScene", "WriteGameVersionFile", "Spiel Version wurde für den Updater aktualisiert.");
    }


    private void SettingsAktualisiereAnzeigen()
    {

    }
    public void SettingsChangeIP(TMP_InputField input)
    {
        if (Config.CLIENT_STARTED || Config.SERVER_STARTED)
            return;
        Config.SERVER_CONNECTION_IP = input.text;
    }
    public void SettingsChangePort(TMP_InputField input)
    {
        if (Config.CLIENT_STARTED || Config.SERVER_STARTED)
            return;
        Config.SERVER_CONNECTION_PORT = int.Parse(input.text);
    }
    public void SettingsChangeServerHost(Toggle toggle)
    {
        if (Config.CLIENT_STARTED || Config.SERVER_STARTED)
            return;
        Config.isServer = toggle.isOn;
    }
    public void SettingsChangeNoIPUsername(TMP_InputField input)
    {
        if (!File.Exists(Application.persistentDataPath + @"/No-IP Settings.txt"))
            return;

        string lines = "";
        foreach (string line in File.ReadAllLines(Application.persistentDataPath + @"/No-IP Settings.txt"))
        {
            if (line.StartsWith("No-IP_Benutzername: "))
                lines += "\n" + "No-IP_Benutzername: " + input.text;
            else
                lines += "\n" + line;
        }
        if (lines.Length > 3)
            lines = lines.Substring("\n".Length);
        File.WriteAllText(Application.persistentDataPath + @"/No-IP Settings.txt", lines);
    }
    public void SettingsChangeNoIPPassword(TMP_InputField input)
    {
        if (!File.Exists(Application.persistentDataPath + @"/No-IP Settings.txt"))
            return;

        string lines = "";
        foreach (string line in File.ReadAllLines(Application.persistentDataPath + @"/No-IP Settings.txt"))
        {
            if (line.StartsWith("No-IP_Passwort: "))
                lines += "\n" + "No-IP_Passwort: " + input.text;
            else
                lines += "\n" + line;
        }
        if (lines.Length > 3)
            lines = lines.Substring("\n".Length);
        File.WriteAllText(Application.persistentDataPath + @"/No-IP Settings.txt", lines);
    }
    public void SettingsChangeNoIPHostname(TMP_InputField input)
    {
        if (!File.Exists(Application.persistentDataPath + @"/No-IP Settings.txt"))
            return;

        string lines = "";
        foreach (string line in File.ReadAllLines(Application.persistentDataPath + @"/No-IP Settings.txt"))
        {
            if (line.StartsWith("No-IP_Hostname: "))
                lines += "\n" + "No-IP_Hostname: " + input.text;
            else
                lines += "\n" + line;
        }
        if (lines.Length > 3)
            lines = lines.Substring("\n".Length);
        File.WriteAllText(Application.persistentDataPath + @"/No-IP Settings.txt", lines);
    }
    public void StartCreationTerminal()
    {
        Logging.log(Logging.LogType.Normal, "StartupScene", "StartCreationTerminal", "Starte das ContentCreationTerminal");
        Config.GAME_TITLE = "ContentCreationTerminal";
        SceneManager.LoadScene("ContentCreationTerminal");
    }
}
