using UnityEngine;

/// <summary>
/// disables paired ConveyorBelt when hinge gate angle exceeds threshold — physics-driven gate
/// </summary>
public class ConveyorBlocker : MonoBehaviour
{
	#region Inspector Fields
	[SerializeField] HingeJoint _hinge;
	[SerializeField] ConveyorBelt _conveyor;
	[SerializeField] float _closedAngle = -80f;
	#endregion

	#region Unity Life Cycle
	private void Update()
	{
		if (_hinge != null && _conveyor != null)
			_conveyor.SetDisabled(_hinge.angle < _closedAngle);
	}
	#endregion
}