using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


public class TrainMoverScript : MonoBehaviour
{
    public GameObject cutsceneCam;

    public DoorController door;
    public float maxTrainSpeed;
    public float currentTrainSpeed;
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
    }

    void MoveTrain()
    {
        transform.Translate(Vector3.back * Time.deltaTime * currentTrainSpeed);

        currentTrainSpeed = Mathf.Lerp(currentTrainSpeed, maxTrainSpeed, Time.deltaTime);
    }

    void StartCutscene()
    {
        cutsceneCam.SetActive(true);
    }
}
