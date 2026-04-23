using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// I'm a placed building in the world. I implement IInteractable so the player can press E to
/// get "Take" and "Pack" options. Take fires RaiseBuildingTakeRequested(definition) — the
/// InventoryOrchestrator creates a ToolBuilder from my definition and adds it to the hotbar.
/// Pack spawns a BuildingCrate at my BuildingCrateSpawnPoint with random velocity, then destroys me.
///
/// I manage my own scaffolding supports (ModularBuildingSupports/ScaffoldingSupportLeg children).
/// EnableBuildingSupports(false) disables all supports; the wrench tool calls this.
/// On Start (non-ghost), I set my PhysicalColliderObject to "BuildingObject" layer.
/// On Destroy (non-ghost), I raycast up to find any supports above me that need to adjust.
///
/// Ghost mode: when IsGhost=true (set by BuildingManager during placement preview), Start enables
/// ExtraGhostRenderers and skips all real building initialization.
///
/// Conveyor snap: ConveyorInputSnapPositions and ConveyorOutputSnapPositions are Transform lists
/// marking where this building's input/output connect to neighboring conveyors.
///
/// Who uses me: ToolBuilder (places me), ToolHammer (Take/Pack interaction), ToolSupportsWrench (EnableBuildingSupports).
/// Events I fire: OnBuildingRemoved (local Action event), RaiseBuildingRemoved, RaiseBuildingTakeRequested.
/// Phase G: implements ISaveLoadableBuildingObject for persistence.
/// </summary>
public class BuildingObject : MonoBehaviour, IInteractable
{
	#region Inspector Fields
	[SerializeField] SavableObjectID _savableObjectID;
	[SerializeField] SO_BuildingInventoryDefinition _definition;
	[SerializeField] Vector3 _buildModePlacementOffset;
	[SerializeField] List<SO_InteractionOption> _interactions;
	[SerializeField] Transform _buildingCrateSpawnPoint;
	[SerializeField] bool _requiresFlatGround;
	[SerializeField] PlacementNodeRequirement _placementNodeRequirement;
	[SerializeField] SupportType _supportType;
	[SerializeField] GameObject _physicalColliderObject;
	[SerializeField] GameObject _buildingPlacementColliderObject;
	[SerializeField] GameObject _extraGhostRenderers;
	[SerializeField] bool _rotatingShouldMirrorWhenSnapped;
	#endregion

	#region public API
	public SO_BuildingInventoryDefinition Definition => _definition;
	public Vector3 BuildModePlacementOffset => _buildModePlacementOffset;
	public bool RequiresFlatGround => _requiresFlatGround;
	public PlacementNodeRequirement PlacementNodeRequirement => _placementNodeRequirement;
	public SupportType SupportType => _supportType;
	public GameObject PhysicalColliderObject => _physicalColliderObject;
	public GameObject BuildingPlacementColliderObject => _buildingPlacementColliderObject;
	public GameObject ExtraGhostRenderers => _extraGhostRenderers;
	public bool RotatingShouldMirrorWhenSnapped => _rotatingShouldMirrorWhenSnapped;
	public List<Transform> ConveyorInputSnapPositions = new List<Transform>();
	public List<Transform> ConveyorOutputSnapPositions = new List<Transform>();
	public bool BuildingSupportsEnabled = true;
	[HideInInspector] public bool IsGhost;
	public event Action OnBuildingRemoved;

	/// <summary> enable/disable scaffolding supports + respawn </summary>
	public void EnableBuildingSupports(bool enabled)
	{
		BuildingSupportsEnabled = enabled;
		foreach (var s in modularSupports) s.RespawnSupports();
	}
	/// <summary> true if this building has any support components </summary>
	public virtual bool CanHaveBuildingSupports() => modularSupports.Count > 0;
	/// <summary> try take into inventory, else pack into crate </summary>
	public void TryTakeOrPack()
	{
		if (IsGhost) return;
		// purpose: InventoryOrchestrator creates ToolBuilder from definition + adds to hotbar
		GameEvents.RaiseBuildingTakeRequested(_definition, 1);
		OnBuildingRemoved?.Invoke();
		// purpose: notify quest/research systems
		GameEvents.RaiseBuildingRemoved(this);
		Destroy(gameObject);
	}
	/// <summary> pack building into a crate on the ground </summary>
	public void Pack()
	{
		if (IsGhost) return;
		Vector3 pos = _buildingCrateSpawnPoint ? _buildingCrateSpawnPoint.position : (transform.position + new Vector3(0f, 0.25f, 0f));
		Quaternion rot = _buildingCrateSpawnPoint ? _buildingCrateSpawnPoint.rotation : Quaternion.identity;
		var crate = Instantiate(
			_definition.PackedPrefab ? _definition.PackedPrefab : Singleton<BuildingManager>.Ins.GetBuildingCratePrefab(),
			pos, rot);
		crate.SetDefinition(_definition);
		var rb = crate.GetComponent<Rigidbody>();
		if (rb != null)
		{
			rb.linearVelocity = new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(-0.5f, 0.5f));
			rb.angularVelocity = UnityEngine.Random.insideUnitSphere;
		}
		OnBuildingRemoved?.Invoke();
		GameEvents.RaiseBuildingRemoved(this);
		Destroy(gameObject);
	}
	#endregion

	#region public API — IInteractable
	public bool ShouldUseInteractionWheel() => true;
	public List<SO_InteractionOption> GetOptions() => _interactions;
	public string GetObjectName() => _definition?.Name ?? "Building";
	public void Interact(SO_InteractionOption selectedOption)
	{
		if (IsGhost) return;
		if (selectedOption.interactionType == InteractionType.Take) TryTakeOrPack();
		// Phase D extra: InteractionType.Pack → Pack()
	}
	#endregion

	#region private API
	List<BaseModularSupports> modularSupports = new List<BaseModularSupports>();
	#endregion

	#region Unity Life Cycle
	private void Awake()
	{
		modularSupports = GetComponentsInChildren<BaseModularSupports>().ToList();
	}
	public void Start()
	{
		if (IsGhost)
		{
			if (_extraGhostRenderers != null) _extraGhostRenderers.SetActive(true);
			return;
		}
		if (_extraGhostRenderers != null) _extraGhostRenderers.SetActive(false);
		// set physical collider to BuildingObject layer
		if (_physicalColliderObject != null)
			UtilsPhaseB.SetLayerRecursively(_physicalColliderObject, LayerMask.NameToLayer("BuildingObject"));
	}
	private void OnDestroy()
	{
		if (!IsGhost) UpdateSupportsAbove(true);
	}
	void UpdateSupportsAbove(bool isDestroying)
	{
		_physicalColliderObject?.SetActive(false);
		_buildingPlacementColliderObject?.SetActive(false);
		if (Physics.Raycast(transform.position, Vector3.up, out var hit, 20f,
			Singleton<BuildingManager>.Ins.GetBuildingSupportsCollisionLayers()))
		{
			var support = hit.collider.GetComponentInParent<ModularBuildingSupports>();
			if (support != null) support.RespawnSupports(RespawnNextFrame: true);
		}
		if (!isDestroying)
		{
			_physicalColliderObject?.SetActive(true);
			_buildingPlacementColliderObject?.SetActive(true);
		}
	}
	#endregion

	#region extra
	// Phase G: ISaveLoadableBuildingObject, ISaveLoadableObject stubs
	// GetPosition, GetRotation, GetSavableObjectID, LoadFromSave, GetCustomSaveData, LoadBuildingSaveData
	#endregion
}