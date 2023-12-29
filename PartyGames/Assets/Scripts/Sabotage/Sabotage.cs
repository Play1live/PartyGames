using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using UnityEngine;
using System.Linq;

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
            else if (line.StartsWith("-"))
            {
                runden[index].Add(line.Substring(1));
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

}

// Spieler Nach einander Reinziehen
public class SabotageTabu
{

}

// Spieler rein ziehen
public class SabotageAuswahlstrategie
{

}
