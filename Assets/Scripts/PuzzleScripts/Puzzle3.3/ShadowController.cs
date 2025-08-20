using System.Transactions;
using UnityEngine;

public class ShadowController : MonoBehaviour
{
    public GameObject player;

    public GameObject anchor;

    public bool nintyDegreesLeft;
    public bool nintyDegreesRight;

    // Update is called once per frame
    void Update()
    {
        int i = 1;
        int j = 1;

        Vector3 playerPos = player.transform.position;
        Vector3 anchorPos = anchor.transform.position;

        //direction from anchor to player
        Vector3 vectorFromAnchorToPlayer = playerPos - anchorPos;
        //Select which way is mirrored
        if (nintyDegreesLeft) i *= -1;
        if (nintyDegreesRight) j *= -1;
        //invert X and/or Y for mirroring
        Vector3 mirroredVector = new Vector3(j * -vectorFromAnchorToPlayer.x, vectorFromAnchorToPlayer.y, i *  -vectorFromAnchorToPlayer.z);

        Vector3 shadowPosition = anchorPos + mirroredVector;

        transform.position = shadowPosition;
    }
}
