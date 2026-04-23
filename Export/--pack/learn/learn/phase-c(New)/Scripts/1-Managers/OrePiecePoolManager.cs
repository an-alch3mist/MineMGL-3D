using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// I recycle ore objects so we never call Instantiate/Destroy after the initial pool warmup.
/// When someone needs an ore piece, they call SpawnPooledOre — I check my dictionary of queues
/// (keyed by ResourceType + PieceType + IsPolished). If a recycled piece exists, I dequeue it
/// and reposition it. If the queue is empty, I Instantiate a new one from the registered prefab.
/// When an ore piece is done (sold, fell out of world, etc.), OrePiece calls Delete → ReturnToPool
/// which deactivates the GO, resets velocity/drag/tags, and enqueues it for next time.
/// On Awake I register all prefabs from _allOrePiecePrefabs by their OrePieceKey.
///
/// Who uses me: OreNode (spawn drops), AutoMiner (timer spawn), OrePiece.Delete (return to pool).
/// Events I fire: none. Events I subscribe to: none.
/// </summary>
public class OrePiecePoolManager : Singleton<OrePiecePoolManager>
{
	#region Inspector Fields
	[SerializeField] List<OrePiece> _allOrePiecePrefabs = new List<OrePiece>();
	#endregion

	#region private API
	readonly Dictionary<OrePieceKey, Queue<OrePiece>> pools = new Dictionary<OrePieceKey, Queue<OrePiece>>();
	readonly Dictionary<OrePieceKey, OrePiece> prefabByKey = new Dictionary<OrePieceKey, OrePiece>();
	Transform root;
	#endregion

	#region public API
	/// <summary> Shorthand that extracts ResourceType/PieceType/IsPolished from the prefab and
	/// calls the full SpawnPooledOre overload. Used by OreNode and AutoMiner. </summary>
	public OrePiece SpawnPooledOre(OrePiece prefab, Vector3 position = default, Quaternion rotation = default)
	{
		return SpawnPooledOre(prefab.ResourceType, prefab.PieceType, prefab.IsPolished, position, rotation);
	}
	/// <summary> Main spawn method — looks up the OrePieceKey in the dictionary, dequeues a recycled
	/// piece if available, otherwise Instantiates a new one from the registered prefab. Repositions
	/// it and activates it. Returns the ready-to-use OrePiece. </summary>
	public OrePiece SpawnPooledOre(ResourceType resourceType, PieceType pieceType, bool isPolished, Vector3 position = default, Quaternion rotation = default)
	{
		// → build composite key from type + piece + polished
		var key = new OrePieceKey(resourceType, pieceType, isPolished);
		// → find the registered prefab for this key
		if (!prefabByKey.TryGetValue(key, out var prefab))
		{
			Debug.LogError($"No OrePiece prefab for key: {key}");
			return null;
		}
		// → find or create the queue for this key
		var queue = pools.GetOrCreate(key);
		// → dequeue recycled piece or Instantiate new one
		OrePiece piece;
		if (queue.Count > 0) piece = queue.Dequeue();
		else
		{
			piece = Object.Instantiate(prefab, root);
			piece.gameObject.name = prefab.gameObject.name + " [Pooled]";
		}
		// → reposition and activate
		piece.transform.SetParent(null, false);
		piece.transform.SetPositionAndRotation(position, rotation);
		piece.gameObject.SetActive(true);
		return piece;
	}
	/// <summary> Accepts a GameObject (like _crushedPrefab from OrePiece). If it has an OrePiece
	/// component, spawns via pool. If not, falls back to regular Instantiate. </summary>
	public OrePiece TrySpawnPooledOre(GameObject prefab, Vector3 position = default, Quaternion rotation = default)
	{
		var orePiece = prefab.GetComponent<OrePiece>();
		if (orePiece != null) return SpawnPooledOre(orePiece, position, rotation);
		// → not an OrePiece — fallback to regular Instantiate (e.g. particle effect prefab)
		Object.Instantiate(prefab, position, rotation);
		return null;
	}
	/// <summary> Takes an ore piece that's done being used (sold, fell out of world, etc.) and
	/// puts it back in the pool for reuse — deactivates it, resets all physics and state, re-tags
	/// as Grabbable, and enqueues it so the next SpawnPooledOre can reuse it. </summary>
	public void ReturnToPool(OrePiece piece)
	{
		if (piece == null || !piece.gameObject.activeSelf) return;
		// → find or create queue for this ore type key
		var key = new OrePieceKey(piece.ResourceType, piece.PieceType, piece.IsPolished);
		var queue = pools.GetOrCreate(key);
		// → deactivate GO + zero out velocity, put Rb to sleep, restore standard drag
		piece.gameObject.SetActive(false);
		if (piece.Rb != null)
		{
			piece.Rb.linearVelocity = Vector3.zero;
			piece.Rb.angularVelocity = Vector3.zero;
			piece.Rb.Sleep();
			piece.Rb.linearDamping = BasePhysicsObject.STANDARD_LINEAR_DAMPING;
			piece.Rb.angularDamping = BasePhysicsObject.STANDARD_ANGULAR_DAMPING;
		}
		// → reset all runtime state: position, rotation, baskets, sieve%, magnet ref, tag
		piece.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
		piece.BasketsThisIsInside.Clear();
		piece.SievePercent = 0f;
		piece.CurrentMagnetTool = null;
		piece.gameObject.SetTag(TagType.Grabbable);
		// → parent under pool root + enqueue for next SpawnPooledOre call
		piece.transform.SetParent(root, false);
		queue.Enqueue(piece);
	}
	/// <summary> Returns how many ore pieces are sitting in pool queues waiting to be reused.
	/// OreTest logs this alongside AllOrePieces.Count to show active vs pooled distribution. </summary>
	public int GetInactiveCount()
	{
		int count = 0;
		foreach (var queue in pools.Values) count += queue.Count;
		return count;
	}
	#endregion

	#region Unity Life Cycle
	/// <summary> Creates the pool root GO, then registers every prefab in _allOrePiecePrefabs by
	/// its OrePieceKey (ResourceType + PieceType + IsPolished) so SpawnPooledOre can look them up. </summary>
	protected override void Awake()
	{
		base.Awake();
		// → create a hidden root GO to parent inactive pooled pieces under
		root = new GameObject("[OrePiecePools]").transform;
		root.SetParent(transform);
		// → register all prefabs by their composite key for O(1) lookup
		prefabByKey.Clear();
		foreach (var prefab in _allOrePiecePrefabs)
		{
			if (prefab == null) continue;
			var key = new OrePieceKey(prefab.ResourceType, prefab.PieceType, prefab.IsPolished);
			if (prefabByKey.ContainsKey(key))
				Debug.LogWarning($"Duplicate OrePiece prefab key: {key}. Keeping first.");
			else
				prefabByKey[key] = prefab;
		}
	}
	#endregion
}