using UnityEngine;

/// <summary>
/// stub — expanded in Phase G with full save/load
/// </summary>
public interface ISaveLoadableObject
{
	bool HasBeenSaved { get; set; }
	bool ShouldBeSaved();
	SavableObjectID GetSavableObjectID();
	Vector3 GetPosition();
	Vector3 GetRotation();
	void LoadFromSave(string customDataJson);
	string GetCustomSaveData();
}