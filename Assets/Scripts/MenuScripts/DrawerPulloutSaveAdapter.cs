using System;
using UnityEngine;

[RequireComponent(typeof(SaveableEntity))]
public class DrawerPulloutSaveAdapter : MonoBehaviour, ISaveable
{
    [Serializable]
    public struct State
    {
        public float pulloutAmount;
        public bool pullingOut;
        public float targetPull;
        public float targetRange;
        public Vector3 position;
    }

    public object CaptureState()
    {
        var d = GetComponent<DrawerPullout>();
        if (d == null) return null;
        return new State
        {
            pulloutAmount = d.pulloutAmount,
            pullingOut = d.pullingOut,
            targetPull = d.targetPull,
            targetRange = d.targetRange,
            position = transform.position
        };
    }

    public void RestoreState(object state)
    {
        var d = GetComponent<DrawerPullout>();
        if (d == null) return;
        var s = (State)state;
        d.pulloutAmount = s.pulloutAmount;
        d.pullingOut = s.pullingOut;
        d.targetPull = s.targetPull;
        d.targetRange = s.targetRange;
        transform.position = s.position;
    }
}
