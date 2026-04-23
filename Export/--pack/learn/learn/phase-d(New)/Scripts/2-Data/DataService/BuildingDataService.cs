using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// purely C# — validates grid placement + detects conveyor snap alignment.
/// Ghost instantiation, material swapping, layer setting stay in BuildingManager (Unity API).
/// Only pure geometry math lives here.
/// </summary>
public class BuildingDataService
{
	#region public API — Grid
	/// <summary> snaps world position to 1m integer grid </summary>
	public Vector3Int GetClosestGridPosition(Vector3 worldPosition)
	{
		worldPosition -= new Vector3(0.5f, 0.4f, 0.5f);
		return new Vector3Int(Mathf.RoundToInt(worldPosition.x), Mathf.RoundToInt(worldPosition.y), Mathf.RoundToInt(worldPosition.z));
	}
	#endregion

	#region public API — Snap
	/// <summary> tests 4 rotations × input/output snap points, returns entries where distance < 0.25 </summary>
	public List<BuildingRotationInfo> GetNearbySnapConnections(Vector3 ghostPos, BuildingObject building, BuildingObject neighbor, bool isMirrored)
	{
		var results = new List<BuildingRotationInfo>();
		// test neighbor outputs → building inputs (4 rotations)
		if (neighbor.ConveyorOutputSnapPositions.Count > 0)
		{
			for (int r = 0; r < 4; r++)
			{
				Quaternion rot = Quaternion.Euler(0f, r * 90f, 0f);
				Matrix4x4 m = Matrix4x4.TRS(ghostPos, rot, Vector3.one);
				foreach (var inp in building.ConveyorInputSnapPositions)
				{
					Vector3 worldInp = m.MultiplyPoint3x4(inp.localPosition);
					foreach (var outp in neighbor.ConveyorOutputSnapPositions)
						if (Vector3.Distance(worldInp, outp.position) < 0.25f)
							results.Add(new BuildingRotationInfo { Rotation = rot, IsMirroredMode = isMirrored });
				}
			}
		}
		// test neighbor inputs → building outputs (4 rotations)
		if (neighbor.ConveyorInputSnapPositions.Count > 0)
		{
			for (int r = 0; r < 4; r++)
			{
				Quaternion rot = Quaternion.Euler(0f, r * 90f, 0f);
				Matrix4x4 m = Matrix4x4.TRS(ghostPos, rot, Vector3.one);
				foreach (var outp in building.ConveyorOutputSnapPositions)
				{
					Vector3 worldOutp = m.MultiplyPoint3x4(outp.localPosition);
					foreach (var inp in neighbor.ConveyorInputSnapPositions)
						if (Vector3.Distance(worldOutp, inp.position) < 0.25f)
							results.Add(new BuildingRotationInfo { Rotation = rot, IsMirroredMode = isMirrored });
				}
			}
		}
		return results;
	}
	/// <summary> groups snap results by rotation, picks most-voted or first </summary>
	public BuildingRotationInfo ResolveBestSnap(List<BuildingRotationInfo> snaps)
	{
		if (snaps.Count <= 1) return snaps[0];
		var groups = snaps.GroupBy(s => s).OrderByDescending(g => g.Count()).ToList();
		return groups[0].Count() > 1 ? groups[0].Key : snaps[0];
	}
	#endregion
}