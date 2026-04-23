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
/// core events shared across ALL phases � partial extended by each phase
/// </summary>
public static partial class GameEvents
{
	// when a certain menu subManager is enable/disabled >>
	public static event Action<bool> OnMenuStateChanged;
	public static void RaiseMenuStateChanged(bool isAnyMenuOpen)
	{
		LogSubscribersCount(nameof(OnMenuStateChanged), OnMenuStateChanged);
		GameEvents.OnMenuStateChanged? // if there is any subscribers
			.Invoke(isAnyMenuOpen);
	}
	// << when a certain menu subManager is enable/disabled

	#region LogSubscribersCount
	static void LogSubscribersCount(string name, Delegate anEvent)
	{
		int subsCount = anEvent?.GetInvocationList().Length ?? 0;
		UnityEngine.Debug.Log($"[GameEvents] {name} raised for -> {subsCount} subscribers".colorTag("lime"));
	}
	#endregion
}