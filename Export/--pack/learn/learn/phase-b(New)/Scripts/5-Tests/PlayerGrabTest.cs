using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

using SPACE_UTIL;

/// <summary>
/// SpringJoint grab test — no inventory, no tools
/// Prerequisites: PlayerMovement.cs, PlayerCamera.cs, PlayerGrab.cs, GameEvents.cs
/// NOT required: Inventory, tools, shop
/// How to test:
///   Right-click on Grabbable → SpringJoint connects, rope visible
///   Right-click again → releases, rope gone
///   M/N → menu toggle
/// Controls: Mouse1 (grab/release), M (menu open), N (menu close)
/// </summary>
public class PlayerGrabTest : MonoBehaviour
{
	#region Unity Life Cycle
	private void Start()
	{
		INPUT.UI.SetCursor(isFpsMode: true);
		// purpose: log grab events
		GameEvents.OnMenuStateChanged += (open) => Debug.Log($"[PlayerGrabTest] MenuState: {open}".colorTag("cyan"));
	}
	private void Update()
	{
		// purpose: simulate menu open
		if (INPUT.K.InstantDown(KeyCode.M)) GameEvents.RaiseMenuStateChanged(isAnyMenuOpen: true);
		// purpose: simulate menu close
		if (INPUT.K.InstantDown(KeyCode.N)) GameEvents.RaiseMenuStateChanged(isAnyMenuOpen: false);
	}
	#endregion
}