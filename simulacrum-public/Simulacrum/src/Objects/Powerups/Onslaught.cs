using Simulacrum.Utils;
using UnityEngine;

namespace Simulacrum.Objects.Powerups;

public class Onslaught : MonoBehaviour
{
    internal static void Create(Vector3 position) => Instantiate(
        SimAssets.PowerupOnslaught, position, Quaternion.identity
    ).transform.Find("Sphere").gameObject.AddComponent<Onslaught>();

    private void OnCollisionEnter(Collision other)
    {
        Simulacrum.Log.LogInfo($"ENTER: {other.transform.tag}");

        if (other.gameObject.CompareTag("Player"))
        {
            Simulacrum.Log.LogInfo("PLEYR ENTER");
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        transform.LookAt(Simulacrum.Player.gameplayCamera.transform);
    }
}