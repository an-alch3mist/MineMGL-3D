using System;
using UnityEngine;

/// <summary>
/// partial extend for phase-c — no modification to phase-a GameEvents
/// </summary>
public static partial class GameEvents
{
	// when ore node was mined (broken) >>
	public static event Action<ResourceType, Vector3> OnOreMined;
	public static void RaiseOreMined(ResourceType type, Vector3 position)
	{
		LogSubscribersCount(nameof(OnOreMined), OnOreMined);
		GameEvents.OnOreMined? // if there is any subscribers
			.Invoke(type, position);
	}
	// << when ore node was mined

	// when ore piece was sold >>
	public static event Action<float, ResourceType, PieceType> OnOreSold;
	public static void RaiseOreSold(float price, ResourceType type, PieceType pieceType)
	{
		LogSubscribersCount(nameof(OnOreSold), OnOreSold);
		GameEvents.OnOreSold? // if there is any subscribers
			.Invoke(price, type, pieceType);
	}
	// << when ore piece was sold

	// when ore limit state changed >>
	public static event Action<OreLimitState> OnOreLimitChanged;
	public static void RaiseOreLimitChanged(OreLimitState state)
	{
		LogSubscribersCount(nameof(OnOreLimitChanged), OnOreLimitChanged);
		GameEvents.OnOreLimitChanged? // if there is any subscribers
			.Invoke(state);
	}
	// << when ore limit state changed
}