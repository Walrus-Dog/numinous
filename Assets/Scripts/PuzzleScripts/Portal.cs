using UnityEngine;

public class Portal : MonoBehaviour
{
    [SerializeField] Transform destination;
    public StairTeleportCounter counter;

    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player") && other.TryGetComponent<Player>(out var player))
        {
            player.Teleport(destination.position);
            counter.numOfTeleports++;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(destination.position, .4f);
        var direction = destination.TransformDirection(Vector3.forward);
        Gizmos.DrawRay(destination.position, direction);
    }
}
