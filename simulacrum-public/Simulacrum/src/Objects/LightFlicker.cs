using System.Collections.Generic;
using Simulacrum.Utils;
using UnityEngine;

namespace Simulacrum.Objects;

public class LightFlicker : MonoBehaviour
{
    public Light lightSource;               // The light component to flicker
    public float minIntensity = 0f;         // Minimum light intensity during flicker
    public float maxIntensity = 1f;         // Maximum light intensity during flicker
    public float minFlickerSpeed = 0.05f;   // Minimum time between flickers during a burst
    public float maxFlickerSpeed = 0.2f;    // Maximum time between flickers during a burst
    public float minRestTime = 1f;          // Minimum time between flicker bursts
    public float maxRestTime = 5f;          // Maximum time between flicker bursts
    public int minFlickersInBurst = 1;      // Minimum number of flickers in a burst
    public int maxFlickersInBurst = 5;      // Maximum number of flickers in a burst

    private float nextFlickerTime;          // Tracks when to flicker next
    private int flickersLeftInBurst;        // How many flickers remain in the current burst
    private bool isInRestPeriod;            // Whether we are in a rest period or in a flicker burst

    void Awake()
    {

        lightSource = GetComponent<Light>();
        maxIntensity = lightSource.intensity;
        // Start with a rest Light
        EnterRestPeriod();
    }

    void Update()
    {
        // Check if it's time to flicker the light or enter a rest period
        if (Time.time >= nextFlickerTime)
        {
            if (isInRestPeriod)
            {
                // End the rest period and start a new flicker burst
                StartFlickerBurst();
            }
            else
            {
                Flicker();

                flickersLeftInBurst--;

                if (flickersLeftInBurst <= 0)
                {
                    // If the burst is over, enter a rest period
                    EnterRestPeriod();
                }
                else
                {
                    // Schedule the next flicker in the burst
                    nextFlickerTime = Time.time + Random.Range(minFlickerSpeed, maxFlickerSpeed);
                }
            }
        }
    }

    void Flicker()
    {
        // Randomly choose a new light intensity
        float newIntensity = Random.Range(minIntensity, maxIntensity);
        lightSource.intensity = newIntensity;
    }

    void StartFlickerBurst()
    {
        isInRestPeriod = false;
        flickersLeftInBurst = Random.Range(minFlickersInBurst, maxFlickersInBurst);
        nextFlickerTime = Time.time + Random.Range(minFlickerSpeed, maxFlickerSpeed);
    }

    void EnterRestPeriod()
    {
        isInRestPeriod = true;
        lightSource.intensity = maxIntensity; // Steady light during rest
        nextFlickerTime = Time.time + Random.Range(minRestTime, maxRestTime);
    }
}