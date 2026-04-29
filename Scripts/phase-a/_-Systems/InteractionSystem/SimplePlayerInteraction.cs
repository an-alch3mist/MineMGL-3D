using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

using SPACE_UTIL;

public class SimplePlayerInteraction : MonoBehaviour
{
	[SerializeField] Camera _cam;
	[SerializeField] float _range = 4f;
	[SerializeField] LayerMask _interactionMask;

	#region Unity Life Cycle
	private void Update()
	{
		if (INPUT.K.InstantDown(KeyCode.E))
			TryInteract();
	} 

	void TryInteract()
	{
		if (Singleton<UIManager>.Ins.GetIsAnyMenuOpen())
			return;

		Ray ray = new Ray(this._cam.transform.position, this._cam.transform.forward);
		if(Physics.Raycast(ray, out RaycastHit hit, this._range, this._interactionMask))
		{
			var interactable = hit.collider.gameObject.GetComponent<IInteractable>();
			if(interactable == null)
			{
				Debug.Log($"no interactable on {hit.collider.gameObject.name}".colorTag("red"));
				return;
			}
			GameEvents.RaiseOpenInteractionView(interactable);
		}
	}
	#endregion
}
