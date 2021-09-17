using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace Broccoli.TreeNodeEditor
{
	/// <summary>
	/// Editor utility class to preview meshes on custom editors.
	/// </summary>
	public class MeshPreview
	{
		#region Vars
		/// <summary>
		/// Shows the triangles and vertices count on the preview.
		/// </summary>
		public bool showTrisCount = true;
		/// <summray>
		/// Draw the mesh using wireframe mode.
		/// </summary>
		public bool showWireframe = false;
		/// <summray>
		/// Draws a pivot dot.
		/// </summary>
		public bool showPivot = false;
		/// <summray>
		/// Position for th'e pivot.
		/// </summary>
		public Vector3 pivotPosition = Vector3.zero;
		/// <summray>
		/// Size used for the handles.
		/// </summary>
		public float handlesSize = 0.2f;
		/// <summray>
		/// Color for the pivot handle.
		/// </summary>
		public Color pivotHandleColor = Color.yellow;
		/// <summray>
		/// DrawExtras delegate definition.
		/// </summary>
		public delegate void DrawExtras (Rect r, Camera camera);
		/// <summray>
		/// DrawExtras multidelegate for handles.
		/// </summary>
		public DrawExtras onDrawHandles;
		/// <summray>
		/// DrawExtras multidelegate for GUI.
		/// </summary>
		public DrawExtras onDrawGUI;
		/// <summary>
		/// Does autozoom on the first mesh added to a viewport.
		/// </summary>
		public bool autoZoomEnabled = false;
		/// <summary>
		/// The meshes.
		/// </summary>
		private List<Mesh> _meshes = new List<Mesh> ();
		/// <summary>
		/// The materials.
		/// </summary>
		private List<Material> _materials = new List<Material> ();
		/// <summary>
		/// Light A on the preview.
		/// </summary>
		private Light _lightA = new Light ();
		/// <summary>
		/// Light B on the preview.
		/// </summary>
		private Light _lightB = new Light ();
		/// <summary>
		/// The default materials.
		/// </summary>
		private List<Material> _defaultMaterials = new List<Material> ();
		/// <summary>
		/// The mesh to viewport.
		/// </summary>
		private Dictionary<int, int> _meshToViewport = new Dictionary<int, int> ();
		/// <summary>
		/// The mesh tris count.
		/// </summary>
		private Dictionary<int, bool> _meshTrisCount = new Dictionary<int, bool> ();
		/// <summary>
		/// The viewport names.
		/// </summary>
		private List<string> _viewportNames = new List<string> ();
		/// <summary>
		/// The default material.
		/// </summary>
		private Material _defaultMaterial = null;
		/// <summary>
		/// The selected viewport.
		/// </summary>
		private int _selectedViewport = -1;
		/// <summary>
		/// The preview render utility.
		/// </summary>
		private PreviewRenderUtility _previewRenderUtility;
		/// <summary>
		/// Camera position.
		/// </summary>
		Vector3 camPos = Vector3.zero;
		/// <summary>
		/// The avatar scale.
		/// </summary>
		private float m_AvatarScale = 1.0f;
		/// <summary>
		/// The zoom factor.
		/// </summary>
		private float m_ZoomFactor = 1.0f;
		/// <summary>
		/// The preview string.
		/// </summary>
		const string s_PreviewStr = "Mesh Preview";
		/// <summary>
		/// The preview direction.
		/// </summary>
		//private Vector2 m_PreviewDir = new Vector2 (120, -20);
		private Vector2 m_PreviewDir = new Vector2 (90, 0);
		/// <summary>
		/// Offset for camera.
		/// </summary>
		/// <returns>Offset.</returns>
		private Vector3 m_PreviewOffset = new Vector3 (0f, 0f, -5.5f);
		/// <summary>
		/// The preview hint.
		/// </summary>
		int m_PreviewHint = s_PreviewStr.GetHashCode();
		/// <summary>
		/// The text style.
		/// </summary>
		private GUIStyle textStyle = new GUIStyle ();
		/// <summary>
		/// The tris count.
		/// </summary>
		private int _trisCount = 0;
		/// <summary>
		/// The verts count.
		/// </summary>
		private int _vertsCount = 0;
		/// <summary>
		/// Flag to display debug information about the preview parameters.
		/// </summary>
		public bool showDebugInfo = false;
		/// <summary>
		/// String used to display debug information.
		/// </summary>
		string debugInfo = string.Empty;
		/// <summary>
		/// Minimum zoom factor value.
		/// </summary>
		public float minZoomFactor = 0.3f;
		/// <summary>
		/// Maximum zoom factor value.
		/// </summary>
		public float maxZoomFactor = 10f;
		#endregion

		#region Constructor
		/// <summary>
		/// Initializes a new instance of the <see cref="Broccoli.TreeNodeEditor.MeshPreview"/> class.
		/// </summary>
		public MeshPreview () {
			// Init PreviewRenderUtility
			_previewRenderUtility = new PreviewRenderUtility ();
			//We set the previews camera to 6 units back, look towards the middle of the 'scene'
			#if UNITY_2017_1_OR_NEWER
			_previewRenderUtility.camera.transform.position = new Vector3 (0, 0, -8);
			_previewRenderUtility.camera.transform.rotation = Quaternion.identity;
			#else
			_previewRenderUtility.m_Camera.transform.position = new Vector3 (0, 0, -8);
			_previewRenderUtility.m_Camera.transform.rotation = Quaternion.identity;
			#endif

			// Lighting
			_lightA = _previewRenderUtility.lights[0];
			_lightA.intensity = 1f;
			_lightA.transform.rotation = Quaternion.Euler(90f, 90f, 0f);
			_lightB = _previewRenderUtility.lights[1];
			_lightB.intensity = 1f;
			_lightB.transform.rotation = Quaternion.Euler(90f, 90f, 0f);

			// Init preview default material.
			_defaultMaterial = new Material (Shader.Find ("Diffuse"));

			// Style
			textStyle.normal.textColor = Color.white;
		}
		/// <summary>
		/// Raises the destroy event.
		/// </summary>
		void OnDestroy () {
			Object.DestroyImmediate (_defaultMaterial);
			_previewRenderUtility.Cleanup();
		}
		#endregion

		#region CRUD
		/// <summary>
		/// Creates a viewport.
		/// </summary>
		/// <returns>The viewport index.</returns>
		/// <param name="name">Name for the viewport.</param>
		public int CreateViewport (string name = "mesh") {
			_viewportNames.Add (name);
			return _viewportNames.Count - 1;
		}
		/// <summary>
		/// Adds a mesh to the viewport.
		/// </summary>
		/// <returns><c>true</c>, if mesh was added, <c>false</c> otherwise.</returns>
		/// <param name="viewportIndex">Viewport index.</param>
		/// <param name="mesh">Mesh.</param>
		/// <param name="countTris">If set to <c>true</c> count tris.</param>
		public bool AddMesh (int viewportIndex, Mesh mesh, bool countTris = false) {
			return AddMesh (viewportIndex, mesh, null, countTris);
		}
		/// <summary>
		/// Adds a mesh to the viewport.
		/// </summary>
		/// <returns><c>true</c>, if mesh was added, <c>false</c> otherwise.</returns>
		/// <param name="viewportIndex">Viewport index.</param>
		/// <param name="mesh">Mesh.</param>
		/// <param name="material">Material.</param>
		/// <param name="countTris">If set to <c>true</c> count tris.</param>
		public bool AddMesh (int viewportIndex, Mesh mesh, Material material = null, bool countTris = false) {
			if (viewportIndex < _viewportNames.Count) {
				_meshes.Add (mesh);
				if (material == null) {
					material = Object.Instantiate (_defaultMaterial);
					_defaultMaterials.Add (material);
				}
				_materials.Add (material);
				bool autoZoom = false;
				if (!_meshToViewport.ContainsValue (viewportIndex)) {
					autoZoom = true;
				}
				_meshToViewport.Add (_meshes.Count - 1, viewportIndex);
				_meshTrisCount.Add (_meshes.Count - 1, countTris);
				if (_selectedViewport == -1) {
					_selectedViewport = 0;
				}
				if (autoZoom && autoZoomEnabled) {
					CalculateZoom (mesh);
				}
				return true;
			}
			return false;
		}
		/// <summary>
		/// Sets Light A intensity and transform rotation.
		/// </summary>
		/// <param name="intensity">Light intensity.</param>
		/// <param name="quaternion">Transform rotation.</param>
		public void SetLightA (float intensity, Quaternion quaternion) {
			SetLigth (_lightA, intensity, quaternion);
		}
		/// <summary>
		/// Sets Light B intensity and transform rotation.
		/// </summary>
		/// <param name="intensity">Light intensity.</param>
		/// <param name="quaternion">Transform rotation.</param>
		public void SetLightB (float intensity, Quaternion quaternion) {
			SetLigth (_lightB, intensity, quaternion);
		}
		/// <summary>
		/// Gets the light A on this instance.
		/// </summary>
		/// <returns>Light A.</returns>
		public Light GetLigthA () {
			return _lightA;
		}
		/// <summary>
		/// Gets the light B on this instance.
		/// </summary>
		/// <returns>Light B.</returns>
		public Light GetLigthB () {
			return _lightB;
		}
		/// <summary>
		/// Sets a ligth intensity and transform rotation.
		/// </summary>
		/// <param name="light">Ligth to set values.</param>
		/// <param name="intensity">Light intensity.</param>
		/// <param name="quaternion">Transform rotation.</param>
		void SetLigth (Light light, float intensity, Quaternion quaternion) {
			light.intensity = intensity;
			light.transform.rotation = quaternion;
		}
		/// <summary>
		/// Selects the viewport for rendering.
		/// </summary>
		/// <returns><c>true</c>, if viewport was selected, <c>false</c> otherwise.</returns>
		/// <param name="index">Index.</param>
		public bool SelectViewport (int index) {
			if (index >= 0 && index < _viewportNames.Count) {
				_selectedViewport = index;
				return true;
			}
			return false;
		}
		/// <summary>
		/// Gets the viewport count.
		/// </summary>
		/// <returns>The viewport count.</returns>
		public int GetViewportCount () {
			return _viewportNames.Count;
		}
		/// <summary>
		/// Clear this instance.
		/// </summary>
		public void Clear () {
			_meshes.Clear ();
			_materials.Clear ();
			_defaultMaterials.Clear ();
			for (int i = 0; i < _defaultMaterials.Count; i++) {
				Object.DestroyImmediate (_defaultMaterials [i]);
			}
			_meshToViewport.Clear ();
			_viewportNames.Clear ();
			_meshTrisCount.Clear ();
			_selectedViewport = -1;
		}
		#endregion

		#region Rendering
		private void PreRenderViewport (Rect r, GUIStyle background) {
			// Handle preview GUI.
			int previewID = GUIUtility.GetControlID(m_PreviewHint, FocusType.Passive, r);
			Event evt = Event.current;
			EventType type = evt.GetTypeForControl(previewID);
			if (r.Contains (evt.mousePosition)) {
				HandleViewTool (evt, type, previewID, r);
			}

			// Begin preview
			_previewRenderUtility.BeginPreview (r, background);

			// Zoom
			#if UNITY_2017_1_OR_NEWER
			_previewRenderUtility.camera.nearClipPlane = 0.5f * m_ZoomFactor;
			_previewRenderUtility.camera.farClipPlane = 200.0f * m_AvatarScale;
			#else
			_previewRenderUtility.m_Camera.nearClipPlane = 0.5f * m_ZoomFactor;
			_previewRenderUtility.m_Camera.farClipPlane = 200.0f * m_AvatarScale;
			#endif

			// Position
			Quaternion camRot = Quaternion.Euler(-m_PreviewDir.y, -m_PreviewDir.x, 0);
			camPos = camRot * (m_PreviewOffset * m_ZoomFactor);
			#if UNITY_2017_1_OR_NEWER
			_previewRenderUtility.camera.transform.position = camPos;
			_previewRenderUtility.camera.transform.rotation = camRot;
			#else
			_previewRenderUtility.m_Camera.transform.position = camPos;
			_previewRenderUtility.m_Camera.transform.rotation = camRot;
			#endif

			#if UNITY_2017_1_OR_NEWER
			/*
			_previewRenderUtility.lights[0] = _lightA;
			_previewRenderUtility.lights[1] = _lightB;
			*/
			_previewRenderUtility.ambientColor = Color.gray;
			#endif
		}
		/// <summary>
		/// Renders the viewport.
		/// </summary>
		/// <param name="r">Rect to render to.</param>
		/// <param name="background">Background.</param>
		public void RenderViewport (Rect r, GUIStyle background) {
			PreRenderViewport (r, background);
			
			// Rendering meshes.
			_vertsCount = 0;
			_trisCount = 0;
			var meshToViewportEnumerator = _meshToViewport.GetEnumerator ();
			while (meshToViewportEnumerator.MoveNext ()) {
				if (meshToViewportEnumerator.Current.Value == _selectedViewport) {
					Mesh meshToDraw = _meshes [meshToViewportEnumerator.Current.Key];
					Material materialToUse;
					if (showWireframe) {
						materialToUse = _defaultMaterial;
					} else {
						materialToUse = _materials [meshToViewportEnumerator.Current.Key];
					}

					for (int j = 0; j < meshToDraw.subMeshCount; j++) {
						_previewRenderUtility.DrawMeshB (meshToDraw, 
							Vector3.zero, Quaternion.identity, materialToUse, j);
					}

					if (showTrisCount && _meshTrisCount [meshToViewportEnumerator.Current.Key]) {
						_trisCount += meshToDraw.triangles.Length / 3;
						_vertsCount += meshToDraw.vertices.Length;
					}
				}
			}

			PostRenderViewport (r, background);
		}
		public void RenderViewport (Rect r, GUIStyle background, Material[] materials) {
			PreRenderViewport (r, background);
			
			// Rendering meshes.
			_vertsCount = 0;
			_trisCount = 0;
			var meshToViewportEnumerator = _meshToViewport.GetEnumerator ();
			while (meshToViewportEnumerator.MoveNext ()) {
				if (meshToViewportEnumerator.Current.Value == _selectedViewport) {
					Mesh meshToDraw = _meshes [meshToViewportEnumerator.Current.Key];
					if (meshToDraw.subMeshCount != materials.Length) return;

					for (int j = 0; j < meshToDraw.subMeshCount; j++) {
						_previewRenderUtility.DrawMeshB (meshToDraw, 
							Vector3.zero, Quaternion.identity, materials[j], j);
					}

					if (showTrisCount && _meshTrisCount [meshToViewportEnumerator.Current.Key]) {
						_trisCount += meshToDraw.triangles.Length / 3;
						_vertsCount += meshToDraw.vertices.Length;
					}
				}
			}

			PostRenderViewport (r, background);
		}
		private void PostRenderViewport (Rect r, GUIStyle background) {
			// Final camera rendering.
			if (showWireframe) {
				GL.wireframe = true;
			}
			bool fog = RenderSettings.fog;
            Unsupported.SetRenderSettingsUseFogNoDirty(false);
			#if UNITY_2017_1_OR_NEWER
			_previewRenderUtility.camera.Render ();
			#else
			_previewRenderUtility.m_Camera.Render ();
			#endif
			if (showWireframe) {
				GL.wireframe = false;
			}
			Unsupported.SetRenderSettingsUseFogNoDirty(fog);
			
			// Draw pivot and handles.
			if (showPivot || onDrawHandles != null) {
				#if UNITY_2017_1_OR_NEWER
				Handles.SetCamera (_previewRenderUtility.camera);
				#else
				Handles.SetCamera (_previewRenderUtility.m_Camera);
				#endif
			}
			if (onDrawHandles != null) {
				#if UNITY_2017_1_OR_NEWER
				onDrawHandles (r, _previewRenderUtility.camera);
				#else
				onDrawHandles (r, _previewRenderUtility.m_Camera);
				#endif
			}
			if (showPivot) {
				Handles.color = Color.yellow;
				#if UNITY_2017_1_OR_NEWER
				Handles.DrawSolidDisc (Vector3.zero, 
					_previewRenderUtility.camera.transform.forward, 
					0.1f * GetHandleSize (Vector3.zero, _previewRenderUtility.camera));
				#else
				Handles.DrawSolidDisc (Vector3.zero, 
					_previewRenderUtility.m_Camera.transform.forward, 
					0.1f * GetHandleSize (Vector3.zero, _previewRenderUtility.m_Camera));
				#endif
			}

			// Draw rendered texture
			Texture resultRender = _previewRenderUtility.EndPreview();
			GUI.DrawTexture (r, resultRender, ScaleMode.StretchToFill, false);

			if (showTrisCount) {
				GUI.Label (r, "Tris: " + _trisCount + ", Verts: " + _vertsCount, textStyle);
			}
			if (showDebugInfo) {
				GUI.Label (r, "\n" + GetDebugInfo (), textStyle);
			}
			if (onDrawGUI != null) {
				#if UNITY_2017_1_OR_NEWER
				onDrawGUI (r, _previewRenderUtility.camera);
				#else
				onDrawGUI (r, _previewRenderUtility.m_Camera);
				#endif
			}
		}
        /// <summary>
		/// Get world space size of a manipulator handle at given position.
		/// </summary>
		/// <param name="position">Postion of the handle.</param>
		/// <param name="camera">Camera.</param>
		public static float GetHandleSize (Vector3 position, Camera camera)
        {
            position = Handles.matrix.MultiplyPoint(position);
			float k_KHandleSize = 80.0f;
            if (camera)
            {
                Transform tr = camera.transform;
                Vector3 camPos = tr.position;
                float distance = Vector3.Dot(position - camPos, tr.TransformDirection(new Vector3(0, 0, 1)));
                Vector3 screenPos = camera.WorldToScreenPoint(camPos + tr.TransformDirection(new Vector3(0, 0, distance)));
                Vector3 screenPos2 = camera.WorldToScreenPoint(camPos + tr.TransformDirection(new Vector3(1, 0, distance)));
                float screenDist = (screenPos - screenPos2).magnitude;
                return (k_KHandleSize / Mathf.Max(screenDist, 0.0001f)) * EditorGUIUtility.pixelsPerPoint;
            }
            return 20.0f;
        }
		#endregion

		#region UI Events
		/// <summary>
		/// Handles the view tool.
		/// </summary>
		/// <param name="evt">Evt.</param>
		/// <param name="eventType">Event type.</param>
		/// <param name="id">Identifier.</param>
		/// <param name="previewRect">Preview rect.</param>
		protected void HandleViewTool (Event evt, EventType eventType, int id, Rect previewRect) {
			switch (eventType)
			{
			case EventType.ScrollWheel: DoAvatarPreviewZoom (evt, HandleUtility.niceMouseDeltaZoom * (evt.shift ? 2.0f : 0.5f)); break;
			case EventType.MouseDown:   HandleMouseDown (evt, id, previewRect); break;
			case EventType.MouseUp:     HandleMouseUp (evt, id); break;
			case EventType.MouseDrag:   HandleMouseDrag (evt, id, previewRect); break;
			}
		}
		/// <summary>
		/// Handles the mouse down.
		/// </summary>
		/// <param name="evt">Evt.</param>
		/// <param name="id">Identifier.</param>
		/// <param name="previewRect">Preview rect.</param>
		protected void HandleMouseDown (Event evt, int id, Rect previewRect)	{
			EditorGUIUtility.SetWantsMouseJumping (1);
			//evt.Use ();
			GUIUtility.hotControl = id;
		}
		/// <summary>
		/// Handles the mouse up.
		/// </summary>
		/// <param name="evt">Evt.</param>
		/// <param name="id">Identifier.</param>
		protected void HandleMouseUp (Event evt, int id)	{
			if (GUIUtility.hotControl == id) {
				GUIUtility.hotControl = 0;
				EditorGUIUtility.SetWantsMouseJumping (0);
				//evt.Use ();
			}
		}
		/// <summary>
		/// Handles the mouse drag.
		/// </summary>
		/// <param name="evt">Evt.</param>
		/// <param name="id">Identifier.</param>
		/// <param name="previewRect">Preview rect.</param>
		protected void HandleMouseDrag (Event evt, int id, Rect previewRect)	{
			if (GUIUtility.hotControl == id) {
				if (evt.control) {
					DoAvatarPreviewOffset (evt, previewRect);
				} else {
					DoAvatarPreviewOrbit (evt, previewRect);
				}
			}
		}
		/// <summary>
		/// Does the avatar preview orbit.
		/// </summary>
		/// <param name="evt">Evt.</param>
		/// <param name="previewRect">Preview rect.</param>
		public void DoAvatarPreviewOrbit (Event evt, Rect previewRect) {
			m_PreviewDir -= evt.delta * (evt.shift ? 3 : 1) / Mathf.Min(previewRect.width, previewRect.height) * 140.0f;
			m_PreviewDir.y = Mathf.Clamp (m_PreviewDir.y, -90, 90);
			//evt.Use();
		}
		/// <summary>
		/// Does the avatar preview offset.
		/// </summary>
		/// <param name="evt">Evt.</param>
		/// <param name="previewRect">Preview rect.</param>
		public void DoAvatarPreviewOffset (Event evt, Rect previewRect) {
			m_PreviewOffset.x -= evt.delta.x * 0.01f;
			m_PreviewOffset.y -= evt.delta.y * 0.01f;
			//evt.Use();
		}
		/// <summary>
		/// Does the avatar preview zoom.
		/// </summary>
		/// <param name="evt">Evt.</param>
		/// <param name="delta">Delta.</param>
		public void DoAvatarPreviewZoom(Event evt, float delta)	{
			float zoomDelta = -delta * 0.05f;
			m_ZoomFactor += m_ZoomFactor * zoomDelta;
			if (m_ZoomFactor < minZoomFactor) 
				m_ZoomFactor = minZoomFactor;
			else if (m_ZoomFactor > maxZoomFactor) 
				m_ZoomFactor = maxZoomFactor;
			// zoom is clamp too 10 time closer than the original zoom
			m_ZoomFactor = Mathf.Max (m_ZoomFactor, m_AvatarScale / 2.0f);
			//evt.Use();
		}
		/// <summary>
		/// Sets a zoom value for the camera.
		/// </summary>
		/// <param name="factor">Factor for zoom.</param>
		public void DoZoom (float factor) {
			if (factor < minZoomFactor) 
				factor = minZoomFactor;
			else if (factor > maxZoomFactor) 
				factor = maxZoomFactor;
			m_ZoomFactor = factor;
		}
		/// <summary>
		/// Sets an offset value for the camera.
		/// </summary>
		/// <param name="offset"></param>
		public void DoOffset (Vector3 offset) {
			m_PreviewOffset = offset;
		}
		/// <summary>
		/// Calculates the best zoom for a mesh.
		/// </summary>
		/// <param name="mesh"></param>
		public void CalculateZoom (Mesh mesh) {
			mesh.RecalculateBounds ();
			float distance = Vector3.Distance (mesh.bounds.min, mesh.bounds.max);
			DoZoom (distance * 0.8f);
		}
		#endregion

		#region Debug
		/// <summary>
		/// Get a string with debug information about this mesh view.
		/// </summary>
		/// <returns>String with debug information.</returns>
		public string GetDebugInfo () {
			debugInfo = string.Empty;
			debugInfo += string.Format ("Camera Pos: {0}, {1}, {2}\n", camPos.x.ToString ("F3"), camPos.y.ToString ("F3"), camPos.z.ToString ("F3"));
			debugInfo += string.Format ("Camera Offset: {0}, {1}, {2}\n", m_PreviewOffset.x.ToString ("F3"), m_PreviewOffset.y.ToString ("F3"), m_PreviewOffset.z.ToString ("F3"));
			debugInfo += string.Format ("Camera Direction: {0}, {1}\n", m_PreviewDir.x.ToString ("F3"), m_PreviewDir.y.ToString ("F3"));
			debugInfo += string.Format ("Zoom Factor: {0}\n", m_ZoomFactor);
			debugInfo += string.Format ("Light 0 Intensity: {0}, Rotation: {1}\n", _lightA.intensity.ToString ("F2"), _lightA.transform.rotation.eulerAngles);
			debugInfo += string.Format ("Light 1 Intensity: {0}, Rotation: {1}\n", _lightB.intensity.ToString ("F2"), _lightB.transform.rotation.eulerAngles);
			return debugInfo;
		}
		#endregion
	}
}