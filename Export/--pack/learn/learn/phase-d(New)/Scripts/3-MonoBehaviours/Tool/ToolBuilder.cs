using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// I'm the equippable tool for placing buildings. Phase B created me as a partial stub — Phase D
/// completes all logic. Every frame in Update, I raycast from the camera, snap the hit point to
/// the 1m grid via BuildingDataService.GetClosestGridPosition, then call
/// BuildingManager.UpdateGhostObject to position/validate/material-swap the ghost preview.
///
/// PrimaryFire: validates via BuildingManager.CanPlaceObject (overlap, flat, node, snap) — if Valid,
/// Instantiates the real building, fires RaiseBuildingPlaced, decrements Quantity, consumes self at 0.
/// Reload: rotates ghost 90° (or -90° if UseReverseRotationDirection).
/// QButtonPressed: cycles to next building prefab variant (e.g. L/R mirror) + destroys current ghost.
/// DropItem: spawns a BuildingCrate (not a raw tool drop) with Definition + Quantity, destroys self.
/// OnDisable: cleans up ghost object + hides placement node ghosts.
///
/// I also show/hide BuildingPlacementNode ghost indicators during Update — each node shows whether
/// it matches the current building's PlacementNodeRequirement.
///
/// Who uses me: InventoryOrchestrator (PrimaryFire, Reload, QButtonPressed, DropItem via tool actions).
/// Events I fire: RaiseBuildingPlaced, RaiseItemDropped.
/// </summary>
public class ToolBuilder : BaseHeldTool
{
	#region Inspector Fields
	[SerializeField] float _useRange = 3f;
	#endregion

	#region private API
	SO_BuildingInventoryDefinition definition;
	int quantity = 1;
	Quaternion currentRotation = Quaternion.identity;
	int currentPrefabIndex;
	bool isMirrored;
	#endregion

	#region public API
	public SO_BuildingInventoryDefinition GetDefinition() => definition;
	public void SetDefinition(SO_BuildingInventoryDefinition def) => definition = def;
	public int GetQuantity() => quantity;
	public void SetQuantity(int qty) => quantity = qty;
	public Quaternion GetCurrentRotation() => currentRotation;
	public void SetCurrentRotation(Quaternion rot) => currentRotation = rot;
	public bool IsUsingMirroredVersion() => isMirrored;
	/// <summary> configure from definition — called after Instantiate </summary>
	public void Setup()
	{
		if (definition == null) return;
		_name = definition.Name;
		_inventoryIcon = definition.GetIcon();
	}
	/// <summary> get the currently selected building prefab </summary>
	public BuildingObject GetCurrentPrefab()
	{
		if (definition == null || definition.BuildingPrefabs == null || definition.BuildingPrefabs.Count == 0) return null;
		currentPrefabIndex = Mathf.Clamp(currentPrefabIndex, 0, definition.BuildingPrefabs.Count - 1);
		return definition.BuildingPrefabs[currentPrefabIndex];
	}
	#endregion

	#region public API — overrides
	public override void PrimaryFire()
	{
		if (owner == null || definition == null) return;
		var prefab = GetCurrentPrefab();
		if (prefab == null) return;
		var mgr = Singleton<BuildingManager>.Ins;
		Camera cam = owner.PlayerCam;
		if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, _useRange)) return;
		Vector3Int gridPos = mgr.DataService.GetClosestGridPosition(hit.point);
		var canPlace = mgr.CanPlaceObject(gridPos, prefab, currentRotation,
			prefab.RequiresFlatGround, definition.CanBePlacedInTerrain, prefab.PlacementNodeRequirement, this);
		if (canPlace != CanPlaceBuilding.valid) return;
		// place the real building
		var placed = Object.Instantiate(prefab,
			gridPos + new Vector3(0.5f, 0f, 0.5f) + prefab.BuildModePlacementOffset,
			currentRotation);
		placed.IsGhost = false;
		// purpose: quest/research systems track building placed
		GameEvents.RaiseBuildingPlaced(placed);
		// consume quantity
		quantity--;
		if (quantity <= 0)
		{
			// remove tool from inventory + cleanup ghost
			mgr.CleanUpGhostObject();
			GameEvents.RaiseItemDropped(this);
			Destroy(gameObject);
		}
		else
		{
			mgr.SetCurrentObjectIsSnapped(false);
		}
	}
	public override void Reload()
	{
		float dir = definition != null && definition.UseReverseRotationDirection ? -90f : 90f;
		currentRotation *= Quaternion.Euler(0f, dir, 0f);
		Singleton<BuildingManager>.Ins.SetCurrentObjectIsSnapped(false);
	}
	public override void QButtonPressed()
	{
		if (definition == null || definition.BuildingPrefabs == null || definition.BuildingPrefabs.Count <= 1) return;
		currentPrefabIndex = (currentPrefabIndex + 1) % definition.BuildingPrefabs.Count;
		isMirrored = !isMirrored;
		Singleton<BuildingManager>.Ins.CleanUpGhostObject();
	}
	public override void DropItem()
	{
		// drop as crate, not raw tool
		Singleton<BuildingManager>.Ins.CleanUpGhostObject();
		if (definition != null)
		{
			var crate = Object.Instantiate(
				definition.PackedPrefab ? definition.PackedPrefab : Singleton<BuildingManager>.Ins.GetBuildingCratePrefab(),
				owner != null ? owner.PlayerCam.transform.position + owner.PlayerCam.transform.forward * 1.5f : transform.position,
				Quaternion.identity);
			crate.SetDefinition(definition);
			crate.SetQuantity(quantity);
		}
		Destroy(gameObject);
	}
	#endregion

	#region Unity Life Cycle
	private void Update()
	{
		if (owner == null || definition == null) return;
		var prefab = GetCurrentPrefab();
		if (prefab == null) return;
		Camera cam = owner.PlayerCam;
		if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, _useRange)) return;
		Vector3Int gridPos = Singleton<BuildingManager>.Ins.DataService.GetClosestGridPosition(hit.point);
		Singleton<BuildingManager>.Ins.UpdateGhostObject(gridPos, prefab, currentRotation, this);
		// show placement node ghosts
		foreach (var node in BuildingPlacementNode.All)
			node.ShowGhost(true, prefab.PlacementNodeRequirement);
	}
	protected override void OnDisable()
	{
		base.OnDisable();
		Singleton<BuildingManager>.Ins?.CleanUpGhostObject();
		foreach (var node in BuildingPlacementNode.All)
			node.ShowGhost(false);
	}
	#endregion
}