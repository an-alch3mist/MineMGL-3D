using UnityEngine;

/// <summary> LMB disables supports, RMB enables. Phase D adds BuildingObject.EnableBuildingSupports. </summary>
[AddComponentMenu("MineMGL/Tools/ToolSupportsWrench")]
public class ToolSupportsWrench : BaseHeldTool
{
	[SerializeField] float _useRange = 3f;

	public override void PrimaryFire()
	{
		if (ownerCam == null) return;
		if (!Physics.Raycast(ownerCam.transform.position, ownerCam.transform.forward, out var hit, _useRange)) return;
	}
	public override void SecondaryFire()
	{
		if (ownerCam == null) return;
		if (!Physics.Raycast(ownerCam.transform.position, ownerCam.transform.forward, out var hit, _useRange)) return;
	}
}