using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class WerBietetMehrSpiel
{
    public static int minPlayer = 3;
    public static int maxPlayer = 9;
    public static string path = "/Spiele/WerBietetMehr";
    private List<WerBietetMehr> liste;
    private WerBietetMehr selected;

    public WerBietetMehrSpiel()
    {
        Logging.log(Logging.LogType.Debug, "WerBietetMehrSpiel", "WerBietetMehrSpiel", "Spiele werden geladen.");
        liste = new List<WerBietetMehr>();

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

            // L�dt alle Flaggen und speichert diese ab
            liste.Add(new WerBietetMehr(sfile));
        }

        if (liste.Count > 0)
        {
            setSelected(liste[0]);
        }
    }

    public int getMinPlayer() { return minPlayer; }
    public int getMaxPlayer() { return maxPlayer; }
    public List<WerBietetMehr> getSpiele() { return liste; }
    public WerBietetMehr getSelected() { return selected; }
    public void setSelected(WerBietetMehr liste) { selected = liste; }
    public List<string> getGamesAsList()
    {
        List<string> quizzes = new List<string>();
        foreach (WerBietetMehr quiz in getSpiele())
        {
            quizzes.Add(quiz.getTitel());
        }
        return quizzes;
    }
    public int getIndex(WerBietetMehr quiz) { return liste.IndexOf(quiz); }
    public WerBietetMehr getQuizByIndex(int index) { return liste[index]; }


}
