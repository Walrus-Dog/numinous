using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class TrainMoverScript : MonoBehaviour
{
    public GameObject cutsceneCam;

    public DoorController door;
    public float maxTrainSpeed;
    public float currentTrainSpeed;
    public float trainAcceleration = .1f;
    public bool backForward = false;
    public bool increasingOpacity = false;

    public Image blackOutScreen;
    public float blackoutSpeed = 1;

    public AudioClip trainMovingSound;

    public AudioClip trainCrashSound;
    public AudioSource audioSource;
    public float audioDelay = 5f;

    public float creditsDelay = 10f;
    public Scene credits;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (door.hasOpened)
        {
            MoveTrain();
            StartCutscene();
        }

        if (increasingOpacity)
        {
            Color tempColor = blackOutScreen.color;
            tempColor.a += Time.deltaTime * blackoutSpeed;
            blackOutScreen.color = tempColor;

            audioDelay -= Time.deltaTime;
            creditsDelay -= Time.deltaTime;

            if (audioDelay <= 0)
            {
                audioDelay += 10000f; // Prevent replaying
                audioSource.PlayOneShot(trainCrashSound);
            }

            if (creditsDelay <= 0)
            {
                SceneManager.LoadScene("Credits");
            }


        }
    }

    void MoveTrain()
    {
        int i = 1;
        if (backForward)
        {
            i = -1;
        }

        if (audioSource.isPlaying == false)
        {
            audioSource.PlayOneShot(trainMovingSound);
        }

        transform.Translate(Vector3.back * Time.deltaTime * currentTrainSpeed * i);

        currentTrainSpeed = Mathf.Lerp(currentTrainSpeed, maxTrainSpeed, Time.deltaTime * trainAcceleration);
    }

    void StartCutscene()
    {
        cutsceneCam.SetActive(true);

        increasingOpacity = true;
    }
}
