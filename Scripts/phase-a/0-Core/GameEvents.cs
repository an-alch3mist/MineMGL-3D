using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

using SPACE_UTIL;

public static class GameEvents
{
	// when Somthng occured >>
	public static event Action<int> OnSomthng;
	public static void RaiseSomthng(int index)
	{
		LogSubscribersCount(nameof(OnSomthng), OnSomthng);
		GameEvents.OnSomthng? // if there is any subscribers
			.Invoke(index);
	}
	// << when Somthng occured

	// when money changed >>
	public static event Action<float> OnMoneyChanged;
	public static void RaiseMoneyChanged(float money)
	{
		LogSubscribersCount(nameof(OnMoneyChanged), OnMoneyChanged);
		GameEvents.OnMoneyChanged? // if there is any subscribers
			.Invoke(money);
	}
	// << when money changed

	// when a certain menu state changed to enabled/disabled >>
	public static event Action<bool> OnMenuStateChanged;
	public static void RaiseMenuStateChanged(bool isAnyMenuOpen)
	{
		LogSubscribersCount(nameof(OnMenuStateChanged), OnMenuStateChanged);
		GameEvents.OnMenuStateChanged? // if there is any subscribers
			.Invoke(isAnyMenuOpen);
	}
	// << when a certain menu state changed to enabled/disabled

	// when shop view is toggled >>
	public static event Action OnOpenShopView;
	public static void RaiseOpenShopView()
	{
		LogSubscribersCount(nameof(OnOpenShopView), OnOpenShopView);
		GameEvents.OnOpenShopView? // if there is any subscribers
			.Invoke();
	}
	// <<  when shop view is toggled
	// when shop view is toggled >>
	public static event Action OnCloseShopView;
	public static void RaiseCloseShopView()
	{
		LogSubscribersCount(nameof(OnCloseShopView), OnCloseShopView);
		GameEvents.OnCloseShopView? // if there is any subscribers
			.Invoke();
	}
	// <<  when shop view is toggled

	// when interaction view is toggled >>
	public static event Action<List<SO_Interaction>> OnOpenInteractionView;
	public static void RaiseOpenInteractionView(List<SO_Interaction> INTERACTION)
	{
		LogSubscribersCount(nameof(OnOpenInteractionView), OnOpenInteractionView);
		GameEvents.OnOpenInteractionView? // if there is any subscribers
			.Invoke(INTERACTION);
	}
	// <<  when shop view is toggled
	// when shop view is toggled >>
	public static event Action OnCloseInteractionView;
	public static void RaiseCloseInteractionView()
	{
		LogSubscribersCount(nameof(OnCloseInteractionView), OnCloseInteractionView);
		GameEvents.OnCloseInteractionView? // if there is any subscribers
			.Invoke();
	}
	// <<  when shop view is toggled

	static void LogSubscribersCount(string name, Delegate anEvent)
	{
		int subsCount = anEvent?.GetInvocationList().Length ?? 0;
		UnityEngine.Debug.Log($"[GamEvents] {name} raised for -> {subsCount} subscribers".colorTag("lime"));
	}
}
