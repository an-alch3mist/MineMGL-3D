using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using SPACE_UTIL;

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