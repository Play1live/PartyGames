using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class KniffelScene : MonoBehaviour
{
    [SerializeField] GameObject IntroGo;
    [SerializeField] AudioSource IntroSound;

    [SerializeField] GameObject Client;
    [SerializeField] GameObject Server;

    [SerializeField] GameObject[] ServerSided;
    [SerializeField] GameObject[] DeactivateForServer;
    [SerializeField] GameObject[] DeactivateForClient;

    [SerializeField] GameObject Einstellungen;
    [SerializeField] AudioMixer audiomixer;

    private void Start()
    {
        Application.targetFrameRate = 120;
#if UNITY_EDITOR
        Application.targetFrameRate = 200;
#endif
    }

    void OnEnable()
    {
        if (!Config.APPLICATION_INITED)
        {
            SceneManager.LoadScene("Startup");
            return;
        }
        Utils.EinstellungenStartSzene(Einstellungen, audiomixer, Utils.EinstellungsKategorien.Audio, Utils.EinstellungsKategorien.Grafik);
        Utils.EinstellungenGrafikApply(false);

        if (Config.isServer)
        {
            Client.SetActive(false);
            Server.SetActive(true);
            foreach (GameObject go in ServerSided)
                go.SetActive(true);
            foreach (GameObject go in DeactivateForServer)
                go.SetActive(false);
        }
        else
        {
            Server.SetActive(false);
            Client.SetActive(true);
            foreach (GameObject go in ServerSided)
                go.SetActive(false);
            foreach (GameObject go in DeactivateForClient)
                go.SetActive(false);
        }

        StartCoroutine(IntroAnimation());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (Config.APPLICATION_CONFIG != null)
            Config.APPLICATION_CONFIG.Save();
    }

    /// <summary>
    /// Spielt die Introanimation ab
    /// </summary>
    IEnumerator IntroAnimation()
    {
        Logging.log(Logging.LogType.Debug, "KniffelScene", "IntroAnimation", "Spielt die Introanimation ab");
        IntroSound.Play();
        IntroGo.SetActive(true);

        //Wait for 10 secs.
        yield return new WaitForSeconds(10);

        IntroGo.SetActive(false);
    }
    /// <summary>
    /// Aktualisiert die Screen Resolution f�r den Einzelspieler
    /// </summary>
    /// <param name="drop"></param>
    public void UpdateScreenResolution(TMP_Dropdown drop)
    {
        Config.APPLICATION_CONFIG.SetInt("GAME_DISPLAY_RESOLUTION", drop.value);
        Utils.EinstellungenGrafikApply(false);
    }
    /// <summary>
    /// Aktualisiert die Vollbildeinstellung f�r den Einzelspieler
    /// </summary>
    /// <param name="toggle"></param>
    public void UpdateFullscreen(Toggle toggle)
    {
        Config.APPLICATION_CONFIG.SetBool("GAME_DISPLAY_FULLSCREEN", toggle.isOn);
        Utils.EinstellungenGrafikApply(false);
    }
}