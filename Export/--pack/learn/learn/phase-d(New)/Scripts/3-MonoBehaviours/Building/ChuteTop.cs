using System.Collections;
using UnityEngine;

/// <summary>
/// I'm the top piece of a chute building. On enable, I raycast upward to check if a Hopper is
/// placed directly above me. If so, I replace myself with the hopper-compatible chute version
/// prefab (different mesh that connects visually to the hopper). This runs after a 1-frame delay
/// to ensure all buildings in the scene have finished their Awake/Start cycle.
///
/// Who uses me: Self (OnEnable triggers check). No external callers.
/// Events I fire: none.
/// </summary>
public class ChuteTop : MonoBehaviour
{
	#region Inspector Fields
	/// <summary> the chute variant prefab that connects to a hopper above </summary>
	[SerializeField] GameObject _hopperChuteVersionPrefab;
	#endregion

	#region public API
	/// <summary> replaces this chute piece with the hopper-compatible version at same position/rotation </summary>
	public void ConvertToHopperVersion()
	{
		Instantiate(_hopperChuteVersionPrefab, transform.position, transform.rotation);
		Destroy(gameObject);
	}
	#endregion

	#region Unity Life Cycle
	/// <summary> wait 1 frame then raycast up — if Hopper above, convert to hopper version </summary>
	private void OnEnable() => StartCoroutine(WaitThenCheckForHopperAbove());
	#endregion

	#region private API
	IEnumerator WaitThenCheckForHopperAbove()
	{
		if (Singleton<BuildingManager>.Ins == null) yield break;
		yield return new WaitForEndOfFrame();
		if (!enabled) yield break;
		// → raycast up 1m to check for Hopper building above
		Vector3 origin = transform.position + Vector3.up * 0.25f;
		if (Physics.Raycast(origin, Vector3.up, out var hit, 1f,
			Singleton<BuildingManager>.Ins.GetBuildingPlacementCollisionLayers()))
		{
			// Phase E: check for Hopper component
			// if (hit.collider.GetComponentInParent<Hopper>() != null) ConvertToHopperVersion();
		}
	}
	#endregion
}