using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Simulacrum.Objects;

public class ParcourPlatform : MonoBehaviour
{
    private static readonly int Enter = Animator.StringToHash("enter");
    private float movementOffset;
    private float movementSpeed;
    private float movementAplitude;

    internal event Action OnEnterPlatform;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        movementOffset = Random.Range(-Mathf.PI, Mathf.PI);
        movementAplitude = Random.Range(3f, 6f);
        movementSpeed = Random.Range(0f, 1f) < 0.5f ? Random.Range(0.4f, 1.4f) : 0;

        var stepTrigger = transform.Find("StepTrigger").gameObject.AddComponent<TriggerCallback>();
        stepTrigger.OnEnter += collider =>
        {
            if (!collider.gameObject.CompareTag("Player")) return;
            Simulacrum.Player.PlayJumpAudio();
            animator.SetTrigger(Enter);
        };
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            OnEnterPlatform?.Invoke();
            other.transform.SetParent(transform, true);
        }
    }

    private void LateUpdate()
    {
        if (movementSpeed == 0) return;

        // This needs to happen in late update to override the animation postion X value
        transform.localPosition = new Vector3(
            Mathf.Sin(Time.time * movementSpeed + movementOffset) * movementAplitude,
            transform.localPosition.y,
            transform.localPosition.z
        );
    }
}