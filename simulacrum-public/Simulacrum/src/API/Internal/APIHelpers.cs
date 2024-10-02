namespace Simulacrum.API.Internal;

internal static class APIHelpers
{
    /// <summary>
    ///     Throws an <see cref="SimulacrumAPIException" /> when Simulacrum is not ready to serve API calls.
    /// </summary>
    /// <exception cref="SimulacrumAPIException">When Simulacrum is not ready to serve API calls.</exception>
    internal static void AssertSimulacrumReady()
    {
        if (Simulacrum.IsInitialized) return;

        throw new SimulacrumAPIException("Failed to execute API call. Simulacrum has not yet been initialized.");
    }
}