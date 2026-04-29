using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

using SPACE_UTIL;

public class Field_InteractionOption : MonoBehaviour
{
	public TextMeshProUGUI _text;
	public Image _buttonIcon;
	public Button _button;

	public void SetData(string str, Sprite sprite)
	{
		this._text.text = str;
		this._buttonIcon.sprite = sprite;
	}
}
