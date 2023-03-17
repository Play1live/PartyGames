using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Auktion
{
    private string titel;
    private List<Sprite> sprites;

    public Auktion(int index)
    {
        titel = index+" - "+ Resources.Load<TextAsset>("Spiele/Mosaik/"+index+"/#Titel").text;
        sprites = new List<Sprite>();

        foreach (Sprite sp in Resources.LoadAll<Sprite>("Spiele/Mosaik/"+index))
        {
            if (sprites.Count < 20)
            {
                sprites.Add(sp);
            }
            else
                return;
        }
    }

    public string getTitel() { return this.titel; }

    public List<Sprite> getSprites() { return sprites; }

}