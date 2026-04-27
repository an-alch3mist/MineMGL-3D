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
/// collections to json
/// </summary>
public static class PhaseBLOG
{
	/*
	public static string LIST_SLOT__TO__JSON(List<InventorySlot> SLOT)
	{
		var snapshot = SLOT.map(slot =>
		{
			return new
			{
				tool = new
				{
					toolName = (slot != null) ? slot.tool.GetName() : "null",
					toolQty = (slot.tool != null) ? slot.tool.GetQty() : 0,
				},
				slot.index,
				slot.isHotBar,
			};
		});
		return snapshot.ToNSJson(pretify: true);
	}
	*/
	/// <summary> snapshot of all inventory slots </summary>
	public static string LIST_SLOT__TO__JSON(List<InventorySlot> SLOT)
	{
		var snapshot = SLOT.map(slot => new
		{
			iInventoryItem = new
			{
				item = slot.item != null ? slot.item.GetName() : "null",
				qty = slot.item != null ? slot.item.GetQty() : 0,
			},
			slot.index,
			slot.isHotBar,
		});
		return snapshot.ToNSJson(pretify: true);
	}
}
