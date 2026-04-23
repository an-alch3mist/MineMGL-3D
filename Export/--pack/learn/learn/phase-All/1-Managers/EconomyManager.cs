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
[DefaultExecutionOrder(-100)]
public class EconomyManager : Singleton<EconomyManager>
{
	[SerializeField] float _defaultMoney = 400f;

	#region private API
	float money = 0f;
	#endregion

	#region public API
	public float GetMoney() => money;
	public void AddMoney(float deltaMoney)
	{
		money += deltaMoney; GameEvents.RaiseMoneyChanged(money);
	}
	public bool CanAfford(float price)
	{
		return price <= money;
	}
	#endregion

	#region extra
	// Phase G: SetMoney for save/load
	// public void SetMoney(float value) { money = value; GameEvents.RaiseMoneyChanged(money); }
	#endregion

	#region Unity Life Cycle
	protected override void Awake()
	{
		Debug.Log(C.method(this));
		base.Awake();
		money = this._defaultMoney;
	}
	#endregion
}