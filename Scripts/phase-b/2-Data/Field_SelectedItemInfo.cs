using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

using SPACE_UTIL;

public class Field_SelectedItemInfo : MonoBehaviour
{
	#region inspector fields
	public Image _selectedItemIcon;
	public TMP_Text _selectedItemNameText, selectedItemCountText, _selectedItemDescrText;
	public TMP_Text _equipButtonText;
	public Button _equipButton;
	public Button _dropButton;
	#endregion
	#region public API
	public void SetData(Sprite sprite, string name, string descr, string count, string equipText)
	{
		this._selectedItemIcon.enabled = true;
		this._selectedItemIcon.sprite = sprite;
		this._selectedItemNameText.text = $"{name}";
		this._selectedItemDescrText.text = $"{descr}";
		this.selectedItemCountText.text = $"{count}";
		this._equipButtonText.text = $"{equipText}";
		// this._countText.text = (count <= 1) ? $"" : count.ToString();
	}
	#endregion
}
