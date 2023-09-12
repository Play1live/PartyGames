
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TabuSpiel
{
    public static int minPlayer = 4;
    public static int maxPlayer = 8;
    public static string GameType = "1 Wort"; // 1 Wort | Normal | Timer | Battle Royale
    private List<Tabu> tabus;
    private Tabu selected;
    public static int TABU_WORTE_COUNT = 6;
    public int wortcounter;

    public TabuSpiel()
    {
        Logging.log(Logging.LogType.Debug, "TabuSpiel", "TabuSpiel", "Spieldateien werden geladen.");
        tabus = new List<Tabu>();
        wortcounter = 0;
        List<string> packnames = new List<string>();
        packnames.AddRange(new string[] { "Original Spiel", "Normal", "Schwer" });

        // Weihnachtspack 15.11.XXXX - 30.12.XXXX
        if (DateTime.Compare(DateTime.Now, new DateTime(DateTime.Now.Year, 11, 15)) > 0 && DateTime.Compare(DateTime.Now, new DateTime(DateTime.Now.Year, 12, 30)) < 0)
            packnames.Add("Weihnachten");
        // Oasternpack 15.03.XXXX - 30.4.XXXX
        if (DateTime.Compare(DateTime.Now, new DateTime(DateTime.Now.Year, 3, 15)) > 0 && DateTime.Compare(DateTime.Now, new DateTime(DateTime.Now.Year, 4, 30)) < 0)
            packnames.Add("Ostern");

        foreach (string item in packnames)
        {
            tabus.Add(new Tabu(item, Resources.Load<TextAsset>("Spiele/Tabu/" + item).text.Replace("\n", "").Replace("\\n", "")));
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
            if (item.getTitel().Equals("Schwer"))
                foreach (TabuItem i in item.getGeheimwörter())
                {
                    if (!verwendete.Contains(i.geheimwort))
                        verwendete.Add(i.geheimwort);
                    foreach (string worte in i.tabuworte.Replace("\\n", "-").Split("-"))
                        if (!worte.Contains(" ") && worte.Length < 10)
                            if (!nichtverwendete.Contains(worte) && !verwendete.Contains(worte))
                                nichtverwendete.Add(worte);
                }
        foreach (string item in verwendete)
            while (nichtverwendete.Contains(item))
                nichtverwendete.Remove(item);
        foreach (string item in nichtverwendete)
            while (verwendete.Contains(item))
                nichtverwendete.Remove(item);

        Debug.LogWarning("Nicht verwendete Worte: " + nichtverwendete.Count);
        string ausgabe = "";
        foreach (string item in nichtverwendete)
        {
            if (!ausgabe.Contains(item))
                ausgabe += item + "\n";
        }
        File.WriteAllText(Application.persistentDataPath + "/NichtverwendeteWorte.txt", ausgabe);
        */
    }

    public int getMinPlayer() { return minPlayer; }
    public int getMaxPlayer() { return maxPlayer; }
    public List<Tabu> getListen() { return this.tabus; }
    public int getIndex(Tabu wort) { return tabus.IndexOf(wort); }
    public Tabu getListe(int index) { return tabus[index]; }
    public Tabu getRandomListe() { return tabus[UnityEngine.Random.Range(0, getListen().Count)]; }
    public Tabu getSelected() { return selected; }
    public void setSelected(Tabu wort) { selected = wort; }
    public static int[] genWorteList(TabuItem item)
    {
        List<string> wtemp = item.getWorte();
        int counter = TabuSpiel.TABU_WORTE_COUNT;
        if (wtemp.Count < TabuSpiel.TABU_WORTE_COUNT)
            counter = wtemp.Count;
        int[] zahlentemp = new int[counter];
        for (int i = 0; i < counter; i++)
        {
            int random = UnityEngine.Random.Range(0, wtemp.Count);
            zahlentemp[i] = item.getWorte().IndexOf(wtemp[random]);
            wtemp.Remove(wtemp[random]);
        }
        return zahlentemp;
    }
    public static string getIntArrayToString(int[] list)
    {
        string ausgabe = "";
        foreach (int item in list)
            ausgabe += "." + item;
        if (ausgabe.Length > 1)
            ausgabe = ausgabe.Substring(1);
        return ausgabe;
    }
    public static string getStringArrayToString(string worte, string list)
    {
        string ausgabe = "";
        List<string> wlist = new List<string>();
        wlist.AddRange(worte.Split('-'));
        foreach (string item in list.Split('.'))
            ausgabe += "-" + wlist[Int32.Parse(item)];
        if (ausgabe.Length > 1)
            ausgabe = ausgabe.Substring(1);
        return ausgabe;
    }
    public static string getKartenWorte(string worte, int[] zahlen)
    {
        string ausgabe = "";
        List<string> wlist = new List<string>();
        wlist.AddRange(worte.Split('-'));
        foreach (int item in zahlen)
            ausgabe += "-" + wlist[item];
        if (ausgabe.Length > 1)
            ausgabe = ausgabe.Substring(1);
        return ausgabe;
    }
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
    public static bool needToSafe = false;
    private string name;
    private List<TabuItem> worte;

    public Tabu(string name, string inhalt)
    {
        Logging.log(Logging.LogType.Debug, "Tabu", "Tabu", "Lade Datei: " + name);
        this.name = name;
        needToSafe = false;
        this.worte = new List<TabuItem>();
        /*
#if UNITY_EDITOR
        string fileinhalt = Resources.Load<TextAsset>("Spiele/Tabu/" + name).text;
        List<string> filezeilen = new List<string>();
        for (int i = 0; i < fileinhalt.Split('\n').Length; i++)
        {
            if (fileinhalt.Split('\n')[i].StartsWith("-"))
                filezeilen[filezeilen.Count - 1] += fileinhalt.Split('\n')[i];
            else
                filezeilen.Add(fileinhalt.Split('\n')[i]);
        }
        if (filezeilen.Count != fileinhalt.Split('\n').Length)
        {
            string newFile = "";
            foreach (var item in filezeilen)
                newFile += "\n" + item;
            if (newFile.Length > 0)
                newFile = newFile.Substring(1);
            File.WriteAllText(Application.dataPath + "/Resources/Spiele/Tabu/" + name + ".txt", newFile);
        }
#endif*/

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
                // TODO: dopplung am anfang wird nicht gehandelt
                if (!wort.Contains(temp.Split('|')[0]))
                {
                    if (temp.Length <= 1 || temp.Equals(""))
                        continue;
                    wort.Add(temp.Split('|')[0]);
                    worte.Add(new TabuItem(temp.Split('|')[0], temp.Split('|')[1]));
                }
                else
                {
                    Logging.log(Logging.LogType.Warning, "Tabu", "Tabu", name + " -> Dopplung: " + temp);
                    // TODO: Temporär werden Tabuworte nicht zusammengefügt
                    /*foreach (var dopplungswort in worte)
                    {
                        if (dopplungswort.geheimwort.Equals(temp.Split('|')[0]))
                        {
                            dopplungswort.AddTabuWorte(temp.Split('|')[1]);
                            break;
                        }
                    }*/

                    needToSafe = true;
                }
            }
            catch
            {
                Logging.log(Logging.LogType.Warning, "Tabu", "Tabu", "Fehler beim laden: " + name + " -> " + temp + " >>" + worte.Count);
            }
        }

#if UNITY_EDITOR
        Debug.LogWarning(worte.Count);
        // Save Files
        if (needToSafe)
        {
            string newFile = "";
            foreach (TabuItem item in this.worte)
            {
                newFile += "\n~" + item.geheimwort.Replace("ß", "#ss#")
                .Replace("Ü", "#UE#").Replace("ü", "#ue#")
                .Replace("Ö", "#OE#").Replace("ö", "#oe#")
                .Replace("Ä", "#AE#").Replace("ä", "#ae#")
                + "|" + item.tabuworte.Replace("\n", "-")
                .Replace("ß", "#ss#")
                .Replace("Ü", "#UE#").Replace("ü", "#ue#")
                .Replace("Ö", "#OE#").Replace("ö", "#oe#")
                .Replace("Ä", "#AE#").Replace("ä", "#ae#");
            }

            if (newFile.Length > 2)
                newFile = newFile.Substring("\n".Length);

            File.WriteAllText(Application.dataPath + "/Resources/Spiele/Tabu/" + name + ".txt", newFile);
            Logging.log(Logging.LogType.Normal, "Tabu", "Tabu", "File: " + name + " wurde gespeichert.");
        }
#endif
    }
    public string getTitel() { return this.name; }
    public List<TabuItem> getGeheimwörter() { return this.worte; }
    public TabuItem GetRandomItem(bool deleteFromList)
    {
        if (this.worte.Count == 0)
            return new TabuItem("Keine weiteren Worte", "");
        TabuItem item = this.worte[UnityEngine.Random.Range(0, this.worte.Count)];
        if (deleteFromList)
            this.worte.Remove(item);
        return item;
    }

    private string replaceShit(string data)
    {
        string tempsafe = data;

        data = data.Replace("#ß#", "ß");
        data = data.Replace("Flu#ss#", "Fluss");


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

public class TabuItem
{
    public string geheimwort;
    public string tabuworte;
    
    public TabuItem(string geheimwort, string tabuworte)
    {
        this.geheimwort = geheimwort;
        List<string> worte = new List<string>();
        this.tabuworte = "";
        foreach (string item in tabuworte.Split('-'))
        {
            if (!worte.Contains(item.ToLower()) && item.ToLower() != this.geheimwort.ToLower())
            {
                worte.Add(item.ToLower());
                this.tabuworte += "-" + item;
            }
            else
            {
                Logging.log(Logging.LogType.Warning, "TabuItem", "TabuItem", geheimwort + " -> Dopplung: " + item);
                Tabu.needToSafe = true;
            }
        }
        if (this.tabuworte.Length > 1)
            this.tabuworte = this.tabuworte.Substring(1);
    }

    public List<string> getWorte()
    {
        List<string> worte = new List<string>();
        worte.AddRange(this.tabuworte.Split('-'));
        return worte;
    }

    public void AddTabuWorte(string liste)
    {
        List<string> neueworte = new List<string>();
        foreach (var item in tabuworte.Split('-'))
        {
            if (item.Length == 0)
                continue;
            neueworte.Add(item);
        }
        foreach (var item in liste.Split('-'))
        {
            if (item.Length == 0)
                continue;
            if (item.Contains(geheimwort) || item.Contains(geheimwort.ToLower()))
                continue;
            if (!neueworte.Contains(item))
                neueworte.Add(item);
        }
        tabuworte = "";
        foreach (var item in neueworte)
            tabuworte += "-" + item;
        if (tabuworte.Length > 0)
            tabuworte = tabuworte.Substring(1);
    }
}

public class TabuData
{
    // CORRECT, WRONG, SKIP
    public static List<int> P_1WORT = new List<int> { 1, 0, 0 };
    public static List<int> P_NORMAL = new List<int> { 1, -1, 0 };
    public static List<int> P_TIMER = new List<int> { 20, -10, -5 };
    public static List<int> P_BATTLE_ROYALE = new List<int> { 10, -15, -5 };

    private static List<string> TimerDecPoints = new List<string>{ "Timer", "Battle Royale" };

    public static int InitTeamPoints(string team)
    {
        switch (TabuSpiel.GameType)
        {
            default:
                return 0;
            case "Timer":
                return 300;
            case "Battle Royale":
                return 600;
        }
    }
    public static bool TimerDecreasePoints()
    {
        if (TimerDecPoints.Contains(TabuSpiel.GameType))
            return true;
        else
            return false;
    }
}