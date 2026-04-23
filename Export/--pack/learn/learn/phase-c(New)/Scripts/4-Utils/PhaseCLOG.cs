using System.Collections.Generic;
using UnityEngine;
using SPACE_UTIL;

/// <summary>
/// formats resource description snapshots for test logging
/// </summary>
public static class PhaseCLOG
{
	/// <summary> snapshot of all resource descriptions </summary>
	public static string LIST_RESOURCE_DESC__TO__JSON(List<ResourceDescription> RESOURCE_DESC)
	{
		var snapshot = RESOURCE_DESC.map(rd => new
		{
			rd.ResourceType,
			Color = $"#{ColorUtility.ToHtmlStringRGB(rd.DisplayColor)}",
		});
		return snapshot.ToNSJson(pretify: true);
	}
}