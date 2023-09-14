using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class JeopardySpiel
{
    public static int minPlayer = 3;
    public static int maxPlayer = 9;
    public static string path = "/Spiele/Jeopardy";
    private List<Jeopardy> jeopardy;
    private Jeopardy selected;
    private Sprite beispiel;

    public JeopardySpiel()
    {
        Logging.log(Logging.LogType.Debug, "JeopardySpiel", "JeopardySpiel", "Lade Spiele.");
        jeopardy = new List<Jeopardy>();
        beispiel = Resources.Load<Sprite>("Spiele/Jeopardy/Beispiel");

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
            jeopardy.Add(new Jeopardy(sfile));
        }

        if (jeopardy.Count > 0)
            setSelected(jeopardy[0]);
    }

    public int getMinPlayer() { return minPlayer; }
    public int getMaxPlayer() { return maxPlayer; }
    public List<Jeopardy> getJeopardy() { return jeopardy; }
    public int getIndex(Jeopardy jeopardy) { return this.jeopardy.IndexOf(jeopardy); }
    public int getIndex(string titel) { foreach (Jeopardy jeopardy in jeopardy) if (jeopardy.getTitel().ToLower().Equals(titel.ToLower())) return this.jeopardy.IndexOf(jeopardy); return -1; }
    public Jeopardy getJeopardy(int index) { return jeopardy[index]; }
    public Jeopardy getJeopardy(string titel)
    {
        foreach (Jeopardy jeopardy in jeopardy)
        {
            if (jeopardy.getTitel().ToLower().Equals(titel.ToLower()))
                return jeopardy;
        }
        return null;
    }
    public void setSelected(Jeopardy jeopardy) { selected = jeopardy; }
    public Jeopardy getSelected() { return selected; }
    public Sprite getBeispiel() { return beispiel; }

    public List<string> getGamesAsStringList()
    {
        List<string> list = new List<string>();
        foreach (Jeopardy jeopardy in jeopardy)
        {
            if (jeopardy.getThemen().Count > 0)
                list.Add(jeopardy.getTitel());
        }
        return list;
    }
}
