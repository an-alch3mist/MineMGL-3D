using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;

/// <summary>
/// I'm a breakable rock embedded in mine walls/floor. When the player swings a pickaxe at me,
/// ToolPickaxe calls my TakeDamage (via IDamageable interface) which subtracts from my health.
/// When health hits zero, BreakNode fires — I pick a random drop count (2-4), use WeightedRandom
/// on my _possibleDrops list to pick OrePiece prefabs, spawn them via OrePiecePoolManager with
/// random velocity (they fly out and bounce), play a break particle burst via ParticleManager,
/// fire OnOreMined so the quest system can track mining progress, then Destroy myself permanently.
/// On Start I pick one random model variant from _models and hide the rest (visual variety).
///
/// Who uses me: ToolPickaxe (TakeDamage via IDamageable raycast).
/// Events I fire: OnOreMined(resourceType, position).
/// </summary>
public class OreNode : MonoBehaviour, IDamageable
{
	#region Inspector Fields
	[SerializeField] ResourceType _resourceType;
	[SerializeField] float _health = 100f;
	[SerializeField] int _minDrops = 2;
	[SerializeField] int _maxDrops = 4;
	[SerializeField] List<WeightedNodeDrop> _possibleDrops = new List<WeightedNodeDrop>();
	[SerializeField] GameObject[] _models;
	#endregion

	#region public API
	public ResourceType ResourceType => _resourceType;
	#endregion

	#region public API — IDamageable
	/// <summary> Called by ToolPickaxe via IDamageable when the pickaxe raycast hits me. Subtracts
	/// damage from health, and if health reaches zero, calls BreakNode which spawns ore pieces,
	/// plays break particles, fires OnOreMined, and destroys this node permanently. </summary>
	public void TakeDamage(float damage, Vector3 position)
	{
		// Phase H: play take damage sound
		// → subtract damage from health
		_health -= damage;
		// → at zero health: spawn 2-4 ore pieces, play particles, fire OnOreMined, destroy node
		if (_health <= 0f) BreakNode(position);
	}
	#endregion

	#region private API
	/// <summary> Spawns 2-4 ore pieces with random velocity from a point between node center and
	/// hit position, plays break particle burst, fires OnOreMined for quest tracking, then
	/// permanently destroys this node. </summary>
	void BreakNode(Vector3 hitPosition)
	{
		// → pick random drop count between _minDrops and _maxDrops
		int dropCount = Random.Range(_minDrops, _maxDrops + 1);
		Vector3 spawnCenter = (transform.position + hitPosition) * 0.5f;
		// → for each drop: weighted random prefab, spawn via pool, apply random velocity + spin
		for (int i = 0; i < dropCount; i++)
		{
			Vector3 pos = spawnCenter + Random.insideUnitSphere * 0.15f;
			OrePiece prefab = GetOrePrefab();
			if (prefab == null) continue;
			var spawned = Singleton<OrePiecePoolManager>.Ins.SpawnPooledOre(prefab, pos, Quaternion.identity);
			if (spawned?.Rb != null)
			{
				spawned.Rb.linearVelocity = new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(2f, 4f), Random.Range(-1.5f, 1.5f));
				spawned.Rb.angularVelocity = Random.insideUnitSphere * Random.Range(1f, 50f);
			}
		}
		// → spawn break particle burst at hit point
		// Phase H: play break sound
		Singleton<ParticleManager>.Ins.CreateParticle(
			Singleton<ParticleManager>.Ins.GetBreakOreNodeParticlePrefab(), hitPosition);
		// → fire OnOreMined so quest system (Phase F) can track mining progress
		// purpose: quest system + other listeners react to mining
		GameEvents.RaiseOreMined(_resourceType, transform.position);
		// Phase G: MarkStaticPositionAsBroken for save/load
		// → permanently destroy this node GO
		Destroy(gameObject);
	}
	/// <summary> Picks one OrePiece prefab from _possibleDrops using weighted random — higher
	/// weight entries are more likely. Returns null if the list is empty. </summary>
	OrePiece GetOrePrefab()
	{
		return UtilsPhaseC.WeightedRandom(_possibleDrops, d => d.Weight)?.OrePrefab;
	}
	#endregion

	#region Unity Life Cycle
	/// <summary> Picks one random model variant from _models and hides the rest — gives each
	/// node in the scene a slightly different look even if they use the same prefab. </summary>
	private void Start()
	{
		// → pick one random model, hide all others
		if (_models.Length > 0)
		{
			int chosen = Random.Range(0, _models.Length);
			for (int i = 0; i < _models.Length; i++)
				_models[i].SetActive(i == chosen);
		}
	}
	#endregion
}