using System.Transactions;
using UnityEngine;

public class PortalButtons : MonoBehaviour
{
    Vector3 pressedPos;
    Vector3 unpressedPos;

    // ? Added this field because "Button" had it before — now we define it ourselves
    public bool activeState;

    private void Start()
    {
        //Set unpressed position
        unpressedPos = transform.position;

        //Set pressed position
        Vector3 whenPressedPos = new Vector3(transform.position.x, transform.position.y - .25f, transform.position.z);
        pressedPos = whenPressedPos;
    }

    // Update is called once per frame
    void Update()
    {
        // ? Removed base.Update(); — no longer inheriting from UnityEngine.UI.Button

        //Press button down when active
        if (activeState)
        {
            transform.position = pressedPos;
        }
        else
        {
            transform.position = unpressedPos;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        //Check if Shadow activates ShadowButton
        if (gameObject.CompareTag("ShadowButton") && other.gameObject.CompareTag("Shadow"))
        {
            activeState = true;
        }

        //Check if Player activates regular Button
        if (gameObject.CompareTag("Button") && other.gameObject.CompareTag("Player"))
        {
            activeState = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        //Deactivate ShadowButton when Shadow leaves
        if (gameObject.CompareTag("ShadowButton") && other.gameObject.CompareTag("Shadow"))
        {
            activeState = false;
        }

        //Deactivate Button when Player leaves
        if (gameObject.CompareTag("Button") && other.gameObject.CompareTag("Player"))
        {
            activeState = false;
        }
    }
}
