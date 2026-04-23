using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

using SPACE_UTIL;

[CreateAssetMenu(menuName = "SO/SO_InteractionOption", fileName = "SO_InteractionOption")]
public class SO_InteractionOption : ScriptableObject
{
	public string interactionName;
	[TextArea(2, 3)] public string descr;
	public Sprite sprite;
}