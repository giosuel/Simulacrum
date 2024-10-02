using Simulacrum.API.Internal;
using Simulacrum.API.Types;
using Simulacrum.Objects.Events;

namespace Simulacrum.API;

public static class EventRegistry
{
    public static void Register<T>(EventDefinition definition) where T : ISimulacrumEvent
    {
        APIHelpers.AssertSimulacrumReady();

        Simulacrum.EventRegistry.RegisterEvent<T>(definition);
    }
}