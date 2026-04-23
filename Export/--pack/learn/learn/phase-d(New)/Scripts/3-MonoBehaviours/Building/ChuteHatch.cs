using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// I'm a toggleable hatch on a chute building — open/close via IInteractable "Toggle". When toggled,
/// two rotating parts animate between open/closed euler angles and the light indicator swaps between
/// green (open) and red (closed) materials. Same toggle pattern as RoutingConveyor but with dual
/// rotating parts and light material swap.
///
/// Who uses me: InteractionWheelUI (player presses E → "Toggle").
/// Events I fire: none. Events I subscribe to: none.
/// Phase G: implements ICustomSaveDataProvider to persist IsClosed state.
/// Phase H: DOTween replaces instant rotation with smooth lerp + toggle sound.
/// </summary>
public class ChuteHatch : MonoBehaviour, IInteractable
{
	#region Inspector Fields
	[SerializeField] bool _isClosed;
	[SerializeField] GameObject _closedObjects;
	[SerializeField] GameObject _openObjects;
	[SerializeField] Vector3 _closedRotation;
	[SerializeField] Vector3 _openRotation;
	[SerializeField] Vector3 _closedRotation2;
	[SerializeField] Vector3 _openRotation2;
	[SerializeField] Transform _rotatingPart;
	[SerializeField] Transform _rotatingPart2;
	[SerializeField] Renderer _lightRenderer;
	[SerializeField] float _rotateDuration = 0.35f;
	[SerializeField] List<SO_InteractionOption> _interactions;
	#endregion

	#region public API
	/// <summary> current open/closed state </summary>
	public bool IsClosed => _isClosed;

	/// <summary> flip between open and closed </summary>
	public void ToggleDirection() => SetDirection(!_isClosed);

	/// <summary> set direction with animation — swaps objects, rotates parts, changes light material.
	/// Phase H: replace instant rotation with DOTween DOLocalRotate. </summary>
	public void SetDirection(bool closed)
	{
		_isClosed = closed;
		// Phase H: play toggle sound
		ChangeLightMaterial(closed
			? Singleton<BuildingManager>.Ins.GetRedLightMaterial()
			: Singleton<BuildingManager>.Ins.GetGreenLightMaterial());
		// → swap active objects
		if (_closedObjects != null) _closedObjects.SetActive(closed);
		if (_openObjects != null) _openObjects.SetActive(!closed);
		// → rotate parts (Phase H: DOTween lerp instead of instant)
		_rotatingPart.localRotation = Quaternion.Euler(closed ? _closedRotation : _openRotation);
		if (_rotatingPart2 != null)
			_rotatingPart2.localRotation = Quaternion.Euler(closed ? _closedRotation2 : _openRotation2);
	}
	#endregion

	#region public API — IInteractable
	public bool ShouldUseInteractionWheel() => true;
	public string GetObjectName() => "Chute Hatch";
	public List<SO_InteractionOption> GetOptions() => _interactions;
	/// <summary> player interacted via wheel — Toggle flips direction </summary>
	public void Interact(SO_InteractionOption selectedOption)
	{
		if (selectedOption.interactionType == InteractionType.Toggle) ToggleDirection();
	}
	#endregion

	#region private API
	/// <summary> swaps the light indicator material at index 2 on the light renderer </summary>
	void ChangeLightMaterial(Material mat)
	{
		if (_lightRenderer == null) return;
		var mats = _lightRenderer.sharedMaterials;
		if (mats.Length > 2) { mats[2] = mat; _lightRenderer.sharedMaterials = mats; }
	}
	#endregion

	#region Unity Life Cycle
	/// <summary> restore state on enable (scene load or re-enable) </summary>
	private void OnEnable() => SetDirection(_isClosed);
	#endregion

	#region extra
	// Phase G: ICustomSaveDataProvider — LoadFromSave/GetCustomSaveData using RoutingConveyorSaveData
	#endregion
}