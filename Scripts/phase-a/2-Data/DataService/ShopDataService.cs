using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using SPACE_UTIL;

/// <summary>
/// purely C# Collection Service.
/// (Build + Get + Add + Remove + (boolean questions) + snapshot) data.
/// </summary>
public class ShopDataService
{
	#region private API
	List<SO_ShopCategory> CATEGORY = new List<SO_ShopCategory>();
	Dictionary<SO_ShopCategory, List<WShopItem>> DOC__category_wShopItem;

	List<CartItem> CARTITEM = new List<CartItem>();
	#endregion

	#region Nested Type
	public class CartItem
	{
		public WShopItem wShopItem;
		public int qty;
	}
	#endregion

	#region public API
	/*
	required:
		0. build the collections required.
		1. get wShopItems for a given category.
		2. boolean (are all items locked for a given category) ?

		3. add to shop cart collection for a given wShopItem (id it already exist try alter its qty by +1)
		4. remove ....
		5. try alter  the qty for given CartItem
		6. get total cost of cart
		7. remove all cart items.

		#. snapshot of all collections
	*/

	public void BuildCategories(List<SO_ShopCategory> CATEGORY)
	{
		this.CATEGORY = CATEGORY;
		DOC__category_wShopItem = new Dictionary<SO_ShopCategory, List<WShopItem>>();
		foreach (var category in CATEGORY)
			DOC__category_wShopItem[category] = category.ITEM_DEF.map(def => new WShopItem(def)).ToList();
	}
	public List<SO_ShopCategory> GetCategories() => CATEGORY;
	public List<WShopItem> GetWShopItems(SO_ShopCategory category) => DOC__category_wShopItem[category];
	public bool IsAllWShopItemsLocked(SO_ShopCategory category)
	{
		return DOC__category_wShopItem[category]
				.all(wShopItem => wShopItem.isLockedCurr);
	}

	public List<CartItem> GetCartItems() => CARTITEM;
	public bool TryAddNewCartItem(WShopItem wShopItem)
	{
		// TODO, can add only if can still afford.
		var existing = CARTITEM.find(ci => ci.wShopItem == wShopItem);
		if (existing != null)
		{
			existing.qty += 1;
			return false;
		}
		CartItem cartItem = new CartItem() { wShopItem = wShopItem, qty = 1 };
		CARTITEM.Add(cartItem);
		return true;
	}
	public void RemoveCartItem(CartItem cartItem)
	{
		CARTITEM.Remove(cartItem);
	}
	public void AlterCartItem(CartItem cartItem, int dQty = 1)
	{
		cartItem.qty += dQty;
		if (cartItem.qty <= 0)
			CARTITEM.Remove(cartItem);
	}

	public void ClearCartItems() => CARTITEM.Clear();

	public float GetCartTotalPrice()
	{
		float sum = 0;
		CARTITEM.ForEach(cartItem =>
		{
			sum += cartItem.wShopItem.itemDef.defaultPrice * cartItem.qty;
		});
		return sum;
	}
	public bool CanAffordCartItems(bool useCustomMoney = false, float customMoney = 1000f)
	{
		if (useCustomMoney == true)
			return GetCartTotalPrice() <= customMoney;
		else
			return GetCartTotalPrice() <= Singleton<EconomyManager>.Ins.GetMoney();
	}
	#endregion

	#region snapShot
	public string GetSnapShot()
	{
		return $@"
// CATEGORY
{PhaseALOG.LIST_CATEGORY__TO__JSON(CATEGORY)}
// DOC__category_wShopItem
{PhaseALOG.DOC_CATEGORY_ITEM__TO__JSON(DOC__category_wShopItem)}
// CARTITEM
{PhaseALOG.LIST_CARTITEM__TO__JSON(CARTITEM)}";
	}
	#endregion
}