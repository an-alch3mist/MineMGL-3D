using System;
using UnityEngine;

/// <summary>
/// extends ConveyorBelt with left-right + up-down oscillation for sieving tables
/// </summary>
public class ConveyorBeltShaker : ConveyorBelt
{
	#region Inspector Fields
	[Header("Shaker Settings")]
	[SerializeField] float _shakeSpeed = 2f;
	[SerializeField] float _shakeFrequency = 2f;
	[SerializeField] float _verticalShakeSpeed = 1f;
	[SerializeField] float _verticalShakeFrequency = 3f;
	#endregion

	#region private API
	Vector3 rightDir;
	Vector3 upDir;
	#endregion

	#region Unity Life Cycle
	protected override void OnEnable()
	{
		base.OnEnable();
		rightDir = transform.right;
		upDir = transform.up;
	}
	protected override void FixedUpdate()
	{
		if (GetDisabled() || _physicsObjectsOnBelt.Count == 0) return;
		float t = Time.fixedTime;
		float lateral = Mathf.Sign(Mathf.Sin(t * MathF.PI * 2f * _shakeFrequency));
		Vector3 shake = rightDir * (_shakeSpeed * lateral);
		float vertical = Mathf.Sin(t * MathF.PI * 2f * _verticalShakeFrequency);
		shake += upDir * (_verticalShakeSpeed * vertical);
		for (int i = _physicsObjectsOnBelt.Count - 1; i >= 0; i--)
			_physicsObjectsOnBelt[i].AddConveyorVelocity(_pushVelocity + shake, GetRetainYVelocity());
	}
	#endregion
}