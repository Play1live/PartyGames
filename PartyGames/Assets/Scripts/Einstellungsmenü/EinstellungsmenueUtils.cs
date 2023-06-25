using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EinstellungsmenueUtils : MonoBehaviour
{
    public bool ForceFullscreen;

    public static bool blockDebugMode;
    public void DebugModeChange(Toggle toggle)
    {
        if (blockDebugMode)
            return;

        Config.APPLICATION_CONFIG.SetBool("APPLICATION_DEBUGMODE", toggle.isOn);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Automatisierter Neustart
        Process game = new Process();
        game.StartInfo.FileName = Application.dataPath.Replace("\\PartyGames_Data", "").Replace("/PartyGames_Data", "") + "/" + Application.productName + ".exe";        
        game.Start();
        Application.Quit();
#endif

    }

    /// <summary>
    /// Aktualisiert die Screen Resolution für den Einzelspieler
    /// </summary>
    /// <param name="drop"></param>
    public void UpdateScreenResolution(TMP_Dropdown drop)
    {
        Config.APPLICATION_CONFIG.SetInt("GAME_DISPLAY_RESOLUTION", drop.value);
        Utils.EinstellungenGrafikApply(ForceFullscreen);
    }
    /// <summary>
    /// Aktualisiert die Vollbildeinstellung für den Einzelspieler
    /// </summary>
    /// <param name="toggle"></param>
    public void UpdateFullscreen(Toggle toggle)
    {
        Config.APPLICATION_CONFIG.SetBool("GAME_DISPLAY_FULLSCREEN", toggle.isOn);
        Utils.EinstellungenGrafikApply(ForceFullscreen);
    }
}
