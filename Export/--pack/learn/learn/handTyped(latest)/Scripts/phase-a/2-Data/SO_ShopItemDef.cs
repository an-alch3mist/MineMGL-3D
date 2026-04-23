using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

using SPACE_UTIL;

[CreateAssetMenu(menuName = "SO/SO_ShopItemDef", fileName = "SO_ShopItemDef")]
public class SO_ShopItemDef : ScriptableObject
{
	public string itemDefName = "itemDefName";
	[TextArea(2, 3)]
	public string descr;
	public float defaultPrice;
	public bool isDefaultLocked = false;

	public Sprite sprite;
	public GameObject pfObject;
	public int maxStackableCount = 10;
}