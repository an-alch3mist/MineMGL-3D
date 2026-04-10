using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using SPACE_UTIL;

public class InteractableComputer : MonoBehaviour, IInteractable
{
	[SerializeField] List<SO_InteractionOption> _OPTION;

	public string GetObjectName()
	{
		return this.gameObject.name;
	}
	public bool ShouldUseInteractionWheel()
	{
		return true;
	}
	public List<SO_InteractionOption> GetInteractions()
	{
		return this._OPTION;
	}
	public void Interact(SO_InteractionOption selectedInteraction)
	{
		// TODO, check type of interation made through enum (possibly).
		GameEvents.RaiseCloseInteractionView();
		GameEvents.RaiseOpenShopView();
		// TODO use UIManager to monitor and close UI menus on priority or close all, using GameEvents or Singleton<>.
	}
}
