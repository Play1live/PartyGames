
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class Utils
{
    #region Einstellungsmenü
    #region Allgemeine Kategorie Einstellungen
    public enum EinstellungsKategorien
    {
        ContentObject,
        Audio,
        Grafik,
        Sonstiges,
        Server
    }
    /// <summary>
    /// Blendet beliebige Kategorien ein und aktualisiert deren Werte
    /// </summary>
    /// <param name="EinstellungsParent"></param>
    /// <param name="audiomixer"></param>
    /// <param name="kategorien"></param>
    public static void EinstellungenStartSzene(GameObject EinstellungsParent, AudioMixer audiomixer, params EinstellungsKategorien[] kategorien)
    {
        EinstellungenDeaktiviereAlle(EinstellungsParent);
        for (int i = 0; i < kategorien.Length; i++)
        {
            EinstellungenAktiviere(EinstellungsParent, kategorien[i], true);
        }

        EinstellungenGrafikUpdate(EinstellungsParent);
        EinstellungenAudioUpdateVolume(EinstellungsParent, audiomixer);
        EinstellungenVersionUpdate(EinstellungsParent);
    }
    /// <summary>
    /// Blendet beliebig viele Kategorien ein, die nicht genannten, werden ausgeblendet
    /// </summary>
    /// <param name="EinstellungsParent"></param>
    /// <param name="kategorien"></param>
    public static void EinstellungenToggle(GameObject EinstellungsParent, params EinstellungsKategorien[] kategorien)
    {
        EinstellungenDeaktiviereAlle(EinstellungsParent);
        for (int i = 0; i < kategorien.Length; i++)
        {
            EinstellungenAktiviere(EinstellungsParent, kategorien[i], true);
        }
    }
    /// <summary>
    /// Gibt mit dem Einstellungs Gameobject das jeweilig passende für die Kategorie zurück
    /// </summary>
    /// <param name="EinstellungsParent"></param>
    /// <param name="kategorie"></param>
    /// <returns></returns>
    private static GameObject EinstellungenGetKategorie(GameObject EinstellungsParent, EinstellungsKategorien kategorie)
    {
        Transform EinstellungsContent = EinstellungsParent.transform.GetChild(2).GetChild(3).GetChild(0).GetChild(0);
        switch (kategorie)
        {
            default:
                Logging.log(Logging.LogType.Error, "Utils_Einstellungen", "EinstellungsKategorie", "Unbekannte Kategorie.");
                return null;
            case EinstellungsKategorien.ContentObject:
                return EinstellungsContent.gameObject;
            case EinstellungsKategorien.Audio:
                return EinstellungsContent.GetChild(0).gameObject;
            case EinstellungsKategorien.Grafik:
                return EinstellungsContent.GetChild(1).gameObject;
            case EinstellungsKategorien.Sonstiges:
                return EinstellungsContent.GetChild(2).gameObject;
            case EinstellungsKategorien.Server:
                return EinstellungsContent.GetChild(3).gameObject;
        }
    }
    /// <summary>
    /// Deaktiviert alle Kategorien
    /// </summary>
    /// <param name="EinstellungsParent"></param>
    private static void EinstellungenDeaktiviereAlle(GameObject EinstellungsParent)
    {
        GameObject content = EinstellungenGetKategorie(EinstellungsParent, EinstellungsKategorien.ContentObject);
        for (int i = 0; i < content.transform.childCount; i++)
            content.transform.GetChild(i).gameObject.SetActive(false);
    }
    /// <summary>
    /// Aktiviert/Deaktiviert bestimmte Kategorien
    /// </summary>
    /// <param name="EinstellungsParent"></param>
    /// <param name="kategorie"></param>
    /// <param name="toggle"></param>
    public static void EinstellungenAktiviere(GameObject EinstellungsParent, EinstellungsKategorien kategorie, bool toggle)
    {
        EinstellungenGetKategorie(EinstellungsParent, kategorie).SetActive(toggle);
    }
    #endregion
    #region weitere Einstellungen
    /// <summary>
    /// Aktualisiert die Angezeigte Version
    /// </summary>
    /// <param name="EinstellungsParent"></param>
    public static void EinstellungenVersionUpdate(GameObject EinstellungsParent)
    {
        EinstellungsParent.transform.GetChild(2).GetChild(1).GetComponent<TMP_Text>().text = "v" + Config.APPLICATION_VERSION;
    }
    #endregion
    #region Audio Einstellungen
    /// <summary>
    /// Aktualisiert die Audiovolume Anzeige
    /// </summary>
    /// <param name="EinstellungsParent"></param>
    /// <param name="audiomixer"></param>
    public static void EinstellungenAudioUpdateVolume(GameObject EinstellungsParent, AudioMixer audiomixer)
    {
        Transform AudioContent = EinstellungenGetKategorie(EinstellungsParent, EinstellungsKategorien.Audio).transform;

        float master = Config.APPLICATION_CONFIG.GetFloat("GAME_MASTER_VOLUME", 0f);
        audiomixer.SetFloat("MASTER", master);
        AudioContent.GetChild(1).GetChild(1).GetComponent<Slider>().value = master / 10;
        AudioContent.GetChild(1).GetChild(1).GetChild(3).GetComponentInChildren<TMP_Text>().text = ((master * 3) + 100) + "%";

        float sfx = Config.APPLICATION_CONFIG.GetFloat("GAME_SFX_VOLUME", 0f);
        audiomixer.SetFloat("SFX", sfx);
        AudioContent.GetChild(2).GetChild(1).GetComponent<Slider>().value = sfx / 10;
        AudioContent.GetChild(2).GetChild(1).GetChild(3).GetComponentInChildren<TMP_Text>().text = ((sfx * 3) + 100) + "%";

        float bgm = Config.APPLICATION_CONFIG.GetFloat("GAME_BGM_VOLUME", 0f);
        audiomixer.SetFloat("BGM", bgm);
        AudioContent.GetChild(3).GetChild(1).GetComponent<Slider>().value = bgm / 10;
        AudioContent.GetChild(3).GetChild(1).GetChild(3).GetComponentInChildren<TMP_Text>().text = ((bgm * 3) + 100) + "%";
    }
    #endregion
    #region Server Einstellungen
    /// <summary>
    /// Aktualisiert die IP und Port Anzeige
    /// </summary>
    /// <param name="EinstellungsParent"></param>
    /// <param name="isServer"></param>
    /// <param name="ip"></param>
    /// <param name="port"></param>
    public static void EinstelungenServerUpdatePortIP(GameObject EinstellungsParent, bool isServer, string ip, string port)
    {
        Transform ServerContent = EinstellungenGetKategorie(EinstellungsParent, EinstellungsKategorien.Server).transform;

        ServerContent.GetChild(1).GetChild(1).GetComponent<Toggle>().isOn = isServer;
        ServerContent.GetChild(2).GetChild(1).GetChild(0).GetComponent<TMP_InputField>().text = ip;
        ServerContent.GetChild(3).GetChild(1).GetChild(0).GetComponent<TMP_InputField>().text = port;
    }
    /// <summary>
    /// Aktualisiert die NoIP Anzeige
    /// </summary>
    /// <param name="EinstellungsParent"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <param name="hostname"></param>
    public static void EinstellungenServerNoIpUpdate(GameObject EinstellungsParent, string username, string password, string hostname)
    {
        Transform ServerContent = EinstellungenGetKategorie(EinstellungsParent, EinstellungsKategorien.Server).transform;

        ServerContent.GetChild(5).GetChild(1).GetComponentInChildren<TMP_InputField>().text = username;
        ServerContent.GetChild(6).GetChild(1).GetComponentInChildren<TMP_InputField>().text = password;
        ServerContent.GetChild(7).GetChild(1).GetComponentInChildren<TMP_InputField>().text = hostname;
    }
    #endregion
    #region Grafik Einstellungen
    /// <summary>
    /// Aktualisiert die Anzeige für die Grafikeinstellungs
    /// </summary>
    /// <param name="EinstellungsParent"></param>
    public static void EinstellungenGrafikUpdate(GameObject EinstellungsParent)
    {
        Transform GrafikContent = EinstellungenGetKategorie(EinstellungsParent, EinstellungsKategorien.Grafik).transform;

        GrafikContent.GetChild(2).GetChild(1).GetComponent<TMP_Dropdown>().value = Config.APPLICATION_CONFIG.GetInt("GAME_DISPLAY_RESOLUTION", 2);
        GrafikContent.GetChild(3).GetChild(1).GetComponent<Toggle>().isOn = Config.APPLICATION_CONFIG.GetBool("GAME_DISPLAY_FULLSCREEN", true);
    }
    /// <summary>
    /// Wendet die Grafik Einstellungen an, mit der Bedingung ob vollbild pflicht ist
    /// </summary>
    public static void EinstellungenGrafikApply(bool ignoreSettings)
    {
        if (ignoreSettings == true)
        {
            if (Config.FULLSCREEN == true)
            {
                if (Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen)
                {
                    return;
                }
                else
                {
                    List<DisplayInfo> displays = new List<DisplayInfo>();
                    Screen.GetDisplayLayout(displays);
                    if (displays?.Count > 1) // don't bother running if only one display exists...
                    {
                        Screen.MoveMainWindowTo(displays[0], new Vector2Int(displays[0].width / 2, displays[0].height / 2));
                    }

                    Screen.SetResolution(Display.displays[0].systemWidth, Display.displays[0].systemWidth, true);
                    Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                    
                    return;
                }
            }
        }

        string[] ress = new string[] { "2560x1440", "1920x1080", "1280x720" };
        int width = Int32.Parse(ress[Config.APPLICATION_CONFIG.GetInt("GAME_DISPLAY_RESOLUTION", 2)].Split('x')[0]);
        int height = Int32.Parse(ress[Config.APPLICATION_CONFIG.GetInt("GAME_DISPLAY_RESOLUTION", 2)].Split('x')[1]);
        bool full = Config.APPLICATION_CONFIG.GetBool("GAME_DISPLAY_FULLSCREEN", true);

        if (full)
        {
            if (Screen.fullScreenMode != FullScreenMode.ExclusiveFullScreen)
            {
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                Screen.SetResolution(Display.displays[0].systemWidth, Display.displays[0].systemWidth, true);
            }
        }
        else
        {
            if (Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen)
                Screen.fullScreenMode = FullScreenMode.Windowed;
            Screen.SetResolution(width, height, false);
        }
    }
    #endregion
    #endregion
}
