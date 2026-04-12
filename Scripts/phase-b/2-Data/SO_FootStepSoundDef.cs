using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

using SPACE_UTIL;

[CreateAssetMenu(menuName = "SO/SO_FootStepSoundDef", fileName = "SO_FootStepSoundDef")]
public class SO_FootStepSoundDef : ScriptableObject
{
	public AudioClip leftFootStepClip, 
					 rightGootStepClip;
}
