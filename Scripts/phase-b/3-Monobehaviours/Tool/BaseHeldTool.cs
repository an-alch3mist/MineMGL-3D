using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using SPACE_UTIL;

public class BaseHeldTool : MonoBehaviour, IInteractable, ISaveLoadableObject
{
	public PlayerController owner;

	public string GetObjectName()
	{
		throw new System.NotImplementedException();
	}
	public List<SO_InteractionOption> GetOptions()
	{
		throw new System.NotImplementedException();
	}
	public void Interact(SO_InteractionOption selectedInteraction)
	{
		throw new System.NotImplementedException();
	}
	public bool ShouldUseInteractionWheel()
	{
		throw new System.NotImplementedException();
	}

	public bool hasBeenSaved { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
	public string GetCustomDataJsonSnapshot()
	{
		throw new System.NotImplementedException();
	}
	public Vector3 GetPos()
	{
		throw new System.NotImplementedException();
	}
	public Vector3 GetRot()
	{
		throw new System.NotImplementedException();
	}
	public SavableObjectID GetSavableObjectTypeId()
	{
		throw new System.NotImplementedException();
	}
	public void LoadFromSave(string customDataJson)
	{
		throw new System.NotImplementedException();
	}
	public bool ShouldBeSaved()
	{
		throw new System.NotImplementedException();
	}

	public int GetMaxAmount() { return -1; }
	public int GetToolType() { return -1; }
	public int GetQty() { return -1; }
	public string GetName() { return $"";  }
	int qty = 0;
	public void AddQty(int count) { qty += count; }
}
