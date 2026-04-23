using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// I throttle ore spawning when the physics engine has too many active objects. Every 15 seconds
/// I count how many OrePieces have non-sleeping Rigidbodies. If the count exceeds _movingObjectLimit
/// I escalate through states: Regular → SlightlyLimited (AutoMiner 25% slower) → HighlyLimited
/// (50% slower) → Blocked (AutoMiner stops entirely). On state change I fire OnOreLimitChanged
/// so PhysicsLimitUIWarning can show/hide the warning panel. First time a limit is hit, I log
/// a one-time warning popup. AutoMiner reads my GetAutoMinerSpawnTimeMultiplier each spawn cycle.
///
/// Who uses me: AutoMiner (ShouldBlockOreSpawning, GetAutoMinerSpawnTimeMultiplier).
/// Events I fire: OnOreLimitChanged(state).
/// </summary>
public class OreLimitManager : Singleton<OreLimitManager>
{
	#region Inspector Fields
	[SerializeField] int _movingObjectLimit = 500;
	#endregion

	#region private API
	OreLimitState currentState;
	float timeSinceLastCheck;
	#endregion

	#region public API
	/// <summary> true if spawning should be blocked entirely </summary>
	public bool ShouldBlockOreSpawning() => currentState == OreLimitState.Blocked;
	/// <summary> spawn time multiplier based on limit state </summary>
	public float GetAutoMinerSpawnTimeMultiplier()
	{
		switch (currentState)
		{
			case OreLimitState.SlightlyLimited: return 1.25f;
			case OreLimitState.HighlyLimited: return 1.5f;
			case OreLimitState.Blocked: return 2f;
			default: return 1f;
		}
	}
	/// <summary> force immediate recheck — called when objects are bulk destroyed </summary>
	public void OnObjectLimitChanged() => timeSinceLastCheck = 10f;
	#endregion

	#region private API
	bool hasShownPopup;
	void TryShowWarningPopup()
	{
		if (hasShownPopup) return;
		hasShownPopup = true;
		Debug.Log($"[OreLimitManager] Ore limit reached! Current limit: {_movingObjectLimit}".colorTag("orange"));
		// Phase I: Singleton<UIManager>.Ins.ShowInfoMessagePopup("Ore Limit", message);
	}
	#endregion

	#region Unity Life Cycle
	/// <summary> Every 15 seconds, counts how many ore pieces have non-sleeping Rigidbodies.
	/// Compares against _movingObjectLimit to determine throttle state (Regular → Blocked). </summary>
	private void Update()
	{
		// → only check every 15 seconds to avoid per-frame overhead
		timeSinceLastCheck += Time.deltaTime;
		if (timeSinceLastCheck < 15f) return;
		timeSinceLastCheck = 0f;
		if (_movingObjectLimit >= 10000)
		{
			SetState(OreLimitState.Regular);
			return;
		}
		int softLimit = _movingObjectLimit + 100;
		int hardLimit = _movingObjectLimit + 200;
		int movingCount = 0;
		var allOre = OrePiece.AllOrePieces;
		for (int i = 0; i < allOre.Count; i++)
		{
			if (allOre[i].Rb != null && !allOre[i].Rb.IsSleeping())
			{
				movingCount++;
				if (movingCount > hardLimit) break;
			}
		}
		if (movingCount > hardLimit) { SetState(OreLimitState.Blocked); TryShowWarningPopup(); }
		else if (movingCount > softLimit) { SetState(OreLimitState.HighlyLimited); TryShowWarningPopup(); }
		else if (movingCount > _movingObjectLimit) { SetState(OreLimitState.SlightlyLimited); TryShowWarningPopup(); }
		else SetState(OreLimitState.Regular);
	}
	/// <summary> Only fires OnOreLimitChanged when the state actually changes — avoids spamming
	/// the event every 15 seconds when the state hasn't moved. </summary>
	void SetState(OreLimitState state)
	{
		if (currentState == state) return;
		currentState = state;
		// → fire event so PhysicsLimitUIWarning can show/hide the warning panel
		// purpose: PhysicsLimitUIWarning shows/hides warning text
		GameEvents.RaiseOreLimitChanged(currentState);
	}
	#endregion
}