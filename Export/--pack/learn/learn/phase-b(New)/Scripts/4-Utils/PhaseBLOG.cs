using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using SPACE_UTIL;

/// <summary>
/// formats inventory + tool snapshots for test logging
/// </summary>
public static class PhaseBLOG
{
	/// <summary> snapshot of all inventory slots </summary>
	public static string LIST_SLOTS__TO__JSON(List<InventoryDataService.Slot> SLOTS)
	{
		var snapshot = SLOTS.map(slot => new
		{
			slot.Index,
			slot.IsHotbar,
			Tool = slot.Tool != null ? slot.Tool.Name : "null",
			Qty = slot.Tool != null ? slot.Tool.Quantity : 0,
		});
		return snapshot.ToNSJson(pretify: true);
	}
}