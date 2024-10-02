#region

using System;

#endregion

namespace Simulacrum.API.Internal;

public class SimulacrumAPIException(string message) : Exception(message);