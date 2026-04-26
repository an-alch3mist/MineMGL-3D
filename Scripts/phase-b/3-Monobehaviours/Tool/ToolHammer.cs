using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using SPACE_UTIL;

/// <summary>
/// I pick up or pack placed buildings. Left-click raycasts from the camera — Phase D will add
/// BuildingObject.TryTakeOrPack and BuildingCrate.TryAddToInventory when buildings exist.
/// Right-click does the same for a secondary action. Currently both are stubs with comments.
///
/// Who uses me: InventoryOrchestrator (PrimaryFire, SecondaryFire).
/// </summary>
public class ToolHammer : BaseHeldTool
{
	#region Inspector Fields
	[SerializeField] float _useRange = 3f;
	#endregion

	#region public API — overrides
	public override void PrimaryFire()
	{
		if (owner == null) return;
		Camera cam = owner.GetPlayerCam();
		if (cam == null) return;
		if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, _useRange)) return;
		// Phase D: BuildingObject / BuildingCrate interaction
		// BuildingObject bo = hit.collider.GetComponentInParent<BuildingObject>();
		// if (bo != null) { bo.TryTakeOrPack(); return; }
		// BuildingCrate bc = hit.collider.GetComponentInParent<BuildingCrate>();
		// if (bc != null) bc.TryAddToInventory();
	}
	public override void SecondaryFire()
	{
		if (owner == null) return;
		Camera cam = owner.GetPlayerCam();
		if (cam == null) return;
		if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, _useRange)) return;
		// Phase D: BuildingObject.Pack()
	}
	#endregion
}
