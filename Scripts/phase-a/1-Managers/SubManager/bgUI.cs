using UnityEngine;

/// <summary>
/// toggle bgUI
/// </summary>
public class bgUI : MonoBehaviour
{
	bool isFirstEnable = true;
	private void OnEnable()
	{
		if(isFirstEnable)
		{
			GameEvents.OnMenuStateChanged += (isAnyMenuOpen) => this.gameObject.SetActive(isAnyMenuOpen);
			isFirstEnable = false;
		}
	}
}
