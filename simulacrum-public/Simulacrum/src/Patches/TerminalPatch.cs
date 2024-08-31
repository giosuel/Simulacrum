using HarmonyLib;
using TMPro;
using UnityEngine;

namespace Simulacrum.Patches;

[HarmonyPatch(typeof(Terminal))]
internal static class TerminalPatch
{
    [HarmonyPostfix, HarmonyPatch("Start")]
    private static void StartPatch()
    {
        var ingamePlayerHud = GameObject.Find("IngamePlayerHUD");
        if (ingamePlayerHud)
        {
            var tipText = ingamePlayerHud.transform.Find("BottomMiddle/SystemsOnline/TipLeft1");
            tipText.GetComponent<TMP_Text>().text = "WELCOME TO THE SIMULACRUM";
            tipText.GetComponent<TMP_Text>().fontSize = 23;
        }
    }
}