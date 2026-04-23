using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// defines a building type — name, icon, prefabs, stack size, placement options
/// </summary>
[CreateAssetMenu(menuName = "SO/SO_BuildingInventoryDefinition", fileName = "SO_BuildingInventoryDefinition")]
public class SO_BuildingInventoryDefinition : ScriptableObject
{
	public string Name = "Unknown Item";
	public Sprite ProgrammerInventoryIcon;
	public Sprite InventoryIcon;
	[TextArea] public string Description = "Placeholder Description!";
	public string QButtonFunction = "Mirror";
	public int MaxInventoryStackSize = 1;
	public List<BuildingObject> BuildingPrefabs;
	public BuildingCrate PackedPrefab;
	public bool UseReverseRotationDirection;
	public bool CanBePlacedInTerrain;

	/// <summary> returns the primary (first) building prefab </summary>
	public BuildingObject GetMainPrefab() => BuildingPrefabs?.FirstOrDefault();
	/// <summary> returns inventory icon (programmer icon as fallback) </summary>
	public Sprite GetIcon()
	{
		// Phase H: SettingsManager.ShouldUseProgrammerIcons() check
		return InventoryIcon != null ? InventoryIcon : ProgrammerInventoryIcon;
	}
}