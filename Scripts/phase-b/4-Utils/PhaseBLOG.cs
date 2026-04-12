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
	public static string LIST_SLOT__TO__JSON(List<InventoryDataService.Slot> SLOT)
	{
		var snapshot = SLOT.map(slot =>
		{
			return new
			{
				tool = new
				{
					toolName = (slot.tool != null) ? slot.tool.GetName() : "null",
					toolQty = (slot.tool != null) ? slot.tool.GetQty() : 0,
				},
				slot.index,
				slot.isHotBar,
			};
		});
		return snapshot.ToNSJson(pretify: true);
	}
}
