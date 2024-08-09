using System.Collections;
using UnityEngine;
using NativeWebSocket;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;

public class Startup : MonoBehaviour
{
    private bool lockcmds;
    [SerializeField] Button ConnectBTN;
    private TMP_InputField inputtest;

    [SerializeField] AudioSource DisconnectSound;

    void Start()
    {
        ConnectBTN.interactable = true;
        if (Config.connected)
            DisconnectSound.Play();
        Config.connected = false;
        lockcmds = false;
        Config.uuid = System.Guid.NewGuid();
        Config.client = null;
        Config.spieler = null;
        Config.players = null;
        Utils.LoadPlayerIcons();
        ClientUtils.SetupConnection();
        inputtest = GameObject.Find("NameInput").GetComponent<TMP_InputField>();
    }

    void Update()
    {
        /// Verarbeite alle Nachrichten, die seit dem letzten Frame empfangen wurden
        while (Config.msg_queue.Count > 0)
        {
            if (lockcmds)
                return;
            string message = null;
            lock (Config.msg_queue)
            {
                message = Config.msg_queue.Dequeue();
            }
            OnCommand(message);
        }
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (ConnectBTN.gameObject.activeInHierarchy)
                ConnectToServer(inputtest);
        }
    }

    public void OnCommand(string message)
    {
        if (message.Split('|').Length < 3)
            return;
        string gametitle = message.Split('|')[0];
        string cmd = message.Split('|')[1];
        string data = message.Split('|')[2];

        switch (cmd)
        {
            default: Utils.Log("Unbekannter Befehl: " + cmd + " " + data); return;
            case "ClientSetName":
                lockcmds = true;
                Config.spieler = new Player(Guid.Parse(data.Split('#')[0]), data.Split('#')[1], int.Parse(data.Split('#')[2]));
                Config.players = new List<Player>();
                SceneManager.LoadScene("Lobby");
                break;
            case "HideCommunication": HideCommunication(data); break;
        }
    }

    public void CheckNameInput(TMP_InputField input)
    {
        if (input.text.Length <= 3)
            ConnectBTN.gameObject.SetActive(false);
        else if (input.text.Length > 14)
            input.text = input.text.Substring(0, 14);
        if (input.text.Length > 3 && input.text.Length <= 14)
            ConnectBTN.gameObject.SetActive(true);
    }

    public void ConnectToServer(TMP_InputField input)
    {
        // Verbinde zum WebSocket-Server
        Config.client.Connect();
        Config.client.OnOpen += IdentifyWithServer;
        ConnectBTN.interactable = false;
    }
    private void IdentifyWithServer()
    {
        Config.client.OnOpen -= IdentifyWithServer;
        TMP_InputField input = GameObject.Find("NameInput")?.GetComponent<TMP_InputField>();
        ClientUtils.SendMessage("Lobby", "ClientSetName", Config.uuid.ToString() + "#" + input.text);
    }
    private void HideCommunication(string data)
    {
        Config.hide_communication = bool.Parse(data);
    }
}
