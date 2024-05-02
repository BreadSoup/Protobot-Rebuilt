using System;
using UnityEditor;
using UnityEngine;

namespace Measure
{
	/// <summary>
	/// Responsible for building and configuring a line to be rendered in the Unity's SceneView.
	/// Uses the Builder pattern to allow chained configuration of line properties.
	/// </summary>
	public class LineBuilder
	{
		private readonly SceneView currentSceneView;
		private readonly Vector3 center1;
		private readonly Vector3 center2;
		private bool overrideDotsAsLineColor, drawStartMarker, drawEndMarker, drawXYZ, withDots;
		private Color lineColor, labelColor;
		private int thickness = 4;

		/// <summary>
		/// Initializes a new instance of the <see cref="LineBuilder"/> class.
		/// </summary>
		/// <param name="currentSceneView">The current scene view where the line will be rendered.</param>
		/// <param name="center1">The starting point of the line.</param>
		/// <param name="center2">The ending point of the line.</param>
		public LineBuilder(SceneView currentSceneView, Vector3 center1, Vector3 center2)
		{
			this.currentSceneView = currentSceneView;
			this.center1 = center1;
			this.center2 = center2;
		}

		/// <summary>
		/// Overrides dots to use the specified line color.
		/// </summary>
		/// <returns>The current <see cref="LineBuilder"/> instance.</returns>
		public LineBuilder OverrideDotsAsLineColor()
		{
			overrideDotsAsLineColor = true;
			return this;
		}

		/// <summary>
		/// Enables drawing a marker at the start of the line.
		/// </summary>
		/// <returns>The current <see cref="LineBuilder"/> instance.</returns>
		public LineBuilder WithStartMarker()
		{
			drawStartMarker = true;
			return this;
		}

		/// <summary>
		/// Enables drawing a marker at the end of the line.
		/// </summary>
		/// <returns>The current <see cref="LineBuilder"/> instance.</returns>
		public LineBuilder WithEndMarker()
		{
			drawEndMarker = true;
			return this;
		}

		/// <summary>
		/// Enables drawing XYZ coordinates along the line.
		/// </summary>
		/// <returns>The current <see cref="LineBuilder"/> instance.</returns>
		public LineBuilder WithXYZ()
		{
			drawXYZ = true;
			return this;
		}

		/// <summary>
		/// Sets the color of the line.
		/// </summary>
		/// <param name="color">The desired line color.</param>
		/// <returns>The current <see cref="LineBuilder"/> instance.</returns>
		public LineBuilder WithLineColor(Color color)
		{
			lineColor = color;
			return this;
		}

		/// <summary>
		/// Sets the color of the label, if any.
		/// </summary>
		/// <param name="color">The desired label color.</param>
		/// <returns>The current <see cref="LineBuilder"/> instance.</returns>
		public LineBuilder WithLabelColor(Color color)
		{
			labelColor = color;
			return this;
		}

		/// <summary>
		/// Enables drawing of dots along the line.
		/// </summary>
		/// <returns>The current <see cref="LineBuilder"/> instance.</returns>
		public LineBuilder WithDots()
		{
			withDots = true;
			return this;
		}

		/// <summary>
		/// Sets the thickness of the line.
		/// </summary>
		/// <param name="val">Desired thickness value. If less than 1, it defaults to 1.</param>
		/// <returns>The current <see cref="LineBuilder"/> instance.</returns>
		public LineBuilder WithThickness(int val)
		{
			if (val < 1) val = 1;
			thickness = val;
			return this;
		}

		/// <summary>
		/// Renders the configured line in the Unity SceneView.
		/// </summary>
		public void Draw()
		{
			// Calculate the midpoint between the start and end points
			var midpoint = (center1 + center2) / 2.0f;

			// Compute the distance between the start and end points
			var distance = Vector3.Distance(center1, center2);
			Handles.color = lineColor;

			// Draw the line either with dots or as a solid line based on configuration
			if (withDots) Handles.DrawDottedLine(center1, center2, thickness);
			else Handles.DrawLine(center1, center2);

			// Determine the background color for the label based on the MeasureEditor settings
			var bgColor = MeasureEditor.LabelBgColor switch
			{
				LabelBackground.Transparent => Texture2D.blackTexture,
				LabelBackground.White => Texture2D.whiteTexture,
				LabelBackground.LightGrey => Texture2D.linearGrayTexture,
				LabelBackground.DarkGrey => Texture2D.grayTexture,
				_ => throw new ArgumentOutOfRangeException()
			};

			// Configure the style for the distance label
			var labelStyle = new GUIStyle
			{
				normal =
				{
					textColor = labelColor,
					background = bgColor,
				},
				fontSize = Mathf.FloorToInt(30 * MeasureEditor.TextSizeScale),
				alignment = TextAnchor.UpperCenter,
			};

			// Draw the distance label if configured
			if (labelStyle.fontSize > 0)
			{
				DrawLabel(midpoint, labelStyle, distance, drawXYZ);
			}

			// Configuration for the start and end markers
			var normal = -currentSceneView.camera.transform.forward;
			var fromDirection = Vector3.up;

			// Compute the handle sizes for the start and end markers
			var handleSizeAtCenter1 = HandleUtility.GetHandleSize(center1);
			var handleSizeAtCenter2 = HandleUtility.GetHandleSize(center2);

			// Ensure the arrow markers don't exceed 25% of the line length
			var maxArrowSize = 0.25f * distance;
			handleSizeAtCenter1 = Mathf.Min(handleSizeAtCenter1, maxArrowSize);
			handleSizeAtCenter2 = Mathf.Min(handleSizeAtCenter2, maxArrowSize);

			// Scale the marker dot sizes based on MeasureEditor settings
			var percentage = MeasureEditor.MarkerDotSizeScale * 0.001f;
			var radius1 = percentage * currentSceneView.position.width * handleSizeAtCenter1;
			var radius2 = percentage * currentSceneView.position.width * handleSizeAtCenter2;

			// Draw the configured markers
			Handles.color = MeasureEditor.VertexSelectColor;
			switch (MeasureEditor.LineType)
			{
				case LineType.Arrowheads:
					// Arrowhead drawing logic
					DrawArrowHeads(handleSizeAtCenter1, handleSizeAtCenter2);

					break;
				case LineType.Dots:
					DrawDotEndMarkers(normal, fromDirection, radius1, radius2);
					break;
				case LineType.NoEndMarker:
					// No markers to draw
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Draws solid dot markers at the endpoints of the line in the SceneView.
		/// </summary>
		/// <param name="normal">The normal direction to use when drawing the arc.</param>
		/// <param name="fromDirection">The initial direction from which to start drawing the arc.</param>
		/// <param name="radius1">The radius of the start marker dot.</param>
		/// <param name="radius2">The radius of the end marker dot.</param>
		private void DrawDotEndMarkers(Vector3 normal, Vector3 fromDirection, float radius1, float radius2)
		{
			// Determine the color to use for the dot markers
			Handles.color = overrideDotsAsLineColor ? lineColor : MeasureEditor.VertexSelectColor;

			// Draw the start marker dot if configured to do so
			if (drawStartMarker) Handles.DrawSolidArc(center1, normal, fromDirection, 360f, radius1);

			// Draw the end marker dot if configured to do so
			if (drawEndMarker) Handles.DrawSolidArc(center2, normal, fromDirection, 360f, radius2);
		}

		/// <summary>
		/// Draws arrowhead markers at the endpoints of the line in the SceneView.
		/// </summary>
		/// <param name="handleSizeAtCenter1">The size of the arrowhead at the start point.</param>
		/// <param name="handleSizeAtCenter2">The size of the arrowhead at the end point.</param>
		private void DrawArrowHeads(float handleSizeAtCenter1, float handleSizeAtCenter2)
		{
			// Set the color for the arrowheads
			Handles.color = lineColor;

			// Determine the direction between the start and end points
			var directionFromCenter1ToCenter2 = (center2 - center1).normalized;

			// Calculate the start positions of the arrowheads
			var arrowStartPos1 = center1 + directionFromCenter1ToCenter2 * handleSizeAtCenter1;
			var arrowStartPos2 = center2 - directionFromCenter1ToCenter2 * handleSizeAtCenter2;

			// Determine the directions for the arrowheads
			var directionForStartArrow = -directionFromCenter1ToCenter2;
			var directionForEndArrow = directionFromCenter1ToCenter2;

			var defaultDirection = Vector3.up;

			// Ensure we have a valid direction for both arrowheads
			if (directionForStartArrow.magnitude < 1e-5) directionForStartArrow = defaultDirection;
			if (directionForEndArrow.magnitude < 1e-5) directionForEndArrow = defaultDirection;

			// Draw the start arrowhead if configured to do so
			if (drawStartMarker)
				Handles.ArrowHandleCap(0, arrowStartPos1,
					Quaternion.LookRotation(directionForStartArrow),
					handleSizeAtCenter1, EventType.Repaint);

			// Draw the end arrowhead if configured to do so
			if (drawEndMarker)
				Handles.ArrowHandleCap(0, arrowStartPos2,
					Quaternion.LookRotation(directionForEndArrow),
					handleSizeAtCenter2, EventType.Repaint);
		}

		/// <summary>
		/// Draws a multi-color label in the SceneView. 
		/// </summary>
		/// <param name="position">Position to place the label.</param>
		/// <param name="mainText">Primary text to display.</param>
		/// <param name="mainColor">Color of the primary text.</param>
		/// <param name="subTexts">Additional lines of text.</param>
		/// <param name="subColors">Colors for the additional lines of text.</param>
		/// <param name="style">Styling for the label.</param>
		public static void DrawMultiColorLabel(Vector3 position, string mainText, Color mainColor, string[] subTexts,
			Color[] subColors, GUIStyle style)
		{
			float yOffset = style.lineHeight * 1.01f;
			Vector2 guiPosition = HandleUtility.WorldToGUIPoint(position);

			GUIStyle noBackgroundStyle = new GUIStyle(style);

			if (!string.IsNullOrEmpty(mainText))
			{
				noBackgroundStyle.normal.textColor = mainColor;
				Vector3 worldPositionForMain = HandleUtility.GUIPointToWorldRay(guiPosition).GetPoint(10);
				Handles.Label(worldPositionForMain, mainText, noBackgroundStyle);
				guiPosition.y += yOffset;
			}

			for (int i = 0; i < subTexts.Length; i++)
			{
				noBackgroundStyle.normal.textColor = subColors[i];
				Vector3 worldPositionForSub = HandleUtility.GUIPointToWorldRay(guiPosition).GetPoint(10);
				Handles.Label(worldPositionForSub, subTexts[i], noBackgroundStyle);
				guiPosition.y += yOffset;
			}
		}

		/// <summary>
		/// Draws the distance label based on the provided configuration.
		/// </summary>
		/// <param name="midpoint">Midpoint of the line where the label will be placed.</param>
		/// <param name="labelStyle">Styling for the label.</param>
		/// <param name="distance">Distance value to be shown.</param>
		/// <param name="showXYZ">Flag to indicate if the XYZ values should also be shown.</param>
		private void DrawLabel(Vector3 midpoint, GUIStyle labelStyle, float distance, bool showXYZ)
		{
			if (MeasureEditor.ShowDistanceXYZ && showXYZ)
			{
				var xDelta = Mathf.Abs(center1.x - center2.x);
				var yDelta = Mathf.Abs(center1.y - center2.y);
				var zDelta = Mathf.Abs(center1.z - center2.z);
				var precision = MeasureEditor.LabelPrecision;

				string mainText = string.Format("{0:F" + precision + "}", distance);
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

				DrawMultiColorLabel(midpoint, mainText, labelColor, subTexts, subColors, labelStyle);
			}
			else
			{
				Handles.Label(midpoint, distance.ToString($"F{MeasureEditor.LabelPrecision}"), labelStyle);
			}
		}
	}
}