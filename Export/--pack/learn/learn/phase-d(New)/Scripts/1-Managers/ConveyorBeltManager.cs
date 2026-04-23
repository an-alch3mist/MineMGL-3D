using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// applies accumulated conveyor velocities to registered physics objects in FixedUpdate.
/// BasePhysicsObject.AddConveyorVelocity accumulates per-belt velocities during ConveyorBelt.FixedUpdate,
/// then ConveyorBeltManager.FixedUpdate applies the averaged result as rb.linearVelocity.
/// Also does round-robin belt cleanup (null/inactive objects) in Update.
/// </summary>
[DefaultExecutionOrder(-10)]
public class ConveyorBeltManager : Singleton<ConveyorBeltManager>
{
	#region private API
	static readonly List<BasePhysicsObject> Objects = new List<BasePhysicsObject>();
	int currentBeltIndex;
	#endregion

	#region public API
	public static void Register(BasePhysicsObject obj)
	{
		if (obj != null && !Objects.Contains(obj)) Objects.Add(obj);
	}
	public static void Unregister(BasePhysicsObject obj)
	{
		if (obj != null) Objects.Remove(obj);
	}
	#endregion

	#region Unity Life Cycle
	private void Update()
	{
		var allBelts = ConveyorBelt.AllConveyorBelts;
		if (allBelts.Count == 0) { currentBeltIndex = 0; return; }
		if (currentBeltIndex >= allBelts.Count) currentBeltIndex = 0;
		var belt = allBelts[currentBeltIndex];
		if (belt == null) allBelts.RemoveAt(currentBeltIndex);
		else belt.ClearNullObjectsOnBelt();
		currentBeltIndex++;
	}
	private void FixedUpdate()
	{
		for (int i = 0; i < Objects.Count; i++)
		{
			var obj = Objects[i];
			if (obj.Count == 0) { obj.ResetAccum(); continue; }
			Rigidbody rb = obj.Rb;
			Vector3 vel = obj.SumVelocity / obj.Count;
			if (obj.RetainY) vel.y = rb.linearVelocity.y;
			else if (obj.BestY > 0f) vel.y = obj.BestY;
			rb.linearVelocity = vel;
			obj.ResetAccum();
		}
	}
	#endregion
}