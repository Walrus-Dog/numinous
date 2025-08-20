using System.Transactions;
using UnityEngine;

public class PortalButtons : MonoBehaviour
{
    public bool pressed;
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
        if (pressed)
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
            pressed = true;
        }

        if (gameObject.CompareTag("Button") && other.gameObject.CompareTag("Player"))
        {
            pressed = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (gameObject.CompareTag("ShadowButton") && other.gameObject.CompareTag("Shadow"))
        {
            pressed = false;
        }
        if (gameObject.CompareTag("Button") && other.gameObject.CompareTag("Player"))
        {
            pressed = false;
        }
    }
}
