using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ContentCreationTerminalAuktion : MonoBehaviour
{
    private GameObject AuktionSelectButton;
    [SerializeField] Toggle VerfuegbareSpiele;
    [SerializeField] Toggle AusgeblendeteSpiele;
    [SerializeField] GameObject SpieldateiEditor;
    [SerializeField] GameObject ScrollDateienContent;
    [SerializeField] GameObject SpieldateienTemplate;
    [SerializeField] GameObject AusgewaehlterTitel;
    [SerializeField] GameObject ScrollAuktionContent;

    private string datapath;
    private int displayedGames;
    private List<string> hiddenGameFiles;
    private List<string> activeGameFiles;
    private List<AuktionElement> elemente;
    private Coroutine loadimagesintoscrollview;
    private Coroutine loadchangedimages;

    private void OnEnable()
    {
        InitVars();
        LoadGameFiles();
        AuktionSelectButton.GetComponent<Button>().interactable = false;
    }
    private void OnDisable()
    {
        AuktionSelectButton.GetComponent<Button>().interactable = true;
    }

    /// <summary>
    /// Initialisiert die benötigten Variablen
    /// </summary>
    private void InitVars()
    {
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalAuktion", "InitVars", "Starte Initialisierung...");
        AuktionSelectButton = GameObject.Find("Spielauswahl/Viewport/Content/Auktion");
        datapath = Config.MedienPath + AuktionSpiel.path;
        displayedGames = 0;
        hiddenGameFiles = new List<string>();
        activeGameFiles = new List<string>();
        elemente = new List<AuktionElement>();
        VerfuegbareSpiele.isOn = true;
        AusgeblendeteSpiele.isOn = true;
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalAuktion", "InitVars", "Initialisierung beendet.");
    }
    /// <summary>
    /// Lädt alle Spielfiles
    /// </summary>
    public void LoadGameFiles()
    {
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalAuktion", "LoadGameFiles", "Lade Spieldateien...");
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
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalAuktion", "LoadGameFiles", "Spieldateien wurden geladen");

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
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalAuktion", "ClearGameList", "Gamelist wird geleert.");
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
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalAuktion", "PrintHiddenGames", "Ausgebendete Spieldateien werden angezeigt.");
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
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalAuktion", "PrintActiveGames", "Aktive Spieldateien werden angezeigt.");
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

        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalAuktion", "CreateFile", "Erstelle neue Datei.");
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
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalAuktion", "LoadGameIntoScene", "Lade Fragen der Spieldatei in die Scene.");
        AusgewaehlterTitel.transform.GetChild(0).GetComponentInChildren<TMP_Text>().text = Spieldatei.gameObject.GetComponentInChildren<TMP_Text>().text;
        AusgewaehlterTitel.transform.GetChild(1).GetComponentInChildren<TMP_InputField>().text = Spieldatei.transform.parent.GetChild(1).GetComponent<TMP_InputField>().text;

        ReloadAuktionElements();
    }
    /// <summary>
    /// Reloaded alle Mosaike
    /// </summary>
    private void ReloadAuktionElements()
    {
        elemente.Clear();
        for (int i = 0; i < ScrollAuktionContent.transform.childCount; i++)
        {
            ScrollAuktionContent.transform.GetChild(i).gameObject.SetActive(false);
        }

        foreach (string s in LadeDateien.listInhalt(datapath + "/" + AusgewaehlterTitel.transform.GetChild(1).GetComponentInChildren<TMP_InputField>().text + ".txt"))
        {
            try
            {
                if (s.StartsWith("- "))
                {
                    string[] tmp = s.Substring("- ".Length).Replace(" <!#!#!> ", "|").Split('|');
                    string name = tmp[0];
                    float preis = float.Parse(tmp[1]);
                    string url = tmp[2];
                    Sprite[] bilder = new Sprite[5];
                    string[] bilderURL = new string[5];
                    bilderURL[0] = tmp[3];
                    bilderURL[1] = tmp[4];
                    bilderURL[2] = tmp[5];
                    bilderURL[3] = tmp[6];
                    bilderURL[4] = tmp[7];
                    elemente.Add(new AuktionElement(name, preis, url, bilder, bilderURL));
                }
            }
            catch (Exception e)
            {
                Logging.log(Logging.LogType.Warning, "ContentCreationTerminalAuktion", "ReloadAuktionElements", "Spieldatei konnte nicht geladen werden.", e);
            }
        }

        for (int i = elemente.Count; i < 10; i++)
        {
            string[] urls = new string[5];
            urls[0] = "";
            urls[1] = "";
            urls[2] = "";
            urls[3] = "";
            urls[4] = "";
            elemente.Add(new AuktionElement("", 0.0f, "", new Sprite[5], urls));
        }

        for (int i = 0; i < elemente.Count; i++)
        {
            if (i >= ScrollAuktionContent.transform.childCount)
                break;
            ScrollAuktionContent.transform.GetChild(i).GetChild(0).GetComponent<TMP_InputField>().text = elemente[i].getName();
            ScrollAuktionContent.transform.GetChild(i).GetChild(1).GetComponent<TMP_InputField>().text = elemente[i].getPreis() + "";
            ScrollAuktionContent.transform.GetChild(i).GetChild(2).GetComponent<TMP_InputField>().text = elemente[i].getURL();
            ScrollAuktionContent.transform.GetChild(i).GetChild(8).GetComponent<TMP_InputField>().text = elemente[i].getBilderURL()[0];
            ScrollAuktionContent.transform.GetChild(i).GetChild(9).GetComponent<TMP_InputField>().text = elemente[i].getBilderURL()[1];
            ScrollAuktionContent.transform.GetChild(i).GetChild(10).GetComponent<TMP_InputField>().text = elemente[i].getBilderURL()[2];
            ScrollAuktionContent.transform.GetChild(i).GetChild(11).GetComponent<TMP_InputField>().text = elemente[i].getBilderURL()[3];
            ScrollAuktionContent.transform.GetChild(i).GetChild(12).GetComponent<TMP_InputField>().text = elemente[i].getBilderURL()[4];
            ScrollAuktionContent.transform.GetChild(i).gameObject.SetActive(true);
        }

        if (loadimagesintoscrollview != null)
        {
            StopCoroutine(loadimagesintoscrollview);
            loadimagesintoscrollview = null;
        }
        loadimagesintoscrollview = StartCoroutine(LoadImagesIntoScrollView());
    }
    /// <summary>
    /// Leert die MosaikAnzeige
    /// </summary>
    public void ClearListenAnzeige()
    {
        AusgewaehlterTitel.transform.GetChild(0).GetComponentInChildren<TMP_Text>().text = "";
        AusgewaehlterTitel.transform.GetChild(1).GetComponentInChildren<TMP_InputField>().text = "";

        elemente.Clear();
        for (int i = 0; i < ScrollAuktionContent.transform.childCount; i++)
        {
            ScrollAuktionContent.transform.GetChild(i).gameObject.SetActive(false);
        }
    }
    /// <summary>
    /// Aktualisiert Name & Url und Image von Mosaiken aus der Gameliste
    /// </summary>
    /// <param name="element"></param>
    public void ChangeAuktionElementText(GameObject element)
    {
        int index = Int32.Parse(element.name.Replace("Element (", "").Replace(")", ""));
        int getchangedURL = -1;
        try
        {
            if (element.transform.GetChild(8).GetComponent<TMP_InputField>().text != elemente[index].getBilderURL()[0])
                getchangedURL = 0;
            else if (element.transform.GetChild(9).GetComponent<TMP_InputField>().text != elemente[index].getBilderURL()[1])
                getchangedURL = 1;
            else if (element.transform.GetChild(10).GetComponent<TMP_InputField>().text != elemente[index].getBilderURL()[2])
                getchangedURL = 2;
            else if (element.transform.GetChild(11).GetComponent<TMP_InputField>().text != elemente[index].getBilderURL()[3])
                getchangedURL = 3;
            else if (element.transform.GetChild(12).GetComponent<TMP_InputField>().text != elemente[index].getBilderURL()[4])
                getchangedURL = 4;

            elemente[index].setName(element.transform.GetChild(0).GetComponent<TMP_InputField>().text);
            elemente[index].setPreis(float.Parse(element.transform.GetChild(1).GetComponent<TMP_InputField>().text));
            elemente[index].setURL(element.transform.GetChild(2).GetComponent<TMP_InputField>().text);
            elemente[index].setBilderURL(element.transform.GetChild(8).GetComponent<TMP_InputField>().text, 0);
            elemente[index].setBilderURL(element.transform.GetChild(9).GetComponent<TMP_InputField>().text, 1);
            elemente[index].setBilderURL(element.transform.GetChild(10).GetComponent<TMP_InputField>().text, 2);
            elemente[index].setBilderURL(element.transform.GetChild(11).GetComponent<TMP_InputField>().text, 3);
            elemente[index].setBilderURL(element.transform.GetChild(12).GetComponent<TMP_InputField>().text, 4);
        }
        catch (Exception e)
        {
            Logging.log(Logging.LogType.Warning, "ContentCreationTerminalAuktion", "ChangeAuktionElementText", "Fehler bei Element: " + elemente[index].getName(), e);
        }

        WriteFile();

        if (getchangedURL == -1)
            return;
        if (loadchangedimages != null)
        {
            StopCoroutine(loadchangedimages);
            loadchangedimages = null;
        }
        loadchangedimages = StartCoroutine(LoadChangedImages(element.transform.GetChild(8 + getchangedURL).GetComponent<TMP_InputField>()));
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
        for (int i = 0; i < elemente.Count; i++)
        {
            lines += "\n- " + elemente[i].getName() + " <!#!#!> " + 
                elemente[i].getPreis() + " <!#!#!> " + 
                elemente[i].getURL() + " <!#!#!> " + 
                elemente[i].getBilderURL()[0] + " <!#!#!> " +
                elemente[i].getBilderURL()[1] + " <!#!#!> " +
                elemente[i].getBilderURL()[2] + " <!#!#!> " +
                elemente[i].getBilderURL()[3] + " <!#!#!> " +
                elemente[i].getBilderURL()[4];
        }
        if (lines.Length > 1)
            lines = lines.Substring("\n".Length);

        File.WriteAllText(datapath + "/" + AusgewaehlterTitel.transform.GetChild(1).GetComponentInChildren<TMP_InputField>().text + ".txt", lines);
    }
    /// <summary>
    /// Lädt fehlende Bilder herunter und blendet diese ein
    /// </summary>
    /// <returns></returns>
    IEnumerator LoadImagesIntoScrollView()
    {
        for (int i = 0; i < elemente.Count; i++)
        {
            if (i >= ScrollAuktionContent.transform.childCount)
                break;

            for (int j = 0; j < 5; j++)
            {
                if (elemente[i].getBilderURL()[j].Length == 0)
                {
                    ScrollAuktionContent.transform.GetChild(i).GetChild(3 + j).GetComponent<Image>().sprite = null;
                    continue;
                }

                UnityWebRequest www = UnityWebRequestTexture.GetTexture(elemente[i].getBilderURL()[j]);
                yield return www.SendWebRequest();
                if (www.result != UnityWebRequest.Result.Success)
                {
                    Logging.log(Logging.LogType.Warning, "ContentCreationTerminalAuktion", "LoadImagesIntoScrollView", "Bild konnte nicht herunter geladen werden: " + www.error);
                }
                else
                {
                    Logging.log(Logging.LogType.Normal, "ContentCreationTerminalAuktion", "LoadImagesIntoScrollView", "Bild wurde erfolgreich herunter geladen.");
                    Texture2D myTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                    Sprite sprite = Sprite.Create(myTexture, new Rect(0, 0, myTexture.width, myTexture.height), new Vector2(0.5f, 0.5f), 100);
                    elemente[i].getBilder()[j] = sprite;
                    ScrollAuktionContent.transform.GetChild(i).GetChild(3 + j).GetComponent<Image>().sprite = sprite;
                }
            }
        }
        yield break;
    }
    /// <summary>
    /// Lädt geändertes Bild herunter
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    IEnumerator LoadChangedImages(TMP_InputField input)
    {
        int index = Int32.Parse(input.transform.parent.gameObject.name.Replace("Element (", "").Replace(")", ""));
        for (int j = 0; j < 5; j++)
        {
            if (elemente[index].getBilderURL()[j].Length == 0)
            {
                ScrollAuktionContent.transform.GetChild(index).GetChild(3 + j).GetComponent<Image>().sprite = null;
                continue;
            }

            UnityWebRequest www = UnityWebRequestTexture.GetTexture(elemente[index].getBilderURL()[j]);
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Logging.log(Logging.LogType.Warning, "ContentCreationTerminalAuktion", "LoadChangedImages", "Bild konnte nicht herunter geladen werden: " + www.error);
            }
            else
            {
                Logging.log(Logging.LogType.Normal, "ContentCreationTerminalAuktion", "LoadChangedImages", "Bild wurde erfolgreich herunter geladen.");
                Texture2D myTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                Sprite sprite = Sprite.Create(myTexture, new Rect(0, 0, myTexture.width, myTexture.height), new Vector2(0.5f, 0.5f), 100);
                elemente[index].getBilder()[j] = sprite;
                ScrollAuktionContent.transform.GetChild(index).GetChild(3 + j).GetComponent<Image>().sprite = sprite;
            }
        }
        yield break;
    }
}
