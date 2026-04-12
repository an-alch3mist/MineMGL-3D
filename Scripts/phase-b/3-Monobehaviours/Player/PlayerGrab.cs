using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

using SPACE_UTIL;

public class PlayerGrab : MonoBehaviour
{
	#region inspector fields
	[SerializeField] Camera _cam;
	[SerializeField] Transform _holdPos;
	[SerializeField] GameObject _dragger;
	[SerializeField] LineRenderer _lrRope;
	[SerializeField] float _interactionRange = 2f;
	[SerializeField] LayerMask _interactionLayerMask;
	#endregion

	#region private API
	GameObject heldObject;
	SpringJoint grabJoint;
	float grabOriginalDrag;
	float grabOriginalAngularDrag;
	bool isAnyMenuOpen;
	#endregion

	#region Unity Life Cycle
	bool isFirstOpen = true;
	private void OnEnable()
	{
		if(isFirstOpen)
		{
			// do somthng
			GameEvents.OnMenuStateChanged += (isAnyMenuOpen) => this.isAnyMenuOpen = isAnyMenuOpen;
			this._lrRope.enabled = false;
			isFirstOpen = false;
		}
	}
	private void Update()
	{
		if (!isAnyMenuOpen)
		{
			if (INPUT.K.InstantDown(KeyCode.Mouse1))
				HandleTryGrab();
			if (heldObject != null)
				if (!heldObject.activeInHierarchy)
					HandleRelease();
		}
		HandleRope();
	}

	void HandleTryGrab()
	{
		if (heldObject != null || grabJoint != null)
		{
			HandleRelease();
			return;
		}
		if (!Physics.Raycast(_cam.transform.position, _cam.transform.forward, out RaycastHit hit, this._interactionRange, this._interactionLayerMask))
			return;
		if (!hit.collider.CompareTag("grabbable"))
			return;
		GrabObject(hit);
	}
	void GrabObject(RaycastHit hit)
	{
		heldObject = hit.collider.gameObject;
		Rigidbody rb = heldObject.GetComponent<Rigidbody>();
		grabJoint = _dragger.AddComponent<SpringJoint>();

		_dragger.SetActive(true);
		_dragger.transform.parent = _holdPos;
		_dragger.GetComponent<Rigidbody>().isKinematic = true;
		//
		SetUpSpringJointProperties(grabJoint, hit.point, connectedRb: rb);
		//
		UtilsPhaseB.IgnoreAllCollisions(heldObject, this.gameObject, true);
		//
		this._lrRope.positionCount = 2;
		this._lrRope.enabled = true;
		//
		grabOriginalDrag = rb.linearDamping;
		grabOriginalAngularDrag = rb.angularDamping;
		rb.isKinematic = false;
		rb.interpolation = RigidbodyInterpolation.Interpolate;
		rb.linearDamping = 2.5f;
		rb.angularDamping = 0.3f;
	}
	void SetUpSpringJointProperties(SpringJoint grabJoint, Vector3 point, Rigidbody connectedRb)
	{
		grabJoint.breakForce = 120f;
		grabJoint.breakTorque = 20f;
		grabJoint.transform.position = point;
		grabJoint.anchor = Vector3.zero;
		grabJoint.spring = 100f;
		grabJoint.damper = 32;
		grabJoint.minDistance = 0.1f;
		grabJoint.maxDistance = 0f;
		grabJoint.connectedBody = connectedRb;
		grabJoint.gameObject.transform.position = _holdPos.position;
	}
	void HandleRelease()
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
		this._lrRope.enabled = false;
	}
	IEnumerator DisableInterpolationLater(Rigidbody body)
	{
		yield return new WaitForSeconds(3f);
		if (body != null && (heldObject == null || heldObject.GetComponent<Rigidbody>() != body))
			body.interpolation = RigidbodyInterpolation.None;
	}
	void HandleRope()
	{
		if (grabJoint != null && heldObject != null)
		{
			this._lrRope.SetPosition(0, _dragger.transform.position);
			Vector3 anchorWorld = grabJoint.connectedBody.transform.TransformPoint(grabJoint.connectedAnchor);
			this._lrRope.SetPosition(1, anchorWorld);
			this._lrRope.enabled = true;
		}
		else
			this._lrRope.enabled = false;
	}
	#endregion

	#region public API
	public bool IsHoldingObject()
	{
		return heldObject != null;
	}
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
		this._lrRope.enabled = false;
	}
	#endregion
	#endregion
}
