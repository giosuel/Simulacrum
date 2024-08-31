using HarmonyLib;

namespace Simulacrum.Patches;

[HarmonyPatch(typeof(StartOfRound))]
internal static class StartOfRoundPatch
{
    [HarmonyPrefix, HarmonyPatch("openingDoorsSequence", MethodType.Enumerator)]
    private static bool openingDoorsSequencePatch()
    {
        Simulacrum.Environment.OnLevelPostload();
        return false;
    }

    [HarmonyPrefix, HarmonyPatch("TeleportPlayerInShipIfOutOfRoomBounds")]
    private static bool TeleportPlayerInShipIfOutOfRoomBoundsPatch() => false;
}