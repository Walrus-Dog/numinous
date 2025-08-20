using System.Collections.Generic;
using UnityEngine;

public class InteractorMain : MonoBehaviour
{
    Camera cam;

    public List<GameObject> inventory = new List<GameObject>();
    public List<int> numbersCollected = new List<int>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            RaycastHit[] hit = Physics.RaycastAll(ray, 10f);

            HandleKeypad(hit);
            HandlePickup(hit);
        }
    }

    void HandleKeypad(RaycastHit[] hit)
    {
        for (int i = 0; i < hit.Length; i++)
        {
            if (hit[i].collider.gameObject.CompareTag("Button") && hit[i].collider.gameObject.GetComponent<ButtonStats>() != null)
            {
                numbersCollected.Add(hit[i].collider.gameObject.GetComponent<ButtonStats>().buttonValue);
                Debug.Log(numbersCollected[numbersCollected.Count - 1]);
            }
        }
    }

    void HandlePickup(RaycastHit[] hit)
    {
        for (int i = 0; i < hit.Length; i++)
        {
            if (hit[i].collider.gameObject.CompareTag("Pickup") )//&& hit[i].collider.gameObject.GetComponent<ButtonStats>() != null)
            {
                inventory.Add(hit[i].collider.gameObject);
                Destroy(hit[i].collider.gameObject);
            }
        }
    }
}
