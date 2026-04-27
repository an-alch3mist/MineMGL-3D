using UnityEngine;

/// <summary> Phase B skeleton — Reload rotates, Q cycles. Phase D completes PrimaryFire + Update. </summary>
[AddComponentMenu("MineMGL/Tools/ToolBuilder")]
public class ToolBuilder : BaseHeldTool
{
	[SerializeField] float _useRange = 3f;
	int currentPrefabIndex;
	Quaternion currentRotation = Quaternion.identity;

	public override string GetEquipButtonLabel() => "Build";
	public override void PrimaryFire() { /* Phase D: full placement */ }
	public override void Reload()
	{
		currentRotation *= Quaternion.Euler(0f, 90f, 0f);
	}
	public override void QButtonPressed() { currentPrefabIndex++; }
	public override void DropItem() { base.DropItem(); /* Phase D: spawn BuildingCrate */ }
	protected override void OnDisable() { base.OnDisable(); /* Phase D: cleanup ghost */ }
}