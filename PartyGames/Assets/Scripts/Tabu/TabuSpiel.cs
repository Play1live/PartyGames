
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TabuSpiel
{
    public static int minPlayer = 4;
    public static int maxPlayer = 8;
    public static string GameType = "1 Wort"; // 1 Wort | Timer
    private List<Tabu> tabus;
    private Tabu selected;
    public int wortcounter;

    public TabuSpiel()
    {
        Logging.log(Logging.LogType.Debug, "TabuSpiel", "TabuSpiel", "Spieldateien werden geladen.");
        tabus = new List<Tabu>();
        wortcounter = 0;
        string[] packnames = new string[] { "Original Spiel", "Normal", "Schwer" }; 
        // TODO: Alle Dateien aus dem Ordner laden

        foreach (string item in packnames)
        {
            tabus.Add(new Tabu(item, Resources.Load<TextAsset>("Spiele/Tabu/" + item).text));
        }

        foreach (Tabu item in tabus)
            wortcounter += item.getGeheimwörter().Count;

        if (tabus.Count > 0)
            setSelected(tabus[0]);

        /*
        //Erstelle Liste mit allen Worten die nicht verwendet werden
        List<string> verwendete = new List<string>();
        List<string> nichtverwendete = new List<string>();
        foreach (Tabu item in tabus)
            foreach (TabuItem i in item.getGeheimwörter())
                verwendete.Add(i.geheimwort);
        foreach (Tabu item in tabus)
            foreach (TabuItem i in item.getGeheimwörter())
                foreach (string worte in i.verboteneWorte.Replace("\\n", "-").Split("-"))
                    if (!verwendete.Contains(worte))
                        nichtverwendete.Add(worte);
        foreach (string item in verwendete)
            while (nichtverwendete.Contains(item))
                nichtverwendete.Remove(item);
        
        Debug.LogWarning(nichtverwendete.Count);
        string ausgabe = "";
        foreach (string item in nichtverwendete)
        {
            if (!ausgabe.Contains(item))
                ausgabe += item + "\n";
        }
        File.WriteAllText(Application.persistentDataPath + "/NichtverwendeteWorte.txt", ausgabe);*/
    }

    public int getMinPlayer() { return minPlayer; }
    public int getMaxPlayer() { return maxPlayer; }
    public List<Tabu> getListen() { return this.tabus; }
    public int getIndex(Tabu wort) { return tabus.IndexOf(wort); }
    public Tabu getListe(int index) { return tabus[index]; }
    public Tabu getRandomListe() { return tabus[Random.Range(0, getListen().Count)]; }
    public Tabu getSelected() { return selected; }
    public void setSelected(Tabu wort) { selected = wort; }

    public List<string> getGamesAsStringList()
    {
        List<string> s = new List<string>();
        foreach (Tabu ge in tabus)
        {
            s.Add(ge.getTitel());
        }
        return s;
    }
}

public class Tabu
{
    private string name;
    private List<TabuItem> worte;

    public Tabu(string name, string inhalt)
    {
        Logging.log(Logging.LogType.Debug, "Tabu", "Tabu", "Lade Datei: " + name);
        this.name = name;

        this.worte = new List<TabuItem>();
        List<string> wort = new List<string>();
        foreach (string item in inhalt.Split('\n'))
        {
            string temp = item.Replace("##ss##", "ß").Replace("#ss#", "ß")
                .Replace("#ue#", "ü").Replace("#UE#", "Ü")
                .Replace("#oe#", "ö").Replace("#OE#", "Ö")
                .Replace("#ae#", "ä").Replace("#AE#", "Ä");
            try
            {
                if (!wort.Contains(temp.Split('|')[0]))
                {
                    if (temp.Equals(""))
                        continue;
                    wort.Add(temp.Split('|')[0]);
                    worte.Add(new TabuItem(temp.Split('|')[0], temp.Split('|')[1]));
                }
                else
                {
                    Logging.log(Logging.LogType.Warning, "Tabu", "Tabu", name + " -> Dopplung: " + temp.Split('|')[0]);
                }
            }
            catch
            {
                Logging.log(Logging.LogType.Warning, "Tabu", "Tabu", "Fehler beim laden: " + name + " -> " + temp + " >>" + worte.Count);
            }
        }

        // Save Files
        if (inhalt.Split('\n').Length != worte.Count)
        {
            string newFile = "";
            foreach (TabuItem item in this.worte)
                newFile += "\n" + item.geheimwort.Replace("ß", "#ss#")
                .Replace("Ü", "#UE#").Replace("ü", "#ue#")
                .Replace("Ö", "#OE#").Replace("ö", "#oe#")
                .Replace("Ä", "#AE#").Replace("ä", "#ae#")
                + "|" + item.verboteneWorte.Replace("\\n", "-").Replace("ß", "#ss#")
                .Replace("Ü", "#UE#").Replace("ü", "#ue#")
                .Replace("Ö", "#OE#").Replace("ö", "#oe#")
                .Replace("Ä", "#AE#").Replace("ä", "#ae#");
            if (newFile.Length > 2)
                newFile = newFile.Substring("\n".Length);

            File.WriteAllText(Application.dataPath + "/Resources/Spiele/Tabu/" + name + ".txt", newFile);
            Logging.log(Logging.LogType.Normal, "Tabu", "Tabu", "File: " + name + " wurde gespeichert.");
        }
    }
    public string getTitel() { return this.name; }
    public List<TabuItem> getGeheimwörter() { return this.worte; }
    public TabuItem GetRandomItem()
    {
        if (this.worte.Count == 0)
            return new TabuItem("Keine weiteren Worte", "");
        TabuItem item = this.worte[UnityEngine.Random.Range(0, this.worte.Count)];
        this.worte.Remove(item);
        return item;
    }
}

public class TabuItem
{
    public string geheimwort;
    public string verboteneWorte;

    public TabuItem(string geheimwort, string verboteneWorte)
    {
        this.geheimwort = geheimwort;
        this.verboteneWorte = verboteneWorte.Replace("-", "\\n");
    }
}