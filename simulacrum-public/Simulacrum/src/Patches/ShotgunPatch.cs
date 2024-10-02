using HarmonyLib;
using Unity.Netcode;

namespace Simulacrum.Patches;

[HarmonyPatch(typeof(ShotgunItem))]
internal static class ShotgunPatch
{
    [HarmonyPostfix, HarmonyPatch("ShootGun")]
    private static void ShootGunPatch(ShotgunItem __instance)
    {
        Simulacrum.Players.ShootGun(__instance);
    }

    [HarmonyPostfix, HarmonyPatch("ReloadGunEffectsServerRpc")]
    private static void ReloadGunEffectsServerRpcPatch(ShotgunItem __instance, bool start)
    {
        if (start)
        {
            Simulacrum.Players.StartReloadingGun(__instance);
        }
        else
        {
            Simulacrum.Players.EndReloadingGun(__instance);
        }
    }
}