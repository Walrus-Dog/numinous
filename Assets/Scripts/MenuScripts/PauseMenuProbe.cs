using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenuProbe : MonoBehaviour
{
    public PauseMenu pm;

    void Awake()
    {
        if (!pm) pm = GetComponent<PauseMenu>();
    }

    void Update()
    {
        // F1: force show the pause canvas (ignores pause logic)
        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            if (pm && pm.PauseMenuScreen)
            {
                pm.PauseMenuScreen.SetActive(true);
                Debug.Log("[Probe] Forced PauseMenuScreen.SetActive(true)");
            }
            else
            {
                Debug.LogWarning("[Probe] PauseMenu or PauseMenuScreen is NULL");
            }
        }

        // F2: print wiring info
        if (Keyboard.current.f2Key.wasPressedThisFrame)
        {
            var screen = pm ? pm.PauseMenuScreen : null;
            var settingsGO = GameObject.Find("SettingsMenu"); // canvas is named 'SettingsMenu' in your scene
            Debug.Log($"[Probe] pm={(pm ? pm.name : "NULL")}, screen={(screen ? screen.name : "NULL")}, settingsGO={(settingsGO ? settingsGO.name : "NULL")}");
        }

        // F3: list PauseMenu instances
        if (Keyboard.current.f3Key.wasPressedThisFrame)
        {
            var all = Object.FindObjectsByType<PauseMenu>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Debug.Log($"[Probe] PauseMenu count={all.Length}. {(all.Length > 1 ? "Potential conflict!" : "OK")}");
            foreach (var p in all) Debug.Log($"   - {p.name} (activeInHierarchy={p.gameObject.activeInHierarchy})");
        }
    }
}
