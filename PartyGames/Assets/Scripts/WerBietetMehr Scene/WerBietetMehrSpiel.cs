using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class WerBietetMehrSpiel
{
    public static string path = "/Spiele/WerBietetMehr";
    private List<WerBietetMehr> liste;
    private WerBietetMehr selected;

    public WerBietetMehrSpiel()
    {
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

            // Lädt alle Flaggen und speichert diese ab
            liste.Add(new WerBietetMehr(sfile));
        }
    }

    public List<WerBietetMehr> getSpiele() { return liste; }
    public WerBietetMehr getSelected() { return selected; }
    public void setSelected(WerBietetMehr liste) { selected = liste; }
    public List<string> getQuizzeAsStringList()
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
