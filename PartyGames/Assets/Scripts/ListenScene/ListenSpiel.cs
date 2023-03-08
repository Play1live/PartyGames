using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ListenSpiel
{
    private List<Listen> listen;
    private Listen selected;

    public ListenSpiel()
    {
        listen = new List<Listen>();

        foreach (string sfile in Directory.GetFiles(Config.MedienPath + "/Spiele/Listen"))
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

            // L�dt alle Flaggen und speichert diese ab
            listen.Add(new Listen(sfile));
        }
    }

    public List<Listen> getListen() { return listen; }
    public int getListenLength() { return listen.Count; }
    public int getIndex(Listen liste) { return listen.IndexOf(liste); }
    public int getIndex(string titel) { foreach (Listen liste in listen) if (liste.getTitel().ToLower().Equals(titel.ToLower())) return listen.IndexOf(liste); return -1; }
    public Listen getListe(int index) { return listen[index]; }
    public Listen getListe(string titel)
    {
        foreach (Listen liste in listen)
        {
            if (liste.getTitel().ToLower().Equals(titel.ToLower()))
                return liste;
        }
        return null;
    }
    public Listen getRandomListe() { return listen[Random.Range(0, getListenLength())]; }

    public void setSelected(Listen liste) { selected = liste; }
    public Listen getSelected() { return selected; }

    public List<string> getListenAsStringList()
    {
        List<string> listen = new List<string>();
        foreach (Listen liste in getListen())
        {
            listen.Add(liste.getTitel());
        }
        return listen;
    }
}
