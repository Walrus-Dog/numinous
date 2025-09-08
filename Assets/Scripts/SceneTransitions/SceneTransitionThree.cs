using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionThree : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent<Player>(out var player))
        {
            Debug.Log("galunga");
            SceneManager.LoadScene("Level3");
        }
    }
}
