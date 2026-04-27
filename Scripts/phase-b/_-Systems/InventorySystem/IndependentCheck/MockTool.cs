using UnityEngine;

/// <summary>
/// Lightweight IInventoryItem for testing inventory WITHOUT any tool scripts.
/// Place on a cube in the scene → click to fire RaiseToolPickupRequested → inventory picks it up.
/// Zero dependency on BaseHeldTool, ToolSystem, or PlayerSystem.
///
/// HOW IT WORKS with the Orchestrator:
///   Pickup: Orchestrator calls SetActive(false) → cube disappears from world
///   Equip:  Orchestrator calls SetActive(true) + OnEquipped()
///           → OnEquipped hides renderer (no ViewModel to show — just "in hand" logically)
///   Switch: Orchestrator calls SetActive(false) on previous, SetActive(true) + OnEquipped on new
///   Drop:   DropItem() → SetActive(true), show renderer, apply velocity → cube back in world
/// </summary>
public class MockTool : MonoBehaviour, IInventoryItem
{
	[SerializeField] string _name = "MockPickaxe";
	[SerializeField] Sprite _icon;
	[SerializeField] int _maxStack = 1;
	[SerializeField] Color _color = Color.white;
	[SerializeField] float _equippedScaleMultiplier = 0.3f;
	[SerializeField] float _equippedDistance = 1.5f;
	int qty = 1;
	Renderer rend;
	Camera ownerCam;
	Vector3 originalScale;
	bool isEquipped;

	void Awake() { rend = GetComponent<Renderer>(); originalScale = transform.localScale; }
	void Start() { if (rend != null) rend.material.color = _color; }

	// ── IInventoryItem ──
	public string GetName() => _name;
	public string GetDescription() => $"A mock {_name} for testing";
	public int GetQty() => qty;
	public void SetQty(int v) => qty = v;
	public void AddQty(int delta) => qty += delta;
	public int GetMaxAmount() => _maxStack;
	public bool GetShouldEquipWhenPickedUp() => true;
	public Sprite GetIcon() => _icon;
	public GameObject GetGameObject() => gameObject;
	public string GetEquipButtonLabel() => "Equip";
	public void HandleActiveInput() { /* no-op — mock has no actions */ }
	public void SetOwnerContext(Camera cam, Transform vmc, Transform mag) { ownerCam = cam; }

	/// <summary> Drop one from stack — instantiate a visual clone cube that drops with physics. </summary>
	public void DropOneFromStack(Camera cam)
	{
		var clone = GameObject.CreatePrimitive(PrimitiveType.Cube);
		clone.transform.localScale = originalScale;
		clone.GetComponent<Renderer>().material.color = _color;
		clone.AddComponent<Rigidbody>();
		if (cam != null)
		{
			clone.transform.position = cam.transform.position + cam.transform.forward * 1.5f;
			clone.GetComponent<Rigidbody>().linearVelocity = cam.transform.forward * 5f + Vector3.up * 2f;
		}
		Destroy(clone, 5f); // auto-cleanup after 5 seconds
		Debug.Log($"[MockTool] Dropped 1 clone of {_name}, remaining: {qty}");
	}

	/// <summary> Equip: fire RaiseItemEquipped (so ItemEquipBridge sends camera via SetOwnerContext),
	/// then shrink cube + float in front of camera via LateUpdate.
	/// Disables collider so equipped item doesn't physically interact with world objects. </summary>
	public void OnEquipped()
	{
		isEquipped = true;
		GameEvents.RaiseItemEquipped(this);
		if (rend != null) rend.enabled = true;
		transform.localScale = originalScale * _equippedScaleMultiplier;
		var rb = GetComponent<Rigidbody>();
		if (rb != null) rb.isKinematic = true;
		var col = GetComponent<Collider>();
		if (col != null) col.enabled = false; // no physics interaction while equipped
		Debug.Log($"[MockTool] Equipped: {_name}");
	}

	/// <summary> Drop: restore scale, re-enable collider + physics, apply velocity. </summary>
	public void DropItem()
	{
		isEquipped = false;
		gameObject.SetActive(true);
		if (rend != null) rend.enabled = true;
		transform.localScale = originalScale;
		transform.parent = null;
		var col = GetComponent<Collider>();
		if (col != null) col.enabled = true; // re-enable physics interaction
		var rb = GetComponent<Rigidbody>();
		if (rb != null)
		{
			rb.isKinematic = false;
			if (ownerCam != null)
				rb.linearVelocity = ownerCam.transform.forward * 5f + Vector3.up * 2f;
			else
				rb.linearVelocity = Vector3.up * 3f + Vector3.forward * 2f;
		}
		ownerCam = null;
		Debug.Log($"[MockTool] Dropped: {_name}");
	}

	/// <summary> When GO deactivates (stored in inventory), restore scale for when it comes back. </summary>
	void OnDisable()
	{
		isEquipped = false;
		transform.localScale = originalScale;
		if (rend != null) rend.enabled = true;
	}

	/// <summary> Float in front of camera while equipped (like Proto_InventoryFull). </summary>
	void LateUpdate()
	{
		if (isEquipped && ownerCam != null)
		{
			transform.position = ownerCam.transform.position + ownerCam.transform.forward * _equippedDistance + Vector3.down * 0.3f;
			transform.rotation = ownerCam.transform.rotation;
		}
	}

	// ── Click to pickup (simple — no interaction wheel needed) ──
	void OnMouseDown()
	{
		if (!gameObject.activeInHierarchy) return;
		GameEvents.RaiseToolPickupRequested(this);
		Debug.Log($"[MockTool] Pickup requested: {_name}");
	}
}