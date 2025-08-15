using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Player))]
public abstract class PlayerState : MonoBehaviour
{
    protected Player player;
    protected PlayerInput playerInput;

    public abstract void  OnBeforeMove();
    public virtual void OnGroundStateChange(bool isGrounded)
    {

    }

    public virtual void Awake()
    {
        player = GetComponent<Player>();
        playerInput = GetComponent<PlayerInput>();
    }
}
