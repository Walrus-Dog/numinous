using UnityEngine;

public class StairTeleportCounter : MonoBehaviour
{
    public int numOfTeleports = 0;
    public int targetNumOfTeleports = 3;
    public GameObject key;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (numOfTeleports >= targetNumOfTeleports)
        {
            key.SetActive(true);
        }
    }
}
