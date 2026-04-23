using UnityEngine;

/// <summary>
/// "I add ambient Perlin noise sway + one-shot view punch to camera"
/// 100% source behavior from MainMenuCameraShaker.cs (renamed — generic, reusable)
/// </summary>
public class CameraShaker : MonoBehaviour
{
	#region Inspector Fields
	[Header("Ambient Sway")]
	[SerializeField] float _posAmplitude = 0.05f;
	[SerializeField] float _rotAmplitude = 0.2f;
	[SerializeField] float _posFrequency = 0.2f;
	[SerializeField] float _rotFrequency = 0.1f;
	#endregion

	#region private API
	Vector3 initialPos;
	Quaternion initialRot;
	float timeOffset;

	Vector3 currentPunch = Vector3.zero;
	Vector3 targetPunch = Vector3.zero;
	Vector3 punchVel = Vector3.zero;
	float punchSmoothTime = 0.2f;
	float punchRecoverSpeed = 4f;
	#endregion

	#region public API
	/// <summary>Applies a one-shot rotational kick that decays back to zero.</summary>
	public void ApplyViewPunch(Vector3 punch) => targetPunch += punch;
	#endregion

	#region Unity Life Cycle
	private void Start()
	{
		initialPos = transform.localPosition;
		initialRot = transform.localRotation;
		timeOffset = Random.value * 100f;
	}

	private void Update()
	{
		float t = Time.time + timeOffset;

		Vector3 posNoise = new Vector3(
			(Mathf.PerlinNoise(t * _posFrequency, 0f) - 0.5f) * 2f,
			(Mathf.PerlinNoise(t * _posFrequency, 1f) - 0.5f) * 2f,
			(Mathf.PerlinNoise(t * _posFrequency, 2f) - 0.5f) * 2f
		) * _posAmplitude;

		Vector3 rotNoise = new Vector3(
			(Mathf.PerlinNoise(t * _rotFrequency, 3f) - 0.5f) * 2f,
			(Mathf.PerlinNoise(t * _rotFrequency, 4f) - 0.5f) * 2f,
			(Mathf.PerlinNoise(t * _rotFrequency, 5f) - 0.5f) * 2f
		) * _rotAmplitude;

		currentPunch = Vector3.SmoothDamp(currentPunch, targetPunch, ref punchVel, punchSmoothTime);
		targetPunch = Vector3.Lerp(targetPunch, Vector3.zero, Time.deltaTime * punchRecoverSpeed);

		transform.localPosition = initialPos + posNoise;
		transform.localRotation = initialRot * Quaternion.Euler(rotNoise + currentPunch);
	}
	#endregion
}