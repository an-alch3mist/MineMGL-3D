using System;
using UnityEngine;

/// <summary>
/// save data for one placed building — Phase G uses this for persistence
/// </summary>
[Serializable]
public class BuildingObjectEntry
{
	public SavableObjectID SavableObjectID;
	public Vector3 Position;
	public Vector3 Rotation;
	public string CustomDataJson;
	public bool BuildingSupportsEnable = true;
}