using System;
using System.Xml.Schema;
using UnityEngine;

public class C : MonoBehaviour
{
    public GameObject hourHand;
    public GameObject minuteHand;
    public GameObject secondHand;

    DateTime time;
    // Update is called once per frame
    void Update()
    {
        time = DateTime.Now;

        float hourRotation = (time.Hour % 12) * 30f; // 360° / 12 hours
        float minuteRotation = time.Minute * 6f;     // 360° / 60 minutes
        float secondRotation = time.Second * 6f;     // 360° / 60 seconds

        hourHand.transform.localRotation = Quaternion.Euler(0f, 0f, hourRotation);
        minuteHand.transform.localRotation = Quaternion.Euler(0f, 0f, minuteRotation);
        secondHand.transform.localRotation = Quaternion.Euler(0f, 0f, secondRotation);
    }
}
