using UnityEngine;

/// <summary>
/// I'm a special OrePiece (like an OreCluster) that can take damage from collisions. When
/// something hits me hard enough (velocity > _minDamageVelocity), I lose health. At zero health
/// I play break particles, call CompleteClusterBreaking (spawns smaller ore pieces), and trigger
/// a small explosion force. I also prevent PhysicsSoundPlayer from existing on the same GO
/// since I handle my own collision sounds.
///
/// Who uses me: Physics engine (OnCollisionEnter), ToolPickaxe (can hit me via IDamageable too).
/// Events I fire: none directly (particles + cluster spawn are immediate).
/// </summary>
public class DamageableOrePiece : OrePiece, IDamageable
{
	#region Inspector Fields
	[Header("Damageable Settings")]
	[SerializeField] float _health = 10f;
	[SerializeField] float _minDamageVelocity = 30f;
	[SerializeField] float _cooldown = 0.1f;
	#endregion

	#region private API
	float lastPlayTime;
	#endregion

	#region public API — IDamageable
	/// <summary> take damage — breaks into cluster pieces at 0 health </summary>
	public void TakeDamage(float damage, Vector3 position)
	{
		_health -= damage;
		if (_health <= 0f)
		{
			// Phase H: play break sound
			Singleton<ParticleManager>.Ins.CreateParticle(
				Singleton<ParticleManager>.Ins.GetBreakOreNodeParticlePrefab(), transform.position);
			CompleteClusterBreaking();
			UtilsPhaseB.SimpleExplosion(transform.position, 0.5f, 2f, 0.1f);
		}
	}
	#endregion

	#region Unity Life Cycle
	/// <summary> When something hits me, checks if the impact velocity exceeds _minDamageVelocity.
	/// If so, calculates damage proportional to the excess velocity and calls TakeDamage on myself. </summary>
	private void OnCollisionEnter(Collision collision)
	{
		if (collision.contactCount == 0 || Time.time - lastPlayTime < _cooldown) return;
		lastPlayTime = Time.time;
		float sqrVel = collision.relativeVelocity.sqrMagnitude;
		if (sqrVel > _minDamageVelocity)
		{
			float damage = (sqrVel - _minDamageVelocity) * 0.1f;
			TakeDamage(damage, transform.position);
		}
		// Phase H: play physics impact sound if close enough to player
	}
	#endregion
}