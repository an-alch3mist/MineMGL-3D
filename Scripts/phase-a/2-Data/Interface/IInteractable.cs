using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using SPACE_UTIL;

public interface IInteractable
{
	string GetObjectName();
	bool ShouldUseInteractionWheel();
	List<SO_InteractionOption> GetOptions();

	void Interact(SO_InteractionOption selectedInteraction);
}
