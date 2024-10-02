using System.Collections;
using Simulacrum.API.Types;
using Simulacrum.Utils;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Simulacrum.Objects.Events;

// ReSharper disable once ClassNeverInstantiated.Global
// This type needs to be instantiated dynamically at runtime.
public class PufferInvasion : ISimulacrumEvent
{
    private MonoBehaviour eventMaster;

    private Coroutine effectCoroutine;

    public void OnInit(MonoBehaviour eventMasterObj)
    {
        eventMaster = eventMasterObj;
    }

    public void OnStart()
    {
        effectCoroutine = eventMaster.StartCoroutine(effectAnimation());
    }

    public void OnEnd()
    {
        if (effectCoroutine != null) eventMaster.StopCoroutine(effectCoroutine);
    }

    private IEnumerator effectAnimation()
    {
        Object.Instantiate(
            SimAssets.PufferCloudRef, Simulacrum.Player.transform.position, Quaternion.identity,
            RoundManager.Instance.mapPropsContainer.transform
        );

        yield break;
    }
}