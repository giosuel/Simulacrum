using HarmonyLib;

namespace Simulacrum.Patches;

[HarmonyPatch(typeof(ElevatorAnimationEvents))]
internal static class ElevatorAnimationEventsPatch
{
    [HarmonyPrefix, HarmonyPatch("ElevatorFullyRunning")]
    private static bool ElevatorFullyRunningPatch() => false;
}