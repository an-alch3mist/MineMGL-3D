using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using HighlightPlus;

using SPACE_UTIL;

/// <summary>
/// stub — expanded in Phase G with full save/load
/// </summary>
public interface ISaveLoadableObject
{
	bool HasBeenSaved { get; set; }
	bool ShouldBeSaved();
	SavableObjectID GetSavableObjectID();
	Vector3 GetPosition();
	Vector3 GetRotation();
	void LoadFromSave(string customDataJson);
	string GetCustomSaveData();
}