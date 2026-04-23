using System;
using UnityEngine;

/// <summary>
/// extends ConveyorBelt with left-right oscillation only (no vertical component)
/// </summary>
public class ConveyorBeltShakerHorizontal : ConveyorBelt
{
	#region Inspector Fields
	[Header("Shaker Settings")]
	[SerializeField] float _shakeSpeed = 2f;
	[SerializeField] float _shakeFrequency = 2f;
	#endregion

	#region private API
	Vector3 rightDir;
	#endregion

	#region Unity Life Cycle
	protected override void OnEnable()
	{
		base.OnEnable();
		rightDir = transform.right;
	}
	protected override void FixedUpdate()
	{
		if (GetDisabled() || _physicsObjectsOnBelt.Count == 0) return;
		float lateral = Mathf.Sign(Mathf.Sin(Time.fixedTime * MathF.PI * 2f * _shakeFrequency));
		Vector3 shake = rightDir * (_shakeSpeed * lateral);
		for (int i = _physicsObjectsOnBelt.Count - 1; i >= 0; i--)
			_physicsObjectsOnBelt[i].AddConveyorVelocity(_pushVelocity + shake, GetRetainYVelocity());
	}
	#endregion
}