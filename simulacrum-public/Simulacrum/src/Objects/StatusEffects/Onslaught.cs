using HarmonyLib;
using Simulacrum.API.Types;
using UnityEngine;

namespace Simulacrum.Objects.StatusEffects;

// ReSharper disable once ClassNeverInstantiated.Global
// This type needs to be instantiated dynamically at runtime.
internal class Onslaught : ISumulacrumStatusEffect
{
    private static readonly Harmony OnslaughtHarmony = new(Simulacrum.PLUGIN_GUID + ".Onslaught");

    public void OnActivate(MonoBehaviour effectMaster)
    {
        Simulacrum.Log.LogInfo("ACTIVATE ONSLAUGHT");
        OnslaughtHarmony.PatchAll(typeof(OnslaughtPatches));
    }

    public void OnClear()
    {
        OnslaughtHarmony.UnpatchSelf();
    }

    [HarmonyPatch(typeof(EnemyAI))]
    private static class OnslaughtPatches
    {
        [HarmonyPrefix, HarmonyPatch("HitEnemyOnLocalClient")]
        private static void HitEnemyOnLocalClientPrefixPatch(object[] __args)
        {
            Simulacrum.Log.LogInfo("HIT ENEMY ONSLAUGHT");
            __args[0] = 420;
        }
    }
}