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

    public Mosaik(int index)
    {
        titel = index+" - "+ Resources.Load<TextAsset>("Spiele/Mosaik/"+index+"/#Titel").text;
        sprites = new List<Sprite>();
        names = new List<string>();
        urls = new List<string>();
        istGeladen = new List<bool>();

        foreach (Sprite sp in Resources.LoadAll<Sprite>("Spiele/Mosaik/"+index))
        {
            if (sprites.Count < 20)
            {
                sprites.Add(sp);
                names.Add(sp.name);
                urls.Add("");
                istGeladen.Add(true);
            }
            else
                return;
        }
    }

    public string getTitel() { return this.titel; }

    public List<Sprite> getSprites() { return sprites; }
    public List<string> getNames() { return names; }
    public List<string> getURLs() { return urls; }
    public List<bool> getIstGeladen() { return istGeladen; }

}