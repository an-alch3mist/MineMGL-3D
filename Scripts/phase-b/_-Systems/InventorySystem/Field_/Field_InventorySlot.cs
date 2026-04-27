using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ═══════════════════════════════════════════════════════════════
// Field_InventorySlot — pure display for one inventory slot
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Pure display — shows icon, name, count for one inventory slot. InventoryOrchestrator
/// instantiates these from a prefab and calls SetData/SetEmpty/SetHighlighted/SetDragVisible.
/// Zero logic — just sets Image/TMP_Text properties. Public fields because Field_ convention.
///
/// Prefab hierarchy:
///   pfInventorySlot (Image — slot background, raycastTarget=true)
///   ├── hideWhenDragged (empty parent — SetActive false during drag)
///   │   ├── icon (Image — tool sprite, raycastTarget=false)
///   │   ├── nameText (TMP_Text — raycastTarget=false)
///   │   └── countTxtBg (Panel)
///   │       └── countText (TMP_Text — shows "x3" for stacked, raycastTarget=false)
///   └── orangeBar (Image — hotbar indicator)
/// </summary>
[AddComponentMenu("MineMGL/Inventory/Field_InventorySlot")]
public class Field_InventorySlot : MonoBehaviour
{
	#region inspector fields
	public Image _icon, _bg;
	public TMP_Text _nameText, _countText;
	public GameObject _orangeBarThing, _hideWhenDragged;

	[SerializeField]
	Color _selectedColor = new Color(0.4f, 0.8f, 0.6f, 0.2f),
		  _equippedColor = new Color(0.2f, 0.5f, 0.9f, 0.4f),
		  _notSelectedColor = new Color(0.2f, 0.2f, 0.2f, 0.1f),
		  _hoveredColor = new Color(1f, 1f, 1f, 0.2f);
	#endregion

	#region public API
	/// <summary> show item data — icon, name, count (count shows "x3" for qty > 1, empty for 1) </summary>
	public void SetData(Sprite sprite, string name, int count)
	{
		_icon.enabled = true;
		_icon.sprite = sprite;
		_nameText.text = name;
		_countText.text = (count > 1) ? $"x{count}" : "";
	}
	/// <summary> clear slot — no icon, no text </summary>
	public void SetEmpty()
	{
		_icon.enabled = false;
		_nameText.text = "";
		_countText.text = "";
		SetHighlighted(false, false);
	}
	/// <summary> set slot background color based on selected/equipped state </summary>
	public void SetHighlighted(bool isSelected, bool isEquipped = false)
	{
		_bg.color = isEquipped ? _equippedColor
			: isSelected ? _selectedColor
			: _notSelectedColor;
	}
	/// <summary> hover highlight — temporary color while pointer is over slot </summary>
	public void SetHovered(bool isHovered)
	{
		if (isHovered) _bg.color = _hoveredColor;
	}
	/// <summary> show/hide hotbar indicator bar </summary>
	public void SetIsHotbar(bool isHotBar)
	{
		_orangeBarThing.SetActive(isHotBar);
	}
	/// <summary> hide icon/name/count during drag (slot looks empty while dragging from it) </summary>
	public void SetDragVisible(bool isVisible)
	{
		_hideWhenDragged.SetActive(isVisible);
	}
	#endregion
}
