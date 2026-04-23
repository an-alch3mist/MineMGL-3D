using UnityEngine;

/// <summary>
/// I sell anything that enters my trigger collider (IsTrigger=true). When an OrePiece enters,
/// I call SellAfterDelay which tags it MarkedForDestruction (so other triggers skip it), waits
/// 2 seconds, then adds money via EconomyManager and fires OnOreSold. The OrePiece returns to
/// pool automatically. For non-OrePiece BaseSellableItems, I sell immediately and Destroy them.
/// I skip anything already tagged MarkedForDestruction or without a Rigidbody.
///
/// Who uses me: Scene (placed as a funnel/hopper for ore to fall into).
/// Events I fire: OnOreSold (indirectly via OrePiece.SellAfterDelay).
/// </summary>
public class SellerMachine : MonoBehaviour
{
	#region Unity Life Cycle
	/// <summary> When any collider enters my trigger: skips MarkedForDestruction and no-Rigidbody objects.
	/// If it's an OrePiece, calls SellAfterDelay (2s delay → money → pool). If it's a BaseSellableItem,
	/// sells immediately and Destroys the GO. </summary>
	private void OnTriggerEnter(Collider other)
	{
		if (other.HasTag(TagType.MarkedForDestruction) || other.attachedRigidbody == null) return;
		OrePiece orePiece = other.GetComponentInParent<OrePiece>();
		if (orePiece != null)
		{
			orePiece.SellAfterDelay();
			return;
		}
		// Phase E: BoxObject sell
		// var box = other.GetComponentInParent<BoxObject>();
		// if (box != null) { SellBox(box); return; }
		BaseSellableItem sellable = other.GetComponentInParent<BaseSellableItem>();
		if (sellable != null)
		{
			Singleton<EconomyManager>.Ins.AddMoney(sellable.GetSellValue());
			// purpose: quest system tracks sold items (Phase F)
			GameEvents.RaiseOreSold(sellable.GetSellValue(), ResourceType.INVALID, PieceType.INVALID);
			Object.Destroy(sellable.gameObject);
		}
	}
	#endregion
}