using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-10000)] // run very early
public class InitialInputFreeze : MonoBehaviour
{
    [Tooltip("How long to block player input after the scene loads (realtime).")]
    public float freezeSeconds = 0.4f;

    [Tooltip("If you use a Player action map, put its name here. Leave blank to disable all input on PlayerInput.")]
    public string playerActionMapName = "Player";

    private PlayerInput playerInput;

    void Awake()
    {
        // Find the gameplay PlayerInput (not the UI/EventSystem one)
        playerInput = FindFirstObjectByType<PlayerInput>(FindObjectsInactive.Include);

        if (playerInput != null)
        {
            if (!string.IsNullOrEmpty(playerActionMapName))
                playerInput.actions.FindActionMap(playerActionMapName, true)?.Disable();
            else
                playerInput.DeactivateInput();
        }

        // Make sure we start in FPS cursor mode
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(freezeSeconds);

        if (playerInput != null)
        {
            if (!string.IsNullOrEmpty(playerActionMapName))
                playerInput.actions.FindActionMap(playerActionMapName, true)?.Enable();
            else
                playerInput.ActivateInput();
        }
    }
}
