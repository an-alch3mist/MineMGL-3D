using System.Collections;
using UnityEngine;

/// <summary>
/// swinging arm that alternates ore direction — oscillates between minY and maxY with pause
/// </summary>
public class ConveyorSplitterT2 : MonoBehaviour
{
	#region Inspector Fields
	[SerializeField] float _minY = -35f;
	[SerializeField] float _maxY = 35f;
	[SerializeField] float _duration = 1.5f;
	[SerializeField] float _pauseTime = 2f;
	[SerializeField] float _idleTime = 1f;
	[SerializeField] Transform _rotatingThing;
	#endregion

	#region private API
	Coroutine swingRoutine;
	float timeSinceLastObject;
	#endregion

	#region Unity Life Cycle
	private void OnEnable()
	{
		swingRoutine = StartCoroutine(SwingLoop());
		timeSinceLastObject = 0f;
	}
	private void OnDisable()
	{
		if (swingRoutine != null) StopCoroutine(swingRoutine);
	}
	private void OnTriggerEnter(Collider other) => timeSinceLastObject = 0f;
	private void Update() => timeSinceLastObject += Time.deltaTime;
	#endregion

	#region private API — coroutine
	IEnumerator SwingLoop()
	{
		bool goingToMax = true;
		while (true)
		{
			if (timeSinceLastObject > _idleTime)
			{
				yield return new WaitForSeconds(0.25f);
				continue;
			}
			float startAngle = goingToMax ? _minY : _maxY;
			float endAngle = goingToMax ? _maxY : _minY;
			float elapsed = 0f;
			while (elapsed < _duration)
			{
				elapsed += Time.deltaTime;
				float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / _duration));
				float y = Mathf.Lerp(startAngle, endAngle, t);
				var euler = _rotatingThing.localEulerAngles;
				_rotatingThing.localEulerAngles = new Vector3(euler.x, y, euler.z);
				yield return null;
			}
			yield return new WaitForSeconds(_pauseTime);
			goingToMax = !goingToMax;
		}
	}
	#endregion
}