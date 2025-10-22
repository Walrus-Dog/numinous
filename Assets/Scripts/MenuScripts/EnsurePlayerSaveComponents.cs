using UnityEngine;

public class EnsurePlayerSaveComponents : MonoBehaviour
{
    [Tooltip("Tag used to find the player at runtime.")]
    public string playerTag = "Player";

    private void Start()
    {
        var go = GameObject.FindGameObjectWithTag(playerTag);
        if (go == null)
        {
            Debug.LogWarning($"[EnsurePlayerSaveComponents] No GameObject with tag '{playerTag}' found.");
            return;
        }

        // Ensure SaveableEntity
        var ent = go.GetComponent<SaveableEntity>();
        if (ent == null)
        {
            ent = go.AddComponent<SaveableEntity>();
            Debug.Log("[EnsurePlayerSaveComponents] Added SaveableEntity to Player.");
        }

        // Ensure TransformSaveAdapter
        var adapter = go.GetComponent<TransformSaveAdapter>();
        if (adapter == null)
        {
            adapter = go.AddComponent<TransformSaveAdapter>();
            adapter.applyNextFrame = true;
            adapter.characterControllerSafeTeleport = true;
            adapter.zeroRigidbodyVelocity = true;
            Debug.Log("[EnsurePlayerSaveComponents] Added TransformSaveAdapter to Player.");
        }
    }
}
