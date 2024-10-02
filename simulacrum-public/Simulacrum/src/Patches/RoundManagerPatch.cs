using HarmonyLib;
using Simulacrum.Controllers;

namespace Simulacrum.Patches;

[HarmonyPatch(typeof(RoundManager))]
internal static class RoundManagerPatch
{
    [HarmonyPrefix, HarmonyPatch("GeneratedFloorPostProcessing")]
    private static bool GeneratedFloorPostProcessingPatch() => false;

    [HarmonyPrefix, HarmonyPatch("SetLevelObjectVariables")]
    private static bool SetLevelObjectVariablesPatch() => false;

    [HarmonyPrefix, HarmonyPatch("BeginEnemySpawning")]
    private static bool BeginEnemySpawningPatch() => false;

    [HarmonyPostfix, HarmonyPatch("FinishGeneratingLevel")]
    private static void FinishGeneratingNewLevelClientRpcPrefixPatch()
    {
        Simulacrum.Log.LogInfo("FinishGeneratingLevel");
        Simulacrum.Environment.OnLevelPreinit();
    }
}