using UnityEngine;

/// <summary>
/// Vertical slice test for elevator system independently.
///
/// Internal prerequisites (must be typed first):
///   Singleton, GameEvents (with OnElevatorLanded, OnGamePaused, OnGameUnpaused),
///   StartingElevator, CameraShaker
///
/// External prerequisites (scene/editor setup):
///   1. ElevatorPlatform GO with StartingElevator — assign refs
///   2. Camera with CameraShaker (low amplitude for subtle sway)
///   3. Ground plane
///   4. This script on any GO
///
/// NOT required: PlayerController, ShopUI, EconomyManager, InteractionSystem
///
/// Controls:
///   R = restart elevator descent
///   P = simulate game pause
///   U = simulate game unpause
///   L = log elevator state
/// </summary>
public class ElevatorTest : MonoBehaviour
{
	#region Inspector Fields
	[SerializeField] StartingElevator _elevator;
	[SerializeField] CameraShaker _shaker;
	#endregion
	#region Unity Life Cycle
	private void Start()
	{
		// purpose: log when elevator reaches bottom
		GameEvents.OnElevatorLanded += () => Debug.Log("[ElevatorTest] Elevator landed!");
	}
	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.R))
		{
			Debug.Log("[ElevatorTest] Restarting elevator");
			_elevator.gameObject.SetActive(false);
			_elevator.gameObject.SetActive(true); // triggers OnEnable → LowerTheElevator
		}
		if (Input.GetKeyDown(KeyCode.P)) GameEvents.RaiseGamePaused();
		if (Input.GetKeyDown(KeyCode.U)) GameEvents.RaiseGameUnpaused();
		if (Input.GetKeyDown(KeyCode.V)) _shaker.ApplyViewPunch(new Vector3(2f, 0.5f, 0.3f));
	}
	#endregion
}