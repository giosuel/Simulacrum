#region

using UnityEngine;

#endregion

namespace Simulacrum.Utils;

internal class SimTimer
{
    private float initialTime;
    private float countdown;

    private bool isStopped;

    private SimTimer()
    {
    }

    internal static SimTimer ForInterval(float seconds)
    {
        var timer = new SimTimer
        {
            initialTime = seconds,
            countdown = seconds
        };
        return timer;
    }

    internal void Stop() => isStopped = true;
    internal void Start() => isStopped = false;

    internal void Set(float newTime)
    {
        initialTime = newTime;
        countdown = newTime;
    }

    internal bool Tick()
    {
        if (isStopped) return false;

        countdown -= Time.deltaTime;
        if (countdown <= 0)
        {
            countdown = initialTime;
            return true;
        }

        return false;
    }

    internal void Reset()
    {
        countdown = initialTime;
        isStopped = false;
    }
}