using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// weighted random selection + ore helpers — centralized, DRY
/// </summary>
public static class UtilsPhaseC
{
	/// <summary> Picks one item from a list using weighted random selection. Higher weight = more
	/// likely to be chosen. Used everywhere ore drops are selected: OreNode, AutoMiner, OrePiece sieving. </summary>
	public static T WeightedRandom<T>(List<T> items, Func<T, float> getWeight)
	{
		if (items == null || items.Count == 0) return default;
		if (items.Count == 1) return items[0];
		float total = items.sum(getWeight);
		float roll = UnityEngine.Random.value * total;
		float cumulative = 0f;
		foreach (var item in items)
		{
			cumulative += getWeight(item);
			if (roll <= cumulative) return item;
		}
		return items[items.Count - 1];
	}

	/// <summary> Convenience method that reads a WeightedOreChance list (from SO_AutoMinerResourceDefinition),
	/// optionally filters out gems, then calls WeightedRandom to pick one OrePiece prefab. </summary>
	public static OrePiece PickOrePrefab(List<WeightedOreChance> list, bool canProduceGems)
	{
		if (list == null || list.Count == 0) return null;
		var filtered = canProduceGems ? list : list.FindAll(o => o.OrePrefab.PieceType != PieceType.Gem);
		return WeightedRandom(filtered, o => o.Weight)?.OrePrefab;
	}
}