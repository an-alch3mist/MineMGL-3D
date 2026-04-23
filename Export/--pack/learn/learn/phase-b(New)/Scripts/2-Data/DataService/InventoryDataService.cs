using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using SPACE_UTIL;

/// <summary>
/// I'm the pure C# brain behind the inventory — no Unity dependency, testable via `new`.
/// I manage 40 Slot objects (10 hotbar + 30 extended). Each Slot holds a BaseHeldTool reference
/// or null. InventoryOrchestrator calls my Build() to create the slots, then uses TryAdd (finds
/// first empty slot or stacks), Remove, SwitchTo (changes active index), Scroll (cycles hotbar),
/// Swap (drag-drop between any two slots), and GetSnapshot (test logging). I track activeSlotIndex
/// so the Orchestrator knows which tool is equipped. I'm completely decoupled from UI — I have
/// no idea Field_InventorySlot exists.
///
/// Who uses me: InventoryOrchestrator (all operations), DEBUG_CheckB (plain C# test).
/// Events I fire: none. Events I subscribe to: none. Pure data.
/// </summary>
public class InventoryDataService
{
	#region private API
	List<Slot> SLOTS = new List<Slot>();
	int activeSlotIndex = 0;
	static int HOTBAR_SIZE = 10;
	static int TOTAL_SIZE = 40;
	#endregion

	#region Nested Type
	public class Slot
	{
		public BaseHeldTool Tool;
		public int Index;
		public bool IsHotbar => Index < HOTBAR_SIZE;
	}
	#endregion

	#region public API
	/// <summary> create 40 empty slots (10 hotbar + 30 extended) </summary>
	public void Build()
	{
		SLOTS.Clear();
		for (int i = 0; i < TOTAL_SIZE; i++)
			SLOTS.Add(new Slot { Tool = null, Index = i });
		activeSlotIndex = 0;
	}
	/// <summary> returns all slots </summary>
	public List<Slot> GetSlots() => SLOTS;
	/// <summary> currently selected slot </summary>
	public Slot ActiveSlot => (activeSlotIndex >= 0 && activeSlotIndex < SLOTS.Count) ? SLOTS[activeSlotIndex] : null;
	/// <summary> currently equipped tool (or null) </summary>
	public BaseHeldTool ActiveTool => ActiveSlot?.Tool;
	/// <summary> active slot index </summary>
	public int ActiveSlotIndex => activeSlotIndex;
	/// <summary> hotbar size constant </summary>
	public int GetHotbarSize() => HOTBAR_SIZE;
	/// <summary> total size constant </summary>
	public int GetTotalSize() => TOTAL_SIZE;

	/// <summary> find slot index for a tool, or -1 </summary>
	public int GetIndexFor(BaseHeldTool tool)
	{
		for (int i = 0; i < SLOTS.Count; i++)
			if (SLOTS[i].Tool == tool) return i;
		return -1;
	}

	/// <summary> stack if possible → preferred slot → first empty. returns index or -1 </summary>
	public int TryAdd(BaseHeldTool tool, int preferredSlot = -1)
	{
		// stacking: if tool supports stacking, find matching and add qty
		if (tool.MaxAmount > 1)
		{
			for (int i = 0; i < SLOTS.Count; i++)
			{
				if (SLOTS[i].Tool != null && SLOTS[i].Tool.GetType() == tool.GetType()
					&& SLOTS[i].Tool.Quantity < SLOTS[i].Tool.MaxAmount)
				{
					int space = SLOTS[i].Tool.MaxAmount - SLOTS[i].Tool.Quantity;
					int add = (tool.Quantity <= space) ? tool.Quantity : space;
					SLOTS[i].Tool.Quantity += add;
					tool.Quantity -= add;
					if (tool.Quantity <= 0) return i;
				}
			}
		}
		// preferred slot
		int target = -1;
		if (preferredSlot >= 0 && preferredSlot < SLOTS.Count && SLOTS[preferredSlot].Tool == null)
			target = preferredSlot;
		// first empty
		if (target == -1)
		{
			for (int i = 0; i < SLOTS.Count; i++)
				if (SLOTS[i].Tool == null) { target = i; break; }
		}
		if (target == -1) return -1;
		SLOTS[target].Tool = tool;
		return target;
	}

	/// <summary> nulls the slot containing this tool </summary>
	public void Remove(BaseHeldTool tool)
	{
		int idx = GetIndexFor(tool);
		if (idx >= 0) SLOTS[idx].Tool = null;
		if (ActiveTool == tool) activeSlotIndex = 0;
	}

	/// <summary> nulls all slots, resets active to 0 </summary>
	public void Clear()
	{
		for (int i = 0; i < SLOTS.Count; i++) SLOTS[i].Tool = null;
		activeSlotIndex = 0;
	}

	/// <summary> sets activeSlotIndex (hotbar only) </summary>
	public void SwitchTo(int index)
	{
		if (index >= 0 && index < HOTBAR_SIZE)
			activeSlotIndex = index;
	}

	/// <summary> wraps activeSlotIndex by ±1 within hotbar, skipping empty </summary>
	public void Scroll(int delta)
	{
		if (HOTBAR_SIZE <= 0) return;
		int start = activeSlotIndex;
		int dir = (delta > 0) ? 1 : -1;
		for (int i = 0; i < HOTBAR_SIZE; i++)
		{
			int next = (start + dir + HOTBAR_SIZE) % HOTBAR_SIZE;
			start = next;
			if (SLOTS[next].Tool != null)
			{
				activeSlotIndex = next;
				return;
			}
		}
	}

	/// <summary> swaps tools between two slots </summary>
	public void Swap(int indexA, int indexB)
	{
		if (indexA < 0 || indexB < 0 || indexA >= SLOTS.Count || indexB >= SLOTS.Count) return;
		var temp = SLOTS[indexA].Tool;
		SLOTS[indexA].Tool = SLOTS[indexB].Tool;
		SLOTS[indexB].Tool = temp;
	}
	#endregion

	#region snapShot
	/// <summary> PhaseBLOG formatter </summary>
	public string GetSnapshot(string header = "inventory snapshot")
	{
		return $@"
{'='.repeat(4) + header + '='.repeat(4)}
// SLOTS
{PhaseBLOG.LIST_SLOTS__TO__JSON(SLOTS)}
// activeSlotIndex: {activeSlotIndex}";
	}
	#endregion
}