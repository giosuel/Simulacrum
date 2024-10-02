using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace Simulacrum.Utils;

public class WeightedListHolder<T>
{
    private List<(T, float)> calculatedChances = [];

    private int totalWeight;
    private int lastCalculationCount;

    internal T PickRandomWeightedEvent(List<(T, int)> collectionWithWeights)
    {
        if (lastCalculationCount < collectionWithWeights.Count) CalculateEventChances(collectionWithWeights);

        var randomValue = Random.Range(0, totalWeight);

        var cumulativeWeight = 0f;
        foreach (var (item, itemWeight) in calculatedChances)
        {
            cumulativeWeight += itemWeight;
            if (randomValue < cumulativeWeight)
            {
                return item;
            }
        }

        return calculatedChances.Last().Item1;
    }

    private void CalculateEventChances(List<(T, int)> collectionWithWeights)
    {
        if (lastCalculationCount < collectionWithWeights.Count)
        {
            lastCalculationCount = collectionWithWeights.Count;

            totalWeight = collectionWithWeights.Sum(entry => entry.Item2);
            calculatedChances = collectionWithWeights.Select(
                entry => (entry.Item1, entry.Item2 / (float)totalWeight)
            ).ToList();
        }
    }
}