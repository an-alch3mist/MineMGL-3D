using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using SPACE_UTIL;

public class DEBUG_Check : MonoBehaviour
{
	private void Start()
	{
		StopAllCoroutines();
		StartCoroutine(STIMULATE());
	}

	IEnumerator STIMULATE()
	{
		while(true)
		{
			if (INPUT.M.InstantDown(0))
			{
				// checkExtensionIEnumerable();
				checkShopDataService();
			}
			yield return null;
		}

		yield return null;
	}

	[Header("checkExtensionIEnumerable")]
	[SerializeField] List<bool> _LIST = new List<bool>()
	{
		true, false, true, false
	};
	void checkExtensionIEnumerable()
	{
		Debug.Log(C.method(this));
		Debug.Log($".all: {this._LIST.all(elem => elem)}".colorTag("lime"));
		Debug.Log($".any: {this._LIST.any(elem => elem)}".colorTag("green"));
	}

	[Header("checkShopDataService")]
	[SerializeField] List<SO_ShopCategory> _CATEGORY;
	void checkShopDataService()
	{
		ShopDataService dataService = new ShopDataService();
		// build collections
		dataService.BuildCategories(this._CATEGORY);
		// CARTITEM collection operations
		dataService.TryAddNewCartItem(dataService.GetWShopItems(this._CATEGORY[0]).getRandom());
		dataService.TryAddNewCartItem(dataService.GetWShopItems(this._CATEGORY[0]).getRandom());
		dataService.TryAddNewCartItem(dataService.GetWShopItems(this._CATEGORY[0]).getRandom());
		dataService.TryAddNewCartItem(dataService.GetWShopItems(this._CATEGORY[0]).getRandom());
		dataService.TryAddNewCartItem(dataService.GetWShopItems(this._CATEGORY[0]).getRandom());
		dataService.AlterCartItem(dataService.GetCartItems()[0], 100);
		//
		LOG.AddLog(dataService.GetSnapShot(), "json");

		dataService.AlterCartItem(dataService.GetCartItems()[0], -200);
		LOG.AddLog(dataService.GetSnapShot(), "json");

		dataService.AlterCartItem(dataService.GetCartItems()[0], -(dataService.GetCartItems()[0].qty - 1));
		dataService.TryAddNewCartItem(dataService.GetCartItems()[0].wShopItem);
		dataService.TryAddNewCartItem(dataService.GetCartItems()[0].wShopItem);
		dataService.TryAddNewCartItem(dataService.GetCartItems()[0].wShopItem);
		LOG.AddLog(dataService.GetSnapShot(), "json");
	}
}
