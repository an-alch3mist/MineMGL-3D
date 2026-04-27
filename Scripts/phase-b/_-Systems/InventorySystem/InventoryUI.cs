using UnityEngine;

using SPACE_UTIL;

/// <summary>
/// SubManager for the inventory panel. First enable: creates DataService, inits Orchestrator,
/// subscribes to Open/Close events, self-disables. Tab opens, ESC/Tab closes.
/// ⚠️ CRITICAL: HotbarPanel must be a SIBLING, NOT a child.
/// </summary>
[AddComponentMenu("MineMGL/Inventory/InventoryUI")]
public class InventoryUI : MonoBehaviour
{
	[SerializeField] InventoryOrchestrator _orchestrator;

	InventoryDataService dataService = new InventoryDataService();

	bool isFirstEnable = true;
	private void OnEnable()
	{
		if (isFirstEnable)
		{
			_orchestrator.Init(dataService);
			GameEvents.OnOpenInventoryView += () => gameObject.SetActive(true);
			GameEvents.OnCloseInventoryView += () => gameObject.SetActive(false);
			gameObject.SetActive(false);
			isFirstEnable = false;
			return;
		}
		GameEvents.RaiseMenuStateChanged(isAnyMenuOpen: true);
	}
	private void Update()
	{
		if (INPUT.K.InstantDown(KeyCode.Escape) || INPUT.K.InstantDown(KeyCode.Tab))
			GameEvents.RaiseCloseInventoryView();
	}
	private void OnDisable()
	{
		GameEvents.RaiseMenuStateChanged(isAnyMenuOpen: false);
	}
}