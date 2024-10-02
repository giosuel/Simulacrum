using System;
using UnityEngine;

namespace Simulacrum.API.Types;

/// <summary>
/// A Simulacrum status effect. Status effects are things that can buff or debuff a player. They can be activated
/// by picking up powerups or through events or other means.
/// </summary>
public interface ISumulacrumStatusEffect
{
    /// <summary>
    /// Called when the event is executed.
    /// <param name="effectMaster">A mono behaviour that can be used to start coroutines if necessary</param>
    /// </summary>
    public void OnActivate(MonoBehaviour effectMaster)
    {
    }

    /// <summary>
    /// Called when the player cleanses the status effect. This is also called between teleports.
    /// The object will be destroyed after this.
    /// </summary>
    public void OnClear()
    {
    }

    /// <summary>
    /// Called every frame.
    /// </summary>
    public void OnUpdate()
    {
    }
}