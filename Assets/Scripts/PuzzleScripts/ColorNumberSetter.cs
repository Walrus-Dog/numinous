using NUnit.Framework;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class ColorNumberSetter : MonoBehaviour
{
    public GameObject[] blueObjs;
    public GameObject[] greenObjs;
    public GameObject[] redObjs;

    public TextMeshProUGUI colorKey;

    List<int> usedNumbers;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        usedNumbers = new List<int>();

        int blueValue = GenerateNumToUse();
        foreach (GameObject obj in blueObjs)
        {
            obj.GetComponent<ButtonStats>().buttonValue = blueValue;
        }
        int greenValue = GenerateNumToUse();
        foreach (GameObject obj in greenObjs)
        {
            obj.GetComponent<ButtonStats>().buttonValue = greenValue;
        }
        int redValue = GenerateNumToUse();
        foreach (GameObject obj in redObjs)
        {
            obj.GetComponent<ButtonStats>().buttonValue = redValue;
        }


        colorKey.text = $"Blue = {blueValue} \n" +
            $"Green = {greenValue} \n" +
            $"Red = {redValue}";

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    int GenerateNumToUse()
    {
        int returnValue = Random.Range(1, 4);

        while (usedNumbers.Contains(returnValue))
        {
            returnValue = Random.Range(1, 4);
        }
        
        usedNumbers.Add(returnValue);
       
        return returnValue;
    }
}
