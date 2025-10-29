using UnityEngine;
using UnityEngine.SceneManagement;

public class CursorOnMenuLoader : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.Equals(mainMenuSceneName))
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
