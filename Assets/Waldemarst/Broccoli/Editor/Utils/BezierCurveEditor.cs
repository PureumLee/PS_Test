using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using Broccoli.Model;
using Broccoli.Utils;

namespace Broccoli.Utils
{
    /// <summary>
    /// Bezier Curves Editor.
    /* 
    EVENTS
    Order is onValidateAction, onBeforeAction, onAction
    onValidateAction: stops or continues the propagation of events.
    onBeforeAction: the action passed and is about to be applied.
                    Useful for undo: Undo.RecordObject (objectToRecord, "Add Node");
    onAction: action has been applied to target curve or node.
              Should call serialize target object and update views (undo or not implemented).

    EVENTS LIST
    onCurveChanged      A change has been made to one of the curve nodes.
    onValidateAddNode   Returns true if a point can be added.
    onSelectionChanged  The selection of nodes has changed.
    onBeforeAddNode     Event to call before adding a node.
    onAddNode           Event to call after a node has been added.
    onBeforeEditNode    Event before editing a node properties (ex. node handle style).
    onEditNode          Event after editing a node properties.
    onCheckMoveNodes    Validates allowed moving offsets for selected nodes (restricting movement).
    onBeginMoveNodes    Event to raise when a selection of nodes begin moving.
    onEndMoveNodes      Event to raise when a selection of nodes lose focus after having moved.
    onBeforeMoveNodes   Event before moving nodes.
    onMoveNodes         Event after moving nodes.
    onSelectNode         Event to call when a node is clicked.
    onDeselectNode      Event to call when a node is deselected.
    onCheckRemoveNodes  Validates if nodes can be removed.
    onBeforeRemoveNodes Event before moving nodes.
    onRemoveNodes       Event after moving nodes.
    onBeginMoveHandle   Event to raise when a selected handle begins moving.
    onEndMoveHandle     Event to raise whan a selected handle lost focus after having moved.
    onBeforeMoveHandle  Event before moving node handles.
    onMoveHandle        Event after moving node handles.
    onSelectHandle      Event to call when a handle has been selected.
    OnDeselectHandle    Event to call when a handle has been deselected.
    onCheckDrawNode     Validates if a point in a curve should be drawn (with controls or not) or not.
    onCheckDrawNodeMoveControls Validates drawing a moving gizmo on a node.
    */
    /// </summary>
    public class BezierCurveEditor {
        #region Style Vars
        /// <summary>
		/// Color for the curve.
		/// </summary>
		public Color curveColor = Color.white;
        /// <summary>
        /// Color for the curve when selected.
        /// </summary>
        public Color selectedCurveColor = Color.gray;
		/// <summary>
		/// Width for the curve.
		/// </summary>
		public float curveWidth = 1f;
        /// <summary>
		/// Width for the curve when selected.
		/// </summary>
		public float selectedCurveWidth = 2f;
		/// <summary>
		/// Resolution of the curve.
		/// </summary>
		public float curveResolution = 8f;
        public Color nodeColor = new Color (0.8f, 0.8f, 0.8f, 1f);
        public Color selectedNodeColor = new Color (1f, 1f, 1f, 1f);
        public Color nodeHandleColor = new Color (0.8f, 0.8f, 0.8f, 1f);
        public Color selectedNodeHandleColor = new Color (1f, 1f, 1f, 1f);
        public Color nodeHandleLineColor = new Color (0.8f, 0.8f, 0.8f, 1f);
        public Color preselectedNodeColor = Color.yellow;
        /// <summary>
        /// Size on extra small nodes.
        /// </summary>
        public float extraSmallNodeSize = 0.05f;
        /// <summary>
        /// Size on small nodes.
        /// </summary>
        public float smallNodeSize = 0.075f;
        /// <summary>
		/// Size of the node handle.
		/// </summary>
		public float nodeSize = 0.08f;
		/// <summary>
		/// Size of the bezier nodes handles handles.
		/// </summary>
		public float nodeHandleSize = 0.05f;
        /// <summary>
        /// Label size for the nodes.
        /// </summary>
        public float labelSize = 0.4f;
        public Handles.CapFunction nodeDrawFunction = Handles.RectangleHandleCap;
        public Handles.CapFunction selectedNodeDrawFunction = Handles.DotHandleCap;
        /// <summary>
        /// Function used to draw bezier node handlers.
        /// </summary>
        public Handles.CapFunction nodeHandleDrawFunction = Handles.RectangleHandleCap;
        public Handles.CapFunction selectedNodeHandleDrawFunction = Handles.DotHandleCap;
        /// <summary>
        /// Temporary variable to save the handles color.
        /// </summary>
        private Color _tmpColor = Color.white;
        #endregion

        #region Mode and Settings Vars
        /// <summary>
		/// <c>True</c> if the the editor allows to select multiple nodes.
		/// </summary>
		public bool multiselectEnabled = true;
        /// <summary>
        /// Scale to draw and edit this curve.
        /// </summary>
        public float scale = 1f;
        /// <summary>
		/// Modes available for the editor.
		/// </summary>
		public enum EditMode {
            Show,
			Selection,
			Add
		}
        /// <summary>
		/// Current editor mode.
		/// </summary>
		private EditMode _editMode = EditMode.Selection;
        /// <summary>
        /// Current editor mode.
        /// </summary>
        /// <value>Editor mode from the enum.</value>
        public EditMode editMode {
            get { return _editMode; }
            set {
                if (_editMode != value) {
                    if (value == EditMode.Add) {
                        SetAddNodeCandidates ();
                    } else {
                        _nodeCandidates.Clear ();
                        _nodeCandidatesAdded.Clear ();
                        _nodeCandidatesIndex.Clear ();
                    }
                }
                _editMode = value;
            }
        }
        /// <summary>
        /// True to display handles to edit this curve.
        /// </summary>
        public bool showHandles = true;
        /// <summary>
        /// True to show the first handle on each node.
        /// Usually this handle is not drawn at the first node of the curve.
        /// </summary>
        public bool showFirstHandleAlways = false;
        /// <summary>
        /// True to show the second handle on each node.
        /// Usually this handle is not drawn at the last node of the curve.
        /// </summary>
        public bool showSecondHandleAlways = false;
        /// <summary>
        /// Distance on t position to create candidates to add nodes between
        /// existing nodes.
        /// </summary>
        public float addNodeResolution = 0.05f;
        #endregion

        #region Debug Vars
        /// <summary>
        /// Global flag to enable debug to show gizmos.
        /// </summary>
        public bool debugEnabled = false;
        /// <summary>
        /// Enabled to show curve points on the curve.
        /// </summary>
        public bool debugShowCurvePoints = false;
        /// <summary>
        /// Flag to show fine curve points (samples) on the curve.
        /// </summary>
        public bool debugShowCurveFinePoints = false;
        /// <summary>
        /// Flag to show labels next to each node on the curve.
        /// </summary>
        public bool debugShowNodeLabels = false;
        /// <summary>
        /// Flag to show an arrow pointing at the forward vector of each point.
        /// </summary>
        public bool debugShowPointForward = false;
        /// <summary>
        /// Flag to show an arrow pointing at the forward vector of each point.
        /// </summary>
        public bool debugShowPointNormal = false;
        /// <summary>
        /// Flag to show an arrow pointing at the up vector of each point.
        /// </summary>
        public bool debugShowPointUp = false;
        /// <summary>
        /// Flag to show an arrow pointing at the tangent vector of each point.
        /// </summary>
        public bool debugShowPointTangent = false;
        /// <summary>
        /// Labels for nodes color.
        /// </summary>
        public Color nodeLabelColor = Color.gray;
        #endregion

        #region Vars
        /// <summary>
        /// Type of control used on the nodes.
        /// </summary>
        public enum ControlType {
            FreeMove,
            SliderMove,
            FreeRotation,
            DrawOnly,
            None
        }
        /// <summary>
        /// Vector to use on a node slider control.
        /// </summary>
        public Vector3 sliderVector = Vector3.up;
        Vector3 lastKnownOffset = Vector3.zero;
        public Vector3 offsetStep = Vector3.zero;
        /// <summary>
        /// Curve id, used to relate the selection.
        /// </summary>
        public int curveId = -1;
        /// <summary>
		/// Curve to draw and edit.
		/// </summary>
		public BezierCurve _curve;
		/// <summary>
		/// Accessor to the curve managed by this editor.
		/// </summary>
		/// <value></value>
		public BezierCurve curve {
			get {
				return _curve;
			}
			set {
				bool curveChanged = false;
				if (_curve != value) {
					curveChanged = true;
                    if (_editMode == EditMode.Add) {
                        SetAddNodeCandidates ();
                    } else {
                        _nodeCandidates.Clear ();
                        _nodeCandidatesAdded.Clear ();
                        _nodeCandidatesIndex.Clear ();
                    }
				}
				_curve = value;
				if (curveChanged && onCurveChanged != null) {
					onCurveChanged (_curve);
				}
				UpdateSelection ();
			}
		}
        #endregion

        #region Selection Vars
        /// <summary>
		/// Flag to turn when a selection of a node happened.
		/// </summary>
		private bool selectionHappened = false;
		/// <summary>
		/// Selected nodes.
		/// </summary>
		private List<BezierNode> _selectedNodes = new List<BezierNode> ();
        /// <summary>
		/// Selected node index;
		/// </summary>
		private List<int> _selectedNodesIndex = new List<int> ();
        /// <summary>
        /// Selected curve ids.
        /// </summary>
        private List<int> _selectedCurveIds = new List<int> ();
        /// <summary>
        /// Relatonship between selected nodes and their ids.
        /// </summary>
        /// <typeparam name="Guid">Id of the bezier node.</typeparam>
        /// <typeparam name="BezierNode">Bezier node.</typeparam>
        /// <returns>Id to bezier node relationship.</returns>
        private Dictionary<System.Guid, BezierNode> _idToNode = new Dictionary<System.Guid, BezierNode> ();
        /// <summary>
        /// Node id to curve id.
        /// </summary>
        /// <typeparam name="Guid">Node id.</typeparam>
        /// <typeparam name="int">Curve id.</typeparam>
        /// <returns>Node id to curve id relationship.</returns>
        private Dictionary<System.Guid, int> _nodeToCurve = new Dictionary<System.Guid, int> ();
        public Dictionary<System.Guid, int> nodeToCurve {
            get { return _nodeToCurve; }
        }
		/// <summary>
		/// Single selected node or the first on a multiselection.
		/// </summary>
		/// <value>Single node selected.</value>
		public BezierNode selectedNode {
			get {
				if (_selectedNodes.Count > 0)
					return _selectedNodes[0];
				return null;	
			}
		}
		/// <summary>
		/// Single selected node index or the first index in the selection.
		/// </summary>
		/// <value>Single selected node index.</value>
		public int selectedNodeIndex {
			get {
				if (_selectedNodesIndex.Count > 0)
					return _selectedNodesIndex[0];
				return -1;
			}
		}
        /// <summary>
        /// Single selected curve or the first curve id in the selection.
        /// </summary>
        /// <value>Single selected curve.</value>
        public int selectedCurveId {
            get {
				if (_selectedCurveIds.Count > 0)
					return _selectedCurveIds[0];
				return -1;
			}
        }
        /*
        /// <summary>
        /// Return the id of the curve the first selected node belongs to.
        /// </summary>
        /// <value>Id of the curve the node belongs to, otherwise -1.</value>
        public int selectedNodeCurveId {
            get {
                if (_selectedNodes.Count > 0 && _nodeToCurve.ContainsKey(_selectedNodes[0].guid)) {
                    return _nodeToCurve[_selectedNodes[0].guid];
                }
                return -1;
            }
        }
        */
		/// <summary>
		/// Selected nodes.
		/// </summary>
		/// <value>Selected nodes list.</value>
		public List<BezierNode> selectedNodes {
			get { return _selectedNodes; }
		}
		/// <summary>
		/// Selected nodes indexes.
		/// </summary>
		/// <value>Selected nodes indexes for the selection.</value>
		public List<int> selectedNodesIndex {
			get { return _selectedNodesIndex; }
		}
        /// <summary>
        /// Selected curve ids.
        /// </summary>
        /// <value>Selected curve ids for the selection.</value>
        public List<int> selectedCurveIds {
            get { return _selectedCurveIds; }
        }
		/// <summary>
		/// Checks if one or more nodes are selected.
		/// </summary>
		/// <value><c>True</c> if there is a selection.</value>
		public bool hasSelection {
			get {
				if (_selectedNodes.Count > 0)
					return true;
				return false;
			}
		}
		/// <summary>
		/// Checks if only one node in the curve is selected.
		/// </summary>
		/// <value><c>True</c> with a single node selected.</value>
		public bool hasSingleSelection {
			get {
				if (_selectedNodes.Count == 1)
					return true;
				return false;
			}
		}
        public bool hasMultipleSelection {
            get {
                if (_selectedNodes.Count > 1)
                    return true;
                return false;
            }
        }
        #endregion

        #region Handlers Vars
        /// <summary>
        /// List of additional nodes to add to the curve.
        /// </summary>
        /// <typeparam name="Vector3">World position of the candidate node.</typeparam>
        /// <returns>List of candidate nodes.</returns>
        private List<Vector3> _nodeCandidates = new List<Vector3> ();
        /// <summary>
        /// Index of the existing node on the curve that generated the 
        /// candidate node at each position.
        /// </summary>
        /// <typeparam name="int">Index of the existing node on the curve that generated
        /// the candidate node.
        /// </typeparam>
        /// <returns>List of index nodes already on the curve.</returns>
        private List<int> _nodeCandidatesIndex = new List<int> ();
        /// <summary>
        /// Flags to mark candidate nodes once they have been added to the curve.
        /// </summary>
        /// <typeparam name="bool"><c>True</c> if the candidate node at the curve has been added.</typeparam>
        /// <returns>List of flags on added candidate nodes.</returns>
        private List<bool> _nodeCandidatesAdded = new List<bool> ();
        /// <summary>
        /// Temporary variable used to draw nodes.
        /// </summary>
        private static Vector3 s_tmpNode;
        /// <summary>
        /// Temporary variable used to draw the first handle on bezier nodes.
        /// </summary>
        private static Vector3 s_tmpHandle1;
        /// <summary>
        /// Temporary variable used to draw the second handle on bezier nodes.
        /// </summary>
        private static Vector3 s_tmpHandle2;
        private static bool s_tmpHandle1Drawn = true;
        private static bool s_tmpHandle2Drawn = true;
        /// <summary>
        /// Temporary variables for mouse positions.
        /// </summary>
        private static Vector2 s_StartMousePosition, s_CurrentMousePosition;
        /// <summary>
        /// Temporary variables for mouse dragging.
        /// </summary>
        private static Vector3 s_StartPosition;
        /// <summary>
        /// Set to false when first selecting a handle, checking against this flag marks the first movement of the handle
        /// in order to call OnBeginMove events.
        /// </summary>
        private bool _selectedHandleHasMoved = false;
        /// <summary>
        /// Set to false when first selecting a node, checking against this flag marks the first movement of the node
        /// in order to call OnBeginMove events.
        /// </summary>
        private bool _selectedNodeHasMoved = false;
        #endregion

        #region Inspector GUI Vars
        /// <summary>
		/// If <c>true</c> shows the remove node(s) button if there are nodes selected.
		/// </summary>
		public bool removeNodeButtonEnabled = true;
		/// <summary>
		/// Array of GUI contents for the edit mode buttons.
		/// </summary>
		GUIContent[] editModeOptions;
		/// <summary>
		/// Saves the index for the edit mode.
		/// </summary>
		int editModeIndex = 0;
		/// <summary>
		/// Array of GUI contents for the node handle style buttons.
		/// </summary>
		GUIContent[] handleStyleOptions;
		/// <summary>
		/// Saves the style index for a single selected node.
		/// </summary>
		int handleStyleIndex = -1;
		/// <summary>
		/// GUI content for the remove button.
		/// </summary>
		GUIContent removeNodeButtonContent;
        #endregion

        #region Delegates
        /// <summary>
        /// OnCurve delegate definition.
        /// </summary>
        public delegate void OnCurveDelegate (BezierCurve curve);
        /// <summary>
        /// OnNode delegate definition.
        /// </summary>
        /// <param name="node">Node.</param>
        public delegate void OnNodeDelegate (BezierNode node);
        /// <summary>
        /// OnMultiNodeIndex delegate definition.
        /// </summary>
        /// <param name="nodes">List of nodes.</param>
        /// <param name="index">List of indexes.</param>
        public delegate void OnMultiNodeIndexDelegate (List<BezierNode> nodes, List<int> index, List<int> curveIds);
        /// <summary>
        /// OnNodeIndex delegate definition.
        /// </summary>
        /// <param name="node">Node.</param>
        /// <param name="index">Index on the curve.</param>
        public delegate void OnNodeIndexDelegate (BezierNode node, int index);
        /// <summary>
        /// OnValidatePosition delegate definition.
        /// </summary>
        /// <param name="position">Validates if a position on the canvas is valid for a node.</param>
        /// <returns><c>True</c> if the position is valid.</returns>
        public delegate bool OnValidatePositionDelegate (Vector3 position);
        /// <summary>
        /// OnCheckPosition delegate definition.
        /// </summary>
        /// <param name="offset">Offset to check or modify.</param>
        /// <returns>Position.</returns>
        public delegate Vector3 OnCheckOffsetDelegate (Vector3 offset);
        /// <summary>
        /// OnCheckNodes delegate definition.
        /// </summary>
        /// <param name="nodes">Nodes to check.</param>
        /// <param name="indexes">Index of nodes to check.</param>
        /// <returns></returns>
        public delegate bool OnCheckNodesDelegate (List<BezierNode> nodes, List<int> indexes, List<int> curveIds);
        /// <summary>
        /// OnCheckNodeDelegate delegate definition.
        /// </summary>
        /// <param name="node">Node to check.</param>
        /// <param name="index">Index of the node to check.</param>
        /// <param name="curveId">Id of the curve the node belongs to.</param>
        /// <returns>Boolean</returns>
        public delegate bool OnCheckNodeDelegate (BezierNode node, int index, int curveId);
        /// <summary>
        /// OnCheckNodeHandlerDelegate delegate definition.
        /// </summary>
        /// <param name="node">Node to check.</param>
        /// <param name="index">Index of the node to check.</param>
        /// <param name="curveId">Id of the curve the node belongs to.</param>
        /// <param name="handle">Number of handle selected.</param>
        /// <returns>Boolean</returns>
        public delegate bool OnCheckNodeHandlerDelegate (BezierNode node, int index, int curveId, int handle);
        /// <summary>
        /// OnCheckNodeControlType delegate definition.
        /// </summary>
        /// <param name="node">Node to check.</param>
        /// <param name="index">Index of the node to check.</param>
        /// <param name="curveId">Id of the curve the node belongs to.</param>
        /// <returns>Boolean</returns>
        public delegate ControlType OnCheckNodeControlType (BezierNode node, int index, int curveId);
        /// <summary>
        /// OnBeforeNodeMove delegate definition.
        /// </summary>
        /// <param name="node">Node to move.</param>
        /// <param name="previousPosition">Previous position.</param>
        /// <param name="newPosition">New position.</param>
        /// <param name="scale">Scale on the editor.</param>
        /// <param name="index">Index of the node to move.</param>
        /// <param name="curveId">Id of the curve the node belongs to.</param>
        /// <returns>The resulting offset to apply the move, without scale applied.</returns>
        public delegate Vector3 OnBeforeNodeMove (BezierNode node, Vector3 previousPosition, Vector3 newPosition, float scale, int index, int curveId);
        /// <summary>
        /// Delegate to call when a change has been made to the curve.
        /// </summary>
        public OnCurveDelegate onCurveChanged;
        /// <summary>
        /// Delegate for node selection.
        /// </summary>
        public OnMultiNodeIndexDelegate onSelectionChanged;
        /// <summary>
        /// Delegate to call right before adding a node.
        /// </summary>
        public OnNodeDelegate onBeforeAddNode;
        /// <summary>
        /// Delegate to call when a node has been added.
        /// </summary>
        public OnNodeIndexDelegate onAddNode;
        /// <summary>
        /// Delegate to call before editing a single node.
        /// </summary>
        public OnNodeIndexDelegate onBeforeEditNode;
        /// <summary>
        /// Delegate to call after a node has been edited.
        /// </summary>
        public OnNodeIndexDelegate onEditNode;
        /// <summary>
        /// Delegate to call right before a selection of node begin moving.
        /// </summary>
        public OnMultiNodeIndexDelegate onBeginMoveNodes;
        /// <summary>
        /// Delegate to call after a selection of nodes stop moving.
        /// </summary>
        public OnMultiNodeIndexDelegate onEndMoveNodes;
        /// <summary>
        /// Delegate to call right before moving a node.
        /// </summary>
        public OnMultiNodeIndexDelegate onBeforeMoveNodes;
        /// <summary>
        /// Delegate to call when a node has moved.
        /// </summary>
        public OnMultiNodeIndexDelegate onMoveNodes;
        /// <summary>
        /// Delegate to call once when the movement of a handle start and before the offset gets applied.
        /// </summary>
        public OnCheckNodeHandlerDelegate onBeginMoveHandle;
        /// <summary>
        /// Delegate to call once when the movement of a handle has ended.
        /// </summary>
        public OnCheckNodeHandlerDelegate onEndMoveHandle;
        /// <summary>
        /// Delegate to call right before moving a node handle.
        /// </summary>
        public OnCheckNodeHandlerDelegate onBeforeMoveHandle;
        /// <summary>
        /// Delegate to call when a node handle has moved.
        /// </summary>
        public OnCheckNodeHandlerDelegate onMoveHandle;
        /// <summary>
        /// Delegate to call when a handle has been selected.
        /// </summary>
        public OnCheckNodeHandlerDelegate onSelectHandle;
        /// <summary>
        /// Delegate to call when a handle loses focus.
        /// </summary>
        public OnCheckNodeHandlerDelegate onDeselectHandle;
        /// <summary>
        /// Delegate to call right before removing nodes.
        /// </summary>
        public OnMultiNodeIndexDelegate onBeforeRemoveNodes;
        /// <summary>
        /// Delegate to call when nodes has been removed.
        /// </summary>
        public OnMultiNodeIndexDelegate onRemoveNodes;
        /// <summary>
        /// Delegate to call when validating if a node should be added based on its position.
        /// </summary>
        public OnValidatePositionDelegate onValidateAddNode;
        /// <summary>
        /// Delegate to call when moving nodes.
        /// </summary>
        public OnCheckOffsetDelegate onCheckMoveNodes;
        /// <summary>
        /// Delegate to call to check if deleting nodes is valid or not.
        /// </summary>
        public OnCheckNodesDelegate onCheckRemoveNodes;
        /// <summary>
        /// Delegate to call to check if a node should be drawn.
        /// </summary>
        public OnCheckNodeDelegate onCheckDrawNode;
        /// <summary>
        /// If a node is going to be drawn, this function controls wheter this includes drawing its controls.
        /// </summary>
        public OnCheckNodeControlType onCheckNodeControls;
        public OnBeforeNodeMove onBeforeFreeMove;
        public OnBeforeNodeMove onBeforeSliderMove;
        #endregion

        // TODO: singleton.

        #region Processing
        /// <summary>
		/// Clear this instance.
		/// </summary>
		public void Clear () {
			ClearSelection (true);
			_curve = null;
            _nodeCandidates.Clear ();
            _nodeCandidatesAdded.Clear ();
            _nodeCandidatesIndex.Clear ();
		}
        /// <summary>
        /// Analyzes the curve and generated candidate node between existing ones.
        /// </summary>
        public void SetAddNodeCandidates () {
            _nodeCandidates.Clear ();
            _nodeCandidatesAdded.Clear ();
            _nodeCandidatesIndex.Clear ();
            bool shouldAdd = true;
            for (int i = 0; i < _curve.bezierCurves.Count; i++) {
                for (int j = 0; j < _curve.bezierCurves[i].samples.Count; j++) {
                    if (shouldAdd) {
                        _nodeCandidates.Add (_curve.bezierCurves[i].samples[j].position);
                        _nodeCandidatesIndex.Add (i);
                        _nodeCandidatesAdded.Add (false);
                    }
                    shouldAdd = !shouldAdd;
                }
            }
        }
        #endregion

        #region Scene and Inspector GUI
        public void OnEnable () {
            editModeOptions = new GUIContent[] {
				new GUIContent ("Selection Mode", "Lets you move, modify and delete nodes"),
				new GUIContent ("Addition Mode", "Lets you add new nodes to the curve")
			};
			editModeIndex = (int)editMode;
			handleStyleOptions = new GUIContent[] {
				new GUIContent ("Connected", "Connected"),
				new GUIContent ("Broken", "Broken"),
				new GUIContent ("None", "None")
			};
			handleStyleIndex = -1;
			removeNodeButtonContent = new GUIContent ("Remove Node", "Remove Node");
            ClearSelection (true);
        }
        /// <summary>
        /// Draws the curve and editor tools to the scene view.
        /// </summary>
        /// <param name="curve">Curve to draw.</param>
        /// <param name="sceneOffset">Offset position for the curve.</param>
        public void OnSceneGUI (BezierCurve curve, Vector3 sceneOffset, bool isSelected = false) {
            _curve = curve;
            if (_editMode == EditMode.Selection) { // Selection mode.
                selectionHappened = false;
                for (int i = 0; i < curve.nodeCount; i++) {
                    DrawBezierNode (curve[i], i, sceneOffset, scale, (i == curve.nodeCount - 1), null, showHandles);
                }
                if (Event.current.type == EventType.KeyDown) {
                    switch (Event.current.keyCode) {
                        case KeyCode.Delete:
                            RemoveSelectedNodes ();
                            Event.current.Use ();
                        break;
                    }
                }
            } else { // Addition mode.
                float handleSize = HandleUtility.GetHandleSize (s_tmpNode) * extraSmallNodeSize;
                // Draw existing nodes.
                for (int i = 0; i < _curve.nodeCount; i++) {
                    Handles.DrawSolidDisc ((_curve[i].position * scale) + sceneOffset,
                        Camera.current.transform.forward, 
                        handleSize);
                }
                // Draw add node candidates.
                DrawAddNodeHandles (sceneOffset, scale, Handles.CircleHandleCap);
                
				if (Event.current.keyCode == KeyCode.Escape) {
					editMode = EditMode.Selection;
				}
            }

            // TODO: use cache of nodes for drawing.
            BezierCurveDraw.DrawCurve (curve, sceneOffset, scale, (isSelected?selectedCurveColor:curveColor), curveWidth, 6);
            // Draw debug of points.
            if (debugEnabled) {
                BezierCurveDraw.DrawCurvePoints (curve, sceneOffset, scale, Color.white, 
                    debugShowPointForward, debugShowPointNormal, debugShowPointUp, debugShowPointTangent);
            }
        }
        /// <summary>
        /// Draws the curve and editor tools to the scene view.
        /// </summary>
        /// <param name="curve">Curve to draw.</param>
        /// <param name="sceneOffset">Offset position for the curve.</param>
        public void OnSceneGUIDrawSingleNode (BezierCurve curve, int index, Vector3 sceneOffset, bool isSelected = false, ControlType ctrlType = ControlType.FreeMove, bool drawHandles = false) {
            _curve = curve;
            if (_editMode == EditMode.Selection) { // Selection mode.
                selectionHappened = false;
                for (int i = 0; i < curve.nodeCount; i++) {
                    if (i == index) {
                        DrawBezierNode (curve[i], i, sceneOffset, scale, (i == curve.nodeCount - 1), ctrlType, drawHandles && showHandles);
                        break;
                    }
                }
                if (Event.current.type == EventType.KeyDown) {
                    switch (Event.current.keyCode) {
                        case KeyCode.Delete:
                            RemoveSelectedNodes ();
                            Event.current.Use ();
                        break;
                    }
                }
            }
        }
        public bool OnInspectorGUI (BezierCurve curve) {
            bool change = false;

            // Resolution Angle
            float resolutionAngle = EditorGUILayout.FloatField (_curve.resolutionAngle);
            if (resolutionAngle != _curve.resolutionAngle) {
                _curve.resolutionAngle = resolutionAngle;
                _curve.Process ();
            }

            // Edit mode.
			editModeIndex = (int)editMode;
			editModeIndex = GUILayout.SelectionGrid (editModeIndex, editModeOptions, 1);
			if (editModeIndex != (int)editMode) {
				editMode = (BezierCurveEditor.EditMode)editModeIndex;
				Event.current.Use ();
			}

			// Node handle style.
			handleStyleIndex = -1;
			if (hasSingleSelection) {
				handleStyleIndex = (int)selectedNode.handleStyle;
			}
			EditorGUILayout.Separator (); // TODO: have a "lastAction" exposed with an enum value to consult the last action performed on this editor.
			EditorGUI.BeginDisabledGroup (!hasSingleSelection); // TODO. Enable mode buttons that differ to the selected one.
			handleStyleIndex = GUILayout.SelectionGrid (handleStyleIndex, handleStyleOptions, 1 /*num of cols*/);
			if (hasSingleSelection && handleStyleIndex != (int)selectedNode.handleStyle) {
				// TODO
				ChangeNodeHandleStyle (selectedNode, selectedNodeIndex, (BezierNode.HandleStyle)handleStyleIndex, (selectedNodeIndex == _curve.nodeCount - 1));
				Event.current.Use ();
			}
			EditorGUI.EndDisabledGroup ();

            // Snap selected nodes to axis.
            EditorGUILayout.Space ();
            EditorGUI.BeginDisabledGroup (!hasSelection);
            if (GUILayout.Button ("Snap Selection to X Axis")) {
                SnapSelectedNodes (BezierCurve.Axis.X);
            }
            if (GUILayout.Button ("Snap Selection to Y Axis")) {
                SnapSelectedNodes (BezierCurve.Axis.Y);
            }
            if (GUILayout.Button ("Snap Selection to Z Axis")) {
                SnapSelectedNodes (BezierCurve.Axis.Z);
            }
			EditorGUI.EndDisabledGroup ();

			// Remove node button.
			EditorGUILayout.Separator ();
			EditorGUI.BeginDisabledGroup (!removeNodeButtonEnabled || !hasSelection);
			if (GUILayout.Button (removeNodeButtonContent)) {
				RemoveSelectedNodes ();
			}
			EditorGUI.EndDisabledGroup ();

            if (debugEnabled) {
                EditorGUILayout.Space ();
                debugShowCurvePoints = EditorGUILayout.Toggle ("Show Curve Points", debugShowCurvePoints);
                debugShowCurveFinePoints = EditorGUILayout.Toggle ("Show Curve Fine Points", debugShowCurveFinePoints);
                debugShowNodeLabels = EditorGUILayout.Toggle ("Show Node Labels", debugShowNodeLabels);
                if (GUILayout.Button ("Print Debug Info")) {
                    PrintDebugInfo ();
                }
            }

            return change;
        }
        #endregion
        
        #region Draw and Handles
        /// <summary>
        /// Draws a bezier node.
        /// </summary>
        /// <param name="node">Bezier node to draw.</param>
        /// <param name="index">Index of the bezier node on the curve.</param>
        /// <param name="sceneOffset">Offset of the curve.</param>
        /// <param name="scale">Scale of the curve.</param>
        /// <param name="isLastNode">Node is at the end of the curve.</param>
        void DrawBezierNode (BezierNode node, int index, Vector3 sceneOffset, float scale, bool isLastNode, ControlType? ctrlToDraw = null, bool drawHandles = true) {
            // Check if this node should be drawn.
            bool shouldDraw = true;
            if (ctrlToDraw == null && onCheckDrawNode != null) {
				shouldDraw = onCheckDrawNode (node, index, (_nodeToCurve.ContainsKey(node.guid)?_nodeToCurve[node.guid]:-1));
			}
            if (!shouldDraw) {
                return;
            }

            s_tmpNode = (node.position * scale) + sceneOffset;
            float handleSize = HandleUtility.GetHandleSize (s_tmpNode);
            _tmpColor = Handles.color;
            
            if (debugEnabled && debugShowNodeLabels) {
                Handles.color = nodeLabelColor;
                Handles.Label (node.position + new Vector3 (0, handleSize * labelSize, 0), 
                    "Node " +  index);
            }

            // Draw controls for the node
            if (ctrlToDraw == null) {
                ctrlToDraw = ControlType.FreeMove;
                if (onCheckNodeControls != null) {
                    ctrlToDraw = onCheckNodeControls(node, index, (_nodeToCurve.ContainsKey(node.guid)?_nodeToCurve[node.guid]:-1));
                }
            }
            int ctrlId = GUIUtility.GetControlID (FocusType.Passive);
            switch (ctrlToDraw) {
                case ControlType.FreeMove:
                    Vector3 newPosition;
                    if (node.isSelected) {
                        int hotControl = GUIUtility.hotControl;
                        Handles.color = selectedNodeColor;
                        FreeMoveHandle (ctrlId, s_tmpNode, Quaternion.identity, handleSize * nodeSize,
                            Vector3.zero, selectedNodeDrawFunction);
                        newPosition = Handles.PositionHandle (s_tmpNode, Quaternion.identity);
                        if (hotControl != GUIUtility.hotControl) {
                            if (GUIUtility.hotControl == 0) {
                                // TODO:  Call OnDeselectNode
                                // Call OnEndMoveNode
                                if (_selectedNodeHasMoved && onEndMoveHandle != null) {
                                    onEndMoveNodes (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);
                                }
                            } else {
                                //TODO: Call OnSelectNode
                            }
                            _selectedNodeHasMoved = false;
                        }
                        if (newPosition != s_tmpNode) {
                            Vector3 moveOffset = newPosition - s_tmpNode;
                            // OnBeginFreeMove
                            if (!_selectedNodeHasMoved) {
                                if (onBeginMoveNodes != null) {
                                    lastKnownOffset = moveOffset / scale;
                                    offsetStep = lastKnownOffset;
                                    onBeginMoveNodes (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);
                                }
                                _selectedNodeHasMoved = true;
                            }
                            // OnBeforeFreeMove
                            if (onBeforeFreeMove != null) {
                                moveOffset = onBeforeFreeMove (node, s_tmpNode, newPosition, scale, index, curveId);
                            }
                            if (moveOffset != Vector3.zero) {
                                MoveSelectedNodes (moveOffset / scale);
                            }
                        }
                    } else {
                        Handles.color = nodeColor;
                        newPosition = FreeMoveHandle (ctrlId, s_tmpNode, Quaternion.identity, handleSize * nodeSize,
                            Vector3.zero, nodeDrawFunction);
                    }
                    break;
                case ControlType.SliderMove:
                    Vector3 newSliderPosition;
                    if (node.isSelected) {
                        Handles.color = selectedNodeColor;
                        SliderMoveHandle (ctrlId, s_tmpNode, Quaternion.identity, handleSize * nodeSize,
                                Vector3.zero, nodeDrawFunction);
                        newSliderPosition = Handles.Slider (s_tmpNode, sliderVector);
                        if (newSliderPosition != s_tmpNode) {
                            Vector3 moveOffset = newSliderPosition - s_tmpNode;
                            if (onBeforeSliderMove != null) {
                                moveOffset = onBeforeSliderMove (node, s_tmpNode, newSliderPosition, scale, index, curveId);
                            }
                            if (moveOffset != Vector3.zero) {
                                MoveSelectedNodes (moveOffset / scale);
                            }
                        }
                    } else {
                        Handles.color = nodeColor;
                        newSliderPosition = FreeMoveHandle (ctrlId, s_tmpNode, Quaternion.identity, handleSize * nodeSize,
                            Vector3.zero, nodeDrawFunction);
                    }
                    break;
                case ControlType.DrawOnly:
                    Handles.color = nodeColor;
                    DummyHandle (ctrlId, s_tmpNode, Camera.current.transform.rotation,
                        handleSize * nodeSize, nodeDrawFunction);
                    break;
                case ControlType.None:
                default:
                    break;
            }

            if (drawHandles) {
                // Draw handles.
                s_tmpHandle1Drawn = false;
                s_tmpHandle2Drawn = false;
                if (node.handleStyle != BezierNode.HandleStyle.None) {
                    Handles.color = node.isSelected?selectedNodeHandleColor:nodeHandleColor;
                    // Conditional second handle first drawn.
                    if (isLastNode && (showSecondHandleAlways || _curve.closed)) {
                        DrawBezierNodeHandle2 (node, index, sceneOffset, scale, isLastNode);
                        s_tmpHandle2Drawn = true;
                    }
                    // First handle.
                    if (!(index == 0 && node.handleStyle != BezierNode.HandleStyle.Aligned) || showFirstHandleAlways || _curve.closed) {
                        DrawBezierNodeHandle1 (node, index, sceneOffset, scale, isLastNode);
                        s_tmpHandle1Drawn = true;
                    }
                    // Second handle.
                    if (!(isLastNode && node.handleStyle != BezierNode.HandleStyle.Aligned)) {
                        DrawBezierNodeHandle2 (node, index, sceneOffset, scale, isLastNode);
                        s_tmpHandle2Drawn = true;
                    }

                    Handles.color = nodeHandleLineColor;
                    // First handle line
                    if (s_tmpHandle1Drawn) {
                        Handles.DrawDottedLine(s_tmpNode, s_tmpHandle1, 4f);
                    }
                    // Second handle line
                    if (s_tmpHandle2Drawn) {
                        Handles.DrawDottedLine(s_tmpNode, s_tmpHandle2, 4f);
                    }
                }
            }

            // Node has been selected
            if (selectionHappened) {
                selectionHappened = ManageNodeToSelection (node,
                    index,
                    (Event.current.shift || Event.current.control),
                    !Event.current.control);
                if (selectionHappened && onSelectionChanged != null)
                    onSelectionChanged (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);
                selectionHappened = false;
            }

            Handles.color = _tmpColor;
        }
        void DrawBezierNodeHandle1 (BezierNode node, int index, Vector3 sceneOffset, float scale, bool isLastNode) {
            int hotControl = GUIUtility.hotControl;
            s_tmpHandle1 = node.globalHandle1 * scale + sceneOffset;
            Vector3 newGlobal1 = Handles.FreeMoveHandle (
                s_tmpHandle1, 
                Quaternion.identity, 
                HandleUtility.GetHandleSize (s_tmpHandle1) * nodeHandleSize, 
                Vector3.zero, 
                node.isSelected?selectedNodeHandleDrawFunction:nodeHandleDrawFunction);
            if (hotControl != GUIUtility.hotControl) {
                if (GUIUtility.hotControl == 0) {
                    // Call OnDeselectHandle
                    if (onDeselectHandle != null) {
                        onDeselectHandle (node, index, (_nodeToCurve.ContainsKey(node.guid)?_nodeToCurve[node.guid]:-1), 1);
                    }
                    // Call OnEndMoveHandle
                    if (_selectedHandleHasMoved && onEndMoveHandle != null) {
                        onEndMoveHandle (node, index, (_nodeToCurve.ContainsKey(node.guid)?_nodeToCurve[node.guid]:-1), 1);
                    }
                } else {
                    // Call OnSelectHandle
                    if (onSelectHandle != null) {
                        onSelectHandle (node, index, (_nodeToCurve.ContainsKey(node.guid)?_nodeToCurve[node.guid]:-1), 1);
                    }
                }
                _selectedHandleHasMoved = false;
            }
            if (s_tmpHandle1 != newGlobal1) {
                // Call OnBeginMoveHandle
                if (!_selectedHandleHasMoved) {
                    if (onBeginMoveHandle != null) {
                        onBeginMoveHandle (node, index, (_nodeToCurve.ContainsKey(node.guid)?_nodeToCurve[node.guid]:-1), 1);
                    }
                    _selectedHandleHasMoved = true;
                }
                // Call OnBeforeMoveHandle
                if (onBeforeMoveHandle != null)
                    onBeforeMoveHandle (node, index, (_nodeToCurve.ContainsKey(node.guid)?_nodeToCurve[node.guid]:-1), 1);
                node.globalHandle1 = (newGlobal1 - sceneOffset) / scale;
                // Call OnMoveHandle
                if (onMoveHandle != null) {
                    onMoveHandle (node, index, (_nodeToCurve.ContainsKey(node.guid)?_nodeToCurve[node.guid]:-1), 1);
                }
                CallCurveChangedDelegate ();
            }
        }
        void DrawBezierNodeHandle2 (BezierNode node, int index, Vector3 sceneOffset, float scale, bool isLastNode) {
            int hotControl = GUIUtility.hotControl;
            s_tmpHandle2 = node.globalHandle2 * scale + sceneOffset;
            Vector3 newGlobal2 = Handles.FreeMoveHandle(
                s_tmpHandle2, 
                Quaternion.identity, 
                HandleUtility.GetHandleSize (s_tmpHandle2) * nodeHandleSize, 
                Vector3.zero, 
                node.isSelected?selectedNodeHandleDrawFunction:nodeHandleDrawFunction);
            if (hotControl != GUIUtility.hotControl) {
                if (GUIUtility.hotControl == 0) {
                    // Call OnDeselectHandle
                    if (onDeselectHandle != null) {
                        onDeselectHandle (node, index, (_nodeToCurve.ContainsKey(node.guid)?_nodeToCurve[node.guid]:-1), 2);
                    }
                    // Call OnEndMoveHandle
                    if (_selectedHandleHasMoved && onEndMoveHandle != null) {
                        onEndMoveHandle (node, index, (_nodeToCurve.ContainsKey(node.guid)?_nodeToCurve[node.guid]:-1), 2);
                    }
                } else {
                    // Call OnSelectHandle
                    if (onSelectHandle != null) {
                        onSelectHandle (node, index, (_nodeToCurve.ContainsKey(node.guid)?_nodeToCurve[node.guid]:-1), 2);
                    }
                }
                _selectedHandleHasMoved = false;
            }
            if (s_tmpHandle2 != newGlobal2) {
                // Call OnBeginMoveHandle
                if (!_selectedHandleHasMoved) {
                    if (onBeginMoveHandle != null) {
                        onBeginMoveHandle (node, index, (_nodeToCurve.ContainsKey(node.guid)?_nodeToCurve[node.guid]:-1), 2);
                    }
                    _selectedHandleHasMoved = true;
                }
                if (onBeforeMoveHandle != null)
                    onBeforeMoveHandle (node, index, (_nodeToCurve.ContainsKey(node.guid)?_nodeToCurve[node.guid]:-1), 2);
                node.globalHandle2 = (newGlobal2 - sceneOffset) / scale;
                if (onMoveHandle != null) {
                    onMoveHandle (node, index, (_nodeToCurve.ContainsKey(node.guid)?_nodeToCurve[node.guid]:-1), 2);
                }
                CallCurveChangedDelegate ();
            }
        }
		/// <summary>
		/// Free move handle tool based on Unity method.
		/// </summary>
		/// <param name="id">Handle control identifier.</param>
		/// <param name="position">Position in world space.</param>
		/// <param name="rotation">Rotation.</param>
		/// <param name="size">Size of the handle.</param>
		/// <param name="snap">Snap to grid.</param>
		/// <param name="handleFunction"></param>
		/// <returns>The position of the handle, dragged or not.</returns>
        public Vector3 FreeMoveHandle (int id, Vector3 position, Quaternion rotation, float size, Vector3 snap, Handles.CapFunction handleFunction) {
            Vector3 worldPosition = Handles.matrix.MultiplyPoint (position);
            Matrix4x4 origMatrix = Handles.matrix;
            //VertexSnapping.HandleMouseMove(id); TODO: Version
            Event evt = Event.current;
            switch (evt.GetTypeForControl(id)) {
                case EventType.Layout:
                    // We only want the position to be affected by the Handles.matrix.
                    Handles.matrix = Matrix4x4.identity;
                    handleFunction (id, worldPosition, Camera.current.transform.rotation, size, EventType.Layout);
                    Handles.matrix = origMatrix;
                    break;
                case EventType.MouseDown:
                    if (HandleUtility.nearestControl == id && evt.button == 0) {
                        GUIUtility.hotControl = id;     // Grab mouse focus
                        s_CurrentMousePosition = s_StartMousePosition = evt.mousePosition;
                        s_StartPosition = position;
                        //HandleUtility.ignoreRaySnapObjects = null; TODO: version...
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                        selectionHappened = true;
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id) {
                        bool rayDrag = EditorGUI.actionKey && evt.shift;
                        if (rayDrag) {
                            /* TODO: version
                            if (HandleUtility.ignoreRaySnapObjects == null)
                                Handles.SetupIgnoreRaySnapObjects();
                                */
                            object hit = HandleUtility.RaySnap (HandleUtility.GUIPointToWorldRay(evt.mousePosition));
                            if (hit != null) {
                                RaycastHit rh = (RaycastHit)hit;
                                float offset = 0;
                                /* TODO: ??
                                if (Tools.pivotMode == PivotMode.Center)
                                {
                                    float geomOffset = HandleUtility.CalcRayPlaceOffset(HandleUtility.ignoreRaySnapObjects, rh.normal);
                                    if (geomOffset != Mathf.Infinity)
                                    {
                                        offset = Vector3.Dot(position, rh.normal) - geomOffset;
                                    }
                                }
                                */
                                position = Handles.inverseMatrix.MultiplyPoint (rh.point + (rh.normal * offset));
                            } else {
                                rayDrag = false;
                            }
                        }
                        if (!rayDrag) {
                            // normal drag
                            s_CurrentMousePosition += new Vector2 (evt.delta.x, -evt.delta.y) * EditorGUIUtility.pixelsPerPoint;
                            Vector3 screenPos = Camera.current.WorldToScreenPoint (Handles.matrix.MultiplyPoint (s_StartPosition));
                            screenPos += (Vector3)(s_CurrentMousePosition - s_StartMousePosition);
                            position = Handles.inverseMatrix.MultiplyPoint (Camera.current.ScreenToWorldPoint (screenPos));

                            // Due to floating node inaccuracies, the back-and-forth transformations used may sometimes introduce
                            // tiny unintended movement in wrong directions. People notice when using a straight top/left/right ortho camera.
                            // In that case, just restrain the movement to the plane.
                            if (Camera.current.transform.forward == Vector3.forward || Camera.current.transform.forward == -Vector3.forward)
                                position.z = s_StartPosition.z;
                            if (Camera.current.transform.forward == Vector3.up || Camera.current.transform.forward == -Vector3.up)
                                position.y = s_StartPosition.y;
                            if (Camera.current.transform.forward == Vector3.right || Camera.current.transform.forward == -Vector3.right)
                                position.x = s_StartPosition.x;

                            /* TODO: ??
                            if (Tools.vertexDragging)
                            {
                                if (HandleUtility.ignoreRaySnapObjects == null)
                                    Handles.SetupIgnoreRaySnapObjects();
                                Vector3 near;
                                if (HandleUtility.FindNearestVertex(evt.mousePosition, null, out near)) {
                                    position = Handles.inverseMatrix.MultiplyNode(near);
                                }
                            }
                            */
                            if (EditorGUI.actionKey && !evt.shift) {
                                Vector3 delta = position - s_StartPosition;
                                delta.x = Handles.SnapValue (delta.x, snap.x);
                                delta.y = Handles.SnapValue (delta.y, snap.y);
                                delta.z = Handles.SnapValue (delta.z, snap.z);
                                position = s_StartPosition + delta;
                            }
                        }
                        GUI.changed = true;
                        evt.Use();
                    }
                    break;
                case EventType.MouseUp:
                    int hotcontrol = GUIUtility.hotControl;
                    if (GUIUtility.hotControl == id && (evt.button == 0 || evt.button == 2)) {
                        GUIUtility.hotControl = 0;
                        //HandleUtility.ignoreRaySnapObjects = null; TODO: version
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping (0);
                    }
                    break;
                case EventType.MouseMove:
                    if (id == HandleUtility.nearestControl)
                        HandleUtility.Repaint ();
                    break;
                case EventType.Repaint:
                    Color temp = Handles.color;

                    if (id == GUIUtility.hotControl) {
                        temp = Handles.color;
                        Handles.color = Handles.selectedColor;
                    } else if (id == HandleUtility.nearestControl && GUIUtility.hotControl == 0) {
                        temp = Handles.color;
                        Handles.color = preselectedNodeColor;
                    }

                    // We only want the position to be affected by the Handles.matrix.
                    Handles.matrix = Matrix4x4.identity;
                    handleFunction (id, worldPosition, Camera.current.transform.rotation, size, EventType.Repaint);
                    Handles.matrix = origMatrix;

                    if (id == GUIUtility.hotControl || id == HandleUtility.nearestControl && GUIUtility.hotControl == 0)
                        Handles.color = temp;
                    break;
            }
            return position;
        }
        /// <summary>
		/// Slider move handle tool based on Unity method.
		/// </summary>
		/// <param name="id">Handle control identifier.</param>
		/// <param name="position">Position in world space.</param>
		/// <param name="rotation">Rotation.</param>
		/// <param name="size">Size of the handle.</param>
		/// <param name="snap">Snap to grid.</param>
		/// <param name="handleFunction"></param>
		/// <returns>The position of the handle, dragged or not.</returns>
        public Vector3 SliderMoveHandle (int id, Vector3 position, Quaternion rotation, float size, Vector3 snap, Handles.CapFunction handleFunction) {
            Vector3 worldPosition = Handles.matrix.MultiplyPoint (position);
            Matrix4x4 origMatrix = Handles.matrix;
            //VertexSnapping.HandleMouseMove(id); TODO: Version
            Event evt = Event.current;
            switch (evt.GetTypeForControl(id)) {
                case EventType.Layout:
                    // We only want the position to be affected by the Handles.matrix.
                    Handles.matrix = Matrix4x4.identity;
                    handleFunction (id, worldPosition, Camera.current.transform.rotation, size, EventType.Layout);
                    Handles.matrix = origMatrix;
                    break;
                case EventType.MouseDown:
                    if (HandleUtility.nearestControl == id && evt.button == 0) {
                        GUIUtility.hotControl = id;     // Grab mouse focus
                        s_CurrentMousePosition = s_StartMousePosition = evt.mousePosition;
                        s_StartPosition = position;
                        //HandleUtility.ignoreRaySnapObjects = null; TODO: version...
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                        selectionHappened = true;
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id) {
                        bool rayDrag = EditorGUI.actionKey && evt.shift;
                        if (rayDrag) {
                            /* TODO: version
                            if (HandleUtility.ignoreRaySnapObjects == null)
                                Handles.SetupIgnoreRaySnapObjects();
                                */
                            object hit = HandleUtility.RaySnap (HandleUtility.GUIPointToWorldRay(evt.mousePosition));
                            if (hit != null) {
                                RaycastHit rh = (RaycastHit)hit;
                                float offset = 0;
                                /* TODO: ??
                                if (Tools.pivotMode == PivotMode.Center)
                                {
                                    float geomOffset = HandleUtility.CalcRayPlaceOffset(HandleUtility.ignoreRaySnapObjects, rh.normal);
                                    if (geomOffset != Mathf.Infinity)
                                    {
                                        offset = Vector3.Dot(position, rh.normal) - geomOffset;
                                    }
                                }
                                */
                                position = Handles.inverseMatrix.MultiplyPoint (rh.point + (rh.normal * offset));
                            } else {
                                rayDrag = false;
                            }
                        }
                        if (!rayDrag) {
                            // normal drag
                            s_CurrentMousePosition += new Vector2 (evt.delta.x, -evt.delta.y) * EditorGUIUtility.pixelsPerPoint;
                            Vector3 screenPos = Camera.current.WorldToScreenPoint (Handles.matrix.MultiplyPoint (s_StartPosition));
                            screenPos += (Vector3)(s_CurrentMousePosition - s_StartMousePosition);
                            position = Handles.inverseMatrix.MultiplyPoint (Camera.current.ScreenToWorldPoint (screenPos));

                            // Due to floating node inaccuracies, the back-and-forth transformations used may sometimes introduce
                            // tiny unintended movement in wrong directions. People notice when using a straight top/left/right ortho camera.
                            // In that case, just restrain the movement to the plane.
                            if (Camera.current.transform.forward == Vector3.forward || Camera.current.transform.forward == -Vector3.forward)
                                position.z = s_StartPosition.z;
                            if (Camera.current.transform.forward == Vector3.up || Camera.current.transform.forward == -Vector3.up)
                                position.y = s_StartPosition.y;
                            if (Camera.current.transform.forward == Vector3.right || Camera.current.transform.forward == -Vector3.right)
                                position.x = s_StartPosition.x;

                            /* TODO: ??
                            if (Tools.vertexDragging)
                            {
                                if (HandleUtility.ignoreRaySnapObjects == null)
                                    Handles.SetupIgnoreRaySnapObjects();
                                Vector3 near;
                                if (HandleUtility.FindNearestVertex(evt.mousePosition, null, out near)) {
                                    position = Handles.inverseMatrix.MultiplyNode(near);
                                }
                            }
                            */
                            if (EditorGUI.actionKey && !evt.shift) {
                                Vector3 delta = position - s_StartPosition;
                                delta.x = Handles.SnapValue (delta.x, snap.x);
                                delta.y = Handles.SnapValue (delta.y, snap.y);
                                delta.z = Handles.SnapValue (delta.z, snap.z);
                                position = s_StartPosition + delta;
                            }
                        }
                        GUI.changed = true;
                        evt.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && (evt.button == 0 || evt.button == 2)) {
                        GUIUtility.hotControl = 0;
                        //HandleUtility.ignoreRaySnapObjects = null; TODO: version
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping (0);
                    }
                    break;
                case EventType.MouseMove:
                    if (id == HandleUtility.nearestControl)
                        HandleUtility.Repaint ();
                    break;
                case EventType.Repaint:
                    Color temp = Handles.color;

                    if (id == GUIUtility.hotControl) {
                        temp = Handles.color;
                        Handles.color = Handles.selectedColor;
                    } else if (id == HandleUtility.nearestControl && GUIUtility.hotControl == 0) {
                        temp = Handles.color;
                        Handles.color = preselectedNodeColor;
                    }

                    // We only want the position to be affected by the Handles.matrix.
                    Handles.matrix = Matrix4x4.identity;
                    handleFunction (id, worldPosition, Camera.current.transform.rotation, size, EventType.Repaint);
                    Handles.matrix = origMatrix;

                    if (id == GUIUtility.hotControl || id == HandleUtility.nearestControl && GUIUtility.hotControl == 0)
                        Handles.color = temp;
                    break;
            }
            return position;
        }
        public void DummyHandle (int id, Vector3 position, Quaternion rotation, float size, Handles.CapFunction handleFunction) {
            Vector3 worldPosition = Handles.matrix.MultiplyPoint (position);
            Matrix4x4 origMatrix = Handles.matrix;
            Event evt = Event.current;
            switch (evt.GetTypeForControl(id)) {
                case EventType.Layout:
                    // We only want the position to be affected by the Handles.matrix.
                    Handles.matrix = Matrix4x4.identity;
                    handleFunction (id, worldPosition, Camera.current.transform.rotation, size, EventType.Layout);
                    Handles.matrix = origMatrix;
                    break;
                case EventType.MouseDown:
                    if (HandleUtility.nearestControl == id && evt.button == 0) {
                        GUIUtility.hotControl = id;     // Grab mouse focus
                        s_CurrentMousePosition = s_StartMousePosition = evt.mousePosition;
                        s_StartPosition = position;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                        //selectionHappened = true;
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && (evt.button == 0 || evt.button == 2)) {
                        GUIUtility.hotControl = 0;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping (0);
                    }
                    break;
                case EventType.Repaint:
                    Color temp = Handles.color;

                    // We only want the position to be affected by the Handles.matrix.
                    Handles.matrix = Matrix4x4.identity;
                    handleFunction (id, worldPosition, Camera.current.transform.rotation, size, EventType.Repaint);
                    Handles.matrix = origMatrix;

                    if (id == GUIUtility.hotControl || id == HandleUtility.nearestControl && GUIUtility.hotControl == 0)
                        Handles.color = temp;
                    break;
            }
        }
		/// <summary>
		/// Draw the node candidates to add to the curve.
		/// </summary>
		/// <param name="sceneOffset">Offset of the curve.</param>
		/// <param name="scale">Scale of the curve.</param>
		/// <param name="handleFunction">Handle function for drawing.</param>
        void DrawAddNodeHandles (Vector3 sceneOffset, float scale, Handles.CapFunction handleFunction) {
            Vector3 position, worldPosition;
            for (int i = 0; i < _nodeCandidates.Count; i++) {
                int id = GUIUtility.GetControlID (FocusType.Passive);
                position = (_nodeCandidates [i] * scale) + sceneOffset;
                worldPosition = Handles.matrix.MultiplyPoint (position);
                Matrix4x4 origMatrix = Handles.matrix;
                Event evt = Event.current;
                float handleSize = HandleUtility.GetHandleSize (position) * extraSmallNodeSize;

                if (_nodeCandidatesAdded [i] == true) {
                    Handles.color = Color.white;
                    Handles.matrix = Matrix4x4.identity;
                    Handles.DrawSolidDisc (worldPosition, Camera.current.transform.forward, handleSize);
                    Handles.matrix = origMatrix;
                } else {
                    switch (evt.GetTypeForControl(id)) {
                        case EventType.Layout:
                            // We only want the position to be affected by the Handles.matrix.
                            Handles.matrix = Matrix4x4.identity;
                            handleFunction (id, worldPosition, Camera.current.transform.rotation, handleSize, EventType.Layout);
                            Handles.matrix = origMatrix;
                            break;
                        case EventType.MouseDown:
                            if (HandleUtility.nearestControl == id && evt.button == 0) {
                                GUIUtility.hotControl = id;     // Grab mouse focus
                                s_CurrentMousePosition = s_StartMousePosition = evt.mousePosition;
                                s_StartPosition = position;
                                //HandleUtility.ignoreRaySnapObjects = null; TODO: version...
                                evt.Use();
                                EditorGUIUtility.SetWantsMouseJumping(1);
                                if (!_nodeCandidatesAdded [i]) {
                                    _nodeCandidatesAdded [i] = true;
                                    GUIUtility.hotControl = 0;
                                    
                                    
                                    BezierNode newNode = new BezierNode (_nodeCandidates [i]);
                                    /*
                                    Vector3 tangent = _curve.GetTangentAt (t);
                                    newNode.handle1 = -tangent.normalized * 0.3f;
                                    newNode.handle2 = tangent.normalized * 0.3f;
                                    */
                                    if (onBeforeAddNode != null) {
                                        onBeforeAddNode (newNode);
                                    }
                                    _curve.InsertNode (_nodeCandidatesIndex[i] + 1, newNode);
                                    SetAddNodeCandidates ();
                                    if (onAddNode != null) {
                                        onAddNode (newNode, i + 1);
                                        CallCurveChangedDelegate ();
                                    }
                                    return;
                                }
                            }
                            break;
                        case EventType.MouseUp:
                            if (GUIUtility.hotControl == id && (evt.button == 0 || evt.button == 2)) {
                                GUIUtility.hotControl = 0;
                                //HandleUtility.ignoreRaySnapObjects = null; TODO: version
                                evt.Use();
                                EditorGUIUtility.SetWantsMouseJumping (0);
                            }
                            break;
                        case EventType.MouseMove:
                            if (id == HandleUtility.nearestControl)
                                HandleUtility.Repaint ();
                            break;
                        case EventType.Repaint:
                            Color temp = Color.white;

                            if (id == GUIUtility.hotControl) {
                                temp = Handles.color;
                                Handles.color = Color.green;//Handles.selectedColor;
                            } else if (id == HandleUtility.nearestControl && GUIUtility.hotControl == 0) {
                                temp = Handles.color;
                                Handles.color = Color.red; //preselectedNodeColor;
                            } else {
                                temp = Handles.color;
                                Handles.color = Color.blue;//new Color (0.6f, 0.6f, 0.6f, 1f);
                            }

                            // We only want the position to be affected by the Handles.matrix.
                            Handles.matrix = Matrix4x4.identity;
                            handleFunction (id, worldPosition, Camera.current.transform.rotation, handleSize, EventType.Repaint);
                            Handles.matrix = origMatrix;

                            if (id == GUIUtility.hotControl || id == HandleUtility.nearestControl && GUIUtility.hotControl == 0)
                                Handles.color = temp;
                            break;
                    }
                }
            }
		}
        #endregion

        #region Selection
		/// <summary>
		/// Handles the node selection.
		/// </summary>
		/// <param name="node">Node to add to the selection.</param>
		/// <param name="requestMulti">If the selection mode is on to select multiple nodes.</param>
		/// <param name="additive"><c>True</c> if the node is to be added to the selection.</param>
		/// <returns><c>True</c> if the selection changes.</returns>
		private bool ManageNodeToSelection (BezierNode node, int index, bool requestMulti = true, bool additive = false) {
			bool selectionChanged = false;
			//if (index >= 0 && index < _curve.nodeCount) {
            if (multiselectEnabled && requestMulti) { // MULTISELECT
                if (additive) { // Add the node if is not part of the selection already, keep it if it already is.
                    if (!_selectedNodes.Contains (node)) {
                        _selectedNodes.Add (node);
                        _selectedNodesIndex.Add (index);
                        _selectedCurveIds.Add (curveId);
                        _idToNode.Add (node.guid, node);
                        _nodeToCurve.Add (node.guid, curveId);
                        node.isSelected = true;
                    } else {
                        int indexToRemove = _selectedNodes.IndexOf (node);
                        if (indexToRemove >= 0) {
                            _selectedNodes.RemoveAt (indexToRemove);
                            _selectedNodesIndex.RemoveAt (indexToRemove);
                            _selectedCurveIds.RemoveAt (indexToRemove);
                            _idToNode.Remove (node.guid);
                            _nodeToCurve.Remove (node.guid);
                            node.isSelected = false;
                        }
                    }
                    selectionChanged = true;
                } else { // Add the node if is not part of the selection already
                    if (!_selectedNodes.Contains (node)) {
                        _selectedNodes.Add (node);
                        _selectedNodesIndex.Add (index);
                        _selectedCurveIds.Add (curveId);
                        _idToNode.Add (node.guid, node);
                        _nodeToCurve.Add (node.guid, curveId);
                        node.isSelected = true;
                        selectionChanged = true;
                    }
                }
            } else { // SINGLE SELECT
                // Node selection handling with single selection.
                if (_selectedNodes.Count == 1  && _selectedNodes[0] == node) {
                    // Same node selected
                } else {
                    // Deselect everything
                    for (int i = 0; i < _selectedNodes.Count; i++) {
                        _selectedNodes [0].isSelected = false;
                    }
                    ClearSelection ();
                    _selectedNodes.Add (node);
                    _selectedNodesIndex.Add (index);
                    _selectedCurveIds.Add (curveId);
                    _idToNode.Add (node.guid, node);
                    _nodeToCurve.Add (node.guid, curveId);
                    node.isSelected = true;
                    selectionChanged = true;
                }
            }
			//}
            /*
			if (selectionChanged) {
				for (int i = 0; i < _curve.nodeCount; i++) {
					_curve[i].isSelected = false;
				}
				for (int i = 0; i < _selectedNodes.Count; i++) {
					_selectedNodes[i].isSelected = true;
				}
			}
            */
			return selectionChanged;
		}
        bool _AddNodeToSelection (BezierNode node, int index, int curveId = -1) {
            if (node != null && !_idToNode.ContainsKey(node.guid)) {
                _selectedNodes.Add (node);
                _selectedNodesIndex.Add (index);
                _selectedCurveIds.Add (curveId);
                _idToNode.Add (node.guid, node);
                _nodeToCurve.Add (node.guid, curveId);
                node.isSelected = true;
                return true;
            }
            return false;
        }
        public bool AddNodeToSelection (BezierNode node, int index, int curveId = -1) {
            bool result = _AddNodeToSelection (node, index, curveId);
            if (result && onSelectionChanged != null) {
                onSelectionChanged (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);
            }
            return result;
        }
        public bool AddNodesToSelection (List<BezierNode> nodes, List<int> indexes, List<int> curveIds) {
            bool added = false;
            if (nodes.Count == curveIds.Count) {
                for (int i = 0; i < nodes.Count; i++) {
                    if (_AddNodeToSelection (nodes[i], indexes[i], curveIds[i])) {
                        added = true;
                }
                    }
            }
            if (added && onSelectionChanged != null) {
                onSelectionChanged (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);
            }
            return added;
        }
		#endregion

        #region Curve Methods
        public void PrintDebugInfo () {
            int samplesTotal = 0;
            int pointsTotal = _curve.points.Count;
            for (int i = 0; i < _curve.bezierCurves.Count; i++) {
                samplesTotal += _curve.bezierCurves[i].samples.Count;
            }
            Debug.LogFormat ("Curve has {0} nodes, {1} samples, {2} points.", 
                _curve.nodes.Count,
                samplesTotal,
                pointsTotal);
            for (int i = 0; i < _curve.nodes.Count; i++) {
                Debug.LogFormat ("Node {0}:\t {1}, handle1({2}), handle2({3})", 
                    i, _curve.nodes[i].position, _curve.nodes[i].handle1, _curve.nodes[i].handle2);
            }
            int sampleCount = 0;
            for (int i = 0; i < _curve.bezierCurves.Count; i++) {
                for (int j = 0; j < _curve.bezierCurves[i].samples.Count; j++) {
                    Debug.LogFormat ("  Sample {0}, {1}", sampleCount, _curve.bezierCurves[i].samples[j].position);
                    sampleCount++;
                }
            }
        }
		/// <summary>
		/// Calls the delegate for curve changes if there are some registered.
		/// </summary>
		private void CallCurveChangedDelegate () {
			if (onCurveChanged != null) {
				onCurveChanged (_curve);
			}
		}
        /*
        /// <summary>
        /// Return all the selected curve Ids in this instance.
        /// </summary>
        /// <returns>List of curve ids.</returns>
        public List<int> GetSelectedCurveIds () {
            List<int> curveIds = new List<int> ();
            foreach (int _curveId in _nodeToCurve.Values) {
                if (_curveId > -1 && !curveIds.Contains(_curveId)) {
                    curveIds.Add (_curveId);
                }
                
            }
            return curveIds;
        }
        */
		#endregion

		#region Node Methods
		/// <summary>
		/// Move the selection of nodes using an offset.
		/// </summary>
		/// <param name="offset">Offset to move.</param>
		public void MoveSelectedNodes (Vector3 offset) {
			if (onCheckMoveNodes != null) {
				offset = onCheckMoveNodes (offset);
			}
            offsetStep = offset - lastKnownOffset;
			if (offset != Vector3.zero) {
				if (onBeforeMoveNodes != null)
					onBeforeMoveNodes (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);
				for (int i = 0; i < _selectedNodes.Count; i++) {
					_selectedNodes[i].position += offset;
				}
				if (onMoveNodes != null)
					onMoveNodes (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);
					CallCurveChangedDelegate ();
			}
            lastKnownOffset = offset;
		}
        /// <summary>
		/// Move the selection of nodes using an offset.
		/// </summary>
		/// <param name="offset">Offset to move.</param>
		public void SnapSelectedNodes (BezierCurve.Axis axis, BezierNode referenceNode = null) {
            if (referenceNode == null) {
                referenceNode = _selectedNodes[_selectedNodes.Count - 1];
            }
            if (onBeforeMoveNodes != null)
                onBeforeMoveNodes (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);
            _curve.SnapNodesToAxis (_selectedNodesIndex, axis, referenceNode);
            if (onMoveNodes != null)
                onMoveNodes (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);
                CallCurveChangedDelegate ();
		}
		/// <summary>
		/// Remove nodes in the selection.
		/// </summary>
		/// <returns>True if the nodes were removed.</returns>
		public bool RemoveSelectedNodes () {
			bool canRemove = true;
			if (onCheckRemoveNodes != null) {
				canRemove = onCheckRemoveNodes (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);
			}
			if (canRemove) {
				if (onBeforeRemoveNodes != null) {
					onBeforeRemoveNodes (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);
				}
				//List<BezierNode> remainingNodes = new List<BezierNode> ();
				for (int i = 0; i < _selectedNodesIndex.Count; i++) {
					_curve.RemoveNode (_selectedNodesIndex[i]);
				}
				if (onRemoveNodes != null) {
					onRemoveNodes (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);
					CallCurveChangedDelegate ();
				}
				ClearSelection ();
				return true;
			}
			return false;
		}
		/// <summary>
		/// Clears the selection of nodes.
		/// </summary>
		public void ClearSelection (bool force = false) {
            if (_curve != null) {
                for (int i = 0; i < _curve.nodes.Count; i++) {
                    _curve.nodes[i].isSelected = false;
                }
            }
            _selectedNodes.Clear ();
            _selectedNodesIndex.Clear ();
            _selectedCurveIds.Clear ();
            if (onSelectionChanged != null) {
                onSelectionChanged (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);
            }
            _idToNode.Clear ();
            _nodeToCurve.Clear ();
		}
        /// <summary>
        /// Update the selected nodes from a curve.
        /// </summary>
        public void UpdateSelection () {
            _selectedNodes.Clear ();
            _selectedNodesIndex.Clear ();
            _selectedCurveIds.Clear ();
            if (onSelectionChanged != null) {
                onSelectionChanged (_selectedNodes, _selectedNodesIndex, _selectedCurveIds);
            }
            _idToNode.Clear ();
            _nodeToCurve.Clear ();
            for (int i = 0; i < _curve.nodes.Count; i++) {
                if (_curve.nodes[i].isSelected) {
                    _AddNodeToSelection (_curve.nodes[i], i, -1); // TODO: need to set curve id.
                }
            }
        }
		/// <summary>
		/// Changes the style of a handle of a node.
		/// </summary>
		/// <param name="node">Node owner of the handles.</param>
		/// <param name="index">Node index in the curve.</param>
		/// <param name="handleStyle">Handle style to change to.</param>
        /// <param name="isLastNode">Node is at the end of the curve.</param>
		/// <returns><c>True</c> if the handle style was changed.</returns>
		public bool ChangeNodeHandleStyle (BezierNode node, int index, BezierNode.HandleStyle handleStyle, bool isLastNode) {
			if (node.handleStyle != handleStyle) {
				if (onBeforeEditNode != null) {
					onBeforeEditNode (node, index);
				}
				node.handleStyle = handleStyle;
                // Set handle values to reset mirrored positions if handle style is connected.
                if (handleStyle == BezierNode.HandleStyle.Aligned) {
                    if (isLastNode) {
                        node.handle1 = node.handle1;
                    } else {
                        node.handle2 = node.handle2;
                    }
                }
				if (onEditNode != null) {
					onEditNode (node, index);
					CallCurveChangedDelegate ();
				}
				return true;
			}
			return false;
		}
		#endregion
    }
}