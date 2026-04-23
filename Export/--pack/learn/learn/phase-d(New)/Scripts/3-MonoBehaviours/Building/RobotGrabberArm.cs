using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// I'm an automated arm that grabs ore pieces from a trigger zone, moves them along an arc path
/// to a target position, then drops them. Used on machine buildings to feed ore from one belt
/// to another or into a machine input. I filter by ResourceType + PieceType.
///
/// How I work: OnTriggerEnter adds ore to _orePiecesInRange. SelectNewTarget picks the first
/// valid ore (not held by magnet, not MarkedForDestruction), tags it "MarkedForDestruction" so
/// other grabbers/sellers ignore it. Update moves IKTarget toward the ore, once close enough
/// I grab it (make kinematic, parent to nothing, start arc movement). At the target position
/// I release it (un-kinematic, re-tag Grabbable) and select a new target.
///
/// Who uses me: Self (Update + trigger). No external callers.
/// Events I fire: none.
/// </summary>
public class RobotGrabberArm : MonoBehaviour
{
	#region Inspector Fields
	[SerializeField] ResourceType _filterResourceType;
	[SerializeField] PieceType _filterPieceType;
	[SerializeField] Transform _origin;
	[SerializeField] Transform _ikTarget;
	[SerializeField] Transform _targetPosition;
	[SerializeField] float _moveSpeed = 5f;
	[SerializeField] float _rotateSpeed = 10f;
	[SerializeField] float _grabDistance = 0.2f;
	[SerializeField] float _releaseDistance = 0.1f;
	[SerializeField] float _arcHeight = 0.5f;
	#endregion

	#region private API
	HashSet<OrePiece> orePiecesInRange = new HashSet<OrePiece>();
	Transform targetOrePiece;
	bool isGrabbing;
	Rigidbody grabbedRb;
	Vector3 grabStartPos;
	float grabProgress;

	/// <summary> selects the first valid ore piece from the trigger zone — marks it MarkedForDestruction </summary>
	void SelectNewTarget()
	{
		orePiecesInRange.RemoveWhere(ore => ore == null);
		var valid = orePiecesInRange.Where(ore =>
			ore.CurrentMagnetTool == null && !ore.gameObject.CompareTag("MarkedForDestruction"));
		if (valid.Any())
		{
			var ore = valid.First();
			targetOrePiece = ore.transform;
			targetOrePiece.gameObject.tag = "MarkedForDestruction";
			orePiecesInRange.Remove(ore);
		}
	}
	/// <summary> releases the currently held ore — un-kinematic, re-tag, clear references </summary>
	void DropObject()
	{
		if (grabbedRb != null) { grabbedRb.isKinematic = false; grabbedRb = null; }
		if (targetOrePiece != null)
		{
			targetOrePiece.gameObject.tag = "Grabbable";
			targetOrePiece = null;
			isGrabbing = false;
		}
	}
	/// <summary> returns a point along a parabolic arc from start to end at progress t </summary>
	Vector3 GetArcPosition(Vector3 start, Vector3 end, float height, float t)
	{
		t = Mathf.Clamp01(t);
		Vector3 linear = Vector3.Lerp(start, end, t);
		float arc = 4f * height * t * (1f - t);
		return linear + Vector3.up * arc;
	}
	#endregion

	#region Unity Life Cycle
	/// <summary> every frame: move IK arm toward target, grab when close, arc-move to drop point </summary>
	private void Update()
	{
		if (targetOrePiece == null) return;
		Vector3 toTarget = targetOrePiece.position - _origin.position;
		// → if too far and not grabbing, drop
		if (!isGrabbing && toTarget.magnitude > 3f) { DropObject(); return; }
		// → move IK toward ore
		Vector3 aimPos = targetOrePiece.position - toTarget.normalized * 0.2f;
		_ikTarget.position = Vector3.MoveTowards(_ikTarget.position, aimPos, _moveSpeed * Time.deltaTime);
		if (toTarget.normalized != Vector3.zero)
		{
			Quaternion lookRot = Quaternion.LookRotation(toTarget.normalized, Vector3.forward);
			_ikTarget.rotation = Quaternion.Slerp(_ikTarget.rotation, lookRot, _rotateSpeed * Time.deltaTime);
		}
		// → grab when close enough
		if (!isGrabbing && Vector3.Distance(_ikTarget.position, targetOrePiece.position) < _grabDistance)
		{
			isGrabbing = true;
			grabbedRb = targetOrePiece.GetComponent<Rigidbody>();
			if (grabbedRb != null) grabbedRb.isKinematic = true;
			targetOrePiece.SetParent(null);
			grabStartPos = targetOrePiece.position;
			grabProgress = 0f;
		}
		// → while grabbing: move along arc toward target position
		if (isGrabbing)
		{
			grabProgress += Time.deltaTime * _moveSpeed / Vector3.Distance(grabStartPos, _targetPosition.position);
			Vector3 arcPos = GetArcPosition(grabStartPos, _targetPosition.position, _arcHeight, grabProgress);
			targetOrePiece.position = arcPos;
			// → at destination: drop + select next
			if (Vector3.Distance(arcPos, _targetPosition.position) < _releaseDistance)
			{
				targetOrePiece.position = _targetPosition.position;
				DropObject();
				SelectNewTarget();
			}
		}
	}
	/// <summary> release grabbed ore when disabled (building packed/destroyed) </summary>
	private void OnDisable() => DropObject();
	/// <summary> ore enters trigger range — add to candidates, auto-select if none targeted </summary>
	private void OnTriggerEnter(Collider other)
	{
		var ore = other.GetComponent<OrePiece>();
		if (ore == null) return;
		orePiecesInRange.Add(ore);
		if (targetOrePiece == null) SelectNewTarget();
	}
	/// <summary> ore exits trigger range — remove from candidates </summary>
	private void OnTriggerExit(Collider other)
	{
		var ore = other.GetComponent<OrePiece>();
		if (ore != null) orePiecesInRange.Remove(ore);
	}
	#endregion
}