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
/// toggle UI + events on enable/disable handled here, reamining dynamic button or inputField events inside orchestrator
/// reads data from DataService
/// </summary>
public class ShopUI : MonoBehaviour
{
	[SerializeField] List<SO_ShopCategory> _CATEGORY;
	[SerializeField] ShopUIOrchestrator _orchestrator;

	#region private API
	ShopDataService shopDataService = new ShopDataService();
	public ShopDataService GetDataServiceForTest() => shopDataService;
	#endregion

	#region Unity Life 
	/*
	==== before ====
	private void OnEnable()
	{
		Debug.Log(C.method(this));
		GameEvents.RaiseMenuStateChanged(isAnyMenuOpen: true); // for cursorLock purpose
	}
	private void Awake()
	{
		Debug.Log(C.method(this, adMssg: "all data service build, orchestor is done here"));
		shopDataService.BuildCategories(this._CATEGORY);
		this._orchestor.Init(shopDataService, this._CATEGORY); // link the shopDataService and the LIST<category> into orchestor
		this._orchestor.BuildAndOrchestrateCategoryView();
		this._orchestor.WirePurchaseButton();

		GameEvents.OnOpenShopView += () => this.gameObject.SetActive(true);
		GameEvents.OnCloseShopView += () => this.gameObject.SetActive(false);
		//
		this.gameObject.SetActive(false); // deactivate once setup
	}
	==== before ====
	*/
	bool isFirstEnable = true;
	private void OnEnable()
	{
		Debug.Log(C.method(this));
		if(isFirstEnable)
		{
			Debug.Log("shopUI first time enabled".colorTag("lime"));
			// ------------------------------------------------------------------------------------------- //
			shopDataService.BuildCategories(this._CATEGORY);
			this._orchestrator.InitBuildOrchestrateAndSubscribe(shopDataService, this._CATEGORY); // link the shopDataService and the LIST<category> into orchestor
			// ------------------------------------------------------------------------------------------- //
			// Self Subscribed to View and disabled after setup, // TODO Unsubscribe on disable() / on destroy()
			GameEvents.OnOpenShopView += () => { Singleton<UIManager>.Ins.CloseAllSubManager(); this.gameObject.SetActive(true); };
			GameEvents.OnCloseShopView += () => this.gameObject.SetActive(false);
			// 
			this.gameObject.SetActive(false); // once all setup done, deactivate;
			isFirstEnable = false;
		}
		GameEvents.RaiseMenuStateChanged(isAnyMenuOpen: true);
	}
	// not required for now
	private void Update()
	{
		/*
		// 
		if (INPUT.K.InstantDown(KeyCode.E) || INPUT.K.InstantDown(KeyCode.Escape))
		{
			Debug.Log("toggle false");
			// .toggle is in namespace SPACE_UTIL as extension behaves same as .SetActive();
			GameEvents.RaiseCloseShopView();
		}
		*/
	}
	private void OnDisable()
	{
		Debug.Log(C.method(this, "orange"));
		GameEvents.RaiseMenuStateChanged(isAnyMenuOpen: false);  // for cursorLock purpose
	}
	#endregion
}