using System.Collections.Generic;
using UnityEngine;

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        //INPUT KEY IS E. 
        if (Input.GetKeyDown(KeyCode.E))
        {
            //Cast a ray from center of camera that hits all objects xf infront of it.
            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            RaycastHit[] hit = Physics.RaycastAll(ray, 10f);

            //Handle keypad. MUST BE TAGGED BUTTON
            HandleKeypad(hit);
            //Handle pickup. MUST BE TAGGED PICKUP
            HandlePickup(hit);
        }

        if (Input.GetKey(KeyCode.E))
        {
            //Cast a ray from center of camera that hits all objects xf infront of it.
            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            RaycastHit[] hit = Physics.RaycastAll(ray, 10f);
            HandleDrawer(hit);
        }

        if (Input.GetKeyUp(KeyCode.E) && lastDrawer != null)
        {
            lastDrawer.GetComponent<DrawerPullout>().pullingOut = false;
        }

        if (numbersCollected.Count > codeCount)
        {
            numbersCollected.Clear();
        }
    }

    void HandleKeypad(RaycastHit[] hit)
    {
        if (hit[0].collider.gameObject.CompareTag("Button") && hit[0].collider.gameObject.GetComponent<ButtonStats>() != null)
        {
            numbersCollected.Add(hit[0].collider.gameObject.GetComponent<ButtonStats>().buttonValue);
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
                lastDrawer = hit[i].collider.gameObject;
                DrawerPullout drawer = hit[i].collider.gameObject.GetComponent<DrawerPullout>();
                drawer.PulloutDrawer();
                drawer.pullingOut = true;
            }
        }
    }
}
