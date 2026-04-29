using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

using SPACE_UTIL;

public class Field_ShopItem : MonoBehaviour
{
	public TextMeshProUGUI _nameText, _descrText, _priceText, _buttonText;
	public Image _icon, _buttonBg;
	public Button _addToCartButton;

	public void SetData(string name, string descr, string price, string buttonText, Sprite sprite)
	{
		this._nameText.text = name;
		this._descrText.text = descr;
		this._priceText.text = price;
		this._buttonText.text = buttonText;
		this._icon.sprite = sprite;
	}
	public void SetButtonInteractable(bool isInteractable, string buttonText, Color normalCol, Color nonInteractableCol)
	{
		this._buttonBg.color = (isInteractable) ? normalCol : nonInteractableCol;
		this._buttonText.color = (isInteractable) ? normalCol * 0.5f : nonInteractableCol * 0.5f;
		this._addToCartButton.interactable = isInteractable;
	}
}
