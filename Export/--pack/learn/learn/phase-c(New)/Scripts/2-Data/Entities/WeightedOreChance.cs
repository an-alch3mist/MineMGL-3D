using System;

/// <summary>
/// Same as WeightedNodeDrop but used by AutoMiner (SO_AutoMinerResourceDefinition.PossibleOrePrefabs)
/// and Phase E machines (OrePiece sieving, cluster breaking). Pairs an OrePiece prefab with a Weight.
/// UtilsPhaseC.PickOrePrefab reads these lists and does the weighted random selection.
/// </summary>
[Serializable]
public class WeightedOreChance
{
	public OrePiece OrePrefab;
	public float Weight = 100f;
}