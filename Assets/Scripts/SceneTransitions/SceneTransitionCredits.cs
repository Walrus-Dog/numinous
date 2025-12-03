using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionCredits : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent<Player>(out var player))
        {
            SceneManager.LoadScene("Credits");
        }
    }
}
