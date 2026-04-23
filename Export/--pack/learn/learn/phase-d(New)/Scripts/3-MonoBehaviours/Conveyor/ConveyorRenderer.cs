using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// batch-renders all conveyor meshes using DrawMeshInstanced — reads ConveyorBatchRenderingComponent
/// static list, groups by MeshIndex, renders in batches of 1023 (Unity limit)
/// </summary>
public class ConveyorRenderer : MonoBehaviour
{
	#region Inspector Fields
	[Serializable]
	public class ConveyorMeshSet
	{
		public Mesh Mesh;
		public Material[] Materials;
	}
	[SerializeField] ConveyorMeshSet[] _conveyorMeshSets;
	#endregion

	#region private API
	readonly Dictionary<int, List<Matrix4x4>> meshBatches = new Dictionary<int, List<Matrix4x4>>();
	const int BATCH_SIZE = 1023;
	#endregion

	#region Unity Life Cycle
	private void LateUpdate()
	{
		if (ConveyorBatchRenderingComponent.GetNeedsUpdate())
		{
			meshBatches.Clear();
			foreach (var conv in ConveyorBatchRenderingComponent.AllConveyors)
			{
				int idx = conv.GetMeshIndex();
				if (!meshBatches.TryGetValue(idx, out var list))
				{
					list = new List<Matrix4x4>();
					meshBatches[idx] = list;
				}
				list.Add(conv.GetCachedMatrix());
			}
			ConveyorBatchRenderingComponent.SetNeedsUpdate(false);
		}
		foreach (var kvp in meshBatches)
		{
			var meshSet = _conveyorMeshSets[kvp.Key];
			var matrices = kvp.Value;
			for (int mat = 0; mat < meshSet.Materials.Length; mat++)
			{
				for (int i = 0; i < matrices.Count; i += BATCH_SIZE)
				{
					int count = Mathf.Min(BATCH_SIZE, matrices.Count - i);
					Graphics.DrawMeshInstanced(meshSet.Mesh, mat, meshSet.Materials[mat], matrices.GetRange(i, count));
				}
			}
		}
	}
	#endregion
}