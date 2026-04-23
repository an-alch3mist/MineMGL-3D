using System.Collections.Generic;
using UnityEngine;
using SPACE_UTIL;

/// <summary>
/// Spawn/mine/sell flow test — needs scene with OreNodes, Pool, Seller
/// Prerequisites: All Phase C scripts + EconomyManager
/// NOT required: Player (use test controls), inventory, shop
/// How to test:
///   Space → damage nearest OreNode by 50
///   U → spawn OrePiece at camera forward
///   I → log OrePiece.AllOrePieces count
///   O → log OreDataService snapshot
///   M/N → menu toggle (sim)
/// </summary>
public class OreTest : MonoBehaviour
{
	#region Inspector Fields
	[SerializeField] OreNode _testNode;
	[SerializeField] OrePiece _testOrePrefab;
	[SerializeField] Camera _cam;
	[TextArea(5, 10)]
	string README = @"Space → damage testNode by 50
U → spawn ore at camera forward
I → log ore count
O → log OreDataService snapshot
M/N → menu toggle";
	#endregion

	#region Unity Life Cycle
	private void Start()
	{
		// purpose: log when ore is mined
		GameEvents.OnOreMined += (type, pos) => Debug.Log($"[OreTest] Mined: {type} at {pos}".colorTag("lime"));
		// purpose: log when ore is sold
		GameEvents.OnOreSold += (price, type, piece) => Debug.Log($"[OreTest] Sold: {type} {piece} for ${price}".colorTag("cyan"));
		// purpose: log ore limit state changes
		GameEvents.OnOreLimitChanged += (state) => Debug.Log($"[OreTest] LimitState: {state}".colorTag("orange"));
		// purpose: log money changes
		GameEvents.OnMoneyChanged += (money) => Debug.Log($"[OreTest] Money: {money.formatMoney()}".colorTag("lime"));
	}
	private void Update()
	{
		if (INPUT.K.InstantDown(KeyCode.Space) && _testNode != null)
		{
			_testNode.TakeDamage(50f, _testNode.transform.position);
		}
		else if (INPUT.K.InstantDown(KeyCode.U) && _testOrePrefab != null && _cam != null)
		{
			Vector3 pos = _cam.transform.position + _cam.transform.forward * 2f;
			Singleton<OrePiecePoolManager>.Ins.SpawnPooledOre(_testOrePrefab, pos, Quaternion.identity);
		}
		else if (INPUT.K.InstantDown(KeyCode.I))
		{
			Debug.Log($"OrePiece active: {OrePiece.AllOrePieces.Count}, pooled: {Singleton<OrePiecePoolManager>.Ins.GetInactiveCount()}".colorTag("cyan"));
		}
		else if (INPUT.K.InstantDown(KeyCode.O))
		{
			LOG.AddLog(Singleton<OreManager>.Ins.DataService.GetSnapshot("ore test"), "json");
		}
		else if (INPUT.K.InstantDown(KeyCode.M)) GameEvents.RaiseMenuStateChanged(true);
		else if (INPUT.K.InstantDown(KeyCode.N)) GameEvents.RaiseMenuStateChanged(false);
	}
	#endregion
}