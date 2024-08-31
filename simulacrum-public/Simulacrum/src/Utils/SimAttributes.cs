using System;

namespace Simulacrum.Utils;

public class SimAttributes
{
    /// <summary>
    ///     Methods marked with this attribute should only ever be executed on the host. As they require server permissions.
    ///     Can only be called by the host.
    /// </summary>
    internal class HostOnly : Attribute;
}