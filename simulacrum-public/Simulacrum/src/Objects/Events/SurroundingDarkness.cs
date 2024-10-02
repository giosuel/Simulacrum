using System.Collections;
using Simulacrum.API.Types;
using Simulacrum.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Simulacrum.Objects.Events;

// ReSharper disable once ClassNeverInstantiated.Global
// This type needs to be instantiated dynamically at runtime.
public class SurroundingDarkness : ISimulacrumEvent
{
    private const float buildupTime = 3.4f;

    private MonoBehaviour eventMaster;

    private Coroutine effectCoroutine;

    public void OnInit(MonoBehaviour eventMasterObj)
    {
        eventMaster = eventMasterObj;
    }

    public void OnStart()
    {
        effectCoroutine = eventMaster.StartCoroutine(blindnessAnimation());
    }

    public void OnEnd()
    {
        if (effectCoroutine != null) eventMaster.StopCoroutine(effectCoroutine);
        Simulacrum.Player.statusEffectAudio.Stop();
    }

    private static IEnumerator blindnessAnimation()
    {
        Simulacrum.Player.statusEffectAudio.PlayOneShot(SimAssets.SurroundingDarknessSFX);

        yield return new WaitForSeconds(buildupTime);

        Simulacrum.StatusEffects.Apply("Blindness", 26.2f);
    }
}