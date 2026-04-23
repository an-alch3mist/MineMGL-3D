using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure data config for one AutoMiner placement — spawn probability (default 80%), spawn rate
/// (seconds between spawns), and a weighted list of possible OrePiece prefabs. AutoMiner reads
/// my public fields on Start to configure itself. UtilsPhaseC.PickOrePrefab reads PossibleOrePrefabs
/// to do the weighted random selection. I have zero methods — all logic lives in the consumer.
/// Created in Unity: Create → SO → SO_AutoMinerResourceDefinition.
/// </summary>
[CreateAssetMenu(menuName = "SO/SO_AutoMinerResourceDefinition", fileName = "SO_AutoMinerResourceDefinition")]
public class SO_AutoMinerResourceDefinition : ScriptableObject
{
	[Range(0f, 100f)] public float SpawnProbability = 80f;
	[Range(0f, 20f)] public float SpawnRate = 2f;
	public List<WeightedOreChance> PossibleOrePrefabs = new List<WeightedOreChance>();
}