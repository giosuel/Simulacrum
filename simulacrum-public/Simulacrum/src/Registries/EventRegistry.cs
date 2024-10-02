using System.Collections.Generic;
using System.Linq;
using Simulacrum.API.Types;
using Simulacrum.Objects.Events;
using Simulacrum.Utils;

namespace Simulacrum.Registries;

public class EventRegistry
{
    internal Dictionary<string, EventDefinition> RegisteredEvents { get; } = [];

    private readonly WeightedListHolder<string> eventWeightHolder = new();

    internal void RegisterEvent<T>(EventDefinition eventDefinition) where T : ISimulacrumEvent
    {
        eventDefinition.EventType = typeof(T);
        if (!RegisteredEvents.TryAdd(eventDefinition.Name, eventDefinition))
        {
            Simulacrum.Log.LogWarning($"[PWREG] Duplicate event detected: {eventDefinition.Name}. Skipping.");
        }
    }

    internal string PickRandomWeightedEvent()
    {
        return eventWeightHolder.PickRandomWeightedEvent(
            RegisteredEvents.Select(entry => (entry.Key, entry.Value.Weight)).ToList()
        );
    }
}