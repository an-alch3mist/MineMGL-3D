/// <summary>
/// Enums shared across ALL phases — lives in phase-All so every phase can reference them.
/// These grow as phases add new values. Unity project tags must match TagType values.
/// </summary>

/// <summary> Unity inspector tags as enum — replaces magic strings like "Grabbable".
/// Used via extension methods in UtilsPhaseB: gameObject.SetTag(TagType.X), collider.HasTag(TagType.X).
/// When adding a new tag here, also add it in Unity: Edit → Project Settings → Tags and Layers. </summary>
public enum TagType
{
	Untagged,
	Grabbable,
	MarkedForDestruction,
	// Phase D:
	// ConveyorBelt,
}