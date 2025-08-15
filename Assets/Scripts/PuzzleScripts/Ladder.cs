using Unity.VisualScripting;
using UnityEngine;

public class Ladder : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent<Player>(out var player))
        {
            player.StartClimbing();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent<Player>(out var player))
        {
            player.StartWalking();
        }
    }
}
