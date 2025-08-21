using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    public enum doorTypes { codeDoor, itemDoor, puzzleDoor}
    public doorTypes doorType;

    public GameObject player;

    public List<int> code;
    public List<GameObject> item;
    public List<GameObject> puzzle;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        int trueCount = 0;

        switch (doorType) 
        {
            case doorTypes.codeDoor:
                if (player.GetComponent<InteractorMain>().numbersCollected.Count == code.Count) 
                {
                    for (int i = 0; i < code.Count; i++)
                    {
                        if (player.GetComponent<InteractorMain>().numbersCollected[i] == code[i])
                        {
                            trueCount++;
                        }
                    }
                    if (trueCount == code.Count)
                    {
                        Destroy(gameObject);
                    }
                }
                break;
            case doorTypes.itemDoor:
                if (player.GetComponent<InteractorMain>().inventory == item)
                {
                    Destroy(gameObject);
                }
                break;
            case doorTypes.puzzleDoor:
                foreach (var element in puzzle) 
                {
                    if (element.gameObject.GetComponent<Button>().activeState == true)
                    {
                        //Count how many are true and compare that value to the number of puzzle elements
                        trueCount++;
                    }
                    if (trueCount == puzzle.Count)
                    {
                        Destroy(gameObject);
                    }
                }
                break;


        }
    }
}
