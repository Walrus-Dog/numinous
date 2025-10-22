using System;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(SaveableEntity))]
[DisallowMultipleComponent]
public class SettingsMenuSaveAdapter : MonoBehaviour, ISaveable
{
    [Serializable]
    public struct State
    {
        public int graphicsQuality;
        public float masterVol;
        public float musicVol;
        public float sfxVol;
        public bool vibrate;
        public float brightness;   // postExposure slider value
        public float sensitivity;  // mouse sensitivity slider value
    }

    private SettingsMenuManager mgr;

    private void Awake()
    {
        mgr = GetComponent<SettingsMenuManager>();
        if (mgr == null)
            Debug.LogWarning("[SettingsMenuSaveAdapter] SettingsMenuManager not found on same GameObject.");
    }

    public object CaptureState()
    {
        if (mgr == null) return null;

        var st = new State
        {
            graphicsQuality = QualitySettings.GetQualityLevel(),
            masterVol = TryGetMixerFloat(mgr.MainMixer, "MasterVolume", PlayerPrefs.GetFloat("MasterVolume", 0.75f)),
            musicVol = TryGetMixerFloat(mgr.MainMixer, "MusicVolume", PlayerPrefs.GetFloat("MusicVolume", 0.75f)),
            sfxVol = TryGetMixerFloat(mgr.MainMixer, "SFXVolume", PlayerPrefs.GetFloat("SFXVolume", 0.75f)),
            vibrate = SettingsMenuManager.isVibrate,
            brightness = PlayerPrefs.GetFloat("BrightnessValue", 0f),
            sensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 1f)
        };
        return st;
    }

    public void RestoreState(object state)
    {
        if (mgr == null || state == null) return;

        var s = (State)state;

        // Graphics
        if (mgr.graphicsDropdown != null)
        {
            mgr.graphicsDropdown.value = Mathf.Clamp(s.graphicsQuality, 0, QualitySettings.names.Length - 1);
            mgr.ChangeGraphicsQuality();
        }
        else
        {
            QualitySettings.SetQualityLevel(Mathf.Clamp(s.graphicsQuality, 0, QualitySettings.names.Length - 1));
            PlayerPrefs.SetInt("GraphicsQuality", s.graphicsQuality);
        }

        // Audio
        if (mgr.masterVol != null) { mgr.masterVol.value = s.masterVol; mgr.ChangeMasterVolume(); }
        else SetMixerAndPref(mgr.MainMixer, "MasterVolume", s.masterVol, "MasterVolume");

        if (mgr.musicVol != null) { mgr.musicVol.value = s.musicVol; mgr.ChangeMusicVolume(); }
        else SetMixerAndPref(mgr.MainMixer, "MusicVolume", s.musicVol, "MusicVolume");

        if (mgr.sfxVol != null) { mgr.sfxVol.value = s.sfxVol; mgr.ChangeSFXVolume(); }
        else SetMixerAndPref(mgr.MainMixer, "SFXVolume", s.sfxVol, "SFXVolume");

        // Vibrate
        if (mgr.vibrateToggle != null)
        {
            mgr.vibrateToggle.isOn = s.vibrate;
            mgr.ChangeVibrate();
        }
        else
        {
            SettingsMenuManager.isVibrate = s.vibrate;
            PlayerPrefs.SetInt("Vibrate", s.vibrate ? 1 : 0);
        }

        // Brightness
        mgr.SetBrightness(s.brightness);

        // Sensitivity
        var sliderField = typeof(SettingsMenuManager)
            .GetField("sensitivitySlider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var sensSlider = sliderField != null ? sliderField.GetValue(mgr) as UnityEngine.UI.Slider : null;

        if (sensSlider != null)
        {
            sensSlider.value = s.sensitivity;
            mgr.ChangeSensitivity();
        }
        else
        {
            PlayerPrefs.SetFloat("MouseSensitivity", s.sensitivity);
            PlayerPrefs.Save();
            var player = UnityEngine.Object.FindFirstObjectByType<Player>();
            if (player != null) player.SetSensitivity(s.sensitivity);
        }
    }

    private static float TryGetMixerFloat(AudioMixer mixer, string param, float fallback)
    {
        if (mixer != null && mixer.GetFloat(param, out float v)) return v;
        return fallback;
    }

    private static void SetMixerAndPref(AudioMixer mixer, string param, float value, string prefKey)
    {
        if (mixer != null) mixer.SetFloat(param, value);
        PlayerPrefs.SetFloat(prefKey, value);
    }
}
