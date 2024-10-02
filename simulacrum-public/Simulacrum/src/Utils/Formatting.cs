#region

using System;
using System.Globalization;
using UnityEngine;

#endregion

namespace Simulacrum.Utils;

public static class Formatting
{

    /// <summary>
    ///     Creates a formatted string from a unity <see cref="Vector3" />.
    ///     If a unit is provided, the unit will be appended to each scalar.
    ///     Format: "(x[unit](separator)y[unit](separator)z[unit])"
    /// </summary>
    /// <param name="input"></param>
    /// <param name="roundDigits">To how many digits the scalars should be rounded</param>
    /// <param name="separator">Scalar separator</param>
    /// <param name="unit">Optional scalar unit</param>
    /// <returns></returns>
    public static string FormatVector(
        Vector3 input,
        int roundDigits = -1,
        string separator = "/",
        string unit = ""
    )
    {
        var x = roundDigits > -1 ? MathF.Round(input.x, roundDigits) : input.x;
        var y = roundDigits > -1 ? MathF.Round(input.y, roundDigits) : input.y;
        var z = roundDigits > -1 ? MathF.Round(input.z, roundDigits) : input.z;
        return $"({x}{unit}{separator}{y}{unit}{separator}{z}{unit})";
    }
}