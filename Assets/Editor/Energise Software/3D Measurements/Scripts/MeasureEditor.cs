using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Measure
{
	public class MeasureEditor : EditorWindow
	{
		#region Props

		private static SerializedProperty modifierProp;
		private static SerializedProperty selectionProp;
		private static SerializedProperty objectSelectColorProp;
		private static SerializedProperty vertexSelectColorProp;
		private static SerializedProperty distanceLineColorProp;
		private static SerializedProperty sizeXColorProp;
		private static SerializedProperty sizeYColorProp;
		private static SerializedProperty sizeZColorProp;
		private static SerializedProperty textColorProp;
		private static SerializedProperty showClosestPointDistanceProp;
		private static SerializedProperty showCentreMassDistanceProp;
		private static SerializedProperty showSizeProp;
		private static SerializedProperty showDistanceXYZProp;
		private static SerializedProperty worldTypeProp;
		private static SerializedProperty textSizeScaleProp;
		private static SerializedProperty markerDotSizeScaleProp;
		private static SerializedProperty interactionTypeProp;
		private static SerializedProperty lineTypeProp;
		private static SerializedProperty labelBgColorProp;
		private static SerializedProperty clickAccuracyThresholdProp;
		private static SerializedProperty labelPrecisionProp;
		private static SerializedProperty showHitXYZProp;
		private static SerializedProperty sequenceProp;
		private static SerializedProperty enabledProp;

		public static EventModifiers Modifier => (EventModifiers) modifierProp.intValue;
		public static SelectionType Selection => (SelectionType) selectionProp.enumValueIndex;
		public static Color ObjectSelectColor => objectSelectColorProp.colorValue;
		public static Color VertexSelectColor => vertexSelectColorProp.colorValue;
		public static Color DistanceLineColor => distanceLineColorProp.colorValue;
		public static Color SizeXColor => sizeXColorProp.colorValue;
		public static Color SizeYColor => sizeYColorProp.colorValue;
		public static Color SizeZColor => sizeZColorProp.colorValue;
		public static Color TextColor => textColorProp.colorValue;
		public static bool ShowClosestPointDistance => showClosestPointDistanceProp.boolValue;
		public static bool ShowCentreMassDistance => showCentreMassDistanceProp.boolValue;
		public static bool ShowSize => showSizeProp.boolValue;
		public static bool ShowDistanceXYZ => showDistanceXYZProp.boolValue;
		public static SpaceType WorldType => (SpaceType) worldTypeProp.enumValueIndex;
		public static float TextSizeScale => textSizeScaleProp.floatValue;
		public static float MarkerDotSizeScale => markerDotSizeScaleProp.floatValue;
		public static InteractionType InteractionType => (InteractionType) interactionTypeProp.enumValueIndex;
		public static LineType LineType => (LineType) lineTypeProp.enumValueIndex;
		public static LabelBackground LabelBgColor => (LabelBackground) labelBgColorProp.enumValueIndex;
		public static float ClickAccuracyThreshold => clickAccuracyThresholdProp.floatValue;
		public static int LabelPrecision => labelPrecisionProp.intValue;
		public static bool ShowHitXYZ => showHitXYZProp.boolValue;
		public static bool Enabled => enabledProp.boolValue;

		public static MeasureSequence Sequence => (MeasureSequence) sequenceProp.enumValueIndex;

		private static Dictionary<RaycastHitData, MeshData> closestVertexCache = new();

		private static MeasureEditorRendering rendering;
		private static Stage lastStage;
		private static EditorSettings settings;
		private static SerializedObject serializedSettings;
		public static RaycastHitListData hitListData { get; private set; }
		private static SerializedObject serializedHitListData;
		private static SerializedProperty hitListDataProp;

		private bool settingsFoldout;
		private Vector2 scrollPosition;

		#endregion

		[MenuItem("Window/3D Measure &%m")]
		public static void ShowWindow() => GetWindow<MeasureEditor>("Measure Editor");

		private void OnEnable() => Setup();

		/// <summary>
		/// Sets up the editor, including cleaning previous state.
		/// </summary>
		private void Setup()
		{
			//Clear previous data
			Clear();
			DestroyImmediate(settings);
			DestroyImmediate(hitListData);

			//create objects
			rendering = new MeasureEditorRendering();
			settings = CreateInstance<EditorSettings>();
			serializedSettings = new SerializedObject(settings);
			hitListData = CreateInstance<RaycastHitListData>();
			serializedHitListData = new SerializedObject(hitListData);

			//Manage events
			SceneView.beforeSceneGui -= BeforeScene;
			SceneView.beforeSceneGui += BeforeScene;
			Undo.undoRedoPerformed -= OnUndoRedoPerformed;
			Undo.undoRedoPerformed += OnUndoRedoPerformed;
			EditorApplication.playModeStateChanged += PlayModeChanged;
			EditorApplication.playModeStateChanged -= PlayModeChanged;
			EditorApplication.hierarchyChanged -= HierarchyChanged;
			EditorApplication.hierarchyChanged += HierarchyChanged;

			CreateProperties();
		}

		private void OnUndoRedoPerformed() => Repaint();

		/// <summary>
		/// Create the properties from the EditorSettings within the serializedObjet
		/// </summary>
		private static void CreateProperties()
		{
			modifierProp = serializedSettings.FindProperty("modifier");
			selectionProp = serializedSettings.FindProperty("selection");
			objectSelectColorProp = serializedSettings.FindProperty("ObjectSelectColor");
			vertexSelectColorProp = serializedSettings.FindProperty("VertexSelectColor");
			distanceLineColorProp = serializedSettings.FindProperty("DistanceLineColor");
			sizeXColorProp = serializedSettings.FindProperty("SizeXColor");
			sizeYColorProp = serializedSettings.FindProperty("SizeYColor");
			sizeZColorProp = serializedSettings.FindProperty("SizeZColor");
			textColorProp = serializedSettings.FindProperty("TextColor");
			showClosestPointDistanceProp = serializedSettings.FindProperty("ShowClosestPointDistance");
			showCentreMassDistanceProp = serializedSettings.FindProperty("ShowCentreMassDistance");
			showSizeProp = serializedSettings.FindProperty("ShowSize");
			showDistanceXYZProp = serializedSettings.FindProperty("ShowDistanceXYZ");
			worldTypeProp = serializedSettings.FindProperty("WorldType");
			textSizeScaleProp = serializedSettings.FindProperty("TextSizeScale");
			markerDotSizeScaleProp = serializedSettings.FindProperty("MarkerDotSizeScale");
			interactionTypeProp = serializedSettings.FindProperty("InteractionType");
			lineTypeProp = serializedSettings.FindProperty("LineType");
			labelBgColorProp = serializedSettings.FindProperty("LabelBgColor");
			hitListDataProp = serializedHitListData.FindProperty("hits");
			clickAccuracyThresholdProp = serializedSettings.FindProperty("ClickAccuracyThreshold");
			labelPrecisionProp = serializedSettings.FindProperty("LabelPrecision");
			showHitXYZProp = serializedSettings.FindProperty("ShowHitXYZ");
			sequenceProp = serializedSettings.FindProperty("Sequence");
			enabledProp = serializedSettings.FindProperty("Enabled");

			LoadPropertiesFromPrefs();
		}

		/// <summary>
		/// Load properties from editorprefs
		/// </summary>
		private static void LoadPropertiesFromPrefs()
		{
			if (EditorPrefs.HasKey("modifier")) modifierProp.intValue = EditorPrefs.GetInt("modifier");
			if (EditorPrefs.HasKey("selection")) selectionProp.enumValueIndex = EditorPrefs.GetInt("selection");

			if (EditorPrefs.HasKey("ObjectSelectColor"))
				objectSelectColorProp.colorValue = StringToColor(EditorPrefs.GetString("ObjectSelectColor"));
			if (EditorPrefs.HasKey("VertexSelectColor"))
				vertexSelectColorProp.colorValue = StringToColor(EditorPrefs.GetString("VertexSelectColor"));
			if (EditorPrefs.HasKey("DistanceLineColor"))
				distanceLineColorProp.colorValue = StringToColor(EditorPrefs.GetString("DistanceLineColor"));
			if (EditorPrefs.HasKey("SizeXColor"))
				sizeXColorProp.colorValue = StringToColor(EditorPrefs.GetString("SizeXColor"));
			if (EditorPrefs.HasKey("SizeYColor"))
				sizeYColorProp.colorValue = StringToColor(EditorPrefs.GetString("SizeYColor"));
			if (EditorPrefs.HasKey("SizeZColor"))
				sizeZColorProp.colorValue = StringToColor(EditorPrefs.GetString("SizeZColor"));
			if (EditorPrefs.HasKey("TextColor"))
				textColorProp.colorValue = StringToColor(EditorPrefs.GetString("TextColor"));
			if (EditorPrefs.HasKey("ShowClosestPointDistance"))
				showClosestPointDistanceProp.boolValue = EditorPrefs.GetBool("ShowClosestPointDistance");
			if (EditorPrefs.HasKey("ShowCentreMassDistance"))
				showCentreMassDistanceProp.boolValue = EditorPrefs.GetBool("ShowCentreMassDistance");
			if (EditorPrefs.HasKey("ShowSize")) showSizeProp.boolValue = EditorPrefs.GetBool("ShowSize");
			if (EditorPrefs.HasKey("ShowDistanceXYZ"))
				showDistanceXYZProp.boolValue = EditorPrefs.GetBool("ShowDistanceXYZ");
			if (EditorPrefs.HasKey("WorldType")) worldTypeProp.enumValueIndex = EditorPrefs.GetInt("WorldType");
			if (EditorPrefs.HasKey("TextSizeScale"))
				textSizeScaleProp.floatValue = EditorPrefs.GetFloat("TextSizeScale");
			if (EditorPrefs.HasKey("MarkerDotSizeScale"))
				markerDotSizeScaleProp.floatValue = EditorPrefs.GetFloat("MarkerDotSizeScale");
			if (EditorPrefs.HasKey("InteractionType"))
				interactionTypeProp.enumValueIndex = EditorPrefs.GetInt("InteractionType");
			if (EditorPrefs.HasKey("LineType")) lineTypeProp.enumValueIndex = EditorPrefs.GetInt("LineType");
			if (EditorPrefs.HasKey("LabelBgColor"))
				labelBgColorProp.enumValueIndex = EditorPrefs.GetInt("LabelBgColor");
			if (EditorPrefs.HasKey("ClickAccuracyThreshold"))
				clickAccuracyThresholdProp.floatValue = EditorPrefs.GetFloat("ClickAccuracyThreshold");
			if (EditorPrefs.HasKey("LabelPrecision"))
				labelPrecisionProp.intValue = EditorPrefs.GetInt("LabelPrecision");
			if (EditorPrefs.HasKey("ShowHitXYZ"))
				showHitXYZProp.boolValue = EditorPrefs.GetBool("ShowHitXYZ");
			if (EditorPrefs.HasKey("Sequence"))
				sequenceProp.enumValueIndex = EditorPrefs.GetInt("Sequence");
			if (EditorPrefs.HasKey("Enabled"))
				enabledProp.boolValue = EditorPrefs.GetBool("Enabled");
			serializedSettings.ApplyModifiedProperties();
		}

		/// <summary>
		/// Save properties to editorprefs
		/// </summary>
		private static void SavePropertiesToPrefs()
		{
			EditorPrefs.SetInt("modifier", modifierProp.intValue);
			EditorPrefs.SetInt("selection", selectionProp.enumValueIndex);
			EditorPrefs.SetString("ObjectSelectColor", ColorToString(objectSelectColorProp.colorValue));
			EditorPrefs.SetString("VertexSelectColor", ColorToString(vertexSelectColorProp.colorValue));
			EditorPrefs.SetString("DistanceLineColor", ColorToString(distanceLineColorProp.colorValue));
			EditorPrefs.SetString("SizeXColor", ColorToString(sizeXColorProp.colorValue));
			EditorPrefs.SetString("SizeYColor", ColorToString(sizeYColorProp.colorValue));
			EditorPrefs.SetString("SizeZColor", ColorToString(sizeZColorProp.colorValue));
			EditorPrefs.SetString("TextColor", ColorToString(textColorProp.colorValue));
			EditorPrefs.SetBool("ShowClosestPointDistance", showClosestPointDistanceProp.boolValue);
			EditorPrefs.SetBool("ShowCentreMassDistance", showCentreMassDistanceProp.boolValue);
			EditorPrefs.SetBool("ShowSize", showSizeProp.boolValue);
			EditorPrefs.SetBool("ShowDistanceXYZ", showDistanceXYZProp.boolValue);
			EditorPrefs.SetInt("WorldType", worldTypeProp.enumValueIndex);
			EditorPrefs.SetFloat("TextSizeScale", textSizeScaleProp.floatValue);
			EditorPrefs.SetFloat("MarkerDotSizeScale", markerDotSizeScaleProp.floatValue);
			EditorPrefs.SetInt("InteractionType", interactionTypeProp.enumValueIndex);
			EditorPrefs.SetInt("LineType", lineTypeProp.enumValueIndex);
			EditorPrefs.SetInt("LabelBgColor", labelBgColorProp.enumValueIndex);
			EditorPrefs.SetFloat("ClickAccuracyThreshold", clickAccuracyThresholdProp.floatValue);
			EditorPrefs.SetInt("LabelPrecision", labelPrecisionProp.intValue);
			EditorPrefs.SetBool("ShowHitXYZ", showHitXYZProp.boolValue);
			EditorPrefs.SetInt("Sequence", sequenceProp.enumValueIndex);
			EditorPrefs.SetBool("Enabled", enabledProp.boolValue);
		}

		#region CacheConverts

		private static string ColorToString(Color color) => $"{color.r},{color.g},{color.b},{color.a}";

		private static Color StringToColor(string colorString)
		{
			var values = colorString.Split(',');
			return new Color(float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2]),
				float.Parse(values[3]));
		}

		#endregion

		/// <summary>
		/// Performs necessary clean-up when the editor window or component is disabled. This includes unsubscribing from various events.
		/// </summary>
		private void OnDisable()
		{
			SavePropertiesToPrefs();
			SceneView.beforeSceneGui -= BeforeScene;
			Undo.undoRedoPerformed -= OnUndoRedoPerformed;
			EditorApplication.playModeStateChanged -= PlayModeChanged;
			EditorApplication.hierarchyChanged -= HierarchyChanged;
		}

		/// <summary>
		/// Handles mouse input prior to rendering the scene view. This method is often attached as a callback to the SceneView's rendering event.
		/// </summary>
		/// <param name="obj">The current scene view.</param>
		private static void BeforeScene(SceneView obj) => HandleMouseInput();

		/// <summary>
		/// Resets and sets up necessary configurations when entering the edit mode in the editor.
		/// </summary>
		/// <param name="state">The play mode state change event arguments.</param>
		private void PlayModeChanged(PlayModeStateChange state)
		{
			if (state == PlayModeStateChange.EnteredEditMode)
			{
				Setup();
			}
		}

		/// <summary>
		/// Handles the GUI rendering for the editor window.
		/// </summary>
		private void OnGUI()
		{
			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

			OnGuiSetup(out var boldLabelStyle, out var infoStyle);
			EditorGUILayout.PropertyField(enabledProp, new GUIContent("Enabled:"));

			EditorGUILayout.PropertyField(selectionProp, new GUIContent("Selection Type:"));

			EditorGUILayout.Separator();
			EditorGUILayout.LabelField("Distance Markers:", boldLabelStyle);
			EditorGUILayout.Separator();

			EditorGUILayout.PropertyField(showDistanceXYZProp, new GUIContent("Show Distance as XYZ:"));
			EditorGUILayout.PropertyField(showClosestPointDistanceProp, new GUIContent("Closest Point Distance:"));

			RenderSelectionTypeOptions();

			EditorGUILayout.PropertyField(worldTypeProp, new GUIContent("Space:", "The 3d space to use"));
			EditorGUILayout.PropertyField(sequenceProp,
				new GUIContent("Measure Sequence:", "How to process measuring objects"));

			EditorGUILayout.Separator();

			if (Selection == SelectionType.Object)
			{
				EditorGUILayout.LabelField("Size:", boldLabelStyle);
				EditorGUILayout.Separator();
				EditorGUILayout.PropertyField(showSizeProp, new GUIContent("Show Size:"));
				EditorGUILayout.Separator();
			}

			RenderActions(boldLabelStyle);
			settingsFoldout = EditorGUILayout.Foldout(settingsFoldout, "Settings");
			if (settingsFoldout)
			{
				RenderSettings(boldLabelStyle);
			}

			RenderInfo(infoStyle);

			serializedSettings?.ApplyModifiedProperties();
			serializedHitListData?.ApplyModifiedProperties();
			EditorGUILayout.EndScrollView();
		}

		/// <summary>
		/// Renders the selection type options for the GUI based on the current selection type.
		/// </summary>
		private static void RenderSelectionTypeOptions()
		{
			switch (selectionProp.enumValueIndex)
			{
				case (int) SelectionType.Vertex:
					EditorGUILayout.PropertyField(showHitXYZProp, new GUIContent("Show Hit/Vertex XYZ:"));

					break;
				case (int) SelectionType.Object:
					EditorGUILayout.PropertyField(showCentreMassDistanceProp, new GUIContent("Centre Distance:"));
					EditorGUILayout.PropertyField(interactionTypeProp,
						new GUIContent("Component Type Preference:",
							"The underlying data to use. This is a preference, so if collider is selected and there is not one available, it will try to use the mesh"));

					break;
				case (int) SelectionType.HitPoint:
					EditorGUILayout.PropertyField(showHitXYZProp, new GUIContent("Show Hit/Vertex XYZ:"));

					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Renders action buttons such as 'Clear' and 'Invalidate Caches' for the GUI.
		/// </summary>
		/// <param name="boldLabelStyle">The GUI style for bold labels.</param>
		private static void RenderActions(GUIStyle boldLabelStyle)
		{
			EditorGUILayout.LabelField("Actions:", boldLabelStyle);
			EditorGUILayout.Separator();

			if (GUILayout.Button("Clear (Shift C)")) Clear();
			if (GUILayout.Button("Invalidate Caches")) ClearCache();
			EditorGUILayout.Separator();
		}

		/// <summary>
		/// Sets up styles and other necessary configurations for the OnGUI rendering.
		/// Ensures proper setup when stage changes or when serialized properties are null.
		/// </summary>
		/// <param name="boldLabelStyle">Output parameter for the bold label style.</param>
		/// <param name="infoStyle">Output parameter for the information label style.</param>
		private void OnGuiSetup(out GUIStyle boldLabelStyle, out GUIStyle infoStyle)
		{
			var currentStage = StageUtility.GetCurrentStage();
			if (currentStage != lastStage)
			{
				lastStage = currentStage;
				Setup();
			}

			if (serializedHitListData == null || serializedHitListData.targetObject == null ||
			    serializedSettings == null || serializedSettings.targetObject == null)
			{
				Setup();
			}

			serializedHitListData?.Update();
			serializedSettings?.Update();

			boldLabelStyle = new GUIStyle(EditorStyles.label)
			{
				fontStyle = FontStyle.Bold
			};
			infoStyle = new GUIStyle(EditorStyles.label)
			{
				alignment = TextAnchor.MiddleCenter,
				fontStyle = FontStyle.Bold,
				wordWrap = true
			};
		}

		/// <summary>
		/// Renders information related to the tool, including tool description, usage, and contact information.
		/// </summary>
		/// <param name="infoStyle">The GUI style for displaying the information.</param>
		private static void RenderInfo(GUIStyle infoStyle)
		{
			EditorGUILayout.Separator();
			EditorGUILayout.Separator();

			EditorGUILayout.LabelField("3D Measurements", infoStyle);
			infoStyle.fontStyle = FontStyle.Normal;
			EditorGUILayout.LabelField(
				"Thanks for using our 3D Measurements.\n In the settings you can specify the key modifier. Simply click with the modifier to select. Re-clicking removes the item. Shift+C, or the button on this window can be used to clear all objects",
				infoStyle);
			EditorGUILayout.Separator();

			EditorGUILayout.LabelField(
				"There is options to change between selecting objects, the closest vertex or the actual hit point. You can choose to show the size of the selected object, as well as the XYZ detail",
				infoStyle);

			EditorGUILayout.Separator();

			EditorGUILayout.LabelField(
				"Any comments, requests, feedback, please email: EnergiseTools@gmail.com\n\n Thanks, Stuart Heath (Energise Software)",
				infoStyle);
		}

		/// <summary>
		/// Renders the settings-related controls for the GUI, including options for keys, layers, colors, and graphics.
		/// </summary>
		/// <param name="boldLabelStyle">The GUI style for bold labels.</param>
		private static void RenderSettings(GUIStyle boldLabelStyle)
		{
			EditorGUILayout.Separator();

			EditorGUILayout.PropertyField(modifierProp, new GUIContent("Modifier Key:"));
			EditorGUILayout.Separator();
			EditorGUILayout.LabelField("Colors:", boldLabelStyle);
			EditorGUILayout.Separator();

			EditorGUILayout.PropertyField(objectSelectColorProp, new GUIContent("Object Selection Color:"));
			EditorGUILayout.PropertyField(vertexSelectColorProp, new GUIContent("Vertex Selection Color:"));
			EditorGUILayout.PropertyField(distanceLineColorProp, new GUIContent("Distance Line Color:"));
			EditorGUILayout.PropertyField(textColorProp, new GUIContent("Label Color:"));
			EditorGUILayout.PropertyField(labelBgColorProp, new GUIContent("Label Background Color:"));
			EditorGUILayout.PropertyField(sizeXColorProp, new GUIContent("Size X Color:"));
			EditorGUILayout.PropertyField(sizeYColorProp, new GUIContent("Size Y Color:"));
			EditorGUILayout.PropertyField(sizeZColorProp, new GUIContent("Size Z Color:"));

			EditorGUILayout.Separator();
			EditorGUILayout.LabelField("Graphics:", boldLabelStyle);
			EditorGUILayout.Separator();

			EditorGUILayout.PropertyField(lineTypeProp, new GUIContent("Line Type:"));
			EditorGUILayout.Slider(textSizeScaleProp, 0f, 1f, new GUIContent("Text Size Scale:"));
			EditorGUILayout.Slider(markerDotSizeScaleProp, 0f, 1f, new GUIContent("Marker Dot Size Scale:"));
			EditorGUILayout.Slider(clickAccuracyThresholdProp, 1f, 20f,
				new GUIContent("Click Accuracy Threshold:",
					"The threshold for determining if a click on top of an existing click is sufficiently close to count as the same location"));
			EditorGUILayout.IntSlider(labelPrecisionProp, 1, 5,
				new GUIContent("Label Float Precision:"));
		}

		/// <summary>
		/// Handles mouse inputs within the editor. Allows for selecting objects and vertices with a raycast.
		/// </summary>
		private static void HandleMouseInput()
		{
			if (!Enabled) return;
			var e = Event.current;

			// Check for a key press of 'Shift + C' to clear selections
			if (e.type == EventType.KeyDown && e.keyCode == KeyCode.C && e.modifiers == EventModifiers.Shift) Clear();

			// Check for a mouse click with specific modifiers for casting ray
			if (e.type != EventType.MouseDown || e.button != 0 || e.modifiers != Modifier) return;

			CastRayFromEditor(e.mousePosition);

			// Prevent further propagation of the event and set current tool to None
			e.Use();
			Tools.current = Tool.None;
		}

		/// <summary>
		/// Casts a ray from the editor view based on the given mouse position.
		/// If it hits a GameObject, it checks for a vertex hit or simply selects the object.
		/// </summary>
		/// <param name="mousePosition">Position of the mouse cursor in screen coordinates.</param>
		private static void CastRayFromEditor(Vector2 mousePosition)
		{
			var pickedObject = HandleUtility.PickGameObject(mousePosition, false);

			if (pickedObject != null)
			{
				var h = CheckForVertexHit(pickedObject, HandleUtility.GUIPointToWorldRay(Event.current.mousePosition));
				if (h != null) OnObjectSelect(h);

				return;
			}

			// Cast a physics ray into the world to detect any colliders
			if (!Physics.Raycast(HandleUtility.GUIPointToWorldRay(mousePosition), out var hit, float.MaxValue)) return;
			var rchd = CreateInstance<RaycastHitData>();
			rchd.SetRaycastHit(hit);

			OnObjectSelect(CreateInstance<RaycastHitData>());
		}

		/// <summary>
		/// Checks the mesh of the object to the hit point
		/// </summary>
		private static RaycastHitData CheckForVertexHit(GameObject selectedObject, Ray ray)
		{
			// Initialize the closest intersection distance to a large value.
			float closestIntersection = float.MaxValue;

			// Create a placeholder for the position of the newly detected vertex.
			Vector3 closestVert = Vector3.zero;
			Vector3 hitPosition = Vector3.zero;
			Transform hitTransform = null;

			// A flag to check if a vertex was found in the ray's path.
			var meshFilters = selectedObject.GetComponentsInChildren<MeshFilter>();
			foreach (var meshFilter in meshFilters)
			{
				var mesh = meshFilter.sharedMesh;
				var vertices = mesh.vertices;
				var triangles = mesh.triangles;

				// Iterate over the triangles, checking every 3 vertices (since triangles have 3 points).
				for (var i = 0; i < triangles.Length; i += 3)
				{
					// Convert the local space vertices of the triangle to world space.
					Vector3 v0 = meshFilter.transform.TransformPoint(vertices[triangles[i]]);
					Vector3 v1 = meshFilter.transform.TransformPoint(vertices[triangles[i + 1]]);
					Vector3 v2 = meshFilter.transform.TransformPoint(vertices[triangles[i + 2]]);

					// Check if the given ray intersects with the current triangle.
					// If not, continue to the next triangle.
					if (!RayIntersectsTriangle(ray, v0, v1, v2, out var intersection)) continue;

					// Calculate the distance from the ray's origin to the intersection point.
					float intersectionDistance = Vector3.Distance(ray.origin, intersection);

					// If the current intersection is closer than any previous intersection.
					if (intersectionDistance < closestIntersection)
					{
						closestIntersection = intersectionDistance;
						hitPosition = intersection;
						// Determine the vertex of the intersected triangle that is closest to the intersection point.
						Vector3 localIntersection = meshFilter.transform.InverseTransformPoint(intersection);
						float d0 = Vector3.Distance(localIntersection, vertices[triangles[i]]);
						float d1 = Vector3.Distance(localIntersection, vertices[triangles[i + 1]]);
						float d2 = Vector3.Distance(localIntersection, vertices[triangles[i + 2]]);

						// Assign the closest vertex to newVertexPosition.
						if (d0 < d1 && d0 < d2) closestVert = vertices[triangles[i]];
						else if (d1 < d0 && d1 < d2) closestVert = vertices[triangles[i + 1]];
						else closestVert = vertices[triangles[i + 2]];

						// Convert the new vertex position to world space.
						closestVert = meshFilter.transform.TransformPoint(closestVert);
						hitTransform = meshFilter.transform;
					}
				}
			}

			if (hitTransform == null) return null;
			var raycastHitData = CreateInstance<RaycastHitData>();
			raycastHitData.CacheTransform(hitTransform);
			raycastHitData.Point = hitPosition;
			raycastHitData.Collider = hitTransform.GetComponent<Collider>();
			raycastHitData.ClosestVertex = closestVert;
			return raycastHitData;
		}

		/// <summary>
		/// Determines if a ray intersects a triangle.
		/// </summary>
		/// <param name="ray">The ray to check intersection for.</param>
		/// <param name="v0">First vertex of the triangle.</param>
		/// <param name="v1">Second vertex of the triangle.</param>
		/// <param name="v2">Third vertex of the triangle.</param>
		/// <param name="hit">The intersection point if it exists.</param>
		/// <returns>Returns true if the ray intersects the triangle, otherwise false.</returns>
		private static bool RayIntersectsTriangle(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, out Vector3 hit)
		{
			hit = Vector3.zero;

			// Implementation of Möller–Trumbore intersection algorithm,
			// referenced translation to C# https://discussions.unity.com/t/a-fast-triangle-triangle-intersection-algorithm-for-unity/126010/4
			Vector3 h, s, q;
			float a, f, u, v;
			Vector3 e1 = v1 - v0;
			Vector3 e2 = v2 - v0;
			h = Vector3.Cross(ray.direction, e2);
			a = Vector3.Dot(e1, h);

			const float EPSILON = 0.000001f;
			if (a > -EPSILON && a < EPSILON)
				return false;
			f = 1.0f / a;
			s = ray.origin - v0;
			u = f * Vector3.Dot(s, h);
			if (u < 0.0f || u > 1.0f)
				return false;
			q = Vector3.Cross(s, e1);
			v = f * Vector3.Dot(ray.direction, q);
			if (v < 0.0f || u + v > 1.0f)
				return false;
			float t = f * Vector3.Dot(e2, q);
			if (t > EPSILON)
			{
				hit = ray.origin + ray.direction * t;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Handles the selection logic when an object is selected.
		/// </summary>
		/// <param name="raycastHitData">The data for the selected object.</param>
		private static void OnObjectSelect(RaycastHitData raycastHitData)
		{
			if (raycastHitData.Transform == null) return;

			var indexes = FindIndexesOfHitIn3DSpace(raycastHitData);

			if (indexes.Count > 0) RemoveFromHitsAtIndexes(indexes);
			else AddToHits(raycastHitData);
		}

		/// <summary>
		/// Finds the indexes of a hit in 3D space.
		/// </summary>
		/// <param name="raycastHitData">The raycast hit data to check.</param>
		/// <returns>Returns a list of indexes.</returns>
		private static List<int> FindIndexesOfHitIn3DSpace(RaycastHitData raycastHitData)
		{
			var rayHitPoint = raycastHitData.Point;
			var clickThreshold = 1f * (ClickAccuracyThreshold * 0.01f);
			List<int> indexes = new();

			if (Selection == SelectionType.Object)
			{
				for (var i = 0; i < hitListData.hits.Count; i++)
				{
					if (hitListData.hits[i].Transform == raycastHitData.Transform) indexes.Add(i);
				}

				return indexes;
			}

			for (var i = 0; i < hitListData.hits.Count; i++)
			{
				var hitWorldPosition = Selection == SelectionType.HitPoint
					? hitListData.hits[i].Point
					: hitListData.hits[i].ClosestVertex;

				if (!(Vector3.Distance(hitWorldPosition ?? Vector3.positiveInfinity, rayHitPoint) <
				      clickThreshold)) continue;
				indexes.Add(i);
				return indexes;
			}

			return indexes;
		}

		/// <summary>
		/// Clears all selected hits.
		/// </summary>
		private static void Clear()
		{
			ClearHits();
			ClearCache();
		}

		/// <summary>
		/// Clears the cache data.
		/// </summary>
		private static void ClearCache() => closestVertexCache.Clear();

		/// <summary>
		/// Checks for any changes in the hierarchy and updates the hit data accordingly.
		/// </summary>
		private void HierarchyChanged()
		{
			List<RaycastHitData> hitsToRemove = new();

			foreach (var hit in hitListData.hits)
			{
				if (hit.Collider == null || hit.Collider.gameObject == null) hitsToRemove.Add(hit);
			}

			foreach (var hitToRemove in hitsToRemove)
			{
				var index = hitListData.hits.IndexOf(hitToRemove);
				RemoveFromHitsAtIndexes(new List<int>() {index});
			}
		}

		/// <summary>
		/// Clears all raycast hits.
		/// </summary>
		private static void ClearHits()
		{
			if (hitListData == null || hitListDataProp == null) return;
			Undo.RecordObject(hitListData, "Clear RaycastHits");
			hitListDataProp?.ClearArray();
			serializedHitListData?.ApplyModifiedProperties();
		}

		/// <summary>
		/// Adds a new raycast hit data to the hit list.
		/// </summary>
		/// <param name="raycastHitData">The data for the raycast hit.</param>
		private static void AddToHits(RaycastHitData raycastHitData)
		{
			Undo.RecordObject(hitListData, "Add RaycastHit");
			hitListDataProp.InsertArrayElementAtIndex(hitListDataProp.arraySize);
			hitListDataProp.GetArrayElementAtIndex(hitListDataProp.arraySize - 1).objectReferenceValue = raycastHitData;
			serializedHitListData.ApplyModifiedProperties();
		}

		/// <summary>
		/// Removes raycast hits from the list based on their indexes.
		/// </summary>
		/// <param name="indexes">List of indexes of hits to be removed.</param>
		private static void RemoveFromHitsAtIndexes(List<int> indexes)
		{
			Undo.RecordObject(hitListData, "Remove RaycastHit");
			for (var i = hitListData.hits.Count - 1; i >= 0; i--)
			{
				if (indexes.Contains(i)) hitListDataProp.DeleteArrayElementAtIndex(i);
			}

			serializedHitListData.ApplyModifiedProperties();
		}

		/// <summary>
		/// Updates the object. Calls the repaint method.
		/// </summary>
		private void Update() => Repaint();

		/// <summary>
		/// Calculates the closest vertex to a raycast hit.
		/// </summary>
		/// <param name="hit">The raycast hit data.</param>
		/// <param name="closestVertex">The closest vertex to the raycast hit.</param>
		/// <returns>Returns true if a closest vertex could be determined, otherwise false.</returns>
		public static bool CalculateClosestVertex(RaycastHitData hit, out Vector3 closestVertex)
		{
			closestVertex = Vector3.negativeInfinity;
			if (hit == null || hit.MeshFilter == null) return false;
			var transform = hit.Transform;
			if (CheckVertexCache(hit, ref closestVertex, transform)) return true;

			closestVertex = Vector3.positiveInfinity;

			closestVertex = hit.TriangleIndex == -1
				? GetClosestVertexFromMesh(hit, hit.MeshFilter)
				: GetClosestVertexFromNonMesh(hit, hit.MeshFilter);

			if (closestVertex == Vector3.positiveInfinity) return true;
			hit.ClosestVertex = closestVertex;
			closestVertexCache.TryAdd(hit, new MeshData(hit.MeshFilter.sharedMesh, closestVertex, transform));

			return true;
		}

		/// <summary>
		/// Gets the closest vertex to the raycast hit from a mesh.
		/// </summary>
		/// <param name="hit">The raycast hit data.</param>
		/// <param name="meshFilter">The mesh filter containing the vertices.</param>
		/// <returns>Returns the closest vertex.</returns>
		private static Vector3 GetClosestVertexFromMesh(RaycastHitData hit, MeshFilter meshFilter)
		{
			if (hit == null || meshFilter == null) return Vector3.positiveInfinity;
			Vector3 closestVertex = Vector3.zero;
			var vertices = meshFilter.sharedMesh.vertices;
			var minDistance = float.MaxValue;

			Vector3 transformedHitPoint = hit.Point;

			// Convert the hitpoint to worldcoords
			if (hit.Transform != null)
				transformedHitPoint = hit.AdjustToWorldUsingCurrentTransform(hit.Point, hit.Transform);

			foreach (var vertex in vertices)
			{
				var vertexWorldPos = meshFilter.transform.TransformPoint(vertex);
				var distance = Vector3.Distance(vertexWorldPos, transformedHitPoint);

				if (!(distance < minDistance)) continue;
				minDistance = distance;
				closestVertex = vertexWorldPos;
			}

			return closestVertex;
		}

		/// <summary>
		/// Gets the closest vertex to the raycast hit from a non-mesh object.
		/// </summary>
		/// <param name="hit">The raycast hit data.</param>
		/// <param name="meshFilter">The mesh filter containing the vertices.</param>
		/// <returns>Returns the closest vertex.</returns>
		private static Vector3 GetClosestVertexFromNonMesh(RaycastHitData hit, MeshFilter meshFilter)
		{
			var vertices = meshFilter.sharedMesh.vertices;
			var triangles = meshFilter.sharedMesh.triangles;

			var vertex1 = meshFilter.transform.TransformPoint(vertices[triangles[hit.TriangleIndex * 3]]);
			var vertex2 = meshFilter.transform.TransformPoint(vertices[triangles[hit.TriangleIndex * 3 + 1]]);
			var vertex3 = meshFilter.transform.TransformPoint(vertices[triangles[hit.TriangleIndex * 3 + 2]]);

			var triangleVertices = new[] {vertex1, vertex2, vertex3};
			var closestVertex = triangleVertices.OrderBy(v => Vector3.Distance(v, hit.Point)).First();
			return closestVertex;
		}

		/// <summary>
		/// Checks if the closest vertex for a raycast hit is cached.
		/// </summary>
		/// <param name="hit">The raycast hit data.</param>
		/// <param name="closestVertex">The closest vertex to the raycast hit.</param>
		/// <param name="transform">The transform of the object hit by the raycast.</param>
		/// <returns>Returns true if the vertex is found in cache, otherwise false.</returns>
		private static bool CheckVertexCache(RaycastHitData hit, ref Vector3 closestVertex, Transform transform)
		{
			if (!closestVertexCache.TryGetValue(hit, out var value)) return false;
			if (hit.Collider == null || !hit.MeshFilter ||
			    hit.MeshFilter.sharedMesh != value.mesh || value.HasTransformChanged(transform)) return false;
			closestVertex = value.vert;
			return true;
		}
	}
}