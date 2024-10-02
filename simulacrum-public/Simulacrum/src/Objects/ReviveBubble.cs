using Simulacrum.Utils;
using UnityEngine;

namespace Simulacrum.Objects;

public class ReviveBubble : MonoBehaviour
{
    private const float bubbleRadius = 16;
    private const float noiseRadius = 6;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = transform.Find("Audio").GetComponent<AudioSource>();
        audioSource.transform.SetParent(Simulacrum.Player.transform, false);
        audioSource.clip = SimAssets.ReviveBubbleWallSFX;
        audioSource.outputAudioMixerGroup = Simulacrum.Player.itemAudio.outputAudioMixerGroup;
        audioSource.Play();
    }

    private void LateUpdate()
    {
        var distance = Vector3.Distance(Simulacrum.Player.transform.position, transform.position);

        audioSource.volume = 1 - Mathf.Clamp(Mathf.Abs(distance - bubbleRadius) / noiseRadius, 0, 1);
    }
}