using HarmonyLib;

namespace Simulacrum.Patches;

[HarmonyPatch(typeof(TimeOfDay))]
internal static class TimeOfDayPatch
{
    [HarmonyPrefix, HarmonyPatch("MoveGlobalTime")]
    private static bool MoveGlobalTimePatch() => false;
}