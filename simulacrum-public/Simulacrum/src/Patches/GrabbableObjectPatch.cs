using HarmonyLib;

namespace Simulacrum.Patches;

[HarmonyPatch(typeof(GrabbableObject))]
internal static class GrabbableObjectPatch
{
    [HarmonyPrefix, HarmonyPatch("GrabItemOnClient")]
    private static void GrabItemOnClientPatch()
    {
        if (Simulacrum.SetupScene.IsInSetupScene) Simulacrum.SetupScene.onPlayerReady.InvokeServer();
    }

    [HarmonyPostfix, HarmonyPatch("EquipItem")]
    private static void EquipItemPatch(GrabbableObject __instance)
    {
        Simulacrum.Players.GrabItem(__instance);
    }
}