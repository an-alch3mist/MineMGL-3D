using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Relays Unity EventSystem events to Action callbacks. Added via code (AddComponent) by
/// InventoryOrchestrator — NOT added via inspector, so filename != classname is fine here.
/// But we name it UIEventRelay.cs anyway for consistency.
/// </summary>
public class UIEventRelay : MonoBehaviour,
	IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler,
	IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
	public int Index;
	public Action<UIEventRelay, PointerEventData> onBeginDrag;
	public Action<PointerEventData> onDrag;
	public Action<UIEventRelay, PointerEventData> onEndDrag;
	public Action<UIEventRelay, PointerEventData> onDrop;
	public Action<UIEventRelay> onPointerEnter;
	public Action<UIEventRelay> onPointerExit;
	public Action<UIEventRelay> onPointerDown;

	public void OnBeginDrag(PointerEventData e) => onBeginDrag?.Invoke(this, e);
	public void OnDrag(PointerEventData e) => onDrag?.Invoke(e);
	public void OnEndDrag(PointerEventData e) => onEndDrag?.Invoke(this, e);
	public void OnDrop(PointerEventData e) => onDrop?.Invoke(this, e);
	public void OnPointerEnter(PointerEventData e) => onPointerEnter?.Invoke(this);
	public void OnPointerExit(PointerEventData e) => onPointerExit?.Invoke(this);
	public void OnPointerDown(PointerEventData e) => onPointerDown?.Invoke(this);
}