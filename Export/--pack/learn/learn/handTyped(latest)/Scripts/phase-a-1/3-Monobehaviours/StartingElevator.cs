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
/// lower the elevator + playParticleEffect
/// </summary>
public class StartingElevator : MonoBehaviour
{
	#region inspector fields
	[SerializeField] float _startingHeight = 12f, _endingHeight = 0f;
	[SerializeField] Transform _playerTeleport, _elevatorPlatform;
	[SerializeField] CharacterController _cc;
	[SerializeField] GameObject _landingParticlesObj; 
	#endregion

	#region private API
	void TeleportPlayer()
	{
		this._cc.enabled = false;
		this._cc.transform.position = this._playerTeleport.position;
		this._cc.transform.rotation = this._playerTeleport.rotation;
		this._cc.enabled = true;
	}
	bool isLowering = false;
	Vector3 xzStartPos;
	void LowerElevator()
	{
		this._elevatorPlatform.position = this._elevatorPlatform.position.xz() + Vector3.up * this._startingHeight;
		isLowering = true;
	} 
	#endregion

	#region Unity Life Cycle
	bool isFirstEnable = true;
	private void OnEnable()
	{
		if (isFirstEnable)
		{
			//
			// do somthng
			xzStartPos = this._elevatorPlatform.position.xz();
			isFirstEnable = false;
		}
		TeleportPlayer();
		LowerElevator();
		this._landingParticlesObj.SetActive(false);
		GameEvents.OnGamePaused += HandleGamePaused;
		GameEvents.OnGameUnPaused += HandleGameUnPaused;
	}
	private void Update()
	{
		if (isLowering == false)
			return;
		Vector3 pos = Vector3.up * this._elevatorPlatform.position.y + xzStartPos;
		float t = (pos.y - this._endingHeight) / (this._startingHeight - this._endingHeight);
		// Debug.Log(t.ToString().colorTag("cyan"));

		float speed = Z.lerp(0.7f, 1.25f, t);
		pos.y -= speed * Time.deltaTime;
		#region shake
		float shake = Z.lerp(0.02f, 0.04f, t);
		pos.x = xzStartPos.x + (Mathf.PerlinNoise(Time.time * 20f, 0f) - 0.5f) * shake;
		pos.z = xzStartPos.z + (Mathf.PerlinNoise(0f, Time.time * 20f) - 0.5f) * shake; 
		#endregion

		this._elevatorPlatform.position = pos;
		if(pos.y <= this._endingHeight)
		{
			isLowering = false;
			this._landingParticlesObj.SetActive(true);
			// purpose: other system react to this landing (sound, tutorial, effects etc....)
			GameEvents.RaiseElevatorLanded();
			GameEvents.RaiseCamViewPunch(new Vector3(3.5f, 0.7f, 0.5f), duration: 0.4f);
		}
	}
	private void OnDisable()
	{
		GameEvents.OnGamePaused -= HandleGamePaused;
		GameEvents.OnGameUnPaused -= HandleGameUnPaused;
	}
	void HandleGamePaused() { /* play a sound in later phases */ }
	void HandleGameUnPaused() { /* play a sound in later phases */ } 
	#endregion
}