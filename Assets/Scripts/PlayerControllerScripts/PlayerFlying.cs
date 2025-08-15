using UnityEngine;
using UnityEngine.InputSystem.XR;

[RequireComponent(typeof(Player))]
public class PlayerFlying : PlayerState
{
    /// <ATTENTION>
    /// 
    /// DISABLE THIS CODE BEFORE LAUNCH <-----------
    /// 
    /// </ATTENTION>


    [SerializeField] float flyingSpeed = 10f;
    [SerializeField] float acceleration = 20f;

    public override void OnBeforeMove()
    {
        UpdateMovementFlying();
        player.UpdateLook();
    }

    void UpdateMovementFlying()
    {
        var input = player.GetMovementInput(flyingSpeed, false);

        var factor = acceleration * Time.deltaTime;
        player.velocity = Vector3.Lerp(player.velocity, input, factor);
    }
}
