using System;

namespace Simulacrum.API.Types;

public struct EventDefinition()
{
    public string Name { get; init; }
    public int Weight { get; init; } = 1;

    public bool IsMutuallyExclusive { get; init; }
    public bool AdvanceIsCancel { get; init; }

    public Type EventType { get; internal set; }
}