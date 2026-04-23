using System.Collections;
using UnityEngine;

/// <summary>
/// "I lower the player into the mine on scene start"
/// Code-driven descent: Perlin noise shake, roof collider, landing particle.
/// 100% source behavior from StartingElevator.cs
/// </summary>
[DefaultExecutionOrder(1000)]
public class StartingElevator : MonoBehaviour
{
	#region Inspector Fields
	[SerializeField] float _startingHeight = 15f;
	[SerializeField] float _endHeight = 0f;
	[SerializeField] Transform _playerTeleportPos;
	[SerializeField] GameObject _roofCollider;
	[SerializeField] GameObject _landingParticle;
	#endregion

	#region private API
	bool isLowering;
	bool hasPlayedLandingParticle;

	void LowerTheElevator()
	{
		_landingParticle.SetActive(false);
		hasPlayedLandingParticle = false;
		_roofCollider.SetActive(true);
		transform.localPosition = new Vector3(0f, _startingHeight, 0f);
		isLowering = true;
	}

	void TeleportPlayer()
	{
		// Phase A uses SimplePlayerController — disable CharacterController, set position, re-enable
		var player = FindObjectOfType<SimplePlayerController>();
		if (player == null) return;
		var cc = player.GetComponent<CharacterController>();
		if (cc != null) cc.enabled = false;
		player.transform.position = _playerTeleportPos.position;
		if (cc != null) cc.enabled = true;
	}
	#endregion

	#region Unity Life Cycle
	private void Start()
	{
		// purpose: pause/unpause elevator sound (Phase H will wire actual sound)
		GameEvents.OnGamePaused += HandleGamePaused;
		GameEvents.OnGameUnpaused += HandleGameUnpaused;
	}

	private void OnEnable()
	{
		_landingParticle.SetActive(false);
		// always lower on enable for now — Phase I adds SceneWasLoadedFromNewGame check
		LowerTheElevator();
		TeleportPlayer();
	}

	private void OnDestroy()
	{
		GameEvents.OnGamePaused -= HandleGamePaused;
		GameEvents.OnGameUnpaused -= HandleGameUnpaused;
	}

	private void Update()
	{
		if (!isLowering) return;

		Vector3 pos = transform.localPosition;

		float distRemaining = Mathf.Max(0f, pos.y - _endHeight);
		float progress = Mathf.InverseLerp(0.15f, 0f, distRemaining);

		float speed = Mathf.Lerp(1.25f, 0.1f, Mathf.Clamp01(progress));
		float shake = Mathf.Lerp(0.02f, 0f, Mathf.Clamp01(progress));

		pos.y -= speed * Time.deltaTime;
		pos.x = Mathf.PerlinNoise(Time.time * 20f, 0f) * shake - shake / 2f;
		pos.z = Mathf.PerlinNoise(0f, Time.time * 20f) * shake - shake / 2f;

		if (!hasPlayedLandingParticle && pos.y <= _endHeight + 1f)
		{
			hasPlayedLandingParticle = true;
			_landingParticle.SetActive(true);
		}

		if (pos.y <= _endHeight + 0.001f)
		{
			pos.y = _endHeight;
			pos.x = 0f;
			pos.z = 0f;
			_roofCollider.SetActive(false);
			isLowering = false;
			// purpose: other systems react to landing (tutorial, music, UI — future phases)
			GameEvents.RaiseElevatorLanded();
		}

		transform.localPosition = pos;
	}
	#endregion

	#region Event Handlers
	// purpose: pause/unpause elevator sound when game pauses (Phase H wires actual sound)
	void HandleGamePaused() { /* Sound pause — Phase H */ }
	void HandleGameUnpaused() { /* Sound unpause — Phase H */ }
	#endregion
}