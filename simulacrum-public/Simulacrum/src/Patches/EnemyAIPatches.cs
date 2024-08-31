using HarmonyLib;
using Unity.Netcode;

namespace Simulacrum.Patches;

[HarmonyPatch(typeof(EnemyAI))]
internal static class EnemyAIPatches
{
    [HarmonyPrefix, HarmonyPatch("KillEnemy")]
    private static void KillEnemyPatch(EnemyAI __instance)
    {
        if (NetworkManager.Singleton.IsHost) Simulacrum.Waves.OnKillEntity(__instance);
    }
}