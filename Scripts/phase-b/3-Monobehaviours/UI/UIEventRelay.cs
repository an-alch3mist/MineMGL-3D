using System;

using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// I relay Unity EventSystem events (drag, drop, pointer enter/exit/down) to Action callbacks.
/// InventoryOrchestrator adds me to each Field_InventorySlot GO at runtime via AddComponent,
/// then wires my Action fields (onBeginDrag, onDrag, onEndDrag, onDrop, onPointerDown, etc.)
/// to its handler methods. This keeps Field_ pure display — zero interfaces on it. I'm reusable
/// across any UI system that needs drag-drop or pointer events (quest tree, research tree, etc.).
///
/// Who uses me: InventoryOrchestrator (wires all Actions in BuildSlotFields).
/// </summary>
public class UIEventRelay : MonoBehaviour,
	IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler,
	IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
	#region public API
	public int Index;
	public Action<UIEventRelay, PointerEventData> onBeginDrag;
	public Action<PointerEventData> onDrag;
	public Action<UIEventRelay, PointerEventData> onEndDrag;
	public Action<UIEventRelay, PointerEventData> onDrop;
	public Action<UIEventRelay> onPointerEnter;
	public Action<UIEventRelay> onPointerExit;
	public Action<UIEventRelay> onPointerDown;
	#endregion

	#region relay interface triggers
	public void OnPointerEnter(PointerEventData e) => onPointerEnter?.Invoke(this);
	public void OnPointerExit(PointerEventData e) => onPointerExit?.Invoke(this);
	public void OnPointerDown(PointerEventData e) => onPointerDown?.Invoke(this);

	public void OnBeginDrag(PointerEventData e) => onBeginDrag?.Invoke(this, e);
	public void OnDrag(PointerEventData e) => onDrag?.Invoke(e);
	public void OnEndDrag(PointerEventData e) => onEndDrag?.Invoke(this, e);
	public void OnDrop(PointerEventData e) => onDrop?.Invoke(this, e);
	#endregion
}
