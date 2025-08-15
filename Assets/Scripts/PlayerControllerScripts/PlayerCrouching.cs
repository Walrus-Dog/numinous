using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Player))]
public class PlayerCrouching : PlayerState
{
    [SerializeField] float crouchHeight = 1f;
    [SerializeField] float crouchSpeedMultiplier = .5f;
    [SerializeField] float currentHeight;

    InputAction crouchAction;

    public override void Awake()
    {
        base.Awake();
        crouchAction = playerInput.actions["crouch"];
    }

    public override void OnBeforeMove()
    {
        player.MoveHorizontal();
        player.UpdateLook();
        player.UpdateGravity();
        player.UpdateGround();
        

        var isTryingToCrouch = crouchAction.ReadValue<float>() > 0;

        var heightTarget = isTryingToCrouch ? crouchHeight : player.standingHeight;

        if (currentHeight == 0)
        {
            currentHeight = player.standingHeight;
        }

        if (!isTryingToCrouch)
        {
            var castOrigin = transform.position + new Vector3(0, 0.8f, 0);

            //DEBUG CODE
            Debug.DrawRay(castOrigin, Vector3.up * 2f, Color.blue);

            if (Physics.Raycast(castOrigin, Vector3.up, out RaycastHit hit, currentHeight / 2))
            {
                var distanceToCeiling = hit.point.y - castOrigin.y;
                heightTarget = Mathf.Min
                (
                    currentHeight + distanceToCeiling - 0.1f, 
                    crouchHeight
                );
            }
            else
            {
                player.StartWalking();
            }
        }

        if (player.IsGrounded)
        {
            player.movementSpeedMutliplier *= crouchSpeedMultiplier;
        }

        player.UpDateCameraPosition(heightTarget);
    }
}
