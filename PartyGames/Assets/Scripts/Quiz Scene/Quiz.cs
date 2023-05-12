using System.Collections.Generic;
using UnityEngine;

public class Quiz
{
    private string path;
    private string titel;
    private List<QuizFragen> fragen;

    public Quiz(string path)
    {
        Logging.log(Logging.LogType.Debug, "QuizSpiel", "QuizSpiel", "Spiel wird geladen: " + path);
        if (path == "Freestyle")
        {
            titel = "Freestyle";
            fragen = new List<QuizFragen>();
            fragen.Add(new QuizFragen("Freestyle", "Freestyle", "Freestyle"));
            return;
        }

        this.path = path;
        string temp = path.Split('\\')[path.Split('\\').Length - 1];
        this.titel = temp.Split('/')[temp.Split('/').Length - 1].Replace(".txt", "");
        fragen = new List<QuizFragen>();

        string[] zeilen = LadeDateien.listInhalt(path);
        for (int i = 0; i < zeilen.Length; )
        {
            string frage = zeilen[i];
            i++;
            string antwort = zeilen[i].Replace("\\n", "\n");
            i++;
            string info = zeilen[i].Replace("\\n", "\n");
            i++;

            if (frage.StartsWith("Frage"))
                frage = frage.Substring("Frage".Length);
            if (frage.StartsWith(":"))
                frage = frage.Substring(":".Length);
            if (frage.StartsWith(" "))
                frage = frage.Substring(" ".Length);

            if (antwort.StartsWith("Antwort"))
                antwort = antwort.Substring("Antwort".Length);
            if (antwort.StartsWith(":"))
                antwort = antwort.Substring(":".Length);
            if (antwort.StartsWith(" "))
                antwort = antwort.Substring(" ".Length);

            if (info.StartsWith("Info"))
                info = info.Substring("Info".Length);
            if (info.StartsWith(":"))
                info = info.Substring(":".Length);
            if (info.StartsWith(" "))
                info = info.Substring(" ".Length);

            fragen.Add(new QuizFragen(frage, antwort, info));
        }
    }

    public string getPath() { return this.path; }
    public string getTitel() { return this.titel; }
    public List<QuizFragen> getFragen() { return this.fragen; }
    public int getFragenCount() { return this.fragen.Count; }
    public QuizFragen getFrage(int index) { return this.fragen[index]; }
}

public class QuizFragen
{
    private string frage;
    private string antwort;
    private string info;

    public QuizFragen(string frage, string antwort, string info)
    {
        this.frage = frage;
        this.antwort = antwort;
        this.info = info;
    }

    public string getFrage() { return this.frage; }
    public string getAntwort() { return this.antwort; }
    public string getInfo() { return this.info; }
}