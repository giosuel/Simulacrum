using System;
using LethalNetworkAPI;
using Simulacrum.Network;
using Simulacrum.Utils;
using Unity.Netcode;

namespace Simulacrum;

[SimAttributes.HostOnly]
internal class GameState
{
    /*
     * Game start RPCs (Server -> Client)
     */
    private readonly LNetworkMessage<IterationStartMessage> onIterationStartMessage;
    private readonly LNetworkMessage<IterationEndMessage> onIterationEndMessage;
    private readonly LNetworkMessage<WaveStartMessage> onWaveStartMessage;

    /*
     * Client game state events
     */
    internal event Action<IterationStartMessage> onIterationStartClients;
    internal event Action<IterationEndMessage> onIterationEndClients;
    internal event Action<WaveStartMessage> onWaveStartClients;

    /*
     * Server game state events
     */
    [SimAttributes.HostOnly] internal event Action<int> onIterationStartServer;
    [SimAttributes.HostOnly] internal event Action onIterationEndServer;
    [SimAttributes.HostOnly] internal event Action onWaveStartServer;

    internal int CurrentIteration { get; private set; } = -1;
    internal int CurrentWave { get; private set; } = -1;
    internal bool IsIterationRunning { get; private set; }

    internal GameState()
    {
        onIterationStartMessage = LNetworkMessage<IterationStartMessage>.Connect("OnIterationStart");
        onIterationEndMessage = LNetworkMessage<IterationEndMessage>.Connect("OnIterationEnd");
        onWaveStartMessage = LNetworkMessage<WaveStartMessage>.Connect("OnWaveStart");

        onIterationStartMessage.OnClientReceived += OnIterationStartClient;
        onIterationEndMessage.OnClientReceived += OnIterationEndClient;
        onWaveStartMessage.OnClientReceived += OnWaveStartClients;
    }

    [SimAttributes.HostOnly]
    internal void StartIteration(int waveCount)
    {
        Simulacrum.Log.LogInfo($"START NEXT ITERATION: {CurrentIteration} -> {CurrentIteration+1}");

        CurrentIteration++;

        onIterationStartServer?.Invoke(waveCount);
        onIterationStartMessage.SendClients(new IterationStartMessage
        {
            IterationNumber = CurrentIteration,
            WaveCount = waveCount
        });

        CurrentWave = -1;
        IsIterationRunning = true;
    }

    [SimAttributes.HostOnly]
    internal void EndIteration()
    {
        IsIterationRunning = false;

        onIterationEndServer?.Invoke();
        onIterationEndMessage.SendClients(new IterationEndMessage
        {
            IterationNumber = CurrentIteration
        });
    }

    [SimAttributes.HostOnly]
    internal void StartNextWave()
    {
        Simulacrum.Log.LogInfo($"START NEXT WAVE: {CurrentWave} -> {CurrentWave+1}");
        CurrentWave++;

        onWaveStartServer?.Invoke();
        onWaveStartMessage.SendClients(new WaveStartMessage
        {
            WaveNumber = CurrentWave
        });
    }

    private void OnIterationStartClient(IterationStartMessage message)
    {
        CurrentIteration = message.IterationNumber;
        CurrentWave = -1;

        onIterationStartClients?.Invoke(message);

        IsIterationRunning = true;
    }

    private void OnIterationEndClient(IterationEndMessage message)
    {
        onIterationEndClients?.Invoke(message);

        IsIterationRunning = false;
    }

    private void OnWaveStartClients(WaveStartMessage message)
    {
        CurrentWave = message.WaveNumber;

        onWaveStartClients?.Invoke(message);
    }
}