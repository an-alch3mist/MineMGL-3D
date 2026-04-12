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
