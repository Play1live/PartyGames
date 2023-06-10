using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Flagge
{
    private string path; // Nur mit Ländernamen
    private string name;
    private Sprite bild;
    private string[] farben;
    private string hauptstadt;
    private double flaeche; // in km²
    private int einwohner; // in einer

    public Flagge(Sprite sprite, bool loadInhalt)
    {
        Logging.log(Logging.LogType.Debug, "Flagge", "Flagge", "Lade Flagge: " + sprite.name + " Inhalt: " + loadInhalt);
        this.path = "";
        this.name = sprite.name.Replace("_Flagge", "");
        this.bild = sprite;

        if (loadInhalt == true)
        {
            string[] inhalt = Resources.Load<TextAsset>("Spiele/Flaggen/" + name.Replace("_Flagge", "")+ "_Inhalt").text.Replace("\n", "|").Split('|');
            this.farben = inhalt[0].Replace("Farben: ", "").Replace(", ", "#").Split('#');
            this.hauptstadt = inhalt[1].Replace("Hauptstadt: ", "");
            this.flaeche = Convert.ToDouble(inhalt[2].Substring(0, inhalt[2].Length - 4).Replace("Fläche: ", "").Replace(".", ""));
            this.einwohner = Int32.Parse(inhalt[3].Replace("Einwohnerzahl: ", "").Replace(".", ""));
        }
    }

    public string getPath() { return this.path; }
    public string getName() { return this.name; }
    public Sprite getBild() { return this.bild; }
    public string[] getFarben() { return this.farben; }
    public string getHauptstadt() { return this.hauptstadt; }
    public double getFlaeche() { return this.flaeche; }
    public int getEinwohner() { return this.einwohner; }

}
