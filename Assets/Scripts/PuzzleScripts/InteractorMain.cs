using System.Collections.Generic;
using TMPro;
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
    public TextMeshProUGUI CodeDisplay;

    public int codeCount;

    GameObject lastDrawer;

    public PlayerInput playerInput;
    InputAction interactAction;

    //So buttons arent held down.
    public bool hasInteracted = false;

    public AudioSource interactAudio;

    // FIX: warn if CodeDisplay isn't assigned
    bool _warnedNoCodeDisplay;   // FIX

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Find the main camera safely
        cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("InteractorMain: No Main Camera found in scene!");
        }

        if (codeCount == 0)
        {
            codeCount = 3;
        }
    }

    private void Awake()
    {
        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInput>();
            if (playerInput == null)
                playerInput = FindFirstObjectByType<PlayerInput>();
        }

        if (playerInput != null)
        {
            interactAction = playerInput.actions["Interact"];
        }
        else
        {
            Debug.LogWarning("InteractorMainScript: No PlayerInput found. assign one in the inspector.", this);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (PauseMenu.Paused) return;
        if (playerInput == null || interactAction == null || cam == null) return;

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

        //Display code
        if (numbersCollected.Count != 0)
        {
            string codeToDisplay = string.Empty;

            for (int i = 0; i < numbersCollected.Count; i++)
            {
                if (i < numbersCollected.Count - 1)
                {
                    codeToDisplay += $"{numbersCollected[i]}, ";
                }
                else
                {
                    codeToDisplay += numbersCollected[i];
                }
            }

            if (CodeDisplay != null)                      // FIX: guard to prevent NRE
                CodeDisplay.text = codeToDisplay;
        }
        else
        {
            if (CodeDisplay != null)                      // FIX: guard to prevent NRE
                CodeDisplay.text = string.Empty;
        }

        // FIX: warn if not assigned 
        if (CodeDisplay == null && !_warnedNoCodeDisplay) // FIX
        {
            Debug.LogWarning("InteractorMain: CodeDisplay is not assigned. Codes will not show on UI.", this);
            _warnedNoCodeDisplay = true;
        }
    }

    void HandleSequence(RaycastHit[] hit)
    {
        for (int i = 0; i < hit.Length; i++)
        {
            Debug.Log(hit[i].collider.gameObject.name);

            if (hit[i].collider.gameObject.CompareTag("Button") &&
                hit[i].collider.gameObject.GetComponent<ButtonStats>() != null)
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
                //Prevents "Last drawer" from changing without pushing drawer in.
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
