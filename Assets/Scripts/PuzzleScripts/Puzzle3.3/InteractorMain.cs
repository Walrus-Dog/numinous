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
        if (hit[0].collider.gameObject.CompareTag("Pickup"))
        {
            inventory.Add(hit[0].collider.gameObject);
            Destroy(hit[0].collider.gameObject);
        }
    }
}
