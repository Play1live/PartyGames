using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class QuizScene : MonoBehaviour
{
    [SerializeField] GameObject IntroGO;
    [SerializeField] AudioSource IntroSound;

    [SerializeField] GameObject Client;
    [SerializeField] GameObject Server;
    [SerializeField] GameObject[] ServerSided;
    [SerializeField] GameObject[] DeactivateForServer;
    [SerializeField] GameObject[] DeactivateForClient;

    private void Start()
    {
        Application.targetFrameRate = 60;
#if UNITY_EDITOR
        Application.targetFrameRate = 120;
#endif
    }

    void OnEnable()
    {
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

    IEnumerator IntroAnimation()
    {
        IntroSound.Play();
        IntroGO.SetActive(true);

        //Wait for 10 secs.
        yield return new WaitForSeconds(10);

        IntroGO.SetActive(false);
    }

    void Update()
    {
        
    }

    private void OnApplicationQuit()
    {
        //Logging.add(Logging.Type.Normal, "StartupScene", "OnApplicationQuit", "Programm wird beendet");
        //MedienUtil.WriteLogsInDirectory();
    }
}
