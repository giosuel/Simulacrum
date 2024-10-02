using HarmonyLib;
using Simulacrum.Controllers;
using Unity.Netcode;

namespace Simulacrum.Patches;

[HarmonyPatch(typeof(StartOfRound))]
internal static class StartOfRoundPatch
{
    [HarmonyPrefix, HarmonyPatch("Start")]
    private static void StartPatch()
    {
        EnvironmentController.Levels = Simulacrum.GameConfig.Environment
            .FilterDisabledMoons(LethalLevelLoader.PatchedContent.ExtendedLevels)
            .ToArray();
        Simulacrum.GameConfig.Sheets.CheckMoonSheets(EnvironmentController.Levels);
    }

    [HarmonyPrefix, HarmonyPatch("TeleportPlayerInShipIfOutOfRoomBounds")]
    private static bool TeleportPlayerInShipIfOutOfRoomBoundsPatch() => false;
}