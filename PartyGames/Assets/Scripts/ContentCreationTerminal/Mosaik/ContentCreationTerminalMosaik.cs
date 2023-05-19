using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ContentCreationTerminalMosaik : MonoBehaviour
{
    private GameObject MosaikSelectButton;
    [SerializeField] Toggle VerfuegbareSpiele;
    [SerializeField] Toggle AusgeblendeteSpiele;
    [SerializeField] GameObject SpieldateiEditor;
    [SerializeField] GameObject ScrollDateienContent;
    [SerializeField] GameObject SpieldateienTemplate;
    [SerializeField] GameObject AusgewaehlterTitel;
    [SerializeField] GameObject MosaikHinzufügen;
    [SerializeField] Image ImageVorschau;
    [SerializeField] GameObject ScrollMosaikContent;
    [SerializeField] GameObject MosaikTemplate;

    private string datapath;
    private int displayedGames;
    private List<string> hiddenGameFiles;
    private List<string> activeGameFiles;
    private Mosaik mosaike;

    private Coroutine reloadImages;
    private Coroutine loadIntoPreview;

    void OnEnable()
    {
        InitVars();
        LoadGameFiles();
        MosaikSelectButton.GetComponent<Button>().interactable = false;
    }
    private void OnDisable()
    {
        MosaikSelectButton.GetComponent<Button>().interactable = true;
    }

    void Update()
    {
        
    }

    /// <summary>
    /// Initialisiert die benötigten Variablen
    /// </summary>
    private void InitVars()
    {
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalMosaik", "InitVars", "Starte Initialisierung...");
        MosaikSelectButton = GameObject.Find("Spielauswahl/Viewport/Content/Mosaik");
        datapath = Config.MedienPath + MosaikSpiel.path;
        displayedGames = 0;
        hiddenGameFiles = new List<string>();
        activeGameFiles = new List<string>();
        VerfuegbareSpiele.isOn = true;
        AusgeblendeteSpiele.isOn = true;
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalMosaik", "InitVars", "Initialisierung beendet.");
    }
    /// <summary>
    /// Lädt alle Spielfiles
    /// </summary>
    public void LoadGameFiles()
    {
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalMosaik", "LoadGameFiles", "Lade Spieldateien...");
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
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalMosaik", "LoadGameFiles", "Spieldateien wurden geladen");

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
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalMosaik", "ClearGameList", "Gamelist wird geleert.");
        displayedGames = 0;
        mosaike = null;
        for (int i = 0; i < ScrollDateienContent.transform.childCount; i++)
        {
            if (ScrollDateienContent.transform.GetChild(i).gameObject.name.Equals("Spieldatei"))
                continue;

            Destroy(ScrollDateienContent.transform.GetChild(i).gameObject);
        }
        ImageVorschau.sprite = null;
    }
    /// <summary>
    /// Blendet hidden Spiele ein
    /// </summary>
    public void PrintHiddenGames()
    {
        if (!AusgeblendeteSpiele.isOn)
            return;
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalMosaik", "PrintHiddenGames", "Ausgebendete Spieldateien werden angezeigt.");
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
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalMosaik", "PrintActiveGames", "Aktive Spieldateien werden angezeigt.");
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

        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalMosaik", "CreateFile", "Erstelle neue Datei.");
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
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalMosaik", "LoadGameIntoScene", "Lade Fragen der Spieldatei in die Scene.");
        AusgewaehlterTitel.transform.GetChild(0).GetComponentInChildren<TMP_Text>().text = Spieldatei.gameObject.GetComponentInChildren<TMP_Text>().text;
        AusgewaehlterTitel.transform.GetChild(1).GetComponentInChildren<TMP_InputField>().text = Spieldatei.transform.parent.GetChild(1).GetComponent<TMP_InputField>().text;

        ReloadMosaike();
    }
    /// <summary>
    /// Reloaded alle Mosaike
    /// </summary>
    private void ReloadMosaike()
    {
        mosaike = null;
        for (int i = 0; i < ScrollMosaikContent.transform.childCount; i++)
        {
            ScrollMosaikContent.transform.GetChild(i).gameObject.SetActive(false);
        }

        mosaike = new Mosaik(datapath + "/" + AusgewaehlterTitel.transform.GetChild(1).GetComponentInChildren<TMP_InputField>().text + ".txt");

        if (reloadImages != null)
        {
            StopCoroutine(reloadImages);
            reloadImages = null;
        }
        reloadImages = StartCoroutine(ReloadImages());
        for (int i = 0; i < mosaike.getNames().Count; i++)
        {
            if (i >= ScrollMosaikContent.transform.childCount)
                break;
            int index = ScrollMosaikContent.transform.childCount - i - 1;
            ScrollMosaikContent.transform.GetChild(index).GetChild(1).GetComponent<TMP_InputField>().text = mosaike.getNames()[i];
            ScrollMosaikContent.transform.GetChild(index).GetChild(2).GetComponent<TMP_InputField>().text = mosaike.getURLs()[i];
            ScrollMosaikContent.transform.GetChild(index).gameObject.SetActive(true);
        }
    }
    /// <summary>
    /// Reloaded alle Images der Mosaikliste
    /// </summary>
    /// <returns></returns>
    IEnumerator ReloadImages()
    {
        for (int i = 0; i < mosaike.getURLs().Count; i++)
        {
            if (i >= ScrollMosaikContent.transform.childCount)
                break;
            if (mosaike.getIstGeladen()[i] == true)
            {
                int index = ScrollMosaikContent.transform.childCount - i - 1;
                ScrollMosaikContent.transform.GetChild(index).GetChild(0).GetComponent<Image>().sprite = mosaike.getSprites()[i];
                continue;
            }

            UnityWebRequest www = UnityWebRequestTexture.GetTexture(mosaike.getURLs()[i]);
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Logging.log(Logging.LogType.Warning, "ContentCreationTerminalMosaik", "ReloadImages", "Bild konnte nicht herunter geladen werden: " + www.error);
            }
            else
            {
                Logging.log(Logging.LogType.Normal, "ContentCreationTerminalMosaik", "ReloadImages", "Bild wurde erfolgreich herunter geladen.");
                Texture2D myTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                Sprite sprite = Sprite.Create(myTexture, new Rect(0, 0, myTexture.width, myTexture.height), new Vector2(0.5f, 0.5f), 100);
                mosaike.getSprites()[i] = sprite;
                mosaike.getIstGeladen()[i] = true;

                int index = ScrollMosaikContent.transform.childCount - i - 1;
                ScrollMosaikContent.transform.GetChild(index).GetChild(0).GetComponent<Image>().sprite = mosaike.getSprites()[i];
            }
        }
        yield break;
    }
    /// <summary>
    /// Lädt ein Image per URL in die Vorschau
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    IEnumerator LoadImageIntoPreview(string url)
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success)
        {
            Logging.log(Logging.LogType.Warning, "ContentCreationTerminalMosaik", "LoadImageIntoPreview", "Bild konnte nicht herunter geladen werden: " + www.error);
        }
        else
        {
            Logging.log(Logging.LogType.Normal, "ContentCreationTerminalMosaik", "LoadImageIntoPreview", "Bild wurde erfolgreich herunter geladen.");
            Texture2D myTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;
            Sprite sprite = Sprite.Create(myTexture, new Rect(0, 0, myTexture.width, myTexture.height), new Vector2(0.5f, 0.5f), 100);
            MosaikHinzufügen.transform.GetChild(0).GetComponent<Image>().sprite = sprite;
            ImageVorschau.sprite = sprite;
        }
    }
    /// <summary>
    /// Löscht Mosaik aus der Spielliste
    /// </summary>
    /// <param name="Mosaik"></param>
    public void DeleteMosaik(GameObject Mosaik)
    {
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalMosaik", "DeleteMosaik", "Mosaik: " + Mosaik.transform.GetChild(1).GetComponent<TMP_InputField>().text + " wird gelöscht!");
        for (int i = 0; i < mosaike.getNames().Count; i++)
        {
            if (mosaike.getNames()[i] == Mosaik.transform.GetChild(1).GetComponent<TMP_InputField>().text &&
                mosaike.getURLs()[i] == Mosaik.transform.GetChild(2).GetComponent<TMP_InputField>().text)
            {
                mosaike.getSprites().RemoveAt(i);
                mosaike.getNames().RemoveAt(i);
                mosaike.getURLs().RemoveAt(i);
                mosaike.getIstGeladen().RemoveAt(i);
                break;
            }
        }
        for (int i = 0; i < ScrollMosaikContent.transform.childCount; i++)
        {
            ScrollMosaikContent.transform.GetChild(i).gameObject.SetActive(false);
        }
        for (int i = 0; i < mosaike.getNames().Count; i++)
        {
            if (i >= ScrollMosaikContent.transform.childCount)
                break;
            int index = ScrollMosaikContent.transform.childCount - i - 1;
            ScrollMosaikContent.transform.GetChild(index).GetChild(0).GetComponent<Image>().sprite = mosaike.getSprites()[i];
            ScrollMosaikContent.transform.GetChild(index).GetChild(1).GetComponent<TMP_InputField>().text = mosaike.getNames()[i];
            ScrollMosaikContent.transform.GetChild(index).GetChild(2).GetComponent<TMP_InputField>().text = mosaike.getURLs()[i];
            ScrollMosaikContent.transform.GetChild(index).gameObject.SetActive(true);
        }
        // Datei speichern
        string lines = "";
        for (int i = 0; i < mosaike.getNames().Count; i++)
        {
            lines += "\n- " + mosaike.getNames()[i] + " [!#!] " + mosaike.getURLs()[i];
        }
        if (lines.Length > 1)
            lines = lines.Substring("\n".Length);

        File.WriteAllText(datapath + "/" + AusgewaehlterTitel.transform.GetChild(1).GetComponentInChildren<TMP_InputField>().text + ".txt", lines);
    }
    /// <summary>
    /// Fügt Mosaik der Liste hinzu
    /// </summary>
    public void AddMosaik()
    {
        if (AusgewaehlterTitel.transform.GetChild(1).GetComponentInChildren<TMP_InputField>().text.Length == 0)
            return;
        Logging.log(Logging.LogType.Normal, "ContentCreationTerminalMosaik", "AddMosaik", "Neues Mosaik wird hinzugefügt.");
        string name = MosaikHinzufügen.transform.GetChild(1).GetComponent<TMP_InputField>().text;
        string url = MosaikHinzufügen.transform.GetChild(2).GetComponent<TMP_InputField>().text;

        MosaikHinzufügen.transform.GetChild(0).GetComponent<Image>().sprite = null;
        MosaikHinzufügen.transform.GetChild(1).GetComponent<TMP_InputField>().text = "";
        MosaikHinzufügen.transform.GetChild(2).GetComponent<TMP_InputField>().text = "";
        ImageVorschau.GetComponent<Image>().sprite = null;

        if (name.Length == 0 || url.Length == 0)
            return;

        string lines = File.ReadAllText(datapath + "/" + AusgewaehlterTitel.transform.GetChild(1).GetComponentInChildren<TMP_InputField>().text + ".txt");

        if (lines.Length > 1)
            lines += "\n- " + name + " [!#!] "+ url;
        else
            lines += "- " + name + " [!#!] " + url;

        File.WriteAllText(datapath + "/" + AusgewaehlterTitel.transform.GetChild(1).GetComponentInChildren<TMP_InputField>().text + ".txt", lines);

        ReloadMosaike();
    }
    /// <summary>
    /// Leert die MosaikAnzeige
    /// </summary>
    public void ClearFragenAnzeige()
    {
        AusgewaehlterTitel.transform.GetChild(0).GetComponentInChildren<TMP_Text>().text = "";
        AusgewaehlterTitel.transform.GetChild(1).GetComponentInChildren<TMP_InputField>().text = "";

        MosaikHinzufügen.transform.GetChild(0).GetComponent<Image>().sprite = null;
        MosaikHinzufügen.transform.GetChild(1).GetComponent<TMP_InputField>().text = "";
        MosaikHinzufügen.transform.GetChild(2).GetComponent<TMP_InputField>().text = "";

        mosaike = null;
        for (int i = 0; i < ScrollMosaikContent.transform.childCount; i++)
        {
            ScrollMosaikContent.transform.GetChild(i).gameObject.SetActive(false);
        }
    }
    /// <summary>
    /// Aktualisiert Name & Url und Image von Mosaiken aus der Gameliste
    /// </summary>
    /// <param name="Frage"></param>
    public void ChangeFrageText(GameObject Frage)
    {
        int index = 49 - Int32.Parse(Frage.name.Replace("Mosaik (", "").Replace(")", ""));

        mosaike.getNames()[index] = Frage.transform.GetChild(1).GetComponent<TMP_InputField>().text;
        mosaike.getURLs()[index] = Frage.transform.GetChild(2).GetComponent<TMP_InputField>().text;

        if (reloadImages != null)
        {
            StopCoroutine(reloadImages);
            reloadImages = null;
        }
        reloadImages = StartCoroutine(ReloadImages());

        string lines = "";
        for (int i = 0; i < mosaike.getNames().Count; i++)
        {
            lines += "\n- " + mosaike.getNames()[i] + " [!#!] " + mosaike.getURLs()[i];
        }
        if (lines.Length > 2)
            lines = lines.Substring("\n".Length);

        File.WriteAllText(datapath + "/" + AusgewaehlterTitel.transform.GetChild(1).GetComponentInChildren<TMP_InputField>().text + ".txt", lines);
    }
    /// <summary>
    /// Lädt das Image das hinzugefügt werden soll, in die Vorschau
    /// </summary>
    public void LoadImageIntoPreview()
    {
        if (MosaikHinzufügen.transform.GetChild(2).GetComponent<TMP_InputField>().text.Length == 0)
            return;
        if (loadIntoPreview != null)
        {
            StopCoroutine(loadIntoPreview);
            loadIntoPreview = null;
        }
        loadIntoPreview = StartCoroutine(LoadImageIntoPreview(MosaikHinzufügen.transform.GetChild(2).GetComponent<TMP_InputField>().text));
    }
}
