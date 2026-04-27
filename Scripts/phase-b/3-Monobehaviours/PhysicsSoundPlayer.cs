using UnityEngine;

/// <summary>
/// I play an impact sound when this physics object collides with something. I check the
/// collision velocity against _minImpactVelocity and respect a cooldown to prevent spam.
/// ToolPickaxe also calls my PlayImpactSound directly when it hits me with the pickaxe.
/// All actual sound calls are Phase H stubs until SoundManager exists.
///
/// Who uses me: Unity physics (OnCollisionEnter), ToolPickaxe (PlayImpactSound on hit).
/// </summary>
public class PhysicsSoundPlayer : MonoBehaviour
{
	#region Inspector Fields
	[SerializeField] AudioClip _impactClip;
	[SerializeField] float _minImpactVelocity = 1f;
	[SerializeField] float _cooldown = 0.1f;
	#endregion

	#region private API
	float lastPlayTime;
	#endregion

	#region Unity Life Cycle
	/// <summary> When something collides with me, checks if the impact velocity exceeds the minimum
	/// threshold and if enough time has passed since the last play. If both pass, plays the impact
	/// sound at the contact point (Phase H stub — no actual sound until SoundManager exists). </summary>
	private void OnCollisionEnter(Collision collision)
	{
		// → skip if no contact or cooldown hasn't elapsed
		if (collision.contactCount == 0 || Time.time - lastPlayTime < _cooldown) return;
		// → check if collision velocity is strong enough to trigger sound
		float sqrVel = collision.relativeVelocity.sqrMagnitude;
		if (sqrVel <= _minImpactVelocity) return;
		lastPlayTime = Time.time;
		// → play impact sound at contact point (Phase H adds actual SoundManager call)
		// Phase H: Singleton<SoundManager>.Ins.PlaySoundAtLocation(_impactDef, collision.GetContact(0).point);
	}
	#endregion

	#region public API
	/// <summary> Called by ToolPickaxe when its raycast hits an object with this component.
	/// Forces an impact sound to play at this object's position regardless of collision state. </summary>
	public void PlayImpactSound()
	{
		// Phase H: Singleton<SoundManager>.Ins.PlaySoundAtLocation(_impactDef, transform.position);
	}
	#endregion
}