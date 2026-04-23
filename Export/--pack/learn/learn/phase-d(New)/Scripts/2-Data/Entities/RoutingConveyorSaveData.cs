using System;

/// <summary>
/// Simple save data struct for any toggleable building component (RoutingConveyor, ConveyorBlockerT2,
/// ChuteHatch). Stores whether the component is in its "closed" state. Phase G's SavingLoadingManager
/// serializes this to JSON via ICustomSaveDataProvider.GetCustomSaveData().
/// </summary>
[Serializable]
public class RoutingConveyorSaveData
{
	public bool IsClosed;
}