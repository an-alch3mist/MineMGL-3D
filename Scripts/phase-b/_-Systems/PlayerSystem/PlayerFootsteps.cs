using UnityEngine;

/// <summary>
/// I play footstep sounds when the player walks. I read PlayerMovement's speed and grounded
/// state to decide when to trigger a footstep. I alternate left/right clips from the assigned
/// SO_FootstepSoundDefinition. Currently all sound calls are Phase H stubs — no audio plays
/// until SoundManager exists. I also have a water variant for when IsInWater is true.
///
/// Who uses me: Self (Update reads PlayerMovement). No external callers.
/// Events I fire: none. Events I subscribe to: none.
/// </summary>
public class PlayerFootsteps : MonoBehaviour
{
	#region Inspector Fields
	[SerializeField] PlayerMovement _movement;
	[SerializeField] SO_FootstepSoundDef _defaultFootsteps;
	[SerializeField] SO_FootstepSoundDef _waterFootsteps;
	[SerializeField] float _baseFootstepInterval = 0.6f;
	[SerializeField] float _minMoveSpeed = 0.1f;
	[SerializeField] LayerMask _groundCheckLayer;
	#endregion

	#region private API
	float footstepTimer;
	bool lastWasLeft;
	#endregion

	#region Unity Life Cycle
	private void Update()
	{
		float speed = (_movement.MoveInput * _movement.SelectedWalkSpeed).magnitude;
		if (speed > _minMoveSpeed && _movement.CC.isGrounded)
		{
			footstepTimer -= Time.deltaTime;
			float t = Mathf.Clamp01(speed / _movement.SprintSpeed);
			float interval = Mathf.Lerp(_baseFootstepInterval * 1.5f, _baseFootstepInterval * 0.5f, t);
			if (footstepTimer <= 0f)
			{
				lastWasLeft = !lastWasLeft;
				// Phase H: Singleton<SoundManager>.Ins.PlayFootstep(def, lastWasLeft);
				footstepTimer = interval;
			}
		}
		else footstepTimer -= Time.deltaTime * 2f;
	}
	#endregion
}