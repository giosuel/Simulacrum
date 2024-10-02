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

    internal static Func<float, float> Reciprocal(float start, float steepness)
    {
        return t => start / (steepness * t);
    }

    internal static Func<float, float> Quadratic(float a, float b, float c)
    {
        return t => a * Mathf.Pow(t, 2) + b * t + c;
    }

    internal static Func<float, float> Cubic(float a, float b, float c, float d)
    {
        return t => a * Mathf.Pow(t, 3) + b * Mathf.Pow(t, 2) + c * t + d;
    }

    internal static Func<float, float> Exponential(float e)
    {
        return t => Mathf.Pow(e, t);
    }

    internal static Func<float, float, float> BoxMullerRandomNormal(float r1, float r2)
    {
        return (mean, stdDev) =>
            mean + stdDev * (float)(Math.Sqrt(-2 * Math.Log(1 - r1)) * Math.Sin(2 * Math.PI * (1 - r2)));
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

    internal static int[] DistributeItemsOverWaves(int itemCount, int waveCount)
    {
        var eventsPerWave = new int[waveCount];
        var remainingEvents = itemCount;

        var totalWeight = (float)Enumerable.Range(1, waveCount).Sum();

        for (var wave = 0; wave < waveCount; wave++)
        {
            if (wave == waveCount - 1)
            {
                eventsPerWave[wave] = remainingEvents;
            }
            else
            {
                var maxPossibleEvents = remainingEvents - (waveCount - wave - 1);
                var eventChance = (wave + 1) / totalWeight * maxPossibleEvents;

                var eventsThisWave = Math.Min(remainingEvents, Random.Range(0, Mathf.CeilToInt(eventChance)));
                eventsPerWave[wave] = eventsThisWave;

                remainingEvents -= eventsThisWave;
            }
        }

        return eventsPerWave;
    }

    /// <summary>
    /// Generates a normal value distributed by the provided distribution method.
    /// </summary>
    /// <param name="min">The value's lower bound</param>
    /// <param name="max">The value's upper bound</param>
    /// <param name="distribution">The distribution method that should be used</param>
    /// <param name="randomGenerator">The random number generator that is used for the input</param>
    /// <param name="normalMean">The mean, if normal distribution is used</param>
    /// <param name="normalStdDev">The standard deviation, if normal distribution is used</param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException">The provided distribution is invalid</exception>
    internal static float GenerateRandomDistributedValue(
        float min, float max,
        Distribution distribution,
        float normalMean, float normalStdDev,
        Func<float> randomGenerator
    )
    {
        var distributedNumber = Mathf.Clamp(distribution switch
        {
            Distribution.Linear => randomGenerator(),
            Distribution.Quadratic => Quadratic(1, 0, 0)(randomGenerator()),
            Distribution.Cubic => Cubic(1, 0, 0, 0)(randomGenerator()),
            Distribution.Exponential => Exponential(100)(randomGenerator() - 1),
            Distribution.Reciprocal => Reciprocal(1, 10)(randomGenerator()),
            Distribution.Normal => BoxMullerRandomNormal(randomGenerator(), randomGenerator())(normalMean, normalStdDev),
            _ => throw new ArgumentOutOfRangeException()
        }, 0, 1);

        return Mathf.Lerp(min, max, distributedNumber);
    }

    internal static float UniformRandomNumber() => Random.Range(0f, 1f);
}