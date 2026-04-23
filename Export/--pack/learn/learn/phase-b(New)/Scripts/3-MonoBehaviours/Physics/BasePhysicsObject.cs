using System.Collections.Generic;
using UnityEngine;

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
	public const float STANDARD_LINEAR_DAMPING = 0.2f;
	public const float STANDARD_ANGULAR_DAMPING = 0.05f;
	#endregion

	#region private API
	[HideInInspector] public Vector3 SumVelocity;
	[HideInInspector] public float BestY;
	[HideInInspector] public int Count;
	[HideInInspector] public bool RetainY;
	Rigidbody rb;
	#endregion

	#region public API
	public Rigidbody Rb => rb;
	/// <summary> add conveyor velocity contribution </summary>
	public void AddConveyorVelocity(Vector3 velocity, bool retainY)
	{
		if (Count == 0) SumVelocity = velocity;
		else SumVelocity += velocity;
		if (velocity.y > BestY) BestY = velocity.y;
		Count++;
		if (retainY) RetainY = true;
	}
	/// <summary> reset per-frame accumulation </summary>
	public void ResetAccum()
	{
		SumVelocity = default;
		BestY = 0f;
		Count = 0;
		RetainY = false;
	}
	#endregion

	#region Unity Life Cycle
	/// <summary> Caches the Rigidbody reference and sets standard drag values so all physics
	/// objects start with consistent drag (used as reset values by OrePiecePoolManager). </summary>
	protected virtual void Awake()
	{
		// → cache Rigidbody for conveyor velocity + pool reset
		rb = GetComponent<Rigidbody>();
		if (rb != null)
		{
			rb.linearDamping = STANDARD_LINEAR_DAMPING;
			rb.angularDamping = STANDARD_ANGULAR_DAMPING;
		}
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