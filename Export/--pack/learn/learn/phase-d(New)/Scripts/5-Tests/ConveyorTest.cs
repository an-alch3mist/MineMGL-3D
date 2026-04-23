using UnityEngine;
using SPACE_UTIL;

/// <summary>
/// I test conveyor belt physics — ore flows along belt chain, shaker oscillation, blocker gate,
/// routing direction switch, splitter arm. I spawn ore above the first belt via Space key, and
/// toggle ConveyorBlockerT2 (U) and RoutingConveyor (I) to verify all conveyor variants.
///
/// Prerequisites: ConveyorBeltManager singleton, OrePiecePoolManager singleton (Phase C), at least
/// 3-4 ConveyorBelt instances pre-placed in scene end-to-end. Optional: ConveyorBeltShaker,
/// ConveyorBlockerT2, RoutingConveyor, ConveyorSplitterT2, SellerMachine at chain end.
/// NOT required: Player movement, shop, building placement.
///
/// How to test:
///   1. Create scene with ConveyorBeltManager + OrePiecePoolManager + EconomyManager singletons
///   2. Place 3-4 ConveyorBelt prefabs end-to-end (forward arrows aligned)
///   3. Optionally add ConveyorBlockerT2, RoutingConveyor, ConveyorSplitterT2, SellerMachine
///   4. Create ConveyorTest GO → assign all _testXxx fields
///   5. Press Play → use keys below
///
/// Controls:
///   Space → spawn ore piece above first belt (via OrePiecePoolManager)
///   U     → toggle ConveyorBlockerT2 open/closed (gate slides)
///   I     → toggle RoutingConveyor direction (path switches)
///   O     → log active ore count + belt count
///   P     → log conveyor belt snapshot (PhaseDLOG)
///   M/N   → menu toggle (simulate)
/// </summary>
public class ConveyorTest : MonoBehaviour
{
	#region Inspector Fields
	[SerializeField] OrePiece _testOrePrefab;
	[SerializeField] Transform _spawnPoint;
	[SerializeField] ConveyorBlockerT2 _testBlocker;
	[SerializeField] RoutingConveyor _testRouter;
	[TextArea(5, 10)]
	string README = @"Space → spawn ore above belt
U → toggle ConveyorBlockerT2
I → toggle RoutingConveyor
O → log ore + belt counts
P → log conveyor snapshot
M/N → menu toggle";
	#endregion

	#region Unity Life Cycle
	/// <summary> subscribes to money/ore events for console logging </summary>
	private void Start()
	{
		// purpose: log money changes when ore is sold at SellerMachine
		GameEvents.OnMoneyChanged += (m) => Debug.Log($"[ConveyorTest] Money: {m}".colorTag("lime"));
		// purpose: log when ore is sold (tracks resource type + piece type)
		GameEvents.OnOreSold += (price, type, piece) => Debug.Log($"[ConveyorTest] Sold: {type} {piece} for ${price}".colorTag("cyan"));
		// purpose: log ore limit state changes
		GameEvents.OnOreLimitChanged += (state) => Debug.Log($"[ConveyorTest] LimitState: {state}".colorTag("orange"));
	}
	/// <summary> routes keyboard inputs — spawn ore, toggle variants, log state </summary>
	private void Update()
	{
		// → spawn ore piece above first belt
		if (INPUT.K.InstantDown(KeyCode.Space) && _testOrePrefab != null && _spawnPoint != null)
		{
			Singleton<OrePiecePoolManager>.Ins.SpawnPooledOre(_testOrePrefab, _spawnPoint.position, Quaternion.identity);
			Debug.Log("[ConveyorTest] Spawned ore above belt".colorTag("cyan"));
		}
		// → toggle ConveyorBlockerT2 open/closed
		else if (INPUT.K.InstantDown(KeyCode.U) && _testBlocker != null)
		{
			_testBlocker.Toggle();
			Debug.Log($"[ConveyorTest] Blocker: {(_testBlocker.IsClosed ? "CLOSED" : "OPEN")}".colorTag("orange"));
		}
		// → toggle RoutingConveyor direction
		else if (INPUT.K.InstantDown(KeyCode.I) && _testRouter != null)
		{
			_testRouter.ToggleDirection();
			Debug.Log($"[ConveyorTest] Router: {(_testRouter.IsClosed ? "CLOSED" : "OPEN")}".colorTag("orange"));
		}
		// → log active ore + belt counts
		else if (INPUT.K.InstantDown(KeyCode.O))
		{
			Debug.Log($"Active ore: {OrePiece.AllOrePieces.Count}, Belts: {ConveyorBelt.AllConveyorBelts.Count}".colorTag("cyan"));
		}
		// → log full conveyor belt snapshot
		else if (INPUT.K.InstantDown(KeyCode.P))
		{
			Debug.Log(PhaseDLOG.LIST_CONVEYOR_BELTS__TO__JSON());
		}
		// → simulate menu open/close
		else if (INPUT.K.InstantDown(KeyCode.M)) GameEvents.RaiseMenuStateChanged(true);
		else if (INPUT.K.InstantDown(KeyCode.N)) GameEvents.RaiseMenuStateChanged(false);
	}
	#endregion
}