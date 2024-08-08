using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = System.Random;

public class Lobby : MonoBehaviour
{
    private bool lockcmds;
    [SerializeField] Transform lobby_grid;
    [SerializeField] GameObject moderator_menue;
    DateTime pingtime;

    [SerializeField] GameObject Gameselection_Item_Text;
    [SerializeField] GameObject Gameselection_Item_Select_1;
    [SerializeField] GameObject Gameselection_Item_Select_2;
    [SerializeField] GameObject Gameselection_Item_Select_1_Upload;
    [SerializeField] GameObject Gameselection_Item_Upload;

    [SerializeField] AudioSource ConnectSound;
    [SerializeField] AudioSource DisconnectSound;

    // Start is called before the first frame update
    void Start()
    {
        Utils.Log("Starting Lobby");
        lockcmds = false;
        FindFittingIconByName();
        UpdateModeratorView();
        StartCoroutine(SendPingUpdate());
    }

    // Update is called once per frame
    void Update()
    {
        // Verarbeite alle Nachrichten, die seit dem letzten Frame empfangen wurden
        while (Config.msg_queue.Count > 0)
        {
            if (lockcmds)
                return;
            string message = null;
            lock (Config.msg_queue)
            {
                message = Config.msg_queue.Dequeue();
            }
            Utils.Log(message);
            OnCommand(message);
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
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
            case "SpielerUpdate": UpdateSpieler(data); break;
            case "ClientSetModerator": Utils.Log("Du bist nun Moderator"); Config.spieler.isModerator = true; UpdateModeratorView(); break;
            case "ClientUnSetModerator": Utils.Log("Du bist nun kein Moderator mehr"); Config.spieler.isModerator = false; UpdateModeratorView(); break;
            case "PlayConnectSound": ConnectSound.Play(); break;
            case "PlayDisconnectSound": DisconnectSound.Play(); break;
            case "SetPingTime": ClientUtils.SendMessage("Lobby", "PingTime", ""+ (DateTime.Now-pingtime).TotalMilliseconds); pingtime = DateTime.MinValue; break;
            case "PingUpdate": UpdatePingInfo(data); break;
            case "SetSpielData": UpdateGameselection(data); break;
            case "StartGame": lockcmds = true; SceneManager.LoadScene(data); break;
        }
    }

    private IEnumerator SendPingUpdate()
    {
        while (true)
        {
            pingtime = DateTime.Now;
            ClientUtils.SendMessage("Lobby", "GetPingTime", "");
            yield return new WaitUntil(() => pingtime == DateTime.MinValue);
            yield return new WaitForSeconds(10);
            yield return new WaitForSeconds(new Random().Next(0, 5));
        }
        yield break;
    }
    private void UpdateSpieler(string data)
    {
        string[] infos = data.Split('*');
        Config.players = new List<Player>();
        for (int i = 0; i < infos.Length; i++)
        {
            Player p = new Player(Guid.Parse(infos[i].Split('#')[0]), infos[i].Split('#')[1], int.Parse(infos[i].Split('#')[2]));
            Config.players.Add(p);
            if (p.uuid == Config.spieler.uuid)
            {
                bool is_moderator = Config.spieler.isModerator;
                Config.spieler = p;
                Config.spieler.isModerator = is_moderator;
            }
            lobby_grid.GetChild(i).gameObject.SetActive(true);
            //lobby_grid.GetChild(i).GetChild(1).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/Ping/Ping 3"); // Ping
            lobby_grid.GetChild(i).GetChild(2).GetComponent<Image>().sprite = p.icon; // Icon
            lobby_grid.GetChild(i).GetChild(3).GetComponent<TMP_Text>().text = p.name; // Name
        }
        for (int i = infos.Length; i < lobby_grid.childCount; i++)
        {
            lobby_grid.GetChild(i).gameObject.SetActive(false);
        }
        GameObject.Find("Title_LBL/Spieleranzahl").GetComponent<TMP_Text>().text = infos.Length + "/" + lobby_grid.childCount;
    }
    private void UpdatePingInfo(string data)
    {
        Player p = Player.getPlayerByName(data.Split('#')[0]);
        if (p == null)
        {
            Utils.Log("Spieler wurde nicht gefunden!");
            return;
        }
        float ms = float.Parse(data.Split('#')[1]);
        int pid = Config.players.IndexOf(p);

        if (ms < 15)
            lobby_grid.GetChild(pid).GetChild(1).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/Ping/Ping 3");
        else if (ms < 30)
            lobby_grid.GetChild(pid).GetChild(1).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/Ping/Ping 2");
        else if (ms < 75)
            lobby_grid.GetChild(pid).GetChild(1).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/Ping/Ping 1");
        else
            lobby_grid.GetChild(pid).GetChild(1).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/Ping/Ping 0");
    }
    private void FindFittingIconByName()
    {
        // Abbruch falls bereits verbunden
        if (Config.connected)
            return;
        Config.connected = true;
        ConnectSound.Play();

        int iconindex = 0;
        for (int i = 0; i < Config.player_icons.Count; i++)
        {
            foreach (var item in Config.player_icons[i].name.Split('_'))
            {
                if (Config.spieler.name.ToLower().Contains(item.ToLower()))
                {
                    iconindex = i;
                    break;
                }
            }
        }
        iconindex = iconindex % Config.player_icons.Count;
        string liste_aller_icons = string.Join(",", Enumerable.Range(0, Config.player_icons.Count));
        ClientUtils.SendMessage("Lobby", "SetIcon", iconindex + "#" + liste_aller_icons);
    }
    public void ChangeIcon()
    {
        int iconindex = (Config.spieler.icon_id + 1) % Config.player_icons.Count;
        string liste_aller_icons = string.Join(",", Enumerable.Range(0, Config.player_icons.Count));
        ClientUtils.SendMessage("Lobby", "SetIcon", iconindex + "#" + liste_aller_icons);
    }
    #region Moderator
    public void UpdateModeratorView()
    {
        GameObject.Find("Moderator/Menue")?.SetActive(false);
        GameObject.Find("Moderator")?.SetActive(false);
        if (!Config.spieler.isModerator)
        {
            moderator_menue.SetActive(false);
            return;
        }
        moderator_menue.SetActive(true);
    }
    public void ChangeModerator(TMP_InputField input)
    {
        if (!Config.spieler.isModerator)
        {
            moderator_menue.SetActive(false);
            return;
        }
        ClientUtils.SendMessage("Lobby", "ChangeModerator", input.text);
        input.text = "";
    }
    public void GetGameselectionUpdate()
    {
        ClientUtils.SendMessage("Lobby", "GetSpielData", "");
    }
    private void UpdateGameselection(string data)
    {
        for (int i = 0; i < Gameselection_Item_Text.transform.parent.childCount; i++)
        {
            Transform child = Gameselection_Item_Text.transform.parent.GetChild(i);
            if (child.gameObject.activeInHierarchy)
            {
                Destroy(child.gameObject);
                i--; // Reduziere i, um sicherzustellen, dass kein Kind übersprungen wird
            }
        }

        List<string> gamedata = data.Split('~').ToList();
        Transform prefab_parent = GameObject.Find("Gameselection/Viewport/Content").transform;
        foreach (string item in gamedata)
        {
            switch (item.Split("[TYPE]")[1])
            {
                default: Utils.Log("Lobby - UpdateGameselection Unbekannter Typ: " + item); break;
                case "Text":
                    GameObject instance = Instantiate(Gameselection_Item_Text, prefab_parent);
                    instance.transform.GetChild(1).GetComponent<TMP_Text>().text = item.Split("[TITLE]")[1];
                    instance.name = item.Split("[TITLE]")[1] + "*" + Gameselection_Item_Text.name;
                    instance.SetActive(true);
                    break;
                case "Select_1":
                    instance = Instantiate(Gameselection_Item_Select_1, prefab_parent);
                    instance.transform.GetChild(1).GetComponentInChildren<TMP_Text>().text = item.Split("[SPIELER_ANZ]")[1];
                    instance.transform.GetChild(2).GetComponent<TMP_Text>().text = item.Split("[TITLE]")[1];
                    instance.name = item.Split("[TITLE]")[1] + "*" + Gameselection_Item_Select_1.name;
                    TMP_Dropdown drop = instance.transform.GetChild(4).GetComponent<TMP_Dropdown>();
                    drop.ClearOptions();
                    foreach (var option in item.Split("[SELECTION_1]")[1].Split("[TRENNER]").ToList())
                        drop.options.Add(new TMP_Dropdown.OptionData(option));
                    drop.value = 0;
                    instance.SetActive(true);
                    break;
                case "Select_2":
                    instance = Instantiate(Gameselection_Item_Select_2, prefab_parent);
                    instance.transform.GetChild(1).GetComponentInChildren<TMP_Text>().text = item.Split("[SPIELER_ANZ]")[1];
                    instance.transform.GetChild(2).GetComponent<TMP_Text>().text = item.Split("[TITLE]")[1];
                    instance.name = item.Split("[TITLE]")[1] + "*" + Gameselection_Item_Select_2.name;
                    drop = instance.transform.GetChild(4).GetComponent<TMP_Dropdown>();
                    drop.ClearOptions();
                    foreach (var option in item.Split("[SELECTION_1]")[1].Split("[TRENNER]").ToList())
                        drop.options.Add(new TMP_Dropdown.OptionData(option));
                    drop.value = 0;
                    drop = instance.transform.GetChild(5).GetComponent<TMP_Dropdown>();
                    drop.ClearOptions();
                    foreach (var option in item.Split("[SELECTION_2]")[1].Split("[TRENNER]").ToList())
                        drop.options.Add(new TMP_Dropdown.OptionData(option));
                    drop.value = 0;
                    instance.SetActive(true);
                    break;
                case "Select_1,Upload":
                    instance = Instantiate(Gameselection_Item_Select_1_Upload, prefab_parent);
                    instance.transform.GetChild(1).GetComponentInChildren<TMP_Text>().text = item.Split("[SPIELER_ANZ]")[1];
                    instance.transform.GetChild(2).GetComponent<TMP_Text>().text = item.Split("[TITLE]")[1];
                    instance.name = item.Split("[TITLE]")[1] + "*" + Gameselection_Item_Select_1_Upload.name;
                    drop = instance.transform.GetChild(4).GetComponent<TMP_Dropdown>();
                    drop.ClearOptions();
                    foreach (var option in item.Split("[SELECTION_1]")[1].Split("[TRENNER]").ToList())
                        drop.options.Add(new TMP_Dropdown.OptionData(option));
                    drop.value = 0;
                    instance.SetActive(true);
                    break;
                case "Upload":
                    instance = Instantiate(Gameselection_Item_Upload, prefab_parent);
                    instance.transform.GetChild(1).GetComponentInChildren<TMP_Text>().text = item.Split("[SPIELER_ANZ]")[1];
                    instance.transform.GetChild(2).GetComponent<TMP_Text>().text = item.Split("[TITLE]")[1];
                    instance.name = item.Split("[TITLE]")[1] + "*" + Gameselection_Item_Upload.name;
                    instance.SetActive(true);
                    break;
            }
        }
    }
    public void Gameselection_Handler(GameObject item)
    {
        if (item.name.Equals("Starten"))
        {
            string type = item.transform.parent.name;
            Transform parent = item.transform.parent;
            string name = parent.GetChild(2).GetComponent<TMP_Text>().text;
            if (type.Equals(name + "*Gameselection_Item_Select_1"))
            {
                TMP_Dropdown drop_1 = parent.GetChild(4).GetComponent<TMP_Dropdown>();
                ClientUtils.SendMessage("Lobby", "StartGame", name + "#" + drop_1.value);
            }
            else if (type.Equals(name + "*Gameselection_Item_Select_2"))
            {
                TMP_Dropdown drop_1 = parent.GetChild(4).GetComponent<TMP_Dropdown>();
                TMP_Dropdown drop_2 = parent.GetChild(5).GetComponent<TMP_Dropdown>();
                ClientUtils.SendMessage("Lobby", "StartGame", name + "#" + drop_1.value + "#" + drop_2.value);
            }
            else if (type.Equals(name + "*Gameselection_Item_Select_1,Upload"))
            {
                TMP_Dropdown drop_1 = parent.GetChild(4).GetComponent<TMP_Dropdown>();
                ClientUtils.SendMessage("Lobby", "StartGame", name + "#" + drop_1.value);
            }
            else
                Utils.Log("Lobby - Gameselection_Handler > Unbekannter Typ: " + item.name + " " + item.transform.parent.name);
        }
        else if (item.name.Equals("Upload"))
        {
            Utils.Log("Lobby - Gameselection_Handler > noch nicht implementiert: Upload");
        }
        else
            Utils.Log("Lobby - Gameselection_Handler > Unbekannter Knopf: " + item.name);
    }
    #endregion
}
