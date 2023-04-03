using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Auktion
{
    private string path;
    private string titel;
    private List<AuktionElement> elemente;

    public Auktion(string path)
    {
        this.path = path;
        string temp = path.Split('\\')[path.Split('\\').Length - 1];
        this.titel = temp.Split('/')[temp.Split('/').Length - 1].Replace(".txt", "");
        this.elemente = new List<AuktionElement>();

        foreach (string s in LadeDateien.listInhalt(path))
        {
            try
            {
                // Elemente
                if (s.StartsWith("- "))
                {
                    string[] tmp = s.Substring("- ".Length).Replace(" <!#!#!> ", "|").Split('|');
                    string name = tmp[0];
                    float preis = float.Parse(tmp[1]);
                    string url = tmp[2];
                    Sprite[] bilder = new Sprite[5]; // TODO: Die bilder direkt beim reinladen laden und dort durch Coroutines, wenns gar nicht geht, dann wie mosaik
                                                     // probeweise den link von Mosaik nehmen von github, da gehts mit coroutines
                    string[] bilderURL = new string[5];
                    bilderURL[0] = tmp[3];
                    bilderURL[1] = tmp[4];
                    bilderURL[2] = tmp[5];
                    bilderURL[3] = tmp[6];
                    bilderURL[4] = tmp[7];
                    elemente.Add(new AuktionElement(name, preis, url, bilder, bilderURL));
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Fehler beim Laden von Auktion: " + titel + " ->  " + s);
            }
        }
    }


    private void LoadImages(Sprite destination, string url)
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Auktion loading Image: " + www.error);
        }
        else
        {
            Texture2D myTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;
            Sprite sprite = Sprite.Create(myTexture, new Rect(0, 0, myTexture.width, myTexture.height), new Vector2(0.5f, 0.5f));
            destination = sprite;
        }
    }

    public string getTitel() { return this.titel; }
    public List<AuktionElement> getElemente() { return elemente; }
}

public class AuktionElement
{
    private string name;
    private float preis;
    private string url;
    private Sprite[] bilder;
    private string[] bilderURL;
    private bool wurdeverkauft;
    private float verkaufspreis;
    private int kaeuferId;

    public AuktionElement(string name, float preis, string url, Sprite[] bilder, string[] bilderurl)
    {
        this.name = name;
        this.preis = preis;
        this.url = url;
        this.bilder = bilder;
        this.bilderURL = bilderurl;
        this.wurdeverkauft = false;
        this.verkaufspreis = 0.0f;
        this.kaeuferId = -1;
    }

    public string getName() { return this.name; }
    public float getPreis() { return this.preis; }
    public void setPreis(float preis) { this.preis = preis; }
    public string getURL() { return this.url; }
    public Sprite[] getBilder() { return this.bilder; }
    public string[] getBilderURL() { return this.bilderURL; }
    public bool getWurdeverkauft() { return this.wurdeverkauft; }
    public void setWurdeverkauft(bool verkauft) { this.wurdeverkauft = verkauft; } 
    public float getVerkaufspreis() { return this.verkaufspreis; }
    public void setVerkaufspreis(float t) { this.verkaufspreis = t; }
    public int getKaueferId() { return this.kaeuferId; }
    public void setKaueferId(int id) { this.kaeuferId = id; }
}