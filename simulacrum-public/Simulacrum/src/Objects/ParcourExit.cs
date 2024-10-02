using System;
using UnityEngine;

namespace Simulacrum.Objects;

public class ParcourExit : MonoBehaviour
{
    private static readonly int ClipThreshold = Shader.PropertyToID("_ClipThreshold");

    internal event Action OnCollision;

    internal MeshRenderer Renderer;

    private void Awake()
    {
        Renderer = GetComponent<MeshRenderer>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Simulacrum.Log.LogInfo("EXIT COLLISION");
        if (other.gameObject.CompareTag("Player"))
        {
            OnCollision?.Invoke();
            Simulacrum.Log.LogInfo("PLAYER COLLISION ENTER");
        }
    }

    internal void Destroy()
    {
        Destroy(transform.parent.gameObject);
    }

    private void Update()
    {
        transform.parent.LookAt(Simulacrum.Player.gameplayCamera.transform);
        transform.parent.transform.rotation = Quaternion.Euler(0, transform.parent.rotation.eulerAngles.y, 0);
    }
}