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
/// Pickaxe + Magnet + Hammer + MiningHat usage test — needs player + inventory + tools in scene
/// Prerequisites: PlayerMovement, PlayerCamera, InventoryOrchestrator, BaseHeldTool, ToolPickaxe, ToolMagnet, ToolHammer, ToolMiningHat, GameEvents
/// NOT required: Shop, interaction system, ore nodes, buildings
/// How to test:
///   Space/U/I/O → pickup each tool from world
///   1-4 → switch between equipped tools
///   Left-click hold → pickaxe swing / magnet launch
///   Right-click hold → magnet pull
///   R → magnet gentle drop, Q → cycle mode, G → drop tool
/// Controls: Space, U, I, O, 1-4, Mouse0, Mouse1, R, Q, G
/// </summary>
public class ToolActionTest : MonoBehaviour
{
	#region Inspector Fields
	[SerializeField] List<BaseHeldTool> _testTools;
	[TextArea(5, 10)]
	string README = @"Space → pickup testTools[0] (Pickaxe)
U → pickup testTools[1] (Magnet)
I → pickup testTools[2] (Hammer)
O → pickup testTools[3] (MiningHat)
1-4 → switch tools
Hold LMB → pickaxe swing / magnet launch
Hold RMB → magnet pull
R → magnet drop, Q → cycle mode, G → drop tool";
	#endregion

	#region Unity Life Cycle
	private void Start()
	{
		INPUT.UI.SetCursor(isFpsMode: true);
		// purpose: log tool switch
		GameEvents.OnToolSwitched += (idx) => Debug.Log($"[ToolActionTest] Switched to slot {idx}".colorTag("lime"));
		// purpose: log tool pickup
		GameEvents.OnItemPickedUp += (tool) => Debug.Log($"[ToolActionTest] Picked up: {tool.GetName()}".colorTag("cyan"));
		// purpose: log tool drop
		GameEvents.OnItemDropped += (tool) => Debug.Log($"[ToolActionTest] Dropped: {tool.GetName()}".colorTag("orange"));
	}
	private void Update()
	{
		// purpose: pickup tools from world into inventory
		if (INPUT.K.InstantDown(KeyCode.Space) && _testTools.Count > 0) GameEvents.RaiseToolPickupRequested(_testTools[0]);
		else if (INPUT.K.InstantDown(KeyCode.U) && _testTools.Count > 1) GameEvents.RaiseToolPickupRequested(_testTools[1]);
		else if (INPUT.K.InstantDown(KeyCode.I) && _testTools.Count > 2) GameEvents.RaiseToolPickupRequested(_testTools[2]);
		else if (INPUT.K.InstantDown(KeyCode.O) && _testTools.Count > 3) GameEvents.RaiseToolPickupRequested(_testTools[3]);
	}
	#endregion

}
