using System.Collections;
using UnityEngine;
using TMPro;

using SPACE_UTIL;

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
	private void OnDisable()
	{
		GameEvents.OnMoneyChanged -= HandleMoneyChanged;
	}
}
