using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using SPACE_UTIL;

/// <summary>
/// plain C# data collection service.
/// </summary>
public class ShopCategoryItemService
{
	// build and holds 
	// - List<SO_ShopCategory>
	// - Doc<SO_ShopCategory, List<WShopItem>>

	#region private API
	List<SO_ShopCategory> CATEGORY;
	Dictionary<SO_ShopCategory, List<WShopItem>> MAP__CATEGORY__WITEM_LIST;
	#endregion

	#region public API
	public void Build(List<SO_ShopCategory> CATEGORY)
	{
		this.CATEGORY = CATEGORY;
		MAP__CATEGORY__WITEM_LIST = new Dictionary<SO_ShopCategory, List<WShopItem>>();
		foreach (var category in CATEGORY)
			MAP__CATEGORY__WITEM_LIST[category] = category.ITEM_DEF.map(def => new WShopItem(def)).ToList();
	}

	public List<SO_ShopCategory> GetCategories() => CATEGORY;
	public List<WShopItem> GetWItems(SO_ShopCategory category) => MAP__CATEGORY__WITEM_LIST[category];
	public bool IsAllLocked(SO_ShopCategory category)
	{
		foreach (var wItem in MAP__CATEGORY__WITEM_LIST[category])
			if (wItem.isLockedCurr == false)
				return false;
		return true;
	}
	// LOG
	public string GetSnapShot()
	{
		return PhaseALOG.
	}
	#endregion
}

/// <summary>
/// plain C# data collection service.
/// </summary>
public class ShopCartService
{



}