using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using UnityEngine;
using System.Linq;
using System.Reflection;
using UnityEngine.Experimental.AI;

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
    public int punkteProText = 10;
    public List<string> saetze;
    public string erklaerung = "Ich lese gleich einen Satz 2 mal vor und danach habt ihr 10sek Zeit um euch zu beraten" +
        " wie man welche Worte schreibt. Damit ihr als Team alle alles korrekt habt." +
        "\nPunkteverteilung: Pro richtigen Satz 10p fürs Team und pro falschen Satz 10p für den Saboteur";

    public SabotageDiktat()
    {
        this.index = -1;
        if (!File.Exists(Config.MedienPath + @"/Spiele/Sabotage/Diktat.txt"))
            File.Create(Config.MedienPath + @"/Spiele/Sabotage/Diktat.txt").Close();
        this.saetze = new List<string>();
        this.saetze.AddRange(File.ReadAllLines(Config.MedienPath + @"/Spiele/Sabotage/Diktat.txt"));
    }

    public string GetNew(int change)
    {
        if (this.index <= 0 && change == -1)
            return this.saetze[0];
        else if (this.index == saetze.Count - 1 && change == 1)
            return this.saetze[saetze.Count - 1];
        this.index += change;
        return this.saetze[this.index];
    }
    public string GetSatz() { return saetze[index]; }
    public string markDifferences(string input)
    {
        while (input.EndsWith(" "))
            input = input.Substring(0, input.Length - 1);
        while (input.StartsWith(" "))
            input = input.Substring(1);

        bool allCorrect = true;
        string[] referenceWords = Regex.Split(saetze[index], @"\b");
        string[] userWords = Regex.Split(input, @"\b");

        StringBuilder resultBuilder = new StringBuilder();

        for (int i = 0; i < Math.Max(referenceWords.Length, userWords.Length); i++)
        {
            string referenceWord = i < referenceWords.Length ? referenceWords[i] : "";
            string userWord = i < userWords.Length ? userWords[i] : "";

            if (string.Equals(referenceWord, userWord, StringComparison.OrdinalIgnoreCase))
            {
                // Worte sind identisch
                resultBuilder.Append($"<color=\"green\">{userWord}</color>");
            }
            else
            {
                // Worte sind unterschiedlich
                if (userWord.EndsWith(","))
                {
                    // Benutzerwort endet mit einem Komma oder Punkt, überprüfen Sie die Anzahl der Kommas
                    int referenceCommas = referenceWord.Count(c => c == ',');
                    int userCommas = userWord.Count(c => c == ',');

                    if (referenceCommas != userCommas)
                    {
                        resultBuilder.Append($"<color=\"red\"><b>{userWord}</b></color>");
                        continue;
                    }
                    else
                    {
                        resultBuilder.Append($"<color=\"green\">{userWord}</color>");
                        continue;
                    }
                }
                else if (userWord.EndsWith("."))
                {
                    // Benutzerwort endet mit einem Komma oder Punkt, überprüfen Sie die Anzahl der Kommas
                    int referenceCommas = referenceWord.Count(c => c == '.');
                    int userCommas = userWord.Count(c => c == '.');

                    if (referenceCommas != userCommas)
                    {
                        resultBuilder.Append($"<color=\"red\"><b>{userWord}</b></color>");
                        continue;
                    }
                    else
                    {
                        resultBuilder.Append($"<color=\"green\">{userWord}</color>");
                        continue;
                    }
                }

                resultBuilder.Append($"<color=\"red\"><b>{userWord}</b></color>");
                allCorrect = false;
            }
        }

        return allCorrect + resultBuilder.ToString();
    }
}

public class SabotageSortieren
{
    public int index;
    public int punkteProEinsortierung = 10;
    //     Runde Elemente  
    public List<string> sortby;
    public List<List<string>> runden;
    public string erklaerung = "10 Runden, nach einander, nur 1 kann einsortieren. Alle dürfen reden" +
        " Jede Liste enthält 5 Einzusortierende Elemente und 1 Vorgegebenes." +
        "\nPunkteverteilung: jede korrekte Einsortierung gibt 10p, jede falsche 10p"+
        " Nach einer falschen, wird aber das falsche automatisch richtig einsortiert, damit es nicht zu viele Punkte geben kann";

    public SabotageSortieren()
    {
        this.index = -1;
        this.sortby = new List<string>();
        for (int i = 0; i < 10; i++)
            this.sortby.Add("Leer-Leer");
        this.runden = new List<List<string>>();
        while (this.runden.Count < 10)
            this.runden.Add(new List<string>());
        if (!File.Exists(Config.MedienPath + @"/Spiele/Sabotage/Sortieren.txt"))
            File.Create(Config.MedienPath + @"/Spiele/Sabotage/Sortieren.txt").Close();

        

        foreach (var item in File.ReadAllLines(Config.MedienPath + @"/Spiele/Sabotage/Sortieren.txt"))
        {
            string line = item;
            int index = int.Parse(line.Substring(0, 1));
            line = line.Substring("X_".Length);
            if (line.StartsWith("SortBy:"))
            {
                sortby[index] = line.Substring("SortBy:".Length);
            }
            else
            {
                runden[index].Add(line);
            }
        }
        
        for (int i = 0; i < this.runden.Count; i++)
        {
            while (this.runden[i].Count < 6)
                this.runden[i].Add("Leer");
        }

    }
    
    public int ChangeIndex(int change)
    {
        if (this.index <= 0 && change == -1)
            return 0;
        else if (this.index == this.runden.Count - 1 && change == 1)
            return this.runden.Count-1;
        this.index += change;
        return this.index;
    }
    public List<string> GetInhalt()
    {
        return this.runden[index];
    }
    public string GetSortBy()
    {
        return this.sortby[index];
    }
}

public class SabotageMemory
{
    List<Sprite> sprites;
    public string erklaerung = "Punkteverteilung: Start bei 500 und -5 pro falsches Paar" +
        " Immer nach einander dran ";

    public SabotageMemory() 
    {
        this.sprites = new List<Sprite>();

        List<Sprite> temp = new List<Sprite>();
        temp.AddRange(Resources.LoadAll<Sprite>("Spiele/Sabotage/Memory/"));
        temp.AddRange(Resources.LoadAll<Sprite>("Spiele/Sabotage/Memory/"));
        while (temp.Count > 0)
        {
            Sprite sprite = temp[UnityEngine.Random.Range(0, temp.Count)];
            temp.Remove(sprite);
            this.sprites.Add(sprite);
        }
    }

    public List<Sprite> getIcons()
    {
        return this.sprites;
    }

    public string getSequence()
    {
        string sequence = "";
        foreach (Sprite sprite in this.sprites)
            sequence += "~" + sprite.name;
        if (sequence.Length > 0)
            sequence = sequence.Substring(1);
        return sequence;
    }
}

public class SabotageDerZugLuegt
{
    // 5 Runden mit je 10 Elementen - 50 Elemente insgesamt 
    // 50 Punkte pro Element
    // ca. 10-15 Lügen
    public List<string> thema;
    public List<List<string>> rounds;
    public int index;

    public SabotageDerZugLuegt()
    {
        if (!File.Exists(Config.MedienPath + @"/Spiele/Sabotage/DerZugLuegt.txt"))
            File.Create(Config.MedienPath + @"/Spiele/Sabotage/DerZugLuegt.txt").Close();

        index = 0;
        this.thema = new List<string>();
        this.rounds = new List<List<string>>();
        for (int i = 0; i < 5; i++)
            this.rounds.Add(new List<string>());

        foreach (var item in File.ReadAllLines(Config.MedienPath + @"/Spiele/Sabotage/DerZugLuegt.txt"))
        {
            int index = int.Parse(item.Substring(0,1));
            string temp = item.Substring(1);
            if (temp.StartsWith("Title:"))
                this.thema.Add(temp.Substring("Title:".Length));
            else
                this.rounds[index].Add(temp);
        }

        for (int i = 0; i < 5; i++)
            while (this.rounds[i].Count < 10)
                this.rounds[i].Add("False|Error");
        while (this.thema.Count < 5)
            this.thema.Add("Kein Thema");
    }

    public int ChangeIndex(int change)
    {
        if (this.index <= 0 && change == -1)
            return 0;
        else if (this.index == this.rounds.Count - 1 && change == 1)
            return this.rounds.Count - 1;
        this.index += change;
        return this.index;
    }
    public string GetThema()
    {
        return this.thema[index];
    }
    public string GetElement(int i)
    {
        return this.rounds[index][i].Split('|')[1];
    }
    public bool GetElementType(int i)
    {
        return bool.Parse(this.rounds[index][i].Split('|')[0]);
    }
}

// Spieler Nach einander Reinziehen
// 3 Min Zeit
public class SabotageTabu
{
    public int index;
    public List<string> tabus;

    public SabotageTabu()
    {
        this.tabus = new List<string>();
        this.index = -1;

        if (!File.Exists(Config.MedienPath + @"/Spiele/Sabotage/Tabu.txt"))
            File.Create(Config.MedienPath + @"/Spiele/Sabotage/Tabu.txt").Close();

        foreach (var item in File.ReadAllLines(Config.MedienPath + @"/Spiele/Sabotage/Tabu.txt"))
        {
            this.tabus.Add(item);
        }
    }

    public int ChangeIndex(int change)
    {
        if (this.index <= 0 && change == -1)
            return 0;
        else if (this.index == this.tabus.Count - 1 && change == 1)
            return this.tabus.Count - 1;
        this.index += change;
        return this.index;
    }
    public string GetWort()
    {
        return this.tabus[index].Split('|')[0];
    }
    public string GetTabus()
    {
        return this.tabus[index].Split('|')[1].Replace("~", "   ");
    }
}

// Spieler rein ziehen
public class SabotageAuswahlstrategie
{
    public int index;
    public List<List<Sprite>> runden;
    public List<string> playerturn;

    public SabotageAuswahlstrategie()
    {
        this.index = -1;
        this.runden = new List<List<Sprite>>();
        for (int i = 0; i < 5; i++) 
        {
            List<Sprite> sprites = new List<Sprite>();
            foreach (var item in Resources.LoadAll<Sprite>("Spiele/Sabotage/Auswahlstrategie/"))
            {
                if (int.Parse(item.name.Substring(0, 1)) == i)
                {
                    sprites.Add(item);
                }
            }
            this.runden.Add(sprites);
        }
        this.playerturn = new List<string>();
        this.playerturn.Add("0~4");
        this.playerturn.Add("2~3");
        this.playerturn.Add("1~4");
        this.playerturn.Add("3~0");
        this.playerturn.Add("1~2");
    }
    public int ChangeIndex(int change)
    {
        if (this.index <= 0 && change == -1)
            return 0;
        else if (this.index == this.runden.Count - 1 && change == 1)
            return this.runden.Count - 1;
        this.index += change;
        return this.index;
    }

    public List<Sprite> GetList()
    {
        return runden[index];
    }

    public string GetPlayerTurn()
    {
        return playerturn[index];
    }
}
