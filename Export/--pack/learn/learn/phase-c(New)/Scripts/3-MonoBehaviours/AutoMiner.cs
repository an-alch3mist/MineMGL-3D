using UnityEngine;

/// <summary>
/// I spawn ore pieces on a timer. My Rotator child spins continuously (visual drill animation).
/// Every _spawnRate seconds, I roll against _spawnProbability (default 80%) — if it passes,
/// I call UtilsPhaseC.PickOrePrefab to do a weighted random selection from the SO definition's
/// PossibleOrePrefabs list, then spawn it via OrePiecePoolManager at my _oreSpawnPoint.
/// OreLimitManager throttles me — at SlightlyLimited my timer is 25% slower, at Blocked I
/// skip spawning entirely. On Start I read SpawnProbability and SpawnRate from my SO definition.
///
/// Who uses me: Scene (placed on mine walls). OreLimitManager (throttles my spawn rate).
/// Events I fire: none. Events I subscribe to: none.
/// </summary>
public class AutoMiner : MonoBehaviour
{
	#region Inspector Fields
	[SerializeField] GameObject _rotator;
	[SerializeField] Transform _oreSpawnPoint;
	[SerializeField] bool _rotateY;
	[SerializeField] bool _rotateZ;
	[SerializeField] bool _enabled = true;
	[SerializeField] int _oresPerRotation = 12;
	[SerializeField] OrePiece _fallbackOrePrefab;
	[SerializeField] SO_AutoMinerResourceDefinition _resourceDefinition;
	[Header("Configured from Definition")]
	[SerializeField] [Range(0f, 100f)] float _spawnProbability = 80f;
	[SerializeField] [Range(0f, 20f)] float _spawnRate = 2f;
	#endregion

	#region private API
	Vector3 rotationAxis;
	float timeUntilNextSpawn;

	/// <summary> Checks if spawning is blocked by OreLimitManager, rolls against spawn probability,
	/// then picks a weighted random prefab from the SO definition and spawns it via the pool. </summary>
	void TrySpawnOre()
	{
		// → if OreLimitManager says we're at Blocked state, skip entirely
		if (Singleton<OreLimitManager>.Ins.ShouldBlockOreSpawning()) return;
		// → random probability roll — 80% default means ~20% of cycles produce nothing
		if (Random.Range(0f, 100f) > _spawnProbability) return;
		// → weighted random pick from SO's PossibleOrePrefabs, or fallback if no definition
		OrePiece prefab = _resourceDefinition != null
			? UtilsPhaseC.PickOrePrefab(_resourceDefinition.PossibleOrePrefabs, true)
			: _fallbackOrePrefab;
		// → spawn via pool at the ore spawn point position
		if (prefab != null)
			Singleton<OrePiecePoolManager>.Ins.SpawnPooledOre(prefab, _oreSpawnPoint.position, _oreSpawnPoint.rotation);
	}
	#endregion

	#region Unity Life Cycle
	/// <summary> Sets up the rotation axis, initializes spawn timer, and reads spawn probability
	/// and rate from the SO definition (overrides inspector defaults if definition exists). </summary>
	private void Start()
	{
		// → initialize spawn timer and pick rotation axis
		timeUntilNextSpawn = _spawnRate;
		rotationAxis = _rotateZ ? Vector3.back : (_rotateY ? Vector3.down : Vector3.right);
		// → read config from SO definition (overrides inspector defaults)
		if (_resourceDefinition != null)
		{
			_spawnProbability = _resourceDefinition.SpawnProbability;
			_spawnRate = _resourceDefinition.SpawnRate;
		}
	}
	/// <summary> Every frame: rotates the drill visual at a speed based on spawn rate, counts
	/// down the spawn timer, and calls TrySpawnOre when the timer hits zero. Timer is throttled
	/// by OreLimitManager's multiplier during high load. </summary>
	private void Update()
	{
		if (!_enabled || _spawnRate <= 0f) return;
		// → rotate the drill visual (speed = 360° per spawnRate * oresPerRotation)
		float angle = 360f / (_spawnRate * _oresPerRotation) * Time.deltaTime;
		_rotator.transform.Rotate(rotationAxis, angle);
		// → count down spawn timer
		timeUntilNextSpawn -= Time.deltaTime;
		timeUntilNextSpawn = Mathf.Min(timeUntilNextSpawn, _spawnRate);
		// → try to spawn ore when timer hits zero, then reset with throttle multiplier
		if (timeUntilNextSpawn <= 0f)
		{
			TrySpawnOre();
			timeUntilNextSpawn += _spawnRate * Singleton<OreLimitManager>.Ins.GetAutoMinerSpawnTimeMultiplier();
		}
	}
	#endregion

	#region extra
	// nice-to-have: Phase D adds IInteractable (Turn On/Off), ICustomSaveDataProvider, BuildingObject integration
	// nice-to-have: Phase D adds Toggle(on/off) with light material swap + looping sound
	#endregion
}