using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// interactable sliding gate blocker — Toggle opens/closes with lerp. Implements IInteractable.
/// Phase H adds DOTween animation. Currently uses instant position set.
/// </summary>
public class ConveyorBlockerT2 : MonoBehaviour, IInteractable
{
	#region Inspector Fields
	[SerializeField] bool _isClosed;
	[SerializeField] Vector3 _closedPosition;
	[SerializeField] Vector3 _openPosition;
	[SerializeField] Transform _movingPart;
	[SerializeField] float _moveDuration = 0.35f;
	[SerializeField] List<SO_InteractionOption> _interactions;
	#endregion

	#region public API
	public bool IsClosed => _isClosed;
	public void Toggle() => SetClosed(!_isClosed);
	public void SetClosed(bool closed)
	{
		_isClosed = closed;
		// Phase H: DOTween or coroutine lerp
		_movingPart.localPosition = closed ? _closedPosition : _openPosition;
		// Phase H: play toggle sound
	}
	#endregion

	#region public API — IInteractable
	public bool ShouldUseInteractionWheel() => true;
	public string GetObjectName() => "Conveyor Blocker T2";
	public List<SO_InteractionOption> GetOptions() => _interactions;
	public void Interact(SO_InteractionOption selectedOption)
	{
		if (selectedOption.interactionType == InteractionType.Toggle) Toggle();
	}
	#endregion

	#region Unity Life Cycle
	private void OnEnable() => SetClosed(_isClosed);
	#endregion

	#region extra
	// Phase G: ICustomSaveDataProvider — LoadFromSave/GetCustomSaveData for IsClosed state
	#endregion
}