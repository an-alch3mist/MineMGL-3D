using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

using SPACE_UTIL;

/// <summary>
/// physics helpers — collision ignoring, simple explosion
/// </summary>
public static class UtilsPhaseB
{
	/// <summary> ignore/restore collisions between all colliders on two GOs </summary>
	public static void IgnoreAllCollisions(GameObject a, GameObject b, bool ignore)
	{
		Collider[] colsA = a.GetComponentsInChildren<Collider>();
		Collider[] colsB = b.GetComponentsInChildren<Collider>();
		foreach (var ca in colsA)
			foreach (var cb in colsB)
				if (ca != null && cb != null && ca != cb)
					Physics.IgnoreCollision(ca, cb, ignore);
	}

	/// <summary> apply explosion force to nearby rigidbodies </summary>
	public static void SimpleExplosion(Vector3 center, float radius, float force, float upwardsMod = 0.5f)
	{
		Collider[] cols = Physics.OverlapSphere(center, radius);
		foreach (var col in cols)
			col.attachedRigidbody?.AddExplosionForce(force, center, radius, upwardsMod, ForceMode.Impulse);
	}

	#region extra
	// nice-to-have: SetLayerRecursively — used by BuildingManager (Phase D) for ghost layer switching
	public static void SetLayerRecursively(GameObject obj, int layer)
	{
		obj.layer = layer;
		foreach (Transform child in obj.transform)
			SetLayerRecursively(child.gameObject, layer);
	}
	#endregion
}