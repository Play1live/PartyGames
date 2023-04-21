using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SloxikonSpiel
{
    public static string path = "/Spiele/Sloxikon";
    private List<Sloxikon> games;
    private Sloxikon selected;

    public SloxikonSpiel()
    {
        Logging.log(Logging.LogType.Debug, "SloxikonSpiel", "SloxikonSpiel", "Lade Spieldateien");
        games = new List<Sloxikon>();

        foreach (string sfile in Directory.GetFiles(Config.MedienPath + path))
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

            // Lädt alle Flaggen und speichert diese ab
            games.Add(new Sloxikon(sfile));
        }
    }

    public List<Sloxikon> getGames() { return games; }
    public Sloxikon getSelected() { return selected; }
    public void setSelected(Sloxikon game) { selected = game; }
    public List<string> getGamesAsStringList()
    {
        List<string> games = new List<string>();
        foreach (Sloxikon game in getGames())
        {
            games.Add(game.getTitel());
        }
        return games;
    }
    public int getQuizzeLength() { return games.Count; }
    public int getIndex(Sloxikon game) { return games.IndexOf(game); }
    public int getIndex(string titel) { foreach (Sloxikon game in games) if (game.getTitel().ToLower().Equals(titel.ToLower())) return games.IndexOf(game); return -1; }
    public Sloxikon getQuizByIndex(int index) { return games[index]; }
    public Sloxikon getQuizByTitle(string title) { foreach (Sloxikon game in games) if (game.getTitel().ToLower().Equals(title.ToLower())) return game; return null; }
    public Sloxikon getRandomQuiz() { return games[Random.Range(0, getQuizzeLength())]; }
}
