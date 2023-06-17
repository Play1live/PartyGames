using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AuktionScene : MonoBehaviour
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
    /// Starte die Introanimation
    /// </summary>
    /// <returns></returns>
    IEnumerator IntroAnimation()
    {
        IntroSound.Play();
        IntroGo.SetActive(true);

        //Wait for 10 secs.
        yield return new WaitForSeconds(10);

        IntroGo.SetActive(false);
    }
}