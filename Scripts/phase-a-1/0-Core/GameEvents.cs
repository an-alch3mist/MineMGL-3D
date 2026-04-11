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
/// no modification to phase-a GameEvents required
/// </summary>
public static partial class GameEvents
{
	/*
	// when Somthng occured >>
	public static event Action<int> OnSomthng;
	public static void RaiseSomthng(int index)
	{
		LogSubscribersCount(nameof(OnSomthng), OnSomthng);
		GameEvents.OnSomthng? // if there is any subscribers
			.Invoke(index);
	}
	// << when Somthng occured
	*/

	// when elevator landed >>
	public static event Action OnElevatorLanded;
	public static void RaiseElevatorLanded()
	{
		LogSubscribersCount(nameof(OnElevatorLanded), OnElevatorLanded);
		GameEvents.OnElevatorLanded? // if there is any subscribers
			.Invoke();
	}
	// <<  when elevator landed

	// when game paused >>
	public static event Action OnGamePaused;
	public static void RaiseGamePaused()
	{
		LogSubscribersCount(nameof(OnGamePaused), OnGamePaused);
		GameEvents.OnGamePaused? // if there is any subscribers
			.Invoke();
	}
	// << when game paused


	// when game un-paused >>
	public static event Action OnGameUnPaused;
	public static void RaiseGameUnPaused()
	{
		LogSubscribersCount(nameof(OnGameUnPaused), OnGameUnPaused);
		GameEvents.OnGameUnPaused? // if there is any subscribers
			.Invoke();
	}
	// << when game un-paused
}
