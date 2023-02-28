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
        Config.log = new List<Logging>();
    }

    void Start()
    {
#if UNITY_EDITOR
        Config.isServer = true;
#endif
        /*Testzwecke*/
        Config.isServer = false;

        // Init Playerlist
        Config.PLAYERLIST = new Player[] { new Player(1), new Player(2), new Player(3) };

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

    void Update()
    {
        
    }

    private void OnApplicationQuit()
    {
        Debug.Log("Quit");
    }

    // Startet den Client/Server
    public void StartConnection()
    {
        // Game is Player
        if (!Config.isServer)
        {
            // Initiate Gameobjects
            GameObject.Find("ConnectingToServer_LBL").gameObject.GetComponent<TMP_Text>().text = "Verbindung zum Server wird aufgebaut...";
            Client.SetActive(true);
        }
        // Game is Server
        else
        {
            // Initiate Gameobjects
            GameObject.Find("ConnectingToServer_LBL").gameObject.GetComponent<TMP_Text>().text = "Server wird gestartet...";
            Config.SERVER_DISPLAY_NAME = GameObject.Find("ChooseYourName_TXT").gameObject.GetComponent<TMP_InputField>().text;
            Server.SetActive(true);
        }
    }

}
