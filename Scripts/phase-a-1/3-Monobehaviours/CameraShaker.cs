using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;

public class CameraShaker : MonoBehaviour
{
	#region inspector fields
	[SerializeField] Transform _player;
	[SerializeField] float _posAmp = 0.05f, _rotAmp = 0.4f, _posFreq = 0.2f, _rotFreq = 0.2f;
	#endregion

	#region private API
	Vector3 initalPos;
	Quaternion initialRot;
	float timeOffset;

	Vector3 currPunchEuRot, targetPunchEuRot, punchVel;
	float punchSmoothTime = 0.2f, punchRecoverTime = 4f;
	#endregion

	#region Unity Life Cycle
	// apply on top of movement Update() WASD which was already performed in the playerController.
	private void LateUpdate()
	{
		// do nothing if elapsedTime below zero
		if (elapsedTime < 0f)
			return;
		elapsedTime -= Time.deltaTime;

		#region posNoise, rotNoise based on Time.time, freq, amp vals
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
		#endregion

		currPunchEuRot = Vector3.SmoothDamp(currPunchEuRot, targetPunchEuRot, ref punchVel, punchSmoothTime);
		targetPunchEuRot = Vector3.Lerp(targetPunchEuRot, Vector3.zero, Time.deltaTime * punchRecoverTime);

		// the character controller shall be stunned at this movement no gravity works
		this._player.localPosition = initalPos + posNoise;
		this._player.rotation = initialRot * Quaternion.Euler(rotNoise + currPunchEuRot);
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
	float elapsedTime = -1f;
	void HandleViewPunch(Vector3 punchAmount, float duration = 3f)
	{
		initalPos = this._player.position;
		initialRot = this._player.rotation;
		timeOffset = C.Random(0, 100);

		targetPunchEuRot += punchAmount;
		elapsedTime = duration;
	}
	#endregion
}
