/// <summary>
/// Contract for buildings that persist across saves. Extends ISaveLoadableObject (Phase B stub)
/// with building-specific data: support enable state. Phase G implements the actual save/load flow.
///
/// Who implements me: BuildingObject.
/// Used by: SavingLoadingManager (Phase G) during save/load.
/// </summary>
public interface ISaveLoadableBuildingObject : ISaveLoadableObject
{
	/// <summary> returns whether building supports are currently enabled </summary>
	bool GetBuildingSupportsEnabled();
	/// <summary> restores building state from save file entry </summary>
	void LoadBuildingSaveData(BuildingObjectEntry entry);
}