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
