using System.Collections;
using Simulacrum.Utils;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

namespace Simulacrum.Objects;

public class EntitySpawn : MonoBehaviour
{
    [SimAttributes.HostOnly]
    private EnemyAI entity;
    private NetworkObject entityNetObj;
    private readonly SimTimer targetTimer = SimTimer.ForInterval(5);

    internal static void SpawnEntity(Vector3 position, EnemyType entity) => Instantiate(
        SimAssets.EntitySpawn, position, Quaternion.identity
    ).gameObject.AddComponent<EntitySpawn>().Spawn(entity);

    private void Spawn(EnemyType entityType) => StartCoroutine(spawnEntityAnimation(entityType));

    private IEnumerator spawnEntityAnimation(EnemyType entityType)
    {
        var flatParticles = transform.Find("FlatParticles").GetComponent<VisualEffect>();
        flatParticles.Play();
        var flameParticles = transform.Find("FlameParticles").GetComponent<VisualEffect>();
        flameParticles.Play();
        yield return new WaitForSeconds(0.5f);

        if (NetworkManager.Singleton.IsHost)
        {
            var entityObj = Instantiate(entityType.enemyPrefab, transform.position, Quaternion.identity);
            entityObj.SetActive(false);

            entity = entityObj.GetComponent<EnemyAI>();
            entityNetObj = entityObj.GetComponent<NetworkObject>();
            entityNetObj.Spawn();
            Simulacrum.Waves.onEntityRetarget.SendClients(entityNetObj);
        }

        yield return new WaitForSeconds(1.5f);
        flatParticles.Stop();
        flameParticles.Stop();
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }

    private void Update()
    {
        if (!entity || entity.isEnemyDead) return;

        if (targetTimer.Tick())
        {
            Simulacrum.Waves.onEntityRetarget.SendServer(entityNetObj);
            targetTimer.Set(Random.Range(3, 9));
        }
    }
}