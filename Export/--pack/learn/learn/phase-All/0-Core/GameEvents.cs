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
/// core events shared across ALL phases — partial extended by each phase
/// </summary>
public static partial class GameEvents
{
	// when close all sub managers >>
	public static event Action OnCloseAllSubManagers;
	public static void RaiseCloseAllSubManagers()
	{
		LogSubscribersCount(nameof(OnCloseAllSubManagers), OnCloseAllSubManagers);
		GameEvents.OnCloseAllSubManagers? // if there is any subscribers
			.Invoke();
	}
	// << when close all sub managers

	#region LogSubscribersCount
	static void LogSubscribersCount(string name, Delegate anEvent)
	{
		int subsCount = anEvent?.GetInvocationList().Length ?? 0;
		UnityEngine.Debug.Log($"[GameEvents] {name} raised for -> {subsCount} subscribers".colorTag("lime"));
	} 
	#endregion
}