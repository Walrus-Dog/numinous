using System;
using UnityEngine;

[RequireComponent(typeof(SaveableEntity))]
public class ButtonStatsSaveAdapter : MonoBehaviour, ISaveable
{
    [Serializable]
    public struct State { public int buttonValue; }

    public object CaptureState()
    {
        var bs = GetComponent<ButtonStats>();
        if (bs == null) return null;
        return new State { buttonValue = bs.buttonValue };
    }

    public void RestoreState(object state)
    {
        var bs = GetComponent<ButtonStats>();
        if (bs == null) return;
        var s = (State)state;
        bs.buttonValue = s.buttonValue;
    }
}
