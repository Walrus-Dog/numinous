using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class BrightnessController : MonoBehaviour
{
    private Volume globalVolume;
    private ColorAdjustments colorAdjustments;
    private Slider brightnessSlider;
    private Image uiOverlay; 

    private const string BrightnessKey = "BrightnessValue";

    void Start()
    {
        // --- 1. Find Global Volume ---
        globalVolume = Object.FindFirstObjectByType<Volume>();

        if (globalVolume == null)
        {
            Debug.LogError("No Global Volume found in scene!");
            return;
        }

        if (!globalVolume.profile.TryGet(out colorAdjustments))
        {
            Debug.LogError("No Color Adjustments override found in Global Volume!");
            return;
        }

        // --- 2. Find UI Slider ---
        brightnessSlider = GameObject.Find("BrightnessSlider")?.GetComponent<Slider>();
        if (brightnessSlider == null)
            brightnessSlider = Object.FindFirstObjectByType<Slider>();

        // --- 3. Find UI Overlay (The canvas in most cases, name it "UIBrightnessOverlay") ---
        GameObject overlayObj = GameObject.Find("UIBrightnessOverlay");
        if (overlayObj != null)
            uiOverlay = overlayObj.GetComponent<Image>();

        // --- 4. Load saved brightness ---
        float savedBrightness = PlayerPrefs.GetFloat(BrightnessKey, 0f);
        ApplyBrightness(savedBrightness);

        // --- 5. Hook up slider ---
        if (brightnessSlider != null)
        {
            brightnessSlider.minValue = -2f;
            brightnessSlider.maxValue = 2f;
            brightnessSlider.value = savedBrightness;
            brightnessSlider.onValueChanged.AddListener(SetBrightness);
        }
        else
        {
            Debug.LogWarning("No Slider found in the scene. Brightness can only be changed via script.");
        }
    }

    public void SetBrightness(float value)
    {
        ApplyBrightness(value);
        PlayerPrefs.SetFloat(BrightnessKey, value);
        PlayerPrefs.Save();
    }

    public void ResetBrightness()
    {
        SetBrightness(0f);
        if (brightnessSlider != null) brightnessSlider.value = 0f;
    }

    private void ApplyBrightness(float value)
    {
        // --- Game world brightness ---
        if (colorAdjustments != null)
            colorAdjustments.postExposure.value = value;

        // --- UI brightness ---
        if (uiOverlay != null)
        {
            // Map brightness (-2..2) to overlay alpha (0..0.6)
            float alpha = Mathf.InverseLerp(-2f, 2f, -value) * 0.6f;
            uiOverlay.color = new Color(0f, 0f, 0f, alpha);
        }
    }
}
