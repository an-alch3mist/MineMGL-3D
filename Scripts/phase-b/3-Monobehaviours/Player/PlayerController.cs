using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

using SPACE_UTIL;

/// <summary>
/// walk, sprint, duck, jump, slope sliding - replaces SimplePlayerController
/// </summary>
public class PlayerController : Singleton<PlayerController>
{
	#region Inspector Fields
	[Header("Move")]
	[SerializeField] float _walkSpeed = 3f;
	[SerializeField] float _sprintSpeed = 6f;
	[SerializeField] float _duckSpeed = 2f;
	[SerializeField] float _jumpHeight = 2f;
	[SerializeField] float _gravity = -10f;
	[SerializeField] float _slideSpeed = 8f;
	[SerializeField] float _standingSlopeLimit = 75f;
	[SerializeField] float _duckHeight = 1f;
	[SerializeField] float _standingHeight = 2f;
	[SerializeField] float _duckingSpeed = 10f;

	[SerializeField] CharacterController _cc;
	// [SerializeField] Transform _groundCheck;
	[SerializeField] LayerMask _groundLayer;
	// [SerializeField] Transform _characterModel;

	[Header("References (Owner chain) For Tools")]
	[SerializeField] Camera _playerCam;
	[SerializeField] Transform _viewModelContainer;
	[SerializeField] Transform _holdPosition;
	[SerializeField] Transform _magnetToolPosition;
	[SerializeField] LayerMask _interactLayerMask;
	[Header("Mining Hat Lights (nice to have)")]
	[SerializeField] GameObject _nightVisionLight;
	[SerializeField] GameObject _miningHatLight;
	#endregion

	#region private API fields
	bool isAnyMenuOpen;
	Vector2 moveInput;
	Vector3 velocity;
	float xRot;
	float yRot;

	bool isGrounded;
	bool isDucking;
	bool isUsingNoclip;
	bool isminingLampEnabled;
	float cameraHeightVelocity;
	float selectedWalkSpeed;
	#endregion

	#region Unity Life Cycle
	bool isFirstEnable = true;
	private void OnEnable()
	{
		Debug.Log(C.method(this));
		if(isFirstEnable)
		{
			INPUT.UI.SetCursor(isFpsMode: true);
			// purpose: lock/unlock movement when menus open
			GameEvents.OnMenuStateChanged += (isAnyMenuOpen) => this.isAnyMenuOpen = isAnyMenuOpen;
			isFirstEnable = false;
		}
	}
	private void Update()
	{
		INPUT.UI.SetCursor(isFpsMode: !isAnyMenuOpen);
		isGrounded = !isUsingNoclip && _cc.isGrounded;

		if (!isAnyMenuOpen)
		{
			#region extra: NoClipMovement
			if (INPUT.K.InstantDown(KeyCode.V)) ToggleNoclip();
			if (isUsingNoclip) HandleNoclipMovement(); 
			#endregion
			else
			{
				HandleMovement();
				HandleDucking();
			}
		}
		if (!isUsingNoclip) HandleGravityAndSlope();
		if (transform.position.y <= -200f)
			Teleport(PlayerSpawnPoint.GetRandomSpawnPoint());
	}
	void HandleMovement()
	{
		moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
		bool sprinting = Input.GetKey(KeyCode.LeftShift) && !isDucking && isGrounded;
		selectedWalkSpeed = sprinting ? _sprintSpeed : (isDucking ? _duckSpeed : _walkSpeed);

		Vector3 move = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;
		_cc.Move(move * selectedWalkSpeed * Time.deltaTime);

		if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
		{
			velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
			velocity.x = 0f; velocity.z = 0f;
		}
	}
	void HandleDucking()
	{
		bool wantsDuck = Input.GetKey(KeyCode.LeftControl);
		bool canStand = true;
		if (!wantsDuck && isDucking)
		{
			float headroom = _standingHeight - _duckHeight;
			Vector3 center = transform.position + _cc.center + Vector3.up * (headroom / 2f);
			Vector3 half = new Vector3(_cc.radius * 0.95f, headroom / 2f, _cc.radius * 0.95f);
			canStand = !Physics.CheckBox(center, half, Quaternion.identity, _groundLayer, QueryTriggerInteraction.Ignore);
		}
		isDucking = wantsDuck || (isDucking && !canStand);

		float targetHeight = isDucking ? _duckHeight : _standingHeight;
		_cc.height = Mathf.Lerp(_cc.height, targetHeight, Time.deltaTime * _duckingSpeed);
		float camTarget = _cc.height / 2f - 0.5f;
		float camY = Mathf.SmoothDamp(_playerCam.transform.localPosition.y, camTarget, ref cameraHeightVelocity, 0.1f);
		if (float.IsNaN(camY)) camY = 0f;
		_playerCam.transform.localPosition = new Vector3(_playerCam.transform.localPosition.x, camY, _playerCam.transform.localPosition.z);
		/*
		if (_characterModel != null)
			_characterModel.localScale = new Vector3(1f, _cc.height / _standingHeight, 1f);
		*/
	}
	void HandleGravityAndSlope()
	{
		if (isGrounded)
		{
			Vector3 slideDir = Vector3.zero;
			int slopeCount = 0;
			int sampleCount = 6;
			float radius = _cc.radius * 0.98f;
			float rayLen = _cc.height / 2f + 0.2f;
			for (int i = 0; i < sampleCount; i++)
			{
				float angle = (float)i * Mathf.PI * 2f / sampleCount;
				Vector3 origin = transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
				if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayLen, _groundLayer))
				{
					float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
					if (slopeAngle > _standingSlopeLimit)
					{
						slideDir += new Vector3(hit.normal.x, -hit.normal.y, hit.normal.z);
						slopeCount++;
					}
				}
				else slopeCount++;
			}
			if (slopeCount == sampleCount)
				velocity += slideDir.normalized * _slideSpeed * Time.deltaTime;
			else if (velocity.y < 0f) { velocity.y = -2f; velocity.x = 0f; velocity.z = 0f; }
		}
		velocity.y += _gravity * Time.deltaTime;
		CollisionFlags flags = _cc.Move(velocity * Time.deltaTime);
		if ((flags & CollisionFlags.Above) != 0 && velocity.y > 0f) velocity.y = 0f;
	}
	#region extra: ToggleNoclip(), HandleNoclipMovement(), ToggleMiningLightFromTool
	// nice-to-have: noclip — V key fly mode, no gravity, through walls
	void ToggleNoclip()
	{
		isUsingNoclip = !isUsingNoclip;
		_cc.enabled = !isUsingNoclip;
	}
	void HandleNoclipMovement()
	{
		Vector3 forward = _playerCam.transform.forward;
		Vector3 right = _playerCam.transform.right;
		Vector3 move = forward * moveInput.y + right * moveInput.x;
		if (move.sqrMagnitude > 1f) move.Normalize();
		bool sprinting = Input.GetKey(KeyCode.LeftShift);
		selectedWalkSpeed = sprinting ? _sprintSpeed * 4f : _walkSpeed * 2f;
		float vertical = 0f;
		if (Input.GetKey(KeyCode.Space)) vertical += 1f;
		if (Input.GetKey(KeyCode.C)) vertical -= 1f;
		_cc.transform.position += (move * selectedWalkSpeed + Vector3.up * vertical * selectedWalkSpeed) * Time.deltaTime;
	}
	// nice-to-have: mining hat dual-light — toggles between nightVisionLight and miningHatLight on player
	public void ToggleMiningLightFromTool(bool enable)
	{
		isminingLampEnabled = enable;
		if (_nightVisionLight != null) _nightVisionLight.SetActive(!isminingLampEnabled);
		if (_miningHatLight != null) _miningHatLight.SetActive(isminingLampEnabled);
	}
	#endregion
	#endregion

	#region public API fields
	public Camera GetPlayerCam()
	{
		return this._playerCam;
	}
	public Transform GetViewModelContainer()
	{
		return this._viewModelContainer;
	}
	public Transform GetHoldPos()
	{
		return this._holdPosition;
	}
	public Transform GetMagnetToolPos()
	{
		return this._magnetToolPosition;
	}
	public CharacterController GetCC()
	{
		return this._cc;
	}
	public LayerMask GetInteractionMask()
	{
		return this._interactLayerMask;

	}
	public float GetMoveSpeed()
	{
		return selectedWalkSpeed;
	}
	public float GetWalkSpeed()
	{
		return this._walkSpeed;
	}
	public float GetSprintSpeed()
	{
		return this._sprintSpeed;
	}
	public float GetDuckSpeed()
	{
		return this._duckSpeed;
	}
	public Vector2 GetMoveInput()
	{
		return moveInput;
	}
	public bool GetIsGrounded()
	{
		return isGrounded;
	}
	public bool IsInWater { get; set; }
	#endregion

	#region public API
	/// <summary>
	/// teleport player to pos(by disable-enable character COntroller)
	/// </summary>
	/// <param name="pos"></param>
	public void Teleport(Vector3 pos)
	{
		IsInWater = false;
		bool wasEnabled = _cc.enabled;
		_cc.enabled = false;
		transform.position = pos;
		_cc.enabled = wasEnabled;
	}
	/// <summary> 
	/// teleport player to position + rotation 
	/// </summary>
	public void TeleportWithRotation(Vector3 position, Vector3 rotation)
	{
		Teleport(position);
		transform.rotation = Quaternion.Euler(rotation);
	}
	#endregion
}
