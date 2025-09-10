using UnityEngine;
using UnityEngine.InputSystem;

public class CameraToggle : MonoBehaviour
{

    PlayerInput input;
    InputAction toggle;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        input = GetComponent<PlayerInput>();

        toggle = input.actions["ToggleCamera"]; 
    }

    // Update is called once per frame
    void Update()
    {
        var toggleInput = toggle.ReadValue<float>();
    }
}
