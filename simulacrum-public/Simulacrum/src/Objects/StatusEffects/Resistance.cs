using GameNetcodeStuff;
using HarmonyLib;
using Simulacrum.API.Types;
using UnityEngine;

namespace Simulacrum.Objects.StatusEffects;

// ReSharper disable once ClassNeverInstantiated.Global
// This type needs to be instantiated dynamically at runtime.
internal class Resistance : ISumulacrumStatusEffect
{
    private const float resistanceMultiplier = 0.3f;

    private static readonly Harmony BlindnessHarmony = new(Simulacrum.PLUGIN_GUID + ".Resistance");

    public void OnActivate(MonoBehaviour effectMaster)
    {
        BlindnessHarmony.PatchAll(typeof(ResistancePatches));
    }

    public void OnClear()
    {
        BlindnessHarmony.UnpatchSelf();
    }

    [HarmonyPatch(typeof(PlayerControllerB))]
    private static class ResistancePatches
    {
        [HarmonyPrefix, HarmonyPatch("DamagePlayer"), HarmonyPriority(Priority.Low)]
        private static void DamagePlayerPrefixPatch(object[] __args)
        {
            __args[0] = Mathf.FloorToInt((int)__args[0] * resistanceMultiplier);
        }
    }
}