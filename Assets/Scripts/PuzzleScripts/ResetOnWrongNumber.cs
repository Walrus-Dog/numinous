using UnityEngine;

public class ResetOnWrongNumber : MonoBehaviour
{
    InteractorMain interactor;
    public DoorController doorController;

    public Transform startPosition;

    private void Start()
    {
        interactor = GetComponent<InteractorMain>();
    }

    private void Update()
    {
        for (int i = 0; i < interactor.numbersCollected.Count; i++)
        {
            if (interactor.numbersCollected[i] != doorController.code[i])
            {
                ResetPuzzle();
            }
        }
    }

    void ResetPuzzle()
    {
        interactor.numbersCollected.Clear();

        this.gameObject.transform.position = startPosition.position;
    }
}
