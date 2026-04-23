using System;
using UnityEngine;

/// <summary>
/// Pairs a ResourceType with its display color for UI text. OreManager holds a list of these
/// in the inspector — OreDataService.Build() stores them, then GetResourceColor(type) looks up
/// the matching entry. Used by GetColoredResourceTypeString to wrap ore names in rich text color tags.
/// </summary>
[Serializable]
public class ResourceDescription
{
	public ResourceType ResourceType;
	public Color DisplayColor;
}