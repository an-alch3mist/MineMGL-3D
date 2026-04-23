using System.Collections.Generic;
using UnityEngine;
using SPACE_UTIL;

/// <summary>
/// OreDataService plain C# test — zero scene dependency
/// Prerequisites: OreDataService.cs, GlobalEnumsC.cs
/// NOT required: Player, tools, nodes, pool — nothing
/// How to test:
///   Space → Build + log snapshot
///   U → GetColoredResourceTypeString(Iron)
///   I → GetDefaultSellValue test (needs OrePiece prefabs — limited without scene)
///   O → Log full snapshot
/// </summary>
public class DEBUG_CheckC : MonoBehaviour
{
	#region Inspector Fields
	[SerializeField] List<ResourceDescription> _testDescriptions;
	#endregion

	#region Unity Life Cycle
	OreDataService ds = new OreDataService();
	private void Update()
	{
		if (INPUT.K.InstantDown(KeyCode.Space))
		{
			ds.Build(_testDescriptions);
			Debug.Log(ds.GetSnapshot("after Build()"));
		}
		else if (INPUT.K.InstantDown(KeyCode.U))
		{
			Debug.Log(ds.GetColoredResourceTypeString(ResourceType.Iron).colorTag("cyan"));
			Debug.Log(ds.GetColoredFormattedResourcePieceString(ResourceType.Iron, PieceType.Ore));
			Debug.Log(ds.GetColoredFormattedResourcePieceString(ResourceType.Gold, PieceType.DrillBit));
			Debug.Log(ds.GetColoredFormattedResourcePieceString(ResourceType.Slag, PieceType.Pipe));
		}
		else if (INPUT.K.InstantDown(KeyCode.O))
		{
			LOG.AddLog(ds.GetSnapshot("full snapshot"), "json");
		}
	}
	#endregion
}