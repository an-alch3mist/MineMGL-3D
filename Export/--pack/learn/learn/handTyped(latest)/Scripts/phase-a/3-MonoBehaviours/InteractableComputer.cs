using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using SPACE_UTIL;

public class InteractableComputer : MonoBehaviour, IInteractable
{
	[SerializeField] List<SO_InteractionOption> _OPTION;
	[SerializeField] bool _selectFirstOptionDefault = true;

	public string GetObjectName()
	{
		return this.gameObject.name;
	}
	public bool ShouldUseInteractionWheel()
	{
		return this._selectFirstOptionDefault == false;
	}
	public List<SO_InteractionOption> GetOptions()
	{
		return this._OPTION;
	}
	public void Interact(SO_InteractionOption selectedInteraction)
	{
		// Debug.Log($"should perform {selectedInteraction.interactionName}".colorTag("lime"));
		if(selectedInteraction.interactionName == "openShopView")
		{
			GameEvents.RaiseOpenShopView();
		}
	}
}