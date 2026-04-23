using UnityEngine;

/// <summary>
/// pairs left/right footstep sounds
/// </summary>
[CreateAssetMenu(menuName = "SO/SO_FootstepSoundDefinition", fileName = "SO_FootstepSoundDefinition")]
public class SO_FootstepSoundDefinition : ScriptableObject
{
	// Phase H: change to SO_SoundDefinition when SoundManager exists
	public AudioClip LeftFootstepClip;
	public AudioClip RightFootstepClip;
}