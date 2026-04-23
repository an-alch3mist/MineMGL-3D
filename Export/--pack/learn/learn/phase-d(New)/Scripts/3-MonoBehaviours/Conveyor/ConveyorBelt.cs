using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// I push physics objects forward via trigger volume. When a Rigidbody enters my trigger collider
/// (OnTriggerEnter), I find or add a BasePhysicsObject component on it and add it to my internal
/// _physicsObjectsOnBelt list. Every FixedUpdate, I call AddConveyorVelocity(_pushVelocity) on
/// each object — this ACCUMULATES velocity (not sets it), so ore on multiple overlapping belts
/// gets averaged velocity from ConveyorBeltManager.FixedUpdate. When ore exits (OnTriggerExit),
/// I remove it from my list. I self-register in the static AllConveyorBelts list on OnEnable.
///
/// Subclasses: ConveyorBeltShaker (adds oscillation), ConveyorBeltShakerHorizontal (lateral only).
///
/// Who uses me: ConveyorBeltManager (round-robin ClearNullObjectsOnBelt), BasePhysicsObject (accumulates velocity).
/// Events I fire: none. Events I subscribe to: none.
/// Static list: AllConveyorBelts — used by ConveyorBeltManager and ConveyorRenderer.
/// </summary>
public class ConveyorBelt : MonoBehaviour
{
	#region Inspector Fields
	[SerializeField] float _speed = 0.8f;
	[SerializeField] bool _disabled;
	[SerializeField] bool _retainYVelocity;
	#endregion

	#region public API — getters/setters (ConveyorBlocker reads/writes Disabled, subclasses read Speed)
	public float GetSpeed() => _speed;
	public void SetSpeed(float val) { _speed = val; _pushVelocity = transform.forward * _speed; }
	public bool GetDisabled() => _disabled;
	public void SetDisabled(bool val) => _disabled = val;
	public bool GetRetainYVelocity() => _retainYVelocity;
	#endregion

	#region private API
	protected List<BasePhysicsObject> _physicsObjectsOnBelt = new List<BasePhysicsObject>();
	protected Vector3 _pushVelocity;
	#endregion

	#region public API
	public static List<ConveyorBelt> AllConveyorBelts { get; private set; } = new List<ConveyorBelt>();

	/// <summary> change speed at runtime </summary>
	public void ChangeSpeed(float newSpeed)
	{
		_speed = newSpeed;
		_pushVelocity = transform.forward * _speed;
	}
	/// <summary> add a physics object to this belt's push list </summary>
	public void AddPhysicsObject(BasePhysicsObject obj)
	{
		_physicsObjectsOnBelt.Add(obj);
		obj.AddTouchingConveyorBelt(this);
	}
	/// <summary> remove a physics object from this belt's push list </summary>
	public void RemovePhysicsObject(BasePhysicsObject obj) => _physicsObjectsOnBelt.Remove(obj);
	/// <summary> remove null/inactive/kinematic entries — called by ConveyorBeltManager round-robin </summary>
	public void ClearNullObjectsOnBelt()
	{
		for (int i = _physicsObjectsOnBelt.Count - 1; i >= 0; i--)
		{
			var obj = _physicsObjectsOnBelt[i];
			if (obj == null || !obj.isActiveAndEnabled || obj.Rb.isKinematic)
				_physicsObjectsOnBelt.RemoveAt(i);
		}
	}
	#endregion

	#region Unity Life Cycle
	protected virtual void OnEnable()
	{
		_pushVelocity = transform.forward * Speed;
		AllConveyorBelts.Add(this);
	}
	protected virtual void OnDisable()
	{
		foreach (var obj in _physicsObjectsOnBelt)
			if (obj != null) obj.RemoveTouchingConveyorBelt(this);
		_physicsObjectsOnBelt.Clear();
		AllConveyorBelts.Remove(this);
	}
	protected virtual void FixedUpdate()
	{
		if (_disabled || _physicsObjectsOnBelt.Count == 0) return;
		for (int i = _physicsObjectsOnBelt.Count - 1; i >= 0; i--)
			_physicsObjectsOnBelt[i].AddConveyorVelocity(_pushVelocity, _retainYVelocity);
	}
	private void OnTriggerEnter(Collider other)
	{
		var rb = other.attachedRigidbody;
		if (rb == null || rb.isKinematic) return;
		var obj = rb.GetComponent<BasePhysicsObject>();
		if (obj == null) obj = rb.gameObject.AddComponent<BasePhysicsObject>();
		if (obj != null) AddPhysicsObject(obj);
	}
	private void OnTriggerExit(Collider other)
	{
		var rb = other.attachedRigidbody;
		if (rb == null) return;
		var obj = rb.GetComponent<BasePhysicsObject>();
		if (obj != null)
		{
			_physicsObjectsOnBelt.Remove(obj);
			obj.RemoveTouchingConveyorBelt(this);
		}
	}
	#endregion
}