using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

using SPACE_UTIL;

/// <summary>
/// I handle everything the camera does — mouse look (X/Y rotation), FOV changes during sprint,
/// camera head bob while walking, viewmodel bob (subtle hand sway), and view punch (recoil from
/// explosions or elevator landing). I read PlayerMovement's speed and grounded state to scale
/// bob intensity. I subscribe to OnCamViewPunch for external systems (StartingElevator, future
/// explosions) to shake the camera. OnMenuStateChanged is handled by PlayerMovement — I just
/// read isAnyMenuOpen to skip look input.
///
/// Who uses me: PlayerMovement (I'm on the same GO). StartingElevator (view punch event).
/// Events I subscribe to: OnCamViewPunch (camera shake from external sources).
/// </summary>
public class PlayerCamera : MonoBehaviour
{
	#region Inspector Fields
	[Header("Look")]
	[SerializeField] float _mouseSensitivity = 2f;
	[SerializeField] Camera _cam;
	[SerializeField] PlayerController _pc;

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
	/// <summary> Initializes FOV to base value and subscribes to OnCamViewPunch so external
	/// systems (elevator landing, explosions) can shake the camera. </summary>
	private void Start()
	{
		currentFOV = _baseFOV;
		_cam.fieldOfView = currentFOV;
		// purpose: lock/unlock look when menus open
		GameEvents.OnMenuStateChanged += (open) => isAnyMenuOpen = open;
		// purpose: apply view punch from elevator landing, explosions, etc.
		GameEvents.OnCamViewPunch += HandleViewPunch;
	}
	/// <summary> Every frame: handles mouse look (when no menu open), FOV sprint widening,
	/// camera head bob, viewmodel hand sway, and view punch decay from explosions. </summary>
	private void Update()
	{
		if (Time.timeScale == 0f) return;
		// → skip look input when any menu is open
		if (!isAnyMenuOpen)
		{
			HandleLook();
			HandleCameraBobbing();
			HandleViewModelBobbing();
		}
		HandleFOV();
		HandleViewPunchDecay();
	}
	/// <summary> Reads Mouse X/Y, applies sensitivity, rotates player around Y-axis (horizontal)
	/// and tilts camera around X-axis (vertical, clamped to ±88°). </summary>
	void HandleLook()
	{
		float mx = Input.GetAxis("Mouse X") * _mouseSensitivity;
		float my = Input.GetAxis("Mouse Y") * _mouseSensitivity;
		lookDelta = new Vector2(mx, my);
		xRot = (xRot - my).clamp(-88f, 88f);
		_pc.transform.Rotate(Vector3.up * mx);
	}
	/// <summary> Widens FOV by 5% when sprinting, smoothly returns to base FOV when stopping. </summary>
	void HandleFOV()
	{
		bool sprinting = _pc.SelectedWalkSpeed > _pc.WalkSpeed && _pc.IsGrounded;
		float targetFOV = sprinting ? _baseFOV * 1.05f : _baseFOV;
		currentFOV = Mathf.SmoothDamp(currentFOV, targetFOV, ref fovVelocity, 0.1f);
		_cam.fieldOfView = currentFOV;
	}
	/// <summary> Applies head bob (subtle pitch/yaw oscillation) based on movement speed and
	/// grounded state. Faster movement = faster + larger bob. Airborne = no bob. </summary>
	void HandleCameraBobbing()
	{
		bool moving = _pc.MoveInput.sqrMagnitude > 0.01f;
		if (!_pc.IsGrounded)
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
			float speedRatio = _pc.SelectedWalkSpeed / Mathf.Max(_pc.WalkSpeed, 0.01f);
			bobbingCounter += Time.deltaTime * _baseBobbingSpeed * speedRatio;
			if (bobbingCounter > Mathf.PI * 2f) { bobbingCounter -= Mathf.PI * 2f; yawDirMultiplier *= -1f; }
			float wave = Mathf.Sin(bobbingCounter);
			bobbingVerticalOffset = Mathf.SmoothDamp(bobbingVerticalOffset, _baseBobbingAmount * wave * speedRatio, ref bobbingVerticalVel, 0.05f);
			bobbingPitch = Mathf.SmoothDamp(bobbingPitch, wave * _baseBobbingPitchAmount * speedRatio, ref bobbingPitchVel, 0.05f);
			bobbingYaw = Mathf.SmoothDamp(bobbingYaw, wave * _baseBobbingYawAmount * speedRatio * yawDirMultiplier, ref bobbingYawVel, 0.05f);
		}
		Vector3 baseLocal = new Vector3(_cam.transform.localPosition.x, _pc.CC.height / 2f - 0.5f, _cam.transform.localPosition.z);
		_cam.transform.localPosition = baseLocal + new Vector3(0f, bobbingVerticalOffset, 0f);
		Quaternion lookRot = Quaternion.Euler(xRot + viewPunchCurrent.x, viewPunchCurrent.y, viewPunchCurrent.z);
		Quaternion bobRot = Quaternion.Euler(bobbingPitch, bobbingYaw, 0f);
		_cam.transform.localRotation = lookRot * bobRot;
	}
	/// <summary> Applies hand sway to the ViewModel container — similar to camera bob but with
	/// separate frequency/amplitude for a natural first-person hand movement feel. </summary>
	void HandleViewModelBobbing()
	{
		if (_viewModelContainer == null) return;
		bool moving = _pc.MoveInput.sqrMagnitude > 0.01f;
		bool justJumped = wasGroundedLastFrame && !_pc.IsGrounded;
		bool justLanded = !wasGroundedLastFrame && _pc.IsGrounded;
		if (justJumped) jumpTargetOffset = _jumpBounceAmount;
		else if (justLanded) jumpTargetOffset = _landBounceAmount;
		jumpOffset = Mathf.SmoothDamp(jumpOffset, jumpTargetOffset, ref jumpVelocity, _jumpSmoothTime);
		jumpTargetOffset = Mathf.MoveTowards(jumpTargetOffset, 0f, Time.deltaTime * Mathf.Abs(jumpTargetOffset / _jumpSmoothTime));
		wasGroundedLastFrame = _pc.IsGrounded;

		if (!_pc.IsGrounded || !moving)
		{
			float smoothTime = _pc.IsGrounded ? 0.1f : 0.2f;
			viewBobPitch = Mathf.SmoothDamp(viewBobPitch, 0f, ref viewBobPitchVel, smoothTime);
			viewBobYaw = Mathf.SmoothDamp(viewBobYaw, 0f, ref viewBobYawVel, smoothTime);
			viewBobVertical = Mathf.SmoothDamp(viewBobVertical, 0f, ref viewBobVerticalVel, smoothTime);
		}
		else
		{
			float sr = _pc.SelectedWalkSpeed / Mathf.Max(_pc.WalkSpeed, 0.01f);
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
	/// <summary> Called via OnCamViewPunch event — sets a target punch rotation that decays
	/// over the given duration. Used by elevator landing and future explosion effects. </summary>
	void HandleViewPunch(Vector3 punchAmount, float duration)
	{
		viewPunchTarget = punchAmount;
		viewPunchDuration = duration;
		viewPunchTimer = duration;
	}
	/// <summary> Gradually reduces the view punch rotation back to zero over the duration set
	/// by HandleViewPunch. Applied to the camera's local rotation each frame. </summary>
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
	/// <summary> Unsubscribes from OnCamViewPunch to prevent null ref if camera is destroyed
	/// before the event bus (e.g. scene unload). </summary>
	private void OnDestroy()
	{
		GameEvents.OnCamViewPunch -= HandleViewPunch;
	}
	#endregion
}