using UnityEngine;
using TMPro;

/// <summary> While equipped, raycasts and shows the name of whatever the player looks at. </summary>
[AddComponentMenu("MineMGL/Tools/ToolResourceScanner")]
public class ToolResourceScanner : BaseHeldTool
{
	[SerializeField] float _useRange = 3f;
	[SerializeField] TMP_Text _thingNameText;
	[SerializeField] LayerMask _scanLayers;

	public override void PrimaryFire() { }
	private void Update()
	{
		if (ownerCam == null) return;
		string text = "No Target";
		if (Physics.Raycast(ownerCam.transform.position, ownerCam.transform.forward, out var hit, _useRange, _scanLayers))
		{
			var item = hit.collider.GetComponentInParent<IInventoryItem>();
			if (item != null) text = item.GetName();
		}
		_thingNameText.text = text;
	}
}