using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// I pull nearby Grabbable physics objects toward me using SpringJoints. When the player holds
/// right-click, I OverlapSphere at my pull origin and create a MagnetAnchor GO with a SpringJoint
/// for each nearby Rigidbody — they fly toward me. Left-click launches them all forward (push force).
/// R drops them gently. Q cycles my selection mode (Everything/NotInFilter/NotOnConveyors) and
/// updates the TMP text on my ViewModel. I clean up broken joints every FixedUpdate, and track
/// recently dropped bodies to disable interpolation after 3 seconds. DetachBody is called by
/// OrePiece when it enters a machine (Phase C+) so it releases from me cleanly.
///
/// Who uses me: InventoryOrchestrator (SecondaryFireHeld, PrimaryFire, Reload, QButtonPressed).
/// Who calls DetachBody: OrePiece.SellAfterDelay, future machine triggers.
/// Events I fire: none.
/// </summary>
public class ToolMagnet : BaseHeldTool
{
	#region Inspector Fields
	[SerializeField] float _pullRadius = 2f;
	[SerializeField] float _pushForce = 3f;
	[SerializeField] float _dropForce = 1f;
	[SerializeField] Transform _pullOrigin;
	[SerializeField] LayerMask _grabbableLayer;
	[SerializeField] TMP_Text _selectionModeText;
	#endregion

	#region private API
	struct DroppedBodyInfo { public Rigidbody Rb; public float Timer; }
	MagnetToolSelectionMode selectionMode;
	List<Rigidbody> heldBodies = new List<Rigidbody>();
	List<SpringJoint> joints = new List<SpringJoint>();
	List<GameObject> anchors = new List<GameObject>();
	bool wantsToMagnet;
	Vector3 pullTargetVelocity;
	Vector3 lastPlayerPos;
	Vector3 playerVelocity;
	readonly List<DroppedBodyInfo> droppedBodies = new List<DroppedBodyInfo>();

	void CleanupBrokenJoints()
	{
		for (int i = joints.Count - 1; i >= 0; i--)
		{
			if (joints[i] == null || joints[i].connectedBody == null)
			{
				if (i < anchors.Count && anchors[i] != null) Destroy(anchors[i]);
				if (i < heldBodies.Count)
				{
					Rigidbody rb = heldBodies[i];
					if (rb != null) { rb.linearDamping = BasePhysicsObject.STANDARD_LINEAR_DAMPING; rb.angularDamping = BasePhysicsObject.STANDARD_ANGULAR_DAMPING; }
					heldBodies.RemoveAt(i);
				}
				joints.RemoveAt(i);
				anchors.RemoveAt(i);
			}
		}
	}
	void GrabNearbyObjects()
	{
		Collider[] cols = Physics.OverlapSphere(_pullOrigin.position, _pullRadius, _grabbableLayer);
		foreach (var col in cols)
		{
			Rigidbody rb = col.attachedRigidbody;
			if (rb == null || heldBodies.Contains(rb)) continue;

			GameObject anchor = new GameObject("MagnetAnchor");
			anchor.transform.position = _pullOrigin.position;
			anchor.transform.parent = _pullOrigin;
			anchor.AddComponent<Rigidbody>().isKinematic = true;
			SpringJoint sj = anchor.AddComponent<SpringJoint>();
			sj.connectedBody = rb;
			sj.autoConfigureConnectedAnchor = false;
			sj.connectedAnchor = rb.transform.InverseTransformPoint(col.transform.position);
			sj.spring = 100f; sj.damper = 25f; sj.maxDistance = 0.01f;
			sj.breakForce = 120f; sj.breakTorque = 20f;
			rb.linearDamping = 3f; rb.angularDamping = 1.5f;
			rb.interpolation = RigidbodyInterpolation.Interpolate;
			if (owner != null) UtilsPhaseB.IgnoreAllCollisions(rb.gameObject, owner.gameObject, true);
			heldBodies.Add(rb);
			joints.Add(sj);
			anchors.Add(anchor);
		}
		wantsToMagnet = false;
	}
	void DropObjects(float force)
	{
		for (int i = 0; i < joints.Count; i++)
			if (joints[i] != null) Destroy(joints[i].gameObject);
		joints.Clear();
		anchors.Clear();
		foreach (var rb in heldBodies)
		{
			if (rb == null) continue;
			if (owner != null) rb.AddForce(owner.PlayerCam.transform.forward * force, ForceMode.Impulse);
			rb.linearDamping = BasePhysicsObject.STANDARD_LINEAR_DAMPING;
			rb.angularDamping = BasePhysicsObject.STANDARD_ANGULAR_DAMPING;
			if (owner != null) UtilsPhaseB.IgnoreAllCollisions(rb.gameObject, owner.gameObject, false);
			droppedBodies.Add(new DroppedBodyInfo { Rb = rb, Timer = 3f });
		}
		heldBodies.Clear();
	}
	void CycleSelectionMode()
	{
		MagnetToolSelectionMode[] modes = (MagnetToolSelectionMode[])Enum.GetValues(typeof(MagnetToolSelectionMode));
		int next = (Array.IndexOf(modes, selectionMode) + 1) % modes.Length;
		selectionMode = modes[next];
		if (_selectionModeText != null) _selectionModeText.text = selectionMode.ToString();
	}
	void CleanupDroppedBodies()
	{
		for (int i = droppedBodies.Count - 1; i >= 0; i--)
		{
			var info = droppedBodies[i];
			if (info.Rb == null) { droppedBodies.RemoveAt(i); continue; }
			info.Timer -= Time.fixedDeltaTime;
			if (info.Timer <= 0f) { info.Rb.interpolation = RigidbodyInterpolation.None; droppedBodies.RemoveAt(i); }
			else droppedBodies[i] = info;
		}
	}
	#endregion

	#region extra
	// nice-to-have: DetachBody — Phase C OrePiece calls this when entering machines to release from magnet
	public void DetachBody(Rigidbody rb)
	{
		if (rb == null) return;
		int idx = heldBodies.IndexOf(rb);
		if (idx < 0) return;
		if (idx < joints.Count && joints[idx] != null) Destroy(joints[idx].gameObject);
		if (idx < anchors.Count && anchors[idx] != null) Destroy(anchors[idx]);
		joints.RemoveAt(idx);
		anchors.RemoveAt(idx);
		heldBodies.RemoveAt(idx);
		rb.linearDamping = BasePhysicsObject.STANDARD_LINEAR_DAMPING;
		rb.angularDamping = BasePhysicsObject.STANDARD_ANGULAR_DAMPING;
		if (owner != null) UtilsPhaseB.IgnoreAllCollisions(rb.gameObject, owner.gameObject, false);
		droppedBodies.Add(new DroppedBodyInfo { Rb = rb, Timer = 3f });
	}
	// nice-to-have: selection mode TMP display — shows current grab filter on tool viewmodel
	// wired via [SerializeField] TMP_Text _selectionModeText above
	#endregion

	#region public API — overrides
	public override void SecondaryFireHeld() => wantsToMagnet = true;
	public override void PrimaryFire() => DropObjects(_pushForce);
	public override void Reload() => DropObjects(_dropForce);
	public override void QButtonPressed() => CycleSelectionMode();
	public override void DropItem() { DropObjects(_dropForce); base.DropItem(); }
	#endregion

	#region Unity Life Cycle
	protected override void OnEnable() { base.OnEnable(); if (_selectionModeText != null) _selectionModeText.text = selectionMode.ToString(); }
	protected override void OnDisable() { base.OnDisable(); DropObjects(_dropForce); }
	private void FixedUpdate()
	{
		if (owner == null) return;
		Vector3 pos = owner.transform.position;
		playerVelocity = (pos - lastPlayerPos) / Time.fixedDeltaTime;
		lastPlayerPos = pos;
		Vector3 target = owner.MagnetToolPosition.position + playerVelocity * Time.fixedDeltaTime * 10f;
		_pullOrigin.position = Vector3.SmoothDamp(_pullOrigin.position, target, ref pullTargetVelocity, 0.03f, 10f, Time.fixedDeltaTime);
		CleanupBrokenJoints();
		CleanupDroppedBodies();
		if (wantsToMagnet) GrabNearbyObjects();
	}
	#endregion
}