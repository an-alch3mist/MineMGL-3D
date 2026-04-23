using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// I manage the entire building placement flow as a Singleton. When the player equips ToolBuilder,
/// I create a ghost preview (transparent clone of the building prefab), position it at grid-snapped
/// coordinates every frame, validate placement (overlap check via Physics.OverlapBox, flat ground
/// raycast, placement node requirement matching, conveyor snap voting), and swap the ghost's
/// materials between green (valid), red (invalid), or yellow (requirements not met).
///
/// Architecture split: I handle Unity API calls (Instantiate ghost, material swap, layer set,
/// Physics.OverlapBox). Pure geometry math (grid snap, snap connection testing, best-snap voting)
/// lives in BuildingDataService — testable without a scene.
///
/// Ghost lifecycle: SetupGhostObject Instantiates the building prefab as a ghost — sets IsGhost=true,
/// destroys trigger colliders, disables all MonoBehaviours except BuildingObject, disables audio +
/// particles, sets layer to "BuildingGhost", makes all Rigidbodies kinematic. CleanUpGhostObject
/// destroys the ghost when exiting build mode.
///
/// Who uses me: ToolBuilder (Update calls UpdateGhostObject, PrimaryFire calls CanPlaceObject).
/// Events I fire: none (ToolBuilder fires RaiseBuildingPlaced).
/// </summary>
public class BuildingManager : Singleton<BuildingManager>
{
	#region Inspector Fields
	[Header("Layer Masks")]
	[SerializeField] LayerMask _buildingPlacementCollisionLayers;
	[SerializeField] LayerMask _scaffoldingPlacementCollisionLayers;
	[SerializeField] LayerMask _buildingSupportsCollisionLayers;
	[SerializeField] LayerMask _scaffoldingSupportsCollisionLayers;
	[SerializeField] LayerMask _collisionLayersExcludeGround;
	[SerializeField] LayerMask _buildingObjectLayer;
	[Header("Ghost Materials")]
	[SerializeField] Material _ghostMaterial;
	[SerializeField] Material _invalidGhostMaterial;
	[SerializeField] Material _requirementGhostMaterial;
	[SerializeField] List<Material> _materialsToNotReplaceOnBuildingGhost;
	[SerializeField] Material _buildingNodeGhost;
	[SerializeField] Material _buildingNodeGhost_WrongType;
	[Header("Indicator Materials")]
	[SerializeField] Material _greenLightMaterial;
	[SerializeField] Material _redLightMaterial;
	[SerializeField] Material _orangeLightMaterial;
	[Header("Prefabs")]
	[SerializeField] BuildingCrate _buildingCratePrefab;
	[SerializeField] ToolBuilder _buildingToolPrefab;
	#endregion

	#region public API — read-only getters (only what external scripts need)
	public LayerMask GetBuildingPlacementCollisionLayers() => _buildingPlacementCollisionLayers;
	public LayerMask GetBuildingSupportsCollisionLayers() => _buildingSupportsCollisionLayers;
	public LayerMask GetScaffoldingSupportsCollisionLayers() => _scaffoldingSupportsCollisionLayers;
	public Material GetBuildingNodeGhost() => _buildingNodeGhost;
	public Material GetBuildingNodeGhost_WrongType() => _buildingNodeGhost_WrongType;
	public Material GetGreenLightMaterial() => _greenLightMaterial;
	public Material GetRedLightMaterial() => _redLightMaterial;
	public Material GetOrangeLightMaterial() => _orangeLightMaterial;
	public BuildingCrate GetBuildingCratePrefab() => _buildingCratePrefab;
	public ToolBuilder GetBuildingToolPrefab() => _buildingToolPrefab;
	#endregion

	#region private API
	BuildingObject ghostObject;
	BuildingDataService dataService = new BuildingDataService();
	Vector3 previousGhostPos;
	bool isEligibleForSnapping;
	#endregion

	#region public API
	bool currentObjectIsSnapped;
	public bool GetCurrentObjectIsSnapped() => currentObjectIsSnapped;
	public void SetCurrentObjectIsSnapped(bool val) => currentObjectIsSnapped = val;
	public Transform GhostObjectTransform => ghostObject?.transform;
	public BuildingObject GetGhostObject() => ghostObject;
	public bool IsInBuildingMode() => ghostObject != null;
	public BuildingDataService DataService => dataService;

	/// <summary> validates placement — overlap check, flat ground, node requirement, snap </summary>
	public CanPlaceBuilding CanPlaceObject(Vector3Int position, BuildingObject prefab, Quaternion rotation,
		bool requiresFlat, bool canPlaceInTerrain, PlacementNodeRequirement nodeReq, ToolBuilder activeTool)
	{
		if (ghostObject == null) return CanPlaceBuilding.invalid;
		// overlap check using ghost colliders
		LayerMask mask = nodeReq == PlacementNodeRequirement.none
			? (canPlaceInTerrain ? _scaffoldingPlacementCollisionLayers : _buildingPlacementCollisionLayers)
			: _collisionLayersExcludeGround;
		var colliders = new List<Collider>();
		if (ghostObject.BuildingPlacementColliderObject != null)
			colliders.AddRange(ghostObject.BuildingPlacementColliderObject.GetComponentsInChildren<Collider>());
		if (colliders.Count == 0 && ghostObject.PhysicalColliderObject != null)
			colliders.AddRange(ghostObject.PhysicalColliderObject.GetComponentsInChildren<Collider>());
		foreach (var col in colliders)
			if (col != null && UtilsPhaseD.IsOverlapping(col, mask))
				return CanPlaceBuilding.invalid;
		// node requirement check
		if (nodeReq != PlacementNodeRequirement.none)
		{
			BuildingPlacementNode bestNode = null;
			float bestDist = float.MaxValue;
			foreach (var node in BuildingPlacementNode.All)
			{
				if (node.RequirementType != nodeReq || node.AttachedBuildingObject != null) continue;
				float d = Vector3.Distance(position, node.transform.position);
				if (d < 4f && d < bestDist) { bestDist = d; bestNode = node; }
			}
			if (bestNode != null)
			{
				ghostObject.transform.position = bestNode.transform.position;
				ghostObject.transform.rotation = bestNode.transform.rotation;
				return CanPlaceBuilding.valid;
			}
			return CanPlaceBuilding.requirementsNotMet;
		}
		// flat ground check
		if (requiresFlat && !UtilsPhaseD.IsFlatGround(position, _buildingPlacementCollisionLayers, out _))
			return CanPlaceBuilding.requirementsNotMet;
		// conveyor snap
		if (isEligibleForSnapping && ghostObject.ConveyorInputSnapPositions.Count > 0)
		{
			var snaps = new List<BuildingRotationInfo>();
			var nearby = Physics.OverlapSphere(ghostObject.transform.position, 1.25f, _buildingObjectLayer);
			foreach (var col in nearby)
			{
				var neighbor = col.GetComponentInParent<BuildingObject>();
				if (neighbor != null && neighbor != ghostObject)
					snaps.AddRange(dataService.GetNearbySnapConnections(
						ghostObject.transform.position, ghostObject, neighbor,
						activeTool != null && activeTool.IsUsingMirroredVersion()));
			}
			isEligibleForSnapping = false;
			if (snaps.Count > 0)
			{
				var best = dataService.ResolveBestSnap(snaps);
				if (activeTool != null) activeTool.SetCurrentRotation(best.Rotation);
				currentObjectIsSnapped = true;
			}
		}
		return CanPlaceBuilding.valid;
	}
	/// <summary> update ghost position, setup if needed, swap materials based on validity </summary>
	public void UpdateGhostObject(Vector3Int gridPos, BuildingObject prefab, Quaternion rotation, ToolBuilder activeTool)
	{
		SetupGhostObject(gridPos, prefab, rotation);
		if (previousGhostPos != ghostObject.transform.position)
		{
			isEligibleForSnapping = true;
			currentObjectIsSnapped = false;
			previousGhostPos = ghostObject.transform.position;
		}
		Material mat = _ghostMaterial;
		var result = CanPlaceObject(gridPos, prefab, rotation, prefab.RequiresFlatGround,
			activeTool.GetDefinition().CanBePlacedInTerrain, prefab.PlacementNodeRequirement, activeTool);
		if (result == CanPlaceBuilding.invalid) mat = _invalidGhostMaterial;
		else if (result == CanPlaceBuilding.requirementsNotMet) mat = _requirementGhostMaterial;
		// swap all renderer materials to ghost material
		if (ghostObject != null)
		{
			foreach (var r in ghostObject.GetComponentsInChildren<Renderer>())
			{
				if (ghostObject.ExtraGhostRenderers != null && r.transform.IsChildOf(ghostObject.ExtraGhostRenderers.transform))
					continue;
				var mats = r.sharedMaterials;
				for (int i = 0; i < mats.Length; i++)
					if (!_materialsToNotReplaceOnBuildingGhost.Contains(mats[i]))
						mats[i] = mat;
				r.sharedMaterials = mats;
			}
		}
	}
	/// <summary> cleanup ghost when exiting build mode </summary>
	public void CleanUpGhostObject()
	{
		isEligibleForSnapping = true;
		currentObjectIsSnapped = false;
		if (ghostObject != null) { Destroy(ghostObject.gameObject); ghostObject = null; }
	}
	#endregion

	#region private API
	void SetupGhostObject(Vector3Int gridPos, BuildingObject prefab, Quaternion rotation)
	{
		if (ghostObject == null)
		{
			ghostObject = Instantiate(prefab);
			ghostObject.IsGhost = true;
			// disable triggers, non-BuildingObject monobehaviours, audio, particles
			foreach (var col in ghostObject.GetComponentsInChildren<Collider>())
				if (col.isTrigger) Destroy(col.gameObject);
			foreach (var mb in ghostObject.GetComponentsInChildren<MonoBehaviour>(true))
				if (!(mb is BuildingObject)) mb.enabled = false;
			foreach (var audio in ghostObject.GetComponentsInChildren<AudioSource>(true))
				audio.enabled = false;
			foreach (var ps in ghostObject.GetComponentsInChildren<ParticleSystem>())
				ps.gameObject.SetActive(false);
			// set layer, make kinematic
			UtilsPhaseB.SetLayerRecursively(ghostObject.gameObject, LayerMask.NameToLayer("BuildingGhost"));
			foreach (var rb in ghostObject.GetComponentsInChildren<Rigidbody>())
				rb.isKinematic = true;
		}
		ghostObject.transform.position = gridPos + new Vector3(0.5f, 0f, 0.5f) + ghostObject.BuildModePlacementOffset;
		ghostObject.transform.rotation = rotation;
	}
	void OnDestroy()
	{
		if (ghostObject != null) Destroy(ghostObject.gameObject);
	}
	#endregion
}