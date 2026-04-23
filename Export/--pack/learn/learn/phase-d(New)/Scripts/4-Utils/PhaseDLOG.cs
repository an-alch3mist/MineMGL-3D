using System.Collections.Generic;
using UnityEngine;
using SPACE_UTIL;

/// <summary>
/// formats building/conveyor snapshots for test logging
/// </summary>
public static class PhaseDLOG
{
	public static string LIST_CONVEYOR_BELTS__TO__JSON()
	{
		var snapshot = ConveyorBelt.AllConveyorBelts.map(c => new
		{
			Name = c.gameObject.name,
			c.Speed,
			c.Disabled,
			ObjectsOnBelt = c.GetType().Name
		});
		return snapshot.ToNSJson(pretify: true);
	}
}