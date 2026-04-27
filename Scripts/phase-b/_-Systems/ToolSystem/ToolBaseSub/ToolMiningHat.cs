using UnityEngine;

/// <summary> LMB toggles light on/off. Interaction wheel "Toggle" does the same. </summary>
[AddComponentMenu("MineMGL/Tools/ToolMiningHat")]
public class ToolMiningHat : BaseHeldTool
{
	[SerializeField] GameObject _worldModelLight;
	[SerializeField] GameObject _viewModelLight;
	bool isOn;

	void ToggleLight(bool enable)
	{
		isOn = enable;
		_worldModelLight.SetActive(isOn);
		_viewModelLight.SetActive(isOn);
	}

	public override void PrimaryFire() => ToggleLight(!isOn);
	public override void Interact(SO_InteractionOption selectedOption)
	{
		if (selectedOption.interactionType == InteractionType.toggle) ToggleLight(!isOn);
		else base.Interact(selectedOption);
	}
	protected override void OnEnable() { base.OnEnable(); ToggleLight(isOn); }
	protected override void OnDisable() { base.OnDisable(); ToggleLight(isOn); }
}