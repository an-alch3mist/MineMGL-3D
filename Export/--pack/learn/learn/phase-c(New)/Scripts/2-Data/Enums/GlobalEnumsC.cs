/// <summary>
/// All enums introduced in Phase C — one file, one place.
/// </summary>

/// <summary> What kind of resource this ore is. OreNode has one ResourceType, every OrePiece
/// spawned from it inherits that type. OreDataService uses this for color lookups and formatted
/// strings. Used across all phases — machines process by type, quests track by type. </summary>
public enum ResourceType
{
	INVALID = 0,
	Iron = 1,
	Coal = 2,
	Gold = 3,
	Slag = 4,
	Diamond = 5,
	Emerald = 6,
	Copper = 7,
	Broken = 8,
	Ruby = 9,
	Steel = 10,
	Celestite = 11,
	Quartz = 12,
	Amethyst = 13,
	Mystery = 14
}

/// <summary> What shape/processing state this ore piece is in. Ore = raw from node, Crushed = broken
/// by pickaxe or crusher, Ingot/Plate/Rod/Pipe = processed by machines (Phase E). OrePiecePoolManager
/// uses ResourceType + PieceType + IsPolished as the composite key for pool queues. </summary>
public enum PieceType
{
	INVALID = 0,
	Ore = 1,
	Crushed = 2,
	Ingot = 3,
	Plate = 4,
	Gem = 5,
	Pipe = 6,
	Rod = 7,
	DrillBit = 8,
	ThreadedRod = 9,
	Gear = 10,
	OreCluster = 11,
	JunkCast = 12,
	Geode = 13
}

/// <summary> Ore limit throttle state — OreLimitManager checks every 15s how many ore pieces
/// are actively moving. Regular = normal spawning. SlightlyLimited = AutoMiner 25% slower.
/// HighlyLimited = 50% slower. Blocked = AutoMiner stops entirely. PhysicsLimitUIWarning
/// subscribes to OnOreLimitChanged and shows/hides the warning panel based on this. </summary>
public enum OreLimitState
{
	Regular = 0,
	SlightlyLimited = 1,
	HighlyLimited = 2,
	Blocked = 3
}