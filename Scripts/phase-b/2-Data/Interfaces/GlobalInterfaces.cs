using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using HighlightPlus;

using SPACE_UTIL;

/// <summary>
/// Any object that can be outlined when the player looks at it. ObjectHighlighterManager raycasts
/// each frame and calls GetHighlightProfile on the first IHighlightable it hits. The object
/// decides which profile to return (or null to skip highlighting).
///
/// Who implements me: BaseHeldTool (cyan), InteractableComputer (cyan), OrePiece (cyan, skip if magnet-held),
///   BuildingCrate (cyan), BuildingObject (cyan/green/red depending on active tool).
/// Who calls me: ObjectHighlighterManager.OutlineLookedAtThing().
/// </summary>
public interface IHighlightable
{
	/// <summary>
	/// Returns the HighlightProfile to use when the player looks at this object.
	/// Return null to skip highlighting (e.g. ore already held by magnet).
	/// </summary>
	/// <param name="activeTool">is the currently equipped tool (or null if empty-handed).</param>
	HighlightProfile GetHighlightProfile(BaseHeldTool activeTool);
}

/// <summary>
/// contract for item with an inventory icon.
/// </summary>
public interface IIconItem
{
	Sprite GetIcon();
}

/// <summary>
/// stub - contract expanded in phase-G with full save/load
/// </summary>
public interface ISaveLoadableObject
{
	bool hasBeenSaved { get; set; }
	bool ShouldBeSaved();
	SavableObjectID GetSavableObjectTypeId();
	Vector3 GetPos();
	Vector3 GetRot();
	void LoadFromSave(string customDataJson);
	string GetCustomDataJsonSnapshot();
}