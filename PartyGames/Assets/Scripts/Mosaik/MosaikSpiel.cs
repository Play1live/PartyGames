using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MosaikSpiel
{
    public static int minPlayer = 3;
    public static int maxPlayer = 9;
    public static string path = "/Spiele/Mosaik";
    private List<Mosaik> mosaike;
    private Mosaik selected;
    private Sprite beispiel;

    public MosaikSpiel()
    {
        Logging.log(Logging.LogType.Debug, "MosaikSpiel", "MosaikSpiel", "Lade Spiele.");
        mosaike = new List<Mosaik>();
        beispiel = Resources.Load<Sprite>("Spiele/Mosaik/Beispiel");

        foreach (string sfile in Directory.GetFiles(Config.MedienPath + path))
        {
            // Ignoriert #Vorlage.txt
            if (sfile.EndsWith("#Vorlage.txt"))
                continue;
            // Ignoriert Unity Meta Dateien
            if (sfile.EndsWith(".meta"))
                continue;
            // Ignoriert gespielte Spiele
            string tmp = sfile.Split('/')[sfile.Split('/').Length - 1];
            tmp = tmp.Split('\\')[tmp.Split('\\').Length - 1];
            if (tmp.StartsWith("#"))
                continue;

            // Lädt alle Flaggen und speichert diese ab
            mosaike.Add(new Mosaik(sfile));
        }

        if (mosaike.Count > 0)
            setSelected(mosaike[0]);
    }

    public int getMinPlayer() { return minPlayer; }
    public int getMaxPlayer() { return maxPlayer; }
    public List<Mosaik> getMosaike() { return mosaike; }
    public int getIndex(Mosaik mosaik) { return mosaike.IndexOf(mosaik); }
    public int getIndex(string titel) { foreach (Mosaik mosaik in mosaike) if (mosaik.getTitel().ToLower().Equals(titel.ToLower())) return mosaike.IndexOf(mosaik); return -1; }
    public Mosaik getMosaik(int index) { return mosaike[index]; }
    public Mosaik getMosaik(string titel)
    {
        foreach (Mosaik mosaik in mosaike)
        {
            if (mosaik.getTitel().ToLower().Equals(titel.ToLower()))
                return mosaik;
        }
        return null;
    }
    public void setSelected(Mosaik mosaik) { selected = mosaik; }
    public Mosaik getSelected() { return selected; }
    public Sprite getBeispiel() { return beispiel; }

    public List<string> getGamesAsStringList()
    {
        List<string> list = new List<string>();
        foreach (Mosaik mosaik in mosaike)
        {
            if (mosaik.getSprites().Count > 0)
                list.Add(mosaik.getTitel());
        }
        return list;
    }
}
