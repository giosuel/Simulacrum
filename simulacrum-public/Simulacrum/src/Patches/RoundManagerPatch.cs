using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace Simulacrum.Patches;

[HarmonyPatch(typeof(RoundManager))]
internal static class RoundManagerPatch
{
    [HarmonyPostfix, HarmonyPatch("RefreshEnemiesList")]
    private static void RefreshEnemiesListPatch() => Simulacrum.OnShipLand();

    [HarmonyPrefix, HarmonyPatch("GeneratedFloorPostProcessing")]
    private static bool GeneratedFloorPostProcessingPatch() => false;

    [HarmonyPrefix, HarmonyPatch("BeginEnemySpawning")]
    private static bool BeginEnemySpawningPatch() => false;
}