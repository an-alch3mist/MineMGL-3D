using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HighlightPlus;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

using SPACE_UTIL;

/// <summary>
/// I'm the base class every equippable tool inherits from (ToolPickaxe, ToolMagnet, etc.).
/// I handle the common lifecycle: when picked up via "Take" interaction, I fire RaiseToolPickupRequested
/// so InventoryOrchestrator adds me to a hotbar slot. When equipped, my ViewModel shows in front of
/// the camera (parented to Owner.ViewModelContainer). When dropped (G key), I unparent, show my
/// WorldModel, and fly forward with velocity. Subclasses override PrimaryFire, SecondaryFire, etc.
/// Owner (PlayerMovement) is set by PlayerGrab via OnToolEquipped event — I never set it myself.
/// I implement IInteractable (for interaction wheel), IInventoryItem (inventory slot), and ISaveLoadableObject (stub for Phase G).
///
/// Who uses me: InventoryOrchestrator (equip/drop/switch), PlayerGrab (sets Owner via event).
/// Events I fire: RaiseToolPickupRequested (on "Take" interaction).
/// </summary>
[AddComponentMenu("MineMGL/Tools/BaseHeldTool")]
public class BaseHeldTool : BaseSellableItem, IInteractable, ISaveLoadableObject, IHighlightable, IInventoryItem
{
	#region Inspector Fields
	[SerializeField] SavableObjectID _savableObjectID;
	[SerializeField] string _name = "test";
	[TextArea] [SerializeField] string _description = "description";
	[SerializeField] Sprite _programmerIcon;
	[SerializeField] Sprite _inventoryIcon;
	[SerializeField] int _quantity = 1;
	[SerializeField] int _maxAmount = 1;
	[SerializeField] bool _equipWhenPickedUp;
	[SerializeField] bool _shouldUseInteractionWheel = true;
	[SerializeField] protected GameObject _worldModel;
	[SerializeField] protected GameObject _viewModel;
	[SerializeField] protected Animator _viewModelAnimator;
	[SerializeField] List<SO_InteractionOption> _interactions;
	[Header("Highlight")]
	[SerializeField] HighlightProfile _highlightProfile;
	#endregion

	#region private API
	protected Camera ownerCam;
	protected Transform ownerViewModelContainer;
	protected Transform ownerMagnetToolPos;
	#endregion

	#region public API — IInventoryItem
	public string GetName() => _name;
	public string GetDescription() => _description;
	public int GetQty() => _quantity;
	public void SetQty(int val) => _quantity = val;
	public void AddQty(int delta) => _quantity += delta;
	public int GetMaxAmount() => _maxAmount;
	public bool GetShouldEquipWhenPickedUp() => _equipWhenPickedUp;
	public Sprite GetIcon() => _inventoryIcon ?? _programmerIcon;
	public GameObject GetGameObject() => gameObject;
	/// <summary> receives camera + viewmodel container + magnet pos from PlayerGrab via IInventoryItem.
	/// Tool never knows PlayerMovement exists — only stores the transforms it needs. </summary>
	public void SetOwnerContext(Camera cam, Transform viewModelContainer, Transform magnetToolPos)
	{
		ownerCam = cam;
		ownerViewModelContainer = viewModelContainer;
		ownerMagnetToolPos = magnetToolPos;
	}
	/// <summary> called when inventory equips me — fires OnItemEquipped so PlayerGrab sends context </summary>
	public virtual void OnEquipped() => GameEvents.RaiseItemEquipped(this);
	/// <summary> label for the info panel equip button — overridden by ToolBuilder to "Build" </summary>
	public virtual string GetEquipButtonLabel() => "Equip";
	/// <summary> per-frame input routing when I'm the active item — routes LMB/RMB/R/Q to virtual methods </summary>
	public virtual void HandleActiveInput()
	{
		if (INPUT.K.InstantDown(KeyCode.Mouse0)) PrimaryFire();
		if (Input.GetMouseButton(0)) PrimaryFireHeld();
		if (INPUT.K.InstantDown(KeyCode.Mouse1)) SecondaryFire();
		if (Input.GetMouseButton(1)) SecondaryFireHeld();
		if (INPUT.K.InstantDown(KeyCode.R)) Reload();
		if (INPUT.K.InstantDown(KeyCode.Q)) QButtonPressed();
	}
	#endregion

	#region public API — tool actions (virtual)
	/// <summary> single click fire </summary>
	public virtual void PrimaryFire()
	{
		if (_viewModelAnimator != null) _viewModelAnimator.Play(AnimParamType.attack1.ToString(), -1, 0f);
	}
	/// <summary> held fire (continuous) </summary>
	public virtual void PrimaryFireHeld() { }
	/// <summary> single right click </summary>
	public virtual void SecondaryFire() { }
	/// <summary> held right click </summary>
	public virtual void SecondaryFireHeld() { }
	/// <summary> R key </summary>
	public virtual void Reload() { }
	/// <summary> Q key </summary>
	public virtual void QButtonPressed() { }
	#endregion

	#region public API — equip / drop
	/// <summary> drop this tool from inventory to world </summary>
	public virtual void DropItem()
	{
		gameObject.SetActive(true);
		HideWorldModel(hide: false);
		HideViewModel();
		Rigidbody rb = GetComponentInChildren<Rigidbody>();
		if (rb != null && ownerCam != null)
		{
			transform.parent = null;
			rb.isKinematic = false;
			rb.transform.position = ownerCam.transform.position + ownerCam.transform.forward * 0.5f;
			rb.linearVelocity = ownerCam.transform.forward * 5f;
			rb.rotation = ownerCam.transform.rotation;
		}
		ownerCam = null;
		ownerViewModelContainer = null;
		ownerMagnetToolPos = null;
	}
	/// <summary> show/hide view model </summary>
	protected virtual void HideViewModel(bool hide = true) => _viewModel?.SetActive(!hide);
	/// <summary> show/hide world model </summary>
	protected virtual void HideWorldModel(bool hide = true) => _worldModel?.SetActive(!hide);
	#endregion

	#region extra
	// nice-to-have: Equip/UnEquip — called by InventoryOrchestrator during SwitchTool transitions
	/// <summary> called when tool becomes active — hides world, shows view </summary>
	public virtual void Equip() { HideWorldModel(); HideViewModel(hide: false); }
	/// <summary> called when tool is deactivated — hides view </summary>
	public virtual void UnEquip() { HideViewModel(); }
	#endregion


	#region public API — IInteractable
	public bool ShouldUseInteractionWheel() => _shouldUseInteractionWheel;
	public List<SO_InteractionOption> GetOptions() => _interactions;
	public string GetObjectName() => _name;
	public virtual void Interact(SO_InteractionOption selectedOption)
	{
		if (selectedOption.interactionType == InteractionType.take)
		{
			// purpose: InventoryOrchestrator adds this tool to hotbar
			GameEvents.RaiseToolPickupRequested(this);
		}
		else if (selectedOption.interactionType == InteractionType.destroy)
		{
			Destroy(gameObject);
		}
	}
	#endregion

	#region public API — IHighlightable
	/// <summary> Returns the tool's highlight profile (cyan). All tools use the same profile. </summary>
	public virtual HighlightProfile GetHighlightProfile() => _highlightProfile;
	#endregion

	#region public API — ISaveLoadableObject (stub)
	public bool HasBeenSaved { get; set; }
	public bool ShouldBeSaved() => true;
	public SavableObjectID GetSavableObjectID() => _savableObjectID;
	public Vector3 GetPosition() => _worldModel != null ? _worldModel.transform.position : transform.position;
	public Vector3 GetRotation() => _worldModel != null ? _worldModel.transform.rotation.eulerAngles : transform.rotation.eulerAngles;
	public virtual void LoadFromSave(string json) { /* Phase G */ }
	public virtual string GetCustomSaveData() => "{}"; /* Phase G */
	#endregion

	#region Unity Life Cycle
	protected override void OnEnable()
	{
		base.OnEnable();
		if (ownerViewModelContainer == null)
		{
			HideViewModel();
			HideWorldModel(hide: false);
			return;
		}
		HideWorldModel();
		HideViewModel(hide: false);
		if (transform.parent == null || transform.parent != ownerViewModelContainer)
		{
			transform.position = ownerViewModelContainer.position;
			transform.rotation = ownerViewModelContainer.rotation;
			transform.parent = ownerViewModelContainer;
		}
	}
	#endregion
}