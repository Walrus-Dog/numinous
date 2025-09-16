using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractorMain : MonoBehaviour
{
    //Main camera
    Camera cam;
    //Inventory
    public List<GameObject> inventory = new List<GameObject>();
    //List of numbers collected. CREATE WAY TO CLEAR THIS VALUE.
    public List<int> numbersCollected = new List<int>();

    public int codeCount;

    GameObject lastDrawer;

    public PlayerInput playerInput;
    InputAction interactAction;

    //So buttons arent held down.
    public bool hasInteracted = false;
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
            Debug.Log(isTryingToInteract);
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
            numbersCollected.Clear();
        }
    }

    void HandleSequence(RaycastHit[] hit)
    {
        if (hit[0].collider.gameObject.CompareTag("Button") && hit[0].collider.gameObject.GetComponent<ButtonStats>() != null)
        {
            GameObject button = hit[0].collider.gameObject;
            //Make button flash red.
            
            //Add nums to list
            numbersCollected.Add(button.GetComponent<ButtonStats>().buttonValue);
            Debug.Log(numbersCollected[numbersCollected.Count - 1]);
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
            }
        }
    }

    void HandleDrawer(RaycastHit[] hit)
    {
        for (int i = 0; i < hit.Length; i++)
        {
            if (hit[i].collider.gameObject.CompareTag("Drawer"))
            {
                //Set the last drawer intereracted with (so you set pullingOut to false)
                lastDrawer = hit[i].collider.gameObject;
                DrawerPullout drawer = hit[i].collider.gameObject.GetComponent<DrawerPullout>();
                drawer.PulloutDrawer();
                drawer.pullingOut = true;
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
            }
        }
    }
}
