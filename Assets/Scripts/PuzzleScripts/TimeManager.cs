using System;
using System.Xml.Schema;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public GameObject hourHand;
    public GameObject minuteHand;
    public GameObject secondHand;

    public float hourInput;

    DateTime time;

    public bool realTime;
    // Update is called once per frame
    void Update()
    {
        float hourRotation = 0;
        float minuteRotation = 0;
        float secondRotation = 0;
        if (realTime)
        {
            time = DateTime.Now;

            hourRotation = (time.Hour % 12) * 30f; // 360° / 12 hours
            minuteRotation = time.Minute * 6f;     // 360° / 60 minutes
            hourRotation = time.Second * 6f;     // 360° / 60 seconds
        }
        else
        {
            hourRotation = (hourInput % 12) * 30f;
        }


        hourHand.transform.localRotation = Quaternion.Euler(0f, 0f, hourRotation);
        minuteHand.transform.localRotation = Quaternion.Euler(0f, 0f, minuteRotation);
        secondHand.transform.localRotation = Quaternion.Euler(0f, 0f, secondRotation);
    }
}
