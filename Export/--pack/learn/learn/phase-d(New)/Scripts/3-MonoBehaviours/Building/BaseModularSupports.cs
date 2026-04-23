using UnityEngine;

/// <summary>
/// base class for all scaffolding leg systems — Start finds parent BuildingObject + spawns
/// </summary>
public class BaseModularSupports : MonoBehaviour
{
	#region private API
	protected BuildingObject _buildingObject;
	#endregion

	#region public API
	public virtual void SpawnSupports() { }
	public virtual void RespawnSupports(bool RespawnNextFrame = false) { }
	#endregion

	#region Unity Life Cycle
	protected virtual void Start()
	{
		_buildingObject = GetComponentInParent<BuildingObject>();
		SpawnSupports();
	}
	#endregion
}