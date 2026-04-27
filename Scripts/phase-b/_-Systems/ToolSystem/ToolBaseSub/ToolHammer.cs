using UnityEngine;

/// <summary>
/// LMB raycasts → Phase D: BuildingObject.TryTakeOrPack / BuildingCrate.TryAddToInventory.
/// RMB raycasts → Phase D: BuildingObject.Pack.
/// </summary>
[AddComponentMenu("MineMGL/Tools/ToolHammer")]
public class ToolHammer : BaseHeldTool
{
	[SerializeField] float _useRange = 3f;

	public override void PrimaryFire()
	{
		if (ownerCam == null) return;
		if (!Physics.Raycast(ownerCam.transform.position, ownerCam.transform.forward, out RaycastHit hit, _useRange)) return;
		// Phase D: BuildingObject / BuildingCrate interaction
	}
	public override void SecondaryFire()
	{
		if (ownerCam == null) return;
		if (!Physics.Raycast(ownerCam.transform.position, ownerCam.transform.forward, out var hit, _useRange)) return;
		// Phase D: BuildingObject.Pack()
	}
}