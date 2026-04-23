using System;

/// <summary>
/// Composite key for OrePiecePoolManager's dictionary of queues. Combines ResourceType +
/// PieceType + IsPolished into one hashable struct. When spawning or returning ore, the pool
/// creates an OrePieceKey to find the right queue. Two ore pieces with the same key are
/// interchangeable in the pool (same prefab, same visual, just different position/velocity).
/// </summary>
[Serializable]
public struct OrePieceKey : IEquatable<OrePieceKey>
{
	public ResourceType ResourceType;
	public PieceType PieceType;
	public bool IsPolished;

	public OrePieceKey(ResourceType resourceType, PieceType pieceType, bool isPolished)
	{
		ResourceType = resourceType;
		PieceType = pieceType;
		IsPolished = isPolished;
	}
	public override int GetHashCode()
	{
		return ((17 * 31 + ResourceType.GetHashCode()) * 31 + PieceType.GetHashCode()) * 31 + IsPolished.GetHashCode();
	}
	public override bool Equals(object obj) => obj is OrePieceKey other && Equals(other);
	public bool Equals(OrePieceKey other)
	{
		return ResourceType == other.ResourceType && PieceType == other.PieceType && IsPolished == other.IsPolished;
	}
}