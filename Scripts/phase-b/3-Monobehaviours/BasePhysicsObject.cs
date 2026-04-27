using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using TMPro;

using SPACE_UTIL;


/// <summary>
/// I'm the base class for any physics object that can sit on conveyors. I cache my Rigidbody
/// on Awake and set standard drag values. In FixedUpdate I apply accumulated conveyor velocities
/// (Phase D populates these via RegisterConveyor/UnregisterConveyor stubs). OrePiece and
/// BaseSellableItem inherit from me. I also expose STANDARD_LINEAR_DAMPING and STANDARD_ANGULAR_DAMPING
/// constants that OrePiecePoolManager uses when resetting pooled ore.
///
/// Who inherits me: BaseSellableItem → BaseHeldTool → all tools, OrePiece → DamageableOrePiece.
/// </summary>
public class BasePhysicsObject : MonoBehaviour
{
	#region constants
	public static float standardLinearDampening { get;  } = 0.2f;
	public static float standardAngularDampening { get; }  = 0.05f;
	#endregion

	#region private API
	Vector3 sumVel;
	float bestY;
	int count;
	bool retainY;
	Rigidbody rb;
	#endregion

	#region public API
	public Vector3 GetSumVel() => sumVel;
	public float GetBestY() => bestY;
	public int GetCount() => count;
	public bool GetRetainY() => retainY;
	public Rigidbody GetRb() => rb;
	/// <summary> add conveyor velocity contribution </summary>
	public void AddConveyorVelocity(Vector3 velocity, bool retainY)
	{
		if (count == 0) sumVel = velocity;
		else sumVel += velocity;
		if (velocity.y > bestY) bestY = velocity.y;
		count++;
		if (retainY) this.retainY = true;
	}
	/// <summary> reset per-frame accumulation </summary>
	public void ResetAccum()
	{
		sumVel = default;
		bestY = 0f;
		count = 0;
		retainY = false;
	}
	#endregion

	#region Unity Life Cycle
	/// <summary> Caches the Rigidbody reference and sets standard drag values so all physics
	/// objects start with consistent drag (used as reset values by OrePiecePoolManager). </summary>
	protected virtual void Awake()
	{
		// → cache Rigidbody for conveyor velocity + pool reset
		rb = GetComponent<Rigidbody>();
		rb.linearDamping = standardLinearDampening;
		rb.angularDamping = standardAngularDampening;
	}
	protected virtual void OnEnable()
	{
		// Phase D: ConveyorBeltManager.Register(this);
	}
	protected virtual void OnDisable()
	{
		// Phase D: ConveyorBeltManager.Unregister(this);
	}
	#endregion
}
