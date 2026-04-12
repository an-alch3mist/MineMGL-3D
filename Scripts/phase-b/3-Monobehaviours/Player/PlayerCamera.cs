using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using TMPro;

using SPACE_UTIL;

/// <summary>
/// mouseLook, fov, camBobbing, viewModelBobbing
/// </summary>
public class PlayerCamera : MonoBehaviour
{
	#region Inspector Fields
	[Header("Look")]
	[SerializeField] float _mouseSensitivity = 2f;
	[SerializeField] Camera _cam;

	[Header("FOV")]
	[SerializeField] float _baseFOV = 70f;

	[Header("Camera Bob")]
	[SerializeField] float _baseBobbingSpeed = 14f;
	[SerializeField] float _baseBobbingAmount = 0.05f;
	[SerializeField] float _baseBobbingPitchAmount = 1f;
	[SerializeField] float _baseBobbingYawAmount = 1f;

	[Header("ViewModel Bob")]
	[SerializeField] Transform _viewModelContainer;
	[SerializeField] float _viewModelBobSpeed = 8f;
	[SerializeField] float _viewModelBobAmount = 0.05f;
	[SerializeField] float _viewModelBobPitchAmount = 1.5f;
	[SerializeField] float _viewModelBobYawAmount = 1.5f;
	[SerializeField] float _viewModelLookSwayAmount = 0.05f;
	[SerializeField] float _viewModelLookSwayMax = 2f;
	[SerializeField] Vector3 _viewModelBasePos;
	[SerializeField] Vector3 _viewModelBaseRotEuler;
	[SerializeField] float _jumpBounceAmount = -0.12f;
	[SerializeField] float _landBounceAmount = 0.08f;
	[SerializeField] float _jumpSmoothTime = 0.2f;
	#endregion

	#region private API
	PlayerController pc;
	bool isAnyMenuOpen;
	float xRot;
	Vector2 lookDelta;
	// FOV
	float currentFOV;
	float fovVelocity;
	// Camera bob
	float bobbingCounter;
	float bobbingPitch, bobbingYaw, bobbingVerticalOffset;
	float bobbingPitchVel, bobbingYawVel, bobbingVerticalVel;
	float yawDirMultiplier = 1f;
	// ViewModel bob
	float viewBobCounter;
	float viewBobPitch, viewBobYaw, viewBobVertical;
	float viewBobPitchVel, viewBobYawVel, viewBobVerticalVel;
	int viewBobYawDir = 1;
	float smoothedYawSway, smoothedPitchSway;
	float yawSwayVel, pitchSwayVel;
	float jumpOffset, jumpVelocity, jumpTargetOffset;
	bool wasGroundedLastFrame;
	// View punch
	Vector3 viewPunchCurrent;
	float viewPunchTimer, viewPunchDuration;
	Vector3 viewPunchTarget;
	#endregion

	#region Unity Life Cycle
	bool isFirstEnable = true;
	private void OnEnable()
	{
		if (isFirstEnable)
		{
			pc = Singleton<PlayerController>.Ins;
			currentFOV = _baseFOV;
			_cam.fieldOfView = currentFOV;
			// purpose: lock/unlock look when menus open
			GameEvents.OnMenuStateChanged += (open) => isAnyMenuOpen = open;
			// purpose: apply view punch from elevator landing, explosions, etc.
			GameEvents.OnCamViewPunch += HandleViewPunch;
			isFirstEnable = false;
		}
	}
	private void Update()
	{
		if (Time.timeScale == 0f) return;
		if (!isAnyMenuOpen)
		{
			HandleLook();
			HandleFOV();
			HandleCameraBobbing();
			HandleViewModelBobbing();
			HandleViewPunchDecay();
		}
	}
	void HandleLook()
	{
		float mx = Input.GetAxis("Mouse X") * _mouseSensitivity;
		float my = Input.GetAxis("Mouse Y") * _mouseSensitivity;
		lookDelta = new Vector2(mx, my);
		xRot = (xRot - my).clamp(-88f, 88f);
		pc.transform.Rotate(Vector3.up * mx);
	}
	void HandleFOV()
	{
		bool sprinting = pc.GetMoveSpeed() > pc.GetWalkSpeed() && pc.GetIsGrounded();
		float targetFOV = sprinting ? _baseFOV * 1.05f : _baseFOV;
		currentFOV = Mathf.SmoothDamp(currentFOV, targetFOV, ref fovVelocity, 0.1f);
		_cam.fieldOfView = currentFOV;
	}
	void HandleCameraBobbing()
	{
		bool moving = pc.GetMoveInput().sqrMagnitude > 0.01f;
		if (!pc.GetIsGrounded())
		{
			bobbingPitch = Mathf.SmoothDamp(bobbingPitch, 0f, ref bobbingPitchVel, 0.2f);
			bobbingYaw = Mathf.SmoothDamp(bobbingYaw, 0f, ref bobbingYawVel, 0.2f);
			bobbingVerticalOffset = Mathf.SmoothDamp(bobbingVerticalOffset, 0f, ref bobbingVerticalVel, 0.2f);
		}
		else if (!moving)
		{
			bobbingPitch = Mathf.SmoothDamp(bobbingPitch, 0f, ref bobbingPitchVel, 0.1f);
			bobbingYaw = Mathf.SmoothDamp(bobbingYaw, 0f, ref bobbingYawVel, 0.1f);
			bobbingVerticalOffset = Mathf.SmoothDamp(bobbingVerticalOffset, 0f, ref bobbingVerticalVel, 0.1f);
		}
		else
		{
			float speedRatio = pc.GetMoveSpeed() / Mathf.Max(pc.GetWalkSpeed(), 0.01f);
			bobbingCounter += Time.deltaTime * _baseBobbingSpeed * speedRatio;
			if (bobbingCounter > Mathf.PI * 2f) { bobbingCounter -= Mathf.PI * 2f; yawDirMultiplier *= -1f; }
			float wave = Mathf.Sin(bobbingCounter);
			bobbingVerticalOffset = Mathf.SmoothDamp(bobbingVerticalOffset, _baseBobbingAmount * wave * speedRatio, ref bobbingVerticalVel, 0.05f);
			bobbingPitch = Mathf.SmoothDamp(bobbingPitch, wave * _baseBobbingPitchAmount * speedRatio, ref bobbingPitchVel, 0.05f);
			bobbingYaw = Mathf.SmoothDamp(bobbingYaw, wave * _baseBobbingYawAmount * speedRatio * yawDirMultiplier, ref bobbingYawVel, 0.05f);
		}
		Vector3 baseLocal = new Vector3(_cam.transform.localPosition.x, pc.GetCC().height / 2f - 0.5f, _cam.transform.localPosition.z);
		_cam.transform.localPosition = baseLocal + new Vector3(0f, bobbingVerticalOffset, 0f);
		Quaternion lookRot = Quaternion.Euler(xRot + viewPunchCurrent.x, viewPunchCurrent.y, viewPunchCurrent.z);
		Quaternion bobRot = Quaternion.Euler(bobbingPitch, bobbingYaw, 0f);
		_cam.transform.localRotation = lookRot * bobRot;
	}
	void HandleViewModelBobbing()
	{
		if (_viewModelContainer == null) return;
		bool moving = pc.GetMoveInput().sqrMagnitude > 0.01f;
		bool justJumped = wasGroundedLastFrame && !pc.GetIsGrounded();
		bool justLanded = !wasGroundedLastFrame && pc.GetIsGrounded();
		if (justJumped) jumpTargetOffset = _jumpBounceAmount;
		else if (justLanded) jumpTargetOffset = _landBounceAmount;
		jumpOffset = Mathf.SmoothDamp(jumpOffset, jumpTargetOffset, ref jumpVelocity, _jumpSmoothTime);
		jumpTargetOffset = Mathf.MoveTowards(jumpTargetOffset, 0f, Time.deltaTime * Mathf.Abs(jumpTargetOffset / _jumpSmoothTime));
		wasGroundedLastFrame = pc.GetIsGrounded();

		if (!pc.GetIsGrounded() || !moving)
		{
			float smoothTime = pc.GetIsGrounded() ? 0.1f : 0.2f;
			viewBobPitch = Mathf.SmoothDamp(viewBobPitch, 0f, ref viewBobPitchVel, smoothTime);
			viewBobYaw = Mathf.SmoothDamp(viewBobYaw, 0f, ref viewBobYawVel, smoothTime);
			viewBobVertical = Mathf.SmoothDamp(viewBobVertical, 0f, ref viewBobVerticalVel, smoothTime);
		}
		else
		{
			float sr = pc.GetMoveSpeed() / Mathf.Max(pc.GetWalkSpeed(), 0.01f);
			viewBobCounter += Time.deltaTime * _viewModelBobSpeed * sr;
			if (viewBobCounter > Mathf.PI * 2f) { viewBobCounter -= Mathf.PI * 2f; viewBobYawDir *= -1; }
			float wave = Mathf.Sin(viewBobCounter);
			viewBobVertical = Mathf.SmoothDamp(viewBobVertical, _viewModelBobAmount * wave * sr, ref viewBobVerticalVel, 0.05f);
			viewBobPitch = Mathf.SmoothDamp(viewBobPitch, wave * _viewModelBobPitchAmount * sr, ref viewBobPitchVel, 0.05f);
			viewBobYaw = Mathf.SmoothDamp(viewBobYaw, wave * _viewModelBobYawAmount * sr * viewBobYawDir, ref viewBobYawVel, 0.05f);
		}
		if (float.IsNaN(viewBobVertical)) viewBobVertical = 0f;
		if (float.IsNaN(jumpOffset)) jumpOffset = 0f;
		_viewModelContainer.localPosition = _viewModelBasePos + new Vector3(0f, viewBobVertical + jumpOffset, 0f);

		float swayTargetY = Mathf.Clamp(lookDelta.x * _viewModelLookSwayAmount, -_viewModelLookSwayMax, _viewModelLookSwayMax);
		float swayTargetP = Mathf.Clamp(-lookDelta.y * _viewModelLookSwayAmount, -_viewModelLookSwayMax, _viewModelLookSwayMax);
		if (float.IsNaN(yawSwayVel)) yawSwayVel = 0f;
		if (float.IsNaN(pitchSwayVel)) pitchSwayVel = 0f;
		smoothedYawSway = Mathf.SmoothDamp(smoothedYawSway, swayTargetY, ref yawSwayVel, 0.06f);
		smoothedPitchSway = Mathf.SmoothDamp(smoothedPitchSway, swayTargetP, ref pitchSwayVel, 0.06f);
		Quaternion bobQ = Quaternion.Euler(viewBobPitch + smoothedPitchSway, viewBobYaw + smoothedYawSway, 0f);
		_viewModelContainer.localRotation = Quaternion.Euler(_viewModelBaseRotEuler) * bobQ;
	}
	void HandleViewPunch(Vector3 punchAmount, float duration)
	{
		viewPunchTarget = punchAmount;
		viewPunchDuration = duration;
		viewPunchTimer = duration;
	}
	void HandleViewPunchDecay()
	{
		if (viewPunchTimer > 0f)
		{
			viewPunchTimer -= Time.deltaTime;
			float t = Mathf.Clamp01(viewPunchTimer / viewPunchDuration);
			viewPunchCurrent = viewPunchTarget * t;
		}
		else viewPunchCurrent = Vector3.zero;
	}
	private void OnDestroy()
	{
		GameEvents.OnCamViewPunch -= HandleViewPunch;
	}
	#endregion
}
