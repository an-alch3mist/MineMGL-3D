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
/// toggle interaction wheel ui panel.
/// </summary>
[DefaultExecutionOrder(1)] // occurs before its orchestrator 
public class InteractionWheelUI : Singleton<InteractionWheelUI>
{
	List<SO_InteractionOption> INTERACTION;
	[SerializeField] InteractionWheelOrchestrator _orchestrator;
	#region Unity Life 
	bool isFirstEnable = true;
	private void OnEnable()
	{
		Debug.Log(C.method(this));
		if (isFirstEnable)
		{
			GameEvents.OnOpenInteractionView += (obj) =>
			{
				this.gameObject.SetActive(true);
				this._orchestrator.Init(obj);
			};
			GameEvents.OnCloseInteractionView += () => this.gameObject.SetActive(false);
			//
			this.gameObject.SetActive(false); // deactivate once setup complete
			isFirstEnable = false;
		}
		GameEvents.RaiseMenuStateChanged(isAnyMenuOpen: true); // for cursorLock purpose
	}
	private void Update()
	{
		if (INPUT.K.InstantDown(KeyCode.Escape))
		{
			Debug.Log("toggle interactionWheelUI false");
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
