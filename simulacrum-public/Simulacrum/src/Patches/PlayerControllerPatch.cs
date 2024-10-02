using System;
using System.Diagnostics;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Simulacrum.Patches;

[HarmonyPatch(typeof(PlayerControllerB))]
internal static class PlayerControllerPatch
{
    [HarmonyPostfix, HarmonyPatch("ConnectClientToPlayerObject")]
    private static void ConnectClientToPlayerObjectPatch(PlayerControllerB __instance)
    {
        if (GameNetworkManager.Instance.localPlayerController != __instance) return;

        Simulacrum.Launch(__instance);

        __instance.movementSpeed = Simulacrum.GameConfig.Player.MovementSpeed.Value;
        __instance.sprintTime = Simulacrum.GameConfig.Player.SprintTime.Value;
    }

    [HarmonyPostfix, HarmonyPatch("SetHoverTipAndCurrentInteractTrigger")]
    private static void SetHoverTipAndCurrentInteractTriggerPatch(PlayerControllerB __instance)
    {
        if (Simulacrum.SetupScene.IsInSetupScene && !string.IsNullOrEmpty(__instance.cursorTip.text))
        {
            __instance.cursorTip.text = "Are you ready?";
        }
    }

    [HarmonyPrefix, HarmonyPatch("ScrollMouse_performed")]
    private static bool ScrollMouse_performedPatch(PlayerControllerB __instance, InputAction.CallbackContext context)
    {
        // var offset = context.ReadValue<float>() > 0f ? 1 : -1;
        // var nextSlot = __instance.currentItemSlot + offset % 4;
        // if (nextSlot < 0) nextSlot = 4 - nextSlot;
        // return __instance.ItemSlots[nextSlot] != null;
        return true;
    }

    [HarmonyPrefix, HarmonyPatch("Discard_performed")]
    private static bool Discard_performedPatch() => false;

    private static int oneshotDamage => Mathf.FloorToInt(
        Simulacrum.GameConfig.Player.OneshotDamageMultiplier.Value *
        Simulacrum.GameConfig.Player.IncomingDamageMultiplier.Value *
        100
    );

    [HarmonyPrefix, HarmonyPatch("DamagePlayer"), HarmonyPriority(Priority.VeryLow)]
    private static void DamagePlayerPrefixPatch(object[] __args)
    {
        __args[0] = Mathf.FloorToInt((int)__args[0] * Simulacrum.GameConfig.Player.IncomingDamageMultiplier.Value);
        Simulacrum.Log.LogInfo("DamagePlayer Called");
    }

    [HarmonyPostfix, HarmonyPatch("DamagePlayer"), HarmonyPriority(Priority.VeryLow)]
    private static void DamagePlayerPostfixPatch(int damageNumber)
    {
        Simulacrum.Players.TakeDamage(damageNumber);
    }

    [HarmonyPrefix, HarmonyPatch("KillPlayer")]
    private static bool KillPlayerPatch(PlayerControllerB __instance, Vector3 bodyVelocity, CauseOfDeath causeOfDeath)
    {
        if (__instance != Simulacrum.Player) return true;

        if (Simulacrum.Gulag.IsPlayerInGulag)
        {
            Simulacrum.Gulag.DieInGulag();
            return false;
        }

        if (Simulacrum.Gulag.PlayerDiedIndefinitely) return true;

        Simulacrum.Log.LogInfo("KillPlayer Called 2");

        // Convert oneshot kills into oneshot damage if the player is healthy enough
        if (causeOfDeath == CauseOfDeath.Mauling && __instance.health > oneshotDamage)
        {
            // Ignore jester dmaage as jesters are not wave entities
            if (new StackTrace().GetFrame(1).GetMethod().ReflectedType == typeof(JesterAI)) return true;

            __instance.DamagePlayer(
                Mathf.FloorToInt(100 * Simulacrum.GameConfig.Player.OneshotDamageMultiplier.Value),
                causeOfDeath: causeOfDeath,
                force: bodyVelocity
            );

            __instance.inAnimationWithEnemy = null;

            return false;
        }

        Simulacrum.Log.LogInfo("KillPlayer Called 3");

        Simulacrum.Gulag.EnterGulag();
        __instance.SpawnDeadBody((int)__instance.playerClientId, bodyVelocity, (int)causeOfDeath, __instance);

        Simulacrum.Log.LogInfo("KillPlayer Called 3");
        return false;
    }
}