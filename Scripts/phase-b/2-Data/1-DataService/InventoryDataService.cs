using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using SPACE_UTIL;

/// <summary>
/// purely C# Collection, manages all inventory slots: add/remove/switch/stack/swap
/// </summary>
public class InventoryDataService
{
	#region private static API
	static int hotBarSize = 10, totalBarSize = 40;
	#endregion

	#region Nested Entity
	public class Slot
	{
		public BaseHeldTool tool;
		public int index;
		public bool isHotBar { get => index < InventoryDataService.hotBarSize; }
	}
	#endregion

	#region private API
	List<Slot> SLOT = new List<Slot>();
	int selectedSlotIndex = 0;
	#endregion

	#region public API
	public int GetHotBarSize() => hotBarSize;
	public int GetTotalBarSize() => totalBarSize;

	/// <summary>
	/// create new empty slots of totalBarSize
	/// </summary>
	public void Build()
	{
		SLOT.Clear();
		for (int i0 = 0; i0 < totalBarSize; i0 += 1)
			SLOT.Add(new Slot { tool = null, index = i0 });
		selectedSlotIndex = 0;
	}
	/// <summary>
	/// get all slots
	/// </summary>
	/// <returns></returns>
	public List<Slot> GetAllSlots()
	{
		return SLOT;
	}
	public Slot GetActiveSlot()
	{
		return SLOT.TryGetAt(selectedSlotIndex);
	}
	public BaseHeldTool GetActiveTool()
	{
		Slot slot = GetActiveSlot();
		if (slot != null)
			return slot.tool;
		return null;
	}
	public int GetActiveSlotIndex()
	{
		return selectedSlotIndex;
	}
	public int GetIndexFotTool(BaseHeldTool tool)
	{
		return SLOT.findIndex(slot => slot.tool == tool);
	}

	public int TryAddTool(BaseHeldTool tool, int preferredSlot = -1)
	{
		if(tool.GetMaxAmount() > 1) // allows stacking
			for(int i0 = 0; i0 < SLOT.Count; i0 += 1)
			{
				var slot = SLOT[i0];
				if(slot.tool != null)
					if( slot.tool.GetType() == tool.GetType() &&
						slot.tool.GetQty() < slot.tool.GetMaxAmount())
					{
						int space = slot.tool.GetMaxAmount() - slot.tool.GetQty();
						int addCount = tool.GetQty().clamp(0, space);
						slot.tool.AddQty(addCount);
						tool.AddQty(-addCount);
						if (tool.GetQty() <= 0)
							return i0;
					}
			}
		// preferred slot
		int targetIndex = -1;
		if (preferredSlot.inRange(SLOT))
			if (SLOT[preferredSlot].tool == null)
				targetIndex = preferredSlot;
		if(targetIndex == -1)
			for(int i0 = 0; i0 < SLOT.Count; i0 += 1)
				if(SLOT[i0].tool 
== null)
				{
					targetIndex = i0;
					break;
				}
		if (targetIndex == -1)
			return -1;
		SLOT[targetIndex].tool = tool;
		return targetIndex;
	}
	public void RemoveTool(BaseHeldTool tool)
	{
		int index = GetIndexFotTool(tool);
		if (index != -1)
			SLOT[index].tool = null;
		if (GetActiveTool() == tool)
			selectedSlotIndex = 0;
	}
	public void ClearAll()
	{
		SLOT.forEach(slot => slot.tool = null);
		selectedSlotIndex = 0;
	}
	public void SwitchTo(int index)
	{
		if (index.inRange(0, hotBarSize))
			selectedSlotIndex = index;
	}
	/// <summary>
	/// wraps selectedSlotIndex within hhot bar
	/// </summary>
	/// <param name="delta"></param>
	public void Scroll(int delta)
	{
		int start = selectedSlotIndex,
			dir = (delta > 0) ? +1 : -1;
		for (int i0 = 0; i0 <= hotBarSize - 1; i0 += 1)
		{
			int next = (start + dir).mod(length: hotBarSize);
			start = next;
			if(SLOT[next].tool != null)
			{
				selectedSlotIndex = next;
				return;
			}
		}
	}

	/// <summary>
	/// swaps tools between 2 slots
	/// </summary>
	public void Swap(int indexA, int indexB)
	{
		if (!indexA.inRange(SLOT) || !indexB.inRange(SLOT))
			return;
		var temp = SLOT[indexA];
		SLOT[indexA] = SLOT[indexB];
		SLOT[indexB] = temp;
	}

	public string GetSnapshot(string header = "inventory dataService snapshot")
	{
		return $@"
{'='.repeat(4) + header + '='.repeat(4)}
// SLOT
{PhaseBLOG.LIST_SLOT__TO__JSON(SLOT)},
selectedSlotIndex: {selectedSlotIndex}";
	}
	#endregion
}
