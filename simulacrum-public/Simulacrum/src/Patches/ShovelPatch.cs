using HarmonyLib;

namespace Simulacrum.Patches;

[HarmonyPatch(typeof(Shovel))]
internal static class ShovelPatch
{
    [HarmonyPrefix, HarmonyPatch("SwingShovel")]
    private static void SwingShovelPatch(Shovel __instance)
    {
        // Simulacrum.Players.TakeDamage(10);
        Simulacrum.Parcour.PlayerEnter();
    }
}