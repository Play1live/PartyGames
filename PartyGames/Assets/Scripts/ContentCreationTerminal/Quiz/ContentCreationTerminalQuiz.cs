using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ContentCreationTerminalQuiz : MonoBehaviour
{
    private GameObject QuizSelectButton;
    private Toggle VerfuegbareSpiele;
    private Toggle AusgeblendeteSpiele;
    private GameObject SpieldateiEditor;
    private GameObject ScrollDateienContent;
    private GameObject SpieldateienTemplate;
    private GameObject AusgewaehlterTitel;
    private GameObject FrageHinzufügen;
    private GameObject ScrollFragenContent;
    private GameObject FragenTemplate;

    private string datapath;
    private int displayedGames;
    private List<string> hiddenGameFiles;
    private List<string> activeGameFiles;
    private List<QuizFragen> fragenList;

    private void OnEnable()
    {
        InitVars();
        LoadGameFiles();
        QuizSelectButton.GetComponent<Button>().interactable = false;
    }
    private void OnDisable()
    {
        QuizSelectButton.GetComponent<Button>().interactable = true;
    }
    void Update()
    {

    }

    /// <summary>
    /// Initialisiert die benötigten Variablen
    /// </summary>
    private void InitVars()
    {
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalQuiz", "InitVars", "Starte Initialisierung...");
        QuizSelectButton = GameObject.Find("Spielauswahl/Viewport/Content/Quiz");
        datapath = Config.MedienPath + QuizSpiel.path;
        displayedGames = 0;
        hiddenGameFiles = new List<string>();
        activeGameFiles = new List<string>();
        fragenList = new List<QuizFragen>();
        SpieldateiEditor = gameObject.transform.GetChild(0).gameObject;
        VerfuegbareSpiele = gameObject.transform.GetChild(2).GetComponent<Toggle>();
        VerfuegbareSpiele.isOn = true;
        AusgeblendeteSpiele = gameObject.transform.GetChild(3).GetComponent<Toggle>();
        AusgeblendeteSpiele.isOn = true;
        ScrollDateienContent = gameObject.transform.GetChild(4).GetChild(0).GetChild(0).gameObject;
        SpieldateienTemplate = ScrollDateienContent.transform.GetChild(0).gameObject;
        AusgewaehlterTitel = gameObject.transform.GetChild(5).gameObject;
        FrageHinzufügen = gameObject.transform.GetChild(6).gameObject;
        ScrollFragenContent = gameObject.transform.GetChild(7).GetChild(0).GetChild(0).gameObject;
        FragenTemplate = ScrollFragenContent.transform.GetChild(0).gameObject;
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalQuiz", "InitVars", "Initialisierung beendet.");
    }
    /// <summary>
    /// Lädt die Gamespiele aus den Dateien
    /// </summary>
    public void LoadGameFiles()
    {
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalQuiz", "LoadGameFiles", "Lade Spieldateien...");
        hiddenGameFiles.Clear();
        activeGameFiles.Clear();
        foreach (string file in Directory.GetFiles(datapath))
        {
            string title = file.Split('/')[file.Split('/').Length - 1].Split('\\')[file.Split('/')[file.Split('/').Length - 1].Split('\\').Length - 1].Replace(".txt", "");
            if (title.Equals("#Vorlage"))
                continue;
            if (title.StartsWith("#"))
                hiddenGameFiles.Add(title);
            else
                activeGameFiles.Add(title);

        }
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalQuiz", "LoadGameFiles", "Spieldateien wurden geladen");

        ClearGameList();
        PrintActiveGames();
        PrintHiddenGames();

        ClearFragenAnzeige();
    }
    /// <summary>
    /// Leert die Gameliste
    /// </summary>
    public void ClearGameList()
    {
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalQuiz", "ClearGameList", "Gamelist wird geleert.");
        displayedGames = 0;
        for (int i = 0; i < ScrollDateienContent.transform.childCount; i++)
        {
            if (ScrollDateienContent.transform.GetChild(i).gameObject.name.Equals("Spieldatei"))
                continue;

            Destroy(ScrollDateienContent.transform.GetChild(i).gameObject);
        }
    }
    /// <summary>
    /// Blendet hidden Spieldateien ein
    /// </summary>
    public void PrintHiddenGames()
    {
        if (!AusgeblendeteSpiele.isOn)
            return;
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalQuiz", "PrintHiddenGames", "Ausgebendete Spieldateien werden angezeigt.");
        for (int i = 0; i < hiddenGameFiles.Count; i++)
        {
            displayedGames++;
            GameObject go = Instantiate(SpieldateienTemplate, SpieldateienTemplate.transform.position, SpieldateienTemplate.transform.rotation);
            go.name = "File_hidden_" + displayedGames + "_" + UnityEngine.Random.Range(0, 99999) + "*********" + hiddenGameFiles[i];
            go.transform.SetParent(ScrollDateienContent.transform);
            go.transform.GetChild(0).GetComponentInChildren<TMP_Text>().text = displayedGames + "";
            go.transform.GetChild(1).GetComponent<TMP_InputField>().text = hiddenGameFiles[i];
            go.transform.localScale = new Vector3(1, 1, 1);
            go.SetActive(true);
        }
    }
    /// <summary>
    /// Blendet aktive Spieldateien ein
    /// </summary>
    public void PrintActiveGames()
    {
        if (!VerfuegbareSpiele.isOn)
            return;
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalQuiz", "PrintActiveGames", "Aktive Spieldateien werden angezeigt.");
        for (int i = 0; i < activeGameFiles.Count; i++)
        {
            displayedGames++;
            GameObject go = Instantiate(SpieldateienTemplate, SpieldateienTemplate.transform.position, SpieldateienTemplate.transform.rotation);
            go.name = "File_active_" + displayedGames + "_" + UnityEngine.Random.Range(0, 99999) + "*********" + activeGameFiles[i];
            go.transform.SetParent(ScrollDateienContent.transform);
            go.transform.GetChild(0).GetComponentInChildren<TMP_Text>().text = displayedGames + "";
            go.transform.GetChild(1).GetComponent<TMP_InputField>().text = activeGameFiles[i];
            go.transform.localScale = new Vector3(1, 1, 1);
            go.SetActive(true);
        }
    }
    /// <summary>
    /// Ändert den Titel eines Quizspiels
    /// </summary>
    /// <param name="input"></param>
    public void ChangeFileName(TMP_InputField input)
    {
        string prefix = input.transform.parent.name.Replace("*********", "|").Split('|')[0];
        string originalTitel = input.transform.parent.name.Replace("File_active_", "").Replace("File_hidden_", "").Replace("*********", "|").Split('|')[1];

        if (input.text == originalTitel)
            return;

        File.Move(datapath + "/" + originalTitel + ".txt", datapath + "/" + input.text + ".txt");
        input.transform.parent.name = prefix + "*********" + input.text;

        LoadGameFiles();
    }
    /// <summary>
    /// Erstellt ein neues Quizgame
    /// </summary>
    /// <param name="input"></param>
    public void CreateFile(TMP_InputField input)
    {
        if (input.text.Length == 0)
            return;

        if (File.Exists(datapath + "/" + input.text + ".txt"))
        {
            input.text = "";
            return;
        }
        
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalQuiz", "CreateFile", "Erstelle neue Datei.");
        File.Create(datapath + "/" + input.text + ".txt").Close();
        displayedGames++;
        GameObject go = Instantiate(SpieldateienTemplate, SpieldateienTemplate.transform.position, SpieldateienTemplate.transform.rotation);
        go.name = "File_active_" + displayedGames + "_" + UnityEngine.Random.Range(0, 99999) + "*********" + input.text;
        go.transform.SetParent(ScrollDateienContent.transform);
        go.transform.GetChild(0).GetComponentInChildren<TMP_Text>().text = displayedGames + "";
        go.transform.GetChild(1).GetComponent<TMP_InputField>().text = input.text;
        go.transform.localScale = new Vector3(1, 1, 1);
        go.SetActive(true);
        activeGameFiles.Add(input.text);

        input.text = "";
        ClearGameList();
        PrintActiveGames();
        PrintHiddenGames();
    }
    /// <summary>
    /// Lädt die Frageninhalte eines Games in die Scene
    /// </summary>
    /// <param name="Spieldatei"></param>
    public void LoadGameIntoScene(Button Spieldatei)
    {
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalQuiz", "LoadGameIntoScene", "Lade Fragen der Spieldatei in die Scene.");
        AusgewaehlterTitel.transform.GetChild(0).GetComponentInChildren<TMP_Text>().text = Spieldatei.gameObject.GetComponentInChildren<TMP_Text>().text;
        AusgewaehlterTitel.transform.GetChild(1).GetComponentInChildren<TMP_InputField>().text = Spieldatei.transform.parent.GetChild(1).GetComponent<TMP_InputField>().text;

        ReloadFragen();
    }
    /// <summary>
    /// Lädt die Fragen eines Games neu
    /// </summary>
    private void ReloadFragen()
    {
        try
        {
            fragenList.Clear();
            for (int i = 0; i < ScrollFragenContent.transform.childCount; i++)
            {
                ScrollFragenContent.transform.GetChild(i).gameObject.SetActive(false);
            }

            string[] zeilen = File.ReadAllLines(datapath + "/" + AusgewaehlterTitel.transform.GetChild(1).GetComponentInChildren<TMP_InputField>().text + ".txt");
            Logging.log(Logging.LogType.Normal, "CCT-Quiz", "ReloadFragen", "ZeilenZahl: " + zeilen + "/3 = ?");
            for (int i = 0; i < zeilen.Length;)
            {
                string frage = zeilen[i];
                i++;
                string antwort = zeilen[i];
                i++;
                string info = zeilen[i];
                i++;
                Logging.log(Logging.LogType.Normal, "CCT-Quiz", "ReloadFragen", "Frage: " + frage + "\nAntwort: " + antwort + "\nInfo: " + info);

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

                fragenList.Add(new QuizFragen(frage, antwort, info));
            }
            for (int i = 0; i < fragenList.Count; i++)
            {
                if (i >= ScrollFragenContent.transform.childCount)
                    break;
                int index = ScrollFragenContent.transform.childCount - i - 1;
                ScrollFragenContent.transform.GetChild(index).GetChild(0).GetComponentInChildren<TMP_Text>().text = (i + 1) + "";
                ScrollFragenContent.transform.GetChild(index).GetChild(1).GetComponent<TMP_InputField>().text = fragenList[i].getFrage();
                ScrollFragenContent.transform.GetChild(index).GetChild(2).GetComponent<TMP_InputField>().text = fragenList[i].getAntwort();
                ScrollFragenContent.transform.GetChild(index).GetChild(3).GetComponent<TMP_InputField>().text = fragenList[i].getInfo();
                ScrollFragenContent.transform.GetChild(index).gameObject.SetActive(true);
            }
        }
        catch
        {
            Logging.log(Logging.LogType.Error, "CCT-Quiz", "ReloadFragen", "");
        }
    }
    /// <summary>
    /// Löscht eine Frage aus dem Spiel
    /// </summary>
    /// <param name="Frage"></param>
    public void DeleteFrage(GameObject Frage)
    {
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalQuiz", "DeleteFrage", "Frage: " + Frage.transform.GetChild(1).GetComponent<TMP_InputField>().text + " wird gelöscht!");
        for (int i = 0; i < fragenList.Count; i++)
        {
            if (fragenList[i].getFrage() == Frage.transform.GetChild(1).GetComponent<TMP_InputField>().text &&
                fragenList[i].getAntwort() == Frage.transform.GetChild(2).GetComponent<TMP_InputField>().text &&
                fragenList[i].getInfo() == Frage.transform.GetChild(3).GetComponent<TMP_InputField>().text)
            {
                fragenList.RemoveAt(i);
                break;
            }
        }
        for (int i = 0; i < ScrollFragenContent.transform.childCount; i++)
        {
            ScrollFragenContent.transform.GetChild(i).gameObject.SetActive(false);
        }
        for (int i = 0; i < fragenList.Count; i++)
        {
            if (i >= ScrollFragenContent.transform.childCount)
                break;
            int index = ScrollFragenContent.transform.childCount - i - 1;
            ScrollFragenContent.transform.GetChild(index).GetChild(0).GetComponentInChildren<TMP_Text>().text = (i + 1) + "";
            ScrollFragenContent.transform.GetChild(index).GetChild(1).GetComponent<TMP_InputField>().text = fragenList[i].getFrage();
            ScrollFragenContent.transform.GetChild(index).GetChild(2).GetComponent<TMP_InputField>().text = fragenList[i].getAntwort();
            ScrollFragenContent.transform.GetChild(index).GetChild(3).GetComponent<TMP_InputField>().text = fragenList[i].getInfo();
            ScrollFragenContent.transform.GetChild(index).gameObject.SetActive(true);
        }
        // Datei speichern
        string lines = "";
        for (int i = 0; i < fragenList.Count; i++)
        {
            lines += "\nFrage: " + fragenList[i].getFrage();
            lines += "\nAntwort: " + fragenList[i].getAntwort();
            lines += "\nInfo: " + fragenList[i].getInfo();
        }
        if (lines.Length > 2)
            lines = lines.Substring("\n".Length);

        File.WriteAllText(datapath + "/" + AusgewaehlterTitel.transform.GetChild(1).GetComponentInChildren<TMP_InputField>().text + ".txt", lines);
    }
    /// <summary>
    /// Fügt eine Frache dem Game hinzu
    /// </summary>
    public void AddFrage()
    {
        if (AusgewaehlterTitel.transform.GetChild(1).GetComponentInChildren<TMP_InputField>().text.Length == 0)
            return;
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalQuiz", "AddFrage", "Neue Frage wird hinzugefügt.");
        string frage = FrageHinzufügen.transform.GetChild(1).GetComponent<TMP_InputField>().text.Replace("\n", "\\n");
        string antwort = FrageHinzufügen.transform.GetChild(2).GetComponent<TMP_InputField>().text.Replace("\n", "\\n");
        string info = FrageHinzufügen.transform.GetChild(3).GetComponent<TMP_InputField>().text.Replace("\n", "\\n");

        if (frage.Length == 0 || antwort.Length == 0)
            return;

        FrageHinzufügen.transform.GetChild(1).GetComponent<TMP_InputField>().text = "";
        FrageHinzufügen.transform.GetChild(2).GetComponent<TMP_InputField>().text = "";
        FrageHinzufügen.transform.GetChild(3).GetComponent<TMP_InputField>().text = "";

        string lines = File.ReadAllText(datapath + "/" + AusgewaehlterTitel.transform.GetChild(1).GetComponentInChildren<TMP_InputField>().text + ".txt");

        if (lines.Length > 1)
            lines += "\nFrage: " + frage;
        else
            lines += "Frage: " + frage;

        lines += "\nAntwort: " + antwort;
        lines += "\nInfo: " + info;

        File.WriteAllText(datapath + "/" + AusgewaehlterTitel.transform.GetChild(1).GetComponentInChildren<TMP_InputField>().text + ".txt", lines);

        ReloadFragen();
    }
    /// <summary>
    /// Leert die Fragenanzeigen und ausgewähltes Game
    /// </summary>
    private void ClearFragenAnzeige()
    {
        AusgewaehlterTitel.transform.GetChild(0).GetComponentInChildren<TMP_Text>().text = "";
        AusgewaehlterTitel.transform.GetChild(1).GetComponentInChildren<TMP_InputField>().text = "";

        FrageHinzufügen.transform.GetChild(1).GetComponent<TMP_InputField>().text = "";
        FrageHinzufügen.transform.GetChild(2).GetComponent<TMP_InputField>().text = "";
        FrageHinzufügen.transform.GetChild(3).GetComponent<TMP_InputField>().text = "";

        fragenList.Clear();
        for (int i = 0; i < ScrollFragenContent.transform.childCount; i++)
        {
            ScrollFragenContent.transform.GetChild(i).gameObject.SetActive(false);
        }
    }
    /// <summary>
    /// Ändert die Frage/Antwort/Info einer Frage und speichert diese direkt ab
    /// </summary>
    /// <param name="Frage"></param>
    public void ChangeFrageText(GameObject Frage)
    {
        int index = Int32.Parse(Frage.transform.GetChild(0).GetComponentInChildren<TMP_Text>().text) - 1;

        fragenList[index].setFrage(Frage.transform.GetChild(1).GetComponent<TMP_InputField>().text);
        fragenList[index].setAntwort(Frage.transform.GetChild(2).GetComponent<TMP_InputField>().text);
        fragenList[index].setInfo(Frage.transform.GetChild(3).GetComponent<TMP_InputField>().text);

        string lines = "";
        foreach (QuizFragen fragestring in fragenList)
        {
            lines += "\nFrage: " + fragestring.getFrage();
            lines += "\nAntwort: " + fragestring.getAntwort();
            lines += "\nInfo: " + fragestring.getInfo();
        }
        if (lines.Length > 2)
            lines = lines.Substring("\n".Length);

        File.WriteAllText(datapath + "/" + AusgewaehlterTitel.transform.GetChild(1).GetComponentInChildren<TMP_InputField>().text + ".txt", lines);
    }
}
