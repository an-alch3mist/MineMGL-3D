using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using SPACE_UTIL;

public interface IInteractable
{
	string GetObjectName();
	bool ShouldUseInteractionWheel();
	List<SO_Interaction> GetInteractions();

	void Interact(SO_Interaction selectedInteraction);
}
