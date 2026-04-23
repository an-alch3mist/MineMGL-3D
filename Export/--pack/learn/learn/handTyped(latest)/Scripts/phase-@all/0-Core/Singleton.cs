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
/// generic singleton base first instance wins, duplicates are destroyed
/// </summary>
public abstract class Singleton<T> : MonoBehaviour 
										where T: MonoBehaviour
{
	#region public API
	public static T Ins { get; private set; }
	#endregion

	#region Unity Life Cycle
	protected virtual void Awake()
	{
		if (Singleton<T>.Ins == null)
			Singleton<T>.Ins = this as T;
		else
		{
			Debug.Log($"{typeof(T)} instance already exist, so this gameObject is destroyed".colorTag("red"));
			GameObject.Destroy(this.gameObject);
		}
	}
	#endregion
}