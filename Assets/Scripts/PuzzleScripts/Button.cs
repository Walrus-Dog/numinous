using Unity.VisualScripting;
using UnityEngine;

public class Button : MonoBehaviour
{
    public bool activeState = false;

    public void Update()
    {
        if (activeState)
        {
            gameObject.GetComponent<Renderer>().material.color = Color.red;
        }
        else
        {
            gameObject.GetComponent<Renderer>().material.color = Color.white;
        }
    }
}
