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
	// when inventory view opened >>
	public static event Action OnOpenInventoryView;
	public static void RaiseOpenInventoryView()
	{
		LogSubscribersCount(nameof(OnOpenInventoryView), OnOpenInventoryView);
		GameEvents.OnOpenInventoryView? // if there is any subscribers
			.Invoke();
	}
	// << when inventory view opened
	// when inventory view closed >>
	public static event Action OnCloseInventoryView;
	public static void RaiseCloseInventoryView()
	{
		LogSubscribersCount(nameof(OnCloseInventoryView), OnCloseInventoryView);
		GameEvents.OnCloseInventoryView? // if there is any subscribers
			.Invoke();
	}
	// << when inventory view closed

	// when tool pickup was requested from world >>
	public static event Action<BaseHeldTool> OnToolPickupRequested;
	public static void RaiseToolPickupRequested(BaseHeldTool tool)
	{
		LogSubscribersCount(nameof(OnToolPickupRequested), OnToolPickupRequested);
		GameEvents.OnToolPickupRequested? // if there is any subscribers
			.Invoke(tool);
	}
	// << when tool pickup was requested from world

	// when active tool was switched >>
	public static event Action<int> OnToolSwitched;
	public static void RaiseToolSwitched(int slotIndex)
	{
		LogSubscribersCount(nameof(OnToolSwitched), OnToolSwitched);
		GameEvents.OnToolSwitched? // if there is any subscribers
			.Invoke(slotIndex);
	}
	// << when active tool was switched

	// when item was picked up into inventory >>
	public static event Action<BaseHeldTool> OnItemPickedUp;
	public static void RaiseItemPickedUp(BaseHeldTool tool)
	{
		LogSubscribersCount(nameof(OnItemPickedUp), OnItemPickedUp);
		GameEvents.OnItemPickedUp? // if there is any subscribers
			.Invoke(tool);
	}
	// << when item was picked up into inventory

	// when item was dropped from inventory >>
	public static event Action<BaseHeldTool> OnItemDropped;
	public static void RaiseItemDropped(BaseHeldTool tool)
	{
		LogSubscribersCount(nameof(OnItemDropped), OnItemDropped);
		GameEvents.OnItemDropped? // if there is any subscribers
			.Invoke(tool);
	}
	// << when item was dropped from inventory

	// when tool was equipped (activated in hotbar) >>
	public static event Action<BaseHeldTool> OnToolEquipped;
	public static void RaiseToolEquipped(BaseHeldTool tool)
	{
		LogSubscribersCount(nameof(OnToolEquipped), OnToolEquipped);
		GameEvents.OnToolEquipped? // if there is any subscribers
			.Invoke(tool);
	}
	// << when tool was equipped
}