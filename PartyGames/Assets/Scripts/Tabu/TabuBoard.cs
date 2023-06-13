using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.IO;

public class TabuBoard
{
    // Consts
    public static int minPlayer = 4;
    public static int maxPlayer = 8;


    public TabuBoard()
    {
        // TODO: gamepacks einlesen
    }
}

// TODO: auswahl in der Lobby oder vor erstem Rundenstart?
public class TabuGamePacks
{
    public string titel;
    public List<TabuItem> items;

    public TabuGamePacks(string titel, string path)
    {
        this.titel = titel;
        this.items = new List<TabuItem>();

        foreach (string item in Resources.Load<TextAsset>(path).text.Split('\n'))
        {
            items.Add(new TabuItem(item.Split('|')[0], item.Split('|')[1]));
        }
    }

    public TabuItem GetRandomItem()
    {
        if (items.Count == 0)
            return new TabuItem("Keine weiteren Worte", "");
        TabuItem item = items[UnityEngine.Random.Range(0, items.Count)];
        items.Remove(item);
        return item;
    }
}

public class TabuItem
{
    public string geheimwort;
    public string verboteneWorte;

    public TabuItem(string geheimwort, string verboteneWorte)
    {
        this.geheimwort = geheimwort;
        this.verboteneWorte = verboteneWorte.Replace("-", "\\n");
    }
}