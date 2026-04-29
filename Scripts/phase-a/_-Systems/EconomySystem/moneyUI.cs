using System.Collections;
using UnityEngine;
using TMPro;

using SPACE_UTIL;

/// <summary>
/// toggle UI + events on enable/disable handled here, reamining dynamic button or inputField events inside orchestrator
/// reads data from DataService
/// </summary>
public class moneyUI : MonoBehaviour
{
	[SerializeField] TextMeshProUGUI _moneyText;

	void HandleMoneyChanged(float money)
	{
		Debug.Log("money changed gotta alter the UI".colorTag("lime"));
		EconomyManager economyManager = Singleton<EconomyManager>.Ins;
		this._moneyText.text = economyManager.GetMoney().formatMoney();
	}
	bool isFirstEnable = true;
	private void OnEnable()
	{
		if(isFirstEnable == true)
		{
			// do somthng
			GameEvents.OnMoneyChanged += HandleMoneyChanged;
			isFirstEnable = false;
		}
	}
	/*
	private void OnDisable() // this also include when scene is being switched or Application.Quit(), or exist GameMode in Editor.
	{
		// GameEvents.OnMoneyChanged -= HandleMoneyChanged;
	}
	*/
}
