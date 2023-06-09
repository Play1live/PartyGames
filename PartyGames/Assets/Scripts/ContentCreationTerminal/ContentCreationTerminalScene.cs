using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ContentCreationTerminalScene : MonoBehaviour
{
    [SerializeField] GameObject QuizSettings;
    [SerializeField] GameObject SpieleChilds;

    [SerializeField] GameObject Einstellungen;
    [SerializeField] AudioMixer audiomixer;

    void Start()
    {
        Utils.EinstellungenStartSzene(Einstellungen, audiomixer, Utils.EinstellungsKategorien.Audio, Utils.EinstellungsKategorien.Grafik);
        Utils.EinstellungenGrafikApply(false);

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
    /// <summary>
    /// Aktualisiert die Screen Resolution für den Einzelspieler
    /// </summary>
    /// <param name="drop"></param>
    public void UpdateScreenResolution(TMP_Dropdown drop)
    {
        Config.APPLICATION_CONFIG.SetInt("GAME_DISPLAY_RESOLUTION", drop.value);
        Utils.EinstellungenGrafikApply(false);
    }
    /// <summary>
    /// Aktualisiert die Vollbildeinstellung für den Einzelspieler
    /// </summary>
    /// <param name="toggle"></param>
    public void UpdateFullscreen(Toggle toggle)
    {
        Config.APPLICATION_CONFIG.SetBool("GAME_DISPLAY_FULLSCREEN", toggle.isOn);
        Utils.EinstellungenGrafikApply(false);
    }
}
