using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class QuizSpiel
{
    private int minPlayer = 3;
    private int maxPlayer = 9;
    public static string path = "/Spiele/Quiz";
    private List<Quiz> quizze;
    private Quiz selectedQuiz;

    public QuizSpiel()
    {
        Logging.log(Logging.LogType.Debug, "QuizSpiel", "QuizSpiel", "Spiele werden geladen.");
        quizze = new List<Quiz>();
        quizze.Add(new Quiz("Freestyle"));

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
            quizze.Add(new Quiz(sfile));
        }
    }
    public int getMinPlayer() { return minPlayer; }
    public int getMaxPlayer() { return maxPlayer; }
    public List<Quiz> getQuizze() { return quizze; }
    public Quiz getSelected() { return selectedQuiz; }
    public void setSelected(Quiz quiz) { selectedQuiz = quiz; }
    public List<string> getGamesAsStringList()
    {
        List<string> quizzes = new List<string>();
        foreach (Quiz quiz in getQuizze())
        {
            quizzes.Add(quiz.getTitel());
        }
        return quizzes;
    }
    public int getQuizzeLength() { return quizze.Count; }
    public int getIndex(Quiz quiz) { return quizze.IndexOf(quiz); }
    public int getIndex(string titel) { foreach (Quiz quiz in quizze) if (quiz.getTitel().ToLower().Equals(titel.ToLower())) return quizze.IndexOf(quiz); return -1; }
    public Quiz getQuizByIndex(int index) { return quizze[index]; }
    public Quiz getQuizByTitle(string title) { foreach (Quiz quiz in quizze) if (quiz.getTitel().ToLower().Equals(title.ToLower())) return quiz; return null; }
    public Quiz getRandomQuiz() { return quizze[Random.Range(0, getQuizzeLength())]; }
}
