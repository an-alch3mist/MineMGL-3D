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
/// InventoryDataService plain C# test — zero scene dependency
/// Prerequisites: InventoryDataService.cs, PhaseBLOG.cs
/// NOT required: Player, tools, UI, shop, interaction — nothing
/// How to test:
///   Space → Build 40 slots + log snapshot
///   U → Simulate TryAdd at first empty
///   I → Simulate Remove at slot 0
///   O → SwitchTo slot 3
///   P → Log full snapshot
/// </summary>
public class DEBUG_CheckB : MonoBehaviour
{
	#region Unity Life Cycle
	InventoryDataService ds = new InventoryDataService();

	private void Update()
	{
		if (INPUT.K.InstantDown(KeyCode.Space))
		{
			ds.Build();
			Debug.Log(ds.GetSnapshot("after Build()"));
		}
		else if (INPUT.K.InstantDown(KeyCode.U))
		{
			// simulate adding a tool — in real usage TryAdd takes a BaseHeldTool
			// here we just log the index returned
			Debug.Log($"TryAdd would place at first empty slot".colorTag("cyan"));
			Debug.Log(ds.GetSnapshot("after TryAdd"));
		}
		else if (INPUT.K.InstantDown(KeyCode.I))
		{
			var slot = ds.GetSlots().find(s => s.Tool != null);
			if (slot != null)
			{
				ds.Remove(slot.Tool);
				Debug.Log($"Removed tool from slot {slot.Index}".colorTag("orange"));
			}
			else Debug.Log("no tool to remove".colorTag("red"));
			Debug.Log(ds.GetSnapshot("after Remove"));
		}
		else if (INPUT.K.InstantDown(KeyCode.O))
		{
			ds.SwitchTo(3);
			Debug.Log($"Switched to slot 3, active: {ds.ActiveSlotIndex}".colorTag("lime"));
		}
		else if (INPUT.K.InstantDown(KeyCode.P))
		{
			LOG.AddLog(ds.GetSnapshot("full snapshot"), "json");
		}
	}
	#endregion
}