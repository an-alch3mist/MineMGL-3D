using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SPACE_UTIL;

public class PlayerSpawnPoint : MonoBehaviour
{
	public static Vector3 GetRandomSpawnPoint()
	{
		return GameObject.FindObjectsOfType<PlayerSpawnPoint>()
							.getRandom()
							.transform.position;
	}
	private void OnDrawGizmos()
	{
		Gizmos.color = Color.limeGreen;
		Gizmos.DrawSphere(this.transform.position, 0.1f);
	}
}
