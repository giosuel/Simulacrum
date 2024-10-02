using System;
using System.Collections.Generic;
using Simulacrum.API.Types;

namespace Simulacrum.Registries;

public class StatusEffectRegistry
{
    internal Dictionary<string, StatusEffectDefinition> RegisteredStatusEffects { get; } = [];

    internal void RegisterStatusEffect<T>(StatusEffectDefinition effectDefinition) where T : ISumulacrumStatusEffect
    {
        RegisterStatusEffect(effectDefinition, typeof(T));
    }

    internal void RegisterStatusEffect(StatusEffectDefinition effectDefinition, Type type = null)
    {
        effectDefinition.EffectType = type;
        if (!RegisteredStatusEffects.TryAdd(effectDefinition.Name, effectDefinition))
        {
            Simulacrum.Log.LogWarning($"[PWREG] Duplicate status effect detected: {effectDefinition.Name}. Skipping.");
        }
    }
}