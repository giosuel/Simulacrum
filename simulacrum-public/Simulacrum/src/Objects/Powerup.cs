using System;
using UnityEngine;

namespace Simulacrum.Objects;

internal class Powerup : MonoBehaviour
{
    private Action onConsumptionCallback;
    private bool lookAtPlayer;

    internal void Init(Action onConsumption, bool isLookingAtPlayer)
    {
        onConsumptionCallback = onConsumption;
        lookAtPlayer = isLookingAtPlayer;
    }

    public void OnTriggerEnter(Collider other)
    {
        Simulacrum.Log.LogInfo($"POWERUP TRIGGER ENTER: {other.gameObject.tag}");
        if (other.gameObject.CompareTag("Player"))
        {
            onConsumptionCallback?.Invoke();
            Destroy(transform.parent.gameObject);
        }
    }

    private void Update()
    {
        if (lookAtPlayer) transform.Find("Sphere").LookAt(Simulacrum.Player.gameplayCamera.transform);
    }
}