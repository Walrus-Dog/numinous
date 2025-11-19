using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public class TrainMoverScript : MonoBehaviour
{
    public GameObject cutsceneCam;

    public DoorController door;
    public float maxTrainSpeed;
    public float currentTrainSpeed;
    public bool backForward = false;
    public bool increasingOpacity = false;

    public Image blackOutScreen;
    public float blackoutSpeed = 1;
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
        }
    }

    void MoveTrain()
    {
        int i = 1;
        if (backForward)
        {
            i = -1;
        }

        transform.Translate(Vector3.back * Time.deltaTime * currentTrainSpeed * i);

        currentTrainSpeed = Mathf.Lerp(currentTrainSpeed, maxTrainSpeed, Time.deltaTime / 10);
    }

    void StartCutscene()
    {
        cutsceneCam.SetActive(true);

        increasingOpacity = true;
    }
}
