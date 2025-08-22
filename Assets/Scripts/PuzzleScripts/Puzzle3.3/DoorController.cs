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

        //Different door types
        switch (doorType) 
        {
            //If a code door
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
            //If a item/key door
            case doorTypes.itemDoor:
                if (player.GetComponent<InteractorMain>().inventory.Count == item.Count)
                {
                    for (int i = 0; i < item.Count; i++)
                    {
                        if (player.GetComponent<InteractorMain>().inventory[i] == item[i])
                        {
                            trueCount++;
                        }
                    }
                    if (trueCount == item.Count)
                    {
                        Destroy(gameObject);
                    }
                }
                break;
            //If a puzzle element door
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
