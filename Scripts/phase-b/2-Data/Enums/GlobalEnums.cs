// GLOBAL ENUM (@PhaseB) >> //

/// <summary>
/// all enums introduced in Phase B — one file, one place
/// </summary>

/// <summary> interaction option types — replaces magic strings </summary>
public enum InteractionType
{
	Take,
	Destroy,
	Toggle,
	// Pack, // Phase D:
	// TakeBuilding,
	// SetMold, // Phase E:
	// ToggleDirection,
}

/// <summary> magnet tool grab filter modes </summary>
public enum MagnetToolSelectionMode
{
	Everything = 0,
	ResourcesNotInFilter = 1,
	ResourcesNotOnConveyors = 2
}

/// <summary> stub — expanded in Phase G with all savable object IDs </summary>
public enum SavableObjectID
{
	INVALID = 0,
	ToolBuilder = 401,
	HammerBasic = 402,
	Lantern = 403,
	MagnetTool = 404,
	PickaxeBasic = 405,
	ResourceScannerTool = 406,
	WrenchTool = 410,
	MiningHelmet = 417,
}

// << GLOBAL ENUM (@PhaseB) //