using UnityEngine;


public class DrawerPullout : MonoBehaviour
{

    public float pulloutAmount;
    Vector3 newPos;
    Vector3 startPos;
    public float pulloutSpeed;
    public bool pullingOut = false;

    public float targetPull = .5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startPos = transform.position;
        newPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        newPos = new Vector3(startPos.x, startPos.y, startPos.z + Mathf.Clamp(pulloutAmount, 0f, .8f));

        if(!pullingOut)
        {
            PullInDrawer();
        }

        transform.position = newPos;

        if (pulloutAmount < 0f)
        {
            pulloutAmount = 0f;
        }
        if (pulloutAmount > 1f)
        {
            pulloutAmount = 1f;
        }

        pulloutSpeed = ((Mathf.Abs(targetPull - pulloutAmount) + 1f) * Time.deltaTime);
    }

    public void PulloutDrawer()
    {
        //transform.Translate(transform.forward * Time.deltaTime);
        pulloutAmount += Time.deltaTime * pulloutSpeed;
    }

    void PullInDrawer()
    {
        //transform.Translate(transform.forward * Time.deltaTime);
        pulloutAmount -= Time.deltaTime * pulloutSpeed;
    }
}
