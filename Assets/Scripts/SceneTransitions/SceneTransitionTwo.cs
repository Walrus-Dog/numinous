using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionTwo : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent<Player>(out var player))
        {
            SceneManager.LoadScene("Level2");
        }
    }
}
