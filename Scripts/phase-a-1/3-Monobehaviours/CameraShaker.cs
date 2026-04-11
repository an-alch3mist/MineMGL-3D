using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;

public class CameraShaker : MonoBehaviour
{
	#region inspector fields
	[SerializeField] Transform _player;
	[SerializeField] float _posAmp = 0.05f, _rotAmp = 0.2f, _posFreq = 0.2f, _rotFreq = 0.1f;
	#endregion

	#region private API
	Vector3 initalPos;
	Quaternion initialRot;
	float timeOffset;

	Vector3 currPunch, targetPunch, punchVel;
	float punchSmoothTime = 0.2f, punchRecoverTime = 4f;
	#endregion

	#region Unity Life Cycle
	private void LateUpdate()
	{
		if (elapsed < 0f)
			return;
		elapsed -= Time.deltaTime;

		float t = Time.time * timeOffset;
		Vector3 posNoise = new Vector3(
			(Mathf.PerlinNoise(t * this._posFreq, 0f) - 0.5f) * 2f,
			(Mathf.PerlinNoise(t * this._posFreq, 1f) - 0.5f) * 2f,
			(Mathf.PerlinNoise(t * this._posFreq, 2f) - 0.5f) * 2f
		) * this._posAmp;
		Vector3 rotNoise = new Vector3(
			(Mathf.PerlinNoise(t * this._rotFreq, 3f) - 0.5f) * 2f,
			(Mathf.PerlinNoise(t * this._rotFreq, 4f) - 0.5f) * 2f,
			(Mathf.PerlinNoise(t * this._rotFreq, 5f) - 0.5f) * 2f
		) * this._rotAmp;

		currPunch = Vector3.SmoothDamp(currPunch, targetPunch, ref punchVel, punchSmoothTime);
		targetPunch = Vector3.Lerp(targetPunch, Vector3.zero, Time.deltaTime * punchRecoverTime);

		this._player.localPosition = initalPos + posNoise;
		this._player.rotation = initialRot * Quaternion.Euler(rotNoise + currPunch);
	}

	private void OnEnable()
	{
		Debug.Log(C.method(this));
		GameEvents.OnCamViewPunch += HandleViewPunch;
	}
	private void OnDisable()
	{
		Debug.Log(C.method(this, "orange"));
		GameEvents.OnCamViewPunch -= HandleViewPunch;
	}
	float elapsed = -1f;
	void HandleViewPunch(Vector3 punchAmount, float duration = 3f)
	{
		initalPos = this._player.position;
		initialRot = this._player.rotation;
		timeOffset = C.Random(0, 100);

		targetPunch += punchAmount;
		elapsed = duration;
	}
	#endregion
}
