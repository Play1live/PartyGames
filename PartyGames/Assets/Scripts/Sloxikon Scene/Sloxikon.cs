using System.Collections.Generic;
using UnityEngine;

public class Sloxikon
{
    private string path;
    private string titel;
    private List<string> themen;
    private List<string> antwort;

    public Sloxikon(string path)
    {
        Logging.log(Logging.LogType.Debug, "Sloxikon", "Sloxikon", "Lade Datei: " + path);
        this.path = path;
        string temp = path.Split('\\')[path.Split('\\').Length - 1];
        this.titel = temp.Split('/')[temp.Split('/').Length - 1].Replace(".txt", "");
        themen = new List<string>();
        antwort = new List<string>();

        foreach (string zeile in LadeDateien.listInhalt(path))
        {
            if (zeile.StartsWith("- "))
            {
                string zeil = zeile.Substring("- ".Length);
                themen.Add(zeil.Replace(" [!#!] ", "|").Split('|')[0]);
                antwort.Add(zeil.Replace(" [!#!] ", "|").Split('|')[1]);
            }
            else
            {
                Logging.log(Logging.LogType.Warning, "Sloxikon", "Sloxikon", "Datei Fehler: Sloxikon: " + titel);
                return;
            }
        }
    }

    public string getPath() { return this.path; }
    public string getTitel() { return this.titel; }
    public List<string> getThemen() { return this.themen; }
    public string getThemenListe()
    {
        string liste = getThemen()[0];
        for (int i = 1; i < getThemen().Count; i++)
        {
            liste += "\n" + getThemen()[i];
        }
        return liste;
    }
    public List<string> getAntwort() { return this.antwort; }
}
