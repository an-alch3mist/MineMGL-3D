using UnityEngine;

/// <summary>
/// I'm a wearable mining hat that toggles a light. Left-click toggles isOn which shows/hides
/// both _worldModelLight and _viewModelLight. On equip (OnEnable) and unequip (OnDisable) I
/// restore the light to its current state. I also override Interact for the "Toggle" interaction
/// wheel option. PlayerMovement has a separate dual-light system (nightVision vs miningHat)
/// that ToolMiningHat can call via ToggleMiningLightFromTool.
///
/// Who uses me: InventoryOrchestrator (PrimaryFire), InteractionWheel ("Toggle").
/// </summary>
public class ToolMiningHat : BaseHeldTool
{
	#region Inspector Fields
	[SerializeField] GameObject _worldModelLight;
	[SerializeField] GameObject _viewModelLight;
	#endregion

	#region private API
	bool isOn;
	void ToggleLight(bool enable)
	{
		isOn = enable;
		_worldModelLight.SetActive(isOn);
		_viewModelLight.SetActive(isOn);
		// Phase H: play toggle sound
	}
	#endregion

	#region public API — overrides
	public override void PrimaryFire() => ToggleLight(!isOn);
	public override void Interact(SO_InteractionOption selectedOption)
	{
		if (selectedOption.interactionType == InteractionType.Toggle) ToggleLight(!isOn);
		else base.Interact(selectedOption);
	}
	#endregion

	#region Unity Life Cycle
	protected override void OnEnable() { base.OnEnable(); ToggleLight(isOn); }
	protected override void OnDisable() { base.OnDisable(); ToggleLight(isOn); }
	#endregion
}