using Simulacrum.API.Types;
using UnityEngine;

namespace Simulacrum.Objects.StatusEffects;

// ReSharper disable once ClassNeverInstantiated.Global
// This type needs to be instantiated dynamically at runtime.
internal class Speed : ISumulacrumStatusEffect
{
    private float defaultMovementSpeed;
    private float defaultSprintTime;

    public void OnActivate(MonoBehaviour effectMaster)
    {
        defaultMovementSpeed = Simulacrum.Player.movementSpeed;
        defaultSprintTime = Simulacrum.Player.sprintTime;

        Simulacrum.Player.movementSpeed *= 2;
        Simulacrum.Player.sprintTime *= 2;
    }

    public void OnClear()
    {
        Simulacrum.Player.movementSpeed = defaultMovementSpeed;
        Simulacrum.Player.sprintTime = defaultSprintTime;
    }
}