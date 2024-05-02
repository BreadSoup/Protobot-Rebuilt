using System;
using System.Collections.Generic;
using UnityEngine;

namespace Measure
{
	/// <summary>
	/// Settings for the MeasureEditor.
	/// </summary>
	public class EditorSettings : ScriptableObject
	{
		// Keyboard or mouse modifiers to trigger actions.
		public EventModifiers modifier = EventModifiers.Control;

		// Current selection type in the editor.
		public SelectionType selection = SelectionType.Vertex;

		// Colors for various selection and display options.
		public Color ObjectSelectColor = Color.red;
		public Color VertexSelectColor = Color.blue;
		public Color DistanceLineColor = Color.green;
		public Color SizeXColor = Color.red;
		public Color SizeYColor = Color.green;
		public Color SizeZColor = Color.blue;
		public Color TextColor = Color.magenta;

		// Display settings toggles.
		public bool ShowClosestPointDistance = false;
		public bool ShowCentreMassDistance = true;
		public bool ShowSize = true;
		public bool ShowDistanceXYZ = false;

		// Determines the space type for calculations (World or Local).
		public SpaceType WorldType = SpaceType.World;

		// Text and marker scaling.
		public float TextSizeScale = 0.7f;
		public float MarkerDotSizeScale = 0.1f;

		// Determines the interaction type (Collider or Mesh).
		public InteractionType InteractionType = InteractionType.Collider;

		// Determines the line drawing type.
		public LineType LineType = LineType.Arrowheads;

		// Label background color.
		public LabelBackground LabelBgColor = LabelBackground.DarkGrey;

		// Threshold for accurate click detection.
		public float ClickAccuracyThreshold = 10f;

		// Precision level for labels.
		public int LabelPrecision = 2;

		// Show XYZ coordinates for hits.
		public bool ShowHitXYZ;
		public MeasureSequence Sequence = MeasureSequence.All;
		public bool Enabled = true;
	}

	/// <summary>
	/// List of raycast hit data for the editor.
	/// </summary>
	[Serializable]
	public class RaycastHitListData : ScriptableObject
	{
		public List<RaycastHitData> hits = new();
	}

	/// <summary>
	/// Represents raycast hit data in the editor.
	/// </summary>
	public class RaycastHitData : ScriptableObject
	{
		// Cache transform details for optimization.
		public void CacheTransform(Transform transform)
		{
			if (transform == null) return;
			Transform = transform;
			Position = transform.position;
			Rotation = transform.rotation;
			Scale = transform.localScale;
			GameObject = transform.gameObject;
			MeshFilter = transform.GetComponent<MeshFilter>();
		}

		// Closest vertex information.
		public Vector3? ClosestVertex;

		// Cached transform properties.
		public Vector3 Position;
		public Quaternion Rotation;
		public Vector3 Scale;
		public Transform Transform;
		public Collider Collider;
		public Vector3 Point;
		public int TriangleIndex = -1;
		public MeshFilter MeshFilter;
		public GameObject GameObject;

		/// <summary>
		/// Adjusts the original world point to the current transform.
		/// </summary>
		public Vector3 AdjustToWorldUsingCurrentTransform(Vector3 originalWorldPoint, Transform currentTransform)
		{
			// Create a Matrix using the original transform values
			Matrix4x4 originalMatrix = Matrix4x4.TRS(Position, Rotation, Scale);

			// Convert the world point to local space using the inverse of the original transform
			Vector3 localPoint = originalMatrix.inverse.MultiplyPoint3x4(originalWorldPoint);

			// Convert that local point back to world space using the current transform
			return currentTransform.TransformPoint(localPoint);
		}

		// Set data based on a raycast hit.
		public void SetRaycastHit(RaycastHit hitInfo)
		{
			CacheTransform(hitInfo.transform);
			Collider = hitInfo.collider;
			Point = hitInfo.point;
			TriangleIndex = hitInfo.triangleIndex;
		}

		// Retrieve bounds in world space based on interaction type.
		public bool TryGetBoundsInWorld(InteractionType interactionType, out Bounds bounds, out bool isMesh)
		{
			isMesh = false;

			if (interactionType == InteractionType.Collider)
			{
				if (Collider != null)
				{
					bounds = Collider.bounds; // This is already in world space.
					return true;
				}
			}

			if (MeshFilter != null && MeshFilter.sharedMesh != null)
			{
				Vector3 worldCenter = Transform.TransformPoint(MeshFilter.sharedMesh.bounds.center);
				Vector3 worldSize = Vector3.Scale(Transform.lossyScale, MeshFilter.sharedMesh.bounds.size);
				isMesh = true;
				bounds = new Bounds(worldCenter, worldSize);
				return true;
			}

			bounds = new Bounds();
			return false;
		}
	}

	// Various enums to define options/settings in the editor.

	public enum SelectionType
	{
		Object,
		Vertex,
		HitPoint,
	}

	public enum LabelBackground
	{
		Transparent,
		White,
		LightGrey,
		DarkGrey,
	}

	public enum InteractionType
	{
		Collider,
		Mesh,
	}

	public enum SpaceType
	{
		World,
		Local,
	}

	public enum LineType
	{
		Arrowheads,
		Dots,
		NoEndMarker,
	}

	public enum MeasureSequence
	{
		All,
		Linear,
		Pair,
	}

	/// <summary>
	/// Data structure for holding mesh data and checking for changes in transform.
	/// </summary>
	public class MeshData
	{
		public Mesh mesh { get; private set; }
		public Vector3 vert { get; private set; }
		private readonly Vector3 position;
		private readonly Quaternion rotation;
		private readonly Vector3 scale;

		public MeshData(Mesh mesh, Vector3 vert, Transform transform)
		{
			this.mesh = mesh;
			this.vert = vert;
			position = transform.position;
			rotation = transform.rotation;
			scale = transform.localScale;
		}

		// Checks if the transform values have changed.
		public bool HasTransformChanged(Transform transform) =>
			position != transform.position
			|| rotation != transform.rotation
			|| scale != transform.localScale;
	}
}