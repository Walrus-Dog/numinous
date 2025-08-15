using System;
using UnityEngine;
using UnityEngine.InputSystem.XR;

[RequireComponent(typeof(Player))]
public class PlayerClimbing : PlayerState
{
    [SerializeField] float climbingSpeed = 2f;
    [SerializeField] float acceleration = 20f;

    public override void OnBeforeMove()
    {
        UpdateMovementClimbing();
        player.UpdateLook();
    }

    void UpdateMovementClimbing()
    {
        var input = player.GetMovementInput(climbingSpeed, false);
        var forwardInputFactor = Vector3.Dot(transform.forward, input.normalized);

        if (forwardInputFactor > 0)
        {
            input.x = input.x * 0.5f;
            input.z = input.z * 0.5f;

            if (Mathf.Abs(input.y) < 0.2f)
            {
                input.y = Mathf.Sign(input.y) * climbingSpeed;

                //DEBUG CODE 
                Debug.DrawLine(transform.position, transform.position + input, Color.red, 3f);
            }
            else
            {
                //DEBUG CODE 
                Debug.DrawLine(transform.position, transform.position + input, Color.yellow, 3f);
            }
        }
        else
        {
            input.y = 0;
            input.x = input.x * 3f;
            input.z = input.z * 3f;

            //DEBUG CODE 
            Debug.DrawLine(transform.position, transform.position + input, Color.green, 3f);
        }

        var factor = acceleration * Time.deltaTime;
        player.velocity = Vector3.Lerp(player.velocity, input, factor);
    }
}
