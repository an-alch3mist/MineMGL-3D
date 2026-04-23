using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

using SPACE_UTIL;

[CreateAssetMenu(menuName = "SO/SO_ShopCategory", fileName = "SO_ShopCategory")]
public class SO_ShopCategory : ScriptableObject
{
	public string categoryName = "categoryName";
	public Sprite sprite;
	public bool hideIfAllItemsLocked;
	public List<SO_ShopItemDef> ITEM_DEF;
}