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
	[Header("Drop")]
	[SerializeField] Camera _cam;
	[SerializeField] float _dropHeightOffset = 1f;
	[SerializeField] float _dropDistance = 2f;
	#endregion

	#region private API
	InventoryDataService dataService;
	List<Field_InventorySlot> FIELD_SLOT = new List<Field_InventorySlot>();
	IInventoryItem previousActiveItem;
	IInventoryItem selectedItem;
	int dragFromIndex = -1;
	bool dragPreviewActive;

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
		int activeBeforeAdd = dataService.GetActiveSlotIndex();
		int idx = dataService.TryAdd(item, -1);
		if (idx == -1) { Debug.Log("inventory full".colorTag("red")); return; }
		item.GetGameObject().SetActive(false);
		// → only switch if: equip flag set + hotbar slot + NOT the already-active slot (avoids toggle-off on stack)
		if (item.GetShouldEquipWhenPickedUp() && idx < dataService.GetHotbarSize() && idx != activeBeforeAdd)
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
		// → stacked: drop 1, keep rest in slot
		// Note: with real tools (main source), stacking only applies to ToolBuilder which instantiates
		// a new BuildingCrate GO on drop. MockTool has only 1 GO so we can't show a physical drop
		// for qty > 1. The count decrements visually in the slot.
		if (item.GetQty() > 1)
		{
			item.AddQty(-1);
			item.DropOneFromStack(_cam);
			RefreshAllSlots();
			return;
		}
		// → last one: drop the actual GO into the world
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
	/// <summary> During drag: move ghost. If outside UI → show 3D model preview at cursor ray.
	/// If back over UI → hide model. Drop only happens on EndDrag release. </summary>
	void HandleDrag(PointerEventData e)
	{
		_dragGhostIcon.transform.position = e.position;
		if (dragFromIndex < 0) return;
		var slot = dataService.GetAllSlots()[dragFromIndex];
		if (slot.item == null) return;
		var go = slot.item.GetGameObject();
		bool outsideUI = e.pointerCurrentRaycast.gameObject == null;
		if (outsideUI && _cam != null)
		{
			// → show 3D model at cursor ray position (preview before drop)
			if (!dragPreviewActive)
			{
				go.SetActive(true);
				var rb = go.GetComponent<Rigidbody>();
				if (rb != null) rb.isKinematic = true;
				dragPreviewActive = true;
			}
			Ray ray = _cam.ScreenPointToRay(e.position);
			go.transform.position = ray.GetPoint(2f);
			go.transform.rotation = Quaternion.LookRotation(ray.direction);
		}
		else if (dragPreviewActive)
		{
			// → dragged back over UI → hide model
			go.SetActive(false);
			dragPreviewActive = false;
		}
	}
	/// <summary> EndDrag: if outside UI → drop item at current position. If inside → cancel preview. </summary>
	void HandleEndDrag(UIEventRelay relay, PointerEventData e)
	{
		FIELD_SLOT[relay.Index].SetDragVisible(true);
		_dragGhostIcon.SetActive(false);
		if (e.pointerEnter == null && dragFromIndex >= 0)
		{
			var slot = dataService.GetAllSlots()[dragFromIndex];
			if (slot.item != null)
			{
				// → calculate drop position: ray from cursor + height offset (prevents below-ground drops)
				Vector3 dropPos = Vector3.zero;
				Vector3 dropDir = Vector3.down;
				if (_cam != null)
				{
					Ray ray = _cam.ScreenPointToRay(e.position);
					dropPos = ray.GetPoint(_dropDistance) + Vector3.up * _dropHeightOffset;
					dropDir = ray.direction;
				}

				if (slot.item.GetQty() > 1)
				{
					// → stacked: drop 1 clone at cursor position, keep rest
					slot.item.AddQty(-1);
					slot.item.DropOneFromStack(_cam);
					slot.item.GetGameObject().SetActive(false);
				}
				else
				{
					// → last one: drop the actual GO at cursor ray position + height offset
					var go = slot.item.GetGameObject();
					go.SetActive(true);
					go.transform.position = dropPos;
					var col = go.GetComponent<Collider>();
					if (col != null) col.enabled = true;
					var rb = go.GetComponent<Rigidbody>();
					if (rb != null)
					{
						rb.isKinematic = false;
						rb.linearVelocity = dropDir * 2f;
					}
					slot.item.DropItem();
					dataService.Remove(slot.item);
				}
			}
		}
		else if (dragPreviewActive)
		{
			// → dragged back onto UI → cancel, hide model
			var slot = dataService.GetAllSlots()[dragFromIndex];
			if (slot.item != null) slot.item.GetGameObject().SetActive(false);
		}
		dragPreviewActive = false;
		dragFromIndex = -1;
		RefreshAllSlots();
	}
	/// <summary> Drop on target slot: merge if stackable, swap otherwise. </summary>
	void HandleDrop(UIEventRelay relay, PointerEventData e)
	{
		if (dragFromIndex < 0 || dragFromIndex == relay.Index) return;
		// → try merge first (drag Dynamite x2 onto Dynamite x1 → Dynamite x3)
		if (dataService.CanStack(dragFromIndex, relay.Index))
		{
			dataService.TryMerge(dragFromIndex, relay.Index);
		}
		else
		{
			dataService.Swap(dragFromIndex, relay.Index);
		}
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
		// → stacked: drop 1, keep rest in slot, update info panel count
		if (selectedItem.GetQty() > 1)
		{
			selectedItem.AddQty(-1);
			selectedItem.DropOneFromStack(_cam);
			UpdateSelectedItemInfo(selectedItem);
			RefreshAllSlots();
			return;
		}
		// → last one: drop the actual item
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