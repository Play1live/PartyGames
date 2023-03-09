using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Mosaik
{
    private string titel;
    private List<Sprite> sprites;

    public Mosaik(int index)
    {
        titel = index+" - "+ Resources.Load<TextAsset>("Spiele/Mosaik/"+index+"/#Titel").text;
        sprites = new List<Sprite>();

        foreach (Sprite sp in Resources.LoadAll<Sprite>("Spiele/Mosaik/"+index))
        {
            sprites.Add(sp);
        }
    }

    public string getTitel() { return this.titel; }

    public List<Sprite> getSprites() { return sprites; }

}