using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ensures new scenes never start in a paused/broken state.
/// Place one in your bootstrap/first scene (it will persist).
/// </summary>
public class PauseMenuLoadGuard : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameplayStateReset.ResetToGameplay();
    }
}
