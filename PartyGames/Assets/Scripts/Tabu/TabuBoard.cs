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
    }
}

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
            string temp = item.Replace("#ue#", "ü").Replace("#oe#", "ö").Replace("#ae#", "ä").Replace("#ss#", "ß");
            temp = temp.Replace("#UE#", "Ü").Replace("#OE#", "Ö").Replace("#AE#", "Ä");
            try
            {
                items.Add(new TabuItem(temp.Split('|')[0], temp.Split('|')[1]));
            }
            catch (Exception e)
            {
                Logging.log(Logging.LogType.Warning, "TabuBoard", "TabuGamePacks", "Fehler beim einlesen. Titel: " + titel + " Zeile: " + temp, e);
            }
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

