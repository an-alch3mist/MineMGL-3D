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
/// partial extend for phase-b — no modification to phase-a GameEvents
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

	// when tool was equipped (activated in hotbar) >>
	public static event Action<BaseHeldTool> OnToolEquipped;
	public static void RaiseToolEquipped(BaseHeldTool tool)
	{
		LogSubscribersCount(nameof(OnToolEquipped), OnToolEquipped);
		GameEvents.OnToolEquipped? // if there is any subscribers
			.Invoke(tool);
	}
	// << when tool was equipped

	// when item was dropped from inventory >>
	public static event Action<BaseHeldTool> OnItemDropped;
	public static void RaiseItemDropped(BaseHeldTool tool)
	{
		LogSubscribersCount(nameof(OnItemDropped), OnItemDropped);
		GameEvents.OnItemDropped? // if there is any subscribers
			.Invoke(tool);
	}
	// << when item was dropped from inventory

}
