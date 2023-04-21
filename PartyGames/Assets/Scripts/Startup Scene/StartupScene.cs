using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StartupScene : MonoBehaviour
{
    [SerializeField] GameObject Client;
    [SerializeField] GameObject Server;
    [SerializeField] GameObject Hauptmenue;
    [SerializeField] GameObject Lobby;
    [SerializeField] GameObject ServerControl;

    void Start()
    {
#if UNITY_EDITOR
        Config.isServer = true;
        Config.PLAYER_NAME = "Henryk";
#endif
            /*Testzwecke*/
        Config.DEBUG_MODE = true;
        //Config.isServer = !Config.isServer;
        //Config.isServer = true;


        Logging.log(Logging.LogType.Normal, "StartupScene", "Start", "Application Version: "+ Config.APPLICATION_VERSION);
        Logging.log(Logging.LogType.Normal, "StartupScene", "Start", "Debugmode: " + Config.DEBUG_MODE);
        WriteGameVersionFile();

        if (!Config.CLIENT_STARTED && !Config.SERVER_STARTED)
        {
            LoadConfigs.FetchRemoteConfig();    // Lädt Config
            MedienUtil.CreateMediaDirectory();
            StartCoroutine(LoadGameFilesAsync());
            Client.SetActive(false);
            Server.SetActive(false);
        }

        // Init PlayerlistZeigt die geladenen Spiele in der GameÜbersicht an
        if (Config.PLAYERLIST == null)
        {
            InitPlayerlist();
            Hauptmenue.SetActive(true);
            Lobby.SetActive(false);
            ServerControl.SetActive(false);
        }

        // Zeigt die Spielversion & den temporären Spielernamen an
        if (Hauptmenue.activeInHierarchy)
        {
            GameObject.Find("ChooseYourName_TXT").gameObject.GetComponent<TMP_InputField>().text = Config.PLAYER_NAME;
            if (Config.isServer)
                GameObject.Find("Version_LBL").gameObject.GetComponent<TMP_Text>().text = "Version: " + Config.APPLICATION_VERSION + "    Medien: " + Config.MedienPath;
            else
                GameObject.Find("Version_LBL").gameObject.GetComponent<TMP_Text>().text = "Version: " + Config.APPLICATION_VERSION;
        }

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
            GameObject.Find("Hauptmenue/IPandPort").GetComponent<TMP_Text>().text = Config.SERVER_CONNECTION_IP + ":" + Config.SERVER_CONNECTION_PORT;
            bool updatedSuccessful = new UpdateIpAddress().UpdateNoIP_DNS();

            yield return new WaitForSeconds(1);

            // Startet den Server, wenn IP Update erfolgreich war
            if (updatedSuccessful)
                StartConnection();
        }
        yield return null;
    }
    /// <summary>
    /// Lädt die vorbereiteten Spieldateien
    /// </summary>
    IEnumerator LoadGameFilesAsync()
    {
        SetupSpiele.LoadGameFiles();
        yield return null;
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
            GameObject.Find("ConnectToServer_BTN").GetComponent<Button>().interactable = false;
        else
            GameObject.Find("ConnectToServer_BTN").GetComponent<Button>().interactable = true;
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
        Config.PLAYER_NAME = GameObject.Find("ChooseYourName_TXT").gameObject.GetComponent<TMP_InputField>().text;
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
        Logging.log(Logging.LogType.Debug, "StartupScene", "WriteGameVersionFile", "Spiel Version wurde für den Updater aktualisiert.");
    }
}
