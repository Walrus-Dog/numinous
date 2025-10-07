using UnityEngine;

public class SoundTrigger : MonoBehaviour
{
    public AudioSource soundToPlay;

    public bool playOnce = true;
    bool hasPlayed = false;
    private void OnTriggerEnter(Collider other)
    {
        if (playOnce && !hasPlayed || !playOnce)
        {
            if (!soundToPlay.isPlaying)
            {
                soundToPlay.Play();
                hasPlayed = true;
            }
        }
    }
}
