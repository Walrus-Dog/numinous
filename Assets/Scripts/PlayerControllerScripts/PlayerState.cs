using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Player))]
public abstract class PlayerState : MonoBehaviour
{
    protected Player player;
    protected PlayerInput playerInput;

    public virtual void Awake()
    {
        player = GetComponent<Player>();
        playerInput = GetComponent<PlayerInput>();
    }


    public virtual void OnBeforeMove()
    {
        if (PauseMenu.Paused)
            return; // had to update to pause game
    }

    public virtual void OnGroundStateChange(bool isGrounded) { }
}
