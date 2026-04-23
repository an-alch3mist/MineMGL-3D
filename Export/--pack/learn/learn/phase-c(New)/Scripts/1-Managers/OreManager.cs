using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// I'm the top-level manager for the ore system. On Awake I create and build an OreDataService
/// with the resource descriptions list from the inspector. In Update I do round-robin cleanup —
/// checking one ore piece per frame. If it's null (externally destroyed), I remove it from the
/// list. If it fell below y=-1000 (out of world), I call Delete to return it to the pool.
/// Other scripts access my DataService property for resource color lookups and formatted strings.
///
/// Who uses me: OreTest (DataService snapshot), ToolResourceScanner (color/name lookups).
/// Events I fire: none. Events I subscribe to: none.
/// </summary>
public class OreManager : Singleton<OreManager>
{
	#region Inspector Fields
	[SerializeField] List<ResourceDescription> _allResourceDescriptions;
	#endregion

	#region private API
	OreDataService oreDataService = new OreDataService();
	int currentOreIndex;
	#endregion

	#region public API
	/// <summary> expose data service for external queries </summary>
	public OreDataService DataService => oreDataService;
	#endregion

	#region Unity Life Cycle
	/// <summary> Registers singleton, then builds OreDataService with the resource descriptions
	/// list from the inspector so color lookups and formatted strings are ready immediately. </summary>
	protected override void Awake()
	{
		base.Awake();
		// → build OreDataService with inspector resource descriptions
		oreDataService.Build(_allResourceDescriptions);
	}
	/// <summary> Checks one ore piece per frame via round-robin. If null (destroyed externally),
	/// removes it from the list. If fallen below y=-1000 (out of world), returns it to pool. </summary>
	private void Update()
	{
		var allOre = OrePiece.AllOrePieces;
		if (allOre.Count == 0) { currentOreIndex = 0; return; }
		if (currentOreIndex >= allOre.Count) currentOreIndex = 0;
		var piece = allOre[currentOreIndex];
		// → remove null entries (destroyed externally)
		if (piece == null)
		{
			OrePiece.AllOrePieces.Remove(piece);
			return;
		}
		// → return to pool if fallen out of world
		if (piece.transform.position.y < -1000f)
			piece.Delete();
		currentOreIndex++;
	}
	#endregion
}