using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// interactable direction switch — Toggle rotates between two directions. Implements IInteractable.
/// </summary>
public class RoutingConveyor : MonoBehaviour, IInteractable
{
	#region Inspector Fields
	[SerializeField] string _name = "Routing Conveyor";
	[SerializeField] bool _isClosed;
	[SerializeField] GameObject _closedObjects;
	[SerializeField] GameObject _openObjects;
	[SerializeField] Vector3 _closedRotation;
	[SerializeField] Vector3 _openRotation;
	[SerializeField] Transform _rotatingPart;
	[SerializeField] float _rotateDuration = 0.35f;
	[SerializeField] List<SO_InteractionOption> _interactions;
	#endregion

	#region public API
	public bool IsClosed => _isClosed;
	public void ToggleDirection() => SetDirection(!_isClosed);
	public void SetDirection(bool closed)
	{
		_isClosed = closed;
		// Phase H: DOTween or coroutine lerp + sound
		Vector3 euler = closed ? _closedRotation : _openRotation;
		_rotatingPart.localRotation = Quaternion.Euler(euler);
		if (_closedObjects != null) _closedObjects.SetActive(closed);
		if (_openObjects != null) _openObjects.SetActive(!closed);
	}
	#endregion

	#region public API — IInteractable
	public bool ShouldUseInteractionWheel() => true;
	public string GetObjectName() => _name;
	public List<SO_InteractionOption> GetOptions() => _interactions;
	public void Interact(SO_InteractionOption selectedOption)
	{
		if (selectedOption.interactionType == InteractionType.Toggle) ToggleDirection();
	}
	#endregion

	#region Unity Life Cycle
	private void OnEnable() => SetDirection(_isClosed);
	#endregion

	#region extra
	// Phase G: ICustomSaveDataProvider — LoadFromSave/GetCustomSaveData for IsClosed state
	#endregion
}