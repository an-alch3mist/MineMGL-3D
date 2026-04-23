using UnityEngine;

/// <summary>
/// I toggle building support scaffolding on/off. Left-click raycasts and disables supports,
/// right-click enables them. Phase D adds the actual BuildingObject.EnableBuildingSupports call.
/// Currently both are stubs with comments showing where the Phase D logic goes.
///
/// Who uses me: InventoryOrchestrator (PrimaryFire, SecondaryFire).
/// </summary>
public class ToolSupportsWrench : BaseHeldTool
{
	#region Inspector Fields
	[SerializeField] float _useRange = 3f;
	#endregion

	#region public API — overrides
	public override void PrimaryFire()
	{
		if (_owner == null) return;
		Camera cam = _owner.PlayerCam;
		if (cam == null || !Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, _useRange)) return;
		// Phase D: hit.collider.GetComponentInParent<BuildingObject>()?.EnableBuildingSupports(false);
	}
	public override void SecondaryFire()
	{
		if (_owner == null) return;
		Camera cam = _owner.PlayerCam;
		if (cam == null || !Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, _useRange)) return;
		// Phase D: hit.collider.GetComponentInParent<BuildingObject>()?.EnableBuildingSupports(true);
	}
	#endregion
}