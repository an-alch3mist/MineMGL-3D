using System.Collections.Generic;
using UnityEngine;
// ──────────────────────────────────────────────────────────────
// TEMPORARY: URP Renderer Feature + Shader Graph fresnel approach.
// When Highlight Plus is imported, replace the layer-swap logic
// with HighlightEffect.SetHighlighted(true/false) per object.
// ──────────────────────────────────────────────────────────────

/// <summary>
/// I outline whatever the player looks at with a fresnel rim glow. Every Update I raycast
/// from the camera. If I hit a tool/grabbable, I swap its layer to "Highlighted" — the URP
/// Renderer Feature renders it a second time with the additive fresnel material.
/// ClearAll restores original layers each frame. Phase D adds building + wrench colors.
///
/// SETUP REQUIRED:
///   1. Create "Highlighted" layer in Unity (Edit → Project Settings → Tags and Layers)
///   2. Create Shader Graph: Highlight_Fresnel_Additive (see GUIDE.md Art section)
///   3. Create Material M_Highlight_Fresnel from that shader
///   4. URP Renderer Data → Add Renderer Feature → Render Objects:
///      Event=AfterRenderingOpaques, Layer="Highlighted", Override Material=M_Highlight_Fresnel
///   5. Assign _highlightLayer in inspector → "Highlighted"
///
/// Who uses me: Self (Update raycasts from camera). No external callers.
/// Events I fire: none. Events I subscribe to: none.
/// </summary>
public class FresnelHighlighter : MonoBehaviour
{
	#region Inspector Fields
	[Header("Raycast")]
	[SerializeField] Camera _cam;
	[SerializeField] float _interactRange = 2f;
	[SerializeField] LayerMask _interactLayerMask;
	[Header("Highlight Layer (URP Renderer Feature renders this layer with fresnel material)")]
	[SerializeField] int _highlightLayer = 31;
	[Header("Color Overrides (via MaterialPropertyBlock on highlighted renderers)")]
	[SerializeField] Color _toolColor = new Color(0.25f, 0.85f, 1f, 1f);
	[SerializeField] Color _grabbableColor = new Color(0.25f, 0.85f, 1f, 1f);
	// Phase D: [SerializeField] Color _buildingColor, _wrenchEnableColor, _wrenchDisableColor
	#endregion

	#region private API
	struct HighlightedEntry { public GameObject Go; public int OriginalLayer; }
	readonly List<HighlightedEntry> highlighted = new List<HighlightedEntry>();
	static readonly int ColorID = Shader.PropertyToID("_Color");
	MaterialPropertyBlock mpb;

	void HighlightObject(GameObject obj, Color color)
	{
		int origLayer = obj.layer;
		UtilsPhaseB.SetLayerRecursively(obj, _highlightLayer);
		highlighted.Add(new HighlightedEntry { Go = obj, OriginalLayer = origLayer });
		if (mpb == null) mpb = new MaterialPropertyBlock();
		foreach (var r in obj.GetComponentsInChildren<Renderer>(false))
		{
			if (r is ParticleSystemRenderer || !r.enabled) continue;
			mpb.SetColor(ColorID, color);
			r.SetPropertyBlock(mpb);
		}
	}
	void ClearAll()
	{
		foreach (var entry in highlighted)
		{
			if (entry.Go == null) continue;
			UtilsPhaseB.SetLayerRecursively(entry.Go, entry.OriginalLayer);
			foreach (var r in entry.Go.GetComponentsInChildren<Renderer>(false))
				r.SetPropertyBlock(null);
		}
		highlighted.Clear();
	}
	void OutlineLookedAtThing()
	{
		if (!Physics.Raycast(_cam.transform.position, _cam.transform.forward, out RaycastHit hit, _interactRange, _interactLayerMask))
			return;
		if (hit.collider.GetComponentInParent<BaseHeldTool>() != null)
		{
			HighlightObject(hit.collider.gameObject, _toolColor);
			return;
		}
		if (hit.collider.HasTag(TagType.Grabbable))
		{
			HighlightObject(hit.collider.gameObject, _grabbableColor);
			return;
		}
		// Phase A: ComputerTerminal, ContractsTerminal → _toolColor
		// Phase D: BuildingObject (ToolHammer → _buildingColor, ToolSupportsWrench → _wrenchEnable/_wrenchDisableColor)
	}
	#endregion

	#region Unity Life Cycle
	private void Update()
	{
		ClearAll();
		OutlineLookedAtThing();
	}
	#endregion
}