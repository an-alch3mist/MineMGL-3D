using UnityEngine;
using System.Collections;


// since its similar to pure data such as SO_, Field_ no need for Get...() methods
public class InventorySlot
{
	public BaseHeldTool tool;
	public int index;
	public bool isHotBar
	{
		get => index < InventoryDataService.hotBarSize;
	}
}