using System.Collections.Generic;
using LethalNetworkAPI;
using Simulacrum.Network;
using Simulacrum.Objects;
using Simulacrum.Utils;
using Unity.Netcode;
using UnityEngine;

namespace Simulacrum.Controllers;

internal class PowerupController : MonoBehaviour
{
    private int[] powerupsPerWave;

    private LNetworkMessage<PowerupSpawnMessage> onPowerupSpawn;
    private LNetworkMessage<PowerupConsumeRequest> onPowerupConsume;

    private readonly SimTimer spawnTimer = SimTimer.ForInterval(0);

    private Transform container;

    private readonly Dictionary<Vector3, Powerup> activePowerups = [];

    private void Awake()
    {
        container = new GameObject("PowerupContainer").transform;
        container.SetParent(transform);

        onPowerupSpawn = LNetworkMessage<PowerupSpawnMessage>.Connect("OnPowerupSpawn");
        onPowerupConsume = LNetworkMessage<PowerupConsumeRequest>.Connect("OnPowerupConsume");

        onPowerupSpawn.OnClientReceived += OnPowerupSpawnClient;
        onPowerupConsume.OnClientReceived += OnPowerupConsumeClient;

        Simulacrum.State.onIterationEndClients += OnIterationEndClient;

        if (NetworkManager.Singleton.IsHost)
        {
            onPowerupConsume.OnServerReceived += OnPowerupConsumeServer;

            Simulacrum.State.onIterationStartServer += OnIterationStartServerServer;
        }
    }

    [SimAttributes.HostOnly]
    private void OnIterationStartServerServer(int waveCount)
    {
        var powerupCount = Mathf.RoundToInt(SimUtils.GenerateRandomDistributedValue(
            Simulacrum.GameConfig.Powerups.RollLowerBound.Value * Simulacrum.GameConfig.General.DifficultyMultiplier.Value,
            Simulacrum.GameConfig.Powerups.RollUpperBound.Value * Simulacrum.GameConfig.General.DifficultyMultiplier.Value,
            Simulacrum.GameConfig.Powerups.AmountDistribution.Value,
            Simulacrum.GameConfig.Powerups.AmountNormalDistributionMean.Value,
            Simulacrum.GameConfig.Powerups.AmountNormalDistributionStdDev.Value,
            randomGenerator: SimUtils.UniformRandomNumber
        ));

        powerupsPerWave = SimUtils.DistributeItemsOverWaves(powerupCount, waveCount);

        // TODO(giosuel): Reove debug line
        for (var i = 0; i < powerupsPerWave.Length; i++)
        {
            powerupsPerWave[i]++;
        }

        Simulacrum.Log.LogInfo($"[Powerup] Iteration Start, powerups: {powerupCount}");
        for (var i = 0; i < powerupsPerWave.Length; i++)
        {
            Simulacrum.Log.LogInfo($"  WAVE: {i}, POWERUPS: {powerupsPerWave[i]}");
        }
    }

    private void OnIterationEndClient(IterationEndMessage iterationEndMessage)
    {
        foreach (var powerup in activePowerups.Values) Destroy(powerup.gameObject);
        activePowerups.Clear();
    }

    private void OnPowerupSpawnClient(PowerupSpawnMessage message)
    {
        var powerupDefinition = Simulacrum.PowerupRegistry.RegisteredPowerups[message.PowerupName];
        var powerupObj = Instantiate(powerupDefinition.Prefab, container);
        powerupObj.transform.position = message.Position;
        powerupObj.SetActive(true);

        var powerup = powerupObj.AddComponent<Powerup>();
        powerup.Init(powerupDefinition.OnConsume, powerupDefinition.LookingAtPlayer);

        if (!activePowerups.TryAdd(message.Position, powerup))
        {
            Simulacrum.Log.LogError(
                $"[PWUP] Duplicate powerup position found: {Formatting.FormatVector(message.Position)}"
            );
        }
    }

    [SimAttributes.HostOnly]
    private void OnPowerupConsumeServer(PowerupConsumeRequest request, ulong clientId)
    {
        onPowerupConsume.SendClients(request);
    }

    private void OnPowerupConsumeClient(PowerupConsumeRequest request)
    {
        Simulacrum.Log.LogInfo("POWERUP CONSUME");
        if (!activePowerups.TryGetValue(request.Position, out var powerup))
        {
            Simulacrum.Log.LogError(
                $"[PWUP] Failed to find powerup to remove at position: {Formatting.FormatVector(request.Position)}"
            );
            return;
        }

        Destroy(powerup.gameObject);
        activePowerups.Remove(request.Position);
    }

    [SimAttributes.HostOnly]
    private void Update()
    {
        // The wave number is -1 during preparation time, meaning we need to look at wave 0 during that time
        var currentWave = Mathf.Max(Simulacrum.State.CurrentWave, 0);

        if (Simulacrum.State.IsIterationRunning && powerupsPerWave[currentWave] > 0 && spawnTimer.Tick())
        {
            if (Random.Range(0f, 1f) > Mathf.Clamp(Simulacrum.GameConfig.Powerups.SpawnChance.Value, 0, 1)) return;

            onPowerupSpawn.SendClients(new PowerupSpawnMessage
            {
                PowerupName = Simulacrum.PowerupRegistry.PickRandomWeightedPowerup(),
                Position = Simulacrum.Environment.FindFreeNode()
            });

            powerupsPerWave[currentWave]--;
            spawnTimer.Set(Random.Range(4, 6));
        }
    }
}