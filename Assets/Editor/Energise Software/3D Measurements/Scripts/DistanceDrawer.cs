using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Measure
{
	/// <summary>
	/// A class to draw distances on various metrics in the Unity scene view.
	/// </summary>
	public class DistanceDrawer
	{
		/// <summary>
		/// Shows the distance between vertices.
		/// </summary>
		public void ShowVertexDistance()
		{
			int count = MeasureEditor.hitListData.hits.Count;
			if (count < 2) return;

			var currentSceneView = SceneView.lastActiveSceneView;
			if (currentSceneView == null) return;

			switch (MeasureEditor.Sequence)
			{
				case MeasureSequence.All:
					for (var i = 0; i < count - 1; i++)
					{
						for (var j = i + 1; j < count; j++)
						{
							DrawLineBetweenHits(i, j, currentSceneView);
						}
					}

					break;

				case MeasureSequence.Linear:
					for (var i = 0; i < count - 1; i++)
					{
						DrawLineBetweenHits(i, i + 1, currentSceneView);
					}

					break;

				case MeasureSequence.Pair:
					for (var i = 0; i < count - 1; i += 2)
					{
						DrawLineBetweenHits(i, i + 1, currentSceneView);
					}

					break;
			}
		}

		private void DrawLineBetweenHits(int index1, int index2, SceneView sceneView)
		{
			if (!MeasureEditor.CalculateClosestVertex(MeasureEditor.hitListData.hits[index1], out var closestVertex1))
				return;
			if (!MeasureEditor.CalculateClosestVertex(MeasureEditor.hitListData.hits[index2], out var closestVertex2))
				return;

			new LineBuilder(sceneView, closestVertex1, closestVertex2)
				.WithLineColor(MeasureEditor.DistanceLineColor)
				.WithLabelColor(MeasureEditor.TextColor)
				.WithXYZ()
				.WithStartMarker()
				.WithEndMarker()
				.WithDots()
				.Draw();
		}

		/// <summary>
		/// Shows the distance between hit points.
		/// </summary>
		public void ShowHitDistance()
		{
			int count = MeasureEditor.hitListData.hits.Count;
			if (count < 2) return;

			var currentSceneView = SceneView.lastActiveSceneView;
			if (currentSceneView == null) return;

			switch (MeasureEditor.Sequence)
			{
				case MeasureSequence.All:
					for (var i = 0; i < count - 1; i++)
					{
						for (var j = i + 1; j < count; j++)
						{
							DrawLineBetweenHitPoints(i, j, currentSceneView);
						}
					}

					break;

				case MeasureSequence.Linear:
					for (var i = 0; i < count - 1; i++)
					{
						DrawLineBetweenHitPoints(i, i + 1, currentSceneView);
					}

					break;

				case MeasureSequence.Pair:
					for (var i = 0; i < count - 1; i += 2)
					{
						DrawLineBetweenHitPoints(i, i + 1, currentSceneView);
					}

					break;
			}
		}

		private void DrawLineBetweenHitPoints(int index1, int index2, SceneView sceneView)
		{
			var hit1 = MeasureEditor.hitListData.hits[index1];
			var hit2 = MeasureEditor.hitListData.hits[index2];
			Vector3 pointA = hit1.Point;
			Vector3 pointB = hit2.Point;

			if (hit1.Transform != null)
			{
				pointA = MeasureEditor.hitListData.hits[index1]
					.AdjustToWorldUsingCurrentTransform(pointA, hit1.Transform);
			}

			if (hit2.Transform != null)
			{
				pointB = MeasureEditor.hitListData.hits[index2]
					.AdjustToWorldUsingCurrentTransform(pointB, hit2.Transform);
			}

			new LineBuilder(sceneView, pointA, pointB)
				.WithLineColor(MeasureEditor.DistanceLineColor)
				.WithLabelColor(MeasureEditor.TextColor)
				.WithXYZ()
				.WithStartMarker()
				.WithEndMarker()
				.WithDots()
				.Draw();
		}

		/// <summary>
		/// Shows the distance between the center of mass of objects based on the selected MeasureSequence.
		/// </summary>
		public void ShowCentreMassDistance()
		{
			int count = MeasureEditor.hitListData.hits.Count;
			if (count < 2) return;

			var currentSceneView = SceneView.lastActiveSceneView;
			if (currentSceneView == null) return;

			HashSet<string> drawnPairs = new HashSet<string>();

			switch (MeasureEditor.Sequence)
			{
				case MeasureSequence.All:
					for (var i = 0; i < count - 1; i++)
					{
						for (var j = i + 1; j < count; j++)
						{
							DrawCenterMassLine(i, j, currentSceneView, drawnPairs);
						}
					}

					break;

				case MeasureSequence.Linear:
					for (var i = 0; i < count - 1; i++)
					{
						DrawCenterMassLine(i, i + 1, currentSceneView, drawnPairs);
					}

					break;

				case MeasureSequence.Pair:
					for (var i = 0; i < count - 1; i += 2)
					{
						DrawCenterMassLine(i, i + 1, currentSceneView, drawnPairs);
					}

					break;
			}
		}

		/// <summary>
		/// Draws a line between the centers of mass of two hits in the current scene view.
		/// Ensures that lines are only drawn between distinct game objects and avoids redundancy for already processed pairs.
		/// </summary>
		/// <param name="index1">Index of the first hit in the hit list.</param>
		/// <param name="index2">Index of the second hit in the hit list.</param>
		/// <param name="sceneView">The current SceneView.</param>
		/// <param name="drawnPairs">HashSet tracking pairs of game objects that have been processed to prevent redundancy.</param>
		private void DrawCenterMassLine(int index1, int index2, SceneView sceneView, HashSet<string> drawnPairs)
		{
			var hit1 = MeasureEditor.hitListData.hits[index1];
			var hit2 = MeasureEditor.hitListData.hits[index2];

			if (hit1.Transform == null || hit2.Transform == null) return;
			if (hit1.GameObject == hit2.GameObject) return;

			var ids = new List<int>
			{
				hit1.GameObject.GetInstanceID(),
				hit2.GameObject.GetInstanceID()
			};
			ids.Sort();
			var pairId = $"{ids[0]}_{ids[1]}";

			if (drawnPairs.Contains(pairId)) return;
			drawnPairs.Add(pairId);

			if (!hit1.TryGetBoundsInWorld(MeasureEditor.InteractionType, out var b1, out var isMesh) ||
			    !hit2.TryGetBoundsInWorld(MeasureEditor.InteractionType, out var b2, out var isMesh2))
			{
				return;
			}

			Vector3 center1 = b1.center;
			Vector3 center2 = b2.center;

			new LineBuilder(sceneView, center1, center2)
				.WithLineColor(MeasureEditor.DistanceLineColor)
				.WithLabelColor(MeasureEditor.TextColor)
				.WithEndMarker()
				.WithStartMarker()
				.WithXYZ()
				.WithDots()
				.Draw();
		}

		/// <summary>
		/// Shows the distance between the closest points on objects.
		/// </summary>
		public void ShowClosestPointDistance()
		{
			int count = MeasureEditor.hitListData.hits.Count;
			if (count < 2) return;

			var currentSceneView = SceneView.lastActiveSceneView;
			if (currentSceneView == null) return;

			HashSet<string> processedPairs = new HashSet<string>();

			switch (MeasureEditor.Sequence)
			{
				case MeasureSequence.All:
					for (var i = 0; i < count - 1; i++)
					{
						for (var j = i + 1; j < count; j++)
						{
							DrawClosestPointLine(i, j, currentSceneView, processedPairs);
						}
					}

					break;

				case MeasureSequence.Linear:
					for (var i = 0; i < count - 1; i++)
					{
						DrawClosestPointLine(i, i + 1, currentSceneView, processedPairs);
					}

					break;

				case MeasureSequence.Pair:
					for (var i = 0; i < count - 1; i += 2)
					{
						DrawClosestPointLine(i, i + 1, currentSceneView, processedPairs);
					}

					break;
			}
		}

		/// <summary>
		/// Draws a line between the closest points of two hits in the current scene view.
		/// This method ensures that lines are only drawn between distinct game objects and
		/// avoids redrawing for already processed pairs of game objects.
		/// </summary>
		/// <param name="index1">Index of the first hit in the hit list.</param>
		/// <param name="index2">Index of the second hit in the hit list.</param>
		/// <param name="sceneView">The current SceneView.</param>
		/// <param name="processedPairs">HashSet tracking pairs of game objects that have been processed to prevent redundancy.</param>
		private void DrawClosestPointLine(int index1, int index2, SceneView sceneView, HashSet<string> processedPairs)
		{
			var hitA = MeasureEditor.hitListData.hits[index1];
			var hitB = MeasureEditor.hitListData.hits[index2];

			if (hitA.GameObject == hitB.GameObject) return;
			if (hitA.GameObject == null || hitA.Transform == null) return;
			if (hitB.GameObject == null || hitB.Transform == null) return;
			var ids = new List<int>
			{
				hitA.GameObject.GetInstanceID(),
				hitB.GameObject.GetInstanceID()
			};
			ids.Sort();
			var pairId = $"{ids[0]}_{ids[1]}";

			if (processedPairs.Contains(pairId)) return;
			processedPairs.Add(pairId);

			Vector3 initialClosestPointOnB = GetClosestPoint(hitB, hitA.Transform.position);
			Vector3 closestPointOnA = GetClosestPoint(hitA, initialClosestPointOnB);
			Vector3 closestPointOnB = GetClosestPoint(hitB, closestPointOnA);

			new LineBuilder(sceneView, closestPointOnA, closestPointOnB)
				.WithLineColor(MeasureEditor.DistanceLineColor)
				.WithLabelColor(MeasureEditor.TextColor)
				.WithEndMarker()
				.WithStartMarker()
				.WithXYZ()
				.WithDots()
				.Draw();
		}

		/// <summary>
		/// Gets the closest point on a RaycastHitData's object to a given point.
		/// </summary>
		private Vector3 GetClosestPoint(RaycastHitData hit, Vector3 point)
		{
			// If the interaction type is Collider or there's no mesh
			if (MeasureEditor.InteractionType == InteractionType.Collider ||
			    (hit.MeshFilter == null || hit.MeshFilter.sharedMesh == null))
			{
				if (hit.Collider != null)
				{
					return hit.Collider.ClosestPointOnBounds(point);
				}
			}

			// If the interaction type is Mesh or there's no collider
			if (MeasureEditor.InteractionType == InteractionType.Mesh ||
			    hit.Collider == null)
			{
				if (hit.MeshFilter != null && hit.MeshFilter.sharedMesh != null)
				{
					return GetClosestPointOnMesh(hit.MeshFilter, point);
				}
			}

			return point;
		}

		/// <summary>
		/// Determines the closest point on a mesh to a given world point.
		/// </summary>
		private Vector3 GetClosestPointOnMesh(MeshFilter meshFilter, Vector3 worldPoint)
		{
			Mesh mesh = meshFilter.sharedMesh;
			Vector3[] vertices = mesh.vertices;
			int[] triangles = mesh.triangles;
			Vector3 closestPoint = Vector3.zero;
			float closestDistance = float.MaxValue;

			for (int i = 0; i < triangles.Length; i += 3)
			{
				Vector3 worldVertexA = meshFilter.transform.TransformPoint(vertices[triangles[i]]);
				Vector3 worldVertexB = meshFilter.transform.TransformPoint(vertices[triangles[i + 1]]);
				Vector3 worldVertexC = meshFilter.transform.TransformPoint(vertices[triangles[i + 2]]);

				Vector3 closestPointOnTri =
					ClosestPointOnTriangle(worldVertexA, worldVertexB, worldVertexC, worldPoint);
				float distance = Vector3.Distance(closestPointOnTri, worldPoint);

				if (distance < closestDistance)
				{
					closestDistance = distance;
					closestPoint = closestPointOnTri;
				}
			}

			return closestPoint;
		}

		/// <summary>
		/// Determines the closest point on a triangle to a given point.
		/// </summary>
		private Vector3 ClosestPointOnTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 point)
		{
			// http://www.r-5.org/files/books/computers/algo-list/realtime-3d/Christer_Ericson-Real-Time_Collision_Detection-EN.pdf
			// https://jvm-gaming.org/t/triangle-intersection-tests/43457
			var ab = b - a;
			var ac = c - a;
			var ap = point - a;

			var d1 = Vector3.Dot(ab, ap);
			var d2 = Vector3.Dot(ac, ap);
			if (d1 <= 0.0f && d2 <= 0.0f) return a;

			var bp = point - b;
			var d3 = Vector3.Dot(ab, bp);
			var d4 = Vector3.Dot(ac, bp);
			if (d3 >= 0.0f && d4 <= d3) return b;

			var vc = d1 * d4 - d3 * d2;
			if (vc <= 0.0f && d1 >= 0.0f && d3 <= 0.0f)
			{
				var v = d1 / (d1 - d3);
				return a + v * ab;
			}

			var cp = point - c;
			var d5 = Vector3.Dot(ab, cp);
			var d6 = Vector3.Dot(ac, cp);
			if (d6 >= 0.0f && d5 <= d6) return c;

			var vb = d5 * d2 - d1 * d6;
			if (vb <= 0.0f && d2 >= 0.0f && d6 <= 0.0f)
			{
				var w = d2 / (d2 - d6);
				return a + w * ac;
			}

			var va = d3 * d6 - d5 * d4;
			if (va <= 0.0f && (d4 - d3) >= 0.0f && (d5 - d6) >= 0.0f)
			{
				var w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
				return b + w * (c - b);
			}

			var denom = 1.0f / (va + vb + vc);
			var v2 = vb * denom;
			var w2 = vc * denom;

			return a + ab * v2 + ac * w2;
		}
	}
}