using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using SPACE_UTIL;

public static class UtilsPhaseB
{
	/// <summary>
	/// ignore / restore, collisions between all colliders/subColliders betweem two GO
	/// </summary>
	/// <param name="objA"></param>
	/// <param name="objB"></param>
	/// <param name="shouldIgnoreCollision"></param>
	public static void IgnoreAllCollisions(GameObject objA, GameObject objB, bool shouldIgnoreCollision = true)
	{
		foreach (var c0 in objA.GetComponentsInChildren<Collider>())
			foreach (var c1 in objB.GetComponentsInChildren<Collider>())
				if (c0 != null && c1 != null)
					if (c0 != c1) // hmm
						Physics.IgnoreCollision(c0, c1, shouldIgnoreCollision);
	}
}