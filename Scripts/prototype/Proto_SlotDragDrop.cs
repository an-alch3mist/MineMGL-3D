using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// STANDALONE prototype — drop on an empty scene with Canvas + EventSystem and press Play.
/// Tests: UI slot drag-drop, swap, add slot via button, select slot, ghost icon follows cursor.
/// Zero external dependencies — no GameEvents, no DataService, no InventoryOrchestrator.
/// Everything is created at runtime from code.
///
/// SETUP (takes 30 seconds):
///   1. Create new empty scene
///   2. GameObject → UI → Canvas (auto-creates EventSystem)
///   3. Create empty GO → name "Proto_SlotDragDrop" → add this script
///   4. Wire: _canvas → the Canvas GO
///   5. Press Play
///
/// CONTROLS:
///   A       → add tool to first empty slot + auto-equip (simulates pickup)
///   1-5     → select slot — auto-equips if has item, empty hands if empty
///   Same key twice → toggles equip/unequip (empty hands)
///   Scroll  → cycles through occupied slots (skips empty)
///   G       → drop equipped item (slot empties, "Empty hands")
///   Click   → select slot + show info panel
///   Equip btn → same as selecting (auto-equips)
///   Drop btn  → clears slot, "Empty hands"
///   Drag    → drag slot icon to another slot → swap on drop
///
/// WHAT TO VERIFY:
///   - A adds colored tool to first empty slot + auto-equips (blue highlight + status text)
///   - Press 1 → equip. Press 1 again → unequip (empty hands). Press 1 again → re-equip.
///   - Press empty slot → "Empty hands"
///   - Scroll wheel → skips empty, lands on next occupied
///   - G drops equipped, status shows "Empty hands"
///   - Drag A → B → swap, equipped tracking follows
///   - Drag outside → item dropped
///   - Info panel shows Equip + Drop buttons
///   - Status text (top center): "Equipped: X" or "Empty hands"
///   - Console logs every action
/// </summary>
public class Proto_SlotDragDrop : MonoBehaviour
{
	[SerializeField] Canvas _canvas;

	// runtime data
	struct SlotData
	{
		public bool hasItem;
		public Color color;
		public string name;
	}
	List<SlotData> slots = new List<SlotData>();
	List<GameObject> slotGOs = new List<GameObject>();
	GameObject ghostGO;
	Image ghostImage;
	GameObject infoPanelGO;
	TMP_Text infoNameText;
	TMP_Text statusText;
	int selectedIndex = 0;
	int equippedIndex = -1;
	int dragFromIndex = -1;
	int slotCount = 5;
	int toolCounter = 0;
	string[] toolNames = { "Pickaxe", "Magnet", "Hammer", "Hat", "Wrench", "Scanner", "Builder" };

	void Start()
	{
		// → create slot container with HorizontalLayoutGroup
		var container = CreateSlotContainer();
		// → create slots
		for (int i = 0; i < slotCount; i++)
		{
			slots.Add(new SlotData { hasItem = false, color = Color.clear, name = "" });
			slotGOs.Add(CreateSlotGO(container, i));
		}
		// → create ghost (starts hidden)
		ghostGO = CreateGhost();
		// → create Add button
		CreateAddButton();
		// → create info panel with Equip + Drop buttons (starts hidden)
		infoPanelGO = CreateInfoPanel();
		// → create status text (top center)
		statusText = CreateStatusText();
		// → select first slot
		SelectSlot(0);
		UpdateStatus();
		Debug.Log("[Proto] Ready. A=add, 1-5=select/equip, scroll=cycle, G=drop, drag=swap.");
	}

	void Update()
	{
		// A → add tool to first empty slot
		if (Input.GetKeyDown(KeyCode.A)) AddTool();
		// 1-5 → select slot
		if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
		if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
		if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
		if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(3);
		if (Input.GetKeyDown(KeyCode.Alpha5)) SelectSlot(4);
		// scroll wheel: cycle through occupied slots (skip empty)
		float scroll = Input.GetAxis("Mouse ScrollWheel");
		if (scroll != 0f) ScrollToNextOccupied(scroll > 0f ? 1 : -1);
		// G = drop equipped
		if (Input.GetKeyDown(KeyCode.G)) DropEquipped();
	}

	// ─── SLOT ACTIONS ───────────────────────────────────────────

	void AddTool()
	{
		for (int i = 0; i < slots.Count; i++)
		{
			if (!slots[i].hasItem)
			{
				string name = toolNames[toolCounter % toolNames.Length];
				Color color = Color.HSVToRGB((toolCounter * 0.15f) % 1f, 0.7f, 0.9f);
				slots[i] = new SlotData { hasItem = true, color = color, name = name };
				toolCounter++;
				// → auto-equip on add (like EquipWhenPickedUp)
				SelectSlot(i);
				Debug.Log($"[Proto] Added '{name}' to slot {i} (auto-equipped)");
				return;
			}
		}
		Debug.Log("[Proto] All slots full!");
	}

	void SelectSlot(int index)
	{
		if (index < 0 || index >= slots.Count) return;
		// → toggle: pressing same slot again unequips (empty hands)
		if (index == equippedIndex)
		{
			equippedIndex = -1;
			selectedIndex = index;
			RefreshAllSlots();
			UpdateStatus();
			Debug.Log($"[Proto] UNEQUIPPED (empty hands)");
			return;
		}
		selectedIndex = index;
		// → auto-equip if slot has item
		if (slots[index].hasItem)
		{
			equippedIndex = index;
			Debug.Log($"[Proto] EQUIPPED: {slots[index].name} (slot {index})");
		}
		else
		{
			equippedIndex = -1;
			Debug.Log($"[Proto] Selected empty slot {index} (empty hands)");
		}
		RefreshAllSlots();
		UpdateStatus();
		ShowInfoPanel(index);
	}

	void ScrollToNextOccupied(int dir)
	{
		int start = selectedIndex;
		for (int i = 0; i < slots.Count; i++)
		{
			int next = (start + dir + slots.Count) % slots.Count;
			start = next;
			if (slots[next].hasItem) { SelectSlot(next); return; }
		}
	}

	void DropEquipped()
	{
		if (equippedIndex < 0 || !slots[equippedIndex].hasItem) return;
		ClearSlot(equippedIndex);
		equippedIndex = -1;
		UpdateStatus();
	}

	void UpdateStatus()
	{
		if (statusText == null) return;
		if (equippedIndex >= 0 && slots[equippedIndex].hasItem)
			statusText.text = $"Equipped: {slots[equippedIndex].name}";
		else
			statusText.text = "Empty hands";
	}

	void SwapSlots(int a, int b)
	{
		if (a < 0 || b < 0 || a >= slots.Count || b >= slots.Count || a == b) return;
		var temp = slots[a];
		slots[a] = slots[b];
		slots[b] = temp;
		// → equipped tracking follows the swap
		if (equippedIndex == a) equippedIndex = b;
		else if (equippedIndex == b) equippedIndex = a;
		RefreshAllSlots();
		UpdateStatus();
		Debug.Log($"[Proto] Swapped slot {a} ↔ {b}");
	}

	void ClearSlot(int index)
	{
		if (index < 0 || index >= slots.Count) return;
		string name = slots[index].name;
		slots[index] = new SlotData { hasItem = false, color = Color.clear, name = "" };
		RefreshAllSlots();
		Debug.Log($"[Proto] Dropped '{name}' from slot {index}");
	}

	void RefreshAllSlots()
	{
		for (int i = 0; i < slotGOs.Count; i++)
		{
			var bg = slotGOs[i].GetComponent<Image>();
			var icon = slotGOs[i].transform.Find("Icon").GetComponent<Image>();
			var label = slotGOs[i].transform.Find("Label").GetComponent<TMP_Text>();

			// → background: blue if equipped, green if selected, dark grey otherwise
			bg.color = (i == equippedIndex) ? new Color(0.2f, 0.5f, 0.9f, 0.4f)
				: (i == selectedIndex) ? new Color(0.3f, 0.8f, 0.4f, 0.3f)
				: new Color(0.2f, 0.2f, 0.2f, 0.5f);

			if (slots[i].hasItem)
			{
				icon.enabled = true;
				icon.color = slots[i].color;
				label.text = slots[i].name;
			}
			else
			{
				icon.enabled = false;
				label.text = "";
			}
		}
	}

	// ─── DRAG-DROP HANDLERS ─────────────────────────────────────

	void OnSlotBeginDrag(int index, PointerEventData e)
	{
		if (!slots[index].hasItem) return;
		dragFromIndex = index;
		// → hide source slot content
		slotGOs[index].transform.Find("Icon").gameObject.SetActive(false);
		slotGOs[index].transform.Find("Label").gameObject.SetActive(false);
		// → show ghost
		ghostGO.SetActive(true);
		ghostImage.color = slots[index].color;
		ghostGO.transform.SetAsLastSibling();
		Debug.Log($"[Proto] Begin drag from slot {index} ({slots[index].name})");
	}

	void OnSlotDrag(PointerEventData e)
	{
		if (dragFromIndex < 0) return;
		ghostGO.transform.position = e.position;
	}

	void OnSlotEndDrag(int index, PointerEventData e)
	{
		// → restore source slot visuals
		slotGOs[index].transform.Find("Icon").gameObject.SetActive(true);
		slotGOs[index].transform.Find("Label").gameObject.SetActive(true);
		ghostGO.SetActive(false);

		// → if dropped outside any slot (no valid drop target received OnDrop)
		if (dragFromIndex >= 0 && e.pointerEnter == null)
			ClearSlot(dragFromIndex);

		dragFromIndex = -1;
		RefreshAllSlots();
	}

	void OnSlotDrop(int targetIndex, PointerEventData e)
	{
		if (dragFromIndex < 0 || dragFromIndex == targetIndex) return;
		SwapSlots(dragFromIndex, targetIndex);
	}

	void OnSlotClick(int index)
	{
		SelectSlot(index);
		// → show info panel if slot has item, hide if empty
		ShowInfoPanel(index);
	}

	void ShowInfoPanel(int index)
	{
		var s = slots[index];
		if (!s.hasItem) { infoPanelGO.SetActive(false); return; }
		infoPanelGO.SetActive(true);
		infoNameText.text = s.name;
		Debug.Log($"[Proto] Info panel: {s.name} — Equip or Drop?");
	}

	void EquipSelected()
	{
		// → "equip" just means select it (highlight green)
		if (!slots[selectedIndex].hasItem) return;
		Debug.Log($"[Proto] EQUIPPED: {slots[selectedIndex].name} (slot {selectedIndex} highlighted)");
		infoPanelGO.SetActive(false);
	}

	void DropSelected()
	{
		if (!slots[selectedIndex].hasItem) return;
		string name = slots[selectedIndex].name;
		ClearSlot(selectedIndex);
		infoPanelGO.SetActive(false);
		Debug.Log($"[Proto] DROPPED: {name} from slot {selectedIndex}");
	}

	// ─── UI CREATION (all runtime) ──────────────────────────────

	Transform CreateSlotContainer()
	{
		var go = new GameObject("SlotContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(Image));
		go.transform.SetParent(_canvas.transform, false);
		var rt = go.GetComponent<RectTransform>();
		rt.anchorMin = new Vector2(0.5f, 0f);
		rt.anchorMax = new Vector2(0.5f, 0f);
		rt.pivot = new Vector2(0.5f, 0f);
		rt.anchoredPosition = new Vector2(0, 20);
		rt.sizeDelta = new Vector2(slotCount * 80 + 20, 90);
		var hlg = go.GetComponent<HorizontalLayoutGroup>();
		hlg.spacing = 5;
		hlg.padding = new RectOffset(5, 5, 5, 5);
		hlg.childAlignment = TextAnchor.MiddleCenter;
		go.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
		return go.transform;
	}

	GameObject CreateSlotGO(Transform parent, int index)
	{
		// → slot background
		var slotGO = new GameObject($"Slot_{index}", typeof(RectTransform), typeof(Image));
		slotGO.transform.SetParent(parent, false);
		var rt = slotGO.GetComponent<RectTransform>();
		rt.sizeDelta = new Vector2(70, 70);
		slotGO.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
		// → IMPORTANT: raycastTarget = true on the slot background (receives drop events)
		slotGO.GetComponent<Image>().raycastTarget = true;

		// → icon (colored square representing a tool)
		var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
		iconGO.transform.SetParent(slotGO.transform, false);
		var iconRT = iconGO.GetComponent<RectTransform>();
		iconRT.anchorMin = Vector2.zero; iconRT.anchorMax = Vector2.one;
		iconRT.offsetMin = new Vector2(8, 16); iconRT.offsetMax = new Vector2(-8, -8);
		iconGO.GetComponent<Image>().enabled = false;
		iconGO.GetComponent<Image>().raycastTarget = false; // icon must NOT block drop events

		// → label
		var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
		labelGO.transform.SetParent(slotGO.transform, false);
		var labelRT = labelGO.GetComponent<RectTransform>();
		labelRT.anchorMin = new Vector2(0, 0); labelRT.anchorMax = new Vector2(1, 0.3f);
		labelRT.offsetMin = Vector2.zero; labelRT.offsetMax = Vector2.zero;
		var tmp = labelGO.GetComponent<TMP_Text>();
		tmp.fontSize = 14; tmp.alignment = TextAlignmentOptions.Center;
		tmp.raycastTarget = false;

		// → add EventSystem relay (inline — no separate UIEventRelay script needed)
		var relay = slotGO.AddComponent<SlotRelay>();
		int idx = index; // capture for closure
		relay.onBeginDrag = (e) => OnSlotBeginDrag(idx, e);
		relay.onDrag = (e) => OnSlotDrag(e);
		relay.onEndDrag = (e) => OnSlotEndDrag(idx, e);
		relay.onDrop = (e) => OnSlotDrop(idx, e);
		relay.onPointerDown = (e) => OnSlotClick(idx);

		return slotGO;
	}

	GameObject CreateGhost()
	{
		var go = new GameObject("DragGhost", typeof(RectTransform), typeof(Image));
		go.transform.SetParent(_canvas.transform, false);
		var rt = go.GetComponent<RectTransform>();
		rt.sizeDelta = new Vector2(50, 50);
		ghostImage = go.GetComponent<Image>();
		ghostImage.raycastTarget = false; // CRITICAL: ghost must not block drop targets
		go.SetActive(false);
		return go;
	}

	GameObject CreateInfoPanel()
	{
		// → panel background
		var panel = new GameObject("InfoPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
		panel.transform.SetParent(_canvas.transform, false);
		var rt = panel.GetComponent<RectTransform>();
		rt.anchorMin = new Vector2(1f, 0f);
		rt.anchorMax = new Vector2(1f, 0f);
		rt.pivot = new Vector2(1f, 0f);
		rt.anchoredPosition = new Vector2(-20, 20);
		rt.sizeDelta = new Vector2(180, 130);
		panel.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.95f);
		var vlg = panel.GetComponent<VerticalLayoutGroup>();
		vlg.spacing = 5; vlg.padding = new RectOffset(10, 10, 10, 10);
		vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

		// → tool name text
		var nameGO = new GameObject("NameText", typeof(RectTransform), typeof(TextMeshProUGUI));
		nameGO.transform.SetParent(panel.transform, false);
		infoNameText = nameGO.GetComponent<TMP_Text>();
		infoNameText.text = ""; infoNameText.fontSize = 18;
		infoNameText.alignment = TextAlignmentOptions.Center; infoNameText.color = Color.white;
		var nameLE = nameGO.AddComponent<LayoutElement>();
		nameLE.preferredHeight = 30;

		// → equip button
		CreatePanelButton(panel.transform, "Equip", new Color(0.2f, 0.6f, 0.8f, 1f), () => EquipSelected());
		// → drop button
		CreatePanelButton(panel.transform, "Drop", new Color(0.8f, 0.3f, 0.2f, 1f), () => DropSelected());

		panel.SetActive(false);
		return panel;
	}

	void CreatePanelButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction onClick)
	{
		var btnGO = new GameObject(label + "Btn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
		btnGO.transform.SetParent(parent, false);
		btnGO.GetComponent<Image>().color = color;
		btnGO.GetComponent<Button>().onClick.AddListener(onClick);
		btnGO.GetComponent<LayoutElement>().preferredHeight = 35;

		var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
		textGO.transform.SetParent(btnGO.transform, false);
		var textRT = textGO.GetComponent<RectTransform>();
		textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
		textRT.offsetMin = Vector2.zero; textRT.offsetMax = Vector2.zero;
		var tmp = textGO.GetComponent<TMP_Text>();
		tmp.text = label; tmp.fontSize = 16;
		tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white;
		tmp.raycastTarget = false;
	}

	TMP_Text CreateStatusText()
	{
		var go = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
		go.transform.SetParent(_canvas.transform, false);
		var rt = go.GetComponent<RectTransform>();
		rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
		rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = new Vector2(0, -20);
		rt.sizeDelta = new Vector2(400, 40);
		var tmp = go.GetComponent<TMP_Text>();
		tmp.text = "Empty hands"; tmp.fontSize = 22;
		tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white;
		tmp.raycastTarget = false;
		return tmp;
	}

	void CreateAddButton()
	{
		var go = new GameObject("AddButton", typeof(RectTransform), typeof(Image), typeof(Button));
		go.transform.SetParent(_canvas.transform, false);
		var rt = go.GetComponent<RectTransform>();
		rt.anchorMin = new Vector2(0.5f, 0f);
		rt.anchorMax = new Vector2(0.5f, 0f);
		rt.pivot = new Vector2(0.5f, 0f);
		rt.anchoredPosition = new Vector2(0, 120);
		rt.sizeDelta = new Vector2(200, 40);
		go.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.3f, 1f);
		go.GetComponent<Button>().onClick.AddListener(() => AddTool());

		var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
		textGO.transform.SetParent(go.transform, false);
		var textRT = textGO.GetComponent<RectTransform>();
		textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
		textRT.offsetMin = Vector2.zero; textRT.offsetMax = Vector2.zero;
		var tmp = textGO.GetComponent<TMP_Text>();
		tmp.text = "Add Tool (or press A)";
		tmp.fontSize = 16; tmp.alignment = TextAlignmentOptions.Center;
		tmp.color = Color.white; tmp.raycastTarget = false;
	}
}

/// <summary>
/// Minimal inline EventSystem relay — same concept as UIEventRelay but self-contained.
/// No external dependencies. Wired via Action callbacks from the prototype.
/// </summary>
public class SlotRelay : MonoBehaviour,
	IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerDownHandler
{
	public System.Action<PointerEventData> onBeginDrag, onDrag, onEndDrag, onDrop, onPointerDown;
	public void OnBeginDrag(PointerEventData e) => onBeginDrag?.Invoke(e);
	public void OnDrag(PointerEventData e) => onDrag?.Invoke(e);
	public void OnEndDrag(PointerEventData e) => onEndDrag?.Invoke(e);
	public void OnDrop(PointerEventData e) => onDrop?.Invoke(e);
	public void OnPointerDown(PointerEventData e) => onPointerDown?.Invoke(e);
}