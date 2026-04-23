using System;

/// <summary>
/// One entry in OreNode's _possibleDrops inspector list. Pairs an OrePiece prefab with a Weight
/// value. When the node breaks, UtilsPhaseC.WeightedRandom picks one of these — higher weight
/// = more likely to drop. For example: Iron Ore weight 80 + Gold Ore weight 20 = 80% Iron, 20% Gold.
/// </summary>
[Serializable]
public class WeightedNodeDrop
{
	public OrePiece OrePrefab;
	public float Weight = 100f;
}