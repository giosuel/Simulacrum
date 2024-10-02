using System;
using UnityEngine;

namespace Simulacrum.API.Types;

public readonly struct PowerupDefinition()
{
    public string Name { get; init; }
    public Action OnConsume { get; init; }

    public bool LookingAtPlayer { get; init; }
    public int Weight { get; init; } = 1;

    public GameObject Prefab { get; init; }
}