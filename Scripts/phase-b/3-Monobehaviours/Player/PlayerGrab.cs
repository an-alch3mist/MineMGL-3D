using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

using SPACE_UTIL;

/// <summary>
/// I grab physics objects when the player right-clicks on something tagged "Grabbable".
/// I create a SpringJoint between a kinematic RigidbodyDragger and the target's Rigidbody,
/// increase drag for a heavy feel, and draw a LineRenderer rope between hand and object.
/// Right-click again releases — I destroy the joint, restore drag, disable the rope.
/// If the player pulls too far, the SpringJoint breaks and RigidbodyDraggerController calls
/// my ForceRelease(). I also subscribe to OnToolEquipped to set tool.Owner = PlayerMovement,
/// which decouples InventoryOrchestrator from needing a PlayerMovement reference.
///
/// Who uses me: RigidbodyDraggerController (ForceRelease on joint break).
/// Events I subscribe to: OnMenuStateChanged (block grab), OnToolEquipped (set tool.Owner).
/// </summary>
public class PlayerGrab : MonoBehaviour
{
	#region Inspector Fields
	[SerializeField] Camera _cam;
	[SerializeField] Transform _holdPos;
	[SerializeField] GameObject _dragger;
	[SerializeField] LineRenderer _rope;
	[SerializeField] float _interactRange = 2.5f;
	[SerializeField] LayerMask _grabbableMask;
	[SerializeField] PlayerController _pc;
	#endregion

	#region private API
	GameObject heldObject;
	SpringJoint grabJoint;
	float grabOriginalDrag;
	float grabOriginalAngularDrag;
	bool isAnyMenuOpen;
	#endregion

	#region public API
	/// <summary> true if currently holding an object </summary>
	public bool IsHolding => heldObject != null;
	#endregion

	#region extra
	// nice-to-have: RigidbodyDraggerController calls this when SpringJoint breaks — auto-releases orphan grab
	public void ForceRelease()
	{
		if (grabJoint != null)
		{
			grabJoint.connectedBody = null;
			Destroy(grabJoint);
			grabJoint = null;
			_dragger.SetActive(false);
			if (heldObject != null)
			{
				Rigidbody rb = heldObject.GetComponent<Rigidbody>();
				rb.linearDamping = grabOriginalDrag;
				rb.angularDamping = grabOriginalAngularDrag;
				UtilsPhaseB.IgnoreAllCollisions(heldObject, gameObject, false);
				StartCoroutine(DisableInterpolationLater(rb));
			}
		}
		heldObject = null;
		_rope.enabled = false;
	}
	#endregion

	#region Unity Life Cycle
	/// <summary> Subscribes to menu state (blocks grab when open) and tool equip (sets tool.Owner
	/// to PlayerMovement so tools can access camera for raycasts). Disables rope on start. </summary>
	private void Start()
	{
		// purpose: block grab when menu is open
		GameEvents.OnMenuStateChanged += (open) => isAnyMenuOpen = open;
		// purpose: set tool.Owner when equipped — decouples InventoryOrchestrator from PlayerMovement
		GameEvents.OnToolEquipped += (tool) => tool.owner = _pc;
		_rope.enabled = false;
	}
	/// <summary> Every frame: right-click triggers grab/release, updates rope line positions
	/// between dragger and held object, and auto-releases if the held object was deactivated. </summary>
	private void Update()
	{
		// → right-click toggles grab/release
		if (INPUT.K.InstantDown(KeyCode.Mouse1))
			TryGrab();

		if (heldObject != null && !heldObject.activeInHierarchy)
			Release();

		if (grabJoint != null && heldObject != null)
		{
			_rope.SetPosition(0, _dragger.transform.position);
			Vector3 anchorWorld = grabJoint.connectedBody.transform.TransformPoint(grabJoint.connectedAnchor);
			_rope.SetPosition(1, anchorWorld);
			_rope.enabled = true;
		}
		else _rope.enabled = false;
	}
	/// <summary> If menu is open, does nothing. If already holding, releases. Otherwise raycasts
	/// from camera — if it hits a Grabbable-tagged object, starts the grab via GrabObject. </summary>
	void TryGrab()
	{
		Debug.Log(C.method(this, "lime"));
		// → blocked when any menu panel is open
		if (isAnyMenuOpen) return;
		// → already holding? release instead
		Debug.Log(C.method(this, "lime", adMssg: "no menu open"));
		if (heldObject != null || grabJoint != null)
		{ Release(); return; }
		if (Physics.Raycast(_cam.transform.position, _cam.transform.forward, out RaycastHit hit, _interactRange, _grabbableMask))
		{
			Debug.Log(C.method(this, "lime", adMssg: "hit the grababble layer mask"));
			GrabObject(hit);
		}
		// if (!hit.collider.HasTag(TagType.grabbable)) return;
	}
	/// <summary> Creates a SpringJoint between the kinematic dragger and the target’s Rigidbody.
	/// Increases drag for a heavy feel, enables interpolation for smooth visuals, ignores
	/// collisions between player and grabbed object, and enables the rope LineRenderer. </summary>
	void GrabObject(RaycastHit hit)
	{
		heldObject = hit.collider.gameObject;
		Rigidbody rb = heldObject.GetComponent<Rigidbody>();

		_dragger.SetActive(true);
		_dragger.transform.parent = _holdPos;
		grabJoint = _dragger.AddComponent<SpringJoint>();
		_dragger.GetComponent<Rigidbody>().isKinematic = true;

		rb.isKinematic = false;
		UtilsPhaseB.IgnoreAllCollisions(heldObject, gameObject, true);
		rb.interpolation = RigidbodyInterpolation.Interpolate;
		grabJoint.breakForce = 120f;
		grabJoint.breakTorque = 20f;
		grabJoint.transform.position = hit.point;
		grabJoint.anchor = Vector3.zero;
		grabJoint.spring = 100f;
		grabJoint.damper = 25f;
		grabJoint.maxDistance = 0f;
		grabJoint.connectedBody = rb;
		grabJoint.gameObject.transform.position = _holdPos.position;
		_rope.positionCount = 2;
		_rope.enabled = true;
		grabOriginalDrag = rb.linearDamping;
		grabOriginalAngularDrag = rb.angularDamping;
		rb.linearDamping = 2.5f;
		rb.angularDamping = 0.3f;
	}
	/// <summary> Destroys the SpringJoint, deactivates the dragger, restores the object’s original
	/// drag values, re-enables collisions, hides the rope, and starts a 3s coroutine to disable
	/// interpolation on the released object (saves CPU when idle). </summary>
	void Release()
	{
		if (isAnyMenuOpen) return;
		if (grabJoint != null)
		{
			grabJoint.connectedBody = null;
			Destroy(grabJoint);
			grabJoint = null;
			_dragger.SetActive(false);
			if (heldObject != null)
			{
				Rigidbody rb = heldObject.GetComponent<Rigidbody>();
				rb.linearDamping = grabOriginalDrag;
				rb.angularDamping = grabOriginalAngularDrag;
				UtilsPhaseB.IgnoreAllCollisions(heldObject, gameObject, false);
				StartCoroutine(DisableInterpolationLater(rb));
			}
		}
		heldObject = null;
		_rope.enabled = false;
	}
	IEnumerator DisableInterpolationLater(Rigidbody body)
	{
		yield return new WaitForSeconds(3f);
		if (body != null && (heldObject == null || heldObject.GetComponent<Rigidbody>() != body))
			body.interpolation = RigidbodyInterpolation.None;
	}
	#endregion
}