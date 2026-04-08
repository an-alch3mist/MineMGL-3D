using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

using SPACE_UTIL;

public class SimplePlayerController : Singleton<SimplePlayerController>
{
	#region inspector fields
	[Header("move")]
	[SerializeField] float _walkSpeed = 4f;
	[SerializeField] CharacterController _cc;

	[Header("look")]
	[SerializeField] float _mouseSensitivity = 2f;
	[SerializeField] Camera _playerCam;
	#endregion

	#region private API
	bool isAnyMenuOpen;
	float xRot;
	float yRot;
	Vector3 vel;
	#endregion

	#region Unity Life Cycle
	private void Start()
	{
		Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;

		GameEvents.OnMenuStateChanged += (isAnyMenuOpen) => this.isAnyMenuOpen = isAnyMenuOpen;
	}
	private void Update()
	{
		HandleCursorLock();
		if(isAnyMenuOpen == false)
		{
			HandleLook();
			HandleMovement();
		}
	}
	void HandleCursorLock()
	{
		/*
		Cursor.lockState = (isAnyMenuOpen == true) ? CursorLockMode.None : CursorLockMode.Locked; // no lock when any menu is open
		Cursor.visible = isAnyMenuOpen; // always visible when menu is open
		*/
		INPUT.UI.SetCursor(isFpsMode: !isAnyMenuOpen);
	}
	void HandleLook()
	{
		float mx = Input.GetAxis("Mouse X") * this._mouseSensitivity;
		float my = Input.GetAxis("Mouse Y") * this._mouseSensitivity;

		xRot = (xRot - my).clamp(-80f, 80f);
		yRot += mx;
		this._playerCam.transform.localRotation = Quaternion.Euler(Vector3.right * xRot);
		transform.rotation = Quaternion.Euler(Vector3.up * yRot);
	}
	void HandleMovement()
	{
		if (this._cc.isGrounded && vel.y < 0f)
			vel.y = -2f;

		float dt = Time.deltaTime;
		// keyboard movement
		Vector3 move = (transform.right * Input.GetAxisRaw("Horizontal")) + (transform.forward * Input.GetAxisRaw("Vertical"));
		this._cc.Move(move.normalized * this._walkSpeed * dt);

		// gravity movement
		vel.y += (-10f) * dt;
		this._cc.Move(vel * dt);
	}
	#endregion
}
