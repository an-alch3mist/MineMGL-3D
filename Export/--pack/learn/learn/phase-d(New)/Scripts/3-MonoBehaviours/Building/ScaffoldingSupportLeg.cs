using System.Collections;
using UnityEngine;

/// <summary>
/// simple repeating support legs — raycasts down, spawns N leg prefabs at spacing intervals
/// </summary>
public class ScaffoldingSupportLeg : BaseModularSupports
{
	#region Inspector Fields
	[SerializeField] GameObject _supportPrefab;
	[SerializeField] float _supportSpacing = 1f;
	[SerializeField] int _maxSupports = 15;
	#endregion

	#region public API
	public override void SpawnSupports()
	{
		foreach (Transform child in transform) Destroy(child.gameObject);
		if (_buildingObject != null && !_buildingObject.BuildingSupportsEnabled) return;
		if (!Physics.Raycast(transform.position, Vector3.down, out var hit, 20f,
			Singleton<BuildingManager>.Ins.GetScaffoldingSupportsCollisionLayers())) return;
		int count = Mathf.RoundToInt(hit.distance / _supportSpacing) + 1;
		Vector3 pos = transform.position;
		for (int i = 0; i < count; i++)
		{
			Instantiate(_supportPrefab, pos, transform.rotation, transform);
			pos.y -= _supportSpacing;
		}
	}
	public override void RespawnSupports(bool RespawnNextFrame = false)
	{
		if (RespawnNextFrame) StartCoroutine(DelayedRespawn());
		else SpawnSupports();
	}
	IEnumerator DelayedRespawn()
	{
		yield return new WaitForFixedUpdate();
		if (this != null) SpawnSupports();
	}
	#endregion
}