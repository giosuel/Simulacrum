using HarmonyLib;

namespace Simulacrum.Patches;

[HarmonyPatch(typeof(StormyWeather))]
internal static class StormyWeatherPatch
{
    [HarmonyPrefix, HarmonyPatch("Update")]
    private static void UpdatePatch(StormyWeather __instance) => __instance.getObjectToTargetInterval = 0;
}