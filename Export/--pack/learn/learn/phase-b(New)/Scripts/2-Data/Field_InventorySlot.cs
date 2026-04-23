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
/// I display one inventory slot in the UI — icon sprite, name text, amount text, background
/// color for selected/hovered state, orange hotbar indicator, and drag visibility toggle.
/// I have zero logic — no onClick, no drag interfaces, no business logic. InventoryOrchestrator
/// calls my SetData/SetEmpty/SetHighlighted methods to update what I show. UIEventRelay
/// (added to my GO at runtime by Orchestrator) handles all drag-drop input separately.
///
/// Who uses me: InventoryOrchestrator (SetData, SetEmpty, SetHighlighted, SetHovered, SetDragVisible).
/// Events I fire: none. Events I subscribe to: none.
/// </summary>
public class Field_InventorySlot : MonoBehaviour
{
	#region Inspector Fields
	[SerializeField] Image _icon;
	[SerializeField] Image _background;
	[SerializeField] TMP_Text _nameText;
	[SerializeField] TMP_Text _amountText;
	[SerializeField] GameObject _orangeBarThing;
	[SerializeField] GameObject _hideWhenDragged;

	[SerializeField] Color _selectedColor = new Color(0.4f, 0.8f, 0.6f, 0.2f);
	[SerializeField] Color _notSelectedColor = new Color(0.2f, 0.2f, 0.2f, 0.1f);
	[SerializeField] Color _hoveredColor = new Color(1f, 1f, 1f, 0.15f);
	#endregion

	#region public API
	/// <summary> set slot display data </summary>
	public void SetData(Sprite sprite, string name, int amount)
	{
		if (sprite != null)
		{
			_icon.enabled = true;
			_icon.sprite = sprite;
			_nameText.text = "";
		}
		else
		{
			_icon.enabled = false;
			_nameText.text = name;
		}
		_amountText.text = amount > 1 ? amount.ToString() : "";
	}
	/// <summary> clear slot display </summary>
	public void SetEmpty()
	{
		_icon.enabled = false;
		_nameText.text = "";
		_amountText.text = "";
		SetHighlighted(false);
	}
	/// <summary> highlight active slot </summary>
	public void SetHighlighted(bool selected)
	{
		_background.color = selected ? _selectedColor : _notSelectedColor;
	}
	/// <summary> show hovered state </summary>
	public void SetHovered(bool hovered)
	{
		if (hovered) _background.color = _hoveredColor;
	}
	/// <summary> show/hide hotbar indicator </summary>
	public void SetIsHotbar(bool isHotbar)
	{
		_orangeBarThing.SetActive(isHotbar);
	}
	/// <summary> show/hide drag ghost state </summary>
	public void SetDragVisible(bool visible)
	{
		_hideWhenDragged.SetActive(visible);
	}
	#endregion
}