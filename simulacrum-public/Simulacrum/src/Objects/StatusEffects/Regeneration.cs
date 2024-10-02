using Simulacrum.API.Types;
using Simulacrum.Utils;
using UnityEngine;

namespace Simulacrum.Objects.StatusEffects;

// ReSharper disable once ClassNeverInstantiated.Global
// This type needs to be instantiated dynamically at runtime.
internal class Regeneration : ISumulacrumStatusEffect
{
    private const int restoreAmount = 40;
    private const float restoreTime = 6f;
    private const float tickInterval = 0.25f;

    private int tickRestore;

    private readonly SimTimer regenerationTimer = SimTimer.ForInterval(tickInterval);

    public void OnActivate(MonoBehaviour effectMaster)
    {
        Simulacrum.Players.CurrentRegenerationAmount = restoreAmount;
        tickRestore = Mathf.RoundToInt(restoreAmount / (restoreTime / tickInterval));
        Simulacrum.Players.CurrentRegenerationAmount = restoreAmount;
    }

    public void OnClear()
    {
        Simulacrum.Players.CurrentRegenerationAmount = 0;
    }

    public void OnUpdate()
    {
        if (regenerationTimer.Tick())
        {
            Simulacrum.Players.HealDamage(tickRestore);
        }
    }
}