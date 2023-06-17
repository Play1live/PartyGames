using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ContentCreationTerminalSloxikon : MonoBehaviour
{
    private GameObject SloxikonSelectButton;
    [SerializeField] Toggle VerfuegbareSpiele;
    [SerializeField] Toggle AusgeblendeteSpiele;
    [SerializeField] GameObject SpieldateiEditor;
    [SerializeField] GameObject ScrollDateienContent;
    [SerializeField] GameObject SpieldateienTemplate;
    [SerializeField] GameObject AusgewaehlterTitel;
    [SerializeField] GameObject ScrollSloxikonContent;

    [SerializeField] GameObject ElementHinzufuegen;

    private string datapath;
    private int displayedGames;
    private List<string> hiddenGameFiles;
    private List<string> activeGameFiles;
    private Sloxikon sloxikongame;

    private void OnEnable()
    {
        InitVars();
        LoadGameFiles();
        SloxikonSelectButton.GetComponent<Button>().interactable = false;
    }
    private void OnDisable()
    {
        SloxikonSelectButton.GetComponent<Button>().interactable = true;
    }

    /// <summary>
    /// Initialisiert die benötigten Variablen
    /// </summary>
    private void InitVars()
    {
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalSloxikon", "InitVars", "Starte Initialisierung...");
        SloxikonSelectButton = GameObject.Find("Spielauswahl/Viewport/Content/Sloxikon");
        datapath = Config.MedienPath + SloxikonSpiel.path;
        displayedGames = 0;
        hiddenGameFiles = new List<string>();
        activeGameFiles = new List<string>();
        VerfuegbareSpiele.isOn = true;
        AusgeblendeteSpiele.isOn = true;
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalSloxikon", "InitVars", "Initialisierung beendet.");
    }
    /// <summary>
    /// Lädt alle Spielfiles
    /// </summary>
    public void LoadGameFiles()
    {
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalSloxikon", "LoadGameFiles", "Lade Spieldateien...");
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
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalSloxikon", "LoadGameFiles", "Spieldateien wurden geladen");

        ClearGameList();
        PrintActiveGames();
        PrintHiddenGames();

        ClearListenAnzeige();
    }
    /// <summary>
    /// Leert die Gameliste
    /// </summary>
    public void ClearGameList()
    {
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalSloxikon", "ClearGameList", "Gamelist wird geleert.");
        displayedGames = 0;
        sloxikongame = null;
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
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalSloxikon", "PrintHiddenGames", "Ausgebendete Spieldateien werden angezeigt.");
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
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalSloxikon", "PrintActiveGames", "Aktive Spieldateien werden angezeigt.");
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

        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalSloxikon", "CreateFile", "Erstelle neue Datei.");
        File.Create(datapath + "/" + input.text + ".txt");
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
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalSloxikon", "LoadGameIntoScene", "Lade Fragen der Spieldatei in die Scene.");
        AusgewaehlterTitel.transform.GetChild(0).GetComponentInChildren<TMP_Text>().text = Spieldatei.gameObject.GetComponentInChildren<TMP_Text>().text;
        AusgewaehlterTitel.transform.GetChild(1).GetComponentInChildren<TMP_InputField>().text = Spieldatei.transform.parent.GetChild(1).GetComponent<TMP_InputField>().text;

        ReloadSloxikons();
    }
    /// <summary>
    /// Reloaded alle Mosaike
    /// </summary>
    private void ReloadSloxikons()
    {
        sloxikongame = null;
        for (int i = 0; i < ScrollSloxikonContent.transform.childCount; i++)
        {
            ScrollSloxikonContent.transform.GetChild(i).gameObject.SetActive(false);
        }

        sloxikongame = new Sloxikon(datapath + "/" + AusgewaehlterTitel.transform.GetChild(1).GetComponentInChildren<TMP_InputField>().text + ".txt");
        
        for (int i = 0; i < sloxikongame.getThemen().Count; i++)
        {
            if (i >= ScrollSloxikonContent.transform.childCount)
                break;
            int index = ScrollSloxikonContent.transform.childCount - i - 1;
            ScrollSloxikonContent.transform.GetChild(index).GetChild(0).GetComponent<TMP_InputField>().text = sloxikongame.getThemen()[i];
            ScrollSloxikonContent.transform.GetChild(index).GetChild(1).GetComponent<TMP_InputField>().text = sloxikongame.getAntwort()[i];
            ScrollSloxikonContent.transform.GetChild(index).gameObject.SetActive(true);
        }
    }
    /// <summary>
    /// Leert die MosaikAnzeige
    /// </summary>
    public void ClearListenAnzeige()
    {
        AusgewaehlterTitel.transform.GetChild(0).GetComponentInChildren<TMP_Text>().text = "";
        AusgewaehlterTitel.transform.GetChild(1).GetComponentInChildren<TMP_InputField>().text = "";

        sloxikongame = null;
        for (int i = 0; i < ScrollSloxikonContent.transform.childCount; i++)
        {
            ScrollSloxikonContent.transform.GetChild(i).gameObject.SetActive(false);
        }
    }
    /// <summary>
    /// Aktualisiert Name & Url und Image von Mosaiken aus der Gameliste
    /// </summary>
    /// <param name="element"></param>
    public void ChangeSloxikonText(GameObject element)
    {
        int index = 30 - Int32.Parse(element.name.Replace("Element (", "").Replace(")", "")) - 1;

        sloxikongame.getThemen()[index] = element.transform.GetChild(0).GetComponent<TMP_InputField>().text;
        sloxikongame.getAntwort()[index] = element.transform.GetChild(1).GetComponent<TMP_InputField>().text;

        WriteFile();
    }
    /// <summary>
    /// Ändert Quelle, Sortby, Sortbytxt, Einheit
    /// </summary>
    public void ChangeInfos()
    {
        WriteFile();
    }
    /// <summary>
    /// Speichert Änderungen in den Dateien
    /// </summary>
    private void WriteFile()
    {
        string lines = "";
        for (int i = 0; i < sloxikongame.getThemen().Count; i++)
        {
            if (sloxikongame.getThemen()[i].Length == 0)
                continue;
            lines += "\n- " + sloxikongame.getThemen()[i] + " [!#!] " + sloxikongame.getAntwort()[i];
        }
        if (lines.Length > 1)
            lines = lines.Substring("\n".Length);

        File.WriteAllText(datapath + "/" + AusgewaehlterTitel.transform.GetChild(1).GetComponentInChildren<TMP_InputField>().text + ".txt", lines);
    }
    /// <summary>
    /// Löscht ein bestehendes Element
    /// </summary>
    /// <param name="element"></param>
    public void DeleteElement(GameObject element)
    {
        int index = 30 - Int32.Parse(element.name.Replace("Element (", "").Replace(")", "")) - 1;
        sloxikongame.getThemen().RemoveAt(index);
        sloxikongame.getAntwort().RemoveAt(index);

        WriteFile();
        ReloadSloxikons();
    }
    /// <summary>
    /// Fügt ein neues Element hinzu
    /// </summary>
    public void AddElement()
    {
        if (ElementHinzufuegen.transform.GetChild(0).GetComponent<TMP_InputField>().text.Length == 0 ||
            ElementHinzufuegen.transform.GetChild(1).GetComponent<TMP_InputField>().text.Length == 0)
            return;
        if (sloxikongame == null)
            return;
        if (sloxikongame.getThemen().Count >= 30)
            return;

        sloxikongame.getThemen().Add(ElementHinzufuegen.transform.GetChild(0).GetComponent<TMP_InputField>().text.Replace("\n", "\\n"));
        sloxikongame.getAntwort().Add(ElementHinzufuegen.transform.GetChild(1).GetComponent<TMP_InputField>().text.Replace("\n", "\\n"));

        ElementHinzufuegen.transform.GetChild(0).GetComponent<TMP_InputField>().text = "";
        ElementHinzufuegen.transform.GetChild(1).GetComponent<TMP_InputField>().text = "";

        WriteFile();

        ReloadSloxikons();
    }
}
