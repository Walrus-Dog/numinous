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
        public float masterVol;   // linear 0..1
        public float musicVol;    // linear 0..1
        public float sfxVol;      // linear 0..1
        public bool vibrate;
        public float brightness;  // postExposure slider value
        public float sensitivity; // mouse sensitivity slider value
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

            // IMPORTANT: use the same linear [0..1] values as SettingsMenuManager
            masterVol = PlayerPrefs.GetFloat("MasterVolume", 0.75f),
            musicVol = PlayerPrefs.GetFloat("MusicVolume", 0.75f),
            sfxVol = PlayerPrefs.GetFloat("SFXVolume", 0.75f),

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

        // -------- GRAPHICS --------
        int clampedQuality = Mathf.Clamp(s.graphicsQuality, 0, QualitySettings.names.Length - 1);

        if (mgr.graphicsDropdown != null)
        {
            mgr.graphicsDropdown.value = clampedQuality;
            mgr.ChangeGraphicsQuality();
        }
        else
        {
            QualitySettings.SetQualityLevel(clampedQuality);
            PlayerPrefs.SetInt("GraphicsQuality", clampedQuality);
        }

        // -------- AUDIO (linear 0..1) --------
        if (mgr.masterVol != null)
        {
            mgr.masterVol.value = Mathf.Clamp01(s.masterVol);
            mgr.ChangeMasterVolume();
        }
        else
        {
            SetMixerAndPref(mgr.MainMixer, "MasterVolume", s.masterVol, "MasterVolume");
        }

        if (mgr.musicVol != null)
        {
            mgr.musicVol.value = Mathf.Clamp01(s.musicVol);
            mgr.ChangeMusicVolume();
        }
        else
        {
            SetMixerAndPref(mgr.MainMixer, "MusicVolume", s.musicVol, "MusicVolume");
        }

        if (mgr.sfxVol != null)
        {
            mgr.sfxVol.value = Mathf.Clamp01(s.sfxVol);
            mgr.ChangeSFXVolume();
        }
        else
        {
            SetMixerAndPref(mgr.MainMixer, "SFXVolume", s.sfxVol, "SFXVolume");
        }

        // -------- VIBRATE --------
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

        // -------- BRIGHTNESS --------
        // SettingsMenuManager already handles mixer + overlay
        mgr.SetBrightness(s.brightness);

        // -------- SENSITIVITY --------
        // Try to drive the slider if it exists (it's private in SettingsMenuManager)
        var sliderField = typeof(SettingsMenuManager)
            .GetField("sensitivitySlider",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var sensSlider = sliderField != null
            ? sliderField.GetValue(mgr) as UnityEngine.UI.Slider
            : null;

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

    // ------- helpers -------

    private static float LinearToDb(float linear)
    {
        if (linear <= 0.0001f) return -80f;
        return Mathf.Log10(linear) * 20f;
    }

    private static void SetMixerAndPref(AudioMixer mixer, string param, float linearValue, string prefKey)
    {
        float lin = Mathf.Clamp01(linearValue);

        if (mixer != null)
            mixer.SetFloat(param, LinearToDb(lin));

        PlayerPrefs.SetFloat(prefKey, lin);
        PlayerPrefs.Save();
    }
}
