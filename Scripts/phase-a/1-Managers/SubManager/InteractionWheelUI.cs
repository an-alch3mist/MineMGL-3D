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
public class InteractionWheelUI : Singleton<InteractionWheelUI>
{
	#region Unity Life 
	bool isFirstEnable = true;
	private void OnEnable()
	{
		Debug.Log(C.method(this));
		if (isFirstEnable)
		{
			// selfActive Subscribed by Itself At Start >>
			GameEvents.OnOpenInteractionView += (interactable) =>
			{
				this.gameObject.SetActive(true);
				ErazeAndBuildOptionsView(interactable.GetOptions(), interactable);
			};
			GameEvents.OnCloseInteractionView += () => this.gameObject.SetActive(false);
			// << selfActive/Inactive Subscribed by Itself At Start
			this.gameObject.SetActive(false); // deactivate once setup complete
			isFirstEnable = false;
			return;
		}
		GameEvents.RaiseMenuStateChanged(isAnyMenuOpen: true); // for cursorLock purpose
	}
	private void Update()
	{
		if (INPUT.K.InstantDown(KeyCode.Escape) || INPUT.K.InstantDown(KeyCode.E))
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

	#region Orchestrator(Since its just one tab)
	#region inspector fields
	[Header("orchestrator")]
	[SerializeField] Transform _container;
	[SerializeField] GameObject _pfInteractionOption;
	#endregion
	#region private API
	void ErazeAndBuildOptionsView(List<SO_InteractionOption> OPTION, IInteractable interactable)
	{
		if(interactable.ShouldUseInteractionWheel() == false)
		{
			interactable.Interact(OPTION[0]);
			return;
		}
		this._container.destroyLeaves();
		foreach (var option in OPTION)
		{
			var field = GameObject.Instantiate(this._pfInteractionOption, this._container)
									.gc<Field_InteractionOption>();
			field.SetData(option.interactionName, option.sprite);
			field._button.onClick.AddListener(() =>
			{
				interactable.Interact(option);
			});
		}
	}
	#endregion
	#endregion
}