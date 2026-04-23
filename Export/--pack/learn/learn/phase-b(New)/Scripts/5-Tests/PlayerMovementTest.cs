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
/// WASD + jump + sprint test — no inventory, no tools, no shop
/// Prerequisites: PlayerMovement.cs, PlayerCamera.cs, GameEvents.cs (phase-a + phase-b)
/// NOT required: Inventory, tools, shop, interaction
/// How to test:
///   WASD → move, Space → jump, Shift → sprint, C → duck
///   M → simulate menu open (locks input)
///   N → simulate menu close (unlocks input)
/// Controls: WASD, Space, Shift, C, M, N
/// </summary>
public class PlayerMovementTest : MonoBehaviour
{
	#region Unity Life Cycle
	private void Start()
	{
		INPUT.UI.SetCursor(isFpsMode: true);
		// purpose: log when menu state changes
		GameEvents.OnMenuStateChanged += (open) => Debug.Log($"[PlayerMovementTest] MenuState: {open}".colorTag("cyan"));
	}
	private void Update()
	{
		// purpose: simulate menu open for testing cursor lock
		if (INPUT.K.InstantDown(KeyCode.M)) GameEvents.RaiseMenuStateChanged(isAnyMenuOpen: true);
		// purpose: simulate menu close for testing cursor unlock
		if (INPUT.K.InstantDown(KeyCode.N)) GameEvents.RaiseMenuStateChanged(isAnyMenuOpen: false);
	}
	#endregion
}