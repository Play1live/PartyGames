using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ContentCreationTerminalJeopardy : MonoBehaviour
{
    private GameObject JeopardySelectButton;
    [SerializeField] Toggle VerfuegbareSpiele;
    [SerializeField] Toggle AusgeblendeteSpiele;
    [SerializeField] GameObject SpieldateiEditor;
    [SerializeField] GameObject ScrollDateienContent;
    [SerializeField] GameObject SpieldateienTemplate;
    [SerializeField] GameObject AusgewaehlterTitel;

    [SerializeField] GameObject Grid;

    private string datapath;
    private int displayedGames;
    private List<string> hiddenGameFiles;
    private List<string> activeGameFiles;
    private Jeopardy jeopardy;


    void OnEnable()
    {
        InitVars();
        LoadGameFiles();
        JeopardySelectButton.GetComponent<Button>().interactable = false;
    }
    private void OnDisable()
    {
        JeopardySelectButton.GetComponent<Button>().interactable = true;
    }

    void Update()
    {

    }

    /// <summary>
    /// Initialisiert die benötigten Variablen
    /// </summary>
    private void InitVars()
    {
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalJeopardy", "InitVars", "Starte Initialisierung...");
        JeopardySelectButton = GameObject.Find("Spielauswahl/Viewport/Content/Jeopardy");
        datapath = Config.MedienPath + JeopardySpiel.path;
        displayedGames = 0;
        hiddenGameFiles = new List<string>();
        activeGameFiles = new List<string>();
        VerfuegbareSpiele.isOn = true;
        AusgeblendeteSpiele.isOn = true;
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalJeopardy", "InitVars", "Initialisierung beendet.");
    }
    /// <summary>
    /// Lädt alle Spielfiles
    /// </summary>
    public void LoadGameFiles()
    {
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalJeopardy", "LoadGameFiles", "Lade Spieldateien...");
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
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalJeopardy", "LoadGameFiles", "Spieldateien wurden geladen");

        ClearGameList();
        PrintActiveGames();
        PrintHiddenGames();
    }
    /// <summary>
    /// Leert die Gameliste
    /// </summary>
    public void ClearGameList()
    {
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalJeopardy", "ClearGameList", "Gamelist wird geleert.");
        displayedGames = 0;
        jeopardy = null;
        for (int i = 0; i < ScrollDateienContent.transform.childCount; i++)
        {
            if (ScrollDateienContent.transform.GetChild(i).gameObject.name.Equals("Spieldatei"))
                continue;

            Destroy(ScrollDateienContent.transform.GetChild(i).gameObject);
        }
    }
    /// <summary>
    /// Blendet hidden Spiele ein
    /// </summary>
    public void PrintHiddenGames()
    {
        if (!AusgeblendeteSpiele.isOn)
            return;
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalJeopardy", "PrintHiddenGames", "Ausgebendete Spieldateien werden angezeigt.");
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
    /// Blendet aktive Spiele ein
    /// </summary>
    public void PrintActiveGames()
    {
        if (!VerfuegbareSpiele.isOn)
            return;
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalJeopardy", "PrintActiveGames", "Aktive Spieldateien werden angezeigt.");
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
    /// Benennt Spieldatei um
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
    /// Erstellt ein neues Gamefile
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

        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalJeopardy", "CreateFile", "Erstelle neue Datei.");
        File.Create(datapath + "/" + input.text + ".txt").Close();
        File.WriteAllText(datapath + "/" + input.text + ".txt", "|0~~~|0~~~|0~~~|0~~~|0~~~\n|0~~~|0~~~|0~~~|0~~~|0~~~\n|0~~~|0~~~|0~~~|0~~~|0~~~\n|0~~~|0~~~|0~~~|0~~~|0~~~\n|0~~~|0~~~|0~~~|0~~~|0~~~\n|0~~~|0~~~|0~~~|0~~~|0~~~");
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
    /// Lädt Mosaikelemente in die Scene
    /// </summary>
    /// <param name="Spieldatei"></param>
    public void LoadGameIntoScene(Button Spieldatei)
    {
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalJeopardy", "LoadGameIntoScene", "Lade Fragen der Spieldatei in die Scene.");
        AusgewaehlterTitel.transform.GetChild(0).GetComponentInChildren<TMP_Text>().text = Spieldatei.gameObject.GetComponentInChildren<TMP_Text>().text;
        AusgewaehlterTitel.transform.GetChild(1).GetComponentInChildren<TMP_InputField>().text = Spieldatei.transform.parent.GetChild(1).GetComponent<TMP_InputField>().text;

        ReloadJeopardy();
    }
    /// <summary>
    /// Reloaded Jeopardy
    /// </summary>
    private void ReloadJeopardy()
    {
        Logging.log(Logging.LogType.Normal, "CCTJ", "SaveJeopardy", "Lädt die Jeopardy Datei neu in die Vorschau");
        jeopardy = null;
        jeopardy = new Jeopardy(datapath + "/" + AusgewaehlterTitel.transform.GetChild(1).GetComponentInChildren<TMP_InputField>().text + ".txt");

        if (jeopardy == null)
            return;
        for (int i = 0; i < 6; i++)
            {
                Grid.transform.GetChild(i).GetChild(0).GetChild(0).GetComponent<TMP_InputField>().text = "";
                for (int j = 1; j < 5 + 1; j++)
                {
                    Grid.transform.GetChild(i).GetChild(j).GetChild(0).GetComponent<TMP_InputField>().text = "";
                    Grid.transform.GetChild(i).GetChild(j).GetChild(1).GetComponent<TMP_InputField>().text = "";
                    Grid.transform.GetChild(i).GetChild(j).GetChild(2).GetComponent<TMP_InputField>().text = "";
                    Grid.transform.GetChild(i).GetChild(j).GetChild(3).GetComponent<TMP_InputField>().text = "";
                }
            }
        for (int i = 0; i < jeopardy.getThemen().Count; i++)
        {
            Grid.transform.GetChild(i).GetChild(0).GetChild(0).GetComponent<TMP_InputField>().text = jeopardy.getThemen()[i].thema;
            for (int j = 1; j < jeopardy.getThemen()[i].items.Count + 1; j++)
            {
                Grid.transform.GetChild(i).GetChild(j).GetChild(0).GetComponent<TMP_InputField>().text = jeopardy.getThemen()[i].items[j-1].points + "";
                Grid.transform.GetChild(i).GetChild(j).GetChild(1).GetComponent<TMP_InputField>().text = jeopardy.getThemen()[i].items[j-1].frage;
                Grid.transform.GetChild(i).GetChild(j).GetChild(2).GetComponent<TMP_InputField>().text = jeopardy.getThemen()[i].items[j-1].antwort;
                Grid.transform.GetChild(i).GetChild(j).GetChild(3).GetComponent<TMP_InputField>().text = jeopardy.getThemen()[i].items[j-1].imageurl;
            }
        }
    }
    /// <summary>
    /// Speichert die Jeopardy Datei
    /// </summary>
    public void SaveJeopardy()
    {
        Logging.log(Logging.LogType.Normal, "CCTJ", "SaveJeopardy", "Speichert die Jeopardy Datei");
        string lines = "";
        for (int i = 0; i < jeopardy.getThemen().Count; i++)
        {
            lines += "\n" + Grid.transform.GetChild(i).GetChild(0).GetChild(0).GetComponent<TMP_InputField>().text.Replace("~", "").Replace("|", "");
            for (int j = 1; j < jeopardy.getThemen()[i].items.Count + 1; j++)
            {
                lines += "|" + Grid.transform.GetChild(i).GetChild(j).GetChild(0).GetComponent<TMP_InputField>().text.Replace("~", "").Replace("|", "");
                lines += "~" + Grid.transform.GetChild(i).GetChild(j).GetChild(1).GetComponent<TMP_InputField>().text.Replace("~", "").Replace("|", "");
                lines += "~" + Grid.transform.GetChild(i).GetChild(j).GetChild(2).GetComponent<TMP_InputField>().text.Replace("~", "").Replace("|", "");
                lines += "~" + Grid.transform.GetChild(i).GetChild(j).GetChild(3).GetComponent<TMP_InputField>().text.Replace("~", "").Replace("|", "");
            }
        }
        if (lines.Length > 0)
            lines = lines.Substring("\n".Length);

        File.WriteAllText(datapath + "/" + AusgewaehlterTitel.transform.GetChild(1).GetComponentInChildren<TMP_InputField>().text + ".txt", lines);
    }
}
