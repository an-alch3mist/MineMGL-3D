using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

using SPACE_UTIL;

public static class PhaseALOG
{
	public static string LIST_CATEGORY__TO__JSON(List<SO_ShopCategory> CATEGORY)
	{
		var snapshot = CATEGORY.map(category => new
		{
			category.categoryName,
			category.hideIfAllItemsLocked,
			sprite = category.sprite.name,
			ITEM_DEF = category.ITEM_DEF.map(def => new
			{
				def.itemDefName,
				def.defaultPrice,
				def.isDefaultLocked,
				sprite = def.sprite.name,
				obj = def.pfObject.name,
			}),
		});
		return snapshot.ToNSJson(pretify: true);
	}
	public static string DOC_CATEGORY__TO__ITEM(Dictionary<SO_ShopCategory, List<WShopItem>> DOC)
	{
		var snapshot = DOC.map(kvp => new
		{
			kvp.Key.categoryName,
			ITEM_DEF = kvp.Value.map(wItem => new
			{
				wItem.itemDef.name,
				wItem.itemDef.isDefaultLocked,
				wItem.isLockedCurr,
				wItem.timesPurchased,
			}),
		});
		return snapshot.ToNSJson(pretify: true);
	}
}
