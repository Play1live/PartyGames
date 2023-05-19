using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ContentCreationTerminalScene : MonoBehaviour
{
    [SerializeField] GameObject QuizSettings;
    [SerializeField] GameObject SpieleChilds;

    void Start()
    {
        ResizeScene();
        HideUnselectedChilds();
    }

    void Update()
    {
        
    }

    /// <summary>
    /// Blendet nicht genutzt Childs aus
    /// </summary>
    public void HideUnselectedChilds()
    {
        for (int i = 0; i < SpieleChilds.transform.childCount; i++)
        {
            SpieleChilds.transform.GetChild(i).gameObject.SetActive(false);
        }
    }
    /// <summary>
    /// Geht zurück zum Hauptmenü
    /// </summary>
    public void BackToHome()
    {
        SceneManager.LoadSceneAsync("Startup");
    }
    /// <summary>
    /// Ändert die Windowgröße
    /// </summary>
    private void ResizeScene()
    {
        if (Config.APPLICATION_CONFIG == null)
            return;

        bool fullscreen = Config.APPLICATION_CONFIG.GetBool("GAME_DISPLAY_FULLSCREEN", true);
        FullScreenMode mode;
        if (fullscreen)
        {
            mode = FullScreenMode.ExclusiveFullScreen;
            Screen.SetResolution(1920, 1080, mode);
            return;
        }

        mode = FullScreenMode.Windowed;
        int reso = Config.APPLICATION_CONFIG.GetInt("GAME_DISPLAY_RESOLUTION", 2);
        string[] res = new string[] { "2560x1440", "1920x1080", "1280x720" };
        int width = Int32.Parse(res[reso].Split('x')[0]);
        int height = Int32.Parse(res[reso].Split('x')[1]);
        Screen.SetResolution(width, height, mode);
    }
}
