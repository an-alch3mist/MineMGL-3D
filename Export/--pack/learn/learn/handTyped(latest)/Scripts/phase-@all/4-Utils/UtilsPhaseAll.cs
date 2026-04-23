using UnityEngine;

/// <summary>
/// I hold extension methods shared across ALL phases. Since these are C# extensions, you just
/// call them directly on the object (collider.HasTag, gameObject.SetTag) � no need to reference
/// UtilsPhaseAll by name. They're automatically available everywhere because they're static
/// extensions in a static class.
/// </summary>
/// 
public static class UtilsPhaseAll
{
	#region TagType extension helpers.
	/// <summary> Compares a collider's tag against the TagType enum � replaces CompareTag("string") </summary>
	public static bool HasTag(this Collider col, TagType tag) => col.CompareTag(tag.ToString());
	/// <summary> Compares a gameObject's tag against the TagType enum </summary>
	public static bool HasTag(this GameObject go, TagType tag) => go.CompareTag(tag.ToString());
	/// <summary> Sets a gameObject's tag from the TagType enum � replaces go.tag = "string" </summary>
	public static void SetTag(this GameObject go, TagType tag) => go.tag = tag.ToString();
	#endregion
}