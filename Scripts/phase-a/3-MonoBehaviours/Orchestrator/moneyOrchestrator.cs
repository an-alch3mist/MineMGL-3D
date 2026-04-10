using System.Collections;
using UnityEngine;
using TMPro;

using SPACE_UTIL;

/// <summary>
/// create wires UI listeners, instantiate + destory of prefabs, += when to RefreshAll
/// reads data from DataService
/// </summary>
public class moneyOrchestrator : Singleton<moneyOrchestrator>
{
	[SerializeField] TextMeshProUGUI _moneyText;

	void HandleMoneyChanged(float money)
	{
		EconomyManager economyManager = Singleton<EconomyManager>.Ins;
		_moneyText.SetText(economyManager.GetMoney().formatMoney());
	}
	private void OnEnable()
	{
		GameEvents.OnMoneyChanged += HandleMoneyChanged;

	}
	private void OnDisable() // this also include when scene is being switched or Application.Quit(), or exist GameMode in Editor.
	{
		GameEvents.OnMoneyChanged -= HandleMoneyChanged;
	}
}
