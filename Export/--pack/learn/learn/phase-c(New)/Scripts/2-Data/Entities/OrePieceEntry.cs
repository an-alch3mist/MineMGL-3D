using System;
using UnityEngine;

/// <summary>
/// Save data for one ore piece in the world. Phase G serializes every active OrePiece into
/// one of these entries (ResourceType, PieceType, PolishedPercent, Position, Rotation, Scale,
/// MeshID) and stores them in the save file. On load, OrePiecePoolManager recreates pieces
/// from these entries. Not used until Phase G — exists now so the data shape is defined early.
/// </summary>
[Serializable]
public class OrePieceEntry
{
	public ResourceType ResourceType;
	public PieceType PieceType;
	public float PolishedPercent;
	public Vector3 Position;
	public Vector3 Rotation;
	public Vector3 Scale;
	public int MeshID;
}