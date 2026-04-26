using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

using HighlightPlus;

using SPACE_UTIL;
// required: URP Renderer Data → Add Renderer Feature → Highlight Plus


/// <summary>
/// I outline whatever the player looks at using Highlight Plus. Every Update I raycast from
/// the camera. If I hit something with IHighlightable, I ask it for a HighlightProfile via
/// GetHighlightProfile(activeTool). If it returns a profile, I apply it. If null, I skip.
///
/// This is fully decoupled — ObjectHighlighterManager doesn't know about specific types (tools,
/// buildings, computers). Each object decides its OWN profile via IHighlightable. New phases
/// just implement the interface on their new scripts — no changes to this file ever.
///
/// SETUP REQUIRED:
///   1. Import Highlight Plus package (Runtime + Editor + Demo/Profiles)
///   2. URP Renderer Data → Add Renderer Feature → Highlight Plus
///   3. Create HighlightProfile assets (see ShaderGuide.md)
///   4. Assign _cam + _interactLayerMask in inspector
///   5. Implement IHighlightable on any object that should be outlineable
///
/// Who uses me: Self (Update raycasts). External: PauseMenu (ClearCurrent), tutorial (HighlightObject).
/// Events I fire: none. Events I subscribe to: OnMenuStateChanged, OnToolEquipped, OnItemDropped.
/// </summary>
public class ObjectHighlighterManager : Singleton<ObjectHighlighterManager>
{

	#region Inspector Fields
	[Header("Raycast")]
	[SerializeField] Camera _cam;
	[SerializeField] float _interactRange = 3f;
	[SerializeField] LayerMask _highlightableLayerMask;
	#endregion

	#region private API
	HighlightEffect currentHighlight;
	GameObject currentTarget;
	bool isAnyMenuOpen;
	BaseHeldTool activeTool;
	#endregion

	#region public API
	/// <summary> Disables the current highlight. External scripts (PauseMenu, tutorial) can call
	/// this to force-clear when needed (e.g. menu opens, tutorial step changes). </summary>
	public void ClearCurrent()
	{
		if (currentHighlight != null)
		{
			currentHighlight.highlighted = false;
			currentHighlight = null;
			currentTarget = null;
		}
	}
	/// <summary> Highlights a specific object with the given profile. External scripts can call this
	/// to programmatically highlight objects (e.g. tutorial highlighting a specific tool). The highlight
	/// stays until ClearCurrent is called (which happens every frame in Update). </summary>
	public void HighlightObject(GameObject target, HighlightProfile profile)
	{
		ApplyHighlight(target, profile);
	}
	#endregion

	#region private API — raycast + apply
	/// <summary> Single raycast from camera forward. Finds first IHighlightable on the hit object
	/// (via GetComponentInParent). Asks it for a HighlightProfile (passing activeTool so objects like
	/// BuildingObject can return different colors per tool). If profile is non-null, applies it.
	/// Zero type checks — each object owns its highlight logic via IHighlightable. </summary>
	void OutlineLookedAtThing()
	{
		if (!Physics.Raycast(_cam.transform.position, _cam.transform.forward,
			out RaycastHit hit, _interactRange, _highlightableLayerMask))
			return;
		// → find IHighlightable on hit object or its parents
		var highlightable = hit.collider.GetComponentInParent<IHighlightable>();
		if (highlightable == null)
			return;
		// → ask the object what profile it wants (null = skip highlighting)
		var profile = highlightable.GetHighlightProfile(activeTool);
		if (profile == null) return;
		// → apply highlight on the GameObject that has the IHighlightable
		ApplyHighlight(((MonoBehaviour)highlightable).gameObject, profile);
	}
	/// <summary> Gets or adds a HighlightEffect on the target, loads the profile, and turns it on.
	/// HighlightEffect is added at runtime (not pre-placed on prefabs) so any object can be highlighted.
	/// effectGroup = Children ensures ALL child meshes get the outline (e.g. tool with multiple parts). </summary>
	void ApplyHighlight(GameObject target, HighlightProfile profile)
	{
		// → get existing HighlightEffect or add one at runtime
		HighlightEffect effect = target.GetComponent<HighlightEffect>();
		if (effect == null)
		{
			effect = target.AddComponent<HighlightEffect>();
			effect.effectGroup = TargetOptions.Children;
		}
		// → load profile settings (outline color, width, glow, etc.)
		effect.ProfileLoad(profile);
		// → turn on the highlight
		effect.highlighted = true;
		// → store reference for ClearCurrent next frame
		currentHighlight = effect;
		currentTarget = target;
	}
	#endregion

	#region Unity Life Cycle
	/// <summary> Subscribes to menu state (blocks highlight) and tool equip (tracks active tool
	/// so IHighlightable implementors can decide profile based on what the player is holding). </summary>
	private void Start()
	{
		// purpose: block highlight when menu is open
		GameEvents.OnMenuStateChanged += (isAnyMenuOpen) => this.isAnyMenuOpen = isAnyMenuOpen;
		// purpose: track active tool — IHighlightable.GetHighlightProfile receives this
		GameEvents.OnToolEquipped += (tool) => activeTool = tool;
		// purpose: clear active tool when item is dropped
		GameEvents.OnItemDropped += (tool) => { if (activeTool == tool) activeTool = null; };
	}
	/// <summary> Every frame: clears previous highlight, then raycasts (unless menu is open).
	/// Finds IHighlightable on hit → asks for profile → applies. Zero type checks in this script. </summary>
	private void Update()
	{
		ClearCurrent();
		if (isAnyMenuOpen)
			return;
		OutlineLookedAtThing();
	}
	#endregion
}
