using UnityEngine;
using SPACE_UTIL;

/// <summary>
/// stimulates menu open close via keyInstantDown
/// to check weather controller handles paused during menu Open, and resumes when menu closed.
/// </summary>
public class PlayerControllerTest : MonoBehaviour
{
	bool isFirstEnable = true;
	private void OnEnable()
	{
		if (isFirstEnable)
		{
			GameEvents.OnMenuStateChanged += (isAnyMenuOpen) => Debug.Log("menu open check for playerControllerTest".colorTag("lime"));
			isFirstEnable = false;
		}
	}
	private void Update()
	{
		if (INPUT.K.InstantDown(KeyCode.M))
			GameEvents.RaiseMenuStateChanged(isAnyMenuOpen: true);
		if (INPUT.K.InstantDown(KeyCode.N))
			GameEvents.RaiseMenuStateChanged(isAnyMenuOpen: false);
	}
}
