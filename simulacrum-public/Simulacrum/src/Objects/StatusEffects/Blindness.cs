using System.Collections;
using Simulacrum.API.Types;
using Simulacrum.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Simulacrum.Objects.StatusEffects;

// ReSharper disable once ClassNeverInstantiated.Global
// This type needs to be instantiated dynamically at runtime.
internal class Blindness : ISumulacrumStatusEffect
{
    private MonoBehaviour effectMaster;

    private Light light;

    private const float transitionTime = 0.3f;
    private const float waitingTime = 25.9f;

    private const float lampMaxIntensity = 900f;

    private Coroutine effectCoroutine;

    private Volume skyVolume;
    private Vignette vignette;

    private float sunLightStartIntensity;
    private float indirectLightStartIntensity;

    public void OnActivate(MonoBehaviour effectMasterObj)
    {
        effectMaster = effectMasterObj;

        var lightObj = new GameObject("Light");
        lightObj.transform.SetParent(Simulacrum.Player.gameplayCamera.transform);
        lightObj.transform.localPosition = Vector3.up * 5f;
        light = lightObj.AddComponent<Light>();
        light.intensity = 0;

        var hdrpLight = lightObj.gameObject.AddComponent<HDAdditionalLightData>();
        hdrpLight.affectsVolumetric = false;

        var sunContainer = GameObject.Find("SunAnimContainer").transform;
        skyVolume = sunContainer.Find("Sky and Fog Global Volume").GetComponent<Volume>();
        var mainVolumeProfile = GameObject.Find("VolumeMain").GetComponent<Volume>().profile;
        if (!mainVolumeProfile.TryGet(out vignette))
        {
            vignette = mainVolumeProfile.Add<Vignette>();
            vignette.mode.SetValue(new VignetteModeParameter(VignetteMode.Masked));
            vignette.mode.overrideState = true;
            vignette.mask.SetValue(new Texture2DParameter(SimAssets.SurroundingDarknessVignette));
            vignette.mask.overrideState = true;
            vignette.active = true;
            vignette.opacity.SetValue(new FloatParameter(0));
            vignette.opacity.overrideState = true;
        }

        light.intensity = 0;
        sunLightStartIntensity = TimeOfDay.Instance.sunDirect.intensity;
        indirectLightStartIntensity = TimeOfDay.Instance.sunIndirect.intensity;

        effectCoroutine = effectMasterObj.StartCoroutine(blindnessAnimation());
    }

    public void OnClear()
    {
        if (effectCoroutine != null) effectMaster.StopCoroutine(effectCoroutine);

        light.intensity = 0;
        TimeOfDay.Instance.sunDirect.intensity = sunLightStartIntensity;
        TimeOfDay.Instance.sunIndirect.intensity = indirectLightStartIntensity;
        skyVolume.weight = 1;
        vignette.opacity.SetValue(new FloatParameter(0));

        Simulacrum.Player.statusEffectAudio.Stop();
    }

    private IEnumerator blindnessAnimation()
    {
        var elapsedTime = 0f;

        /*
         * Fade in
         */
        while (elapsedTime < transitionTime)
        {
            yield return new WaitForEndOfFrame();

            var t = elapsedTime / transitionTime;

            light.intensity = Mathf.Lerp(0, lampMaxIntensity, t);
            TimeOfDay.Instance.sunDirect.intensity = Mathf.Lerp(sunLightStartIntensity, 0, t);
            TimeOfDay.Instance.sunIndirect.intensity = Mathf.Lerp(indirectLightStartIntensity, 0, t);
            skyVolume.weight = 1 - t;
            vignette.opacity.SetValue(new FloatParameter(t));

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        /*
         * Holding
         */
        elapsedTime = 0;
        while (elapsedTime < waitingTime)
        {
            yield return new WaitForEndOfFrame();

            TimeOfDay.Instance.sunDirect.intensity = 0;
            TimeOfDay.Instance.sunIndirect.intensity = 0;
            skyVolume.weight = 0;
            vignette.opacity.SetValue(new FloatParameter(1));

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        /*
         * Fade out
         */
        elapsedTime = 0f;
        while (elapsedTime < transitionTime)
        {
            yield return new WaitForEndOfFrame();

            var t = elapsedTime / transitionTime;

            light.intensity = Mathf.Lerp(lampMaxIntensity, 0, t);
            TimeOfDay.Instance.sunDirect.intensity = Mathf.Lerp(0, sunLightStartIntensity, t);
            TimeOfDay.Instance.sunIndirect.intensity = Mathf.Lerp(0, indirectLightStartIntensity, t);
            skyVolume.weight = t;
            vignette.opacity.SetValue(new FloatParameter(1 - t));

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }
}