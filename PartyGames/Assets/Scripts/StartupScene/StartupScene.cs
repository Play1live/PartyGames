using System.Collections;
using System.Collections.Generic;
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
 
    private void Awake()
    {
        LoadConfigs.FetchRemoteConfig();
        MedienUtil.CreateMediaDirectory();
        SetupSpiele.LoadGameFiles();
        Application.targetFrameRate = 60;
        Config.log = new List<Logging>();
        Client.SetActive(false);
        Server.SetActive(false);
    }

    void Start()
    {
#if UNITY_EDITOR
        Config.isServer = true;
        Debug.Log("DataPath: "+Config.MedienPath);
#endif
        /*Testzwecke*/// TODO
        Config.isServer = true;

        // Init Playerlist
        Config.PLAYERLIST = new Player[] { new Player(1), new Player(2), new Player(3), new Player(4), new Player(5), new Player(6), new Player(7), new Player(8) };
        // Init Playericons
        Config.PLAYER_ICONS = new List<Sprite>();
        foreach (Sprite sprite in Resources.LoadAll<Sprite>("Images/ProfileIcons/"))
        {
            Config.PLAYER_ICONS.Add(sprite);
        }

        // Gamebojects
        Hauptmenue.SetActive(true);
        if (Config.isServer)
        {
            GameObject.Find("Version_LBL").gameObject.GetComponent<TMP_Text>().text = "Version: " + Config.APPLICATION_VERSION + "    Medien: " + Config.MedienPath;
        }
        else
        {
            GameObject.Find("ChooseYourName_TXT").gameObject.GetComponent<TMP_InputField>().text = Config.PLAYER_NAME;
            GameObject.Find("Version_LBL").gameObject.GetComponent<TMP_Text>().text = "Version: " + Config.APPLICATION_VERSION;
        }
        GameObject.Find("ChooseYourName_TXT").gameObject.GetComponent<TMP_InputField>().text = Config.PLAYER_NAME;
        Lobby.SetActive(false);
        ServerControl.SetActive(false);

    }
    private void OnEnable()
    {
        // TODO: wenn man zurück ins Hauotmenü kommt
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
        Debug.Log("Quit");
    }

    // Begrenzt die Namenslänge
    public void OnChangeName(TMP_InputField input) 
    {
        if (input.text.Length > Config.MAX_PLAYER_NAME_LENGTH)
            input.text = input.text.Substring(0, Config.MAX_PLAYER_NAME_LENGTH);
    }

    // Startet den Client/Server
    public void StartConnection()
    {
        Config.PLAYER_NAME = GameObject.Find("ChooseYourName_TXT").gameObject.GetComponent<TMP_InputField>().text;
        // Game is Player
        if (!Config.isServer)
        {
            // Initiate Gameobjects
            Config.HAUPTMENUE_FEHLERMELDUNG = "Verbindung zum Server wird aufgebaut...";
            Client.SetActive(true);
        }
        // Game is Server
        else
        {
            // Initiate Gameobjects
            Config.HAUPTMENUE_FEHLERMELDUNG = "Server wird gestartet...";
            Server.SetActive(true);
        }
    }

}
