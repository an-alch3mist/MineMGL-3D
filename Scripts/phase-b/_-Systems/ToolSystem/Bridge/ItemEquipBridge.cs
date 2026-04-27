using UnityEngine;

/// <summary>
/// Bridges inventory equip events to items that need player context (camera, viewmodel container, magnet pos).
/// This is the ONLY script that connects PlayerSystem to InventorySystem/ToolSystem.
/// Place on the Player GO alongside PlayerMovement. Wire the 3 transforms in inspector.
///
/// Without this script: tools can't raycast from camera, can't parent ViewModel, can't position magnet.
/// With this script: items receive SetOwnerContext(cam, container, magnetPos) on equip.
///
/// Lives in ToolSystem (not PlayerSystem) because it references IInventoryItem — PlayerSystem stays portable.
/// </summary>
[AddComponentMenu("MineMGL/Bridge/ItemEquipBridge")]
public class ItemEquipBridge : MonoBehaviour
{
	[SerializeField] Camera _cam;
	[SerializeField] Transform _viewModelContainer;
	[SerializeField] Transform _magnetToolPos;
	//
	void Start()
	{
		// purpose: send player context to equipped item so it can raycast + parent ViewModel
		GameEvents.OnItemEquipped += (item) => item.SetOwnerContext(_cam, _viewModelContainer, _magnetToolPos);
	}
}