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
/// owns money
/// </summary>
[DefaultExecutionOrder(-100)] // after INITMAnager, if any of monoBehaviour[in curr game]
public class EconomyManager : Singleton<EconomyManager>
{
	[SerializeField] float _defaultMoney = 400f;
	float currMoney;
	#region public API
	public float GetMoney() => currMoney;
	public void AddMoney(float deltaMoney)
	{
		currMoney += deltaMoney;
	}
	public bool CanAfford(float price)
	{
		return price <= currMoney;
	}
	#endregion

	#region Unity Life Cycle
	// after moneyUI subscribed in onFIrstEnable
	private void Start()
	{
		currMoney = this._defaultMoney;
		// raise or notify in start or further in unity life cycle.
		GameEvents.RaiseMoneyChanged(money: currMoney);
	}
	#endregion
}

public static class MoneyExtension
{
	#region extension
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
	#endregion
}