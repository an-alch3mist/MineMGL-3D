using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

using SPACE_UTIL;

public class DEBUG_CheckB : MonoBehaviour
{
	InventoryDataService dataService = new InventoryDataService();

	#region Unity Life Cycle
	private void Update()
	{
		if (INPUT.K.InstantDown(KeyCode.Space))
		{
			Debug.Log($"dataService.Build()".colorTag("lime"));
			dataService.Build();
		}
		else if(INPUT.K.InstantDown(KeyCode.U))
		{
			// try add in real usage.
			// TODO
		}
		else if (INPUT.K.InstantDown(KeyCode.I))
		{
			// remove first non null slot.tool
			Debug.Log($"dataService.RemoveTool()".colorTag("lime"));
			var slot = dataService.GetAllSlots().find(s => s.tool != null);
			if (slot != null)
				dataService.RemoveTool(slot.tool);
		}
		else if (INPUT.K.InstantDown(KeyCode.O))
		{
			// switch to delta: +3 slot tool
			Debug.Log($"dataService.Switch()".colorTag("lime"));
			dataService.SwitchTo(+3);
		}
		else if (INPUT.K.InstantDown(KeyCode.P))
		{
			LOG.AddLog(dataService.GetSnapshot(), "json");
		}
	}

	[ContextMenu("run dataService sample")]
	public void RunDataServiceSample()
	{
		InventoryDataService dataService = new InventoryDataService();
		Debug.Log(C.method(this, "magenta"));
		dataService.Build();
		LOG.AddLog(dataService.GetSnapshot(), "json");
	}
	#endregion
}
