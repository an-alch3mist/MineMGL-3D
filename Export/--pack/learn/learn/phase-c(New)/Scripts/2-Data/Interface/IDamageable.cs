using UnityEngine;

/// <summary>
/// Any object that can take damage at a world position. OreNode implements this to lose health
/// when the pickaxe hits it. DamageableOrePiece implements this to break into cluster pieces
/// when hit hard enough by collision. ToolPickaxe calls TakeDamage on whatever IDamageable
/// the raycast hits — it doesn't know if it's a node or an ore piece.
/// </summary>
public interface IDamageable
{
	void TakeDamage(float damage, Vector3 position);
}