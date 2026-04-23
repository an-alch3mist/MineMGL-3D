using UnityEngine;
using TMPro;

/// <summary>
/// I show the name of whatever the player is looking at while I'm equipped. In Update I
/// raycast from the camera — if I hit a BaseHeldTool I show its Name, if I hit an OrePiece
/// (Phase C) I show its resource type. The text is displayed on my ViewModel's TMP_Text.
/// PrimaryFire does nothing — I'm a passive scan tool. Phase C/D extend the raycast checks
/// to identify OreNodes, BuildingObjects, etc.
///
/// Who uses me: InventoryOrchestrator (equips me, I run my own Update).
/// </summary>
public class ToolResourceScanner : BaseHeldTool
{
	#region Inspector Fields
	[SerializeField] float _useRange = 3f;
	[SerializeField] TMP_Text _thingNameText;
	[SerializeField] LayerMask _scanLayers;
	#endregion

	#region public API — overrides
	public override void PrimaryFire() { }
	#endregion

	#region Unity Life Cycle
	private void Update()
	{
		if (_owner == null) return;
		Camera cam = _owner.PlayerCam;
		if (cam == null) return;
		string text = "No Target";
		if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, _useRange, _scanLayers))
		{
			BaseHeldTool tool = hit.collider.GetComponentInParent<BaseHeldTool>();
			if (tool != null) text = tool.Name;
			// Phase C: OreNode, OrePiece identification
			// Phase D: BuildingObject identification
		}
		_thingNameText.text = text;
	}
	#endregion
}