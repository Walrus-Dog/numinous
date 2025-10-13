using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
public class PauseMenu : MonoBehaviour
{
    public static bool Paused = false;
    public GameObject PauseMenuScreen;

    //--timescale pauses game--

    void Start()
    {
        Time.timeScale = 1f;
    }

   //--escape key pauses/starts game--

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Paused)
            {


                Play();
            }
            else
            {
                Stop();
            }
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

}
