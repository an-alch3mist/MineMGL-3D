using System.Collections.Generic;
using UnityEngine;
using SPACE_UTIL;

/// <summary>
/// I test building placement, rotation, snap, take-back, and pack-into-crate — no player needed.
/// I create a ToolBuilder at runtime with _testDefinition and Quantity=99 so buildings can be placed
/// endlessly via keyboard. On Start I subscribe to OnBuildingPlaced and OnBuildingRemoved for console
/// logging. Update routes keyboard inputs to ToolBuilder methods.
///
/// Prerequisites: BuildingManager singleton (with all materials/layers assigned), ConveyorBeltManager
/// singleton, GameEvents (phase-d partial), SO_BuildingInventoryDefinition asset with at least one
/// BuildingObject prefab assigned.
/// NOT required: Player movement, shop, ore, interaction system.
///
/// How to test:
///   1. Create scene with BuildingManager + ConveyorBeltManager singletons
///   2. Create SO_BuildingInventoryDefinition → assign a conveyor belt prefab
///   3. Create BuildingTest GO → assign _cam (Camera) + _testDefinition (the SO)
///   4. Press Play → use keys below
///
/// Controls:
///   Space → place building at camera raycast point (grid-snapped)
///   R     → rotate ghost 90° clockwise
///   Q     → cycle to next building variant (mirror/alternate prefab)
///   U     → find nearest BuildingObject → TryTakeOrPack (fires RaiseBuildingTakeRequested)
///   I     → find nearest BuildingObject → Pack into crate
///   O     → log all conveyor belt count + snapshot
///   M/N   → menu toggle (simulate menu open/close)
/// </summary>
public class BuildingTest : MonoBehaviour
{
	#region Inspector Fields
	[SerializeField] Camera _cam;
	[SerializeField] SO_BuildingInventoryDefinition _testDefinition;
	[SerializeField] float _placeRange = 5f;
	[TextArea(5, 10)]
	string README = @"Space → place building
R → rotate 90°
Q → cycle variant
U → take nearest building back
I → pack nearest building into crate
O → log conveyor belt snapshot
M/N → menu toggle";
	#endregion

	#region private API
	ToolBuilder testTool;
	#endregion

	#region Unity Life Cycle
	/// <summary> creates a test ToolBuilder with Quantity=99, subscribes to building events for logging </summary>
	private void Start()
	{
		// purpose: log when a building is placed in the world
		GameEvents.OnBuildingPlaced += (b) => Debug.Log($"[BuildingTest] Placed: {b.Definition?.Name}".colorTag("lime"));
		// purpose: log when a building is removed (taken or packed)
		GameEvents.OnBuildingRemoved += (b) => Debug.Log($"[BuildingTest] Removed: {b.Definition?.Name}".colorTag("orange"));
		// purpose: log when building take is requested (crate or building → inventory)
		GameEvents.OnBuildingTakeRequested += (def, qty) => Debug.Log($"[BuildingTest] TakeRequested: {def?.Name} x{qty}".colorTag("cyan"));
		// → create a test ToolBuilder at runtime — Quantity=99 so we can place endlessly
		testTool = gameObject.AddComponent<ToolBuilder>();
		testTool.Definition = _testDefinition;
		testTool.Quantity = 99;
		testTool.Setup();
	}
	/// <summary> routes keyboard inputs to ToolBuilder methods + take/pack helpers </summary>
	private void Update()
	{
		// → place building at camera raycast hit point
		if (INPUT.K.InstantDown(KeyCode.Space))
		{
			if (_cam == null || _testDefinition == null) return;
			testTool.PrimaryFire();
		}
		// → rotate ghost 90°
		else if (INPUT.K.InstantDown(KeyCode.R)) testTool.Reload();
		// → cycle to next building variant
		else if (INPUT.K.InstantDown(KeyCode.Q)) testTool.QButtonPressed();
		// → find nearest BuildingObject and take it back into inventory
		else if (INPUT.K.InstantDown(KeyCode.U))
		{
			var nearest = FindNearestBuilding();
			if (nearest != null) nearest.TryTakeOrPack();
		}
		// → find nearest BuildingObject and pack it into a crate
		else if (INPUT.K.InstantDown(KeyCode.I))
		{
			var nearest = FindNearestBuilding();
			if (nearest != null) nearest.Pack();
		}
		// → log all conveyor belt count + snapshot
		else if (INPUT.K.InstantDown(KeyCode.O))
		{
			Debug.Log($"ConveyorBelts: {ConveyorBelt.AllConveyorBelts.Count}".colorTag("cyan"));
			Debug.Log(PhaseDLOG.LIST_CONVEYOR_BELTS__TO__JSON());
		}
		// → simulate menu open/close for testing
		else if (INPUT.K.InstantDown(KeyCode.M)) GameEvents.RaiseMenuStateChanged(true);
		else if (INPUT.K.InstantDown(KeyCode.N)) GameEvents.RaiseMenuStateChanged(false);
	}
	/// <summary> finds the closest non-ghost BuildingObject within 10m of camera — used by U/I keys </summary>
	BuildingObject FindNearestBuilding()
	{
		if (_cam == null) return null;
		if (!Physics.Raycast(_cam.transform.position, _cam.transform.forward, out var hit, 10f)) return null;
		return hit.collider.GetComponentInParent<BuildingObject>();
	}
	#endregion
}