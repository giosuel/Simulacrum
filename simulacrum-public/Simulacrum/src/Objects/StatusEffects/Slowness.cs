using Simulacrum.API.Types;
using UnityEngine;

namespace Simulacrum.Objects.StatusEffects;

// ReSharper disable once ClassNeverInstantiated.Global
// This type needs to be instantiated dynamically at runtime.
internal class Slowness : ISumulacrumStatusEffect
{
    private float defaultMovementSpeed;
    private float defaultSprintTime;

    public void OnActivate(MonoBehaviour effectMaster)
    {
        defaultMovementSpeed = Simulacrum.Player.movementSpeed;
        defaultSprintTime = Simulacrum.Player.sprintTime;

        Simulacrum.Player.movementSpeed *= 0.75f;
        Simulacrum.Player.sprintTime *= 0.75f;
    }

    public void OnClear()
    {
        Simulacrum.Player.movementSpeed = defaultMovementSpeed;
        Simulacrum.Player.sprintTime = defaultSprintTime;
    }
}