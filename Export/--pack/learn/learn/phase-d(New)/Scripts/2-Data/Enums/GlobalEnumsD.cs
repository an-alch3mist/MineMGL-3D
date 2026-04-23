/// <summary>
/// all enums introduced in Phase D — one file, one place
/// </summary>

/// <summary> result of BuildingDataService.CanPlace validation </summary>
public enum CanPlaceBuilding
{
	valid = 1,
	invalid = 2,
	requirementsNotMet = 3
}

/// <summary> what kind of placement node a building requires (e.g. AutoMiner spots) </summary>
public enum PlacementNodeRequirement
{
	none = 0,
	autoMiner = 1,
	heavyAutoMiner = 2
}

/// <summary> what kind of support legs a building base connects to </summary>
public enum SupportType
{
	none = 0,
	conveyor = 1,
	roller = 2,
	flat = 3,
	chute = 4,
	walled = 5
}