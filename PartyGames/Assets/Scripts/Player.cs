using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

public class Player
{
    public int id;
    public bool isConnected;
    public bool isDisconnected;
    public TcpClient tcp;
    public string name;
    public Sprite icon;
    public int crowns;
    public int points;

    public Player(int id)
    {
        this.id = id;
        this.isConnected = false;
        this.isDisconnected = false;
        this.tcp = null;
        this.name = "";
        this.crowns = 0;
        this.points = 0;
        this.icon = Resources.Load<Sprite>("Images/ProfileIcons/empty");
    }

    public static int getPosInLists(int id)
    {
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            if (Config.PLAYERLIST[i].id == id)
            {
                return i;
            }
        }
        return -1;
    }

    public static int getIdByName(string name)
    {
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            if (Config.PLAYERLIST[i].name == name)
            {
                return Config.PLAYERLIST[i].id;
            }
        }
        return -1;
    }

    public static int getPosByName(string name)
    {
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            if (Config.PLAYERLIST[i].name == name)
            {
                return i;
            }
        }
        return -1;
    }
}
