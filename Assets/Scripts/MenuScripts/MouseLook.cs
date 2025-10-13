using UnityEngine;
using UnityEngine.UI;

public class SensitivityController : MonoBehaviour
{
    private Slider sensitivitySlider;
    private const string SensitivityKey = "SensitivityValue";

    [Header("Current sensitivity (read-only)")]
    public float sensitivity = 1f; // default

    void Start()
    {
        // --- 1. Auto find slider ---
        sensitivitySlider = GameObject.Find("SensitivitySlider")?.GetComponent<Slider>();
        if (sensitivitySlider == null)
            sensitivitySlider = Object.FindFirstObjectByType<Slider>();

        // --- 2. Load saved sensitivity ---
        sensitivity = PlayerPrefs.GetFloat(SensitivityKey, 1f);

        // --- 3. Configure slider ---
        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = 0.1f;
            sensitivitySlider.maxValue = 10f;
            sensitivitySlider.value = sensitivity;
            sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
        }
        else
        {
            Debug.LogWarning("No Slider found in the scene. Sensitivity can only be changed via script.");
        }
    }

    public void SetSensitivity(float value)
    {
        sensitivity = value;
        PlayerPrefs.SetFloat(SensitivityKey, sensitivity);
        PlayerPrefs.Save();
    }

    public void ResetSensitivity()
    {
        SetSensitivity(1f);
        if (sensitivitySlider != null) sensitivitySlider.value = 1f;
    }
}
