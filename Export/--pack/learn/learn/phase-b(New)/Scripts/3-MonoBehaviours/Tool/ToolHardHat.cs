/// <summary>
/// I'm a separate tool type that extends ToolPickaxe with zero overrides. I exist so the
/// inspector can assign different prefab (hard hat model) and different SavableObjectID
/// while reusing all of ToolPickaxe's swing/raycast/damage logic. Empty class — all behavior inherited.
/// </summary>
public class ToolHardHat : ToolPickaxe { }