using System;
using UnityEngine;

namespace Simulacrum.Objects;

public class TriggerCallback : MonoBehaviour
{
    internal event Action<Collider> OnEnter;
    internal event Action<Collider> OnExit;

    private void OnTriggerEnter(Collider other) => OnEnter?.Invoke(other);
    private void OnTriggerExit(Collider other) => OnExit?.Invoke(other);
}