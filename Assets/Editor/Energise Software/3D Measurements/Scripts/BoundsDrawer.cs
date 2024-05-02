using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Measure
{
	/// <summary>
	/// Provides utility functions to draw bounds and sizes of objects.
	/// </summary>
	public class BoundsDrawer
	{
		/// <summary>
		/// Draws the bounds of the hits recorded in the MeasureEditor's hitListData.
		/// </summary>
		public void DrawBounds()
		{
			var seenTransforms = new HashSet<Transform>();

			foreach (var hit in MeasureEditor.hitListData.hits.Where(x => x.Transform != null))
			{
				if (seenTransforms.Contains(hit.Transform)) continue;

				seenTransforms.Add(hit.Transform);

				if (MeasureEditor.InteractionType == InteractionType.Collider && hit.Collider != null)
					DrawColliderBounds(hit);
				else DrawMeshBounds(hit);
			}
		}

		/// <summary>
		/// Draws the bounds of a mesh associated with the given RaycastHitData.
		/// </summary>
		private void DrawMeshBounds(RaycastHitData hit)
		{
			var meshFilter = hit.Transform.GetComponent<MeshFilter>();
			if (!meshFilter || !meshFilter.sharedMesh) return;

			var meshBounds = meshFilter.sharedMesh.bounds;
			Handles.color = MeasureEditor.ObjectSelectColor;

			var prevMatrix = Handles.matrix;
			Handles.matrix = hit.Transform.localToWorldMatrix;
			Handles.DrawWireCube(meshBounds.center, meshBounds.size);
			Handles.matrix = prevMatrix;
		}

		/// <summary>
		/// Draws the bounds of a collider associated with the given RaycastHitData.
		/// </summary>
		private void DrawColliderBounds(RaycastHitData hit)
		{
			var bounds = hit.Collider.bounds;
			Handles.color = MeasureEditor.ObjectSelectColor;
			Handles.DrawWireCube(bounds.center, bounds.size);
		}

		/// <summary>
		/// Draws the size of the hits recorded in the MeasureEditor's hitListData on the provided SceneView.
		/// </summary>
		public void DrawSize(SceneView currentSceneView)
		{
			HashSet<Bounds> boundsHash = new();

			foreach (var hit in MeasureEditor.hitListData.hits.Where(hit => hit.Transform != null))
			{
				if (!hit.TryGetBoundsInWorld(MeasureEditor.InteractionType, out var bounds, out var isMesh)) return;

				if (boundsHash.Contains(bounds)) continue;
				boundsHash.Add(bounds);
				if (bounds.size == Vector3.zero) continue;

				var rotation = isMesh ? hit.Transform.rotation : Quaternion.identity;

				var startCorner = CalculateStartCorner(bounds, rotation);
				DrawArrow(currentSceneView, startCorner, CalculateEndCornerX(bounds, rotation),
					MeasureEditor.SizeXColor);
				DrawArrow(currentSceneView, startCorner, CalculateEndCornerY(bounds, rotation),
					MeasureEditor.SizeYColor);
				DrawArrow(currentSceneView, startCorner, CalculateEndCornerZ(bounds, rotation),
					MeasureEditor.SizeZColor);
			}
		}

		/// <summary>
		/// Calculates the start corner of the bounding box in world space after applying the specified rotation.
		/// The start corner is the minimum point of the bounding box.
		/// </summary>
		/// <param name="bounds">Bounding box.</param>
		/// <param name="rotation">Rotation to be applied.</param>
		/// <returns>The rotated start corner in world space.</returns>
		private Vector3 CalculateStartCorner(Bounds bounds, Quaternion rotation) =>
			RotateAroundCenter(bounds.min, bounds.center, rotation);

		/// <summary>
		/// Rotates a point around a specified center using the given rotation.
		/// </summary>
		/// <param name="point">Point to be rotated.</param>
		/// <param name="center">Center of rotation.</param>
		/// <param name="rotation">Rotation to be applied.</param>
		/// <returns>The rotated point.</returns>
		private Vector3 RotateAroundCenter(Vector3 point, Vector3 center, Quaternion rotation) =>
			rotation * (point - center) + center;

		/// <summary>
		/// Calculates the end corner of the bounding box along the X axis in world space after applying the specified rotation.
		/// </summary>
		/// <param name="bounds">Bounding box.</param>
		/// <param name="rotation">Rotation to be applied.</param>
		/// <returns>The rotated end corner for the X axis in world space.</returns>
		private Vector3 CalculateEndCornerX(Bounds bounds, Quaternion rotation) =>
			RotateAroundCenter(new Vector3(bounds.max.x, bounds.min.y, bounds.min.z), bounds.center, rotation);

		/// <summary>
		/// Calculates the end corner of the bounding box along the Y axis in world space after applying the specified rotation.
		/// </summary>
		/// <param name="bounds">Bounding box.</param>
		/// <param name="rotation">Rotation to be applied.</param>
		/// <returns>The rotated end corner for the Y axis in world space.</returns>
		private Vector3 CalculateEndCornerY(Bounds bounds, Quaternion rotation) =>
			RotateAroundCenter(new Vector3(bounds.min.x, bounds.max.y, bounds.min.z), bounds.center, rotation);

		/// <summary>
		/// Calculates the end corner of the bounding box along the Z axis in world space after applying the specified rotation.
		/// </summary>
		/// <param name="bounds">Bounding box.</param>
		/// <param name="rotation">Rotation to be applied.</param>
		/// <returns>The rotated end corner for the Z axis in world space.</returns>
		private Vector3 CalculateEndCornerZ(Bounds bounds, Quaternion rotation) =>
			RotateAroundCenter(new Vector3(bounds.min.x, bounds.min.y, bounds.max.z), bounds.center, rotation);

		/// <summary>
		/// Draws an arrow between the start and end positions on the given SceneView with the specified color.
		/// </summary>
		private void DrawArrow(SceneView sceneView, Vector3 start, Vector3 end, Color color)
		{
			new LineBuilder(sceneView, start, end)
				.WithLineColor(color)
				.WithLabelColor(color)
				.WithEndMarker()
				.Draw();
		}
	}
}