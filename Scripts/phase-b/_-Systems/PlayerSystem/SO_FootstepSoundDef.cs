using UnityEngine;

/// <summary>
/// pairs left/right footstep sounds
/// </summary>
[CreateAssetMenu(menuName = "SO/SO_FootstepSoundDef", fileName = "SO_FootstepSoundDef")]
public class SO_FootstepSoundDef : ScriptableObject
{
	// Phase H: change to SO_SoundDefinition when SoundManager exists
	public AudioClip LeftFootstepClip;
	public AudioClip RightFootstepClip;
}