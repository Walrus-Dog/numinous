using UnityEngine;

public class TrainCarWalkedThroughTracker : MonoBehaviour
{
    public InteractorMain interactor;
    public ButtonStats buttonStats;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        interactor = FindAnyObjectByType<InteractorMain>();
        buttonStats = GetComponent<ButtonStats>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        interactor.numbersCollected.Add(buttonStats.buttonValue);
    }
}
