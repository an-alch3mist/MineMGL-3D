using UnityEngine;

public class RbDraggerController : MonoBehaviour
{
	[SerializeField] PlayerGrab _playerGrab;
	private void OnJointBreak(float breakForce)
	{
		this._playerGrab.ForceRelease();
	}
}
