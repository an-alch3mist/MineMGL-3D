using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using SPACE_UTIL;

/// <summary>
/// I'm pure C# — testable via `new`, zero Unity dependency. OreManager creates me on Awake
/// and calls Build() with the resource descriptions list from the inspector. I store them in
/// RESOURCE_DESC and provide lookups: GetResourceDescription (by type), GetResourceColor,
/// GetColoredResourceTypeString (wraps name in rich text color tag), and
/// GetColoredFormattedResourcePieceString (full formatted name with piece type label mapping
/// like "Drill Bit", "Threaded Rod", etc.). ToolResourceScanner and future UI displays use
/// these for colored ore name text.
///
/// Who uses me: OreManager (Build + expose via DataService property), OreTest (snapshot).
/// Events I fire: none. Pure data.
/// </summary>
public class OreDataService
{
	#region private API
	List<ResourceDescription> RESOURCE_DESC = new List<ResourceDescription>();
	#endregion

	#region public API
	/// <summary> Called by OreManager on Awake with the list from the inspector. Stores all
	/// resource descriptions so I can look them up by ResourceType later. </summary>
	public void Build(List<ResourceDescription> descriptions)
	{
		RESOURCE_DESC = descriptions;
	}
	/// <summary> Finds the ResourceDescription for the given type — returns the entry with
	/// matching ResourceType, or null if not found. Used internally by GetResourceColor. </summary>
	public ResourceDescription GetResourceDescription(ResourceType type)
	{
		return RESOURCE_DESC.find(r => r.ResourceType == type);
	}
	/// <summary> Returns the display color for a resource type (e.g. Iron = grey, Gold = yellow).
	/// Falls back to white if the type isn’t in the list. Used by all colored text formatting. </summary>
	public Color GetResourceColor(ResourceType type)
	{
		var desc = GetResourceDescription(type);
		return desc != null ? desc.DisplayColor : Color.white;
	}
	/// <summary> Wraps the resource type name in a rich text color tag — e.g. Iron becomes
	/// "<color=#999999>Iron</color>". Used by ToolResourceScanner and future UI displays. </summary>
	public string GetColoredResourceTypeString(ResourceType type)
	{
		string hex = ColorUtility.ToHtmlStringRGB(GetResourceColor(type));
		return $"<color=#{hex}>{type}</color>";
	}
	/// <summary> Full formatted ore name with piece type label — handles special labels like
	/// "Drill Bit" for DrillBit, "Threaded Rod" for ThreadedRod, "Junk" for Slag Pipe. Optionally
	/// prefixes "Polished" if requirePolished is true. Returns colored rich text. </summary>
	public string GetColoredFormattedResourcePieceString(ResourceType resourceType, PieceType pieceType, bool requirePolished = false)
	{
		string hex = ColorUtility.ToHtmlStringRGB(GetResourceColor(resourceType));
		string pieceName = pieceType.ToString();
		string resName = resourceType.ToString();
		// label overrides
		if (pieceType == PieceType.DrillBit) pieceName = "Drill Bit";
		if (pieceType == PieceType.ThreadedRod) pieceName = "Threaded Rod";
		if (pieceType == PieceType.OreCluster) pieceName = "Ore Cluster";
		if (pieceType == PieceType.JunkCast) pieceName = "Junk Cast";
		if (pieceType == PieceType.Pipe && resourceType == ResourceType.Slag) resName = "Junk";
		string polished = requirePolished ? "Polished " : "";
		if (pieceType == PieceType.Crushed)
			return $"<color=#{hex}>{polished}{pieceName} {resName}</color>";
		return $"<color=#{hex}>{polished}{resName} {pieceName}</color>";
	}
	#endregion

	#region snapShot
	/// <summary> Formats all stored resource descriptions into a JSON snapshot string for
	/// test logging — DEBUG_CheckC calls this to verify the data was built correctly. </summary>
	public string GetSnapshot(string header = "ore data snapshot")
	{
		return $@"
{'='.repeat(4) + header + '='.repeat(4)}
// RESOURCE_DESC
{PhaseCLOG.LIST_RESOURCE_DESC__TO__JSON(RESOURCE_DESC)}";
	}
	#endregion
}