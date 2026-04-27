using UnityEngine;
using HighlightPlus;

/// <summary>
/// I outline whatever the player looks at using Highlight Plus. Every Update I raycast from
/// the camera. If I hit something with IHighlightable, I ask it for a HighlightProfile via
/// GetHighlightProfile(). If it returns a profile, I apply it. If null, I skip.
///
/// FULLY GENERIC — reusable in ANY Unity project. I only know IHighlightable (interface).
/// </summary>
[AddComponentMenu("MineMGL/Highlight/ObjectHighlighterManager")]
public class ObjectHighlighterManager : Singleton<ObjectHighlighterManager>
{
	[Header("Raycast")]
	[SerializeField] Camera _cam;
	[SerializeField] float _interactRange = 3f;
	[SerializeField] LayerMask _interactLayerMask;

	HighlightEffect currentHighlight;
	GameObject currentTarget;
	bool isMenuOpen;

	public void ClearCurrent()
	{
		if (currentHighlight != null)
		{
			currentHighlight.highlighted = false;
			currentHighlight = null;
			currentTarget = null;
		}
	}
	public void HighlightObject(GameObject target, HighlightProfile profile) => ApplyHighlight(target, profile);

	void OutlineLookedAtThing()
	{
		if (!Physics.Raycast(_cam.transform.position, _cam.transform.forward,
			out RaycastHit hit, _interactRange, _interactLayerMask))
			return;
		var highlightable = hit.collider.GetComponentInParent<IHighlightable>();
		if (highlightable == null) return;
		var profile = highlightable.GetHighlightProfile();
		if (profile == null) return;
		ApplyHighlight(((MonoBehaviour)highlightable).gameObject, profile);
	}
	void ApplyHighlight(GameObject target, HighlightProfile profile)
	{
		HighlightEffect effect = target.GetComponent<HighlightEffect>();
		if (effect == null)
		{
			effect = target.AddComponent<HighlightEffect>();
			effect.effectGroup = TargetOptions.Children;
		}
		effect.ProfileLoad(profile);
		effect.highlighted = true;
		currentHighlight = effect;
		currentTarget = target;
	}

	private void Start()
	{
		GameEvents.OnMenuStateChanged += (open) => isMenuOpen = open;
	}
	private void Update()
	{
		ClearCurrent();
		if (isMenuOpen) return;
		OutlineLookedAtThing();
	}
}