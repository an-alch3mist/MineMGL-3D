using UnityEngine;
using HighlightPlus;

// ═══════════════════════════════════════════════════════════════
// IHighlightable — interface for anything that can be outlined
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Any object that can be outlined when the player looks at it. ObjectHighlighterManager raycasts
/// each frame and calls GetHighlightProfile on the first IHighlightable it hits. The object
/// decides which profile to return (or null to skip).
///
/// Who implements me: any MonoBehaviour that should be outlineable on hover.
/// Who calls me: ObjectHighlighterManager.OutlineLookedAtThing().
/// </summary>
public interface IHighlightable
{
	/// <summary> Returns the HighlightProfile to use when the player looks at this object.
	/// Return null to skip highlighting. The object decides its own logic internally. </summary>
	HighlightProfile GetHighlightProfile();
}

// ObjectHighlighterManager lives in its own file: ObjectHighlighterManager.cs
// (Unity 6000.3 requires filename == classname for Add Component search)