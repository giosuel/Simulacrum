using System;

namespace Simulacrum.Utils;

public abstract class SimAttributes
{
    /// <summary>
    ///     Methods marked with this attribute are only ever executed on the host.
    ///     Can only be called by the host.
    /// </summary>
    internal class HostOnly : Attribute;
}