using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

using SPACE_UTIL;

public class Field_Inventory : MonoBehaviour
{
	#region inspector fields
	[SerializeField] Image _icon, _bg;
	[SerializeField] TMP_Text _nameText, _countText;
	[SerializeField] GameObject _orangeBarThing, _hideWhenDragged;

	[SerializeField]
	Color _selectedColor = new Color(0.4f, 0.8f, 0.6f, 0.2f),
		  _notSelectedColor = new Color(0.2f, 0.2f, 0.2f, 0.1f),
		  _hoveredColor = new Color(1f, 1f, 1f, 0.2f);
	#endregion
	#region public API
	public void SetData(Sprite sprite, string name, int count)
	{
		this._icon.enabled = true;
		this._icon.sprite = sprite;
		this._nameText.text = $"";
		this._countText.text = (count <= 1) ? $"" : count.ToString();
	}
	public void SetHighLighted(bool isSelected)
	{
		this._bg.color = (isSelected) ? this._selectedColor : this._notSelectedColor;
	}
	public void SetEmpty()
	{
		this._icon.enabled = false;
		this._nameText.text = $"";
		this._countText.text = $"";
		SetHighLighted(false);
	}
	public void SetHovered(bool isHovered)
	{
		if (isHovered)
			this._bg.color = this._hoveredColor;
	}
	public void SetIsHotBar(bool isHotBar)
	{
		this._orangeBarThing.SetActive(isHotBar);
	}
	public void SetDragVisible(bool isVisible)
	{
		this._hideWhenDragged.SetActive(isVisible);
	}
	#endregion
}
