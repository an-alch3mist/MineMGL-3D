using System;
using UnityEngine;

/// <summary>
/// partial extend for phase-d — building placed/removed events
/// </summary>
public static partial class GameEvents
{
	// when a building was placed in the world >>
	public static event Action<BuildingObject> OnBuildingPlaced;
	public static void RaiseBuildingPlaced(BuildingObject building)
	{
		LogSubscribersCount(nameof(OnBuildingPlaced), OnBuildingPlaced);
		GameEvents.OnBuildingPlaced?.Invoke(building);
	}
	// << when building placed

	// when a building was removed (taken or packed) >>
	public static event Action<BuildingObject> OnBuildingRemoved;
	public static void RaiseBuildingRemoved(BuildingObject building)
	{
		LogSubscribersCount(nameof(OnBuildingRemoved), OnBuildingRemoved);
		GameEvents.OnBuildingRemoved?.Invoke(building);
	}
	// << when building removed

	// when a building crate requests to be added to inventory >>
	public static event Action<SO_BuildingInventoryDefinition, int> OnBuildingTakeRequested;
	public static void RaiseBuildingTakeRequested(SO_BuildingInventoryDefinition def, int qty = 1)
	{
		LogSubscribersCount(nameof(OnBuildingTakeRequested), OnBuildingTakeRequested);
		GameEvents.OnBuildingTakeRequested?.Invoke(def, qty);
	}
	// << when building crate take requested
}