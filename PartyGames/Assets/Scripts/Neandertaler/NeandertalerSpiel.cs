using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class NeandertalerSpiel
{
    public static int minPlayer = 3;
    public static int maxPlayer = 9;
    private List<Neandertaler> neandertalers;
    private Neandertaler selected;
    public int wortcounter;

    public NeandertalerSpiel()
    {
        Logging.log(Logging.LogType.Debug, "NeandertalerSpiel", "NeandertalerSpiel", "Spieldateien werden geladen.");
        neandertalers = new List<Neandertaler>();
        wortcounter = 0;
        List<string> packnames = new List<string>();
        packnames.AddRange(new string[] { "normal" });

        foreach (string item in packnames)
        {
            neandertalers.Add(new Neandertaler(item, Resources.Load<TextAsset>("Spiele/Neandertaler/" + item).text.Replace("\n", "").Replace("\\n", "")));
        }

        foreach (Neandertaler item in neandertalers)
        {
            wortcounter += item.getCount();
        }

        if (neandertalers.Count > 0)
            setSelected(neandertalers[0]);
    }

    public int getMinPlayer() { return minPlayer; }
    public int getMaxPlayer() { return maxPlayer; }
    public List<Neandertaler> getListen() { return this.neandertalers; }
    public int getIndex(Neandertaler wort) { return neandertalers.IndexOf(wort); }
    public Neandertaler getListe(int index) { return neandertalers[index]; }
    public Neandertaler getRandomListe() { return neandertalers[UnityEngine.Random.Range(0, getListen().Count)]; }
    public Neandertaler getSelected() { return selected; }
    public void setSelected(Neandertaler wort) { selected = wort; }
    public List<string> getGamesAsStringList()
    {
        List<string> s = new List<string>();
        foreach (Neandertaler ge in neandertalers)
        {
            s.Add(ge.getTitel());
        }
        return s;
    }
}

public class Neandertaler
{
    private string name;
    private List<string> worte;

    public Neandertaler(string name, string inhalt)
    {
        Logging.log(Logging.LogType.Debug, "Neandertaler", "Neandertaler", "Lade Datei: " + name);
        this.name = name;
        this.worte = new List<string>();
        List<string> wort = new List<string>();
        foreach (string item in inhalt.Split('~'))
        {
            if (item.Length == 0)
                continue;
            string temp = item; //= replaceShit(item);

            temp = temp.Replace("##ss##", "ß").Replace("#ss#", "ß")
                .Replace("#ue#", "ü").Replace("#UE#", "Ü")
                .Replace("#oe#", "ö").Replace("#OE#", "Ö")
                .Replace("#ae#", "ä").Replace("#AE#", "Ä");
            try
            {
                if (temp.Length <= 1 || temp.Equals(""))
                    continue;
                wort.Add(temp);
                worte.Add(temp);
            }
            catch
            {
                Logging.log(Logging.LogType.Warning, "Neandertaler", "Neandertaler", "Fehler beim laden: " + name + " -> " + temp + " >>" + worte.Count);
            }
        }

#if UNITY_EDITOR
        if (false)
        {
            // Save Files
            Logging.log(Logging.LogType.Normal, "Neandertaler", "Neandertaler", "File: " + name + " gefundene Worte: " + this.worte.Count);
            List<string> wortcheck = new List<string>();
            foreach (var item in this.worte)
            {
                if (!wortcheck.Contains(item))
                    wortcheck.Add(item);
                //.Replace("ß", "#ss#").Replace("Ü", "#UE#").Replace("ü", "#ue#").Replace("Ö", "#OE#").Replace("ö", "#oe#").Replace("Ä", "#AE#").Replace("ä", "#ae#");
            }
            Logging.log(Logging.LogType.Normal, "Neandertaler", "Neandertaler", "File: " + name + " ohne Dopplungen Worte: " + wortcheck.Count);

            if (this.worte.Count == wortcheck.Count)
                return;

            string newFile = "";
            foreach (var item in wortcheck)
            {
                newFile += "~" + item.Replace("ß", "#ss#").Replace("Ü", "#UE#").Replace("ü", "#ue#").Replace("Ö", "#OE#").Replace("ö", "#oe#").Replace("Ä", "#AE#").Replace("ä", "#ae#");
            }
            newFile = newFile.Substring("~".Length);
            File.WriteAllText(Application.dataPath + "/Resources/Spiele/Neandertaler/" + name + "2.txt", newFile);
            Logging.log(Logging.LogType.Normal, "Neandertaler", "Neandertaler", "File: " + name + " wurde gespeichert.");
        }
#endif
    }

    public string getTitel() { return this.name; }
    public int getCount() { return this.worte.Count; }
    public string GetRandomItem(bool deleteFromList)
    {
        if (this.worte.Count == 0)
            return "Keine weiteren Worte";
        string item = this.worte[UnityEngine.Random.Range(0, this.worte.Count)];
        if (deleteFromList)
            this.worte.Remove(item);
        return item;
    }

    private string replaceShit(string data)
    {
        string tempsafe = data;

        data = data.Replace("#ß#", "ß");


        if (tempsafe.Equals(data))
            return data;
        else
        {
            Debug.LogWarning(tempsafe);
            Tabu.needToSafe = true;
            return data;
        }
    }
}