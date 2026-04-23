using System;

/// <summary>
/// Phase A½ additions to GameEvents. Uses partial class — compiles with Phase A's GameEvents.
/// No modification to Phase A's file needed.
/// </summary>
public static partial class GameEvents
{
	// when elevator reaches the bottom >>
	public static event Action OnElevatorLanded;
	public static void RaiseElevatorLanded()
	{
		LogSubscribersCount(nameof(OnElevatorLanded), OnElevatorLanded);
		OnElevatorLanded?.Invoke();
	}
	// << when elevator reaches the bottom

	// when game paused >>
	public static event Action OnGamePaused;
	public static void RaiseGamePaused()
	{
		LogSubscribersCount(nameof(OnGamePaused), OnGamePaused);
		OnGamePaused?.Invoke();
	}
	// << when game paused

	// when game unpaused >>
	public static event Action OnGameUnpaused;
	public static void RaiseGameUnpaused()
	{
		LogSubscribersCount(nameof(OnGameUnpaused), OnGameUnpaused);
		OnGameUnpaused?.Invoke();
	}
	// << when game unpaused
}