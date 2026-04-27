using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;

// ═══════════════════════════════════════════════════════════════
// IInventoryItem — interface for anything that sits in a slot
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Contract for anything that can sit in an inventory slot — tools, building crates, future items.
/// InventoryDataService stores IInventoryItem (not BaseHeldTool), so the inventory system is
/// decoupled from the tool hierarchy. A test can create a mock IInventoryItem without needing
/// BaseHeldTool, PlayerMovement, Rigidbody, or any MonoBehaviour.
///
/// Who implements me: BaseHeldTool, BuildingCrate (Phase D), any future storable item.
/// Who uses me: InventoryDataService (InventorySlot.item), InventoryOrchestrator.
/// </summary>
public interface IInventoryItem
{
	string GetName();
	string GetDescription();
	int GetQty();
	void SetQty(int qty);
	void AddQty(int delta);
	int GetMaxAmount();
	bool GetShouldEquipWhenPickedUp();
	Sprite GetIcon();
	GameObject GetGameObject();
	/// <summary> drop this item fully (last one from slot) — show GO, apply velocity </summary>
	void DropItem();
	/// <summary> drop one from a stack — instantiate or spawn a copy in the world.
	/// Default: do nothing (non-stackable items never call this). Override for stackable items
	/// like ToolBuilder which instantiate a BuildingCrate on drop. </summary>
	void DropOneFromStack(Camera cam);
	/// <summary> called when this item becomes the active equipped item — tool fires RaiseToolEquipped,
	/// non-tool items can do nothing or fire their own event </summary>
	void OnEquipped();
	/// <summary> returns the equip button label — "Build" for ToolBuilder, "Equip" for tools, etc. </summary>
	string GetEquipButtonLabel();
	/// <summary> called per-frame when this is the active item — tool routes LMB/RMB/R/Q,
	/// non-tool items can do nothing </summary>
	void HandleActiveInput();
	/// <summary> called by PlayerGrab when this item is equipped — provides camera, viewmodel container,
	/// and magnet position so the item can raycast and parent without knowing PlayerMovement exists </summary>
	void SetOwnerContext(Camera cam, Transform viewModelContainer, Transform magnetToolPos);
}

// ═══════════════════════════════════════════════════════════════
// InventorySlot — pure data entity for one slot
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Pure data — one inventory slot. Holds an IInventoryItem reference (or null) and its index.
/// isHotBar is derived from index vs HOTBAR_SIZE. Since this is pure data (like SO_, Field_),
/// public fields are fine — no Get/Set methods needed.
/// </summary>
public class InventorySlot
{
	public IInventoryItem item;
	public int index;
	public bool isHotBar => index < InventoryDataService.HOTBAR_SIZE;
}

// ═══════════════════════════════════════════════════════════════
// InventoryDataService — pure C# inventory brain
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// I'm the pure C# brain behind the inventory — no Unity dependency, testable via `new`.
/// I manage SLOT objects (HOTBAR_SIZE hotbar + extended). Each slot holds an IInventoryItem or null.
/// Decoupled from BaseHeldTool — any object implementing IInventoryItem can go in a slot.
/// InventoryOrchestrator calls Build() to create slots, then uses TryAdd (stacks or finds empty),
/// Remove, SwitchTo, Scroll (cycles occupied), Swap (drag-drop), GetIndexFor (duplicate guard).
///
/// Who uses me: InventoryOrchestrator (all operations), InventoryTest (snapshot).
/// Events I fire: none. Events I subscribe to: none. Pure data.
/// </summary>
public class InventoryDataService
{
	#region private API
	List<InventorySlot> SLOT = new List<InventorySlot>();
	int activeSlotIndex = 0;
	public static int HOTBAR_SIZE = 5;
	public static int TOTAL_SIZE = 15;
	#endregion

	#region public API
	/// <summary> creates TOTAL_SIZE empty slots (HOTBAR_SIZE hotbar + rest extended) </summary>
	public void Build()
	{
		SLOT.Clear();
		for (int i = 0; i < TOTAL_SIZE; i++)
			SLOT.Add(new InventorySlot { item = null, index = i });
		activeSlotIndex = 0;
	}
	public List<InventorySlot> GetAllSlots() => SLOT;
	public InventorySlot GetActiveSlot() => (activeSlotIndex >= 0 && activeSlotIndex < SLOT.Count) ? SLOT[activeSlotIndex] : null;
	public IInventoryItem GetActiveItem() => GetActiveSlot()?.item;
	public int GetActiveSlotIndex() => activeSlotIndex;
	public int GetHotbarSize() => HOTBAR_SIZE;
	public int GetTotalSize() => TOTAL_SIZE;

	/// <summary> find slot index for an item, or -1 — used as duplicate pickup guard </summary>
	public int GetIndexFor(IInventoryItem item)
	{
		for (int i = 0; i < SLOT.Count; i++)
			if (SLOT[i].item == item) return i;
		return -1;
	}

	/// <summary> Stacks if possible → preferred slot → first empty. Returns slot index or -1.
	/// Stacking scans ALL slots (hotbar + extended). Same type + space → increment qty.
	/// If incoming qty fully absorbed → returns stack target index (incoming GO should be destroyed by caller).
	/// If partial absorb → remainder goes to empty slot. If no space at all → returns -1. </summary>
	public int TryAdd(IInventoryItem item, int preferredSlot = -1)
	{
		// → stacking: scan ALL slots (hotbar + extended) for matching type with space
		if (item.GetMaxAmount() > 1)
		{
			for (int i = 0; i < SLOT.Count; i++)
			{
				if (SLOT[i].item != null
					&& SLOT[i].item.GetType() == item.GetType()
					&& SLOT[i].item.GetName() == item.GetName() // same name too (two ToolBuilders with different defs don't stack)
					&& SLOT[i].item.GetQty() < SLOT[i].item.GetMaxAmount())
				{
					int space = SLOT[i].item.GetMaxAmount() - SLOT[i].item.GetQty();
					int add = (item.GetQty() <= space) ? item.GetQty() : space;
					SLOT[i].item.AddQty(add);
					item.AddQty(-add);
					if (item.GetQty() <= 0) return i; // fully absorbed
				}
			}
		}
		// → remainder (or non-stackable): preferred slot → first empty
		if (item.GetQty() <= 0) return -1; // shouldn't happen but guard
		int target = -1;
		if (preferredSlot >= 0 && preferredSlot < SLOT.Count && SLOT[preferredSlot].item == null)
			target = preferredSlot;
		if (target == -1)
			for (int i = 0; i < SLOT.Count; i++)
				if (SLOT[i].item == null) { target = i; break; }
		if (target == -1) return -1; // full
		SLOT[target].item = item;
		return target;
	}

	/// <summary> Can two items in different slots be merged? Same type + same name + target has space. </summary>
	public bool CanStack(int fromIndex, int toIndex)
	{
		if (fromIndex < 0 || toIndex < 0 || fromIndex >= SLOT.Count || toIndex >= SLOT.Count) return false;
		var from = SLOT[fromIndex].item;
		var to = SLOT[toIndex].item;
		if (from == null || to == null) return false;
		return from.GetType() == to.GetType()
			&& from.GetName() == to.GetName()
			&& to.GetQty() < to.GetMaxAmount();
	}

	/// <summary> Merge from → to (as much as fits). Returns true if from is fully absorbed (caller should remove). </summary>
	public bool TryMerge(int fromIndex, int toIndex)
	{
		if (!CanStack(fromIndex, toIndex)) return false;
		var from = SLOT[fromIndex].item;
		var to = SLOT[toIndex].item;
		int space = to.GetMaxAmount() - to.GetQty();
		int add = (from.GetQty() <= space) ? from.GetQty() : space;
		to.AddQty(add);
		from.AddQty(-add);
		if (from.GetQty() <= 0) { SLOT[fromIndex].item = null; return true; }
		return false;
	}

	/// <summary> nulls the slot containing this item </summary>
	public void Remove(IInventoryItem item)
	{
		int idx = GetIndexFor(item);
		if (idx >= 0) SLOT[idx].item = null;
		if (GetActiveItem() == item) activeSlotIndex = 0;
	}

	/// <summary> nulls all slots, resets active to 0 </summary>
	public void Clear()
	{
		for (int i = 0; i < SLOT.Count; i++) SLOT[i].item = null;
		activeSlotIndex = 0;
	}

	/// <summary> sets activeSlotIndex (hotbar only) </summary>
	public void SwitchTo(int index)
	{
		if (index >= 0 && index < HOTBAR_SIZE)
			activeSlotIndex = index;
	}

	/// <summary> wraps activeSlotIndex by ±1 within hotbar, skipping empty slots </summary>
	public void Scroll(int delta)
	{
		if (HOTBAR_SIZE <= 0) return;
		int start = activeSlotIndex;
		int dir = (delta > 0) ? 1 : -1;
		for (int i = 0; i < HOTBAR_SIZE; i++)
		{
			int next = (start + dir + HOTBAR_SIZE) % HOTBAR_SIZE;
			start = next;
			if (SLOT[next].item != null) { activeSlotIndex = next; return; }
		}
	}

	/// <summary> swaps item field between two slots (preserves index/isHotBar) </summary>
	public void Swap(int indexA, int indexB)
	{
		if (indexA < 0 || indexB < 0 || indexA >= SLOT.Count || indexB >= SLOT.Count) return;
		var temp = SLOT[indexA].item;
		SLOT[indexA].item = SLOT[indexB].item;
		SLOT[indexB].item = temp;
	}

	/// <summary> PhaseBLOG formatter </summary>
	public string GetSnapshot(string header = "inventory snapshot")
	{
		return $@"
{'='.repeat(4) + header + '='.repeat(4)}
// SLOT
{PhaseBLOG.LIST_SLOT__TO__JSON(SLOT)}
// activeSlotIndex: {activeSlotIndex}";
	}
	#endregion
}