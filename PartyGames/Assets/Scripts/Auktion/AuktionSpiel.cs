using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AuktionSpiel
{
    public static string path = "/Spiele/Auktion";
    public static int minPlayer = 3;
    public static int maxPlayer = 9;
    private List<Auktion> elemente;
    private Auktion selected;

    public AuktionSpiel()
    {
        Logging.log(Logging.LogType.Debug, "AuktionSpiel", "AuktionSpiel", "Lade Spieldateien");
        elemente = new List<Auktion>();

        foreach (string sfile in Directory.GetFiles(Config.MedienPath + "/Spiele/Auktion"))
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
            elemente.Add(new Auktion(sfile));
        }

        if (elemente.Count > 0)
            setSelected(elemente[0]);
    }

    public int getMinPlayer() { return minPlayer; }
    public int getMaxPlayer() { return maxPlayer; }
    public List<Auktion> getAuktionen() { return elemente; }
    public int getIndex(Auktion auktion) { return elemente.IndexOf(auktion); }
    public Auktion getAuktion(int index) { return elemente[index]; }
    public Auktion getAuktion(string titel)
    {
        foreach (Auktion element in elemente)
        {
            if (element.getTitel().ToLower().Equals(titel.ToLower()))
                return element;
        }
        return null;
    }
    public void setSelected(Auktion element) { selected = element; }
    public Auktion getSelected() { return selected; }
    public List<string> getGamesAsStringList()
    {
        List<string> list = new List<string>();
        foreach (Auktion element in elemente)
        {
            list.Add(element.getTitel());
        }
        return list;
    }
}
