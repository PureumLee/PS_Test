using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

using Broccoli.Base;
using Broccoli.Pipe;
using Broccoli.Catalog;

namespace Broccoli.TreeNodeEditor
{
	/// <summary>
	/// Branch mesh generator node editor.
	/// </summary>
	[CustomEditor(typeof(BranchMeshGeneratorNode))]
	public class BranchMeshGeneratorNodeEditor : BaseNodeEditor {
		#region Vars
		/// <summary>
		/// The branch mesh generator node.
		/// </summary>
		public BranchMeshGeneratorNode branchMeshGeneratorNode;
		/// <summary>
		/// Options to show on the toolbar.
		/// </summary>
		string[] toolBarOptions = new string[] {"Global", "Branch Shape"};
		/// <summary>
		/// Shape catalog.
		/// </summary>
		ShapeCatalog shapeCatalog;
		/// <summary>
		/// Selected shape index.
		/// </summary>
		int selectedShapeIndex = 0;


		SerializedProperty propMinPolygonSides;
		SerializedProperty propMaxPolygonSides;
		SerializedProperty propSegmentAngle;
		SerializedProperty propUseHardNormals;
		SerializedProperty propUseAverageNormals;
		SerializedProperty propMinBranchCurveResolution;
		SerializedProperty propMaxBranchCurveResolution;
		SerializedProperty propMeshMode;
		SerializedProperty propMeshContext;
		SerializedProperty propMeshRange;
		//SerializedProperty propNodesMode;
		SerializedProperty propMinNodes;
		SerializedProperty propMaxNodes;
		SerializedProperty propMinNodeLength;
		SerializedProperty propMaxNodeLength;
		SerializedProperty propLengthVariance;
		SerializedProperty propNodesDistribution;
		SerializedProperty propShapeScale;
		SerializedProperty propBranchHierarchyScaleAdherence;
		#endregion

		#region Messages
		private static string MSG_ALPHA = "Shape meshing is a feature currently in alpha release. Although functional, improvements and testing is being performed to identify bugs on this feature.";
		private static string MSG_MIN_POLYGON_SIDES = "Minimum number of sides on the polygon used to create the mesh.";
		private static string MSG_MAX_POLYGON_SIDES = "Maximum number of sides on the polygon used to create the mesh.";
		private static string MSG_SEGMENT_MESH_ANGLE = "Rotation angle of the polygon around the branch center.";
		private static string MSG_MIN_BRANCH_CURVE_RESOLUTION = "Minimum resolution used to process branch curves to create segments. " +
			"The minimum value is used when processing the mesh at the lowest resolution LOD.";
		private static string MSG_MAX_BRANCH_CURVE_RESOLUTION = "Maximum resolution used to process branch curves to create segments. " +
			"The maximum value is used when processing the mesh at the highest resolution LOD.";
		private static string MSG_USE_HARD_NORMALS = "Hard normals increases the number vertices per face while " +
			"keeping the same number of triangles. This option is useful to give a lowpoly flat shaded effect on the mesh.";
		private static string MSG_USE_AVERAGE_NORMALS = "The base of children branches average their normals to their parent, " +
			"this gives a smoother light transition between a parent branch and its children.";
		private string MSG_SHAPE = "Selects a shape to use to stylize the branches mesh.";
		private string MSG_MESH_MODE = "Option to select how each branch mesh should be stylized.";
		private string MSG_MESH_CONTEXT = "Selects if a custom shape context encompass a single branch or a follow up series of branches.";
		private string MSG_MESH_RANGE = "Selects if a custom shape should encompass its whole mesh context or be divided by nodes.";
		//private string MSG_NODES_MODE = "How to calculate the number of nodes in the mesh context, either by length of number of nodes.";
		//private string MSG_NODES_MINMAX_LENGTH = "Range of minimum and maximum length of each node (from 0 to 1).";
		private string MSG_NODES_MINMAX = "Range of the number of nodes to generate.";
		private string MSG_NODES_LENGTH_VARIANCE = "Variance in length size of nodes. Variance with value 0 gives nodes with the same length within a mesh context.";
		private string MSG_NODES_DISTRIBUTION = "How to distribute nodes along the mesh context.";
		private string MSG_SHAPE_SCALE = "Scale multiplier for the shape.";
		private string MSG_BRANCH_HIERARCHY_SCALE = "How much of the shape scale is taken based on the branch hierarchy. Value of 1 is full adherence to the branch scale at a given hierarchy position.";
		#endregion

		#region Events
		/// <summary>
		/// Actions to perform on the concrete class when the enable event is raised.
		/// </summary>
		protected override void OnEnableSpecific () {
			branchMeshGeneratorNode = target as BranchMeshGeneratorNode;

			SetPipelineElementProperty ("branchMeshGeneratorElement");
			propMinPolygonSides = GetSerializedProperty ("minPolygonSides");
			propMaxPolygonSides = GetSerializedProperty ("maxPolygonSides");
			propSegmentAngle = GetSerializedProperty ("segmentAngle");
			propUseHardNormals = GetSerializedProperty ("useHardNormals");
			propUseAverageNormals = GetSerializedProperty ("useAverageNormals");
			propMinBranchCurveResolution = GetSerializedProperty ("minBranchCurveResolution");
			propMaxBranchCurveResolution = GetSerializedProperty ("maxBranchCurveResolution");
			propMeshMode = GetSerializedProperty ("meshMode");
			propMeshContext = GetSerializedProperty ("meshContext");
			propMeshRange = GetSerializedProperty ("meshRange");
			//propNodesMode = GetSerializedProperty ("nodesMode");
			propMinNodes = GetSerializedProperty ("minNodes");
			propMaxNodes = GetSerializedProperty ("maxNodes");
			propMinNodeLength = GetSerializedProperty ("minNodeLength");
			propMaxNodeLength = GetSerializedProperty ("maxNodeLength");
			propLengthVariance = GetSerializedProperty ("nodeLengthVariance");
			propNodesDistribution = GetSerializedProperty ("nodesDistribution");
			propShapeScale = GetSerializedProperty ("shapeScale");;
			propBranchHierarchyScaleAdherence = GetSerializedProperty ("branchHierarchyScaleAdherence");

			shapeCatalog = ShapeCatalog.GetInstance ();
			selectedShapeIndex = shapeCatalog.GetShapeIndex (branchMeshGeneratorNode.branchMeshGeneratorElement.selectedShapeId);
		}
		/// <summary>
		/// Raises the inspector GUI event.
		/// </summary>
		public override void OnInspectorGUI() {
			CheckUndoRequest ();

			UpdateSerialized ();

			branchMeshGeneratorNode.selectedToolbar = GUILayout.Toolbar (branchMeshGeneratorNode.selectedToolbar, toolBarOptions);
			EditorGUILayout.Space ();

			if (branchMeshGeneratorNode.selectedToolbar == 0) {
				EditorGUI.BeginChangeCheck ();

				int maxPolygonSides = propMaxPolygonSides.intValue;
				EditorGUILayout.IntSlider (propMaxPolygonSides, 3, 16, "Max Polygon Sides");
				ShowHelpBox (MSG_MAX_POLYGON_SIDES);
				EditorGUILayout.Space ();

				int minPolygonSides = propMinPolygonSides.intValue;
				EditorGUILayout.IntSlider (propMinPolygonSides, 3, 16, "Min Polygon Sides");
				ShowHelpBox (MSG_MIN_POLYGON_SIDES);
				EditorGUILayout.Space ();

				float segmentAngle = propSegmentAngle.floatValue;
				EditorGUILayout.Slider (propSegmentAngle, 0f, 180f, "Segment Mesh Angle");
				ShowHelpBox (MSG_SEGMENT_MESH_ANGLE);
				EditorGUILayout.Space ();

				float maxBranchCurveResolution = propMaxBranchCurveResolution.floatValue;
				EditorGUILayout.Slider (propMaxBranchCurveResolution, 0, 1, "Max Branch Resolution");
				ShowHelpBox (MSG_MAX_BRANCH_CURVE_RESOLUTION);
				EditorGUILayout.Space ();

				float minBranchCurveResolution = propMinBranchCurveResolution.floatValue;
				EditorGUILayout.Slider (propMinBranchCurveResolution, 0, 1, "Min Branch Resolution");
				ShowHelpBox (MSG_MIN_BRANCH_CURVE_RESOLUTION);
				EditorGUILayout.Space ();

				bool useHardNormals = propUseHardNormals.boolValue;
				EditorGUILayout.PropertyField (propUseHardNormals);
				ShowHelpBox (MSG_USE_HARD_NORMALS);
				EditorGUILayout.Space ();

				bool useAverageNormals = propUseAverageNormals.boolValue;
				if (!useHardNormals) {
					useAverageNormals = EditorGUILayout.PropertyField (propUseAverageNormals);
					ShowHelpBox (MSG_USE_AVERAGE_NORMALS);
					EditorGUILayout.Space ();
				}

				if (EditorGUI.EndChangeCheck () &&
					propMinPolygonSides.intValue <= propMaxPolygonSides.intValue &&
					propMinBranchCurveResolution.floatValue <= propMaxBranchCurveResolution.floatValue) {
					if (minPolygonSides != propMinPolygonSides.intValue ||
						maxPolygonSides != propMaxPolygonSides.intValue ||
						minBranchCurveResolution != propMinBranchCurveResolution.floatValue ||
						maxBranchCurveResolution != propMaxBranchCurveResolution.floatValue ||
						segmentAngle != propSegmentAngle.floatValue ||
						useHardNormals != propUseHardNormals.boolValue ||
						useAverageNormals != propUseAverageNormals.boolValue) {
						ApplySerialized ();
						UpdatePipeline (GlobalSettings.processingDelayHigh);
						NodeEditorFramework.NodeEditor.RepaintClients ();
						branchMeshGeneratorNode.branchMeshGeneratorElement.Validate ();
						SetUndoControlCounter ();
					}
				}
			} else {
				EditorGUI.BeginChangeCheck ();

				// MESHING MODES
				EditorGUILayout.PropertyField (propMeshMode);
				ShowHelpBox (MSG_MESH_MODE);
				EditorGUILayout.Space ();

				// IF SHAPE MODE SELECTED
				if (propMeshMode.enumValueIndex == (int)BranchMeshGeneratorElement.MeshMode.Shape) {
					// ALPHA MESSAGE.
					EditorGUILayout.HelpBox (MSG_ALPHA, MessageType.Warning);
					EditorGUILayout.Space ();

					// SELECT SHAPE.
					selectedShapeIndex = EditorGUILayout.Popup ("Shape", selectedShapeIndex, shapeCatalog.GetShapeOptions ());
					ShowHelpBox (MSG_SHAPE);
					EditorGUILayout.Space ();

					EditorGUILayout.PropertyField (propMeshContext);
					ShowHelpBox (MSG_MESH_CONTEXT);
					EditorGUILayout.Space ();

					EditorGUILayout.PropertyField (propMeshRange);
					ShowHelpBox (MSG_MESH_RANGE);
					EditorGUILayout.Space ();

					// IF NODE MESH RANGE SELECTED
					if (propMeshRange.enumValueIndex == (int)BranchMeshGeneratorElement.MeshRange.Nodes) {
						// Default to number node mode.
						/*
						EditorGUILayout.PropertyField (propNodesMode);
						ShowHelpBox (MSG_NODES_MODE);
						EditorGUILayout.Space ();

						if (propNodesMode.enumValueIndex == (int)BranchMeshGeneratorElement.NodesMode.Length) {
							// IF NODE MODE LENGTH
							FloatRangePropertyField (propMinNodeLength, propMaxNodeLength, 0f, 1f, "Node Length");
							ShowHelpBox (MSG_NODES_MINMAX_LENGTH);
						} else {
						*/
							// IF NODE MODE NUMBER
							IntRangePropertyField (propMinNodes, propMaxNodes, 2, 8, "Nodes");
							ShowHelpBox (MSG_NODES_MINMAX);
						//}
						EditorGUILayout.Space ();

						EditorGUILayout.Slider (propLengthVariance, 0f, 1f, "Node Size Variance");
						ShowHelpBox (MSG_NODES_LENGTH_VARIANCE);
						EditorGUILayout.Space ();

						EditorGUILayout.PropertyField (propNodesDistribution);
						ShowHelpBox (MSG_NODES_DISTRIBUTION);
						EditorGUILayout.Space ();
					}

					// SHAPE SCALE.
					EditorGUILayout.Slider (propShapeScale, 0.1f, 5f);
					ShowHelpBox (MSG_SHAPE_SCALE);
					EditorGUILayout.Space ();

					// BRANCH SCALE HIERARCHY ADHERENCE.
					EditorGUILayout.Slider (propBranchHierarchyScaleAdherence, 0f, 1f, "Scale Adherence");
					ShowHelpBox (MSG_BRANCH_HIERARCHY_SCALE);
					EditorGUILayout.Space ();
				}

				if (EditorGUI.EndChangeCheck () && 
					propMinNodes.intValue <= propMaxNodes.intValue && 
					propMinNodeLength.floatValue <= propMaxNodeLength.floatValue)
				{
					ShapeCatalog.ShapeItem shapeItem = shapeCatalog.GetShapeItem (selectedShapeIndex); // -1 because of the 'default' option
					if (shapeItem == null || propMeshMode.enumValueIndex == (int)BranchMeshGeneratorElement.MeshMode.Default) {
						branchMeshGeneratorNode.branchMeshGeneratorElement.shapeCollection = null;
					} else {
						branchMeshGeneratorNode.branchMeshGeneratorElement.selectedShapeId = shapeItem.id;
						branchMeshGeneratorNode.branchMeshGeneratorElement.shapeCollection = shapeItem.shapeCollection;
					}
					EditorUtility.SetDirty (branchMeshGeneratorNode);
					ApplySerialized ();
					UpdatePipeline (GlobalSettings.processingDelayHigh);
					NodeEditorFramework.NodeEditor.RepaintClients ();
					branchMeshGeneratorNode.branchMeshGeneratorElement.Validate ();
					SetUndoControlCounter ();
				}
			}

			if (branchMeshGeneratorNode.branchMeshGeneratorElement.showLODInfoLevel == 1) {
			} else if (branchMeshGeneratorNode.branchMeshGeneratorElement.showLODInfoLevel == 2) {
			} else {
				EditorGUILayout.HelpBox ("LOD0\nVertex Count: " + branchMeshGeneratorNode.branchMeshGeneratorElement.verticesCountSecondPass +
					"\nTriangle Count: " + branchMeshGeneratorNode.branchMeshGeneratorElement.trianglesCountSecondPass + "\nLOD1\nVertex Count: " + branchMeshGeneratorNode.branchMeshGeneratorElement.verticesCountFirstPass +
				"\nTriangle Count: " + branchMeshGeneratorNode.branchMeshGeneratorElement.trianglesCountFirstPass, MessageType.Info);
			}
			EditorGUILayout.Space ();
	
			// Field descriptors option.
			DrawFieldHelpOptions ();
		}
		#endregion
	}
}