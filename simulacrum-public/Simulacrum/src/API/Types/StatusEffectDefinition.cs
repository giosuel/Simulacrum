using System;
using UnityEngine;

namespace Simulacrum.API.Types;

public struct StatusEffectDefinition
{
    public string Name { get; init; }
    public float Duration { get; init; }
    public GameObject Bubble { get; init; }

    public Type EffectType { get; internal set; }
}