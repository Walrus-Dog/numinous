using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Windows;

[RequireComponent(typeof(Player))]
public class PlayerWalking : PlayerState
{

    [SerializeField] public float speedMultiplier = 2f;
    [SerializeField] float jumpSpeed = 5f;
    [SerializeField] float jumpPressBufferTime = .05f;
    [SerializeField] float jumpGroundGracePeriod = .2f;


    bool tryingToJump;
    float lastJumpPressTime;
    float lastGroundedTime;

    InputAction sprintAction;
    InputAction jumpAction;
    InputAction crouchAction;

    public override void Awake()
    {
        base.Awake();
        sprintAction = playerInput.actions["sprint"];
        jumpAction = playerInput.actions["jump"];
        crouchAction = playerInput.actions["crouch"];
    }

    public override void OnBeforeMove()
    {
        UpDateSlopeSliding();

        player.UpdateGravity();
        player.UpdateGround();

        Sprinting();
        player.MoveHorizontal();
        player.UpdateLook();
        player.CheckBounds();
        player.UpDateCameraPosition(player.standingHeight);

        Jumping();
        Crouching();

    }

    void Crouching()
    {
        var isTryingToCrouch = crouchAction.ReadValue<float>() > 0;

        if (isTryingToCrouch)
        {
            player.StartCrouching();
        }
    }

    void Sprinting()
    {
        var sprintInput = sprintAction.ReadValue<float>();

        if (sprintInput == 0) return;

        var forwardMovementFactor = Mathf.Clamp01(Vector3.Dot(player.transform.forward, player.velocity.normalized));
        var multiplier = Mathf.Lerp(1f, speedMultiplier, forwardMovementFactor);

        player.movementSpeedMutliplier *= multiplier;
    }

    void Jumping()
    {
        bool wasTryingToJump = Time.time - lastJumpPressTime < jumpPressBufferTime;
        bool wasGrounded = Time.time - lastGroundedTime < jumpGroundGracePeriod;

        bool isOrWasTryingToJump = tryingToJump || (wasTryingToJump && player.IsGrounded);
        bool isOrWasGrounded = player.IsGrounded || wasGrounded;

        if (isOrWasTryingToJump && isOrWasGrounded)
        {
            player.velocity.y += jumpSpeed;
        }
        tryingToJump = false;
    }

    void OnJump()
    {
        tryingToJump = true;
        lastJumpPressTime = Time.time;
    }

    public override void OnGroundStateChange(bool isGrounded)
    {
        if (!isGrounded) lastGroundedTime = Time.time;
    }

    //DEBUG CODE 
    Action OnNextDrawGizmos;
    void OnDrawGizmos()
    {
        OnNextDrawGizmos?.Invoke();
        OnNextDrawGizmos = null;
    }

    public void UpDateSlopeSliding()
    {
        if (player.IsGrounded)
        {
            var sphereCastVerticalOffset = player.controller.height / 2 - player.controller.radius;
            var castOrigin = transform.position - new Vector3(0, sphereCastVerticalOffset, 0);

            if (Physics.SphereCast(castOrigin, player.controller.radius - .01f, Vector3.down, out var hit, .05f, ~LayerMask.GetMask("Player"), QueryTriggerInteraction.Ignore))
            {
                var colldier = hit.collider;
                var angle = Vector3.Angle(Vector3.up, hit.normal);

                //DEBUG CODE 
                Debug.DrawLine(hit.point, hit.point + hit.normal, Color.black, 1f);
                OnNextDrawGizmos += () =>
                {
                    GUI.color = Color.black;
                    Handles.Label(transform.position + new Vector3(0, 2f, 0), "Angle: " + angle.ToString());
                };

                if (angle > player.controller.slopeLimit)
                {
                    var normal = hit.normal;
                    var yInverse = 1f - normal.y;
                    player.velocity.x += yInverse * normal.x;
                    player.velocity.z += yInverse * normal.z;
                }
            }
        }
    }

}
