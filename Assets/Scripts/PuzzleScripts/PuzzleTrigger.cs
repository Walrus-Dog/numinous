using UnityEngine;

public class PuzzleTrigger : MonoBehaviour
{
    public GameObject[] puzzleElement;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i < puzzleElement.Length; i++)
        {
            puzzleElement[i].SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            foreach (var element in puzzleElement)
            {
                element.SetActive(true);
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            foreach (var element in puzzleElement)
            {
                element.SetActive(false);
            }
        }
    }
}
