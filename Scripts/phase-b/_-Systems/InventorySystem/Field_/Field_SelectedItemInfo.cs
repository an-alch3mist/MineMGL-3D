using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pure display — shows selected item's icon, name, description, count, and equip/drop buttons.
/// InventoryOrchestrator calls SetData() when a slot is clicked. Buttons wired in Init().
/// </summary>
[AddComponentMenu("MineMGL/Inventory/Field_SelectedItemInfo")]
public class Field_SelectedItemInfo : MonoBehaviour
{
	public Image _selectedItemIcon;
	public TMP_Text _selectedItemNameText, _selectedItemCountText, _selectedItemDescrText;
	public TMP_Text _equipButtonText;
	public Button _equipButton;
	public Button _dropButton;

	public void SetData(Sprite sprite, string name, string descr, string count, string equipText)
	{
		_selectedItemIcon.enabled = sprite != null;
		_selectedItemIcon.sprite = sprite;
		_selectedItemNameText.text = name;
		_selectedItemDescrText.text = descr;
		_selectedItemCountText.text = count;
		_equipButtonText.text = equipText;
	}
}