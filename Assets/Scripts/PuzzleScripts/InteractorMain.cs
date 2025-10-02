using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractorMain : MonoBehaviour
{
    //Main camera
    Camera cam;
    //Inventory
    public List<GameObject> inventory = new List<GameObject>();
    //List of numbers collected.
    public List<int> numbersCollected = new List<int>();

    public int codeCount;

    GameObject lastDrawer;

    public PlayerInput playerInput;
    InputAction interactAction;

    //So buttons arent held down.
    public bool hasInteracted = false;

    public AudioSource interactAudio;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cam = Camera.main;

        if (codeCount == 0)
        {
            codeCount = 3;
        }
    }

    private void Awake()
    {
        interactAction = playerInput.actions["Interact"];
    }

    // Update is called once per frame
    void Update()
    {
        var isTryingToInteract = interactAction.ReadValue<float>() > 0;
        //INPUT KEY IS E. 
        if (isTryingToInteract)
        {
            //Cast a ray from center of camera that hits all objects infront of it.
            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            RaycastHit[] hit = Physics.RaycastAll(ray, 5f);

            //Handle keypad. MUST BE TAGGED BUTTON
            if (!hasInteracted)
            {
                HandleSequence(hit);
            }
            //Handle pickup. MUST BE TAGGED PICKUP
            HandlePickup(hit);
            //Handle Drawer. MUST BE TAGGED DRAWER
            HandleDrawer(hit);
            //Handle Door. MUST BE TAGGED DOOR
            HandleDoors(hit);
            hasInteracted = true;
        }

        if (!isTryingToInteract)
        {
            hasInteracted = false;

            if (lastDrawer != null)
            {
                lastDrawer.GetComponent<DrawerPullout>().pullingOut = false;
            }
        }

        if (numbersCollected.Count > codeCount)
        {
            int lastNum = numbersCollected[numbersCollected.Count - 1];
            numbersCollected.Clear();
            numbersCollected.Add(lastNum);
        }
    }

    void HandleSequence(RaycastHit[] hit)
    {
        for (int i = 0; i < hit.Length; i++)
        {
            Debug.Log(hit[i].collider.gameObject.name);
            
            if (hit[i].collider.gameObject.CompareTag("Button") && hit[i].collider.gameObject.GetComponent<ButtonStats>() != null)
            {
                GameObject button = hit[i].collider.gameObject;
                //Make button flash red.

                //Add nums to list
                numbersCollected.Add(button.GetComponent<ButtonStats>().buttonValue);
                Debug.Log(numbersCollected[numbersCollected.Count - 1]);

                interactAudio.Play();

                break;
            }
        }
    }

    void HandlePickup(RaycastHit[] hit)
    {
        for (int i = 0; i < hit.Length; i++)
        {
            if (hit[i].collider.gameObject.CompareTag("Pickup"))
            {
                Debug.Log("That's a pickup");
                inventory.Add(hit[i].collider.gameObject);
                //Move object below the map.
                hit[i].transform.Translate(-1 * transform.up * 100);

                if (!interactAudio.isPlaying)
                {
                    interactAudio.Play();
                }
            }
        }
    }

    void HandleDrawer(RaycastHit[] hit)
    {
        for (int i = 0; i < hit.Length; i++)
        {
            if (hit[i].collider.gameObject.CompareTag("Drawer"))
            {
                if (lastDrawer != hit[i].collider.gameObject && lastDrawer != null)
                {
                    lastDrawer.GetComponent<DrawerPullout>().pullingOut = false;
                }
                //Set the last drawer intereracted with (so you set pullingOut to false)
                lastDrawer = hit[i].collider.gameObject;
                DrawerPullout drawer = hit[i].collider.gameObject.GetComponent<DrawerPullout>();
                drawer.PulloutDrawer();
                drawer.pullingOut = true;

                if (!interactAudio.isPlaying && !hasInteracted)
                {
                    interactAudio.Play();
                }
            }
        }
    }

    void HandleDoors(RaycastHit[] hit)
    {
        for (int i = 0; i < hit.Length; i++)
        {
            if (hit[i].collider.gameObject.CompareTag("Door"))
            {
                DoorController door = hit[i].collider.gameObject.GetComponent<DoorController>();
                
                    door.interacted = true;

                    if (!interactAudio.isPlaying)
                    {
                        interactAudio.Play();
                    }
                
            }
        }
    }
}
