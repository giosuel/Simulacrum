// ReSharper disable Unity.RedundantAttributeOnTarget

using System;
using Unity.Netcode;
using UnityEngine;

namespace Simulacrum.Network;

/*
 * Server broadcasts and updates
 */
internal readonly struct WeatherChangeMessage
{
    [SerializeField] internal LevelWeatherType Type { get; init; }
    [SerializeField] internal int WeatherSeed { get; init; }
}

internal readonly struct WaveStartMessage
{
    [SerializeField] public int WaveNumber { get; init; }
}

internal readonly struct IterationStartMessage
{
    [SerializeField] public int IterationNumber { get; init; }
    [SerializeField] public int WaveCount { get; init; }
}

internal readonly struct IterationEndMessage
{
    [SerializeField] public int IterationNumber { get; init; }
}

internal readonly struct EventExecutionMessage
{
    [SerializeField] public string EventName { get; init; }
}

public readonly struct PowerupSpawnMessage
{
    [SerializeField] public string PowerupName { get; init; }
    [SerializeField] public Vector3 Position { get; init; }
}

/*
 * Client requests and responses
 */
internal readonly struct PowerupConsumeRequest
{
    [SerializeField] public Vector3 Position { get; init; }
}

internal readonly struct TeleportPlayerRequest
{
    [SerializeField] internal ulong PlayerId { get; init; }

    // If set to null, the server will pick a random free node as the destination
    [SerializeField] internal Vector3? Destination { get; init; }
}

internal readonly struct TeleportPlayerResponse
{
    [SerializeField] internal ulong PlayerId { get; init; }
    [SerializeField] internal Vector3 Destination { get; init; }
}

internal readonly struct ItemSpawnRequest()
{
    [SerializeField] public string Name { get; init; }
    [SerializeField] public Vector3 SpawnPosition { get; init; } = default;
    [SerializeField] public bool SpawnWithAnimation { get; init; } = true;
}

internal readonly struct ItemSpawnResponse()
{
    [SerializeField] public NetworkObjectReference Reference { get; init; }
    [SerializeField] public bool SpawnWithAnimation { get; init; } = true;
}

internal readonly struct EntitySpawnResponse
{
    [SerializeField] public NetworkObjectReference Reference { get; init; }
}

internal readonly struct EntitySpawnRequest()
{
    [SerializeField] public string Name { get; init; }
    [SerializeField] public Vector3 SpawnPosition { get; init; } = default;
    [SerializeField] public int Amount { get; init; } = 1;
}

internal readonly struct ActivateStatusEffectRequest
{
    [SerializeField] public string Name { get; init; }
}