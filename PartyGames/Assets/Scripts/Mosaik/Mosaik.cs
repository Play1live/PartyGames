using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Mosaik
{
    private string titel;
    private List<Sprite> sprites;
    private List<string> names;
    private List<string> urls;
    private List<bool> istGeladen;

    public Mosaik(string path)
    {
        Logging.log(Logging.LogType.Debug, "Mosaik", "Mosaik", "Lade Mosaikspiel: " + path);
        string temp = path.Split('\\')[path.Split('\\').Length - 1];
        this.titel = temp.Split('/')[temp.Split('/').Length - 1].Replace(".txt", "");
        sprites = new List<Sprite>();
        names = new List<string>();
        urls = new List<string>();
        istGeladen = new List<bool>();

        foreach (string zeile in LadeDateien.listInhalt(path))
        {
            
            if (zeile.StartsWith("- "))
            {
                string name = zeile.Substring("- ".Length).Replace(" [!#!] ", "|").Split('|')[0];
                string url = zeile.Substring("- ".Length).Replace(" [!#!] ", "|").Split('|')[1];

                sprites.Add(null);
                names.Add(name);
                urls.Add(url);
                istGeladen.Add(false);
            }
            else
            {
                Logging.log(Logging.LogType.Warning, "Mosaik", "Mosaik", "Unbekanntes Objekt gefunden: " + titel + "  -  " + zeile);
            }
        }
    }

    public string getTitel() { return this.titel; }
    public List<Sprite> getSprites() { return sprites; }
    public List<string> getNames() { return names; }
    public List<string> getURLs() { return urls; }
    public List<bool> getIstGeladen() { return istGeladen; }

}