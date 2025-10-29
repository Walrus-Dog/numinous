using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string firstLevelName = "Level1"; // change to your scene name in the Inspector

    public void Play()
    {
        SceneManager.LoadScene(firstLevelName);
    }

    public void Quit()
    {
        Application.Quit();
        Debug.Log("Player quit the game.");
    }
}
