using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using TMPro;
using Unity.RemoteConfig;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    private struct userAttributes { }
    private struct appAttriutes { }

    private string DownloadUrl = "";
    private string DownloadVersion = "";
    private string DownloadPath = "";

    private string InstalledVersion = "";

    [SerializeField] TMP_Text type;
    [SerializeField] Slider slider;

    private bool quitApp = false;

    void Awake()
    {
        DownloadPath = Application.dataPath.Replace("\\PartyGamesUpdater_Data", "").Replace("/PartyGamesUpdater_Data", "");
    }
    void Update()
    {
        if (quitApp == true)
        {
            StopCoroutine(Download());

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
         Application.Quit();
#endif
        }
    }
    void Start()
    {
        WriteGameVersionFile();
		FetchRemoteConfig();
        UnityEngine.Debug.Log(Application.dataPath);
        UnityEngine.Debug.Log(Application.persistentDataPath);

        GameObject.Find("UpdaterVersion").GetComponent<TMP_Text>().text = "Version: " + Application.version;
    }

    private void OnApplicationQuit()
    {
        UnityEngine.Debug.Log("Updater wird beendet.");
        StopCoroutine(Download());
        type.text = "Wird beendet...";
    }

    public void FetchRemoteConfig()
    {
        UnityEngine.Debug.Log("Fetching Config");
        ConfigManager.FetchCompleted += ApplyRemoteSettings;
        ConfigManager.FetchConfigs<userAttributes, appAttriutes>(new userAttributes(), new appAttriutes());
    }

    private void ApplyRemoteSettings(ConfigResponse config)
    {
        DownloadUrl = ConfigManager.appConfig.GetString("Program_Download_Link");
        DownloadVersion = ConfigManager.appConfig.GetString("Program_Download_Version");

        this.DownloadUrl = this.DownloadUrl.Replace("<version>", DownloadVersion);

        MoveWindowToPrimary();
        LoadGameVersion();
        if (DownloadVersion == InstalledVersion)
        {
            #region StartGame
            UnityEngine.Debug.Log("Spiel wird gestartet.");
            type.text = "Spiel wird gestartet...";
            slider.value = 1;
            Process game = new Process();
            game.StartInfo.FileName = DownloadPath + @"/Files/PartyGames.exe";
            game.Start();
            #endregion
            quitApp = true;
            return;
        }
        DeleteOldFiles();
        
        StartCoroutine(Download());
    }

    IEnumerator Download()
    {
        type.text = "Spiel wird heruntergeladen...";
        slider.value = 0;
        UnityWebRequest dlreq = new UnityWebRequest(DownloadUrl);
        dlreq.downloadHandler = new DownloadHandlerFile(DownloadPath + @"/Files/Game.zip");
        UnityWebRequestAsyncOperation op = dlreq.SendWebRequest();
        while (!op.isDone)
        {
            slider.value = dlreq.downloadProgress;
            yield return null;
        }
        if (dlreq.result.Equals(dlreq.error))
        {
            UnityEngine.Debug.LogError(dlreq.error);
            type.text = "Download ist fehlgeschlagen!";

            #region QuitGame
            yield return new WaitForSeconds(0.1f);
            quitApp = true;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
         Application.Quit();
#endif
            #endregion
            yield break;
        }
        else
        {
            type.text = "Download war erfolgreich!";
        }
        dlreq.Dispose();

        yield return new WaitForSeconds(0.2f);
        #region Unzip File
        type.text = "Dateien werden entpackt...";
        slider.value = 0;
        while (Directory.GetFiles(DownloadPath + @"/Files/").Length == 1)
        {
            slider.value = slider.value + 0.01f;
            if (slider.value >= 0.8f)
                ZipFile.ExtractToDirectory(DownloadPath + @"/Files/Game.zip", DownloadPath + @"/Files/");
            yield return null;
        }
        slider.value = 1;
        type.text = "Dateien wurden erfolgreich entpackt!";
        #endregion
        yield return new WaitForSeconds(0.2f);
        #region StartGame
        type.text = "Spiel wird gestartet...";
        slider.value = 0;
        while (slider.value < 0.95f)
        {
            slider.value = slider.value + 0.01f;
            yield return null;
        }
        UnityEngine.Debug.Log("Spiel wird gestartet.");
        type.text = "Spiel wird gestartet...";
        slider.value = 1;
        Process game = new Process();
        game.StartInfo.FileName = DownloadPath + @"/Files/PartyGames.exe";
        game.Start();
        quitApp = true;
        #endregion
        #region QuitGame
        //yield return new WaitForSeconds(0.1f);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
         Application.Quit();
#endif
        #endregion
        yield return null;
    }
    
    private void DeleteOldFiles()
    {
        UnityEngine.Debug.Log("Alte Spieldateien werden gelöscht.");
        if (Directory.Exists(DownloadPath + @"/Files/"))
            Directory.Delete(DownloadPath + @"/Files/", true);
        Directory.CreateDirectory(DownloadPath + @"/Files/");
    }
    private void LoadGameVersion()
    {
        if (File.Exists(DownloadPath + @"/Files/Version.txt"))
            try
            {
                InstalledVersion = File.ReadAllLines(DownloadPath + @"/Files/Version.txt")[0];
            }
            catch
            {
                InstalledVersion = "0";
            }
        else
            InstalledVersion = "0";
        UnityEngine.Debug.Log("Version des installierten Spiels ist. Version:" + InstalledVersion);
    }
    private void MoveWindowToPrimary()
    {
        UnityEngine.Debug.Log("Updater wird auf den primären Bildschirm geschoben.");
        List<DisplayInfo> displays = new List<DisplayInfo>();
        Screen.GetDisplayLayout(displays);
        if (displays?.Count > 1) // don't bother running if only one display exists...
        {
            Screen.MoveMainWindowTo(displays[0], new Vector2Int(displays[0].width / 2, displays[0].height / 2));
        }
    }
    private void WriteGameVersionFile()
    {
        UnityEngine.Debug.Log("Updater Version wird in Datei geschrieben.");
        if (!File.Exists(Application.dataPath.Replace("\\PartyGamesUpdater_Data", "").Replace("/PartyGamesUpdater_Data", "") + @"/Version.txt"))
        {
            using (FileStream fs = File.Create(Application.dataPath.Replace("\\PartyGamesUpdater_Data", "").Replace("/PartyGamesUpdater_Data", "") + @"/Version.txt"))
            {
                byte[] info = new UTF8Encoding(true).GetBytes(Application.version);
                // Add some information to the file.
                fs.Write(info, 0, info.Length);
            }
        }
        else
        {
            string version = File.ReadAllText(Application.dataPath.Replace("\\PartyGamesUpdater_Data", "").Replace("/PartyGamesUpdater_Data", "") + @"/Version.txt");
            if (version != Application.version)
            {
                File.Delete(Application.dataPath.Replace("\\PartyGamesUpdater_Data", "").Replace("/PartyGamesUpdater_Data", "") + @"/Version.txt");
                WriteGameVersionFile();
            }
        }
    }
}
