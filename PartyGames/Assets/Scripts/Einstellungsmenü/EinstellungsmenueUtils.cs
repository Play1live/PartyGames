using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

public class EinstellungsmenueUtils : MonoBehaviour
{
    public static bool blockDebugMode = false;
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
}
