using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class QuizScene : MonoBehaviour
{
    [SerializeField] GameObject IntroGO;
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
        if (!Config.CLIENT_STARTED && !Config.SERVER_STARTED)
            SceneManager.LoadScene("Startup");
    }

    void OnEnable()
    {
        Utils.EinstellungenStartSzene(Einstellungen, audiomixer, Utils.EinstellungsKategorien.Audio);
        Utils.EinstellungenGrafikApply(true);

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
        Config.APPLICATION_CONFIG.Save();
    }


    /// <summary>
    /// Spielt die Introanimation ab
    /// </summary>
    IEnumerator IntroAnimation()
    {
        Logging.log(Logging.LogType.Debug, "QuizScene", "IntroAnimation", "Spiele Introanimation ab.");
        IntroSound.Play();
        IntroGO.SetActive(true);

        //Wait for 10 secs.
        yield return new WaitForSeconds(10);
        IntroGO.SetActive(false);
    }
}
