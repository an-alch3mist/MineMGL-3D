using UnityEngine;

/// <summary>
/// I show/hide a warning panel on the UI when the ore limit is reached. I subscribe to
/// OnOreLimitChanged from OreLimitManager. At Regular state I hide myself entirely. At
/// SlightlyLimited/HighlyLimited I show the soft limit warning text. At Blocked I show
/// the hard limit warning text. I'm fully decoupled from OreLimitManager — I just react
/// to the event, no direct reference.
///
/// Who uses me: Canvas (placed as a child of the main Canvas).
/// Events I subscribe to: OnOreLimitChanged.
/// </summary>
public class PhysicsLimitUIWarning : MonoBehaviour
{
	#region Inspector Fields
	[SerializeField] GameObject _softLimitObject;
	[SerializeField] GameObject _hardLimitObject;
	#endregion

	#region Unity Life Cycle
	private void Start()
	{
		// purpose: react to ore limit state changes from OreLimitManager
		GameEvents.OnOreLimitChanged += HandleLimitChanged;
		HandleLimitChanged(OreLimitState.Regular);
	}
	/// <summary> Switches between hidden (Regular), soft limit warning (Slightly/Highly), and
	/// hard limit warning (Blocked) by toggling _softLimitObject and _hardLimitObject GOs. </summary>
	void HandleLimitChanged(OreLimitState state)
	{
		switch (state)
		{
			case OreLimitState.Regular:
				gameObject.SetActive(false);
				break;
			case OreLimitState.SlightlyLimited:
			case OreLimitState.HighlyLimited:
				gameObject.SetActive(true);
				_softLimitObject.SetActive(true);
				_hardLimitObject.SetActive(false);
				break;
			case OreLimitState.Blocked:
				gameObject.SetActive(true);
				_softLimitObject.SetActive(false);
				_hardLimitObject.SetActive(true);
				break;
		}
	}
	#endregion
}