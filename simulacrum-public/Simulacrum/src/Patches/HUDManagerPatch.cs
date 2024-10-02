using HarmonyLib;
using Unity.Netcode;

namespace Simulacrum.Patches;

[HarmonyPatch(typeof(HUDManager))]
internal static class HUDManagerPatch
{
    /// <summary>
    /// Disables re-appearing of HUD elements when they are pinged.
    /// </summary>
    /// <returns></returns>
    [HarmonyPrefix, HarmonyPatch("PingHUDElement")]
    private static bool PingHUDElementPatch() => false;

    /// <summary>
    /// Disables critical hit notification.
    /// </summary>
    [HarmonyPrefix, HarmonyPatch("UpdateHealthUI")]
    private static bool UpdateHealthUIPatch() => false;
}