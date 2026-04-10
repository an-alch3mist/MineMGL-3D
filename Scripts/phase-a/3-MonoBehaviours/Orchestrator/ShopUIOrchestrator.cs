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
/// create wires UI listeners, instantiate + destory of prefabs, += when to RefreshAll
/// reads data from DataService
/// </summary>
[DefaultExecutionOrder(2)]
public class ShopUIOrchestrator : MonoBehaviour
{
	#region Inspector Fields
	[SerializeField] Transform _categoryContainer, _shopItemContainer, _cartItemContainer;

	[SerializeField] GameObject _pfCategory, _pfShopItem, _pfCartItem;

	[SerializeField] TextMeshProUGUI  _cartTotalPriceText;
	[SerializeField] Button _purchaseButton;

	[SerializeField]
	Color
		_canAffordColor = Color.limeGreen,
		_cannotAffordColor = Color.red * 0.8f,
		_selectedTabColor = new Color(0.3f, 0.6f, 1f),
		_normalTabColor = new Color(0.2f, 0.2f, 0.2f),
		_canBuyColor = Color.limeGreen,
		_cannotBuyColor = Color.red * 0.8f;
	#endregion

	#region private API
	EconomyManager economyManager => Singleton<EconomyManager>.Ins;
	ShopDataService shopDataService;
	List<SO_ShopCategory> CATEGORY;
	SO_ShopCategory selectedCategory;

	// to deselect all category every time a certain category is selected in view.
	Dictionary<SO_ShopCategory, Field_ShopCategory> DOC__Category__Field = new Dictionary<SO_ShopCategory, Field_ShopCategory>();
	// when add to cart from wShopItem is made, the linked field view should update
	Dictionary<ShopDataService.CartItem, Field_ShopCartItem> DOC__CartItem__Field = new Dictionary<ShopDataService.CartItem, Field_ShopCartItem>();

	void SelectCategoryView(SO_ShopCategory category)
	{
		selectedCategory = category;
		DOC__Category__Field.forEach(kvp =>
		{
			kvp.Value.SetSelected(kvp.Key == category, this._normalTabColor, this._selectedTabColor);
		});
		RepopulateShopItemsView();
	}
	/*
	// btw here is destroy leaves in SPACE_UTIL namespace as extension.

		// destroy leaves in descending order
		public static void destroyLeaves(this GameObject gameObject)
		{
			Transform transform = gameObject.transform;
			for (int i = transform.childCount - 1; i >= 0; i -= 1)
				GameObject.Destroy(transform.GetChild(i).gameObject);
		}
		public static void destroyLeaves(this Transform transform) => transform.gameObject.destroyLeaves();
	*/
	void RepopulateShopItemsView()
	{
		this._shopItemContainer.destroyLeaves();

		foreach(var wShopItem in shopDataService.GetWShopItems(selectedCategory))
		{
			// gc is an extension replacement for functionality same as GetComponenet<> inside SPACE_UTIL namespace.
			var fieldWShopItem = GameObject.Instantiate(this._pfShopItem, this._shopItemContainer).gc<Field_ShopItem>();
			fieldWShopItem.SetData(wShopItem.itemDef.itemDefName, wShopItem.itemDef.descr, wShopItem.itemDef.defaultPrice.formatMoneyShort(), "add to cart", wShopItem.itemDef.sprite);
			fieldWShopItem.SetButtonInteractable(wShopItem.isLockedCurr == false, "add to cart" , this._canBuyColor, _cannotBuyColor);

			// when add to cart was pressed
			fieldWShopItem._addToCartButton.onClick.AddListener(() =>
			{
				CreateAndOrchestrateCartItemFields(wShopItem);
				RefreshAllRequired();
			});
		}
	}
	void CreateAndOrchestrateCartItemFields(WShopItem wShopItem)
	{
		// create >>
		var cartItem = shopDataService.TryAddNewCartItem(wShopItem);
		//
		if (DOC__CartItem__Field.ContainsKey(cartItem)) { DOC__CartItem__Field[cartItem].SetQty(cartItem.qty); return; }
		var fieldCartItem = GameObject.Instantiate(_pfCartItem, _cartItemContainer).gc<Field_ShopCartItem>();
		fieldCartItem.SetData(
			cartItem.wShopItem.itemDef.itemDefName,
			cartItem.wShopItem.itemDef.descr,
			cartItem.wShopItem.itemDef.sprite);
		fieldCartItem.SetPrice(cartItem.wShopItem.itemDef.defaultPrice);
		fieldCartItem.SetQty(1);
		DOC__CartItem__Field[cartItem] = fieldCartItem;
		// << create

		// Orchestrate >>
		fieldCartItem._qtyInputField.onEndEdit.AddListener(str =>
		{
			Debug.Log($"input field was edited by submit");
			shopDataService.AlterCartItemQty(cartItem, str.parseInt());
			RefreshAllRequired();
		});

		fieldCartItem._removeButton.onClick.AddListener(() =>
		{
			shopDataService.RemoveCartItem(cartItem);
			GameObject.Destroy(DOC__CartItem__Field[cartItem].gameObject);
			DOC__CartItem__Field.Remove(cartItem);
			RefreshAllRequired();
		});

		fieldCartItem._addButton.onClick.AddListener(() =>
		{
			shopDataService.IncreaseCartItemQty(cartItem, +1);
			fieldCartItem._qtyInputField.text = cartItem.qty.ToString();
			RefreshAllRequired();
		});
		fieldCartItem._subButton.onClick.AddListener(() =>
		{
			shopDataService.IncreaseCartItemQty(cartItem, -1);
			fieldCartItem._qtyInputField.text = cartItem.qty.ToString();
			RefreshAllRequired();
		});
		// << Orchestrate
	}
	

	void PurchaseAllCartItems()
	{
		var CART_ITEM = DOC__CartItem__Field.map(kvp => kvp.Key).ToList(); // the .ToList() performs copy operation
		// var CART_ITEM = DOC__CartItem__Field.map(kvp => kvp.Key) /// -> error if tryna remove from for loopwhile iterating through it without .ToList(), copy.
		// reason: foreach breaks because it uses an IEnumerator. The enumerator checks if the collection's "version" changed after every MoveNext(). If you Add/Remove, version changes → InvalidOperationException.

		foreach (var cartItem in CART_ITEM)
		{
			var field = DOC__CartItem__Field[cartItem];
			float cost = cartItem.wShopItem.itemDef.defaultPrice * cartItem.qty;

			UtilsPhaseA.TrySpawnAtPoint(cartItem.wShopItem.itemDef, cartItem.qty);
			// remove
			shopDataService.RemoveCartItem(cartItem);
			GameObject.Destroy(DOC__CartItem__Field[cartItem].gameObject);
			DOC__CartItem__Field.Remove(cartItem);
			// update money and everything related to it.
			economyManager.AddMoney(deltaMoney: -cost);
			GameEvents.RaiseMoneyChanged(economyManager.GetMoney());
		}
		RefreshAllRequired();
		GameEvents.RaiseCloseShopView();
	}
	#endregion

	#region public API
	public void Init(ShopDataService shopDataService, List<SO_ShopCategory> CATEGORY)
	{
		this.shopDataService = shopDataService;
		this.CATEGORY = CATEGORY;
	}
	//
	public void BuildAndOrchestrateCategoryView()
	{
		this._categoryContainer.destroyLeaves();
		DOC__Category__Field.Clear();

		CATEGORY.forEach(category =>
		{
			var field = GameObject.Instantiate(this._pfCategory, this._categoryContainer).gc<Field_ShopCategory>();
			field.SetNameText(str: category.categoryName);

			DOC__Category__Field[category] = field;
			// improve: the .button include that inside field
			field.button.onClick.AddListener(() =>
			{
				SelectCategoryView(category);
				//
				if (shopDataService.shouldCategoryBeHiddenInView(category) == true)
					field.gameObject.toggle(value: false);
			});
		});
		// improve: firstUnlocked
		var firstUnlockedCategory = CATEGORY.find(category => shopDataService.shouldCategoryBeHiddenInView(category) == false);
		if (firstUnlockedCategory != null)
			SelectCategoryView(firstUnlockedCategory);
	}
	public void OrchestratePurchaseButton()
	{
		this._purchaseButton.onClick.AddListener(() => PurchaseAllCartItems());
	}
	public void RefreshAllRequired()
	{
		this._cartTotalPriceText.text = shopDataService.GetCartTotalPrice().formatMoney();
		this._cartTotalPriceText.color = (shopDataService.CanAffordCartItems()) ? this._canAffordColor : this._cannotAffordColor;
		this._purchaseButton.interactable = shopDataService.CanAffordCartItems();
	}
	#endregion

	/*
	#region Unity Life Cycle
	private void OnEnable()
	{
		GameEvents.OnMoneyChanged += HandleMoneyChanged;
		RefreshAllRequired();
	}
	private void OnDisable()
	{
		GameEvents.OnMoneyChanged -= HandleMoneyChanged;
	}
	#endregion
	*/
}