using GameNetcodeStuff;
using HarmonyLib;

namespace Simulacrum.Patches;

[HarmonyPatch(typeof(PlayerControllerB))]
internal static class PlayerControllerPatch
{
    [HarmonyPostfix, HarmonyPatch("ConnectClientToPlayerObject")]
    private static void ConnectClientToPlayerObjectPatch(PlayerControllerB __instance)
    {
        if (GameNetworkManager.Instance.localPlayerController != __instance) return;

        Simulacrum.Launch(__instance);
    }

    [HarmonyPostfix, HarmonyPatch("SetHoverTipAndCurrentInteractTrigger")]
    private static void SetHoverTipAndCurrentInteractTriggerPatch(PlayerControllerB __instance)
    {
        if (Simulacrum.SetupScene.IsInSetupScene && !string.IsNullOrEmpty(__instance.cursorTip.text))
        {
            __instance.cursorTip.text = "Are you ready?";
        }
    }

    [HarmonyPrefix, HarmonyPatch("DiscardHeldObject")]
    private static bool DiscardHeldObjectPatch() => false;

    [HarmonyPrefix, HarmonyPatch("ScrollMouse_performed")]
    private static bool ScrollMouse_performedPatch() => false;
}