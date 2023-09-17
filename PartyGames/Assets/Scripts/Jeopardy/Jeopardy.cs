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
        for (int i = this.themen.Count; i < 6; i++)
            this.themen.Add(new JeopardyThema());
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
        for (int i = this.items.Count; i < 5; i++)
            this.items.Add(new JeopardyItem(this));
    }
    public JeopardyThema()
    {
        this.thema = "";
        this.items = new List<JeopardyItem>();
        for (int i = this.items.Count; i < 5; i++)
            this.items.Add(new JeopardyItem(this));
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
        try
        {
            this.thema = thema;
            if (line.Split('~')[0].Length != 0)
                this.points = Int32.Parse(line.Split('~')[0]);
            else
                this.points = 0;
            this.frage = line.Split('~')[1];
            this.antwort = line.Split('~')[2];
            if (!(line.Split('~').Length < 4))
                this.imageurl = line.Split('~')[3];
            else
                this.imageurl = "";
        }
        catch
        {
            Logging.log(Logging.LogType.Error, "Jeopardy", "JeopardyItem", "Item konnte nicht geladen werden: " + line);
        }        
    }
    public JeopardyItem(JeopardyThema thema)
    {
        this.thema = thema;
        this.points = 0;
        this.frage = "";
        this.antwort = "";
        this.imageurl = "";
    }
}