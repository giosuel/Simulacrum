using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Simulacrum.Utils;

internal static class SimUtils
{
    internal static Func<float, float> ReciprocalDown(float start, float steepness)
    {
        return t => start / (1 + steepness * t);
    }

    internal static Func<float, float> ReciprocalUp(float start, float end, float steepness)
    {
        return t => start + end - end / (1 + steepness * t);
    }

    internal static Func<float, float> Quadratic(float a, float b, float c)
    {
        return t => a * Mathf.Pow(t, 2) + b * t + c;
    }

    internal static float RandomDeviation(float value, float deviation) => value + Random.Range(-deviation, deviation);

    internal static T PickRandomWeightedItem<T>(List<T> items, List<T> history)
    {
        var historyCount = history.Count;

        var weights = items
            .Select(item => history.LastIndexOf(item))
            .Select(recentPickIndex => recentPickIndex == -1
                ? 1.0
                : Math.Max(0.1, 1.0 - (historyCount - recentPickIndex) * 0.1)
            )
            .ToList();

        var totalWeight = weights.Sum();
        var chances = weights.Select(w => w / totalWeight).ToList();
        var randomValue = Random.Range(0f, 1f);
        var cumulativeChance = 0.0;

        for (var i = 0; i < items.Count; i++)
        {
            cumulativeChance += chances[i];
            if (randomValue < cumulativeChance)
            {
                return items[i];
            }
        }

        return items.Last();
    }
}