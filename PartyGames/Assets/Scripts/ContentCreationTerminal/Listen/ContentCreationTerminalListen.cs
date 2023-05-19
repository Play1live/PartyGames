using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ContentCreationTerminalListen : MonoBehaviour
{
    private GameObject ListenSelectButton;
    [SerializeField] Toggle VerfuegbareSpiele;
    [SerializeField] Toggle AusgeblendeteSpiele;
    [SerializeField] GameObject SpieldateiEditor;
    [SerializeField] GameObject ScrollDateienContent;
    [SerializeField] GameObject SpieldateienTemplate;
    [SerializeField] GameObject AusgewaehlterTitel;

    [SerializeField] GameObject Quelle;
    [SerializeField] GameObject Sortby;
    [SerializeField] GameObject SortbyText;
    [SerializeField] GameObject Einheit;
    [SerializeField] GameObject ScrollListenContent;

    private string datapath;
    private int displayedGames;
    private List<string> hiddenGameFiles;
    private List<string> activeGameFiles;
    private List<Element> elemente;
    private List<string> sortbylist;

    void OnEnable()
    {
        InitVars();
        LoadGameFiles();
        ListenSelectButton.GetComponent<Button>().interactable = false;
    }

    void OnDisable()
    {
        ListenSelectButton.GetComponent<Button>().interactable = true;
    }

    /// <summary>
    /// Initialisiert die benötigten Variablen
    /// </summary>
    private void InitVars()
    {
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalListen", "InitVars", "Starte Initialisierung...");
        ListenSelectButton = GameObject.Find("Spielauswahl/Viewport/Content/Listen");
        datapath = Config.MedienPath + ListenSpiel.path;
        elemente = new List<Element>();
        sortbylist = new List<string>();
        displayedGames = 0;
        hiddenGameFiles = new List<string>();
        activeGameFiles = new List<string>();
        VerfuegbareSpiele.isOn = true;
        AusgeblendeteSpiele.isOn = true;
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalListen", "InitVars", "Initialisierung beendet.");
    }
    /// <summary>
    /// Lädt alle Spielfiles
    /// </summary>
    public void LoadGameFiles()
    {
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalListen", "LoadGameFiles", "Lade Spieldateien...");
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
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalListen", "LoadGameFiles", "Spieldateien wurden geladen");

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
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalListen", "ClearGameList", "Gamelist wird geleert.");
        displayedGames = 0;
        elemente.Clear();
        sortbylist.Clear();
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
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalListen", "PrintHiddenGames", "Ausgebendete Spieldateien werden angezeigt.");
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
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalListen", "PrintActiveGames", "Aktive Spieldateien werden angezeigt.");
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

        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalListen", "CreateFile", "Erstelle neue Datei.");
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
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalListen", "LoadGameIntoScene", "Lade Fragen der Spieldatei in die Scene.");
        AusgewaehlterTitel.transform.GetChild(0).GetComponentInChildren<TMP_Text>().text = Spieldatei.gameObject.GetComponentInChildren<TMP_Text>().text;
        AusgewaehlterTitel.transform.GetChild(1).GetComponentInChildren<TMP_InputField>().text = Spieldatei.transform.parent.GetChild(1).GetComponent<TMP_InputField>().text;

        ReloadListen();
    }
    /// <summary>
    /// Reloaded alle Mosaike
    /// </summary>
    private void ReloadListen()
    {
        elemente.Clear();
        sortbylist.Clear();
        /*for (int i = 0; i < ScrollListenContent.transform.childCount; i++)
        {
            ScrollListenContent.transform.GetChild(i).gameObject.SetActive(false);
        }*/

        string[] lines = LadeDateien.listInhalt(datapath + "/" + AusgewaehlterTitel.transform.GetChild(1).GetComponentInChildren<TMP_InputField>().text + ".txt");
        for (int i = 0; i < lines.Length; i++)
        {
            string s = lines[i];
            try
            {
                // SortBy Angabe
                if (s.StartsWith("SortBy: "))
                {
                    string sortby = s.Substring("SortBy: ".Length);
                    if (sortby.ToLower().Equals("int"))
                        Sortby.GetComponent<TMP_Dropdown>().value = 0;
                    else if (sortby.ToLower().Equals("double"))
                        Sortby.GetComponent<TMP_Dropdown>().value = 1;
                    else
                        Logging.log(Logging.LogType.Warning, "ContentCreationTerminalListen", "ReloadListen", "Sortby konnte nicht gelesen werden.");
                }
                // SortByText Anzeige
                else if (s.StartsWith("SortByAnzeige: "))
                {
                    string sortbytxt = s.Substring("SortByAnzeige: ".Length);
                    if (sortbytxt.Equals("Viel - Wenig"))
                        SortbyText.GetComponent<TMP_Dropdown>().value = 0;
                    else if (sortbytxt.Equals("Schwer - Leicht"))
                        SortbyText.GetComponent<TMP_Dropdown>().value = 1;
                    else if (sortbytxt.Equals("Groß - Klein"))
                        SortbyText.GetComponent<TMP_Dropdown>().value = 2;
                    else
                        Logging.log(Logging.LogType.Warning, "ContentCreationTerminalListen", "ReloadListen", "SortbyAnzeige konnte nicht gelesen werden.");
                }
                else if (s.StartsWith("Quelle: "))
                {
                    Quelle.GetComponent<TMP_InputField>().text = s.Substring("Quelle: ".Length);
                }
                else if (s.StartsWith("Einheit:"))
                {
                    Einheit.GetComponent<TMP_InputField>().text = s.Substring("Einheit:".Length);
                }
                // ListenElement
                else if (s.StartsWith("- "))
                {
                    if (elemente.Count >= 30)
                    {
                        break;
                    }
                    string tmp = s.Substring(2);
                    string[] split = tmp.Replace(" # ", "|").Split('|');
                    string item = split[0];
                    string sortby = split[1];

                    sortbylist.Add(sortby);
                    elemente.Add(new Element(item, sortby, "0", ""));
                }
                else
                {
                    Logging.log(Logging.LogType.Warning, "ContentCreationTerminalListen", "ReloadListen", "Unbekannter Dateiinhalt: " + s);
                }
            }
            catch (Exception e)
            {
                Logging.log(Logging.LogType.Warning, "ContentCreationTerminalListen", "ReloadListen", "Fehler beim Laden von Listen: " + s, e);
            }
        }

        for (int i = elemente.Count; i < ScrollListenContent.transform.childCount; i++)
        {
            elemente.Add(new Element("", "", "0", ""));
            sortbylist.Add("");
        }

        for (int i = 0; i < elemente.Count; i++)
        {
            if (i >= ScrollListenContent.transform.childCount)
                break;
            ScrollListenContent.transform.GetChild(i).GetChild(0).GetComponent<TMP_InputField>().text = elemente[i].getItem();
            ScrollListenContent.transform.GetChild(i).GetChild(1).GetComponent<TMP_InputField>().text = sortbylist[i];
            ScrollListenContent.transform.GetChild(i).gameObject.SetActive(true);
        }
    }
    /// <summary>
    /// Leert die MosaikAnzeige
    /// </summary>
    public void ClearListenAnzeige()
    {
        AusgewaehlterTitel.transform.GetChild(0).GetComponentInChildren<TMP_Text>().text = "";
        AusgewaehlterTitel.transform.GetChild(1).GetComponentInChildren<TMP_InputField>().text = "";

        elemente.Clear();
        for (int i = 0; i < ScrollListenContent.transform.childCount; i++)
        {
            ScrollListenContent.transform.GetChild(i).gameObject.SetActive(false);
        }
    }
    /// <summary>
    /// Aktualisiert Name & Url und Image von Mosaiken aus der Gameliste
    /// </summary>
    /// <param name="Frage"></param>
    public void ChangeListenText(GameObject Frage)
    {
        int index = Int32.Parse(Frage.name.Replace("Element (", "").Replace(")", ""));

        elemente[index].setItem(Frage.transform.GetChild(0).GetComponent<TMP_InputField>().text);
        sortbylist[index] = Frage.transform.GetChild(1).GetComponent<TMP_InputField>().text;

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
        lines += "Quelle: " + Quelle.GetComponent<TMP_InputField>().text;
        lines += "\nSortBy: " + Sortby.GetComponent<TMP_Dropdown>().options[Sortby.GetComponent<TMP_Dropdown>().value].text;
        lines += "\nEinheit:" + Einheit.GetComponent<TMP_InputField>().text;
        lines += "\nSortByAnzeige: " + SortbyText.GetComponent<TMP_Dropdown>().options[SortbyText.GetComponent<TMP_Dropdown>().value].text;

        for (int i = 0; i < elemente.Count; i++)
        {
            if (elemente[i].getItem().Length == 0)
                continue;
            lines += "\n- " + elemente[i].getItem() + " # " + sortbylist[i];
        }

        File.WriteAllText(datapath + "/" + AusgewaehlterTitel.transform.GetChild(1).GetComponentInChildren<TMP_InputField>().text + ".txt", lines);
    }
}
