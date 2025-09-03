using System.Transactions;
using UnityEngine;

public class PortalButtons : Button
{
    Vector3 pressedPos;
    Vector3 unpressedPos;

    private void Start()
    {
        unpressedPos = transform.position;
        Vector3 whenPressedPos = new Vector3(transform.position.x, transform.position.y - .25f, transform.position.z);
        pressedPos = whenPressedPos;
    }
    // Update is called once per frame
    void Update()
    {
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
        if (gameObject.CompareTag("ShadowButton") && other.gameObject.CompareTag("Shadow"))
        {
            activeState = true;
        }

        if (gameObject.CompareTag("Button") && other.gameObject.CompareTag("Player"))
        {
            activeState = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (gameObject.CompareTag("ShadowButton") && other.gameObject.CompareTag("Shadow"))
        {
            activeState = false;
        }
        if (gameObject.CompareTag("Button") && other.gameObject.CompareTag("Player"))
        {
            activeState = false;
        }
    }
}
