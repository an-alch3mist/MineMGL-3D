using UnityEngine;

/// <summary>
/// I sit on the RigidbodyDragger child GO (the kinematic rigidbody that PlayerGrab creates
/// SpringJoints on). When the player pulls a grabbed object too far, the SpringJoint breaks
/// and Unity calls my OnJointBreak. I then call PlayerGrab.ForceRelease() which cleans up
/// the joint, rope, drag values, and collision ignoring — same as a normal release but forced.
///
/// Who uses me: Unity physics (OnJointBreak callback). PlayerGrab (ForceRelease).
/// </summary>
public class RigidbodyDraggerController : MonoBehaviour
{
	[SerializeField] PlayerGrab _playerGrab;

	private void OnJointBreak(float breakForce)
	{
		if (_playerGrab != null) _playerGrab.ForceRelease();
	}
}