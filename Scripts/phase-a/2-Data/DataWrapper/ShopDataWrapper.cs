using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

using SPACE_UTIL;

public class WShopItem
{
	public SO_ShopItemDef itemDef;
	public bool isLockedCurr;
	public int timesPurchased = 0;

	#region constructor
	public WShopItem(SO_ShopItemDef itemDef)
	{
		this.itemDef = itemDef;
		isLockedCurr = itemDef.isDefaultLocked;
		timesPurchased = 0;
	} 
	#endregion

	#region public API
	public bool IsNewlyUnlocked() => ((isLockedCurr == false) && timesPurchased == 0) && (itemDef.isDefaultLocked == true);
	#endregion
}
