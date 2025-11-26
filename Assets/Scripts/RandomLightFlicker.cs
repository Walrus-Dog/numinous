using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class RandomLightFlicker : MonoBehaviour
{
    [Header("Target Light")]
    public Light targetLight;           // leave empty to auto-grab

    [Header("Base Settings")]
    public float baseIntensity = 1.5f;  // normal brightness
    public float minIntensity = 0.2f;   // darkest during flicker
    public float maxIntensity = 2.0f;   // brightest spike

    [Header("Timing")]
    [Tooltip("Time between flicker bursts (seconds).")]
    public float minWaitBetweenBursts = 1.0f;
    public float maxWaitBetweenBursts = 5.0f;

    [Tooltip("How long a single flicker burst lasts (seconds).")]
    public float minBurstDuration = 0.1f;
    public float maxBurstDuration = 0.5f;

    [Tooltip("Delay between individual flickers within a burst.")]
    public float minFlickerStep = 0.02f;
    public float maxFlickerStep = 0.08f;

    [Header("Occasional Full Blackout")]
    [Tooltip("Chance (0–1) that a burst will include a full blackout.")]
    [Range(0f, 1f)] public float blackoutChance = 0.3f;
    [Tooltip("How long the full blackout can last.")]
    public float minBlackoutTime = 0.05f;
    public float maxBlackoutTime = 0.2f;

    private void Awake()
    {
        if (!targetLight)
            targetLight = GetComponent<Light>();

        if (targetLight)
            targetLight.intensity = baseIntensity;
    }

    private void OnEnable()
    {
        if (targetLight)
            StartCoroutine(FlickerLoop());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (targetLight)
            targetLight.intensity = baseIntensity;
    }

    private IEnumerator FlickerLoop()
    {
        while (true)
        {
            // Wait random time before starting a flicker burst
            float wait = Random.Range(minWaitBetweenBursts, maxWaitBetweenBursts);
            yield return new WaitForSeconds(wait);

            // Duration of this burst
            float burstDuration = Random.Range(minBurstDuration, maxBurstDuration);
            float endTime = Time.time + burstDuration;

            // Maybe do a quick total blackout at some point in the burst
            bool willBlackout = Random.value < blackoutChance;
            bool blackoutDone = false;

            while (Time.time < endTime)
            {
                // optional blackout once per burst
                if (willBlackout && !blackoutDone && Random.value < 0.2f)
                {
                    float blackoutTime = Random.Range(minBlackoutTime, maxBlackoutTime);
                    targetLight.intensity = 0f;
                    yield return new WaitForSeconds(blackoutTime);
                    blackoutDone = true;
                }

                // Normal random flicker
                float newIntensity = Random.Range(minIntensity, maxIntensity);
                targetLight.intensity = newIntensity;

                float step = Random.Range(minFlickerStep, maxFlickerStep);
                yield return new WaitForSeconds(step);
            }

            // Return to base intensity between bursts
            targetLight.intensity = baseIntensity;
        }
    }
}
