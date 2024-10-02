using Simulacrum.API.Internal;
using Simulacrum.API.Types;

namespace Simulacrum.API;

public static class PowerupRegistry
{
    public static void Register(PowerupDefinition definition)
    {
        APIHelpers.AssertSimulacrumReady();

        Simulacrum.PowerupRegistry.RegisterPowerup(definition);
    }
}