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
	[SerializeField] GameObject _pfButton; 
	#endregion

	#region public API
	public void BuildInteractionsView(List<SO_Interaction> INTERACTION, GameObject obj)
	{
		this._container.destroyLeaves();
		foreach(var interaction in INTERACTION)
		{

		}
	}
	#endregion
}
