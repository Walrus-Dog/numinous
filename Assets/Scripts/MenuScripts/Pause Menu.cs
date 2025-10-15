using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
public class PauseMenu : MonoBehaviour
{
    public static bool Paused = false;
    public GameObject PauseMenuScreen;
    InputSystem_Actions Actions;
    bool inputRead;
    //--timescale pauses game--

    void Start()
    {
        Time.timeScale = 1f;
    }

   //--escape key pauses/starts game--

    void Update()
    {

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
        else
        {
            inputRead = false;
        }
    }

    void Stop()
    {
        PauseMenuScreen.SetActive(true);
            Time.timeScale = 0f;
        Paused = true;
    }

    public void Play()
    {
        PauseMenuScreen.SetActive(false);
        Time.timeScale = 1f;
        Paused = false;
    }

    //--LOADS SCENE BY SUBTRACTING 1 IN BUILD INDEX! (E.g. PauseMenu (1) -> Quit -> Subtracts 1 -> Main Menu (0))
    //--Will NOT work if the PauseMenu is not behind the Main Menu!!--

    public void QuitButton()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
    }

    private void OnEnable()
    {
        Actions = new InputSystem_Actions();
        Actions.UI.Pause.Enable();
    }

    private void OnDisable()
    {
        Actions.UI.Pause.Disable();
    }

}
