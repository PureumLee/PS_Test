using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Broccoli.Builder;

namespace Broccoli.Pipe {
	/// <summary>
	/// Branch mesh generator element.
	/// </summary>
	[System.Serializable]
	public class BranchMeshGeneratorElement : PipelineElement {
		#region Vars
		/// <summary>
		/// Gets the type of the connection.
		/// </summary>
		/// <value>The type of the connection.</value>
		public override ConnectionType connectionType {
			get { return ConnectionType.Transform; }
		}
		/// <summary>
		/// Gets the type of the element.
		/// </summary>
		/// <value>The type of the element.</value>
		public override ElementType elementType {
			get { return ElementType.MeshGenerator; }
		}
		/// <summary>
		/// Gets the type of the class.
		/// </summary>
		/// <value>The type of the class.</value>
		public override ClassType classType {
			get { return ClassType.BranchMeshGenerator; }
		}
		/// <summary>
		/// Value used to position elements in the pipeline. The greater the more towards the end of the pipeline.
		/// </summary>
		/// <value>The position weight.</value>
		public override int positionWeight {
			get {
				return PipelineElement.meshGeneratorWeight;
			}
		}
		/// <summary>
		/// The minimum polygon sides.
		/// </summary>
		public int minPolygonSides = 3;
		/// <summary>
		/// The max polygon sides.
		/// </summary>
		public int maxPolygonSides = 8;
		/// <summary>
		/// The segment angle.
		/// </summary>
		public float segmentAngle = 0f;
		/// <summary>
		/// The generated mesh uses hard normals.
		/// </summary>
		public bool useHardNormals = false;
		/// <summary>
		/// The minimum branch curve resolution to generate a branch mesh (LOD1).
		/// </summary>
		public float minBranchCurveResolution = 0.25f;
		/// <summary>
		/// The maximum branch curve resolution to generate a branch mesh (LOD0).
		/// </summary>
		public float maxBranchCurveResolution = 0.75f;
		/// <summary>
		/// True to average normals between a parent and a child branch at its base.
		/// This smooths lighting on the mesh branch intersection.
		/// </summary>
		public bool useAverageNormals = true;
		/// <summary>
		/// Level of LOD information to show.
		/// </summary>
		[System.NonSerialized]
		public int showLODInfoLevel = -1;
		/// <summary>
		/// The minimum girth.
		/// </summary>
		[System.NonSerialized]
		public float minGirth;
		/// <summary>
		/// The max girth.
		/// </summary>
		[System.NonSerialized]
		public float maxGirth;
		/// <summary>
		/// The vertices count for the first pass LOD.
		/// </summary>
		[System.NonSerialized]
		public int verticesCountFirstPass = 0;
		/// <summary>
		/// The triangles count for the first pass LOD.
		/// </summary>
		[System.NonSerialized]
		public int trianglesCountFirstPass = 0;
		/// <summary>
		/// The vertices count for the second pass LOD.
		/// </summary>
		[System.NonSerialized]
		public int verticesCountSecondPass = 0;
		/// <summary>
		/// The triangles count for the second pass LOD.
		/// </summary>
		[System.NonSerialized]
		public int trianglesCountSecondPass = 0;
		/// <summary>
		/// Takes a collection of shapes to apply to the branches on the tree structure.
		/// </summary>
		[SerializeField]
		public ShapeDescriptorCollection shapeCollection = null;
		/// <summary>
		/// Meshing mode option for branches.
		/// </summary>
		public enum MeshMode {
			/// <summary>
			/// Default cylinder shaped branch mesh.
			/// </summary>
			Default,
			/// <summary>
			/// Custom shape branch mesh.
			/// </summary>
			Shape
		}
		/// <summary>
		/// Selected meshing mode for branches.
		/// </summary>
		public MeshMode meshMode = MeshMode.Default;
		/// <summary>
		/// Context element to apply a custom shape branch mesh.
		/// </summary>
		public enum MeshContext {
			/// <summary>
			/// The custom shape context domain is a single branch.
			/// </summary>
			PerBranch,
			/// <summary>
			/// The custom shape context domain includes parent and followup branches (a branch skin instance).
			/// </summary>
			BranchSequence
		}
		// The selected mesh context.
		public MeshContext meshContext = MeshContext.PerBranch;
		/// <summary>
		/// Meshing option to encompass the whole mesh context or have several ranges (nodes).
		/// </summary>
		public enum MeshRange {
			Whole,
			Nodes
		}
		/// <summary>
		/// The selected mesh range for the mesh context.
		/// </summary>
		public MeshRange meshRange = MeshRange.Whole;
		/// <summary>
		/// Modes to calculate nodes at a mesh context, by number or length.
		/// </summary>
		public enum NodesMode {
			/// <summary>
			/// The number of nodes is calculated based on minimun and maximum values.
			/// </summary>
			Number,
			/// <summary>
			/// The number of nodes is calculated based on minimum and maximum values relative to
			/// the length of the mesh context.
			/// </summary>
			Length
		}
		/// <summary>
		/// The selected option to calculate the number of nodes in the mesh context.
		/// </summary>
		public NodesMode nodesMode = NodesMode.Number;
		/// <summary>
		/// Minimum number of nodes within the mesh context.
		/// </summary>
		public int minNodes = 2;
		/// <summary>
		/// Maximum number of nodes within the mesh context.
		/// </summary>
		public int maxNodes = 3;
		/// <summary>
		/// Minimum length per nodes.
		/// </summary>
		public float minNodeLength = 0.2f;
		/// <summary>
		/// Maximum length per nodes.
		/// </summary>
		public float maxNodeLength = 0.5f;
		/// <summary>
		/// How much a node length varies in length size from a neighbour node.
		/// </summary>
		public float nodeLengthVariance = 0f;
		/// <summary>
		/// Distribution of nodes along the mesh context.
		/// </summary>
		public enum NodesDistribution {
			/// <summary>
			/// Bigger nodes are positionesd at the bottom of the mesh context.
			/// </summary>
			biggerAtBottom,
			/// <summary>
			/// Bigger nodes are positioned at the top of the mesh context.
			/// </summary>
			biggerAtTop,
			/// <summary>
			/// Nodes are placed randomly in the the mesh context.
			/// </summary>
			Random
		}
		/// <summary>
		/// The selected node distribution.
		/// </summary>
		public NodesDistribution nodesDistribution = NodesDistribution.biggerAtBottom;
		/// <summary>
		/// Additional multiplier to shape scale.
		/// </summary>
		[Range (0.1f, 5f)]
		public float shapeScale = 1f;
		/// <summary>
		/// How much the shape scale adheres to the branch hierarchy branch. A value of 1 is full adherence.
		/// </summary>
		[Range (0f, 1f)]
		public float branchHierarchyScaleAdherence = 1f;
		/// <summary>
		/// Saves the selected shape name from the catalog.
		/// </summary>
		public string selectedShapeId = "";
		#endregion

		#region Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="Broccoli.Pipe.BranchMeshGeneratorElement"/> class.
		/// </summary>
		public BranchMeshGeneratorElement () {}
		#endregion

		#region Cloning
		/// <summary>
		/// Clone this instance.
		/// </summary>
		override public PipelineElement Clone() {
			BranchMeshGeneratorElement clone = ScriptableObject.CreateInstance<BranchMeshGeneratorElement> ();
			SetCloneProperties (clone);
			clone.minPolygonSides = minPolygonSides;
			clone.maxPolygonSides = maxPolygonSides;
			clone.minGirth = minGirth;
			clone.maxGirth = maxGirth;
			clone.segmentAngle = segmentAngle;
			clone.useHardNormals = useHardNormals;
			clone.useAverageNormals = useAverageNormals;
			clone.shapeCollection = shapeCollection;
			clone.meshMode = meshMode;
			clone.meshContext = meshContext;
			clone.meshRange = meshRange;
			clone.nodesMode = nodesMode;
			clone.minNodes = minNodes;
			clone.maxNodes = maxNodes;
			clone.minNodeLength = minNodeLength;
			clone.maxNodeLength = maxNodeLength;
			clone.nodeLengthVariance = nodeLengthVariance;
			clone.nodesDistribution = nodesDistribution;
			clone.shapeScale = shapeScale;
			clone.branchHierarchyScaleAdherence = branchHierarchyScaleAdherence;
			clone.selectedShapeId = selectedShapeId;
			clone.shapeCollection = shapeCollection;
			return clone;
		}
		#endregion
	}
}