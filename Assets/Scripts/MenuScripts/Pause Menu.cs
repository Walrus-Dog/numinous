using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    public static bool Paused = false;

    [Header("UI Elements")]
    public GameObject PauseMenuScreen;

    [Header("Player References")]
    public Player player;             // Added for GameplaySceneInitializer
    public PlayerInput playerInput;   // Added for GameplaySceneInitializer

    private InputSystem_Actions Actions;
    private bool inputRead;

    void Start()
    {
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (Actions == null)
            return;

        var pauseInput = Actions.UI.Pause.ReadValue<float>() > 0;

        if (pauseInput && !inputRead)
        {
            if (Paused)
            {
                inputRead = true;
                Play();
            }
            else
            {
                inputRead = true;
                Stop();
            }
        }
        else if (!pauseInput)
        {
            inputRead = false;
        }
    }


    void Stop()
    {
        if (PauseMenuScreen != null)
            PauseMenuScreen.SetActive(true);

        Time.timeScale = 0f;
        Paused = true;

        // Optionally disable player input when paused
        if (playerInput != null)
            playerInput.enabled = false;
    }

    public void Play()
    {
        if (PauseMenuScreen != null)
            PauseMenuScreen.SetActive(false);

        Time.timeScale = 1f;
        Paused = false;

        // Re-enable player input
        if (playerInput != null)
            playerInput.enabled = true;
    }

    public void QuitButton()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
    }

    public void ResumeGame()
    {
        Play();
    }

    private void OnEnable()
    {
        if (Actions == null)
            Actions = new InputSystem_Actions();

        Actions.UI.Pause.Enable();
    }

    private void OnDisable()
    {
        if (Actions != null)
            Actions.UI.Pause.Disable();
    }

}