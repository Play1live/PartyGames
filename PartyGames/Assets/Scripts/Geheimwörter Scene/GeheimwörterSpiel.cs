
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GeheimwörterSpiel
{
    private List<Geheimwörter> geheimwoerter;
    private Geheimwörter selected;

    public GeheimwörterSpiel()
    {
        geheimwoerter = new List<Geheimwörter>();

        foreach (string sfile in Directory.GetFiles(Config.MedienPath + "/Spiele/Geheimwörter"))
        {
            // Ignoriert #Vorlage.txt
            if (sfile.EndsWith("#Vorlage.txt"))
                continue;
            // Ignoriert Unity Meta Dateien
            if (sfile.EndsWith(".meta"))
                continue;


            // Ignoriert gespielte Spiele
            string tmp = sfile.Split('/')[sfile.Split('/').Length-1];
            tmp = tmp.Split('\\')[tmp.Split('\\').Length - 1];
            if (tmp.StartsWith("#"))
                continue;

            // Lädt alle Flaggen und speichert diese ab
            geheimwoerter.Add(new Geheimwörter(sfile));
        }
    }

    public List<Geheimwörter> getListen() { return this.geheimwoerter; }
    public int getIndex(Geheimwörter wort) { return geheimwoerter.IndexOf(wort); }
    public Geheimwörter getListe(int index) { return geheimwoerter[index]; }
    public Geheimwörter getRandomListe() { return geheimwoerter[Random.Range(0, getListen().Count)]; }
    public Geheimwörter getSelected() { return selected; }
    public void setSelected(Geheimwörter geheimwoerter) { selected = geheimwoerter; }

    public List<string> getListenAsStringList()
    {
        List<string> s = new List<string>();
        foreach (Geheimwörter ge in geheimwoerter)
        {
            s.Add(ge.getTitel());
        }
        return s;
    }
}
