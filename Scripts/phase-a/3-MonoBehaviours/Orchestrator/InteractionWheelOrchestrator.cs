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
/// create wires UI listeners, instantiate + destory of prefabs, += when to RefreshAll
/// reads data from DataService
/// </summary>
[DefaultExecutionOrder(2)] // after ....UI.Awake() is done
public class InteractionWheelOrchestrator : MonoBehaviour
{
	#region inspector fields
	[SerializeField] Transform _container;
	[SerializeField] GameObject _pfInteractionOption;
	#endregion

	#region private API

	void ErazeAndBuildOptionsView(List<SO_InteractionOption> OPTION, IInteractable interactable)
	{
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

	#region public API
	public void Init(GameObject obj)
	{
		ErazeAndBuildOptionsView(
			OPTION: obj.GetComponent<IInteractable>().GetInteractions(), 
			interactable: obj.GetComponent<IInteractable>()
		);
	}
	#endregion
}
