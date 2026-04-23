using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// caches transform matrix for batch rendering — self-registers in static list,
/// disables own Renderer (ConveyorRenderer draws all conveyors via DrawMeshInstanced)
/// </summary>
public class ConveyorBatchRenderingComponent : MonoBehaviour
{
	#region Inspector Fields
	[SerializeField] int _meshIndex;
	#endregion

	#region private API
	static bool needsUpdate = true;
	Matrix4x4 cachedMatrix;
	#endregion

	#region public API
	public static readonly List<ConveyorBatchRenderingComponent> AllConveyors = new List<ConveyorBatchRenderingComponent>();
	public static bool GetNeedsUpdate() => needsUpdate;
	public static void SetNeedsUpdate(bool val) => needsUpdate = val;
	public Matrix4x4 GetCachedMatrix() => cachedMatrix;
	public int GetMeshIndex() => _meshIndex;

	public void RefreshMatrix()
	{
		cachedMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
		needsUpdate = true;
	}
	#endregion

	#region Unity Life Cycle
	private void OnEnable()
	{
		cachedMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
		AllConveyors.Add(this);
		needsUpdate = true;
		GetComponent<Renderer>().enabled = false;
	}
	private void OnDisable()
	{
		AllConveyors.Remove(this);
		needsUpdate = true;
	}
	#endregion
}