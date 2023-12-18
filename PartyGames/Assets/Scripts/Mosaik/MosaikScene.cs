using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MosaikScene : MonoBehaviour
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
        Utils.LoadStartGameInitiations(GameObject.Find("Canvas/Background").GetComponent<Image>());
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

        //StartCoroutine(GetTexture());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (Config.APPLICATION_CONFIG != null)
            Config.APPLICATION_CONFIG.Save();
    }


    /// <summary>
    /// Spielt die IntroAnimation an
    /// </summary>
    IEnumerator IntroAnimation()
    {
        Logging.log(Logging.LogType.Debug, "MosaikScene", "IntroAnimation", "Spielt Introanimation ab");
        IntroSound.Play();
        IntroGo.SetActive(true);

        //Wait for 10 secs.
        yield return new WaitForSeconds(10);

        IntroGo.SetActive(false);
    }
    /// <summary>
    /// Lädt ein Image und blendet es im Objekt ein
    /// </summary>
    IEnumerator GetTexture()
    {
        // TEIL 1: Download des Bildes
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUrl);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
            // Handling, nachricht an server, der erneut befehl geben kann/ oder custom bild einblenden
        }
        else
        {
            // TEIL 2: Zeigt Bild in der Szene an und behält die Seitenverhältnisse bei
            #region Teil 2
            Texture2D myTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;
            Debug.LogError(myTexture.width+ " "+ myTexture.height);
            Sprite sprite = Sprite.Create(myTexture, new Rect(0, 0, myTexture.width, myTexture.height), new Vector2(0.5f, 0.5f), 100);
            imageObject.GetComponent<Image>().sprite = sprite;

            // Skalierung des Bildes, um das Seitenverhältnis beizubehalten und um sicherzustellen, dass das Bild nicht größer als das Image ist
            float imageWidth = imageObject.GetComponent<RectTransform>().rect.width;
            float imageHeight = imageObject.GetComponent<RectTransform>().rect.height;
            float textureWidth = myTexture.width;
            float textureHeight = myTexture.height;
            float widthRatio = imageWidth / textureWidth;
            float heightRatio = imageHeight / textureHeight;
            float ratio = Mathf.Min(widthRatio, heightRatio);
            float newWidth = textureWidth * ratio;
            float newHeight = textureHeight * ratio;

            // Anpassung der Größe des Image-GameObjects und des Sprite-Components
            RectTransform imageRectTransform = imageObject.GetComponent<RectTransform>();
            imageRectTransform.sizeDelta = new Vector2(newWidth, newHeight);
            imageObject.GetComponent<Image>().sprite = sprite;
            #endregion
            // TEIL 3: Passt die Überlagerten Images an die größe an
            #region Teil 3
            float cellWidth = newWidth / 7;
            float cellHeight = newHeight / 7;
            imageObject.GetComponent<GridLayoutGroup>().cellSize = new Vector2(cellWidth, cellHeight);
            #endregion
            // TEIL 4: Überlagerten Images muster geben
            #region Teil 4
            string[] himmelrichtungen = new string[] { "E", "N", "NE", "NW", "S", "SE", "SW", "W" };
            for (int i = 0; i < 49; i++)
            {
                int random = Random.Range(0, himmelrichtungen.Length);
                imageObject.transform.GetChild(i).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/GUI/Arrow "+ himmelrichtungen[random]);
            }
            #endregion
        }
    }

    public string imageUrl = "https://forum.unity.com/styles/UnitySkin/xenforo/avatars/avatar_m.png"; // URL des Bildes hier einfügen
    public GameObject imageObject;
}
