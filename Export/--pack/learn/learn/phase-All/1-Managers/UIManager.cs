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
/// reports menu state + closes all panels + routes keyboard input with priority
/// </summary>
public class UIManager : Singleton<UIManager>
{
	#region public API
	public bool isAnyMenuOpen { get; private set; }
	/// <summary> fires OnCloseAllSubManagers — every SubManager self-closes </summary>
	public void CloseAllSubManager() => GameEvents.RaiseCloseAllSubManagers();
	#endregion

	#region Unity Life Cycle
	private void Start()
	{
		// purpose: track menu state from any SubManager opening/closing
		GameEvents.OnMenuStateChanged += (open) => isAnyMenuOpen = open;
	}
	private void Update()
	{
		// ESC: close all if any menu open, else open pause (Phase H)
		if (INPUT.K.InstantDown(KeyCode.Escape))
		{
			if (isAnyMenuOpen) CloseAllSubManager();
			// Phase H: else GameEvents.RaiseOpenPauseMenu();
			return;
		}
		if (isAnyMenuOpen) return;
		// Phase B: Tab → inventory
		if (INPUT.K.InstantDown(KeyCode.Tab)) GameEvents.RaiseOpenInventoryView();
		// Phase F: Q → quest tree
		// if (INPUT.K.InstantDown(KeyCode.Q)) GameEvents.RaiseOpenQuestTreeView();
	}
	#endregion
}