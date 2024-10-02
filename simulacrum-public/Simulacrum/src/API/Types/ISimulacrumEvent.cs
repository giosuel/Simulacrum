using UnityEngine;

namespace Simulacrum.API.Types;

/// <summary>
/// A Simulacrum event. Events are things that can happen during the course of a wave. They can be player buffs, debuffs or
/// environmental / entity changes that alter the gameplay experience of the player in some way. The duration of an event
/// and the amount of events can be set in the configs.
///
/// Events can overlap or last over multiple waves and iterations. Events that are mutually exclusive or only last one wave,
/// are cancelled when a new event is started or the current wave advances.
/// </summary>
public interface ISimulacrumEvent
{
    /// <summary>
    /// Called when the event is initialized at the beginning of the game.
    /// </summary>
    /// <param name="eventMaster">A mono behaviour that can be used to start coroutines if necessary</param>
    public void OnInit(MonoBehaviour eventMaster)
    {
    }

    /// <summary>
    /// Called when the event is executed.
    /// </summary>
    public void OnStart()
    {
    }

    /// <summary>
    /// Called when the wave is ended by another event or the end of the wave or iteration.
    /// </summary>
    public void OnEnd()
    {
    }

    /// <summary>
    /// Called every frame.
    ///
    /// Note: This is called even when the event is not currently active!
    /// </summary>
    public void OnUpdate()
    {
    }
}