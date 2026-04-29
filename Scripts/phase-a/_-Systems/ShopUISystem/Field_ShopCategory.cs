using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

using SPACE_UTIL;

public class Field_ShopCategory : MonoBehaviour
{
	public Button button;
	public TextMeshProUGUI _nameText;
	public Image _img;

	public void SetNameText(string str) => this._nameText.text = str;
	public void SetSelected(bool isSelected, Color normalCol, Color selectedCol)
	{
		this._img.color = (isSelected) ? selectedCol : normalCol;
	}
}
