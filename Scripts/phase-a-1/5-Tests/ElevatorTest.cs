using UnityEngine;
using SPACE_UTIL;

public class ElevatorTest : MonoBehaviour
{
	[SerializeField] StartingElevator _startingElevator;
	private void Update()
	{
		if (INPUT.K.InstantDown(KeyCode.R))
		{
			this._startingElevator.gameObject.SetActive(false);
			this._startingElevator.gameObject.SetActive(true);
		}
		if (INPUT.K.InstantDown(KeyCode.I)) GameEvents.RaiseCamViewPunch(new Vector3(3f, 0.5f, 0.4f), duration: 0.25f);
		//
		if (INPUT.K.InstantDown(KeyCode.P)) GameEvents.RaiseGamePaused();
		if (INPUT.K.InstantDown(KeyCode.U)) GameEvents.RaiseGameUnPaused();
	}
}
