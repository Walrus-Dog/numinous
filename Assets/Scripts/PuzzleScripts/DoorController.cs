using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using TMPro;

public class DoorController : MonoBehaviour
{
    //What is required to open the door
    public enum doorTypes { codeDoor, itemDoor, puzzleDoor }
    public doorTypes doorType;

    public TextMeshProUGUI codeDisplay;

    public GameObject player;

    //What Items are needed to open door (set by doorTypes)
    public List<int> code;
    public List<GameObject> item;
    public List<GameObject> puzzle;

    bool hasOpened = false;

    //How long till door unlocks after having fufilled the requirments
    public float unlockTimer = 2.5f;

    //Interact with door to open bools
    public bool interactToOpen = false;
    public bool interacted = false;

    public AudioSource doorUnlocking;
    //So door unlocking sound plays once
    bool unlockOnce = false;
    public AudioSource doorOpening;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Find player
        if (player != null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }
        //If door type code, display code to canvas
        if (doorType == doorTypes.codeDoor)
        {
            string codeToDisplay = string.Empty;

            for (int i = 0; i < code.Count; i++)
            {
                if (i < code.Count - 1)
                {
                    codeToDisplay += $"{code[i]}, ";
                }
                else
                {
                    codeToDisplay += code[i];
                }
            }
            codeDisplay.text = codeToDisplay;
        }
    }

    // Update is called once per frame
    void Update()
    {
        //True count increments for each correct code/item/puzzle
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
                        UnlockDoor();
                    }
                }
                break;
            //If a item/key door
            case doorTypes.itemDoor:
                if (player.GetComponent<InteractorMain>().inventory.Count >= item.Count)
                {
                    for (int i = 0; i < item.Count; i++)
                    {
                        if (player.GetComponent<InteractorMain>().inventory.Contains(item[i]))
                        {
                            trueCount++;
                        }
                    }
                    if (trueCount >= item.Count)
                    {
                        UnlockDoor();
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
                        UnlockDoor();
                    }
                }
                break;
        }

    }
    
    //Uncloked Door can be opened (automatically or by interacting)
    void UnlockDoor()
    {
        unlockTimer -= Time.deltaTime;

        if (!unlockOnce)
        {
            doorUnlocking.Play();
            unlockOnce = true;
        }

        if (interactToOpen && interacted || !interactToOpen)
        {
            if (unlockTimer < 0)
            {
                OpenDoor();
            }
        }
    }

    void OpenDoor()
    {
        gameObject.transform.Translate(transform.up * 5 * Time.deltaTime);
        
        if (!hasOpened)
        {
            doorOpening.Play();
            hasOpened = true;
        }
    }
}
