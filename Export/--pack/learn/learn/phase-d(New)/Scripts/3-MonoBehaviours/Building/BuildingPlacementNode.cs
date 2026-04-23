using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// marks where AutoMiners attach — self-registers in static list. Shows ghost indicator when player
/// is in build mode with matching PlacementNodeRequirement. Configures AutoMiner on attachment.
/// </summary>
public class BuildingPlacementNode : MonoBehaviour
{
	#region Inspector Fields
	[SerializeField] PlacementNodeRequirement _requirementType;
	[SerializeField] GameObject _ghostPrefab;
	[SerializeField] SO_AutoMinerResourceDefinition _autoMinerResourceDefinition;
	#endregion

	#region public API
	public static List<BuildingPlacementNode> All = new List<BuildingPlacementNode>();
	public PlacementNodeRequirement RequirementType => _requirementType;
	public BuildingObject AttachedBuildingObject { get; private set; }
	public SO_AutoMinerResourceDefinition AutoMinerResourceDefinition => _autoMinerResourceDefinition;

	/// <summary> attach a building to this node — configures AutoMiner with resource definition </summary>
	public void AttachBuilding(BuildingObject building)
	{
		AttachedBuildingObject = building;
		var autoMiner = building.GetComponent<AutoMiner>();
		if (autoMiner != null && _autoMinerResourceDefinition != null)
		{
			autoMiner.SetResourceDefinition(_autoMinerResourceDefinition);
		}
	}
	/// <summary> show/hide ghost indicator based on build mode + requirement match </summary>
	public void ShowGhost(bool show = true, PlacementNodeRequirement requirement = PlacementNodeRequirement.none)
	{
		if (AttachedBuildingObject != null) show = false;
		if (show && _ghostPrefab != null)
		{
			Material mat = (requirement == PlacementNodeRequirement.none || requirement == _requirementType)
				? Singleton<BuildingManager>.Ins.GetBuildingNodeGhost()
				: Singleton<BuildingManager>.Ins.GetBuildingNodeGhost_WrongType();
			foreach (var r in _ghostPrefab.GetComponentsInChildren<Renderer>())
			{
				var mats = r.sharedMaterials;
				for (int i = 0; i < mats.Length; i++) mats[i] = mat;
				r.sharedMaterials = mats;
			}
		}
		_ghostPrefab?.SetActive(show);
	}
	/// <summary> returns primary resource type for this node's definition </summary>
	public ResourceType GetPrimaryResourceType()
	{
		return _autoMinerResourceDefinition != null ? _autoMinerResourceDefinition.GetPrimaryResourceType() : ResourceType.INVALID;
	}
	#endregion

	#region Unity Life Cycle
	private void OnEnable() => All.Add(this);
	private void OnDisable() => All.Remove(this);
	private void Start() => ShowGhost(false);
	#endregion
}