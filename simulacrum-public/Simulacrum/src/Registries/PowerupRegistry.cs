using System;
using System.Collections.Generic;
using System.Linq;
using Simulacrum.API.Types;
using Simulacrum.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Simulacrum.Registries;

public class PowerupRegistry
{
    internal readonly Dictionary<string, PowerupDefinition> RegisteredPowerups = [];

    private readonly WeightedListHolder<string> powerupWeightHolder = new();

    internal void RegisterPowerup(PowerupDefinition powerup)
    {
        if (!RegisteredPowerups.TryAdd(powerup.Name, powerup))
        {
            Simulacrum.Log.LogWarning($"[PWREG] Duplicate powerup detected: {powerup.Name}. Skipping.");
        }
    }

    internal string PickRandomWeightedPowerup()
    {
        return RegisteredPowerups[RegisteredPowerups.Keys.ToList()[Random.Range(0, RegisteredPowerups.Count)]].Name;
        // return powerupWeightHolder.PickRandomWeightedEvent(
        //     RegisteredPowerups.Select(entry => (entry.Key, entry.Value.Weight)).ToList()
        // );
    }
}