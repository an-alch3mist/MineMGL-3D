using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

using SPACE_UTIL;

public class ShopUITest : MonoBehaviour
{
	[SerializeField] ShopUI _shopUI; // to access data service for test
	[TextArea(10, 20)]
	string REAME = @"(INPUT.K.InstantDown(KeyCode.Space)) GameEvents.RaiseOpenShopView();
(INPUT.K.InstantDown(KeyCode.U)) economyManager.AddMoney(100f);
(INPUT.K.InstantDown(KeyCode.I)) economyManager.AddMoney(-50f);
(INPUT.K.InstantDown(KeyCode.O)) LOG.AddLog(this._shopUI.GetDataServiceForTest().GetSnapShotForTest(), ""json"");";

	[Header("category")]
	[SerializeField] SO_ShopCategory _category;
	#region Unity Life Cycle
	private void Update()
	{
		EconomyManager economyManager = Singleton<EconomyManager>.Ins;
		if (INPUT.K.InstantDown(KeyCode.Space)) GameEvents.RaiseOpenShopView();
		else if (INPUT.K.InstantDown(KeyCode.U)) economyManager.AddMoney(100f);
		else if (INPUT.K.InstantDown(KeyCode.I)) economyManager.AddMoney(-50f);
		else if (INPUT.K.InstantDown(KeyCode.O)) LOG.AddLog(this._shopUI.GetDataServiceForTest().GetSnapShotForTest(), "json");
		else if (INPUT.K.InstantDown(KeyCode.P)) GameEvents.RaiseUnlockedCategory(this._category);
	}
	#endregion
}
