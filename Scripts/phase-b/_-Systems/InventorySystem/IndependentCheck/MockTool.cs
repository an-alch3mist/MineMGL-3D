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
	[SerializeField] Vector3 _equippedOffset = new Vector3(0.4f, -0.3f, 0f); // right + down (like FPS viewmodel)
	[SerializeField] Vector3 _dropOffset = new Vector3(0.5f, -0.3f, 1.5f); // right + down + forward
	int qty = 1;
	Renderer rend;
	Camera ownerCam;
	Transform ownerViewModelContainer;
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
	public void SetOwnerContext(Camera cam, Transform vmc, Transform mag) { ownerCam = cam; ownerViewModelContainer = vmc; }

	/// <summary> Drop one from stack — exact copy with ALL components, values, and behavior.
	/// Clone is a fully functional MockTool that can be re-picked from the world. </summary>
	public void DropOneFromStack(Camera cam)
	{
		var clone = Instantiate(gameObject);
		// → reset clone to fresh world state (not equipped, qty=1, full scale)
		var cloneMock = clone.GetComponent<MockTool>();
		if (cloneMock != null)
		{
			cloneMock.isEquipped = false;
			cloneMock.qty = 1;
			cloneMock.ownerCam = null;
			cloneMock.ownerViewModelContainer = null;
		}
		clone.name = $"{_name}_world";
		clone.SetActive(true);
		clone.transform.parent = null;
		clone.transform.localScale = originalScale;
		var rend2 = clone.GetComponent<Renderer>();
		if (rend2 != null) rend2.enabled = true;
		var col = clone.GetComponent<Collider>();
		if (col != null) col.enabled = true;
		var rb = clone.GetComponent<Rigidbody>();
		if (rb == null) rb = clone.AddComponent<Rigidbody>();
		rb.isKinematic = false;
		if (cam != null)
		{
			clone.transform.position = cam.transform.position
				+ cam.transform.right * _dropOffset.x
				+ cam.transform.up * _dropOffset.y
				+ cam.transform.forward * _dropOffset.z;
			rb.linearVelocity = cam.transform.forward * 5f + Vector3.up * 2f;
		}
		Debug.Log($"[MockTool] Dropped 1 of {_name}, remaining: {qty}");
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
		if (col != null) col.enabled = false;
		// → parent to ViewModelContainer (moves with camera automatically, no LateUpdate needed)
		if (ownerViewModelContainer != null)
		{
			transform.parent = ownerViewModelContainer;
			transform.localPosition = new Vector3(_equippedOffset.x, _equippedOffset.y, _equippedDistance);
			transform.localRotation = Quaternion.identity;
		}
		Debug.Log($"[MockTool] Equipped: {_name}");
	}

	/// <summary> Drop: position in front of camera, restore scale, re-enable collider + physics, apply forward velocity. </summary>
	public void DropItem()
	{
		isEquipped = false;
		gameObject.SetActive(true);
		if (rend != null) rend.enabled = true;
		transform.localScale = originalScale;
		transform.parent = null;
		var col = GetComponent<Collider>();
		if (col != null) col.enabled = true;
		var rb = GetComponent<Rigidbody>();
		if (rb != null) rb.isKinematic = false;
		// → position at offset from camera (right + down + forward) + apply forward velocity
		if (ownerCam != null)
		{
			transform.position = ownerCam.transform.position
				+ ownerCam.transform.right * _dropOffset.x
				+ ownerCam.transform.up * _dropOffset.y
				+ ownerCam.transform.forward * _dropOffset.z;
			if (rb != null) rb.linearVelocity = ownerCam.transform.forward * 5f + Vector3.up * 2f;
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

	// No LateUpdate needed — parented to ViewModelContainer, moves with camera automatically.

	// ── Click to pickup (simple — no interaction wheel needed) ──
	void OnMouseDown()
	{
		if (!gameObject.activeInHierarchy) return;
		GameEvents.RaiseToolPickupRequested(this);
		Debug.Log($"[MockTool] Pickup requested: {_name}");
	}
}