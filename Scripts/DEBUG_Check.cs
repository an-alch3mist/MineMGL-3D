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
	[Header("just to log")]
	[SerializeField] bool isFirstEnable = true;
	private void OnEnable()
	{
		Debug.Log(C.method(this));
		if(isFirstEnable)
		{
			Debug.Log("logic at first enable".colorTag("lime"));
			isFirstEnable = false;
		}
		Debug.Log("logic after firstEnable is performed".colorTag("cyan"));
	}
	private void OnDisable()
	{
		Debug.Log(C.method(this, "orange"));
	}

	IEnumerator STIMULATE()
	{
		while (true)
		{
			if (INPUT.K.HeldDown(KeyCode.LeftAlt) && INPUT.M.InstantDown(0))
			{
				// checkShopDataService();
				checkExtensionIEnumerable();
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
	[SerializeField] List<float> _LIST_FLOAT = new List<float>()
	{
		0, 100, 200, 1.1f, 2.7f,
	};
	void checkExtensionIEnumerable()
	{
		Debug.Log(C.method(this));
		Debug.Log($".all: {this._LIST.all(elem => elem)}".colorTag("lime"));
		Debug.Log($".any: {this._LIST.any(elem => elem)}".colorTag("green"));

		Debug.Log($".sum: {this._LIST_FLOAT.sum(elem => elem)}".colorTag("lime"));
		Debug.Log($".int: {this._LIST_FLOAT.sum(elem => elem.round())}".colorTag("green"));

		Dictionary<int, string> MAP = new Dictionary<int, string>();
		MAP[10] = "somthng";
		MAP.GetOrCreate(100, defaultValue: "default");

		var snapshot = MAP.ToNSJson(pretify: true);
		LOG.H("MAP");
		LOG.AddLog(snapshot, "json");
		LOG.HEnd("MAP");
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
		dataService.IncreaseCartItemQty(dataService.GetCartItems()[0], 100);
		//
		LOG.AddLog(dataService.GetSnapShotForTest(), "json");

		dataService.IncreaseCartItemQty(dataService.GetCartItems()[0], -200);
		LOG.AddLog(dataService.GetSnapShotForTest(), "json");

		dataService.IncreaseCartItemQty(dataService.GetCartItems()[0], -(dataService.GetCartItems()[0].qty - 1));
		dataService.TryAddNewCartItem(dataService.GetCartItems()[0].wShopItem);
		dataService.TryAddNewCartItem(dataService.GetCartItems()[0].wShopItem);
		dataService.TryAddNewCartItem(dataService.GetCartItems()[0].wShopItem);
		LOG.AddLog(dataService.GetSnapShotForTest(), "json");
	}

}
