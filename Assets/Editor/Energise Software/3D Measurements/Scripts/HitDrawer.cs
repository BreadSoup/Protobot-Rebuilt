using System;
using UnityEditor;
using UnityEngine;

namespace Measure
{
	/// <summary>
	/// Responsible for drawing hits on vertices and other hit points in the Unity Editor scene view.
	/// </summary>
	public class HitDrawer
	{
		/// <summary>
		/// Draws a representation of the vertices where hits occurred in the Unity Editor's scene view.
		/// </summary>
		public void DrawVertexHits()
		{
			var currentSceneView = SceneView.lastActiveSceneView;
			var normal = -currentSceneView.camera.transform.forward;
			var fromDirection = Vector3.up;
			var percentage = MeasureEditor.MarkerDotSizeScale * 0.001f;

			foreach (var hit in MeasureEditor.hitListData.hits)
			{
				if (!MeasureEditor.CalculateClosestVertex(hit, out var closestVertex)) continue;

				if (closestVertex == Vector3.positiveInfinity) continue;
				var handleSizeAtCenter = HandleUtility.GetHandleSize(closestVertex);
				handleSizeAtCenter = Mathf.Min(handleSizeAtCenter, 1);
				var radius = percentage * currentSceneView.position.width * handleSizeAtCenter;

				Handles.color = MeasureEditor.VertexSelectColor;
				Handles.DrawSolidArc(closestVertex, normal, fromDirection, 360f, radius);
				if (MeasureEditor.ShowHitXYZ)
				{
					DrawHitLabel(closestVertex, hit, MeasureEditor.WorldType != SpaceType.World);
				}
			}
		}

		/// <summary>
		/// Draws a representation of the points where hits occurred in the Unity Editor's scene view.
		/// </summary>
		public void DrawHitHits()
		{
			var currentSceneView = SceneView.lastActiveSceneView;
			var normal = -currentSceneView.camera.transform.forward;
			var fromDirection = Vector3.up;
			var percentage = MeasureEditor.MarkerDotSizeScale * 0.001f;

			foreach (var hit in MeasureEditor.hitListData.hits)
			{
				var adjustedHitpoint = hit.Point;

				if (hit.Transform != null)
					adjustedHitpoint = hit.AdjustToWorldUsingCurrentTransform(hit.Point, hit.Transform);

				var handleSizeAtCenter1 = HandleUtility.GetHandleSize(adjustedHitpoint);
				handleSizeAtCenter1 = Mathf.Min(handleSizeAtCenter1, 1);
				var radius1 = percentage * currentSceneView.position.width * handleSizeAtCenter1;

				Handles.color = MeasureEditor.VertexSelectColor;
				Handles.DrawSolidArc(adjustedHitpoint, normal, fromDirection, 360f, radius1);
				if (MeasureEditor.ShowHitXYZ)
					DrawHitLabel(adjustedHitpoint, hit, MeasureEditor.WorldType != SpaceType.World);
			}
		}

		/// <summary>
		/// Draws a label at the hit point, displaying the x, y, and z coordinates of the hit.
		/// </summary>
		/// <param name="hitpoint">The point of the hit.</param>
		/// <param name="hit">The RaycastHitData containing data about the hit.</param>
		/// <param name="showMeasurementInLocal">Whether to show measurements in local coordinates.</param>
		private static void DrawHitLabel(Vector3 hitpoint, RaycastHitData hit, bool showMeasurementInLocal = false)
		{
			var currentTransform = hit.Transform;
			if (currentTransform == null) return;

			var bgColor = MeasureEditor.LabelBgColor switch
			{
				LabelBackground.Transparent => Texture2D.blackTexture,
				LabelBackground.White => Texture2D.whiteTexture,
				LabelBackground.LightGrey => Texture2D.linearGrayTexture,
				LabelBackground.DarkGrey => Texture2D.grayTexture,
				_ => throw new ArgumentOutOfRangeException()
			};

			var labelStyle = new GUIStyle
			{
				normal =
				{
					textColor = MeasureEditor.TextColor,
					background = bgColor,
				},
				fontSize = Mathf.FloorToInt(30 * MeasureEditor.TextSizeScale),
				alignment = TextAnchor.UpperCenter,
			};

			Vector3 adjustedHitPoint = hitpoint;

			if (showMeasurementInLocal && currentTransform != null)
				adjustedHitPoint = currentTransform.InverseTransformPoint(hitpoint);

			var xDelta = adjustedHitPoint.x;
			var yDelta = adjustedHitPoint.y;
			var zDelta = adjustedHitPoint.z;
			var precision = MeasureEditor.LabelPrecision;

			string mainText = "";
			string[] subTexts =
			{
				string.Format("X:{0:F" + precision + "}", xDelta),
				string.Format("Y:{0:F" + precision + "}", yDelta),
				string.Format("Z:{0:F" + precision + "}", zDelta)
			};

			Color[] subColors =
			{
				Color.red,
				Color.green,
				Color.blue
			};

			LineBuilder.DrawMultiColorLabel(hitpoint, mainText, MeasureEditor.TextColor, subTexts, subColors,
				labelStyle);
		}
	}
}