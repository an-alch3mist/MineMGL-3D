using System.Collections;
using UnityEngine;

/// <summary>
/// Hold left-click → swing animation → 0.2s delay → raycast from camera.
/// Hits IDamageable → TakeDamage. Hits Rigidbody → AddForce.
/// </summary>
[AddComponentMenu("MineMGL/Tools/ToolPickaxe")]
public class ToolPickaxe : BaseHeldTool
{
	[SerializeField] float _useRange = 2f;
	[SerializeField] float _damage = 10f;
	[SerializeField] float _attackCooldown = 1f;
	[SerializeField] LayerMask _hitLayers;
	[SerializeField] bool _canBreakOreIntoCrushed;

	float lastAttackTime = -1f;

	void SwingPickaxe()
	{
		if (ownerCam == null) return;
		if (_viewModelAnimator != null) _viewModelAnimator.Play(AnimParamType.attack1.ToString(), -1, 0f);
		StartCoroutine(PerformAttack(0.2f));
		lastAttackTime = Time.time;
	}
	IEnumerator PerformAttack(float delaySeconds)
	{
		yield return new WaitForSeconds(delaySeconds);
		if (!gameObject.activeInHierarchy || ownerCam == null) yield break;
		if (!Physics.Raycast(ownerCam.transform.position, ownerCam.transform.forward, out RaycastHit hit, _useRange, _hitLayers))
			yield break;
		Rigidbody hitRb = hit.collider.GetComponent<Rigidbody>();
		if (hitRb != null)
		{
			hitRb.AddForceAtPosition(ownerCam.transform.forward * 5f, hit.point, ForceMode.Impulse);
			PhysicsSoundPlayer soundPlayer = hitRb.GetComponent<PhysicsSoundPlayer>();
			if (soundPlayer != null) soundPlayer.PlayImpactSound();
		}
	}

	public override void PrimaryFire() { }
	public override void PrimaryFireHeld()
	{
		if (Time.time - lastAttackTime >= _attackCooldown) SwingPickaxe();
	}
}