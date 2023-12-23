using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Sabotage
{
    private string path;

    public Sabotage()
    {
        Logging.log(Logging.LogType.Debug, "SabotageSpiel", "SabotageSpiel", "Spiel wird geladen: " + path);
        
    }

    public string getPath() { return this.path; }
}
public class SabotageDiktat
{
    public int index;
    public List<string> saetze;
    public string erklaerung = "Ich lese gleich einen Satz 2 mal vor und danach habt ihr 10sek Zeit um euch zu beraten" +
        " wie man welche Worte schreibt. Damit ihr als Team alle alles korrekt habt." +
        "\nPunkteverteilung: Pro richtigen Satz 10p fürs Team und pro falschen Satz 10p für den Saboteur";

    public SabotageDiktat()
    {
        this.index = 0;
        if (!File.Exists(Config.MedienPath + @"/Spiele/Sabotage/Diktat.txt"))
            File.Create(Config.MedienPath + @"/Spiele/Sabotage/Diktat.txt").Close();
        this.saetze = new List<string>();
        this.saetze.AddRange(File.ReadAllLines(Config.MedienPath + @"/Spiele/Sabotage/Diktat.txt"));
    }

    public string getNext()
    {
        if (this.index == this.saetze.Count)
            return "";
        return this.saetze[this.index++];
    }

    public string markDifferences(string input)
    {
        // TODO: hier farbig markieren
        return "";
    }
}
