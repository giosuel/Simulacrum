using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LethalNetworkAPI;
using Simulacrum.Objects;
using Simulacrum.Objects.Powerups;
using Simulacrum.Utils;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Simulacrum;

[SimAttributes.HostOnly]
internal class WaveController : MonoBehaviour
{
    private int iteration = -1;
    private IterationDefinition currentIteration;

    private int entitiesKilledInWave;
    private int entitiesSpawnedInWave;
    private int currentWaveEntityCount;

    private int currentWave;

    private bool wavesPaused = true;

    private readonly SimTimer spawnTimer = SimTimer.ForInterval(0);

    private const float difficultyMultiplier = 1f;

    private List<Vector3> spawnPositions;
    private List<Vector3> spawnPositionsHistory;

    private List<EnemyType> spawnableEntities = [];

    private const float preparationTime = 4f;

    private readonly Func<float, float> SpawnCooldown = SimUtils.ReciprocalDown(2, 0.3f * difficultyMultiplier);
    private readonly Func<float, float> SpawnCount = SimUtils.Quadratic(0.05f, 1.6f * difficultyMultiplier, 15);
    private readonly Func<float, float> WaveCount = SimUtils.ReciprocalUp(3, 17f, 0.05f * difficultyMultiplier);

    internal LNetworkMessage<NetworkObjectReference> onEntityRetarget { get; private set; }
    private LNetworkEvent onWaveAdvance;
    private LNetworkEvent onIterationFinished;

    private readonly HashSet<string> entityBlacklist =
    [
        "Docile Locust Bees",
        "Manticoil",
        "Red pill",
        "Shiggy",
        "ForestGiant",
        "Lasso"
    ];

    private readonly HashSet<string> passiveEntities =
    [
        "Blob"
    ];

    private void Awake()
    {
        onEntityRetarget = LNetworkMessage<NetworkObjectReference>.Connect("EntityRetarget");
        onWaveAdvance = LNetworkEvent.Connect("WaveAdvance");
        onIterationFinished = LNetworkEvent.Connect("IterationAdvance");

        onEntityRetarget.OnClientReceived += OnOnEntityRetargetClient;
        onWaveAdvance.OnClientReceived += OnOnWaveAdvanceClient;
        onIterationFinished.OnClientReceived += OnOnIterationFinishedClient;

        if (!NetworkManager.Singleton.IsHost)
        {
            enabled = false;
            return;
        }

        spawnableEntities = Resources.FindObjectsOfTypeAll<EnemyType>()
            .Where(entity => entity.canDie && !entityBlacklist.Contains(entity.enemyName))
            .ToList();
        foreach (var spawnableEntity in spawnableEntities)
        {
            Simulacrum.Log.LogInfo($"Spawnable Entity: {spawnableEntity.enemyName}");
        }
    }

    [SimAttributes.HostOnly]
    internal void StartIteration() => AdvanceIteration();

    [SimAttributes.HostOnly]
    private void AdvanceIteration()
    {
        iteration++;

        currentIteration = new IterationDefinition
        {
            WaveCount = Mathf.RoundToInt(SimUtils.RandomDeviation(WaveCount(iteration), 3)),
            AvgEntitiesPerWave = Mathf.RoundToInt(SpawnCount(iteration)),
            AvgEntitySpawnCooldown = SpawnCooldown(iteration),
        };

        spawnPositions = (
            from x in GameObject.FindGameObjectsWithTag("OutsideAINode")
            select x.transform.position
        ).ToList();
        spawnPositionsHistory = [];

        currentWave = 0;
        wavesPaused = false;
        spawnTimer.Set(preparationTime);
    }

    [SimAttributes.HostOnly]
    private void FinishIteration()
    {
        onIterationFinished.InvokeClients();

        DespawnAllEntities();
        wavesPaused = true;

        Simulacrum.Environment.AdvanceIteration();
    }

    [SimAttributes.HostOnly]
    internal static void DespawnAllEntities()
    {
        if (!NetworkManager.Singleton.IsHost) return;

        foreach (var entity in FindObjectsOfType<EnemyAI>())
        {
            entity.GetComponent<NetworkObject>().Despawn();
        }
    }

    [SimAttributes.HostOnly]
    internal void OnKillEntity(EnemyAI enemy) => entitiesKilledInWave++;

    [SimAttributes.HostOnly]
    private void AdvanceWave()
    {
        entitiesKilledInWave = 0;
        entitiesSpawnedInWave = 0;

        currentWaveEntityCount = Mathf.RoundToInt(SimUtils.RandomDeviation(currentIteration.AvgEntitiesPerWave, 1));
        currentWave++;

        Onslaught.Create(SimUtils.PickRandomWeightedItem(spawnPositions, spawnPositionsHistory));
        Health.Create(SimUtils.PickRandomWeightedItem(spawnPositions, spawnPositionsHistory));

        spawnTimer.Set(preparationTime);
        if (currentWave > 0) onWaveAdvance.InvokeClients();
    }

    [SimAttributes.HostOnly]
    private void SpawnRandomEntity()
    {
        var spawnPosition = SimUtils.PickRandomWeightedItem(spawnPositions, spawnPositionsHistory);
        var entity = spawnableEntities[Random.Range(0, spawnableEntities.Count)];

        EntitySpawn.SpawnEntity(spawnPosition, entity);

        // Increase kill count for unkillable entities
        if (passiveEntities.Contains(entity.enemyName)) entitiesKilledInWave++;

        entitiesSpawnedInWave++;
    }

    private void OnOnWaveAdvanceClient() => StartCoroutine(advanceWaveAnimation());

    private void OnOnIterationFinishedClient() => StartCoroutine(finishIterationAnimation());

    private static IEnumerator advanceWaveAnimation()
    {
        Simulacrum.Players.ShowLargeMessage("Next wave in 3...", 25, 1);
        yield return new WaitForSeconds(1);
        Simulacrum.Players.ShowLargeMessage("Next wave in 2...", 25, 1);
        yield return new WaitForSeconds(1);
        Simulacrum.Players.ShowLargeMessage("Next wave in 1...", 25, 1);
    }

    private static IEnumerator finishIterationAnimation()
    {
        Simulacrum.Players.ShowCompletionMessage("Iteration Complete!", "", 24, 3);
        yield break;
    }

    private static void OnOnEntityRetargetClient(NetworkObjectReference netObjRef)
    {
        if (netObjRef.TryGet(out var netObj))
        {
            var entity = netObj.GetComponent<EnemyAI>();
            entity.TargetClosestPlayer();

            Simulacrum.Log.LogInfo($"Entity {entity.enemyType.enemyName} retargeting");
        }
    }

    [SimAttributes.HostOnly]
    private void Update()
    {
        if (!NetworkManager.Singleton.IsHost || wavesPaused) return;

        if (currentWave > currentIteration.WaveCount)
        {
            Simulacrum.Log.LogInfo($"CURRENT WAQVE: {currentWave}");
            Simulacrum.Log.LogInfo($"WAVE OUNT: {currentIteration.WaveCount}");
            FinishIteration();
        }
        else if (entitiesKilledInWave >= currentWaveEntityCount)
        {
            Simulacrum.Log.LogInfo($"ENTITIES KILLED: {entitiesKilledInWave}");
            Simulacrum.Log.LogInfo($"ENTITIES IN WAVE: {currentWaveEntityCount}");

            AdvanceWave();
        }
        else if (entitiesSpawnedInWave < currentWaveEntityCount && spawnTimer.Tick())
        {
            Simulacrum.Log.LogInfo("SPAWNING ENTITY");

            SpawnRandomEntity();
            spawnTimer.Set(SimUtils.RandomDeviation(currentIteration.AvgEntitySpawnCooldown, 4));
        }
    }
}

internal readonly struct IterationDefinition
{
    internal int WaveCount { get; init; }
    internal int AvgEntitiesPerWave { get; init; }
    internal float AvgEntitySpawnCooldown { get; init; }
}