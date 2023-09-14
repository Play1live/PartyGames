using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Jeopardy
{
    private string titel;
    private List<JeopardyThema> themen;

    public Jeopardy(string path)
    {
        Logging.log(Logging.LogType.Debug, "Jeopardy", "Jeopardy", "Lade Jeopardyspiel: " + path);
        string temp = path.Split('\\')[path.Split('\\').Length - 1];
        this.titel = temp.Split('/')[temp.Split('/').Length - 1].Replace(".txt", "");
        this.themen = new List<JeopardyThema>();
        foreach (string zeile in LadeDateien.listInhalt(path))
        {
            this.themen.Add(new JeopardyThema(zeile));
        }
    }

    public string getTitel() { return this.titel; }
    public List<JeopardyThema> getThemen() { return this.themen; }
}

public class JeopardyThema
{
    public string thema;
    public List<JeopardyItem> items;

    public JeopardyThema(string line)
    {
        this.thema = line.Split('|')[0];
        this.items = new List<JeopardyItem>();
        for (int i = 1; i < line.Split('|').Length; i++)
            this.items.Add(new JeopardyItem(line.Split('|')[i], this));
    }
}
public class JeopardyItem
{
    public JeopardyThema thema;
    public int points;
    public string frage;
    public string antwort;
    public string imageurl;

    public JeopardyItem(string line, JeopardyThema thema)
    {
        this.thema = thema;
        this.points = Int32.Parse(line.Split('~')[0]);
        this.frage = line.Split('~')[1];
        this.antwort = line.Split('~')[2];
        this.imageurl = line.Split('~')[3];
    }
}