using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using LethalNetworkAPI;
using Simulacrum.API.Types;
using Simulacrum.Network;
using Simulacrum.Objects.Events;
using Simulacrum.Utils;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Simulacrum.Controllers;

internal class EventController : MonoBehaviour
{
    private int[] eventsPerWave;

    private readonly SimTimer eventTimer = SimTimer.ForInterval(0);

    private LNetworkMessage<EventExecutionMessage> onExecuteEvent;

    private readonly Dictionary<string, ISimulacrumEvent> initializedEvents = [];
    private readonly Dictionary<string, ISimulacrumEvent> currentlyRunningEvents = [];

    private void Awake()
    {
        onExecuteEvent = LNetworkMessage<EventExecutionMessage>.Connect("OnExecuteEvent");
        onExecuteEvent.OnClientReceived += OnEventExecutedClient;

        Simulacrum.State.onIterationEndClients += OnIterationEndClient;
        Simulacrum.State.onWaveStartClients += OnWaveStartClient;

        foreach (var registeredEvent in Simulacrum.EventRegistry.RegisteredEvents.Values)
        {
            var eventInstance = (ISimulacrumEvent)Activator.CreateInstance(registeredEvent.EventType);
            eventInstance.OnInit(this);
            initializedEvents[registeredEvent.Name] = eventInstance;
        }

        if (!NetworkManager.Singleton.IsHost)
        {
            enabled = false;
            return;
        }

        Simulacrum.State.onIterationStartServer += OnIterationStartServerServer;
        Simulacrum.State.onWaveStartServer += OnWaveStartServerServer;
    }

    [SimAttributes.HostOnly]
    private void OnIterationStartServerServer(int waveCount)
    {
        var eventCount = Mathf.RoundToInt(SimUtils.GenerateRandomDistributedValue(
            Simulacrum.GameConfig.Events.RollLowerBound.Value * Simulacrum.GameConfig.General.DifficultyMultiplier.Value,
            Simulacrum.GameConfig.Events.RollUpperBound.Value * Simulacrum.GameConfig.General.DifficultyMultiplier.Value,
            Simulacrum.GameConfig.Events.AmountDistribution.Value,
            Simulacrum.GameConfig.Events.AmountNormalDistributionMean.Value,
            Simulacrum.GameConfig.Events.AmountNormalDistributionStdDev.Value,
            randomGenerator: SimUtils.UniformRandomNumber
        ));

        eventsPerWave = SimUtils.DistributeItemsOverWaves(eventCount, waveCount);

        // TODO(giosuel): Reove debug line
        for (var i = 0; i < eventsPerWave.Length; i++)
        {
            eventsPerWave[i]++;
        }

        Simulacrum.Log.LogInfo($"[Event] Iteration Start, events: {eventCount}");
        for (var i = 0; i < eventsPerWave.Length; i++)
        {
            Simulacrum.Log.LogInfo($"  WAVE: {i}, EVENTS: {eventsPerWave[i]}");
        }

        // TODO(giosuel): DEBUG
        eventTimer.Set(Random.Range(5, 5));
        // eventTimer.Set(Random.Range(15, 28));
    }

    [SimAttributes.HostOnly]
    private void OnWaveStartServerServer()
    {
        eventTimer.Set(Random.Range(5, 5));
        eventTimer.Set(Random.Range(15, 28));
    }

    private void OnWaveStartClient(WaveStartMessage message)
    {
        RemoveAdvanceCancelEvents();
    }

    private void OnIterationEndClient(IterationEndMessage message)
    {
        RemoveAdvanceCancelEvents();
    }

    private void RemoveAdvanceCancelEvents()
    {
        var currentlyRunningCancelEvents = currentlyRunningEvents
            .Where(entry => Simulacrum.EventRegistry.RegisteredEvents[entry.Key].AdvanceIsCancel)
            .ToList();

        foreach (var (eventName, eventInstance) in currentlyRunningCancelEvents)
        {
            eventInstance.OnEnd();
            currentlyRunningEvents.Remove(eventName);
        }
    }

    private void EndMutexEvents()
    {
        var currentlyRunningMutexEvents = currentlyRunningEvents
            .Where(entry => Simulacrum.EventRegistry.RegisteredEvents[entry.Key].IsMutuallyExclusive)
            .ToList();

        foreach (var (eventName, eventInstance) in currentlyRunningMutexEvents)
        {
            eventInstance.OnEnd();
            currentlyRunningEvents.Remove(eventName);
        }
    }

    private void OnEventExecutedClient(EventExecutionMessage message)
    {
        Simulacrum.Log.LogInfo($"[Event] Execute an event on client: {message.EventName}");

        var executingEvent = initializedEvents[message.EventName];

        EndMutexEvents();

        // executingEvent.OnStart();
        currentlyRunningEvents[message.EventName] = executingEvent;
    }

    internal void EndEvents()
    {
        currentlyRunningEvents.Values.Do(activeEvent => activeEvent.OnEnd());
        currentlyRunningEvents.Clear();
    }

    [SimAttributes.HostOnly]
    private void Update()
    {
        // The wave number is -1 during preparation time, meaning we need to look at wave 0 during that time
        var currentWave = Mathf.Max(Simulacrum.State.CurrentWave, 0);

        if (Simulacrum.State.IsIterationRunning && eventsPerWave[currentWave] > 0 && eventTimer.Tick())
        {
            Simulacrum.Log.LogInfo($"EXECURINT EVENT, events this wave: {eventsPerWave[currentWave]}");

            eventsPerWave[currentWave]--;
            eventTimer.Set(Random.Range(30, 40));

            // TODO(giosuel): DEBUG
            // Skip the event execution if the chance roll doesn't hit
            // if (Random.Range(0f, 1f) > Mathf.Clamp(Simulacrum.GameConfig.Events.Chance.Value, 0, 1)) return;

            // Debug line to not repeat the current event
            // var randomEvent = Simulacrum.EventRegistry.PickRandomWeightedEvent();
            // if (currentlyRunningEvents.ContainsKey(randomEvent)) return;

            onExecuteEvent.SendClients(new EventExecutionMessage
            {
                EventName = Simulacrum.EventRegistry.PickRandomWeightedEvent()
            });
        }

        currentlyRunningEvents.Values.Do(activeEvent => activeEvent.OnUpdate());
    }
}