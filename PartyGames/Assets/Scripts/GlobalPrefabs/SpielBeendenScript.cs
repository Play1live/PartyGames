using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SpielBeendenScript : MonoBehaviour
{
    /// <summary>
    /// Beendet das Spiel auf Button
    /// </summary>
    public void SpielBeenden()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
