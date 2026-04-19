/*
	Enums shared across ALL phases — lives in phase-@all so every phase can reference them.
	These grow as phases add new values. Unity project tags must match TagType values.
*/
/// <summary>
/// Used via extension methods in UtilsPhaseAll: gameObject.SetTag(TagType.x), collider.HasTag(TagType.x).
/// When adding a new tag here, also add it in Unity: Edit → Project Settings → Tags and Layers. </summary>
/// </summary>
public enum TagType
{
	unTagged,
	grabbable,
	markedForDestruction,
	// conveyorBelt, // phaseD
}