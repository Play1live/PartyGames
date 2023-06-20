using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class FlaggenSpiel
{
    public static int minPlayer = 3;
    public static int maxPlayer = 9;
    private List<Flagge> flaggen;
    private Sprite fragezeichen;
    public FlaggenSpiel()
    {
        Logging.log(Logging.LogType.Debug, "FlaggenSpiel", "FlaggenSpiel", "Lade Spiele");
        flaggen = new List<Flagge>();
        // Speichert Fragezeichenflagge
        fragezeichen = Resources.Load<Sprite>("Spiele/Flaggen/#Fragezeichen");

        foreach (Sprite sprite in Resources.LoadAll<Sprite>("Spiele/Flaggen"))
        {
            // Ignoriert Dateien die mit "#" beginnen
            if (sprite.name == "#Fragezeichen")
                continue;
            // Lädt alle Flaggen und speichert diese ab
            //flaggen.Add(new Flagge(sprite, Config.isServer));
            flaggen.Add(new Flagge(sprite, true));
        }
    }

    public int getMinPlayer() { return minPlayer; }
    public int getMaxPlayer() { return maxPlayer; }
    public List<Flagge> getFlaggen() { return flaggen; }
    public int getFlaggenLength() { return flaggen.Count; }
    public int getIndex(Flagge flagge) { return flaggen.IndexOf(flagge); }
    public int getIndex(string name) { foreach (Flagge flagge in flaggen) if (flagge.getName().Equals(name)) return flaggen.IndexOf(flagge); return -1; }

    public Flagge getFlagge(int index) { return flaggen[index]; }

    public Flagge getFlagge(string laendername) {
        foreach (Flagge flagge in flaggen)
        {
            if (flagge.getName().Equals(laendername))
                return flagge;
        }
        return null;
    }

    public Flagge getRandomFlagge() { return flaggen[Random.Range(0, getFlaggenLength())]; }

    public Sprite getFragezeichenFlagge() { return fragezeichen; }


}
