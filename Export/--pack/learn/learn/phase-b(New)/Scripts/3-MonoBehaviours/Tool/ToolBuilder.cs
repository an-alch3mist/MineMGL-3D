using UnityEngine;

/// <summary>
/// I show a ghost building preview and place buildings on left-click. Phase B creates the skeleton
/// — Reload rotates 90°, QButtonPressed cycles alternate prefabs, DropItem spawns a BuildingCrate
/// instead of raw drop. Phase D completes PrimaryFire (grid snap, ghost material green/red,
/// conveyor auto-snap) and Update (ghost follows cursor). Currently mostly stubs with comments.
///
/// Who uses me: InventoryOrchestrator (PrimaryFire, Reload, QButtonPressed, DropItem).
/// </summary>
public class ToolBuilder : BaseHeldTool
{
	#region Inspector Fields
	[SerializeField] float _useRange = 3f;
	// Phase D: [SerializeField] SO_BuildingInventoryDefinition _definition;
	#endregion

	#region private API
	int currentPrefabIndex;
	Quaternion currentRotation = Quaternion.identity;
	#endregion

	#region public API — overrides
	public override void PrimaryFire()
	{
		// Phase D: full placement logic — grid snap, ghost, instantiate building
	}
	public override void Reload()
	{
		currentRotation *= Quaternion.Euler(0f, 90f, 0f);
		// Phase D: Singleton<BuildingManager>.Ins.CurrentObjectIsSnapped = false;
	}
	public override void QButtonPressed()
	{
		// Phase D: cycle alternate building models
		currentPrefabIndex++;
	}
	public override void DropItem()
	{
		// Phase D: spawn BuildingCrate instead of raw drop
		base.DropItem();
	}
	#endregion

	#region Unity Life Cycle
	protected override void OnDisable()
	{
		base.OnDisable();
		// Phase D: cleanup ghost object
	}
	#endregion
}