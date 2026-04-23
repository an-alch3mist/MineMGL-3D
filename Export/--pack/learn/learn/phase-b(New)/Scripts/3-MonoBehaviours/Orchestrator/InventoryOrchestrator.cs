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
/// I'm the brain of the inventory system. InventoryUI hands me a DataService on Init — I then
/// Instantiate 40 Field_InventorySlot prefabs (10 hotbar + 30 extended), attach UIEventRelay to
/// each for drag-drop, and subscribe to OnToolPickupRequested so I can add tools to slots.
/// In Update, I route hotbar keys (1-0), scroll wheel, and tool actions (LMB/RMB/R/Q/G) to the
/// active tool. I also manage drag-drop between slots (swap), drop-outside-UI (tool drops to world),
/// and the selected item info panel (click slot → show name/icon/desc → equip/drop buttons).
/// I have zero reference to PlayerMovement — tool.Owner is set via GameEvents.OnToolEquipped by PlayerGrab.
///
/// Who uses me: InventoryUI (Init), GameEvents (OnToolPickupRequested).
/// Events I fire: RaiseToolEquipped, RaiseToolSwitched, RaiseItemPickedUp, RaiseItemDropped, RaiseCloseInventoryView.
/// </summary>
public class InventoryOrchestrator : MonoBehaviour
{
	#region Inspector Fields
	[SerializeField] Transform _hotbarContainer;
	[SerializeField] Transform _extendedContainer;
	[SerializeField] GameObject _pfInventorySlot;
	[Header("Drag Ghost")]
	[SerializeField] GameObject _dragGhostIcon;
	[SerializeField] Image _dragGhostImage;
	[SerializeField] TMP_Text _dragGhostAmountText;
	[Header("Selected Item Info Panel")]
	[SerializeField] GameObject _selectedItemInfo;
	[SerializeField] TMP_Text _selectedItemNameText;
	[SerializeField] TMP_Text _selectedItemDescText;
	[SerializeField] TMP_Text _selectedItemAmountText;
	[SerializeField] Image _selectedItemIcon;
	[SerializeField] TMP_Text _equipButtonText;
	[SerializeField] Button _equipButton;
	[SerializeField] Button _dropButton;
	#endregion

	#region private API
	InventoryDataService dataService;
	List<Field_InventorySlot> FIELD_SLOT = new List<Field_InventorySlot>();
	List<UIEventRelay> RELAY_SLOT = new List<UIEventRelay>();
	BaseHeldTool previousActiveTool;
	int dragFromIndex = -1;

	/// <summary> Destroys any existing slot children, then Instantiates 40 Field_InventorySlot
	/// prefabs (10 under hotbar, 30 under extended). Attaches UIEventRelay to each for drag-drop
	/// and wires all Action callbacks (beginDrag, drag, endDrag, drop, pointerDown). </summary>
	void BuildSlotFields()
	{
		_hotbarContainer.destroyLeaves();
		_extendedContainer.destroyLeaves();
		FIELD_SLOT.Clear();
		for (int i = 0; i < dataService.GetTotalSize(); i++)
		{
			var slot = dataService.GetSlots()[i];
			Transform parent = slot.IsHotbar ? _hotbarContainer : _extendedContainer;
			var field = GameObject.Instantiate(_pfInventorySlot, parent).gc<Field_InventorySlot>();
			field.SetEmpty();
			field.SetIsHotbar(slot.IsHotbar);
			var relay = field.gameObject.AddComponent<UIEventRelay>();
			relay.Index = i;
			relay.onBeginDrag = HandleBeginDrag;
			relay.onDrag = HandleDrag;
			relay.onEndDrag = HandleEndDrag;
			relay.onDrop = HandleDrop;
			relay.onPointerEnter = (r) => FIELD_SLOT[r.Index].SetHovered(true);
			relay.onPointerExit = (r) => FIELD_SLOT[r.Index].SetHighlighted(r.Index == dataService.ActiveSlotIndex);
			relay.onPointerDown = (r) => UpdateSelectedItemInfo(dataService.GetSlots()[r.Index].Tool);
			FIELD_SLOT.Add(field);
			RELAY_SLOT.Add(relay);
		}
	}
	/// <summary> Loops through all 40 slots and updates each Field_ to match the DataService state —
	/// shows icon+name+amount for occupied slots, SetEmpty for empty, highlights the active slot. </summary>
	void RefreshAllSlots()
	{
		for (int i = 0; i < FIELD_SLOT.Count; i++)
		{
			var slot = dataService.GetSlots()[i];
			if (slot.Tool != null)
			{
				Sprite icon = (slot.Tool is IIconItem iconItem) ? iconItem.GetIcon() : null;
				FIELD_SLOT[i].SetData(icon, slot.Tool.Name, slot.Tool.Quantity);
				FIELD_SLOT[i].SetHighlighted(i == dataService.ActiveSlotIndex);
			}
			else
			{
				FIELD_SLOT[i].SetEmpty();
			}
		}
	}
	/// <summary> Called via OnToolPickupRequested when a tool’s “Take” interaction fires. Adds the
	/// tool to the first empty slot via DataService, deactivates it, equips if EquipWhenPickedUp,
	/// fires OnItemPickedUp, and refreshes all slot displays. </summary>
	void HandleToolPickup(BaseHeldTool tool)
	{
		int idx = dataService.TryAdd(tool, -1);
		if (idx == -1) { Debug.Log("inventory full".colorTag("red")); return; }
		tool.gameObject.SetActive(false);
		if (tool.EquipWhenPickedUp && idx < dataService.GetHotbarSize())
			SwitchToSlot(idx);
		// purpose: inform other systems that tool was picked up
		GameEvents.RaiseItemPickedUp(tool);
		RefreshAllSlots();
	}
	/// <summary> Deactivates the previous tool’s GO, activates the new tool’s GO (which triggers
	/// OnEnable → parents to ViewModelContainer), fires RaiseToolEquipped (PlayerGrab sets Owner),
	/// fires RaiseToolSwitched, and refreshes all slot highlights. </summary>
	void SwitchToSlot(int index)
	{
		if (previousActiveTool != null) previousActiveTool.gameObject.SetActive(false);
		var slot = dataService.GetSlots()[index];
		if (slot.Tool == previousActiveTool && slot.Tool != null)
		{
			previousActiveTool.gameObject.SetActive(false);
			previousActiveTool = null;
			dataService.SwitchTo(index);
			RefreshAllSlots();
			return;
		}
		dataService.SwitchTo(index);
		previousActiveTool = dataService.ActiveTool;
		if (previousActiveTool != null)
		{
			previousActiveTool.gameObject.SetActive(true);
			// purpose: PlayerGrab sets tool.Owner = PlayerMovement via this event
			GameEvents.RaiseToolEquipped(previousActiveTool);
		}
		// purpose: notify FresnelHighlighter and other systems about tool change
		GameEvents.RaiseToolSwitched(index);
		RefreshAllSlots();
	}
	/// <summary> Gets the active tool from DataService, calls DropItem (shows WorldModel, applies
	/// forward velocity), removes from DataService, fires OnItemDropped, refreshes slots. </summary>
	void HandleDropActiveTool()
	{
		BaseHeldTool tool = dataService.ActiveTool;
		if (tool == null) return;
		tool.DropItem();
		dataService.Remove(tool);
		previousActiveTool = null;
		// purpose: inform other systems that tool was dropped
		GameEvents.RaiseItemDropped(tool);
		RefreshAllSlots();
	}
	#endregion

	#region private API — drag-drop
	/// <summary> Records which slot the drag started from, hides the slot’s content (SetDragVisible
	/// false), shows the DragGhostIcon with the tool’s sprite following the cursor. </summary>
	void HandleBeginDrag(UIEventRelay relay, UnityEngine.EventSystems.PointerEventData e)
	{
		var slot = dataService.GetSlots()[relay.Index];
		if (slot.Tool == null) return;
		dragFromIndex = relay.Index;
		FIELD_SLOT[relay.Index].SetDragVisible(false);
		_dragGhostIcon.SetActive(true);
		_dragGhostImage.sprite = (slot.Tool is IIconItem icon) ? icon.GetIcon() : null;
		_dragGhostAmountText.text = slot.Tool.Quantity > 1 ? slot.Tool.Quantity.ToString() : "";
		_dragGhostIcon.transform.SetAsLastSibling();
	}
	/// <summary> Every frame during drag — moves the ghost icon to follow the cursor position. </summary>
	void HandleDrag(UnityEngine.EventSystems.PointerEventData e)
	{
		_dragGhostIcon.transform.position = e.position;
	}
	/// <summary> Drag ended — restores the source slot’s visibility, hides the ghost. If dropped
	/// outside any UI slot (e.pointerEnter == null), drops the tool into the world instead. </summary>
	void HandleEndDrag(UIEventRelay relay, UnityEngine.EventSystems.PointerEventData e)
	{
		FIELD_SLOT[relay.Index].SetDragVisible(true);
		_dragGhostIcon.SetActive(false);
		if (e.pointerEnter == null && dragFromIndex >= 0)
		{
			// dropped outside UI → drop the item
			var slot = dataService.GetSlots()[dragFromIndex];
			if (slot.Tool != null) { slot.Tool.DropItem(); dataService.Remove(slot.Tool); }
		}
		dragFromIndex = -1;
		RefreshAllSlots();
	}
	/// <summary> Drop landed on a target slot — swaps the dragged tool with whatever is in the
	/// target slot via DataService.Swap, then refreshes all displays. </summary>
	void HandleDrop(UIEventRelay relay, UnityEngine.EventSystems.PointerEventData e)
	{
		if (dragFromIndex < 0 || dragFromIndex == relay.Index) return;
		dataService.Swap(dragFromIndex, relay.Index);
		RefreshAllSlots();
	}
	#endregion

	#region extra
	// nice-to-have: selected item info panel — click a slot in extended inventory to see name/desc/icon + equip/drop buttons
	BaseHeldTool selectedTool;
	/// <summary> Shows or hides the selected item info panel. If tool is not null, displays its
	/// name, description, icon, amount, and sets the equip button text (“Equip” or “Build”). </summary>
	void UpdateSelectedItemInfo(BaseHeldTool tool)
	{
		selectedTool = tool;
		if (tool == null) { _selectedItemInfo.SetActive(false); return; }
		_selectedItemInfo.SetActive(true);
		_selectedItemNameText.text = tool.Name;
		_selectedItemDescText.text = tool.Description;
		_selectedItemAmountText.text = tool.Quantity > 1 ? tool.Quantity.ToString() : "";
		_selectedItemIcon.sprite = (tool is IIconItem icon) ? icon.GetIcon() : null;
		_equipButtonText.text = (tool is ToolBuilder) ? "Build" : "Equip";
	}
	/// <summary> Equips the currently selected tool by switching to its slot index, then fires
	/// RaiseCloseInventoryView so the inventory panel closes after equipping. </summary>
	void EquipSelectedTool()
	{
		if (selectedTool == null) return;
		int idx = dataService.GetIndexFor(selectedTool);
		if (idx >= 0) SwitchToSlot(idx);
		// purpose: close inventory after equipping
		GameEvents.RaiseCloseInventoryView();
	}
	/// <summary> Drops the currently selected tool from the info panel — calls DropItem on the
	/// tool, removes it from DataService, fires OnItemDropped, and hides the info panel. </summary>
	void DropSelectedTool()
	{
		if (selectedTool == null) return;
		selectedTool.DropItem();
		dataService.Remove(selectedTool);
		// purpose: inform other systems tool dropped
		GameEvents.RaiseItemDropped(selectedTool);
		UpdateSelectedItemInfo(null);
		RefreshAllSlots();
	}
	#endregion

	#region public API
	/// <summary> init from SubManager </summary>
	public void Init(InventoryDataService dataService)
	{
		this.dataService = dataService;
		this.dataService.Build();
		BuildSlotFields();
		RefreshAllSlots();
		SubscribeEvents();
		if (_equipButton != null) _equipButton.onClick.AddListener(() => EquipSelectedTool());
		if (_dropButton != null) _dropButton.onClick.AddListener(() => DropSelectedTool());
		if (_selectedItemInfo != null) UpdateSelectedItemInfo(null);
	}
	/// <summary> expose dataService for test snapshot </summary>
	public InventoryDataService GetDataServiceForTest() => dataService;
	#endregion

	#region private API — subscriptions
	/// <summary> Subscribes to OnToolPickupRequested (so tools can be added to inventory) and
	/// OnMoneyChanged (triggers RefreshAllSlots to update any money-dependent displays). </summary>
	void SubscribeEvents()
	{
		// purpose: InventoryOrchestrator adds this tool to hotbar
		GameEvents.OnToolPickupRequested += HandleToolPickup;
		// purpose: refresh money display on hotbar (cart total, etc.)
		GameEvents.OnMoneyChanged += (money) => RefreshAllSlots();
	}
	#endregion

	#region Unity Life Cycle
	/// <summary> Every frame (when no menu open): routes hotbar keys 1-0, scroll wheel, and
	/// tool action keys (LMB/RMB/R/Q/G) to the active tool’s virtual methods. </summary>
	private void Update()
	{
		// → skip all input when any menu is open
		if (Singleton<UIManager>.Ins.isAnyMenuOpen) return;
		// hotbar keys 1-0
		if (INPUT.K.InstantDown(KeyCode.Alpha1)) SwitchToSlot(0);
		else if (INPUT.K.InstantDown(KeyCode.Alpha2)) SwitchToSlot(1);
		else if (INPUT.K.InstantDown(KeyCode.Alpha3)) SwitchToSlot(2);
		else if (INPUT.K.InstantDown(KeyCode.Alpha4)) SwitchToSlot(3);
		else if (INPUT.K.InstantDown(KeyCode.Alpha5)) SwitchToSlot(4);
		else if (INPUT.K.InstantDown(KeyCode.Alpha6)) SwitchToSlot(5);
		else if (INPUT.K.InstantDown(KeyCode.Alpha7)) SwitchToSlot(6);
		else if (INPUT.K.InstantDown(KeyCode.Alpha8)) SwitchToSlot(7);
		else if (INPUT.K.InstantDown(KeyCode.Alpha9)) SwitchToSlot(8);
		else if (INPUT.K.InstantDown(KeyCode.Alpha0)) SwitchToSlot(9);

		// scroll wheel
		float scroll = Input.GetAxis("Mouse ScrollWheel");
		if (scroll != 0f) { dataService.Scroll(scroll > 0f ? 1 : -1); SwitchToSlot(dataService.ActiveSlotIndex); }

		// tool actions
		BaseHeldTool active = dataService.ActiveTool;
		if (active != null)
		{
			if (INPUT.K.InstantDown(KeyCode.Mouse0)) active.PrimaryFire();
			if (Input.GetMouseButton(0)) active.PrimaryFireHeld();
			if (INPUT.K.InstantDown(KeyCode.Mouse1)) active.SecondaryFire();
			if (Input.GetMouseButton(1)) active.SecondaryFireHeld();
			if (INPUT.K.InstantDown(KeyCode.R)) active.Reload();
			if (INPUT.K.InstantDown(KeyCode.Q)) active.QButtonPressed();
			if (INPUT.K.InstantDown(KeyCode.G)) HandleDropActiveTool();
		}
	}
	/// <summary> Unsubscribes from OnToolPickupRequested to prevent null ref on scene unload. </summary>
	private void OnDestroy()
	{
		GameEvents.OnToolPickupRequested -= HandleToolPickup;
	}
	#endregion
}