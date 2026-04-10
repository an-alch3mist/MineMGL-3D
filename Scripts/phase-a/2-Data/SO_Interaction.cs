using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

using SPACE_UTIL;

[CreateAssetMenu(menuName = "SO/SO_Interaction", fileName = "SO_Interaction")]
public class SO_Interaction : ScriptableObject
{
	public string interactionName;
	[TextArea(2, 3)] public string descr;
	public Sprite icon;
}
