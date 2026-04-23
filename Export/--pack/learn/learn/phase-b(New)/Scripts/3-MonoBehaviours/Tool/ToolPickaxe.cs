using System.Collections;
using UnityEngine;

/// <summary>
/// I'm the pickaxe tool. When the player holds left-click, I play a swing animation then
/// wait 0.2s before raycasting from the camera. If I hit an IDamageable (OreNode), I call
/// TakeDamage on it. If I hit an OrePiece, I try TryConvertToCrushed (splits into 2 crushed).
/// If I hit a plain Rigidbody, I push it with AddForce. I also play the PhysicsSoundPlayer
/// impact sound on the hit target if it has one. My cooldown prevents swing spam.
///
/// Who uses me: InventoryOrchestrator routes PrimaryFireHeld to me.
/// Events I fire: none directly (IDamageable targets fire their own events when broken).
/// </summary>
public class ToolPickaxe : BaseHeldTool
{
	#region Inspector Fields
	[SerializeField] float _useRange = 2f;
	[SerializeField] float _damage = 10f;
	[SerializeField] float _attackCooldown = 1f;
	[SerializeField] LayerMask _hitLayers;
	[SerializeField] bool _canBreakOreIntoCrushed;
	#endregion

	#region private API
	float lastAttackTime = -1f;

	void SwingPickaxe()
	{
		if (owner == null) return;
		Camera cam = owner.PlayerCam;
		if (cam == null) return;
		if (_viewModelAnimator != null) _viewModelAnimator.Play("Attack1", -1, 0f);
		// Phase H: play swing sound
		StartCoroutine(PerformAttack(0.2f));
		lastAttackTime = Time.time;
	}
	IEnumerator PerformAttack(float delaySeconds)
	{
		yield return new WaitForSeconds(delaySeconds);
		if (!gameObject.activeInHierarchy || owner == null) yield break;
		Camera cam = owner.PlayerCam;
		if (cam == null) yield break;
		if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, _useRange, _hitLayers))
			yield break;

		// Phase C: crushed ore conversion check
		// Phase C: IDamageable.TakeDamage(hit)

		Rigidbody hitRb = hit.collider.GetComponent<Rigidbody>();
		if (hitRb != null)
		{
			hitRb.AddForceAtPosition(cam.transform.forward * 5f, hit.point, ForceMode.Impulse);
			PhysicsSoundPlayer soundPlayer = hitRb.GetComponent<PhysicsSoundPlayer>();
			if (soundPlayer != null) soundPlayer.PlayImpactSound();
		}
	}
	#endregion

	#region public API — overrides
	public override void PrimaryFire() { }
	public override void PrimaryFireHeld()
	{
		if (Time.time - lastAttackTime >= _attackCooldown) SwingPickaxe();
	}
	#endregion
}