using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// packed building crate on the ground — Take creates ToolBuilder + fires RaiseBuildingTakeRequested.
/// Shows icon + quantity text on world-space canvas.
/// </summary>
public class BuildingCrate : BaseSellableItem, IInteractable
{
	#region Inspector Fields
	[SerializeField] SavableObjectID _savableObjectID;
	[SerializeField] List<SO_InteractionOption> _interactions;
	#endregion

	#region private API
	SO_BuildingInventoryDefinition definition;
	int quantity = 1;
	#endregion

	#region public API
	public SO_BuildingInventoryDefinition GetDefinition() => definition;
	public void SetDefinition(SO_BuildingInventoryDefinition def) => definition = def;
	public int GetQuantity() => quantity;
	public void SetQuantity(int qty) => quantity = qty;

	/// <summary> creates ToolBuilder from definition, fires pickup event, destroys crate </summary>
	public void TryAddToInventory()
	{
		if (definition == null) { Debug.LogWarning("BuildingCrate missing Definition!"); return; }
		// purpose: InventoryOrchestrator creates ToolBuilder from this definition
		GameEvents.RaiseBuildingTakeRequested(definition, quantity);
		Destroy(gameObject);
	}
	public override float GetSellValue()
	{
		// sell at 90% of buy price × quantity
		return Singleton<EconomyManager>.Ins.GetMoney() * 0f; // Phase A: needs price lookup from ShopDataService
	}
	#endregion

	#region public API — IInteractable
	public bool ShouldUseInteractionWheel() => false;
	public List<SO_InteractionOption> GetOptions() => _interactions;
	public string GetObjectName() => definition?.Name ?? "Crate";
	public void Interact(SO_InteractionOption selectedOption)
	{
		if (selectedOption.interactionType == InteractionType.Take) TryAddToInventory();
		else if (selectedOption.interactionType == InteractionType.Destroy) Destroy(gameObject);
	}
	#endregion

	#region Unity Life Cycle
	private void Start()
	{
		if (definition == null || definition.GetIcon() == null) return;
		foreach (var img in GetComponentsInChildren<Image>()) img.sprite = definition.GetIcon();
		foreach (var txt in GetComponentsInChildren<TMP_Text>())
			txt.text = quantity > 1 ? $"x{quantity}" : "";
	}
	#endregion

	#region extra
	// Phase G: ISaveLoadableObject stubs — LoadFromSave, GetCustomSaveData
	#endregion
}