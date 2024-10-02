using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LethalNetworkAPI;
using Simulacrum.Network;
using Simulacrum.Objects;
using Simulacrum.Utils;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Simulacrum.Controllers;

[SimAttributes.HostOnly]
internal class WaveController : MonoBehaviour
{
    private IterationDefinition currentIteration;

    private int entitiesKilledInWave;
    private int entitiesSpawnedInWave;
    private int currentWaveEntityCount;

    private bool isInPreparation;

    private readonly SimTimer spawnTimer = SimTimer.ForInterval(0);
    private readonly SimTimer preparationTimer = SimTimer.ForInterval(0);

    private List<EnemyType> spawnableEntities = [];

    private readonly Func<float, float> SpawnCooldown = SimUtils.ReciprocalDown(
        10, 0.18f * Simulacrum.GameConfig.General.DifficultyMultiplier.Value
    );

    private readonly Func<float, float> SpawnCount = SimUtils.Quadratic(
        0.05f, 1.6f * Simulacrum.GameConfig.General.DifficultyMultiplier.Value, 15
    );

    private readonly Func<float, float> WaveCount = SimUtils.ReciprocalUp(
        4, 17f, 0.05f * Simulacrum.GameConfig.General.DifficultyMultiplier.Value
    );

    internal LNetworkMessage<NetworkObjectReference> onEntitySpawn { get; private set; }
    internal LNetworkMessage<NetworkObjectReference> onEntityRetarget { get; private set; }

    private void Awake()
    {
        onEntityRetarget = LNetworkMessage<NetworkObjectReference>.Connect("EntityRetarget");
        onEntitySpawn = LNetworkMessage<NetworkObjectReference>.Connect("EntitySpawn");

        onEntityRetarget.OnClientReceived += OnOnEntityRetargetClient;
        onEntitySpawn.OnClientReceived += OnOnEntitySpawnClient;
        Simulacrum.State.onIterationEndClients += OnIterationEndClient;
        Simulacrum.State.onWaveStartClients += OnWaveStartClient;

        if (!NetworkManager.Singleton.IsHost)
        {
            enabled = false;
            return;
        }

        Simulacrum.State.onIterationStartServer += OnIterationStartServerServer;
        Simulacrum.State.onIterationEndServer += OnIterationEndServerServer;
        Simulacrum.State.onWaveStartServer += OnWaveStartServerServer;

        spawnableEntities =
            Simulacrum.GameConfig.Entities.FilterDisabledEntities(Resources.FindObjectsOfTypeAll<EnemyType>().ToList());
    }

    [SimAttributes.HostOnly]
    internal void StartIteration()
    {
        Simulacrum.Log.LogInfo("INVOKING START ITERATION");
        Simulacrum.State.StartIteration(
            Mathf.RoundToInt(SimUtils.RandomDeviation(WaveCount(Simulacrum.State.CurrentIteration), 2))
        );
    }

    [SimAttributes.HostOnly]
    private void OnIterationStartServerServer(int waveCount)
    {
        currentIteration = new IterationDefinition
        {
            WaveCount = waveCount,
            AvgEntitiesPerWave = Mathf.RoundToInt(SpawnCount(Simulacrum.State.CurrentIteration)),
            AvgEntitySpawnCooldown = SpawnCooldown(Simulacrum.State.CurrentIteration),
        };

        entitiesKilledInWave = 0;
        currentWaveEntityCount = 0;

        isInPreparation = true;
        preparationTimer.Set(Simulacrum.GameConfig.General.PreparationTime.Value);
    }

    [SimAttributes.HostOnly]
    private void OnIterationEndServerServer()
    {
        DespawnAllEntities();
    }

    [SimAttributes.HostOnly]
    internal static void DespawnAllEntities()
    {
        if (!NetworkManager.Singleton.IsHost) return;

        foreach (var entity in FindObjectsOfType<EnemyAI>()) entity.GetComponent<NetworkObject>().Despawn();
    }

    [SimAttributes.HostOnly]
    internal void OnKillEntity(EnemyAI enemy) => entitiesKilledInWave++;

    [SimAttributes.HostOnly]
    private void OnWaveStartServerServer()
    {
        entitiesKilledInWave = 0;
        entitiesSpawnedInWave = 0;

        currentWaveEntityCount = Mathf.RoundToInt(SimUtils.RandomDeviation(currentIteration.AvgEntitiesPerWave, 1));
    }

    [SimAttributes.HostOnly]
    private void SpawnRandomEntity()
    {
        var spawnPosition = Simulacrum.Environment.FindFreeNode();
        var entity = spawnableEntities[Random.Range(0, spawnableEntities.Count)];
        // var entity = spawnableEntities.First(entity => entity.enemyName == "Flowerman");

        EntitySpawn.SpawnEntity(spawnPosition, entity);

        // Only count entities that can die towards the entity count
        if (entity.canDie && !Simulacrum.GameConfig.Entities.IsUnkillable(entity.enemyName))
        {
            entitiesSpawnedInWave++;
        }
    }

    private void OnWaveStartClient(WaveStartMessage message)
    {
        // Only show animation after the first wave
        if (Simulacrum.State.CurrentWave > 1) StartCoroutine(advanceWaveAnimation());
    }

    private void OnIterationEndClient(IterationEndMessage message)
    {
        StartCoroutine(finishIterationAnimation());
    }

    private static IEnumerator advanceWaveAnimation()
    {
        Simulacrum.Interface.ShowLargeMessage("Next wave in 3...", 25, 1);
        yield return new WaitForSeconds(1);
        Simulacrum.Interface.ShowLargeMessage("Next wave in 2...", 25, 1);
        yield return new WaitForSeconds(1);
        Simulacrum.Interface.ShowLargeMessage("Next wave in 1...", 25, 1);
        yield return new WaitForSeconds(1);
        Simulacrum.Interface.ShowLargeMessage("FIGHT OR DIE!", 40, 3);
    }

    private static IEnumerator finishIterationAnimation()
    {
        Simulacrum.Interface.ShowCompletionMessage("Iteration Complete!", "", 3);
        Simulacrum.Environment.UnlockNextArea();
        yield break;
    }

    private static void OnOnEntityRetargetClient(NetworkObjectReference netObjRef)
    {
        Simulacrum.Log.LogInfo("Retarget 1");
        if (netObjRef.TryGet(out var netObj))
        {
            Simulacrum.Log.LogInfo("Retarget 2");
            var entity = netObj.GetComponent<EnemyAI>();
            entity.TargetClosestPlayer();
            if (entity.targetPlayer)
            {
                Simulacrum.Log.LogInfo("Retarget 3");
                entity.HitEnemyOnLocalClient(0, playerWhoHit: entity.targetPlayer);
            }
        }
    }

    private void OnOnEntitySpawnClient(NetworkObjectReference netObjRef)
    {
        if (netObjRef.TryGet(out var netObj))
        {
            StartCoroutine(entitySpawnAnimation(netObj.GetComponent<EnemyAI>()));
        }
    }

    private static IEnumerator entitySpawnAnimation(EnemyAI entity)
    {
        entity.GetComponentInChildren<NavMeshAgent>().isStopped = true;
        yield return new WaitForSeconds(2.5f);
        entity.GetComponentInChildren<NavMeshAgent>().isStopped = false;
    }

    [SimAttributes.HostOnly]
    private void Update()
    {
        if (!NetworkManager.Singleton.IsHost || !Simulacrum.State.IsIterationRunning) return;

        if (isInPreparation && !preparationTimer.Tick()) return;
        isInPreparation = false;

        if (Simulacrum.State.CurrentWave > currentIteration.WaveCount)
        {
            Simulacrum.Log.LogInfo("ENDING ITERATION");
            Simulacrum.Log.LogInfo($"CURRENT WAQVE: {Simulacrum.State.CurrentWave}");
            Simulacrum.Log.LogInfo($"WAVE OUNT: {currentIteration.WaveCount}");
            Simulacrum.State.EndIteration();
        }
        else if (entitiesKilledInWave >= currentWaveEntityCount)
        {
            Simulacrum.Log.LogInfo($"STARTING NEW WAVE (CURRENT: {Simulacrum.State.CurrentWave})");
            Simulacrum.Log.LogInfo($"ENTITIES KILLED: {entitiesKilledInWave}");
            Simulacrum.Log.LogInfo($"ENTITIES IN WAVE: {currentWaveEntityCount}");

            Simulacrum.State.StartNextWave();
        }
        else if (entitiesSpawnedInWave < currentWaveEntityCount && spawnTimer.Tick())
        {
            Simulacrum.Log.LogInfo("SPAWNING ENTITY");

            SpawnRandomEntity();
            spawnTimer.Set(SimUtils.RandomDeviation(currentIteration.AvgEntitySpawnCooldown, 4));
        }

        var iterationString = $"{Simulacrum.State.CurrentIteration + 1}{NumberPrefix(Simulacrum.State.CurrentIteration + 1)}";

        if (!Simulacrum.Gulag.IsPlayerInGulag)
        {
            Simulacrum.Interface.SetInfo(
                new StringBuilder()
                    .AppendLine($"{RichText.Color(RichText.Underlined($"{RichText.Bold($"{iterationString} ITERATION")}\t\t"), "purple")}")
                    .AppendLine($"{RichText.Bold("Wave")}: {Simulacrum.State.CurrentWave}/{currentIteration.WaveCount}")
                    .AppendLine($"{RichText.Bold("Remaining")}: {currentWaveEntityCount - entitiesKilledInWave}")
                    .ToString()
            );
        }
    }

    private static string NumberPrefix(int number) => number.ToString().Last() switch
    {
        '1' => "st",
        '2' => "nd",
        _ => "th"
    };
}

internal readonly struct IterationDefinition
{
    internal int WaveCount { get; init; }
    internal int AvgEntitiesPerWave { get; init; }
    internal float AvgEntitySpawnCooldown { get; init; }
}