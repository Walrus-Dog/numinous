using UnityEngine;


public class DrawerPullout : Button
{

    public float pulloutAmount;
    Vector3 newPos;
    Vector3 startPos;
    public float pulloutSpeed;
    public bool pullingOut = false;

    public float speedMultiplier;
    public float minSpeed;
    [Tooltip("The center point of the active area")]
    public float targetPull = .5f;
    [Tooltip("The range that the player sould be in.")]
    public float targetRange;

    public AudioSource drawerSound;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Set start pos
        startPos = transform.position;
        newPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        base.Update();

        //Set position
        newPos = new Vector3(startPos.x, startPos.y, startPos.z + Mathf.Clamp(pulloutAmount, 0f, .8f));

        //Push drawer in when not pulling out
        if(!pullingOut)
        {
            PullInDrawer();
        }

        transform.position = newPos;

        //Keep inside max and min.
        if (pulloutAmount < 0f)
        {
            pulloutAmount = 0f;
        }
        if (pulloutAmount > 1f)
        {
            pulloutAmount = 1f;
        }

        if (pulloutAmount > 0f && pulloutAmount < 1)
        {
            if (!drawerSound.isPlaying)
            {
                drawerSound.Play();
            }
        }

        //Pullout speed equation.
        pulloutSpeed = (Mathf.Abs(targetPull - pulloutAmount) * Time.deltaTime) * speedMultiplier;
        //Set pullout speed to minimum speed.
        if (pulloutSpeed < minSpeed)
        {
            pulloutSpeed = minSpeed;
        }
        //Active state condition
        if (pulloutAmount >= targetPull - targetRange && pulloutAmount <= targetPull + targetRange)
        {
            activeState = true;
        }
        else
        {
            activeState = false;
        }
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
