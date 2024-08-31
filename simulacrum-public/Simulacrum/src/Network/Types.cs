// ReSharper disable Unity.RedundantAttributeOnTarget

using Unity.Netcode;
using UnityEngine;

namespace Simulacrum.Network;

internal readonly struct WeatherChangeMessage
{
    [SerializeField] internal LevelWeatherType Type { get; init; }
    [SerializeField] internal int WeatherSeed { get; init; }
}

internal readonly struct TeleportPlayerRequest
{
    [SerializeField] internal ulong PlayerId { get; init; }
    [SerializeField] internal Vector3 Destination { get; init; }
}

public readonly struct ItemSpawnRequest()
{
    [SerializeField] public string Name { get; init; }
    [SerializeField] public Vector3 SpawnPosition { get; init; } = default;
    [SerializeField] public bool SpawnWithAnimation { get; init; } = true;
}

public readonly struct ItemSpawnResponse()
{
    [SerializeField] public NetworkObjectReference Reference { get; init; }
    [SerializeField] public bool SpawnWithAnimation { get; init; } = true;
}