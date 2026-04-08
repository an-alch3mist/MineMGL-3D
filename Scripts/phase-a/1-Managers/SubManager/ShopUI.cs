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
/// toggle shop ui panel
/// </summary>
public class ShopUI : MonoBehaviour
{
	[SerializeField] List<SO_ShopCategory> _CATEGORY;
	[SerializeField] ShopUIOrchestrator _orchestor;

	#region private API
	ShopDataService shopDataService = new ShopDataService();
	public ShopDataService GetDataServiceForTest() => shopDataService;
	#endregion

	#region Unity Life 
	private void OnEnable()
	{
		Debug.Log(C.method(this));
		GameEvents.RaiseMenuStateChanged(isAnyMenuOpen: true); // for cursorLock purpose
	}
	private void Start()
	{
		Debug.Log(C.method(this, adMssg: "all data service build, orchestor is done here"));
		shopDataService.BuildCategories(this._CATEGORY);
		this._orchestor.Init(shopDataService, this._CATEGORY); // link the shopDataService and the LIST<category> into orchestor
		this._orchestor.BuildCategoryView();

		GameEvents.OnOpenShopView += () => this.gameObject.SetActive(true);
		this.gameObject.SetActive(false);
	}
	private void Update()
	{
		if (INPUT.K.InstantDown(KeyCode.E) || INPUT.K.InstantDown(KeyCode.Escape))
		{
			Debug.Log("toggle false");
			// .toggle is in namespace SPACE_UTIL as extension behaves same as .SetActive();
			this.gameObject.toggle(value: false);
		}
	}
	private void OnDisable()
	{
		Debug.Log(C.method(this, "orange"));
		GameEvents.RaiseMenuStateChanged(isAnyMenuOpen: false);  // for cursorLock purpose
	}
	#endregion
}
