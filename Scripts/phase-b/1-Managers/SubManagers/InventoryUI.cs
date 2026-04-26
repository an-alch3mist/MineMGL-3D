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
/// I'm the SubManager for the inventory panel. I don't touch any data — I just open and close.
/// On first enable, I create an InventoryDataService, hand it to my Orchestrator via Init(),
/// subscribe to Open/Close events, then immediately disable myself (so the panel starts hidden).
/// After that, whenever UIManager fires RaiseOpenInventoryView (Tab key), I activate — which
/// triggers OnEnable → RaiseMenuStateChanged(true) → cursor unlocks, player input freezes.
/// Tab or ESC in my Update fires RaiseCloseInventoryView → I deactivate → OnDisable → cursor locks.
///
/// Who uses me: UIManager (Tab → RaiseOpenInventoryView), self (ESC/Tab → close).
/// Who I talk to: InventoryOrchestrator (Init), GameEvents (menu state).
/// </summary>
public class InventoryUI : MonoBehaviour
{
	#region Inspector Fields
	[SerializeField] InventoryOrchestrator _orchestrator;
	#endregion

	#region private API
	InventoryDataService dataService = new InventoryDataService();
	#endregion

	#region Unity Life Cycle
	/// <summary> First enable: creates DataService, inits Orchestrator, subscribes to Open/Close,
	/// then self-disables (panel starts hidden). Subsequent enables: fires MenuStateChanged(true)
	/// which unlocks cursor and freezes player input. </summary>
	bool isFirstEnable = true;
	private void OnEnable()
	{
		Debug.Log(C.method(this));
		if (isFirstEnable)
		{
			Debug.Log("InventoryUI first time enabled".colorTag("lime"));
			// → hand DataService to Orchestrator which builds 40 slot Field_ instances + wires drag-drop
			_orchestrator.Init(dataService);
			// purpose: InventoryUI self-activates when inventory view is opened
			GameEvents.OnOpenInventoryView += () => this.gameObject.SetActive(true);
			// purpose: InventoryUI self-deactivates when inventory view is closed
			GameEvents.OnCloseInventoryView += () => this.gameObject.SetActive(false);
			// → self-disable so panel starts hidden — no false menu pulse
			this.gameObject.SetActive(false);
			isFirstEnable = false;
			return;
		}
		// purpose: cursor lock/unlock for player controller
		// → tells PlayerMovement to unlock cursor + freeze WASD, tells UIManager isAnyMenuOpen = true
		GameEvents.RaiseMenuStateChanged(isAnyMenuOpen: true);
	}
	/// <summary> ESC or Tab while open → fires close event which deactivates this panel. </summary>
	private void Update()
	{
		if (INPUT.K.InstantDown(KeyCode.Escape) || INPUT.K.InstantDown(KeyCode.Tab))
		{
			// purpose: close inventory panel → triggers OnDisable → RaiseMenuStateChanged(false) → cursor locks
			GameEvents.RaiseCloseInventoryView();
		}
	}
	/// <summary> Fires MenuStateChanged(false) which re-locks cursor and re-enables player input. </summary>
	private void OnDisable()
	{
		Debug.Log(C.method(this, "orange"));
		// purpose: cursor lock/unlock for player controller
		// → tells PlayerMovement to re-lock cursor + re-enable WASD, tells UIManager isAnyMenuOpen = false
		GameEvents.RaiseMenuStateChanged(isAnyMenuOpen: false);
	}
	#endregion
}
