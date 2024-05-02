using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Measure
{
	/// <summary>
	/// Responsible for rendering elements in the MeasureEditor within the SceneView.
	/// Implements IDisposable to safely handle the unsubscription from scene events when not needed.
	/// </summary>
	public class MeasureEditorRendering : IDisposable
	{
		private readonly DistanceDrawer distanceDrawer;
		private readonly HitDrawer hitDrawer;
		private readonly BoundsDrawer boundsDrawer;

		/// <summary>
		/// Initializes new instance of MeasureEditorRendering, subscribing to SceneView events and initializing drawers.
		/// </summary>
		public MeasureEditorRendering()
		{
			SceneView.duringSceneGui += DuringScene;
			distanceDrawer = new DistanceDrawer();
			hitDrawer = new HitDrawer();
			boundsDrawer = new BoundsDrawer();
		}

		/// <summary>
		/// Callback executed during Scene GUI rendering. 
		/// Determines which elements to draw based on the current MeasureEditor selection.
		/// </summary>
		/// <param name="sceneView">The current SceneView.</param>
		private void DuringScene(SceneView sceneView)
		{
			if (!MeasureEditor.Enabled) return;

			switch (MeasureEditor.Selection)
			{
				case SelectionType.Vertex:
					hitDrawer.DrawVertexHits();
					if (MeasureEditor.ShowClosestPointDistance) distanceDrawer.ShowVertexDistance();
					break;
				case SelectionType.Object:
					boundsDrawer.DrawBounds();
					if (MeasureEditor.ShowCentreMassDistance) distanceDrawer.ShowCentreMassDistance();
					if (MeasureEditor.ShowClosestPointDistance) distanceDrawer.ShowClosestPointDistance();
					break;
				case SelectionType.HitPoint:
					hitDrawer.DrawHitHits();
					if (MeasureEditor.ShowClosestPointDistance) distanceDrawer.ShowHitDistance();
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			var currentSceneView = SceneView.lastActiveSceneView;
			if (MeasureEditor.ShowSize && MeasureEditor.Selection == SelectionType.Object)
				boundsDrawer.DrawSize(currentSceneView);

			SceneView.RepaintAll();
		}

		/// <summary>
		/// Unsubscribes from the SceneView's duringSceneGui event.
		/// </summary>
		private void ReleaseUnmanagedResources() => SceneView.duringSceneGui -= DuringScene;

		/// <summary>
		/// Cleans up resources by unsubscribing from events.
		/// </summary>
		public void Dispose()
		{
			ReleaseUnmanagedResources();
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Destructor to ensure unsubscription from events when the object is finalized.
		/// </summary>
		~MeasureEditorRendering() => ReleaseUnmanagedResources();
	}
}