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
	public static event Action<IInventoryItem> OnToolPickupRequested;
	public static void RaiseToolPickupRequested(IInventoryItem item)
	{
		LogSubscribersCount(nameof(OnToolPickupRequested), OnToolPickupRequested);
		GameEvents.OnToolPickupRequested?
			.Invoke(item);
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
	public static event Action<IInventoryItem> OnItemPickedUp;
	public static void RaiseItemPickedUp(IInventoryItem item)
	{
		LogSubscribersCount(nameof(OnItemPickedUp), OnItemPickedUp);
		GameEvents.OnItemPickedUp?
			.Invoke(item);
	}
	// << when item was picked up into inventory

	// when item was dropped from inventory >>
	public static event Action<IInventoryItem> OnItemDropped;
	public static void RaiseItemDropped(IInventoryItem item)
	{
		LogSubscribersCount(nameof(OnItemDropped), OnItemDropped);
		GameEvents.OnItemDropped?
			.Invoke(item);
	}
	// << when item was dropped from inventory

	// when item was equipped (activated in hotbar) >>
	public static event Action<IInventoryItem> OnItemEquipped;
	public static void RaiseItemEquipped(IInventoryItem item)
	{
		LogSubscribersCount(nameof(OnItemEquipped), OnItemEquipped);
		GameEvents.OnItemEquipped?
			.Invoke(item);
	}
	// << when item was equipped
}
