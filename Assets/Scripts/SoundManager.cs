using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    #region Variables
    [SerializeField] private AudioMixer mixer;
    #endregion

    #region Public Methods
    public void SetMusicVolume(float volume)
    {
        float mixerVolume = Mathf.Log10(volume / 100 + 0.001f) * 20;
        mixer.SetFloat("musicVolume", mixerVolume);
        PlayerPrefs.SetFloat("musicVolume", volume);
    }

    public void SetSFXVolume(float volume)
    {
        float mixerVolume = Mathf.Log10(volume / 100 + 0.001f) * 20;
        mixer.SetFloat("sfxVolume", mixerVolume);
        PlayerPrefs.SetFloat("sfxVolume", volume);
    }
    #endregion

    private void Start()
    {
        if (PlayerPrefs.HasKey("musicVolume"))
        {
            SetMusicVolume(PlayerPrefs.GetFloat("musicVolume"));
        }
        if (PlayerPrefs.HasKey("sfxVolume"))
        {
            SetSFXVolume(PlayerPrefs.GetFloat("sfxVolume"));
        }
    }
}