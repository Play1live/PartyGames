using System;
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
    public PlayerIcon icon2;
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
        this.icon2 = new PlayerIcon();
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
    
    /*public static Sprite getSpriteByPlayerName(string name)
    {
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            if (Config.PLAYERLIST[i].name == name)
            {
                return Config.PLAYERLIST[i].icon2.icon;
            }
        }
        return Resources.Load<Sprite>("Images/ProfileIcons/empty");
    }*/
    public static PlayerIcon getPlayerIconByPlayerName(string name)
    {
        for (int i = 0; i < Config.PLAYERLIST.Length; i++)
        {
            if (Config.PLAYERLIST[i].name == name)
            {
                return Config.PLAYERLIST[i].icon2;
            }
        }
        return new PlayerIcon();
    }
    public static PlayerIcon getPlayerIconById(string id)
    {
        try
        {
            return Config.PLAYER_ICONS[Int32.Parse(id)];
        }
        catch
        {
            return new PlayerIcon();
        }
    }
}

public class PlayerIcon
{
    public int id;
    public Sprite icon;
    public string displayname;
    public List<string> names;

    public PlayerIcon(int id, Sprite icon)
    {
        this.id = id;
        this.icon = icon;
        this.names = new List<string>();
        this.names.AddRange(icon.name.Split('_'));
        this.displayname = this.names[0];
    }
    public PlayerIcon()
    {
        this.id = -1;
        this.icon = Resources.Load<Sprite>("Images/ProfileIcons/empty");
        this.names = new List<string>();
        this.names.Add("empty");
        this.displayname = this.names[0];
    }

    public static int getIdByName(string name)
    {
        foreach (var item in Config.PLAYER_ICONS)
        {
            if (item.icon.name == name)
                return item.id;
        }
        return 0;
    }
    public static PlayerIcon getIconById(int id)
    {
        foreach (var item in Config.PLAYER_ICONS)
        {
            if (item.id == id)
                return item;
        }
        return new PlayerIcon();
    }
    public static PlayerIcon getIconById(string id)
    {
        foreach (var item in Config.PLAYER_ICONS)
        {
            if (id.Equals(item.id+""))
                return item;
        }
        return new PlayerIcon();
    }
    public static PlayerIcon getIconByDisplayName(string name)
    {
        foreach (var item in Config.PLAYER_ICONS)
        {
            if (item.displayname == name)
                return item;
        }
        return new PlayerIcon();
    }
}
