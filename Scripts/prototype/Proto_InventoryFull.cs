using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// STANDALONE prototype — full inventory loop: 3D world items + UI slots + equip/drop.
/// Drop on empty scene with Canvas + EventSystem + Camera + Floor → Press Play.
/// Zero external dependencies — no GameEvents, no DataService, no BaseHeldTool.
///
/// SETUP (1 minute):
///   1. New empty scene
///   2. GameObject → UI → Canvas (auto-creates EventSystem)
///   3. Create Camera at (0, 3, -5) looking at origin, tag "MainCamera"
///   4. Create Floor: 3D Object → Plane at (0, 0, 0), scale (3, 1, 3)
///   5. Create empty GO → name "Proto_InventoryFull" → add this script
///   6. Wire: _canvas → Canvas, _cam → Camera
///   7. Press Play
///
/// WHAT HAPPENS ON PLAY:
///   - 4 colored cubes spawn on the floor (world items)
///   - 5-slot hotbar appears at bottom of screen
///   - Info panel (right side) with Equip + Drop buttons
///
/// CONTROLS:
///   Click cube in world  → picks up into first empty slot + auto-equips (cube floats in front of cam)
///   Click cube again     → duplicate guard — nothing happens (already in inventory)
///   1-5                  → select hotbar slot — auto-equips if has item, empty hands if empty
///   Press same key twice → toggles equip/unequip (empty hands)
///   Scroll wheel         → cycles through occupied slots only (skips empty)
///   Click slot           → shows info panel with Equip/Drop buttons
///   Equip button         → same as selecting that slot (auto-equips)
///   Drop button          → cube drops to floor with velocity, slot empties, "Empty hands"
///   Drag slot → slot     → swap contents (equipped tracking follows the swap)
///   Drag outside UI      → drop item to world
///   G key                → drop equipped item
///
/// WHAT TO VERIFY:
///   - Click world cube → disappears, icon in slot, cube floats in front of camera, status "Equipped: X"
///   - Click same cube again → nothing (duplicate guard)
///   - Press 1 → equip slot 0. Press 1 again → unequip (empty hands). Press 1 again → re-equip.
///   - Press 2 (empty slot) → empty hands, no cube visible
///   - Scroll wheel → skips empty slots, lands on next occupied
///   - Drop (G) → cube falls to floor with physics, status "Empty hands"
///   - Pick up dropped cube again → works (inInventory flag was cleared)
///   - Drag slot A → slot B → contents swap, equipped item follows if swapped
///   - Info panel shows correct name
///   - All actions logged to console
///
/// HELPER CLASSES (defined at bottom of this file — no separate .cs needed):
///   WorldItemTag  — MonoBehaviour with public int itemIndex, added to each world cube at runtime
///   SlotRelay2    — IBeginDragHandler/IDragHandler/IEndDragHandler/IDropHandler/IPointerDownHandler,
///                   added to each UI slot GO at runtime via AddComponent
///
/// EVERYTHING IS RUNTIME-CREATED except: Canvas, EventSystem, Camera, Floor, and this script's GO.
/// </summary>
public class Proto_InventoryFull : MonoBehaviour
{
	[SerializeField] Canvas _canvas;
	[SerializeField] Camera _cam;

	// ─── data ───────────────────────────────────────────────────
	struct ItemData
	{
		public string name;
		public Color color;
		public GameObject worldGO;
		public bool inInventory;
	}

	List<ItemData> allItems = new List<ItemData>();
	ItemData?[] slots;
	int slotCount = 5;
	int selectedSlot = 0;
	int equippedSlot = -1;

	// ─── UI refs ────────────────────────────────────────────────
	List<GameObject> slotGOs = new List<GameObject>();
	GameObject ghostGO;
	Image ghostImage;
	GameObject infoPanelGO;
	TMP_Text infoNameText;
	TMP_Text statusText;
	int dragFromIndex = -1;

	// ─── lifecycle ──────────────────────────────────────────────

	void Start()
	{
		slots = new ItemData?[slotCount];

		// → spawn 4 world cubes
		SpawnWorldItem("Pickaxe", new Color(0.8f, 0.4f, 0.2f), new Vector3(-2, 0.5f, 0));
		SpawnWorldItem("Magnet", new Color(0.9f, 0.2f, 0.2f), new Vector3(-0.7f, 0.5f, 0));
		SpawnWorldItem("Hammer", new Color(0.5f, 0.5f, 0.6f), new Vector3(0.7f, 0.5f, 0));
		SpawnWorldItem("Hat", new Color(0.9f, 0.9f, 0.2f), new Vector3(2, 0.5f, 0));

		// → build UI
		var container = CreateSlotContainer();
		for (int i = 0; i < slotCount; i++)
			slotGOs.Add(CreateSlotGO(container, i));
		ghostGO = CreateGhost();
		infoPanelGO = CreateInfoPanel();
		statusText = CreateStatusText();

		SelectSlot(0);
		UpdateStatus();
		Debug.Log("[Proto] Ready. Click cubes to pick up, 1-5 select, scroll cycle, G drop.");
	}

	void Update()
	{
		// → click world items to pick up
		if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
		{
			Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray, out var hit, 50f))
			{
				var worldItem = hit.collider.GetComponent<WorldItemTag>();
				if (worldItem != null) TryPickup(worldItem.itemIndex);
			}
		}

		// → hotbar keys (selecting auto-equips, like main source)
		if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
		if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
		if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
		if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(3);
		if (Input.GetKeyDown(KeyCode.Alpha5)) SelectSlot(4);

		// → scroll wheel: cycle through occupied hotbar slots (skip empty)
		float scroll = Input.GetAxis("Mouse ScrollWheel");
		if (scroll != 0f) ScrollToNextOccupied(scroll > 0f ? 1 : -1);

		// → G = drop equipped item
		if (Input.GetKeyDown(KeyCode.G)) DropEquipped();

		// → keep equipped item floating in front of camera
		if (equippedSlot >= 0 && slots[equippedSlot].HasValue)
		{
			var item = slots[equippedSlot].Value;
			if (item.worldGO != null)
			{
				item.worldGO.transform.position = _cam.transform.position + _cam.transform.forward * 2f + Vector3.down * 0.3f;
				item.worldGO.transform.rotation = _cam.transform.rotation;
			}
		}
	}

	// ─── inventory actions ──────────────────────────────────────

	void TryPickup(int itemIndex)
	{
		if (itemIndex < 0 || itemIndex >= allItems.Count) return;
		var item = allItems[itemIndex];

		// → duplicate guard: skip if already in inventory
		if (item.inInventory)
		{
			Debug.Log($"[Proto] '{item.name}' already in inventory — skipped");
			return;
		}

		// → find first empty slot
		int target = -1;
		for (int i = 0; i < slots.Length; i++)
			if (!slots[i].HasValue) { target = i; break; }
		if (target == -1) { Debug.Log("[Proto] Inventory full!"); return; }

		// → pick up: hide world GO, store in slot
		item.inInventory = true;
		item.worldGO.SetActive(false);
		allItems[itemIndex] = item;
		slots[target] = item;

		// → auto-equip on pickup (like EquipWhenPickedUp in main source)
		SelectSlot(target);
		Debug.Log($"[Proto] Picked up '{item.name}' → slot {target} (auto-equipped)");
	}

	void SelectSlot(int index)
	{
		if (index < 0 || index >= slotCount) return;

		// → hide previously equipped item
		if (equippedSlot >= 0 && slots[equippedSlot].HasValue)
			slots[equippedSlot].Value.worldGO.SetActive(false);

		// → toggle: pressing same slot again unequips (empty hands)
		if (index == equippedSlot)
		{
			equippedSlot = -1;
			selectedSlot = index;
			RefreshAllSlots();
			UpdateStatus();
			Debug.Log($"[Proto] UNEQUIPPED (empty hands)");
			return;
		}

		selectedSlot = index;

		// → auto-equip if slot has item (like main source: pressing 1-5 equips that tool)
		if (slots[index].HasValue)
		{
			var item = slots[index].Value;
			item.worldGO.SetActive(true);
			item.worldGO.GetComponent<Rigidbody>().isKinematic = true;
			equippedSlot = index;
			Debug.Log($"[Proto] EQUIPPED: {item.name} (slot {index})");
		}
		else
		{
			// → selected empty slot = empty hands
			equippedSlot = -1;
			Debug.Log($"[Proto] Selected empty slot {index} (empty hands)");
		}

		RefreshAllSlots();
		UpdateStatus();
	}

	void ScrollToNextOccupied(int dir)
	{
		int start = selectedSlot;
		for (int i = 0; i < slotCount; i++)
		{
			int next = (start + dir + slotCount) % slotCount;
			start = next;
			if (slots[next].HasValue) { SelectSlot(next); return; }
		}
	}

	void UpdateStatus()
	{
		if (statusText == null) return;
		if (equippedSlot >= 0 && slots[equippedSlot].HasValue)
			statusText.text = $"Equipped: {slots[equippedSlot].Value.name}";
		else
			statusText.text = "Empty hands";
	}

	void DropEquipped()
	{
		if (equippedSlot < 0 || !slots[equippedSlot].HasValue) return;
		DropFromSlot(equippedSlot);
		equippedSlot = -1;
	}

	void DropFromSlot(int index)
	{
		if (!slots[index].HasValue) return;
		var item = slots[index].Value;

		// → show world GO, apply physics drop
		item.worldGO.SetActive(true);
		item.worldGO.transform.position = _cam.transform.position + _cam.transform.forward * 2f;
		var rb = item.worldGO.GetComponent<Rigidbody>();
		rb.isKinematic = false;
		rb.linearVelocity = _cam.transform.forward * 3f + Vector3.up * 2f;

		// → mark as not in inventory
		item.inInventory = false;
		int itemIdx = allItems.FindIndex(x => x.name == item.name);
		if (itemIdx >= 0) allItems[itemIdx] = item;

		slots[index] = null;
		if (equippedSlot == index) equippedSlot = -1;
		infoPanelGO.SetActive(false);
		RefreshAllSlots();
		UpdateStatus();
		Debug.Log($"[Proto] DROPPED: {item.name} from slot {index}");
	}

	void SwapSlots(int a, int b)
	{
		if (a < 0 || b < 0 || a >= slotCount || b >= slotCount || a == b) return;
		var temp = slots[a];
		slots[a] = slots[b];
		slots[b] = temp;
		// → update equipped slot tracking
		if (equippedSlot == a) equippedSlot = b;
		else if (equippedSlot == b) equippedSlot = a;
		RefreshAllSlots();
		Debug.Log($"[Proto] Swapped slot {a} ↔ {b}");
	}

	void RefreshAllSlots()
	{
		for (int i = 0; i < slotGOs.Count; i++)
		{
			var icon = slotGOs[i].transform.Find("Icon").GetComponent<Image>();
			var label = slotGOs[i].transform.Find("Label").GetComponent<TMP_Text>();
			var bg = slotGOs[i].GetComponent<Image>();

			bool isSelected = (i == selectedSlot);
			bool isEquipped = (i == equippedSlot);
			bg.color = isEquipped ? new Color(0.2f, 0.5f, 0.9f, 0.4f)
				: isSelected ? new Color(0.3f, 0.8f, 0.4f, 0.3f)
				: new Color(0.2f, 0.2f, 0.2f, 0.5f);

			if (slots[i].HasValue)
			{
				icon.enabled = true;
				icon.color = slots[i].Value.color;
				label.text = slots[i].Value.name;
			}
			else
			{
				icon.enabled = false;
				label.text = "";
			}
		}
	}

	void ShowInfoPanel(int index)
	{
		if (!slots[index].HasValue) { infoPanelGO.SetActive(false); return; }
		infoPanelGO.SetActive(true);
		infoNameText.text = slots[index].Value.name;
	}

	void EquipSelected()
	{
		if (selectedSlot < 0 || !slots[selectedSlot].HasValue) return;
		SelectSlot(selectedSlot); // SelectSlot already handles equip + toggle logic
		Debug.Log($"[Proto] EQUIP BUTTON → slot {selectedSlot}");
	}

	// ─── drag-drop handlers ─────────────────────────────────────

	void OnSlotBeginDrag(int index, PointerEventData e)
	{
		if (!slots[index].HasValue) return;
		dragFromIndex = index;
		slotGOs[index].transform.Find("Icon").gameObject.SetActive(false);
		slotGOs[index].transform.Find("Label").gameObject.SetActive(false);
		ghostGO.SetActive(true);
		ghostImage.color = slots[index].Value.color;
		ghostGO.transform.SetAsLastSibling();
	}

	void OnSlotDrag(PointerEventData e)
	{
		if (dragFromIndex < 0) return;
		ghostGO.transform.position = e.position;
	}

	void OnSlotEndDrag(int index, PointerEventData e)
	{
		slotGOs[index].transform.Find("Icon").gameObject.SetActive(true);
		slotGOs[index].transform.Find("Label").gameObject.SetActive(true);
		ghostGO.SetActive(false);
		// → drop outside UI = drop item to world
		if (dragFromIndex >= 0 && e.pointerEnter == null)
			DropFromSlot(dragFromIndex);
		dragFromIndex = -1;
		RefreshAllSlots();
	}

	void OnSlotDrop(int targetIndex, PointerEventData e)
	{
		if (dragFromIndex < 0 || dragFromIndex == targetIndex) return;
		SwapSlots(dragFromIndex, targetIndex);
	}

	// ─── status text ────────────────────────────────────────────

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

	// ─── world item spawning ────────────────────────────────────

	void SpawnWorldItem(string name, Color color, Vector3 pos)
	{
		var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
		go.name = name;
		go.transform.position = pos;
		go.transform.localScale = Vector3.one * 0.4f;
		go.GetComponent<Renderer>().material.color = color;
		var rb = go.AddComponent<Rigidbody>();
		rb.mass = 0.5f;
		var tag = go.AddComponent<WorldItemTag>();
		tag.itemIndex = allItems.Count;
		allItems.Add(new ItemData { name = name, color = color, worldGO = go, inInventory = false });
	}

	// ─── UI creation (all runtime) ──────────────────────────────

	Transform CreateSlotContainer()
	{
		var go = new GameObject("SlotContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(Image));
		go.transform.SetParent(_canvas.transform, false);
		var rt = go.GetComponent<RectTransform>();
		rt.anchorMin = new Vector2(0.5f, 0f); rt.anchorMax = new Vector2(0.5f, 0f);
		rt.pivot = new Vector2(0.5f, 0f); rt.anchoredPosition = new Vector2(0, 20);
		rt.sizeDelta = new Vector2(slotCount * 80 + 20, 90);
		var hlg = go.GetComponent<HorizontalLayoutGroup>();
		hlg.spacing = 5; hlg.padding = new RectOffset(5, 5, 5, 5); hlg.childAlignment = TextAnchor.MiddleCenter;
		go.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
		return go.transform;
	}

	GameObject CreateSlotGO(Transform parent, int index)
	{
		var slotGO = new GameObject($"Slot_{index}", typeof(RectTransform), typeof(Image));
		slotGO.transform.SetParent(parent, false);
		slotGO.GetComponent<RectTransform>().sizeDelta = new Vector2(70, 70);
		slotGO.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
		slotGO.GetComponent<Image>().raycastTarget = true;

		var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
		iconGO.transform.SetParent(slotGO.transform, false);
		var iconRT = iconGO.GetComponent<RectTransform>();
		iconRT.anchorMin = Vector2.zero; iconRT.anchorMax = Vector2.one;
		iconRT.offsetMin = new Vector2(8, 16); iconRT.offsetMax = new Vector2(-8, -8);
		iconGO.GetComponent<Image>().enabled = false;
		iconGO.GetComponent<Image>().raycastTarget = false;

		var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
		labelGO.transform.SetParent(slotGO.transform, false);
		var labelRT = labelGO.GetComponent<RectTransform>();
		labelRT.anchorMin = new Vector2(0, 0); labelRT.anchorMax = new Vector2(1, 0.3f);
		labelRT.offsetMin = Vector2.zero; labelRT.offsetMax = Vector2.zero;
		var tmp = labelGO.GetComponent<TMP_Text>();
		tmp.fontSize = 8; tmp.alignment = TextAlignmentOptions.Center; tmp.raycastTarget = false;

		var relay = slotGO.AddComponent<SlotRelay2>();
		int idx = index;
		relay.onBeginDrag = (e) => OnSlotBeginDrag(idx, e);
		relay.onDrag = (e) => OnSlotDrag(e);
		relay.onEndDrag = (e) => OnSlotEndDrag(idx, e);
		relay.onDrop = (e) => OnSlotDrop(idx, e);
		relay.onPointerDown = (e) => { SelectSlot(idx); ShowInfoPanel(idx); };
		return slotGO;
	}

	GameObject CreateGhost()
	{
		var go = new GameObject("DragGhost", typeof(RectTransform), typeof(Image));
		go.transform.SetParent(_canvas.transform, false);
		go.GetComponent<RectTransform>().sizeDelta = new Vector2(50, 50);
		ghostImage = go.GetComponent<Image>();
		ghostImage.raycastTarget = false;
		go.SetActive(false);
		return go;
	}

	GameObject CreateInfoPanel()
	{
		var panel = new GameObject("InfoPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
		panel.transform.SetParent(_canvas.transform, false);
		var rt = panel.GetComponent<RectTransform>();
		rt.anchorMin = new Vector2(1f, 0f); rt.anchorMax = new Vector2(1f, 0f);
		rt.pivot = new Vector2(1f, 0f); rt.anchoredPosition = new Vector2(-20, 20);
		rt.sizeDelta = new Vector2(180, 130);
		panel.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.95f);
		var vlg = panel.GetComponent<VerticalLayoutGroup>();
		vlg.spacing = 5; vlg.padding = new RectOffset(10, 10, 10, 10);
		vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

		var nameGO = new GameObject("NameText", typeof(RectTransform), typeof(TextMeshProUGUI));
		nameGO.transform.SetParent(panel.transform, false);
		infoNameText = nameGO.GetComponent<TMP_Text>();
		infoNameText.fontSize = 18; infoNameText.alignment = TextAlignmentOptions.Center;
		infoNameText.color = Color.white;
		nameGO.AddComponent<LayoutElement>().preferredHeight = 30;

		CreatePanelButton(panel.transform, "Equip (E)", new Color(0.2f, 0.6f, 0.8f), () => EquipSelected());
		CreatePanelButton(panel.transform, "Drop (G)", new Color(0.8f, 0.3f, 0.2f), () => DropFromSlot(selectedSlot));
		panel.SetActive(false);
		return panel;
	}

	void CreatePanelButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction onClick)
	{
		var btnGO = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
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
		tmp.text = label; tmp.fontSize = 14; tmp.alignment = TextAlignmentOptions.Center;
		tmp.color = Color.white; tmp.raycastTarget = false;
	}
}

/// <summary> Tags a world cube with its item index so raycast can identify it. </summary>
public class WorldItemTag : MonoBehaviour { public int itemIndex; }

/// <summary> Inline EventSystem relay — same as SlotRelay but separate class name to avoid conflict. </summary>
public class SlotRelay2 : MonoBehaviour,
	IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerDownHandler
{
	public System.Action<PointerEventData> onBeginDrag, onDrag, onEndDrag, onDrop, onPointerDown;
	public void OnBeginDrag(PointerEventData e) => onBeginDrag?.Invoke(e);
	public void OnDrag(PointerEventData e) => onDrag?.Invoke(e);
	public void OnEndDrag(PointerEventData e) => onEndDrag?.Invoke(e);
	public void OnDrop(PointerEventData e) => onDrop?.Invoke(e);
	public void OnPointerDown(PointerEventData e) => onPointerDown?.Invoke(e);
}