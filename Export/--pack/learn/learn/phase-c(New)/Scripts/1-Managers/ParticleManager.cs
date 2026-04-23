using UnityEngine;

/// <summary>
/// I spawn particle effects at world positions. OreNode calls me for break particles when a
/// node shatters, DamageableOrePiece calls me for cluster break particles, and ToolPickaxe
/// can call me for hit impact sparks. I hold references to the prefabs (GenericHitImpactParticle,
/// OreNodeHitParticlePrefab, BreakOreNodeParticlePrefab) as public fields so other scripts
/// can pass them to CreateParticle. I just Instantiate — the particle systems auto-destroy.
///
/// Who uses me: OreNode (break burst), DamageableOrePiece (cluster break), ToolPickaxe (hit sparks).
/// </summary>
public class ParticleManager : Singleton<ParticleManager>
{
	#region Inspector Fields
	[SerializeField] GameObject _genericHitImpactParticle;
	[SerializeField] GameObject _oreNodeHitParticlePrefab;
	[SerializeField] GameObject _breakOreNodeParticlePrefab;
	#endregion

	#region public API — read-only getters (external scripts pass these to CreateParticle)
	public GameObject GetGenericHitImpactParticle() => _genericHitImpactParticle;
	public GameObject GetOreNodeHitParticlePrefab() => _oreNodeHitParticlePrefab;
	public GameObject GetBreakOreNodeParticlePrefab() => _breakOreNodeParticlePrefab;
	/// <summary> spawn a particle at position + rotation </summary>
	public void CreateParticle(GameObject prefab, Vector3 position, Quaternion rotation = default)
	{
		if (prefab == null) return;
		Object.Instantiate(prefab, position, rotation);
	}
	#endregion
}