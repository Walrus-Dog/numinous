using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SaveableEntity))]
public class TransformSaveAdapter : MonoBehaviour, ISaveable
{
    [Serializable]
    public struct State
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;
    }

    [Header("Options")]
    [Tooltip("If true, save/load localPosition/localRotation instead of world space.")]
    public bool useLocalSpace = false;

    [Tooltip("Wait one frame before applying restore (lets other scripts finish Awake/Start).")]
    public bool applyNextFrame = true;

    [Tooltip("Also zero rigidbody velocity on restore.")]
    public bool zeroRigidbodyVelocity = true;

    [Tooltip("Try CharacterController-friendly teleport (disable, set pose, enable).")]
    public bool characterControllerSafeTeleport = true;

    public object CaptureState()
    {
        return new State
        {
            position = useLocalSpace ? transform.localPosition : transform.position,
            rotation = useLocalSpace ? transform.localRotation : transform.rotation,
            localScale = transform.localScale
        };
    }

    public void RestoreState(object state)
    {
        var s = (State)state;
        if (applyNextFrame)
            StartCoroutine(ApplyAfterOneFrame(s));
        else
            ApplyNow(s);
    }

    private IEnumerator ApplyAfterOneFrame(State s)
    {
        // Let other scripts (player controller, camera rig, etc.) initialize first.
        yield return null;  // 1 frame
        // yield return new WaitForEndOfFrame(); // uncomment if something still overrides

        ApplyNow(s);
    }

    private void ApplyNow(State s)
    {
        var rb = GetComponent<Rigidbody>();
        var cc = GetComponent<CharacterController>();

        // If using CharacterController, disable it while we set the transform.
        bool ccWasEnabled = false;
        if (characterControllerSafeTeleport && cc != null)
        {
            ccWasEnabled = cc.enabled;
            cc.enabled = false;
        }

        if (rb != null)
        {
            // Freeze motion while we teleport
            var wasKinematic = rb.isKinematic;
            rb.isKinematic = true;

            if (useLocalSpace)
            {
                transform.localPosition = s.position;
                transform.localRotation = s.rotation;
            }
            else
            {
                transform.SetPositionAndRotation(s.position, s.rotation);
            }
            transform.localScale = s.localScale;

            if (zeroRigidbodyVelocity)
            {
                // Unity 6 uses linearVelocity instead of velocity
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector3.zero;
#else
                rb.velocity = Vector3.zero;
#endif
                rb.angularVelocity = Vector3.zero;
            }

            rb.isKinematic = wasKinematic;
        }
        else
        {
            if (useLocalSpace)
            {
                transform.localPosition = s.position;
                transform.localRotation = s.rotation;
            }
            else
            {
                transform.SetPositionAndRotation(s.position, s.rotation);
            }
            transform.localScale = s.localScale;
        }

        if (characterControllerSafeTeleport && cc != null)
        {
            if (ccWasEnabled) cc.enabled = true;
            // Optional nudge: cc.Move(Vector3.zero);
        }

#if UNITY_EDITOR
        Debug.Log($"[TransformSaveAdapter] Restored '{name}' to {(useLocalSpace ? "LOCAL" : "WORLD")} pos={s.position}, rot={s.rotation.eulerAngles}");
#endif
    }
}
