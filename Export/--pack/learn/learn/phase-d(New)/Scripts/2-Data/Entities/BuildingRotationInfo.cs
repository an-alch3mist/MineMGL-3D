using System;
using UnityEngine;

/// <summary>
/// snap rotation + mirror flag — used during conveyor snap voting to pick best alignment
/// </summary>
public struct BuildingRotationInfo : IEquatable<BuildingRotationInfo>
{
	public Quaternion Rotation;
	public bool IsMirroredMode;

	public bool Equals(BuildingRotationInfo other) => Rotation.Equals(other.Rotation) && IsMirroredMode == other.IsMirroredMode;
	public override bool Equals(object obj) => obj is BuildingRotationInfo other && Equals(other);
	public override int GetHashCode() => (17 * 23 + Rotation.GetHashCode()) * 23 + IsMirroredMode.GetHashCode();
	public static bool operator ==(BuildingRotationInfo l, BuildingRotationInfo r) => l.Equals(r);
	public static bool operator !=(BuildingRotationInfo l, BuildingRotationInfo r) => !l.Equals(r);
}