using System;
using System.Collections.Generic;
using UnityEngine;

public class Player
{
    public Guid uuid;
    public string name;
    public int icon_id;
    public Sprite icon;
    public bool isModerator;

    public Player(Guid uuid, string name, int icon_id)
    {
        this.uuid = uuid;
        this.name = name;
        this.icon_id = icon_id;
        SetIcon(icon_id);
        this.isModerator = false;
    }

    public override string ToString()
    {
        return name + "#" + icon_id;
    }

    public void SetIcon(int icon_id)
    {
        this.icon_id = icon_id;
        this.icon = Config.player_icons[icon_id];
    }

    public static Player getPlayerById(Guid id)
    {
        foreach (var player in Config.players)
            if (player.uuid == id)
                return player;
        return null;
    }
    public static Player getPlayerById(Guid id, List<Player> players)
    {
        foreach (var player in players)
            if (player.uuid == id)
                return player;
        return null;
    }
    public static Player getPlayerByName(string name)
    {
        foreach (var player in Config.players)
            if (player.name == name)
                return player;
        return null;
    }
    public static Player getPlayerByName(string name, List<Player> players)
    {
        foreach (var player in players)
            if (player.name == name)
                return player;
        return null;
    }
}
