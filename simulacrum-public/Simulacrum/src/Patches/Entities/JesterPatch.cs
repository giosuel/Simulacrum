using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Simulacrum.Patches.Entities;

[HarmonyPatch(typeof(JesterAI))]
internal static class JesterPatch
{
    private static readonly HashSet<JesterAI> spawnedJesters = [];

    [HarmonyPostfix, HarmonyPatch("SetJesterInitialValues")]
    private static void SetJesterInitialValuesPatch(JesterAI __instance)
    {
        if (!Simulacrum.Gulag.IsPlayerInGulag) return;

        if (!spawnedJesters.Contains(__instance))
        {
            __instance.popUpTimer = Random.Range(18, 23);
            __instance.beginCrankingTimer = 0f;
            spawnedJesters.Add(__instance);
        }
        else
        {
            __instance.popUpTimer = Random.Range(25, 30);
            __instance.beginCrankingTimer = Random.Range(3, 8);
        }
    }
}