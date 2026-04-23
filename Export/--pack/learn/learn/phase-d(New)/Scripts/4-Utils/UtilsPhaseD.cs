using UnityEngine;

/// <summary>
/// grid math + building placement helpers
/// </summary>
public static class UtilsPhaseD
{
	/// <summary> checks if OverlapBox at collider bounds detects any collision on layerMask </summary>
	public static bool IsOverlapping(Collider col, LayerMask layerMask)
	{
		if (col is BoxCollider box)
		{
			Vector3 center = box.transform.TransformPoint(box.center);
			Vector3 halfExtents = Vector3.Scale(box.size * 0.5f, box.transform.lossyScale);
			return Physics.OverlapBox(center, halfExtents, box.transform.rotation, layerMask, QueryTriggerInteraction.Ignore).Length > 0;
		}
		return false;
	}
	/// <summary> checks if surface normal is flat enough for building (dot with up > 0.9) </summary>
	public static bool IsFlatGround(Vector3 position, LayerMask groundLayer, out float distance)
	{
		distance = 0f;
		if (!Physics.Raycast(position + new Vector3(0.5f, 0.1f, 0.5f), Vector3.down, out var hit, 1f, groundLayer))
			return false;
		distance = hit.distance;
		return Vector3.Dot(hit.normal, Vector3.up) >= 0.9f && hit.distance <= 0.2f;
	}
}