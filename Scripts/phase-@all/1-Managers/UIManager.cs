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
	bool isAnyMenuOpen;
	/// <summary> fires OnCloseAllSubManagers — every SubManager self-closes </summary>
	public void CloseAllSubManager()
	{
		GameEvents.RaiseCloseShopView();
		GameEvents.RaiseCloseInteractionView();
	}

	#region public API
	/// <summary>
	/// isAnyMenuOpen ?
	/// </summary>
	public bool GetIsAnyMenuOpen() => isAnyMenuOpen;
	#endregion

	#region Unity Life Cycle
	private void Update()
	{
		// esc: closeAllSubManagers/openPauseMenu
		if (INPUT.K.InstantDown(KeyCode.O))
		{
			if (isAnyMenuOpen)
			{
				Debug.Log("UIManager request close all".colorTag("orange"));
				CloseAllSubManager();
				return;
			}
			else
			{
				// Phase H: else GameEvents.RaiseOpenPauseMenu
				return;
			}
		}
		//
		if (isAnyMenuOpen == false)
		{
			/*
			// Phase B: Tab → inventory
			if (INPUT.K.InstantDown(KeyCode.Tab))
				GameEvents.RaiseOpenInventoryView();
			// Phase F: Q → quest tree
			if (INPUT.K.InstantDown(KeyCode.Q)) 
				GameEvents.RaiseOpenQuestTreeView();
			*/
		}

	}
	#endregion
}
