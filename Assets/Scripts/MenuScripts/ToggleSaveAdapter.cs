using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(SaveableEntity))]
public class ToggleSaveAdapter : MonoBehaviour, ISaveable
{
    [Serializable] public struct State { public bool isOn; }
    public Toggle toggle;

    private void Reset() { toggle = GetComponent<Toggle>(); }

    public object CaptureState()
    {
        if (!toggle) return null;
        return new State { isOn = toggle.isOn };
    }

    public void RestoreState(object state)
    {
        if (!toggle) return;
        var s = (State)state;
        toggle.isOn = s.isOn;
        toggle.onValueChanged?.Invoke(toggle.isOn);
    }
}
