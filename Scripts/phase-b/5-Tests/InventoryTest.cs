using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using TMPro;

using SPACE_UTIL;

/// <summary>
/// add/remove/switch tools test — no player movement, no grab, no shop
/// Prerequisites: InventoryDataService.cs, InventoryOrchestrator.cs, InventoryUI.cs, Field_InventorySlot.cs, BaseHeldTool.cs, GameEvents.cs
/// NOT required: PlayerMovement, PlayerGrab, shop, interaction
/// How to test:
///   Space → fire RaiseToolPickupRequested on _testTool[0]
///   U → fire RaiseToolPickupRequested on _testTool[1]
///   1-9-0 → switch hotbar slots
///   Scroll → cycle hotbar
///   G → drop active tool
///   Tab → toggle inventory panel
///   O → log snapshot
/// Controls: Space, U, 1-0, Scroll, G, Tab, O
/// </summary>
public class InventoryTest : MonoBehaviour
{
	#region Inspector Fields
	[SerializeField] InventoryOrchestrator _orchestrator;
	[SerializeField] List<BaseHeldTool> _testTools;
	[TextArea(5, 10)]
	string README = @"Space → pickup testTool[0]
U → pickup testTool[1]
1-0 → switch slots
Scroll → cycle
G → drop active
Tab → toggle inventory
O → log snapshot";
	#endregion

	#region Unity Life Cycle
	bool isFirstEnable = true;
	private void OnEnable()
	{
		Debug.Log(C.method(this));
		if (isFirstEnable)
		{
			INPUT.UI.SetCursor(isFpsMode: true);
			// purpose: log tool switch events
			GameEvents.OnToolSwitched += (idx) => Debug.Log($"[InventoryTest] ToolSwitched to slot {idx}".colorTag("lime"));
			// purpose: log tool pickup events
			GameEvents.OnItemPickedUp += (tool) => Debug.Log($"[InventoryTest] Picked up: {tool.GetName()}".colorTag("cyan"));
			// purpose: log tool drop events
			GameEvents.OnItemDropped += (tool) => Debug.Log($"[InventoryTest] Dropped: {tool.GetName()}".colorTag("orange"));
			// purpose: log inventory view events
			GameEvents.OnOpenInventoryView += () => Debug.Log("[InventoryTest] Inventory OPENED".colorTag("lime"));
			GameEvents.OnCloseInventoryView += () => Debug.Log("[InventoryTest] Inventory CLOSED".colorTag("orange"));
			isFirstEnable = false;
		}
	}
	//
	private void Update()
	{
		INPUT.UI.SetCursor(isFpsMode: !Singleton<UIManager>.Ins.GetIsAnyMenuOpen());
		//
		if (INPUT.K.InstantDown(KeyCode.Space))
		{
			// purpose: simulate tool pickup from world
			if (_testTools.Count > 0)
				GameEvents.RaiseToolPickupRequested(_testTools[0]);
		}
		else if (INPUT.K.InstantDown(KeyCode.U))
		{
			// purpose: simulate second tool pickup from world
			if (_testTools.Count > 1)
				GameEvents.RaiseToolPickupRequested(_testTools[1]);
		}
		else if (INPUT.K.InstantDown(KeyCode.O))
		{
			LOG.AddLog(this._orchestrator.GetDataServiceForTest().GetSnapshot("inventory test"), "json");
		}
		else if (INPUT.K.InstantDown(KeyCode.Tab))
		{
			// purpose: toggle inventory view
			GameEvents.RaiseOpenInventoryView();
		}
	}
	#endregion
}
