using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSettings : MonoBehaviour
{
    public AudioMixer mixer;

    public void SetMASTER(Slider slider)
    {
        mixer.SetFloat("MASTER", slider.value * 10);
        slider.transform.GetChild(3).GetComponentInChildren<TMP_Text>().text = ((slider.value * 30) + 100) +"%";
        Config.APPLICATION_CONFIG.SetFloat("GAME_MASTER_VOLUME", slider.value * 10);
    }
    public void SetSFX(Slider slider)
    {
        mixer.SetFloat("SFX", slider.value * 10);
        slider.transform.GetChild(3).GetComponentInChildren<TMP_Text>().text = ((slider.value * 30) + 100) + "%";
        Config.APPLICATION_CONFIG.SetFloat("GAME_SFX_VOLUME", slider.value * 10);
    } 
    public void SetBGM(Slider slider)
    {
        mixer.SetFloat("BGM", slider.value * 10);
        slider.transform.GetChild(3).GetComponentInChildren<TMP_Text>().text = ((slider.value * 30) + 100) + "%";
        Config.APPLICATION_CONFIG.SetFloat("GAME_BGM_VOLUME", slider.value * 10);
    }

}
