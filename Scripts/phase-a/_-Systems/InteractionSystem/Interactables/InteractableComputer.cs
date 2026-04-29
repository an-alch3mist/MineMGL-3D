using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using SPACE_UTIL;
using HighlightPlus;

public class InteractableComputer : MonoBehaviour, IInteractable, IHighlightable
{
	[SerializeField] List<SO_InteractionOption> _OPTION;
	[SerializeField] bool _selectFirstOptionDefault = true;
	[Header("Highlight")]
	[SerializeField] HighlightProfile _highlightProfile;

	#region public API - IInteractable
	public string GetObjectName() => this.gameObject.name;
	public bool ShouldUseInteractionWheel() => this._selectFirstOptionDefault == false;
	public List<SO_InteractionOption> GetOptions() => this._OPTION;
	public void Interact(SO_InteractionOption selectedInteraction)
	{
		// Debug.Log($"should perform {selectedInteraction.interactionName}".colorTag("lime"));
		if (selectedInteraction.interactionType == InteractionType.openShopView)
			GameEvents.RaiseOpenShopView();
	}
	#endregion

	#region public API - IIHighlight
	public HighlightProfile GetHighlightProfile()
	{
		return this._highlightProfile;
	} 
	#endregion
}
