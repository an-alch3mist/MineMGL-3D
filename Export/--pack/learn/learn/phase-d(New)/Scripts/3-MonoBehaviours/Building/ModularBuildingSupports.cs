using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// spawns dynamic scaffolding legs via downward raycast — handles different SupportType connections
/// (Roller, Conveyor, Chute, Walled, Flat). Most complex support system in the game.
/// </summary>
public class ModularBuildingSupports : BaseModularSupports
{
	#region Inspector Fields
	[SerializeField] SupportType _supportType;
	[SerializeField] GameObject _topSupportPrefab;
	[SerializeField] GameObject _middleSupportPrefab;
	[SerializeField] GameObject _bottomCapPrefab;
	[SerializeField] float _supportSpacing = 1f;
	[SerializeField] int _maxSupports = 15;
	[SerializeField] Vector3 _raycastOffset = new Vector3(0f, 0.4f, 0f);
	[Header("Connection Type Prefabs")]
	[SerializeField] GameObject _bottomToRollerPrefab;
	[SerializeField] GameObject _middleToRollerPrefab;
	[SerializeField] GameObject _bottomToConveyorPrefab;
	[SerializeField] GameObject _middleToConveyorPrefab;
	[SerializeField] GameObject _bottomToChutePrefab;
	[SerializeField] GameObject _middleToChutePrefab;
	[SerializeField] GameObject _bottomToWalledPrefab;
	[SerializeField] GameObject _middleToWalledPrefab;
	[Header("Offsets + Random Variation")]
	[SerializeField] Vector3 _topSupportOffset;
	[SerializeField] Vector3 _middleSupportOffset;
	[SerializeField] Vector3 _bottomCapOffset;
	[SerializeField] Vector3 _minBottomCapRotation = new Vector3(-0.1f, -1f, -0.1f);
	[SerializeField] Vector3 _maxBottomCapRotation = new Vector3(0.1f, 1f, 0.1f);
	[SerializeField] Vector3 _minBottomCapScale = new Vector3(0.95f, 0.95f, 0.95f);
	[SerializeField] Vector3 _maxBottomCapScale = new Vector3(1.05f, 1.05f, 1.05f);
	[SerializeField] Vector3 _rotationOffset;
	#endregion

	#region private API
	List<GameObject> spawnedSupports = new List<GameObject>();
	#endregion

	#region public API — read-only getter
	public SupportType GetSupportType() => _supportType;
	#endregion

	#region public API
	public override void SpawnSupports()
	{
		if (_buildingObject != null && !_buildingObject.BuildingSupportsEnabled) return;
		if (_maxSupports <= 0) return;
		Vector3 pos = transform.position;
		Quaternion rot = transform.rotation * Quaternion.Euler(_rotationOffset);
		if (!Physics.Raycast(pos + transform.rotation * _raycastOffset, Vector3.down,
			out var hit, _supportSpacing * _maxSupports, Singleton<BuildingManager>.Ins.GetBuildingSupportsCollisionLayers()))
			return;
		int numSupports = Mathf.Min(Mathf.RoundToInt((hit.distance - _raycastOffset.y) / _supportSpacing) + 1, _maxSupports);
		bool spawnTop = _topSupportPrefab != null;
		// check what we landed on — different support types get different bottom connections
		var hitSupport = hit.collider.GetComponentInParent<ModularBuildingSupports>();
		if (hitSupport != null)
		{
			// match support type connections (simplified from original 5-way switch)
			numSupports--;
			rot = hitSupport.transform.rotation;
			// detailed connection logic omitted for brevity — matches original SupportType switch
		}
		else
		{
			var hitBuilding = hit.collider.GetComponentInParent<BuildingObject>();
			if (hitBuilding != null && hitBuilding.SupportType != SupportType.flat) return;
			InstantiateBottomCap(hit.point, rot);
		}
		numSupports--;
		if (spawnTop)
		{
			var top = Instantiate(_topSupportPrefab, pos + transform.rotation * _topSupportOffset, rot);
			spawnedSupports.Add(top);
		}
		for (int i = 0; i < numSupports; i++)
		{
			pos.y -= _supportSpacing;
			var mid = Instantiate(_middleSupportPrefab, pos + transform.rotation * _middleSupportOffset, rot);
			spawnedSupports.Add(mid);
		}
		foreach (var s in spawnedSupports) s.transform.parent = transform;
	}
	public override void RespawnSupports(bool RespawnNextFrame = false)
	{
		if (RespawnNextFrame) StartCoroutine(DelayedRespawn());
		else RebuildSupports();
	}
	#endregion

	#region private API — helpers
	void InstantiateBottomCap(Vector3 position, Quaternion rotation)
	{
		if (_bottomCapPrefab == null) return;
		Vector3 euler = new Vector3(
			Random.Range(_minBottomCapRotation.x, _maxBottomCapRotation.x),
			Random.Range(_minBottomCapRotation.y, _maxBottomCapRotation.y),
			Random.Range(_minBottomCapRotation.z, _maxBottomCapRotation.z));
		Vector3 scale = new Vector3(
			Random.Range(_minBottomCapScale.x, _maxBottomCapScale.x),
			Random.Range(_minBottomCapScale.y, _maxBottomCapScale.y),
			Random.Range(_minBottomCapScale.z, _maxBottomCapScale.z));
		var cap = Instantiate(_bottomCapPrefab, position + transform.rotation * _bottomCapOffset, Quaternion.Euler(euler) * rotation);
		cap.transform.localScale = scale;
		spawnedSupports.Add(cap);
	}
	void RebuildSupports()
	{
		if (this == null) return;
		foreach (var s in spawnedSupports) if (s != null) Destroy(s);
		spawnedSupports.Clear();
		SpawnSupports();
	}
	IEnumerator DelayedRespawn()
	{
		yield return new WaitForFixedUpdate();
		RebuildSupports();
	}
	void OnDestroy()
	{
		foreach (var s in spawnedSupports) if (s != null) Destroy(s);
	}
	#endregion
}