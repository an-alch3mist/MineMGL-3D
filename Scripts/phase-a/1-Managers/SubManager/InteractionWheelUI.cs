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
	List<SO_Interaction> INTERACTION;
	[SerializeField] InteractionWheelOrchestrator _orchestrator;
	#region Unity Life 
	private void OnEnable()
	{
		Debug.Log(C.method(this));
		GameEvents.RaiseMenuStateChanged(isAnyMenuOpen: true); // for cursorLock purpose
	}
	private void Awake()
	{
		Debug.Log(C.method(this, adMssg: "all data service build, orchestor is done here"));

		// no init() for InteractionOrchestrator.Init() as ShopUIOrchestrator.Init()

		GameEvents.OnOpenInteractionView += (allInteractions, obj) =>
		{
			this.gameObject.SetActive(true);
			this._orchestrator.BuildInteractionsView(allInteractions, obj);
		};
		GameEvents.OnCloseInteractionView+= () => this.gameObject.SetActive(false);
		//
		this.gameObject.SetActive(false); // deactivate once setup complete
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
