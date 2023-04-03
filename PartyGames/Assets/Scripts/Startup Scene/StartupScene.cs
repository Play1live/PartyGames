using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using Unity.RemoteConfig;
using UnityEngine;

public class StartupScene : MonoBehaviour
{
    [SerializeField] GameObject Client;
    [SerializeField] GameObject Server;
    [SerializeField] GameObject Hauptmenue;
    [SerializeField] GameObject Lobby;
    [SerializeField] GameObject ServerControl;
 
    void Start()
    {
        // Speichert Version in TXT Datei
        if (!File.Exists(Application.dataPath.Replace("\\PartyGames_Data", "").Replace("/PartyGames_Data", "") + @"/Version.txt"))
        {
            using (FileStream fs = File.Create(Application.dataPath.Replace("\\PartyGames_Data", "").Replace("/PartyGames_Data", "") + @"/Version.txt"))
            {
                byte[] info = new UTF8Encoding(true).GetBytes(Config.APPLICATION_VERSION);
                // Add some information to the file.
                fs.Write(info, 0, info.Length);
            }
        }

        Application.targetFrameRate = 60;
#if UNITY_EDITOR
        Config.isServer = true;
        Debug.Log("DataPath: " + Config.MedienPath);
        Application.targetFrameRate = 120;
#endif
        /*Testzwecke*/// TODO
                       //Config.isServer = !Config.isServer;

        //Config.isServer = false;

        if (!Config.CLIENT_STARTED && !Config.SERVER_STARTED)
        {
            Config.log = new List<Logging>();
            LoadConfigs.FetchRemoteConfig();    // Lädt Config
            MedienUtil.CreateMediaDirectory();
            StartCoroutine(LoadGameFilesAsync());
            Client.SetActive(false);
            Server.SetActive(false);
        }

        // Init Playerlist
        if (Config.PLAYERLIST == null)
        {
            Config.PLAYERLIST = new Player[] { new Player(1), new Player(2), new Player(3), new Player(4), new Player(5), new Player(6), new Player(7), new Player(8) };
            // Init Playericons
            Config.PLAYER_ICONS = new List<Sprite>();
            foreach (Sprite sprite in Resources.LoadAll<Sprite>("Images/ProfileIcons/"))
            {
                if (sprite.name.Equals("empty"))
                    continue;
                Config.PLAYER_ICONS.Add(sprite);
            }
            Hauptmenue.SetActive(true); 
            Lobby.SetActive(false);
            ServerControl.SetActive(false);
        }

        if (Config.isServer)
        {
            if (Hauptmenue.activeInHierarchy)
            {
                GameObject.Find("Version_LBL").gameObject.GetComponent<TMP_Text>().text = "Version: " + Config.APPLICATION_VERSION + "    Medien: " + Config.MedienPath;
                GameObject.Find("ChooseYourName_TXT").gameObject.GetComponent<TMP_InputField>().text = Config.PLAYER_NAME;
            }
        }
        else
        {
            if (Hauptmenue.activeInHierarchy)
            {
                GameObject.Find("ChooseYourName_TXT").gameObject.GetComponent<TMP_InputField>().text = Config.PLAYER_NAME;
                GameObject.Find("Version_LBL").gameObject.GetComponent<TMP_Text>().text = "Version: " + Config.APPLICATION_VERSION;
            }
        }
    }
    IEnumerator LoadGameFilesAsync()
    {
        SetupSpiele.LoadGameFiles();
        yield return null;
    }
    private void OnEnable()
    {
        Hauptmenue.SetActive(true);
        Lobby.SetActive(false);
        ServerControl.SetActive(false);

        if (Config.isServer)
        {
            if (Config.SERVER_STARTED)
            {
                Server.SetActive(true);
                return;
            }
        }
        else
        {
            if (Config.CLIENT_STARTED)
            {
                Client.SetActive(true);
                return;
            }
        }
    }

    void Update()
    {
        if (Hauptmenue.activeInHierarchy)
        {
            GameObject.Find("Hauptmenue/IPandPort").GetComponent<TMP_Text>().text = Config.SERVER_CONNECTION_IP + ":" + Config.SERVER_CONNECTION_PORT;
            GameObject.Find("ConnectingToServer_LBL").gameObject.GetComponent<TMP_Text>().text = Config.HAUPTMENUE_FEHLERMELDUNG;
        }
    }

    private void OnApplicationQuit()
    {
        //Logging.add(Logging.Type.Normal, "StartupScene", "OnApplicationQuit", "Programm wird beendet");
        //MedienUtil.WriteLogsInDirectory();
    }

    /**
     * Begrenzt die Länge des Namens
     */
    public void OnChangeName(TMP_InputField input) 
    {
        if (input.text.Length > Config.MAX_PLAYER_NAME_LENGTH)
            input.text = input.text.Substring(0, Config.MAX_PLAYER_NAME_LENGTH);
    }

    /**
     *  Beendet das Spiel auf Button
     */
    public void SpielBeenden()
    {
        Application.Quit();
    }

    /**
     * Startet den Server/Client
     */
    public void StartConnection()
    {
        Config.PLAYER_NAME = GameObject.Find("ChooseYourName_TXT").gameObject.GetComponent<TMP_InputField>().text;
        // Game is Player
        if (!Config.isServer)
        {
            // Initiate Gameobjects
            Config.HAUPTMENUE_FEHLERMELDUNG = "Verbindung zum Server wird aufgebaut...";
            Logging.add(Logging.Type.Normal, "StartupScene", "StartConnection", "Verbindung zum Server wird aufgebaut...");
            Client.SetActive(true);
        }
        // Game is Server
        else
        {
            // Initiate Gameobjects
            Config.HAUPTMENUE_FEHLERMELDUNG = "Server wird gestartet...";
            Logging.add(Logging.Type.Normal, "StartupScene", "StartConnection", "Server wird gestartet...");
            Server.SetActive(true);
        }
    }
}
