using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string firstLevelName = "Level1";

    public void Play()
    {
        // Load your first level
        SceneManager.LoadScene(firstLevelName);
    }

    public void Quit()
    {
        Debug.Log("Player quit the game.");

#if UNITY_EDITOR
        // Stop Play mode in the editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Quit the built game
        Application.Quit();
#endif
    }
}
