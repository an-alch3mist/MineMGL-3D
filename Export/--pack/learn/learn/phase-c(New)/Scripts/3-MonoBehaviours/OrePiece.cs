using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;

/// <summary>
/// I'm a physical ore piece that exists in the world — I have a ResourceType (Iron, Gold, etc.),
/// PieceType (Ore, Crushed, Ingot, etc.), and a sell value. On Start I pick a random mesh from
/// _possibleMeshes, apply random scale variance, and set a random price multiplier (0.9-1.1x).
/// I self-register in a static AllOrePieces list (OnEnable adds, OnDisable removes) so OreManager
/// and OreLimitManager can iterate all active pieces. When I enter a SellerMachine trigger, it
/// calls my SellAfterDelay — I detach from magnet, tag as MarkedForDestruction (so other triggers
/// skip me), wait 2s, then add money + fire OnOreSold + return to pool via Delete(). The pool
/// (OrePiecePoolManager) deactivates me, resets everything, and enqueues me for reuse.
///
/// Who uses me: OreNode (spawns me), SellerMachine (sells me), ToolMagnet (pulls me),
///   ToolPickaxe (TryConvertToCrushed), OrePiecePoolManager (pool lifecycle).
/// Events I fire: OnOreSold (via DelayThenSell coroutine).
/// </summary>
public class OrePiece : BaseSellableItem
{
	#region Inspector Fields
	[SerializeField] ResourceType _resourceType;
	[SerializeField] PieceType _pieceType;
	[SerializeField] bool _isPolished;
	[SerializeField] Mesh[] _possibleMeshes;
	[SerializeField] MeshFilter _meshFilter;
	[SerializeField] MeshCollider _meshCollider;
	[SerializeField] bool _useRandomMesh = true;
	[SerializeField] bool _useRandomScale = true;
	[SerializeField] Vector3 _scaleVariance = new Vector3(0.25f, 0.25f, 0.25f);
	[SerializeField] Sprite _inventoryIcon;
	[Header("Conversion Prefabs (Phase E)")]
	[SerializeField] GameObject _crushedPrefab;
	[SerializeField] GameObject _polishedPrefab;
	// Phase E: _ingotPrefab, _platePrefab, _pipePrefab, _rodPrefab, _threadedPrefab
	#endregion

	#region private API
	float randomPriceMultiplier = 1f;
	int meshID;
	#endregion

	#region public API
	public ResourceType ResourceType => _resourceType;
	public PieceType PieceType => _pieceType;
	public bool IsPolished => _isPolished;
	public float RandomPriceMultiplier { get => randomPriceMultiplier; set => randomPriceMultiplier = value; }
	public int MeshID => meshID;
	public float VolumeInsideBox = 0.1f;
	[Obsolete] public float PolishedPercent;
	public float SievePercent;
	public ToolMagnet CurrentMagnetTool;
	public HashSet<object> BasketsThisIsInside = new HashSet<object>(); // Phase E: HashSet<BaseBasket>

	public static List<OrePiece> AllOrePieces = new List<OrePiece>();

	/// <summary> Returns the sell price for this ore piece — BaseSellValue multiplied by
	/// a random multiplier (0.9-1.1) that was set on Start. This is what EconomyManager.AddMoney receives. </summary>
	public override float GetSellValue() => Mathf.Round(BaseSellValue * randomPriceMultiplier * 100f) / 100f;
	/// <summary> Returns my inventory icon sprite — ToolResourceScanner shows this when the
	/// player looks at me with the scanner tool equipped. Null if no icon assigned. </summary>
	public Sprite GetIcon() => _inventoryIcon;

	/// <summary> Returns me to the pool instead of Destroy — OrePiecePoolManager deactivates me,
	/// resets my velocity/drag/tags, and enqueues me for reuse by the next SpawnPooledOre call. </summary>
	public void Delete()
	{
		// → return to pool: deactivate, reset physics, re-tag Grabbable, enqueue
		Singleton<OrePiecePoolManager>.Ins.ReturnToPool(this);
	}
	/// <summary> Called by SellerMachine when I enter its trigger. Detaches me from magnet if held,
	/// tags me as MarkedForDestruction so other triggers skip me, then starts a 2-second coroutine
	/// that sells me (adds money via EconomyManager, fires OnOreSold, returns me to pool). </summary>
	public void SellAfterDelay(float delay = 2f)
	{
		// → detach from magnet so it stops pulling me
		CurrentMagnetTool?.DetachBody(Rb);
		// → tag so other triggers (other sellers, conveyors) skip me
		gameObject.SetTag(TagType.MarkedForDestruction);
		// → after 2s: add money to EconomyManager, fire OnOreSold for quest tracking, return to pool
		StartCoroutine(DelayThenSell(delay));
	}
	/// <summary> Called by ToolPickaxe when it hits me with _canBreakOreIntoCrushed enabled. Spawns
	/// 2 crushed pieces at my position via the pool, then deletes me. Returns false if I have no
	/// _crushedPrefab assigned (some ore types can't be crushed). </summary>
	public bool TryConvertToCrushed()
	{
		if (!gameObject.activeSelf || _crushedPrefab == null) return false;
		for (int i = 0; i < 2; i++)
			Singleton<OrePiecePoolManager>.Ins.TrySpawnPooledOre(_crushedPrefab, transform.position, transform.rotation);
		// Phase H: play crush sound
		Delete();
		return true;
	}
	#endregion

	#region extra
	// nice-to-have: Phase E conversion methods — AddPolish, AddSieveValue, ConvertToPlate, ConvertToRod, etc.
	// nice-to-have: CompleteClusterBreaking — used by DamageableOrePiece and ClusterBreaker machine
	public void CompleteClusterBreaking() { /* Phase E */ }
	#endregion

	#region Unity Life Cycle
	/// <summary> Picks a random mesh from _possibleMeshes, applies it to MeshFilter + MeshCollider,
	/// randomizes scale within _scaleVariance, and sets a random price multiplier (0.9-1.1). </summary>
	private void Start()
	{
		// → pick random mesh variant
		if (_possibleMeshes.Length > 0)
		{
			if (_useRandomMesh) meshID = UnityEngine.Random.Range(0, _possibleMeshes.Length);
			if (_meshCollider != null) _meshCollider.sharedMesh = _possibleMeshes[meshID];
			if (_meshFilter != null) _meshFilter.sharedMesh = _possibleMeshes[meshID];
		}
		if (_useRandomScale)
		{
			Vector3 s = transform.localScale;
			s.x += UnityEngine.Random.Range(-_scaleVariance.x, _scaleVariance.x);
			s.y += UnityEngine.Random.Range(-_scaleVariance.y, _scaleVariance.y);
			s.z += UnityEngine.Random.Range(-_scaleVariance.z, _scaleVariance.z);
			transform.localScale = s;
		}
		randomPriceMultiplier = UnityEngine.Random.Range(0.9f, 1.1f);
	}
	/// <summary> Adds me to the static AllOrePieces list so OreManager and OreLimitManager can
	/// iterate all active pieces. Called when I'm spawned from pool (SetActive true). </summary>
	protected override void OnEnable()
	{
		base.OnEnable();
		AllOrePieces.Add(this);
	}
	/// <summary> Removes me from AllOrePieces when I'm returned to pool (SetActive false) or
	/// destroyed. Keeps the list clean — only active pieces are counted. </summary>
	protected override void OnDisable()
	{
		base.OnDisable();
		AllOrePieces.Remove(this);
	}
	/// <summary> Waits delay seconds, then adds my sell value to EconomyManager, fires OnOreSold
	/// so the quest system can track resource deposits, and returns me to the pool via Delete. </summary>
	IEnumerator DelayThenSell(float delay)
	{
		// → wait for the sell delay (default 2s)
		yield return new WaitForSeconds(delay);
		if (this == null || !isActiveAndEnabled) yield break;
		// → add my sell value to EconomyManager (triggers OnMoneyChanged)
		float price = GetSellValue();
		Singleton<EconomyManager>.Ins.AddMoney(price);
		// → fire OnOreSold so quest system (Phase F) can track resource deposits
		// purpose: quest system tracks resource deposits (Phase F subscribes)
		GameEvents.RaiseOreSold(price, _resourceType, _pieceType);
		// → return me to pool for reuse
		Delete();
	}
	#endregion
}