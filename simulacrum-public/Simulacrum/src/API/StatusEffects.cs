using System.Collections.Generic;
using Simulacrum.API.Internal;
using Simulacrum.API.Types;

namespace Simulacrum.API;

public class StatusEffects
{
    /// <summary>
    /// Registers a new status effect.
    /// </summary>
    /// <param name="definition">The status effect defintion</param>
    /// <typeparam name="T">The type of the event controller that is responsible for the effect lifecycle</typeparam>
    public static void Register<T>(StatusEffectDefinition definition) where T : ISumulacrumStatusEffect
    {
        APIHelpers.AssertSimulacrumReady();

        Simulacrum.StatusEffectRegistry.RegisterStatusEffect<T>(definition);
    }

    /// <summary>
    /// Registers a new status effect.
    /// </summary>
    /// <param name="definition">The status effect definition</param>
    public static void Register(StatusEffectDefinition definition)
    {
        APIHelpers.AssertSimulacrumReady();

        Simulacrum.StatusEffectRegistry.RegisterStatusEffect(definition);
    }

    /// <summary>
    /// Applies a status effect to the player.
    /// </summary>
    /// <param name="name">The name of the status effect</param>
    /// <param name="durationOverride">How long the effect lasts. If set to -1, the duration from the definition is used instead.</param>
    /// <param name="isGlobal">Whether all players should receive the effect</param>
    public void Apply(string name, float durationOverride = -1, bool isGlobal = false)
    {
        APIHelpers.AssertSimulacrumReady();

        Simulacrum.StatusEffects.Apply(name, durationOverride, isGlobal);
    }

    /// <summary>
    /// Checks if the player has a certain status effect.
    /// </summary>
    /// <param name="name">The name of the status effect</param>
    /// <returns></returns>
    public bool IsActive(string name)
    {
        APIHelpers.AssertSimulacrumReady();

        return Simulacrum.StatusEffects.IsActive(name);
    }

    /// <summary>
    /// Returns a list of all active events and their remaining duration.
    /// </summary>
    /// <returns></returns>
    public List<(string, float)> GetActive()
    {
        APIHelpers.AssertSimulacrumReady();

        return Simulacrum.StatusEffects.GetActive();
    }
}