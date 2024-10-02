using GameNetcodeStuff;
using HarmonyLib;
using Simulacrum.API.Types;
using UnityEngine;

namespace Simulacrum.Objects.StatusEffects;

// ReSharper disable once ClassNeverInstantiated.Global
// This type needs to be instantiated dynamically at runtime.
internal class Stealth : ISumulacrumStatusEffect
{
    public void OnActivate(MonoBehaviour effectMaster)
    {
        Simulacrum.Log.LogInfo("STEALTH ACTIVATED");
    }

    public void OnClear()
    {

    }
}