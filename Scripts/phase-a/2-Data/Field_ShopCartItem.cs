using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

using SPACE_UTIL;

public class Field_ShopCartItem : MonoBehaviour
{
	public TextMeshProUGUI _nameText, _descrText, _priceText;
	public Image _icon;
	public TMP_InputField _qtyInputField;
	public Button _addButton, _subButton, _removeButton;

	public void SetData(string name, string descr, Sprite sprite)
	{
		this._nameText.text = name;
		this._descrText.text = descr;
		this._icon.sprite = sprite;
	}
	public void SetPrice(float price)
	{
		this._priceText.text = price.formatMoneyShort();
	}
}
