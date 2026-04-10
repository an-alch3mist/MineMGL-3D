using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

using SPACE_UTIL;

public static class UtilsPhaseA
{
	/// <summary>
	/// eg: 12,345.00
	/// </summary>
	/// <param name="money"></param>
	/// <returns></returns>
	public static string formatMoney(this float money) => $"${money:#,##0.00}";
	/// <summary>
	/// eg: 12,345
	/// </summary>
	/// <param name="money"></param>
	/// <returns></returns>
	public static string formatMoneyShort(this float money) => $"${money:#,##0.##}";

	public static bool TrySpawnAtPoint(SO_ShopItemDef itemDef, int qty)
	{
		var point = ShopSpawnPoint.GetRandomSpawnPoint();
		for(int i0 = 0; i0 < qty; i0 += 1)
		{
			Vector3 offset = C.RandomInsideUnitSphere(); offset.y = offset.y.abs();
			GameObject.Instantiate(itemDef.pfObject).transform.position = point + offset * 0.3f;
		}
		return true;
	}
}
