using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Windows;

public class Player : MonoBehaviour
{
    [SerializeField] float mouseSensitiviy = 1f; // now loaded 
    [SerializeField] float walkingSpeed = 5f;
    [SerializeField] float mass = 1f;
    [SerializeField] float acceleration = 20f;
    [SerializeField] float worldBottomBoundary = -100f;
    [SerializeField] float cameraTransitionSpeed = 10f;
    [SerializeField] float gravityMultiplier = 1f;
    [SerializeField] CharacterController characterController;

    public Transform cameraTransform;

    public bool IsGrounded => controller.isGrounded;

    public float Height
    {
        get => controller.height;
        set => controller.height = value;
    }

    internal float movementSpeedMutliplier;
    public float standingHeight;

    PlayerState currentPlayerState;
    PlayerWalking playerWalking;
    PlayerClimbing playerClimbing;
    PlayerCrouching playerCrouching;
    PlayerFlying playerFlying;

    public CharacterController controller;
    public Vector3 velocity;
    public Vector3 initialCameraPosition;
    public Vector2 look;

    (Vector3, Quaternion) initialPositionAndRotation;

    bool wasGrounded;

    PlayerInput playerInput;
    InputAction moveAction;
    InputAction lookAction;
    InputAction flyUpDownAction;
    InputAction interactAction;

    public AudioSource footstepSound;

    private const string SensitivityKey = "MouseSensitivity"; // Added constant for PlayerPrefs key

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        playerCrouching = GetComponent<PlayerCrouching>();
        playerWalking = GetComponent<PlayerWalking>();
        playerFlying = GetComponent<PlayerFlying>();
        playerClimbing = GetComponent<PlayerClimbing>();

        moveAction = playerInput.actions["move"];
        lookAction = playerInput.actions["look"];
        flyUpDownAction = playerInput.actions["flyUpDown"];
        interactAction = playerInput.actions["interact"];

        currentPlayerState = playerWalking;
    }

    void Start()
    {
        characterController.stepOffset = 1;

        // === Load saved sensitivity from PlayerPrefs (default 1.0) ===
        mouseSensitiviy = PlayerPrefs.GetFloat(SensitivityKey, 1f);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        initialPositionAndRotation = (transform.position, transform.rotation);
        initialCameraPosition = cameraTransform.localPosition;
        standingHeight = Height;
    }

    void Update()
    {
        // pause game
        if (global::PauseMenu.Paused)
            return;

        movementSpeedMutliplier = 1f;

        UpdateMovement();
        UpdateLook();
    }

    public void Teleport(Vector3 position)
    {
        transform.position = position;
        Physics.SyncTransforms();
        //look.x = rotation.eulerAngles.y;
        //look.y = rotation.eulerAngles.z;
        velocity = Vector3.zero;
    }

    public void UpDateCameraPosition(float targetHeight)
    {
        if (!Mathf.Approximately(targetHeight, Height))
        {
            var crouchDelta = Time.deltaTime * cameraTransitionSpeed;
            Height = Mathf.Lerp(Height, targetHeight, crouchDelta);

            var halfHeightDifference = new Vector3(0, (standingHeight - Height) / 2, 0);
            var newCameraPosition = initialCameraPosition - halfHeightDifference;

            cameraTransform.localPosition = newCameraPosition;
        }
    }

    public void CheckBounds()
    {
        if (transform.position.y < worldBottomBoundary)
        {
            var (position, rotation) = initialPositionAndRotation;
            Teleport(position);
        }
    }

    public void UpdateGround()
    {
        if (wasGrounded != IsGrounded)
        {
            currentPlayerState.OnGroundStateChange(IsGrounded);
            wasGrounded = IsGrounded;
        }
    }

    public void UpdateGravity()
    {
        var gravity = Physics.gravity * mass * Time.deltaTime * gravityMultiplier;
        velocity.y = controller.isGrounded ? -1f : velocity.y + gravity.y;
    }

    public Vector3 GetMovementInput(float speed, bool horizontal = true)
    {
        var moveInput = moveAction.ReadValue<Vector2>();
        var flyUpDownInput = flyUpDownAction.ReadValue<float>();

        if (moveInput != Vector2.zero)
        {
            if (!footstepSound.isPlaying)
            {
                footstepSound.Play();
            }
            if (!IsGrounded)
            {
                footstepSound.Stop();
            }
        }
        else
        {
            footstepSound.Stop();
        }

        var input = new Vector3();
        var referenceTransform = horizontal ? transform : cameraTransform;

        input += referenceTransform.forward * moveInput.y;
        input += referenceTransform.right * moveInput.x;

        if (!horizontal)
        {
            input += transform.up * flyUpDownInput;
        }

        input = Vector3.ClampMagnitude(input, 1f);
        input *= speed * movementSpeedMutliplier;

        return input;
    }

    public void UpdateMovement()
    {
        currentPlayerState.OnBeforeMove();
        controller.Move(velocity * Time.deltaTime);
    }

    public void MoveHorizontal()
    {
        var input = GetMovementInput(walkingSpeed);

        var factor = acceleration * Time.deltaTime;
        velocity.x = Mathf.Lerp(velocity.x, input.x, factor);
        velocity.z = Mathf.Lerp(velocity.z, input.z, factor);
    }

    public void UpdateLook()
    {
        var lookInput = lookAction.ReadValue<Vector2>();
        look.x += lookInput.x * mouseSensitiviy;
        look.y += lookInput.y * mouseSensitiviy;

        look.y = Mathf.Clamp(look.y, -89f, 89f);

        cameraTransform.localRotation = Quaternion.Euler(-look.y, 0, 0);
        transform.localRotation = Quaternion.Euler(0, look.x, 0);
    }

    // === Added public method for SettingsMenuManager ===
    public void SetSensitivity(float value)
    {
        // Clamp value to safe range and apply immediately
        mouseSensitiviy = Mathf.Clamp(value, 0.05f, 10f);
    }

    void OnToggleFlying()
    {
        if (currentPlayerState == playerWalking)
        {
            currentPlayerState = playerFlying;
        }
        else
        {
            currentPlayerState = playerWalking;
        }
    }

    public void StartClimbing()
    {
        currentPlayerState = playerClimbing;
    }
    public void StartCrouching()
    {
        currentPlayerState = playerCrouching;
    }

    public void StartWalking()
    {
        currentPlayerState = playerWalking;
    }
}

