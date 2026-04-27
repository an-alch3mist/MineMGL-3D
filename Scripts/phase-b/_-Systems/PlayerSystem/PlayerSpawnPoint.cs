using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;

/// <summary>
/// I mark a spawn position in the scene. On OnEnable I add myself to a static list, OnDisable
/// I remove myself. GetRandomSpawnPoint picks a random entry from that list — zero FindObjectOfType.
/// PlayerMovement.RespawnPlayer calls GetRandomSpawnPoint when the player falls below y=-200.
///
/// Who uses me: PlayerMovement (RespawnPlayer). Self-registering static list pattern.
/// </summary>
public class PlayerSpawnPoint : MonoBehaviour
{
	#region private API
	static List<PlayerSpawnPoint> All = new List<PlayerSpawnPoint>();
	#endregion

	#region public API
	/// <summary> returns a random spawn point position </summary>
	public static Vector3 GetRandomSpawnPoint()
	{
		return All.getRandom().transform.position;
	}
	#endregion

	#region Unity Life Cycle
	private void OnEnable() => All.Add(this);
	private void OnDisable() => All.Remove(this);
	#endregion
}