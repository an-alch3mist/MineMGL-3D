using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

using SPACE_UTIL;

public class UIManager : Singleton<UIManager>
{
	[SerializeField] ShopUI shopUI;
	[SerializeField] InteractionWheelUI interactionUI;

	#region public API
	public bool isAnyMenuOpen { get; private set; }
	public void CloseAllSubManager()
	{
		GameEvents.RaiseCloseShopView();
		GameEvents.RaiseCloseInteractionView();
	}
	#endregion
	#region Unity Life Cycle
	private void Start()
	{
		GameEvents.OnMenuStateChanged += HandleMenuSateChanged;
	}
	private void OnDestroy()
	{
		GameEvents.OnMenuStateChanged -= HandleMenuSateChanged;
	}
	void HandleMenuSateChanged(bool isAnyMenuOpen) => this.isAnyMenuOpen = isAnyMenuOpen; 
	#endregion
}
