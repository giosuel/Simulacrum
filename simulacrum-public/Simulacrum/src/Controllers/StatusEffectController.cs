using System;
using System.Collections.Generic;
using System.Linq;
using LethalNetworkAPI;
using Simulacrum.API.Types;
using Simulacrum.Network;
using Simulacrum.Utils;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Simulacrum.Controllers;

public class StatusEffectController : MonoBehaviour
{
    private static readonly int FillAmount = Shader.PropertyToID("_FillAmount");
    private readonly Dictionary<string, StatusEffect> activeStatusEffects = [];

    private LNetworkMessage<ActivateStatusEffectRequest> activateEffectMessage;

    private Transform statusEffectContainer;

    private void Awake()
    {
        activateEffectMessage = LNetworkMessage<ActivateStatusEffectRequest>.Connect("ActivateStatusEffect");
        activateEffectMessage.OnClientReceived += OnActivateStatusEffectClient;
        if (NetworkManager.Singleton.IsHost) activateEffectMessage.OnServerReceived += OnActivateStatusEffectServer;

        statusEffectContainer = Instantiate(
            SimAssets.StatusEffectContainer, GameObject.Find("TopRightCorner").transform
        ).transform.Find("Container");
    }

    internal void Apply(string effectName, float durationOverride = -1, bool isGlobal = false)
    {
        if (isGlobal)
        {
            activateEffectMessage.SendServer(new ActivateStatusEffectRequest
            {
                Name = effectName,
            });
            return;
        }

        if (!Simulacrum.StatusEffectRegistry.RegisteredStatusEffects.TryGetValue(effectName, out var effectDefinition))
        {
            Simulacrum.Log.LogError($"[STFX] Failed to find status effect '{effectName}'. Skipping.");
            return;
        }

        Simulacrum.Log.LogInfo($"[STFX] Starting status effect '{effectName}'.");

        // Restart effect time if is applied again
        if (activeStatusEffects.ContainsKey(effectName))
        {
            activeStatusEffects[effectName] = activeStatusEffects[effectName] with
            {
                TimeStarted = Time.realtimeSinceStartup
            };
            return;
        }

        var effectController = (ISumulacrumStatusEffect)Activator.CreateInstance(effectDefinition.EffectType);
        effectController.OnActivate(this);

        var statusEffectBubble = Instantiate(effectDefinition.Bubble, statusEffectContainer);

        activeStatusEffects[effectName] = new StatusEffect
        {
            Object = statusEffectBubble,
            Material = statusEffectBubble.GetComponent<Image>().material,
            Duration = durationOverride < 0 ? effectDefinition.Duration : durationOverride,
            TimeStarted = Time.realtimeSinceStartup,
            Controller = effectController
        };
    }

    private void OnActivateStatusEffectClient(ActivateStatusEffectRequest message) => Apply(message.Name);

    [SimAttributes.HostOnly]
    private void OnActivateStatusEffectServer(ActivateStatusEffectRequest message, ulong clientId)
    {
        activateEffectMessage.SendClients(message);
    }

    internal bool IsActive(string effectName) => activeStatusEffects.ContainsKey(effectName);

    internal List<(string, float)> GetActive() => activeStatusEffects
        .Select(entry => (entry.Key, entry.Value.Duration - (Time.realtimeSinceStartup - entry.Value.TimeStarted)))
        .ToList();

    private void Update()
    {
        var effectsToRemove = new List<string>();
        foreach (var (effectName, effectInstance) in activeStatusEffects)
        {
            var progress = (Time.realtimeSinceStartup - effectInstance.TimeStarted) / effectInstance.Duration;

            if (progress > 1)
            {
                effectsToRemove.Add(effectName);
                effectInstance.Controller.OnClear();
                Destroy(effectInstance.Object);
                continue;
            }

            effectInstance.Controller.OnUpdate();
            effectInstance.Material.SetFloat("_FillAmount", 1 - Mathf.Clamp(progress, 0, 1));
        }

        foreach (var effectToRemove in effectsToRemove)
        {
            activeStatusEffects.Remove(effectToRemove);
        }
    }
}

internal record struct StatusEffect
{
    internal GameObject Object { get; init; }
    internal Material Material { get; init; }
    internal float Duration { get; init; }
    internal float TimeStarted { get; init; }

    internal ISumulacrumStatusEffect Controller { get; init; }
}