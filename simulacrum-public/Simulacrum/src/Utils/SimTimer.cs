#region

using UnityEngine;

#endregion

namespace Simulacrum.Utils;

internal class SimTimer
{
    private float initialTime;
    private float countdown;

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

    internal void Set(float newTime)
    {
        initialTime = newTime;
        countdown = newTime;
    }

    internal bool Tick()
    {
        countdown -= Time.deltaTime;
        if (countdown <= 0)
        {
            countdown = initialTime;
            return true;
        }

        return false;
    }

    internal void Reset() => countdown = initialTime;
}