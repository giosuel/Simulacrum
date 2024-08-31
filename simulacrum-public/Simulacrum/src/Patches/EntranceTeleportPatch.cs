using HarmonyLib;

namespace Simulacrum.Patches;

[HarmonyPatch(typeof(EntranceTeleport))]
internal static class EntranceTeleportPatch
{
    [HarmonyPrefix, HarmonyPatch("TeleportPlayer")]
    private static bool TeleportPlayerPrefixPatch(EntranceTeleport __instance)
    {
        return !Simulacrum.Environment.TeleportersDisabled;
    }

    [HarmonyPostfix, HarmonyPatch("TeleportPlayer")]
    private static void TeleportPlayerPostfixPatch(EntranceTeleport __instance)
    {
        if (!__instance.isEntranceToBuilding || Simulacrum.Environment.TeleportersDisabled) return;

        Simulacrum.Environment.OnPlayerEnterBuilding();
    }

    [HarmonyPrefix, HarmonyPatch("TeleportPlayerClientRpc")]
    private static void TeleportPlayerClientRpcPatch(EntranceTeleport __instance)
    {
        if (__instance.isEntranceToBuilding || Simulacrum.Environment.TeleportersDisabled) return;

        Simulacrum.Environment.OnPlayerLeaveBuilding();
    }
}