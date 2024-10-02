using System;
using System.Collections;
using Simulacrum.Utils;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

namespace Simulacrum.Objects;

public class EntitySpawn : MonoBehaviour
{
    [SimAttributes.HostOnly]
    private EnemyAI entity;
    private NetworkObject entityNetObj;
    private readonly SimTimer targetTimer = SimTimer.ForInterval(5);

    private Light light;
    private const float lightMaxIntensity = 500f;
    private const float lightFadeTime = 0.5f;

    internal static void SpawnEntity(Vector3 position, EnemyType entity) => Instantiate(
        SimAssets.EntitySpawn, position, Quaternion.identity
    ).gameObject.AddComponent<EntitySpawn>().Setup(entity);

    private void Setup(EnemyType entityType)
    {
        gameObject.SetActive(true);
        light = transform.Find("Light").GetComponent<Light>();
        StartCoroutine(spawnEntityAnimation(entityType));
    }

    private IEnumerator spawnEntityAnimation(EnemyType entityType)
    {
        var flatParticles = transform.Find("FlatParticles").GetComponent<VisualEffect>();
        flatParticles.Play();
        var flameParticles = transform.Find("FlameParticles").GetComponent<VisualEffect>();
        flameParticles.Play();
        StartCoroutine(lightFadeIn());
        yield return new WaitForSeconds(0.5f);

        if (NetworkManager.Singleton.IsHost)
        {
            var entityObj = Instantiate(entityType.enemyPrefab, transform.position, Quaternion.identity);
            entity = entityObj.GetComponent<EnemyAI>();
            entityNetObj = entityObj.GetComponent<NetworkObject>();
            entityNetObj.Spawn();
            yield return new WaitForSeconds(0.2f);
            Simulacrum.Waves.onEntitySpawn.SendClients(entityNetObj);
            yield return new WaitForSeconds(2.5f);
            Simulacrum.Waves.onEntityRetarget.SendClients(entityNetObj);
        }

        yield return new WaitForSeconds(1.5f);
        flatParticles.Stop();
        flameParticles.Stop();
        StartCoroutine(lightFadeOut());
        yield return new WaitForSeconds(lightFadeTime * 2);
        Simulacrum.Environment.FreeNode(transform.position);
    }

    private IEnumerator lightFadeIn()
    {
        var elapsedTime = 0f;

        while (elapsedTime < lightFadeTime)
        {
            light.intensity = elapsedTime / lightFadeTime * lightMaxIntensity;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator lightFadeOut()
    {
        var elapsedTime = 0f;

        while (elapsedTime < lightFadeTime)
        {
            light.intensity = lightMaxIntensity - elapsedTime / lightFadeTime * lightMaxIntensity;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    private void Update()
    {
        if (!entity || entity.isEnemyDead) return;

        if (targetTimer.Tick())
        {
            Simulacrum.Log.LogInfo("Retarget -1");
            Simulacrum.Waves.onEntityRetarget.SendClients(entityNetObj);
            targetTimer.Set(Random.Range(3, 9));
        }
    }
}