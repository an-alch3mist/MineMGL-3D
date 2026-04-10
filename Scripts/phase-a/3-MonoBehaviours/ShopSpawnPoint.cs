using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

using SPACE_UTIL;

public class ShopSpawnPoint : MonoBehaviour
{
	public static Vector3 GetRandomSpawnPoint()
	{
		var LIST = GameObject.FindObjectsOfType<ShopSpawnPoint>().ToList();
		if (LIST == null) return Vector3.zero;
		if (LIST.Count == 0) return Vector3.zero;
		return LIST.getRandom()
					.transform.position;
	}
	private void OnDrawGizmos()
	{
		Gizmos.color = Color.limeGreen;
		Gizmos.DrawWireSphere(this.transform.position, radius: 0.1f);
	}
}
