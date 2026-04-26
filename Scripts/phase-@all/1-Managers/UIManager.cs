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
[DefaultExecutionOrder(-100)] // just after INITManager
public class UIManager : Singleton<UIManager>
{
	bool isAnyMenuOpen;
	/// <summary> fires OnCloseAllSubManagers — every SubManager self-closes </summary>
	public void CloseAllSubManager()
	{
		GameEvents.RaiseCloseShopView();
		GameEvents.RaiseCloseInteractionView();
		GameEvents.RaiseCloseInventoryView();
	}

	#region public API
	/// <summary>
	/// isAnyMenuOpen ?
	/// </summary>
	public bool GetIsAnyMenuOpen() => isAnyMenuOpen;
	#endregion

	#region Unity Life Cycle
	bool isFirstEnable = true;
	private void OnEnable()
	{
		if(isFirstEnable)
		{
			// subscription always done in onEnable
			// raise is made in start on further in unity life cycle.
			GameEvents.OnMenuStateChanged += (isAnyMenuOpen) => this.isAnyMenuOpen = isAnyMenuOpen;
			isFirstEnable = false;
		}
	}
	private void Update()
	{
		// esc: closeAllSubManagers/openPauseMenu
		if (INPUT.K.InstantDown(KeyCode.Escape))
		{
			Debug.Log($"isAnyMenuOpen: {isAnyMenuOpen}".colorTag("cyan"));
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
