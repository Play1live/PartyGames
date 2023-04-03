using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using TMPro;
using Unity.RemoteConfig;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    public struct userAttributes { }
    public struct appAttriutes { }

    public string url = "";
    public string version = "";
    public string path = "";

    [SerializeField] TMP_Text type;
    [SerializeField] Slider slider;

    void Awake()
    {
        path = Application.dataPath.Replace("\\PartyGamesUpdater_Data", "").Replace("/PartyGamesUpdater_Data", "");
        FetchRemoteConfig();
    }
    void Start()
    {
        UnityEngine.Debug.Log(Application.dataPath);
        UnityEngine.Debug.Log(Application.persistentDataPath);

        GameObject.Find("UpdaterVersion").GetComponent<TMP_Text>().text = "Version: " + Application.version;
    }
    void Update()
    {
        
    }

    private void OnApplicationQuit()
    {
        StopCoroutine(Download());
        type.text = "Wird beendet...";
    }

    public void FetchRemoteConfig()
    {
        ConfigManager.FetchCompleted += ApplyRemoteSettings;
        ConfigManager.FetchConfigs<userAttributes, appAttriutes>(new userAttributes(), new appAttriutes());
    }

    private void ApplyRemoteSettings(ConfigResponse config)
    {
        url = ConfigManager.appConfig.GetString("Program_Download_Link");
        version = ConfigManager.appConfig.GetString("Program_Download_Version");

        // Schiebt das Programm auf den Primären Monitor
        List<DisplayInfo> displays = new List<DisplayInfo>();
        Screen.GetDisplayLayout(displays);
        if (displays?.Count > 1) // don't bother running if only one display exists...
        {
            Screen.MoveMainWindowTo(displays[0], new Vector2Int(displays[0].width / 2, displays[0].height / 2));
        }

        // Prüft ob bereits das neuste Update installiert ist
        string line;
        if (File.Exists(path + @"/Files/Version.txt"))
            try
            {
                line = File.ReadAllLines(path + @"/Files/Version.txt")[0];
            }
            catch (Exception e)
            {
                line = "0";
            }
        else
            line = "0";
        // Version stimmt überein
        if (version == line)
        {
            type.text = "Spiel wird gestartet...";
            slider.value = 1;
            Process game = new Process();
            game.StartInfo.FileName = path + @"/Files/PartyGames.exe";
            game.Start();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
         Application.Quit();
#endif
            return;
        }

        // alte dateien löschen
        if (Directory.Exists(path + @"/Files/"))
            Directory.Delete(path + @"/Files/", true);
        Directory.CreateDirectory(path + @"/Files/");
        StartCoroutine(Download());
    }

    IEnumerator Download()
    {
        type.text = "Spiel wird heruntergeladen...";
        slider.value = 0;
        UnityWebRequest dlreq = new UnityWebRequest(url);
        dlreq.downloadHandler = new DownloadHandlerFile(path + @"/Files/Game.zip");
        UnityWebRequestAsyncOperation op = dlreq.SendWebRequest();
        while (!op.isDone)
        {
            //here you can see download progress
            slider.value = dlreq.downloadProgress;
            yield return null;
        }
        if (dlreq.isNetworkError || dlreq.isHttpError)
        {
            UnityEngine.Debug.LogError(dlreq.error);
            type.text = "Download ist fehlgeschlagen!";
        }
        else
        {
            type.text = "Download war erfolgreich!";
        }
        dlreq.Dispose();

        yield return new WaitForSeconds(0.2f);

        type.text = "Dateien werden entpackt...";
        slider.value = 0;
        while (Directory.GetFiles(path + @"/Files/").Length == 1)
        {
            slider.value = slider.value + 0.005f;
            if (slider.value >= 0.8f)
                ZipFile.ExtractToDirectory(path + @"/Files/Game.zip", path + @"/Files/");
            yield return null;
        }
        slider.value = 1;
        type.text = "Dateien wurden erfolgreich entpackt!";

        yield return new WaitForSeconds(0.2f);

        type.text = "Spiel wird gestartet...";
        slider.value = 0;
        while (slider.value < 0.95f)
        {
            slider.value = slider.value + 0.005f;
            yield return null;
        }
        Process game = new Process();
        game.StartInfo.FileName = path + @"/Files/PartyGames.exe";
        game.Start();

        type.text = "Spiel wurde gestartet!";
        slider.value = 1;
        yield return new WaitForSeconds(5);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
         Application.Quit();
#endif

        yield return null;
    }

}
