/// <summary>
/// I add a sell value to any physics object. SellerMachine checks for me on trigger enter —
/// if something has BaseSellableItem, it can be sold for BaseSellValue. OrePiece inherits me
/// and overrides GetSellValue to multiply by RandomPriceMultiplier. BaseHeldTool also inherits
/// me (tools are technically sellable, though most have zero sell value).
///
/// Who inherits me: BaseHeldTool → all tools, OrePiece → DamageableOrePiece.
/// </summary>
public class BaseSellableItem : BasePhysicsObject
{
	protected float baseSellValue = 1f;

	/// <summary> base sell value, optionally overridden by OrePiece etc. </summary>
	public virtual float GetSellValue() => baseSellValue;
}