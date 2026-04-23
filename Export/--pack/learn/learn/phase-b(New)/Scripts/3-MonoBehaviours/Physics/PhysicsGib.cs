using System.Collections;
using UnityEngine;

/// <summary>
/// I'm a debris piece (rock chunk, metal shard, etc.) that auto-destroys after _lifetime seconds.
/// Spawned by particle effects or node breaks for visual flair. I just exist, fall via gravity,
/// and disappear. No pool recycling — I'm cheap and short-lived.
///
/// Who uses me: ParticleManager or manual Instantiate for break effects.
/// </summary>
public class PhysicsGib : BaseSellableItem
{
	#region private API
	float despawnTime = 8f;
	#endregion

	#region public API
	/// <summary> detach from parent, enable, apply velocity, start despawn timer </summary>
	public void DetachAndDespawn(Vector3? velocity = null)
	{
		transform.SetParent(null);
		gameObject.SetActive(true);
		if (velocity.HasValue && Rb != null)
			Rb.linearVelocity = velocity.Value;
		StartCoroutine(WaitThenDespawn());
	}
	#endregion

	#region private API
	IEnumerator WaitThenDespawn()
	{
		yield return new WaitForSeconds(despawnTime * Random.Range(0.7f, 1.3f));
		if (this != null) Destroy(gameObject);
	}
	#endregion
}