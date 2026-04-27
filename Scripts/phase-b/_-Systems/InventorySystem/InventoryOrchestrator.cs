using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

using SPACE_UTIL;

/// <summary>
/// I'm the brain of the inventory system. InventoryUI hands me a DataService on Init — I then
/// Instantiate Field_InventorySlot prefabs, attach UIEventRelay for drag-drop, subscribe to
/// OnToolPickupRequested. In Update I route hotbar keys, scroll, and G=drop.
/// I store IInventoryItem only — zero references to BaseHeldTool or any tool class.
/// </summary>
[AddComponentMenu("MineMGL/Inventory/InventoryOrchestrator")]
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
	[Header("Selected Item Info")]
	[SerializeField] Field_SelectedItemInfo _selectedItemInfo;
	#endregion

	#region private API
	InventoryDataService dataService;
	List<Field_InventorySlot> FIELD_SLOT = new List<Field_InventorySlot>();
	IInventoryItem previousActiveItem;
	IInventoryItem selectedItem;
	int dragFromIndex = -1;

	void BuildSlotFields()
	{
		_hotbarContainer.destroyLeaves();
		_extendedContainer.destroyLeaves();
		FIELD_SLOT.Clear();
		for (int i = 0; i < dataService.GetTotalSize(); i++)
		{
			var slot = dataService.GetAllSlots()[i];
			Transform parent = slot.isHotBar ? _hotbarContainer : _extendedContainer;
			var field = Instantiate(_pfInventorySlot, parent).gc<Field_InventorySlot>();
			field.SetEmpty();
			field.SetIsHotbar(slot.isHotBar);
			var relay = field.gameObject.AddComponent<UIEventRelay>();
			relay.Index = i;
			relay.onBeginDrag = HandleBeginDrag;
			relay.onDrag = HandleDrag;
			relay.onEndDrag = HandleEndDrag;
			relay.onDrop = HandleDrop;
			relay.onPointerEnter = (r) => FIELD_SLOT[r.Index].SetHovered(true);
			relay.onPointerExit = (r) =>
			{
				int ai = dataService.GetActiveSlotIndex();
				FIELD_SLOT[r.Index].SetHighlighted(r.Index == ai, r.Index == ai && dataService.GetActiveItem() != null);
			};
			relay.onPointerDown = (r) => HandleSlotClick(r.Index);
			FIELD_SLOT.Add(field);
		}
	}
	void RefreshAllSlots()
	{
		int ai = dataService.GetActiveSlotIndex();
		for (int i = 0; i < FIELD_SLOT.Count; i++)
		{
			var slot = dataService.GetAllSlots()[i];
			if (slot.item != null)
			{
				FIELD_SLOT[i].SetData(slot.item.GetIcon(), slot.item.GetName(), slot.item.GetQty());
				FIELD_SLOT[i].SetHighlighted(i == ai, i == ai && previousActiveItem != null);
			}
			else FIELD_SLOT[i].SetEmpty();
		}
	}
	#endregion

	#region private API — pickup / equip / drop
	void HandleItemPickup(IInventoryItem item)
	{
		if (dataService.GetIndexFor(item) >= 0) return;
		int idx = dataService.TryAdd(item, -1);
		if (idx == -1) { Debug.Log("inventory full".colorTag("red")); return; }
		item.GetGameObject().SetActive(false);
		if (item.GetShouldEquipWhenPickedUp() && idx < dataService.GetHotbarSize())
			SwitchToSlot(idx);
		GameEvents.RaiseItemPickedUp(item);
		RefreshAllSlots();
	}
	void SwitchToSlot(int index)
	{
		if (previousActiveItem != null)
			previousActiveItem.GetGameObject().SetActive(false);
		var slot = dataService.GetAllSlots()[index];
		if (slot.item == previousActiveItem && slot.item != null)
		{
			previousActiveItem.GetGameObject().SetActive(false);
			previousActiveItem = null;
			dataService.SwitchTo(index);
			RefreshAllSlots();
			GameEvents.RaiseToolSwitched(index);
			return;
		}
		dataService.SwitchTo(index);
		previousActiveItem = dataService.GetActiveItem();
		if (previousActiveItem != null)
		{
			previousActiveItem.GetGameObject().SetActive(true);
			previousActiveItem.OnEquipped();
		}
		GameEvents.RaiseToolSwitched(index);
		RefreshAllSlots();
	}
	void HandleDropActiveItem()
	{
		var item = dataService.GetActiveItem();
		if (item == null) return;
		item.DropItem();
		dataService.Remove(item);
		previousActiveItem = null;
		GameEvents.RaiseItemDropped(item);
		RefreshAllSlots();
	}
	void HandleSlotClick(int index)
	{
		SwitchToSlot(index);
		UpdateSelectedItemInfo(dataService.GetAllSlots()[index].item);
	}
	#endregion

	#region private API — drag-drop
	void HandleBeginDrag(UIEventRelay relay, PointerEventData e)
	{
		var slot = dataService.GetAllSlots()[relay.Index];
		if (slot.item == null) return;
		dragFromIndex = relay.Index;
		FIELD_SLOT[relay.Index].SetDragVisible(false);
		_dragGhostIcon.SetActive(true);
		_dragGhostImage.sprite = slot.item.GetIcon();
		_dragGhostImage.raycastTarget = false;
		_dragGhostAmountText.text = slot.item.GetQty() > 1 ? $"x{slot.item.GetQty()}" : "";
		_dragGhostIcon.transform.SetAsLastSibling();
	}
	void HandleDrag(PointerEventData e) => _dragGhostIcon.transform.position = e.position;
	void HandleEndDrag(UIEventRelay relay, PointerEventData e)
	{
		FIELD_SLOT[relay.Index].SetDragVisible(true);
		_dragGhostIcon.SetActive(false);
		if (e.pointerEnter == null && dragFromIndex >= 0)
		{
			var slot = dataService.GetAllSlots()[dragFromIndex];
			if (slot.item != null) { slot.item.DropItem(); dataService.Remove(slot.item); }
		}
		dragFromIndex = -1;
		RefreshAllSlots();
	}
	void HandleDrop(UIEventRelay relay, PointerEventData e)
	{
		if (dragFromIndex < 0 || dragFromIndex == relay.Index) return;
		dataService.Swap(dragFromIndex, relay.Index);
		RefreshAllSlots();
	}
	#endregion

	#region private API — selected item info
	void UpdateSelectedItemInfo(IInventoryItem item)
	{
		selectedItem = item;
		if (item == null) { _selectedItemInfo.gameObject.SetActive(false); return; }
		_selectedItemInfo.gameObject.SetActive(true);
		_selectedItemInfo.SetData(
			sprite: item.GetIcon(), name: item.GetName(), descr: item.GetDescription(),
			count: item.GetQty() > 1 ? item.GetQty().ToString() : "",
			equipText: item.GetEquipButtonLabel()
		);
	}
	void EquipSelectedItem()
	{
		if (selectedItem == null) return;
		int idx = dataService.GetIndexFor(selectedItem);
		if (idx >= 0) SwitchToSlot(idx);
		GameEvents.RaiseCloseInventoryView();
	}
	void DropSelectedItem()
	{
		if (selectedItem == null) return;
		selectedItem.DropItem();
		dataService.Remove(selectedItem);
		GameEvents.RaiseItemDropped(selectedItem);
		UpdateSelectedItemInfo(null);
		RefreshAllSlots();
	}
	#endregion

	#region public API
	public void Init(InventoryDataService dataService)
	{
		this.dataService = dataService;
		this.dataService.Build();
		BuildSlotFields();
		RefreshAllSlots();
		GameEvents.OnToolPickupRequested += HandleItemPickup;
		_selectedItemInfo._equipButton.onClick.AddListener(() => EquipSelectedItem());
		_selectedItemInfo._dropButton.onClick.AddListener(() => DropSelectedItem());
		UpdateSelectedItemInfo(null);
	}
	public InventoryDataService GetDataServiceForTest() => dataService;
	#endregion

	#region Unity Life Cycle
	private void Update()
	{
		if (Singleton<UIManager>.Ins.GetIsAnyMenuOpen()) return;
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
		float scroll = Input.GetAxis("Mouse ScrollWheel");
		if (scroll != 0f) { dataService.Scroll(scroll > 0f ? 1 : -1); SwitchToSlot(dataService.GetActiveSlotIndex()); }
		dataService.GetActiveItem()?.HandleActiveInput();
		if (INPUT.K.InstantDown(KeyCode.G)) HandleDropActiveItem();
	}
	private void OnDestroy() => GameEvents.OnToolPickupRequested -= HandleItemPickup;
	#endregion
}