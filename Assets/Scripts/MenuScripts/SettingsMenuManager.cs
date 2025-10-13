using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;

public class SettingsMenuManager : MonoBehaviour
{
    public static bool isVibrate;

    [Header("UI")]
    public TMP_Dropdown graphicsDropdown;
    public Slider masterVol, musicVol, sfxVol;
    public Toggle vibrateToggle;

    [Header("Audio")]
    public AudioMixer MainMixer;

    void Start()
    {
        // === LOAD SETTINGS/PLAYER PREFS ===
        graphicsDropdown.value = PlayerPrefs.GetInt("GraphicsQuality", QualitySettings.GetQualityLevel());
        QualitySettings.SetQualityLevel(graphicsDropdown.value);

        float masterValue = PlayerPrefs.GetFloat("MasterVolume", 0.75f);
        float musicValue = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
        float sfxValue = PlayerPrefs.GetFloat("SFXVolume", 0.75f);

        masterVol.value = masterValue;
        musicVol.value = musicValue;
        sfxVol.value = sfxValue;

        SetMixerVolume("MasterVolume", masterValue);
        SetMixerVolume("MusicVolume", musicValue);
        SetMixerVolume("SFXVolume", sfxValue);

        bool vibrateValue = PlayerPrefs.GetInt("Vibrate", 1) == 1;
        vibrateToggle.isOn = vibrateValue;
        isVibrate = vibrateValue;

        //--AddingEvents--
        graphicsDropdown.onValueChanged.AddListener(delegate { ChangeGraphicsQuality(); });
        masterVol.onValueChanged.AddListener(delegate { ChangeMasterVolume(); });
        musicVol.onValueChanged.AddListener(delegate { ChangeMusicVolume(); });
        sfxVol.onValueChanged.AddListener(delegate { ChangeSFXVolume(); });
        vibrateToggle.onValueChanged.AddListener(delegate { ChangeVibrate(); });
    }

    //=== CHANGE SETTINGS ===
    public void ChangeGraphicsQuality()
    {
        QualitySettings.SetQualityLevel(graphicsDropdown.value);
        PlayerPrefs.SetInt("GraphicsQuality", graphicsDropdown.value);
    }

    public void ChangeMasterVolume()
    {
        SetMixerVolume("MasterVolume", masterVol.value);
        PlayerPrefs.SetFloat("MasterVolume", masterVol.value);
    }

    public void ChangeMusicVolume()
    {
        SetMixerVolume("MusicVolume", musicVol.value);
        PlayerPrefs.SetFloat("MusicVolume", musicVol.value);
    }

    public void ChangeSFXVolume()
    {
        SetMixerVolume("SFXVolume", sfxVol.value);
        PlayerPrefs.SetFloat("SFXVolume", sfxVol.value);
    }

    public void ChangeVibrate()
    {
        isVibrate = vibrateToggle.isOn;
        PlayerPrefs.SetInt("Vibrate", isVibrate ? 1 : 0);
    }

    // === HELPER ===
    private void SetMixerVolume(string parameter, float value)
    {
        // Slider is already in dB
        MainMixer.SetFloat(parameter, value);
    }
}
