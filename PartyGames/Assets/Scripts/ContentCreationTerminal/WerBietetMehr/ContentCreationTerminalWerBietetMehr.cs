using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ContentCreationTerminalWerBietetMehr : MonoBehaviour
{
    private GameObject WBMSelectButton;
    [SerializeField] Toggle VerfuegbareSpiele;
    [SerializeField] Toggle AusgeblendeteSpiele;
    [SerializeField] GameObject SpieldateiEditor;
    [SerializeField] GameObject ScrollDateienContent;
    [SerializeField] GameObject SpieldateienTemplate;
    [SerializeField] GameObject AusgewaehlterTitel;
    [SerializeField] GameObject ScrollWBMContent;
    [SerializeField] GameObject Quelle;

    private string datapath;
    private int displayedGames;
    private List<string> hiddenGameFiles;
    private List<string> activeGameFiles;
    private string quelle;
    private List<string> elemente;

    private void OnEnable()
    {
        InitVars();
        LoadGameFiles();
        WBMSelectButton.GetComponent<Button>().interactable = false;
    }
    private void OnDisable()
    {
        WBMSelectButton.GetComponent<Button>().interactable = true;
    }

    /// <summary>
    /// Initialisiert die benötigten Variablen
    /// </summary>
    private void InitVars()
    {
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalWerBietetMehr", "InitVars", "Starte Initialisierung...");
        WBMSelectButton = GameObject.Find("Spielauswahl/Viewport/Content/WerBietetMehr");
        datapath = Config.MedienPath + WerBietetMehrSpiel.path;
        displayedGames = 0;
        hiddenGameFiles = new List<string>();
        activeGameFiles = new List<string>();
        elemente = new List<string>();
        VerfuegbareSpiele.isOn = true;
        AusgeblendeteSpiele.isOn = true;
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalWerBietetMehr", "InitVars", "Initialisierung beendet.");
    }
    /// <summary>
    /// Lädt alle Spielfiles
    /// </summary>
    public void LoadGameFiles()
    {
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalWerBietetMehr", "LoadGameFiles", "Lade Spieldateien...");
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
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalWerBietetMehr", "LoadGameFiles", "Spieldateien wurden geladen");

        ClearGameList();
        PrintActiveGames();
        PrintHiddenGames();

        ClearWBMAnzeige();
    }
    /// <summary>
    /// Leert die Gameliste
    /// </summary>
    public void ClearGameList()
    {
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalWerBietetMehr", "ClearGameList", "Gamelist wird geleert.");
        displayedGames = 0;
        elemente.Clear();
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
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalWerBietetMehr", "PrintHiddenGames", "Ausgebendete Spieldateien werden angezeigt.");
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
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalWerBietetMehr", "PrintActiveGames", "Aktive Spieldateien werden angezeigt.");
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

        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalWerBietetMehr", "CreateFile", "Erstelle neue Datei.");
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
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalWerBietetMehr", "LoadGameIntoScene", "Lade Fragen der Spieldatei in die Scene.");
        AusgewaehlterTitel.transform.GetChild(0).GetComponentInChildren<TMP_Text>().text = Spieldatei.gameObject.GetComponentInChildren<TMP_Text>().text;
        AusgewaehlterTitel.transform.GetChild(1).GetComponentInChildren<TMP_InputField>().text = Spieldatei.transform.parent.GetChild(1).GetComponent<TMP_InputField>().text;

        ReloadWBMElements();
    }
    /// <summary>
    /// Reloaded alle Mosaike
    /// </summary>
    private void ReloadWBMElements()
    {
        elemente.Clear();
        for (int i = 0; i < ScrollWBMContent.transform.childCount; i++)
        {
            ScrollWBMContent.transform.GetChild(i).gameObject.SetActive(false);
        }

        foreach (string s in LadeDateien.listInhalt(datapath + "/" + AusgewaehlterTitel.transform.GetChild(1).GetComponentInChildren<TMP_InputField>().text + ".txt"))
        {
            try
            {
                if (s.StartsWith("Quelle: "))
                {
                    quelle = s.Substring("Quelle: ".Length);
                }
                else if (s.StartsWith("- "))
                {
                    elemente.Add(s.Substring("- ".Length));
                }
            }
            catch (Exception e)
            {
                Logging.log(Logging.LogType.Warning, "ContentCreationTerminalWerBietetMehr", "ReloadAuktionElements", "Spieldatei konnte nicht geladen werden.", e);
            }
        }

        for (int i = elemente.Count; i < 30; i++)
        {
            elemente.Add("");
        }

        for (int i = 0; i < elemente.Count; i++)
        {
            if (i >= ScrollWBMContent.transform.childCount)
                break;
            ScrollWBMContent.transform.GetChild(i).GetChild(0).GetComponent<TMP_InputField>().text = elemente[i];
            ScrollWBMContent.transform.GetChild(i).gameObject.SetActive(true);
        }
        Quelle.GetComponent<TMP_InputField>().text = quelle;
    }
    /// <summary>
    /// Leert die MosaikAnzeige
    /// </summary>
    public void ClearWBMAnzeige()
    {
        AusgewaehlterTitel.transform.GetChild(0).GetComponentInChildren<TMP_Text>().text = "";
        AusgewaehlterTitel.transform.GetChild(1).GetComponentInChildren<TMP_InputField>().text = "";

        elemente.Clear();
        for (int i = 0; i < ScrollWBMContent.transform.childCount; i++)
        {
            ScrollWBMContent.transform.GetChild(i).gameObject.SetActive(false);
        }
        Quelle.GetComponent<TMP_InputField>().text = "";
    }
    /// <summary>
    /// Aktualisiert Name & Url und Image von Mosaiken aus der Gameliste
    /// </summary>
    /// <param name="element"></param>
    public void ChangeWBMElementText(GameObject element)
    {
        int index = Int32.Parse(element.name.Replace("Element (", "").Replace(")", ""));
        try
        {
            elemente[index] = element.transform.GetChild(0).GetComponent<TMP_InputField>().text;
        }
        catch (Exception e)
        {
            Logging.log(Logging.LogType.Warning, "ContentCreationTerminalWerBietetMehr", "ChangeAuktionElementText", "Fehler bei Element: " + elemente[index], e);
        }

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
        string lines = "Quelle: " + quelle;
        for (int i = 0; i < elemente.Count; i++)
        {
            if (elemente[i].Length == 0)
                continue;
            lines += "\n- " + elemente[i];
        }

        File.WriteAllText(datapath + "/" + AusgewaehlterTitel.transform.GetChild(1).GetComponentInChildren<TMP_InputField>().text + ".txt", lines);
    }
    /// <summary>
    /// Aktualisiert die Quelle des Games
    /// </summary>
    public void UpdateQuelle()
    {
        quelle = Quelle.GetComponent<TMP_InputField>().text;
        WriteFile();
    }
}
