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
    public int points;

    public Player(int id)
    {
        this.id = id;
        this.isConnected = false;
        this.isDisconnected = false;
        this.tcp = null;
        this.name = "";
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
}
