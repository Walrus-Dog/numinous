using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
public class PauseMenu : MonoBehaviour
{
    public static bool Paused = false;
    public GameObject PauseMenuScreen;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Time.timeScale = 1f;
    }

    // Update is called once per frame
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

    public void QuitButton()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
    }

}
