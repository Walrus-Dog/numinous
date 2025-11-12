using UnityEngine;
using TMPro;

public class InteractionPromptController : MonoBehaviour
{
    [Header("Setup")]
    public Camera playerCamera;            // drag your main camera here
    public TMP_Text promptText;            // drag InteractPromptText here

    [Header("Detection")]
    public float maxDistance = 3f;
    public LayerMask raycastMask = ~0;     // default: everything

    private InteractHintTarget currentTarget;

    void Reset()
    {
        // Try auto-fill camera if placed on the Player
        if (!playerCamera)
            playerCamera = Camera.main;
    }

    void Update()
    {
        if (!playerCamera || !promptText) return;

        // Ray from center of screen
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, raycastMask))
        {
            var target = hit.collider.GetComponentInParent<InteractHintTarget>();

            if (target != null)
            {
                // New target or first time
                if (currentTarget != target)
                {
                    currentTarget = target;
                    promptText.text = string.IsNullOrWhiteSpace(target.promptText)
                        ? "Press E to interact"
                        : target.promptText;
                }

                if (!promptText.gameObject.activeSelf)
                    promptText.gameObject.SetActive(true);

                return; // keep showing
            }
        }

        // Nothing interactable in front ? hide
        currentTarget = null;
        if (promptText.gameObject.activeSelf)
            promptText.gameObject.SetActive(false);
    }
}
