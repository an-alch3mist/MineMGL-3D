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
	public float GetMoney() => this._defaultMoney;
	public void AddMoney(float deltaMoney)
	{
		currMoney += deltaMoney;
		GameEvents.RaiseMoneyChanged(currMoney);
	}
	public bool CanAfford(float price)
	{
		return price <= currMoney;
	}
	#endregion

	#region Unity Life Cycle
	protected override void Awake()
	{
		base.Awake();
		Debug.Log(C.method(this));
		currMoney = this._defaultMoney;
	} 
	#endregion
}