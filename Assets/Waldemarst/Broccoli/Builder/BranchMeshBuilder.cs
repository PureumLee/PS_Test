using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Broccoli.Model;
using Broccoli.Utils;

namespace Broccoli.Builder {
	/// <summary>
	/// Mesh building for branches.
	/// </summary>
	public class BranchMeshBuilder {
		#region TriangleInfo Struct
		/// <summary>
		/// Saves information about a triangle on the branch.
		/// </summary>
		public struct TriangleInfo : System.IEquatable<TriangleInfo> {
			#region Vars
			/// <summary>
			/// Index for the face.
			/// </summary>
			public int faceIndex;
			/// <summary>
			/// Segment index on the branch skin.
			/// </summary>
			public int segmentIndex;
			/// <summary>
			/// Vertex A index.
			/// </summary>
			public int firstVertexIndex;
			/// <summary>
			/// Vertex B index.
			/// </summary>
			public int secondVertexIndex;
			/// <summary>
			/// Vertex C index.
			/// </summary>
			public int thirdVertexIndex;
			#endregion

			#region Constructor
			public TriangleInfo (int faceIndex, int segmentIndex, int firstVertexIndex, int secondVertexIndex, int thirdVertexIndex) {
				this.faceIndex = faceIndex;
				this.segmentIndex = segmentIndex;
				this.firstVertexIndex = firstVertexIndex;
				this.secondVertexIndex = secondVertexIndex;
				this.thirdVertexIndex = thirdVertexIndex;
			}
			#endregion

			#region Methods
			public bool Equals (TriangleInfo other) {
				return firstVertexIndex == other.firstVertexIndex && secondVertexIndex == other.secondVertexIndex && thirdVertexIndex == other.thirdVertexIndex;
			}
			public override int GetHashCode() {
				int hash = firstVertexIndex.GetHashCode () + secondVertexIndex.GetHashCode () + thirdVertexIndex.GetHashCode ();
				return hash;
			}
			#endregion
		}
		#endregion

		#region VertexInfo class
		/// <summary>
		/// Saves information about a vertex created by the mesh builder.
		/// </summary>
		public class VertexInfo {
			public int radiusIndex;
			/// <summary>
			/// Position on the segment circunference (normalized 0 to 1).
			/// </summary>
			public float radiusPosition;
			public int segmentIndex;
			/// <summary>
			/// If not -1 this value indicates the last vertex index on the segment.
			/// </summary>
			public int lastIndexAtSegment = -1;
			/// <summary>
			/// VertexInfo constructor.
			/// </summary>
			/// <param name="radiusIndex">Index on the polygon segment circunference.</param>
			/// <param name="radiusPosition">Radial position on the segment.</param>
			/// <param name="segmentIndex">Index of the segment this vertex belongs to.</param>
			public VertexInfo (int radiusIndex, float radiusPosition, int segmentIndex, int lastIndexAtSegment) {
				this.radiusIndex = radiusIndex;
				this.radiusPosition = radiusPosition;
				this.segmentIndex = segmentIndex;
				this.lastIndexAtSegment = lastIndexAtSegment;
			}
			/// <summary>
			/// VertexInfo constructor.
			/// </summary>
			/// <param name="radiusIndex">Index on the polygon segment circunference.</param>
			/// <param name="radiusPosition">Radial position on the segment.</param>
			/// <param name="segmentIndex">Index of the segment this vertex belongs to.</param>
			public VertexInfo (int radiusIndex, float radiusPosition, int segmentIndex) {
				this.radiusIndex = radiusIndex;
				this.radiusPosition = radiusPosition;
				this.segmentIndex = segmentIndex;
			}
			/// <summary>
			/// Clone this instance.
			/// </summary>
			public VertexInfo Clone () {
				VertexInfo clone = new VertexInfo (radiusIndex, radiusPosition, segmentIndex, lastIndexAtSegment);
				return clone;
			}
		}
		#endregion

		#region BranchSkinRange class
		/// <summary>
		/// Defines a region along a BranchSkin instance to be processed by an specific mesh builder.
		/// </summary>
		public class BranchSkinRange {
			#region Vars
			/// <summary>
			/// Initial position of the branch skin range, from 0 to 1.
			/// </summary>
			public float from = 0f;
			/// <summary>
			/// Final position of the branch skin range, from 0 to 1.
			/// </summary>
			public float to = 0f;
			/// <summary>
			/// If the range has a capped shape range, the position the bottom cap begins at.
			/// </summary>
			public float bottomCap = 0f;
			/// <summary>
			/// If the range has a capped shape range, the position the top cap ends at.
			/// </summary>
			public float topCap = 1f;
			/// <summary>
			/// Number of preferred subdivisions on this range.
			/// </summary>
			public int subdivisions = 1;
			public BuilderType builderType = BuilderType.Default;
			/// <summary>
			/// Used only for ranges describing branches within a branch skin instance.
			/// </summary>
			public int branchId = -1;
			/// <summary>
			/// Id of the shape assigned to this range.
			/// </summary>
			public int shapeId = -1;
			#endregion
		}
		#endregion

		#region BranchSkin class
		/// <summary>
		/// Holds the properties of the mesh around a continium of branches.
		/// Used to have the same continuos mesh for parent branches and
		/// their follow up branches.
		/// </summary>
		public class BranchSkin {
			#region Vars
			/// <summary>
			/// Id of this instance, if there are branches then the id of the first branch, otherwise -1.
			/// </summary>
			public int id = -1;
			/// <summary>
			/// The branch identifiers.
			/// </summary>
			protected List<int> _ids = new List<int> ();
			/// <summary>
			/// Center positions for each segment.
			/// </summary>
			protected List<Vector3> _centers = new List<Vector3> ();
			/// <summary>
			/// The directions for each segment.
			/// </summary>
			protected List<Vector3> _directions = new List<Vector3> ();
			/// <summary>
			/// The normals.
			/// </summary>
			protected List<Vector3> _normals = new List<Vector3> ();
			/// <summary>
			/// Series of segments on the skin with their number of vertices.
			/// </summary>
			protected List<int> _segments = new List<int> ();
			/// <summary>
			/// Girth registered for every segment.
			/// </summary>
			protected List<float> _girths = new List<float> ();
			/// <summary>
			/// The positions in the branch for each segment.
			/// </summary>
			protected List<float> _positions = new List<float> ();
			/// <summary>
			/// The positions in the BranchSkin for each segment.
			/// </summary>
			protected List<float> _positionsAtSkin = new List<float> ();
			/// <summary>
			/// The type of builder for each segment.
			/// </summary>
			/// <typeparam name="BuilderType">Type of builder.</typeparam>
			/// <returns>Type of builder for the segment.</returns>
			protected List<BuilderType> _builders = new List<BuilderType> ();
			/// <summary>
			/// Range of action for mesh builders on this BranchSkin instance.
			/// </summary>
			/// <typeparam name="BranchSkinRange">Definition of the range.</typeparam>
			/// <returns>List of mesh builder ranges.</returns>
			protected List<BranchSkinRange> _ranges = new List<BranchSkinRange> ();
			/// <summary>
			/// Ranges branches occupy withing the branch skin instance.
			/// </summary>
			/// <typeparam name="BranchSkinRange">Definition of the range.</typeparam>
			/// <returns>List of branch ranges.</returns>
			protected List<BranchSkinRange> _branchRanges = new List<BranchSkinRange> ();
			/// <summary>
			/// List of relevant positions to be assigned segments to along the branch. Each position ir relative from 0 to 1.
			/// </summary>
			/// <typeparam name="float">Relevant position from 0 to 1 relative to the branch length.</typeparam>
			List<float> _relevantPositions = new List<float> ();
			/// <summary>
			/// Save each relevant position priority on the branch skin.
			/// </summary>
			/// <typeparam name="int">Priority of the position, the higher more piority to replace an existing point.</typeparam>
			List<int> _relevantPositionPriorities = new List<int> ();
			/// <summary>
			/// The minimum polygon sides allowed.
			/// </summary>
			protected int _minPolygonSides = 3;
			/// <summary>
			/// The maximum polygon sides allowed.
			/// </summary>
			protected int _maxPolygonSides = 8;
			/// <summary>
			/// The number of polygon sides used on this mesh.
			/// </summary>
			public int polygonSides = 0;
			/// <summary>
			/// The minimum average girth found on the branches of the tree.
			/// </summary>
			public float minAvgGirth = 0f;
			/// <summary>
			/// The maximum average girth found on the branches of the tree.
			/// </summary>
			public float maxAvgGirth = 0f;
			/// <summary>
			/// Last pointing direction for the segment.
			/// </summary>
			public Vector3 lastDirection = Vector3.zero;
			/// <summary>
			/// True if the skin represents the trunk of the tree.
			/// </summary>
			public bool isTrunk = false;
			/// <summary>
			/// Length of this branch skin.
			/// </summary>
			public float length = 0f;
			/// <summary>
			/// Level of hierarchy for this BranchSkin.
			/// </summary>
			public int level = 0;
			/// <summary>
			/// Vertices structure data.
			/// </summary>
			public List<Vector3> vertices = new List<Vector3> ();
			/// <summary>
			/// VertexInfo structure data.
			/// </summary>
			public List<VertexInfo> vertexInfos = new List<VertexInfo> ();
			/// <summary>
			/// TriangleInfo structure data.
			/// </summary>
			public List<TriangleInfo> triangleInfos = new List<TriangleInfo> ();
			/// <summary>
			/// Counter for polygon faces.
			/// </summary>
			public int faceCount = 0;
			/// <summary>
			/// True if the current faceCount has already been used.
			/// </summary>
			public bool isFaceCountUsed = false;
			/// <summary>
			/// Vertex offset on a mesh.
			/// </summary>
			public int vertexOffset = 0;
			/// <summary>
			/// Start index for the last segment's first vertex added to this instance.
			/// Used as control for soft normals meshing.
			/// </summary>
			public int previousSegmentStartIndex = -1;
			/// <summary>
			/// Dictionary holding every branch Id to their index order.
			/// </summary>
			/// <typeparam name="int">Branch id.</typeparam>
			/// <typeparam name="int">Order in the list of branches of this BranchSkin instance.</typeparam>
			private Dictionary<int, int> _branchIdToIndex = new Dictionary<int, int> ();
			/// <summary>
			/// List holding the start index value for vertices on the mesh for every branch on the BranchSkin instance.
			/// </summary>
			/// <typeparam name="int">Start index of vertices for a branch on its order of listing within the BranchSin instance.</typeparam>
			private List<int> _branchStartVertexIndex = new List<int> ();
			#endregion

			#region Accessors
			public List<int> ids {
				get { return _ids; }
			}
			public List<Vector3> centers {
				get { return _centers; }
			}
			public List<Vector3> directions {
				get { return _directions; }
			}
			public List<Vector3> normals {
				get { return _normals; }
			}
			public List<int> segments {
				get { return _segments; }
			}
			public List<float> girths {
				get { return _girths; }
			}
			public List<float> positions {
				get { return _positions; }
			}
			public List<float> positionsAtSkin {
				get { return _positionsAtSkin; }
			}
			public List<BuilderType> builders {
				get { return _builders; }
			}
			public List<BranchSkinRange> ranges {
				get { return _ranges; }
			}
			public List<BranchSkinRange> branchRanges {
				get { return _branchRanges; }
			}
			public int minPolygonSides {
				get { return _minPolygonSides; }
			}
			public int maxPolygonSides {
				get { return _maxPolygonSides; }
			}
			#endregion

			#region Constructors
			/// <summary>
			/// Initializes a new instance of the <see cref="Broccoli.Builder.BranchMeshBuilder+BranchSkin"/> class.
			/// </summary>
			public BranchSkin () {}
			/// <summary>
			/// Initializes a new instance of the <see cref="Broccoli.Builder.BranchMeshBuilder+BranchSkin"/> class.
			/// </summary>
			/// <param name="minPolygonSides">Minimum polygon sides.</param>
			/// <param name="maxPolygonSides">Max polygon sides.</param>
			/// <param name="minAvgGirth">Minimum avg girth.</param>
			/// <param name="maxAvgGirth">Max avg girth.</param>
			public BranchSkin (int minPolygonSides, int maxPolygonSides, float minAvgGirth, float maxAvgGirth) {
				this._minPolygonSides = minPolygonSides;
				this._maxPolygonSides = maxPolygonSides;
				this.minAvgGirth = minAvgGirth;
				this.maxAvgGirth = maxAvgGirth;
			}
			#endregion

			#region Range
			/// <summary>
			/// Adds a BranchSkinRange to the BranchSkin instance. This represents a region
			/// along the BranchSkin to be processed by another mesh builder.
			/// </summary>
			/// <param name="range"></param>
			public void AddRange (BranchSkinRange range) {
				_ranges.Add (range);
				_ranges.Sort (delegate (BranchSkinRange x, BranchSkinRange y) { return x.from.CompareTo(y.from); });
			}
			/// <summary>
			/// Adds a BranchSkinRange belonging to a branch instance.
			/// </summary>
			/// <param name="range"></param>
			public void AddBranchRange (BranchSkinRange range) {
				_branchRanges.Add (range);
				_branchRanges.Sort (delegate (BranchSkinRange x, BranchSkinRange y) { return x.from.CompareTo(y.from); });
			}
			/// <summary>
			/// Translates a branch skin position to a normalized in range position.
			/// </summary>
			/// <param name="branchSkinPosition">Position on the branch skin.</param>
			/// <param name="range">Range instance if the range matching the position was found, otherwise null.</param>
			/// <returns>In range position.</returns>
			public float TranslateToPositionAtRange (float branchSkinPosition, out BranchSkinRange range) {
				float rangePosition = branchSkinPosition;
				range = null;
				for (int i = 0; i < _ranges.Count; i++) {
					if (branchSkinPosition >= _ranges[i].from && branchSkinPosition <= _ranges[i].to) {
						range = _ranges[i];
						rangePosition = Mathf.InverseLerp (_ranges[i].from, _ranges[i].to, branchSkinPosition);
						break;
					}
				}
				return rangePosition;
			}
			/// <summary>
			/// Get relevant position bound to a specific branch within the branch skin instance.
			/// </summary>
			/// <param name="branchId">Id of the branch.</param>
			/// <param name="priority">Priority limit.</param>
			/// <param name="normalized">Normalize the position within the branch length.</param>
			/// <returns>List of relevant positions within a branch range.</returns>
			public List<float> GetBranchRelevantPositions (int branchId, int priority = 0, bool normalized = false) {
				List<float> relevantPositions = new List<float> ();
				BranchSkinRange branchSkinRange = null;
				for (int i = 0; i < _branchRanges.Count; i++) {
					if (_branchRanges [i].branchId == branchId) {
						branchSkinRange = _branchRanges [i];
						break;
					}
				}
				if (branchSkinRange != null) {
					for (int i = 0; i < _relevantPositions.Count; i++) {
						if (_relevantPositions [i] >= branchSkinRange.from && 
							_relevantPositions [i] <= branchSkinRange.to &&
							_relevantPositionPriorities [i] <= priority) {
							float relevantPosition = _relevantPositions [i];
							if (normalized) {
								relevantPosition = Mathf.InverseLerp (branchSkinRange.from, branchSkinRange.to, relevantPosition);
							}
							if (relevantPosition > 0 && relevantPosition < 1) {
								relevantPositions.Add (relevantPosition);
							}
						}
					}
				}
				relevantPositions.Sort ();
				return relevantPositions;
			}
			#endregion

			#region Structure methods
			/// <summary>
			/// Adds a segment to the skin.
			/// </summary>
			/// <returns><c>true</c>, if segment was added, <c>false</c> otherwise.</returns>
			/// <param name="id">Identifier.</param>
			/// <param name="center">Center.</param>
			/// <param name="direction">Direction.</param>
			/// <param name="normal">Normal.</param>
			/// <param name="numberOfSegments">Number of segments.</param>
			/// <param name="girth">Girth.</param>
			/// <param name="position">Position.</param>
			public bool AddSegment (int id, Vector3 center, Vector3 direction, Vector3 normal, 
				int numberOfSegments, float girth, float position, float positionAtSkin, BuilderType builder)
			{
				_ids.Add (id);
				_centers.Add (center);
				_directions.Add (direction);
				_normals.Add (normal);
				_segments.Add (numberOfSegments);
				_girths.Add (girth);
				_positions.Add (position);
				_positionsAtSkin.Add (positionAtSkin);
				_builders.Add (builder);
				return true;
			}
			/// <summary>
			/// Adds a position to the relevant position list by creating an average or replacing with existing positions using a priority.
			/// </summary>
			/// <param name="position">Relative position on the branch skin instance from 0 to 1.</param>
			/// <param name="range">Range on the new position from 0 to 1.</param>
			/// <param name="priority">Priority of the new position.</param>
			public bool AddRelevantPosition (float position, float range, int priority = 0) {
				if (position <= 0f || position >= 1f) return false;
				float minRange = position - (range / 2f);
				float maxRange = position + (range / 2f);
				// Check range with position 0.
				if (minRange < 0f) {
					// Position 0 has top priority, we drop the new point.
					return false;
				}
				// Check range with position 1.
				if (maxRange > 1f) {
					// Position 1 has top priority, we drop the new point.
					return false;
				}
				// If it is the first relevant position getting added.
				if (_relevantPositions.Count == 0) {
					_relevantPositions.Add (position);
					_relevantPositionPriorities.Add (priority);
					return true;
				}
				// Check range with intermediate positions.
				int candidateIndex = -1;
				for (int i = 0; i < _relevantPositions.Count; i++) {
					if (_relevantPositions[i] >= minRange && _relevantPositions[i] <= maxRange) {
						candidateIndex = i;
						break;
					}
				}
				if (candidateIndex < 0) {
					// TODO: add with order.
					int indexToInsert = _relevantPositions.FindLastIndex(e => e < position);
					if (indexToInsert == 0 || indexToInsert == -1) {
						_relevantPositions.Insert (0, position);
						_relevantPositionPriorities.Insert (0, priority);
					} else {
						_relevantPositions.Insert (indexToInsert + 1, position);
						_relevantPositionPriorities.Insert (indexToInsert + 1, priority);
					}
				} else {
					// Merge or drop.
					if (_relevantPositionPriorities[candidateIndex] == priority) {
						// Case of equal priorities: average.
						_relevantPositions [candidateIndex] = (_relevantPositions [candidateIndex] + position) / 2f;
					} else if (priority > _relevantPositionPriorities [candidateIndex]) {
						// Case of higher priority, replace.
						_relevantPositions [candidateIndex] = position;
						_relevantPositionPriorities [candidateIndex] = priority;
					}
					// Case of lower priority, then drop.
				}
				return true;
			}
			/// <summary>
			/// Get the list of relevant positions.
			/// </summary>
			/// <returns>List of relevant positions.</returns>
			public List<float> GetRelevantPositions () {
				return _relevantPositions;
			}
			/// <summary>
			/// Clear structural data for this instance.
			/// </summary>
			public void Clear () {
				vertices.Clear ();
				vertexInfos.Clear ();
				_branchIdToIndex.Clear ();
				_branchStartVertexIndex.Clear ();
				faceCount = 0;
				isFaceCountUsed = false;
				vertexOffset = 0;
				previousSegmentStartIndex = -1;
				_relevantPositions.Clear ();
				_relevantPositionPriorities.Clear ();
			}
			#endregion

			#region Branch Methods
			/// <summary>
			/// Gets the girth value at a position (0 to 1) on a BranchSkin.
			/// </summary>
			/// <param name="position">Position on the branch skin.</param>
			/// <param name="firstBranch">First branch on the BranchSkin.</param>
			/// <param name="branchSkin">BranchSkin instance to get the girth from.</param>
			/// <returns></returns>
			public static float GetGirthAtPosition (float position, BroccoTree.Branch firstBranch, BranchSkin branchSkin) {
				float girth = 1f;
				float accumLength = 0f;
				float targetLength = branchSkin.length * position;
				BroccoTree.Branch currentBranch = firstBranch;
				do {
					if (accumLength + currentBranch.length > targetLength) {
						return currentBranch.GetGirthAtLength (targetLength - accumLength);
					}
					accumLength += currentBranch.length;
					currentBranch = currentBranch.followUp;
				} while (currentBranch != null);
				return girth;
			}
			/// <summary>
			/// Translates a BranchSkin position at a child Branch position.
			/// </summary>
			/// <param name="branchSkin">BranchSkin instance to get the position from.</param>
			/// <param name="positionAtSkin">Position at the whole BranchSkin length (0-1).</param>
			/// <param name="firstBranch">First branch at the BranchSkin instance.</param>
			/// <param name="branchAtSkin">Gets the branch instance the asked position was found at, null if the position was not found.</param>
			/// <returns>The position at the child branch.</returns>
			public static float TranslateToPositionAtBranch (
				BranchSkin branchSkin, 
				float positionAtSkin,
				BroccoTree.Branch firstBranch,
				out BroccoTree.Branch branchAtSkin) 
			{
				branchAtSkin = null;
				float accumLength = 0f;
				float targetLength = branchSkin.length * positionAtSkin;
				BroccoTree.Branch currentBranch = firstBranch;
				do {
					if (accumLength + currentBranch.length > targetLength) {
						branchAtSkin = currentBranch;
						return (targetLength - accumLength) / currentBranch.length;
					}
					accumLength += currentBranch.length;
					currentBranch = currentBranch.followUp;
				} while (currentBranch != null);
				return 0;
			}
			/// <summary>
			/// Translates a position belonging to a branch instance within a branch skin, to a branch skin position.
			/// </summary>
			/// <param name="positionAtBranch">Position at the branch at skin.</param>
			/// <param name="branchAtSkin">Branch having the position and belonging to the branch skin.</param>
			/// <param name="firstBranch">First branch at the branch skin.</param>
			/// <param name="branchSkin">BranchSkin instance.</param>
			/// <returns>Position at the BranchSkin instance, if the branch does not belong to the branch skin then the returned value is -1.</returns>
			public static float TranslateToPositionAtBranchSkin (float positionAtBranch, BroccoTree.Branch branchAtSkin, BroccoTree.Branch firstBranch, BranchMeshBuilder.BranchSkin branchSkin) {
				float positionAtBranchSkin = -1;
				float accumLength = 0f;
				BroccoTree.Branch currentBranch = firstBranch;
				do {
					if (currentBranch.id == branchAtSkin.id) {
						return (accumLength + currentBranch.length * positionAtBranch) / branchSkin.length;
					}
					accumLength += currentBranch.length;
					currentBranch = currentBranch.followUp;
				} while (currentBranch != null);
				return positionAtBranchSkin;
			}
			public static float TranslateToPositionAtBranchSkin (float positionAtBranch, int branchId, BranchMeshBuilder.BranchSkin branchSkin) {
				float positionAtBranchSkin = -1f;
				BranchMeshBuilder.BranchSkinRange branchSkinRange = null;
				List<BranchMeshBuilder.BranchSkinRange> branchRanges = branchSkin.branchRanges;
				for (int i = 0; i < branchRanges.Count; i++) {
					if (branchRanges [i].branchId == branchId) {
						branchSkinRange = branchRanges [i];
					}
				}
				if (branchSkinRange != null) {
					positionAtBranchSkin = Mathf.Lerp (branchSkinRange.from, branchSkinRange.to, positionAtBranch);
				}
				return positionAtBranchSkin;
			}
			/// <summary>
			/// Checks if a BranchSkin instance position belong to a branch given its id.
			/// </summary>
			/// <param name="branchSkin">BranchSkin instance.</param>
			/// <param name="positionAtSkin">Position at branch skin.</param>
			/// <param name="firstBranch">First branch at the BranchSkin instance.</param>
			/// <param name="belongingBranchId">Id of the branch to search for.</param>
			/// <param name="inBranchPosition">Translated position on the found branch.</param>
			/// <returns>True if the position belongs to the branch given its id.</returns>
			public static bool PositionBelongsToBranch (
				BranchSkin branchSkin, 
				float positionAtSkin,
				BroccoTree.Branch firstBranch,
				int belongingBranchId, 
				out float inBranchPosition)
			{
				inBranchPosition = 0f;
				float accumLength = 0f;
				float targetLength = branchSkin.length * positionAtSkin;
				BroccoTree.Branch currentBranch = firstBranch;
				do {
					inBranchPosition = (targetLength - accumLength) / currentBranch.length;
					if (inBranchPosition < 0 || inBranchPosition > 1) return false;
					if (accumLength + currentBranch.length > targetLength && currentBranch.id == belongingBranchId) {
						//inBranchPosition = (targetLength - accumLength) / currentBranch.length;
						return true;
					}
					accumLength += currentBranch.length;
					currentBranch = currentBranch.followUp;
				} while (currentBranch != null);
				return false;
			}
			/// <summary>
			/// Registers the start index of vertices belonging to one branch.
			/// </summary>
			/// <param name="branchId">Id of the branch.</param>
			/// <param name="startVertexIndex">Start vertex index.</param>
			public void RegisterBranchStartVertexIndex (int branchId, int startVertexIndex) {
				if (!_branchIdToIndex.ContainsKey (branchId)) {
					_branchIdToIndex.Add (branchId, _branchStartVertexIndex.Count);
					_branchStartVertexIndex.Add (startVertexIndex);
				}
			}
			/// <summary>
			/// Gets the start index and vertex count for a branch given its id on the BranchSkin instance.
			/// </summary>
			/// <param name="branchId">Branch id.</param>
			/// <param name="startIndex">Start index on the mesh vertices.</param>
			/// <param name="vertexCount">Total number of vertices assigned to a branch on this BranchSkin instance.</param>
			public void GetVertexStartAndCount (int branchId, out int startIndex, out int vertexCount) {
				if (_branchIdToIndex.ContainsKey (branchId)) {
					int branchIndex = _branchIdToIndex [branchId];
					startIndex = _branchStartVertexIndex [branchIndex];
					if (branchIndex == _branchStartVertexIndex.Count - 1) {
						vertexCount = vertices.Count - _branchStartVertexIndex [branchIndex];
					} else {
						vertexCount = _branchStartVertexIndex [branchIndex + 1] - _branchStartVertexIndex [branchIndex];
					}
				} else {
					startIndex = 0;
					vertexCount = 0;
				}
			}
			#endregion
		}
		#endregion

		#region Var
		/// <summary>
		/// Enumeration for the types of builders known.
		/// </summary>
		public enum BuilderType {
			Default,
			Trunk,
			Shape
		}
		/// <summary>
		/// The minimum polygon sides to use for meshing.
		/// </summary>
		public int minPolygonSides = 3;
		/// <summary>
		/// The maximum polygon sides to use for meshing.
		/// </summary>
		public int maxPolygonSides = 8;
		/// <summary>
		/// The segment angle.
		/// </summary>
		public float segmentAngle = 0f;
		/// <summary>
		/// Use hard normals on the mesh.
		/// </summary>
		public bool useHardNormals = false;
		/// <summary>
		/// The global scale.
		/// </summary>
		float _globalScale = 1f;
		public float globalScale {
			get { return _globalScale; }
			set {
				_globalScale = value;
				foreach (var branchMeshBuilder in _branchMeshBuilders) {
					branchMeshBuilder.Value.SetGlobalScale (_globalScale);
				}
			}
		}
		/// <summary>
		/// Used to get the branches curve points. The lower the value more resolution, the higher lower resolution.
		/// </summary>
		public float branchAngleTolerance = 5f;
		/// <summary>
		/// Limit level of BranchSkin hierarchy to apply average normals to.
		/// </summary>
		public int averageNormalsLevelLimit = 0;
		/// <summary>
		/// The base radius scale factor.
		/// </summary>
		float baseRadiusScaleFactor = 0.80f;
		/// <summary>
		/// Dictionary with initial branch skins, the instance will try to take them from here before
		/// creating a new one for a branch.
		/// </summary>
		/// <typeparam name="int">Branch id.</typeparam>
		/// <typeparam name="BranchSkin">BranchSkin instance.</typeparam>
		/// <returns>Dictionary of SkinBranch instances.</returns>
		protected Dictionary<int, BranchSkin> idToBranchSkin = new Dictionary<int, BranchSkin> ();
		/// <summary>
		/// Reference to the first Branch on every BranchSkin instance generated by this builder.
		/// </summary>
		/// <typeparam name="int">Id of the BranchSkin instance.</typeparam>
		/// <typeparam name="BroccoTree.Branch">Branch instance.</typeparam>
		/// <returns>First branch on a BranchSkin instance.</returns>
		protected Dictionary<int, BroccoTree.Branch> idToFirstBranch = new Dictionary<int, BroccoTree.Branch> ();
		/// <summary>
		/// The branch skins generated or processed by this instance.
		/// </summary>
		public List<BranchSkin> branchSkins = new List<BranchSkin> ();
		/// <summary>
		/// Holds the information about the triangles created for the mesh, the branch id is used as an index.
		/// </summary>
		/// <returns></returns>
		public Dictionary<int, List<TriangleInfo>> triangleInfos = new Dictionary<int, List<TriangleInfo>> ();
		/// <summary>
		/// Dictionary of mesh builder instances used to mesh the branch skins.
		/// </summary>
		protected Dictionary<BuilderType, IBranchMeshBuilder> _branchMeshBuilders = new Dictionary<BuilderType, IBranchMeshBuilder> ();
		/// <summary>
		/// Vertex counter.
		/// </summary>
		public int vertexCount = 0;
		/// <summary>
		/// Maximum girth to expect from the tree.
		/// </summary>
		public float maxGirth = 0f;
		/// <summary>
		/// Maximum girth to expect from the tree.
		/// </summary>
		public float minGirth = 0f;
		/// <summary>
		/// Maximum average girth found on the branches.
		/// </summary>
		public float maxAvgGirth = 0f;
		/// <summary>
		/// Minimum average girth found on the branches.
		/// </summary>
		public float minAvgGirth = 0f;
		/// <summary>
		/// Hold count of the number of vertices created on the processed mesh.
		/// </summary>
		public int verticesGenerated { get; private set; }
		/// <summary>
		/// Hold count of the number of triangles created on the processed mesh.
		/// </summary>
		public int trianglesGenerated { get; private set; }
		/// <summary>
		/// The first and last segment vertices pairs.
		/// </summary>
		Dictionary<int, int> firstLastSegmentVertices = new Dictionary<int, int> ();
		/// <summary>
		/// Vertices to use on the mesh construction.
		/// </summary>
		List<Vector3> meshVertices = new List<Vector3> ();
		/// <summary>
		/// Triangles to use on the mesh construction.
		/// </summary>
		List<int> meshTriangles = new List<int> ();
		/// <summary>
		/// Normals to use on the mesh construction.
		/// </summary>
		List<Vector3> meshNormals = new List<Vector3> ();
		/// <summary>
		/// Temp var to hold base vertices.
		/// </summary>
		List<Vector3> baseVertices = new List<Vector3> ();
		/// <summary>
		/// Temp var to hold base vertex infos.
		/// </summary>
		List<BranchMeshBuilder.VertexInfo> baseVertexInfos = new List<BranchMeshBuilder.VertexInfo> ();
		/// <summary>
		/// Temp var to hold top vertices.
		/// </summary>
		List<Vector3> topVertices = new List<Vector3> ();
		/// <summary>
		/// Temp var to hold top vertex infos.
		/// </summary>
		List<BranchMeshBuilder.VertexInfo> topVertexInfos = new List<BranchMeshBuilder.VertexInfo> ();
		#endregion

		#region Singleton
		/// <summary>
		/// This class singleton.
		/// </summary>
		static BranchMeshBuilder _treeMeshFactory = null;
		/// <summary>
		/// Gets a builder instance.
		/// </summary>
		/// <returns>Singleton instance.</returns>
		public static BranchMeshBuilder GetInstance() {
			if (_treeMeshFactory == null) {
				_treeMeshFactory = new BranchMeshBuilder ();
			}
			return _treeMeshFactory;
		}
		#endregion

		#region Constructor
		public BranchMeshBuilder () {
			DefaultMeshBuilder defaultMeshBuilder = new DefaultMeshBuilder ();
			defaultMeshBuilder.SetGlobalScale (_globalScale);
			_branchMeshBuilders.Add (BuilderType.Default, defaultMeshBuilder);
		}
		#endregion

		#region TriangleInfo
		/// <summary>
		/// Clears the face infos.
		/// </summary>
		protected void ClearTriangleInfos () {
			var enumerator = triangleInfos.GetEnumerator ();
			while (enumerator.MoveNext()) {
				var faceInfoPair = enumerator.Current;
				faceInfoPair.Value.Clear ();
			}
			triangleInfos.Clear ();
		}
		#endregion

		#region Mesh Building
		/// <summary>
		/// Meshs the tree.
		/// </summary>
		/// <returns>The tree.</returns>
		/// <param name="tree">Tree.</param>
		public Mesh MeshTree (BroccoTree tree) { // TODO: cut
			Clear ();
			tree.RecalculateNormals (segmentAngle);
			tree.UpdateGirth ();
			WeighGirth (tree);
			Mesh treeMesh = new Mesh();

			// Get skin segments.
			for (int i = 0; i < tree.branches.Count; i++) {
				BranchSkin trunkBranchSkin = GetOrCreateBranchSkin (tree.branches[i], 0);
				PreprocessBranchSkin (trunkBranchSkin, tree.branches [i]);
				SkinBranch (tree.branches[i], trunkBranchSkin, 0f);
			}

			treeMesh = MeshBranchSkins ();

			BranchMeshMetaBuilder treeMeshMetaBuilder = new BranchMeshMetaBuilder ();
			treeMeshMetaBuilder.SetMeshUVs (treeMesh, tree, branchSkins);
			treeMeshMetaBuilder.SetZeroedUV2 (treeMesh);
			treeMeshMetaBuilder.SetMeshUV3s (treeMesh, tree, branchSkins);
			treeMeshMetaBuilder.SetZeroedUV4 (treeMesh);
			treeMeshMetaBuilder.SetMeshUV5s (treeMesh, tree, branchSkins);

			trianglesGenerated = (int)(treeMesh.triangles.Length / 3f);
			verticesGenerated = treeMesh.vertexCount;

			return treeMesh;
		}
		/// <summary>
		/// Meshs the tree.
		/// </summary>
		/// <returns>The tree.</returns>
		/// <param name="tree">Tree.</param>
		public void MeshTree (BroccoTree tree, Dictionary<int, Mesh> branchMeshes) {
			Clear ();
			branchMeshes.Clear ();
			tree.RecalculateNormals (segmentAngle);
			tree.UpdateGirth ();
			WeighGirth (tree);

			// Get skin segments.
			for (int i = 0; i < tree.branches.Count; i++) {
				BranchSkin trunkBranchSkin = GetOrCreateBranchSkin (tree.branches[i], 0);
				PreprocessBranchSkin (trunkBranchSkin, tree.branches [i]);
				SkinBranch (tree.branches[i], trunkBranchSkin, 0f);
			}

			MeshBranchSkins (branchMeshes);

			BranchMeshMetaBuilder treeMeshMetaBuilder = new BranchMeshMetaBuilder ();
			treeMeshMetaBuilder.BeginUsage (tree, branchSkins);
			var branchEnumerator = branchMeshes.GetEnumerator ();
			while (branchEnumerator.MoveNext ()) {
				/*
				treeMeshMetaBuilder.NewSetMeshUVs (branchEnumerator.Current.Value, branchEnumerator.Current.Key);
				treeMeshMetaBuilder.SetZeroedUV2 (branchEnumerator.Current.Value);
				treeMeshMetaBuilder.SetMeshUV3s (branchEnumerator.Current.Value, tree, branchSkins);
				treeMeshMetaBuilder.SetZeroedUV4 (branchEnumerator.Current.Value);
				treeMeshMetaBuilder.SetMeshUV5s (branchEnumerator.Current.Value, tree, branchSkins);
				*/
			}
			treeMeshMetaBuilder.EndUsage ();

			/*
			trianglesGenerated = (int)(treeMesh.triangles.Length / 3f);
			verticesGenerated = treeMesh.vertexCount;
			*/
		}
		/// <summary>
		/// Check if there is an instance of a mesh builder type already registered.
		/// </summary>
		/// <param name="builderType">Mesh builder type.</param>
		/// <returns>True is there is a mesh builder already registered.</returns>
		public bool ContainsMeshBuilder (BuilderType builderType) {
			return _branchMeshBuilders.ContainsKey (builderType);
		}
		/// <summary>
		/// Gets the branch mesh builder of a given type.
		/// </summary>
		/// <param name="builderType">Type of branch mesh builder.</param>
		/// <returns>Builder of the type specified, if not registered then it returns a default branch mesh builder.</returns>
		public IBranchMeshBuilder GetBranchMeshBuilder (BuilderType builderType) {
			if (_branchMeshBuilders.ContainsKey (builderType)) {
				return _branchMeshBuilders [builderType];
			}
			return _branchMeshBuilders [BuilderType.Default];
		}
		/// <summary>
		/// /Add a mesh builder of a builder type.
		/// </summary>
		/// <param name="branchMeshBuilder">Branch mesh builder.</param>
		/// <returns>True if no other mesh builder of the same builder type was present.</returns>
		public bool AddMeshBuilder (IBranchMeshBuilder branchMeshBuilder) {
			if (ContainsMeshBuilder (branchMeshBuilder.GetBuilderType ())) {
				return false;
			}
			_branchMeshBuilders.Add (branchMeshBuilder.GetBuilderType (), branchMeshBuilder);
			branchMeshBuilder.SetGlobalScale (_globalScale);
			return true;
		}
		/// <summary>
		/// Clear all the mesh builders registered on this instance.
		/// </summary>
		public void ClearMeshBuilders () {
			_branchMeshBuilders.Clear ();
			DefaultMeshBuilder defaultMeshBuilder = new DefaultMeshBuilder ();
			defaultMeshBuilder.SetGlobalScale (_globalScale);
			_branchMeshBuilders.Add (BuilderType.Default, defaultMeshBuilder);
		}
		#endregion

		#region Skin BranchSkin
		/// <summary>
		/// Clears the BranchSkin instances used as reference to begin building the branch meshes.
		/// </summary>
		public void ClearReferenceBranchSkins () {
			idToBranchSkin.Clear ();
			idToFirstBranch.Clear ();
		}
		/// <summary>
		/// Get a BranchSkin instance from the reference dictionary or creates a new one adding it to the dictionary.
		/// </summary>
		/// <param name="branchId">Branch id.</param>
		/// <param name="level">Hierarchy level for the BranchSkin.</param>
		/// <returns>BranchSkin instance.</returns>
		public BranchSkin GetOrCreateBranchSkin (BroccoTree.Branch firstBranch, int level) {
			if (idToBranchSkin.ContainsKey (firstBranch.id)) {
				return idToBranchSkin [firstBranch.id];
			}
			BranchSkin branchSkin = new BranchSkin (minPolygonSides, maxPolygonSides, minAvgGirth, maxAvgGirth);
			branchSkin.id = firstBranch.id;
			branchSkin.level = level;
			idToFirstBranch.Add (firstBranch.id, firstBranch);

			// Set branch skin length.
			BroccoTree.Branch currentBranch = firstBranch;
			float branchSkinLength = 0f;
			do {
				branchSkinLength += currentBranch.length;
				currentBranch = currentBranch.followUp;
			} while (currentBranch != null);
			branchSkin.length = branchSkinLength;

			// Add first branch id to branch skin relationship.
			idToBranchSkin.Add (firstBranch.id, branchSkin);

			// Set branch skin branch ranges.
			currentBranch = firstBranch;
			branchSkinLength = 0f;
			do {
				BranchSkinRange branchSkinRange = new BranchSkinRange ();
				branchSkinRange.from = branchSkinLength / branchSkin.length;
				branchSkinLength += currentBranch.length;
				branchSkinRange.to = branchSkinLength / branchSkin.length;
				branchSkinRange.branchId = currentBranch.id;
				currentBranch = currentBranch.followUp;
				branchSkin.AddBranchRange (branchSkinRange);
			} while (currentBranch != null);

			return branchSkin;
		}
		/// <summary>
		/// Gets an existing BranchSkin instance given its id.
		/// </summary>
		/// <param name="id">Id of the BranchSkin instance.</param>
		/// <returns>BranchSkin instance if found, otherwise null.</returns>
		public BranchSkin GetBranchSkin (int id) {
			if (idToBranchSkin.ContainsKey (id)) {
				return idToBranchSkin [id];
			}
			return null;
		}
		/// <summary>
		/// Preprocessing method to call right after a BranchSkin is created. Processing gets mainly done by
		/// the BranchSkin builders per range.
		/// </summary>
		/// <param name="branchSkin"></param>
		/// <param name="firstBranch"></param>
		protected void PreprocessBranchSkin (BranchSkin branchSkin, BroccoTree.Branch firstBranch, BranchSkin parentBranchSkin = null, BroccoTree.Branch parentBranch = null) {
			// Get every builder per range on the BranchSkin instance.
			for (int i = 0; i < branchSkin.ranges.Count; i++) {
				IBranchMeshBuilder branchMeshBuilder = GetBuilder (branchSkin.ranges [i].builderType);
				if (branchMeshBuilder != null) {
					branchMeshBuilder.PreprocessBranchSkinRange (i, branchSkin, firstBranch, parentBranchSkin, parentBranch);
				}
			}
			// Set position offset for each sprout in branch.
			BroccoTree.Branch currentBranch = firstBranch;
			do {
				for (int i = 0; i < currentBranch.sprouts.Count; i++) {
					IBranchMeshBuilder branchMeshBuilder = GetBuilderAtPosition (branchSkin, 
						BranchSkin.TranslateToPositionAtBranchSkin (currentBranch.sprouts [i].position, currentBranch, firstBranch, branchSkin));
					if (branchMeshBuilder != null) {
						currentBranch.sprouts [i].positionOffset = branchMeshBuilder.GetBranchSkinPositionOffset (currentBranch.sprouts [i].position, 
							currentBranch, currentBranch.sprouts [i].rollAngle, currentBranch.sprouts [i].forward, branchSkin);
					}
				}
				currentBranch = currentBranch.followUp;
			} while (currentBranch != null);
		}
		/// <summary>
		/// Skins the branch.
		/// </summary>
		/// <param name="branch">Branch.</param>
		/// <param name="branchSkin">Branch skin.</param>
		void SkinBranch (BroccoTree.Branch branch, BranchSkin branchSkin, float consumedLength, bool isFollowup = false) {
			//Skin Base
			if (consumedLength == 0) {
				if (branch.parent == null) {
					branchSkin.isTrunk = true;
				}
				consumedLength = SkinBranchBase (branch, branchSkin);
				if (consumedLength < 0) {
					return;
				}
			}

			// Skin Middle
			consumedLength = SkinBranchMiddleBody (branch, consumedLength, branchSkin, isFollowup);
			
			if (branch.followUp != null) {
				SkinBranch (branch.followUp, branchSkin, consumedLength, true);
			}

			if (branch.followUp == null) {
				// Skin Tip
				//SkinBranchTip (branch, branchSkin);
				branchSkins.Add (branchSkin);
			}

			for (int i = 0; i < branch.branches.Count; i++) {
				if (branch.branches[i] != branch.followUp) {
					BranchSkin childBranchSkin = GetOrCreateBranchSkin (branch.branches[i], branchSkin.level + 1);
					PreprocessBranchSkin (childBranchSkin, branch.branches [i], branchSkin, branch);
					branch.branches [i].Update ();
					SkinBranch (branch.branches[i], childBranchSkin, 0f);
				}
			}
		}
		/// <summary>
		/// Add mesh vertices for a branch base.
		/// </summary>
		/// <returns>The branch base.</returns>
		/// <param name="branch">Branch.</param>
		/// <param name="branchSkin">Branch skin.</param>
		protected virtual float SkinBranchBase (BroccoTree.Branch branch, BranchSkin branchSkin) {
			IBranchMeshBuilder branchMeshBuilder = GetBuilderAtPosition (branchSkin, 0);
			float girth = branch.GetGirthAtPosition (0f);
			int numberOfSegments = branchMeshBuilder.GetNumberOfSegments (branchSkin, 0f, girth);
			branchSkin.AddSegment (
				branch.id, 
				branch.origin, 
				branch.GetDirectionAtPosition (0), 
				branch.GetNormalAtPosition (0), 
				numberOfSegments, 
				girth, 
				0f,
				0f,
				branchMeshBuilder.GetBuilderType ());
			branchSkin.lastDirection = branch.direction;
			return 0f;
		}
		/// <summary>
		/// Add mesh vertices for a branch middle body.
		/// </summary>
		/// <param name="branch">Branch.</param>
		/// <param name="consumedLength">Consumed length so far on this branch.</param>
		/// <param name="branchSkin">Branch skin.</param>
		protected virtual float SkinBranchMiddleBody (BroccoTree.Branch branch, float consumedLength, BranchSkin branchSkin, bool isFollowup = false) {
			List<float> relevantPositions = new List<float> ();
			// Add Branches position to relevance list.
			for (int i = 0; i < branch.branches.Count; i++) {
				if (!branch.branches[i].isRoot) {
					relevantPositions.Add (branch.branches[i].position);
				}
			}
			// Add parent skin surface segment.
			if (branchSkin.level > 0 && branchSkin.level <= averageNormalsLevelLimit && branch.parent != null && !isFollowup) {
				float girthAtParent = branch.parent.GetGirthAtPosition (branch.position) / branch.length;
				relevantPositions.Add (girthAtParent);
			}

			// Add relevant positions from the BranchSkin if they belong to this branch.
			float inBranchPosition = 0f;
			List<float> bsRelevantPositions = branchSkin.GetRelevantPositions ();
			for (int i = 0; i < bsRelevantPositions.Count; i++) {
				if (BranchSkin.PositionBelongsToBranch (branchSkin, bsRelevantPositions[i], idToFirstBranch [branchSkin.id], branch.id, out inBranchPosition)) {
					relevantPositions.Add (inBranchPosition);
				}	
			}

			// Add the branch break position if present.
			if (branch.isBroken) {
				relevantPositions.Add (branch.breakPosition);
			}

			//branchRelevantPositions.AddRange (relevantPositions);
			for (int i = 0; i < relevantPositions.Count; i++) {
				//branchSkin.AddRelevantPosition (BranchSkin.TranslateToPositionAtBranchSkin (relevantPositions [i], branch.id, branchSkin), 0.01f, 1);
				branchSkin.AddRelevantPosition (BranchSkin.TranslateToPositionAtBranchSkin (relevantPositions [i], branch.id, branchSkin), 0.1f, 1);
			}

			// Order relevance list
			//relevantPositions.Sort ();
			// Add positions to relevance list.
			List<float> branchRelevantPositions = branchSkin.GetBranchRelevantPositions (branch.id, 2, true);

			branchRelevantPositions.Sort ();

			List<CurvePoint> curvePoints = branch.curve.GetPoints (branchAngleTolerance, branchRelevantPositions);


			IBranchMeshBuilder branchMeshBuilder;
			for (int i = (isFollowup?0:1); i < curvePoints.Count; i++) {
				float curvePointPosition = (float)(curvePoints[i].lengthPosition + consumedLength) / branchSkin.length;
				branchMeshBuilder = GetBuilderAtPosition (branchSkin, (float)curvePointPosition);
				float girth = branch.GetGirthAtPosition (curvePoints[i].relativePosition);
				int numberOfSegments = branchMeshBuilder.GetNumberOfSegments (branchSkin, curvePointPosition, girth);
				Vector3 avgDirection = curvePoints[i].forward;
				if (!branch.isBroken || curvePoints[i].relativePosition <= branch.breakPosition + 0.001f) {
				branchSkin.AddSegment (branch.id, branch.GetPointAtPosition (curvePoints[i].relativePosition), avgDirection,
					curvePoints [i].normal, numberOfSegments, girth, curvePoints[i].relativePosition, curvePointPosition, branchMeshBuilder.GetBuilderType ());
				}
				branchSkin.lastDirection = curvePoints[i].tangent;
			}
			consumedLength += branch.length;
			return consumedLength;
		}
		#endregion

		#region Skin BranchSkinRange
		/// <summary>
		/// Get the type of builder given a branchskin position.
		/// </summary>
		/// <param name="branchSkin">BranchSkin class.</param>
		/// <param name="position">Posiion on the branch skin.</param>
		/// <returns>Builder type assigned to the branch skin position.</returns>
		BuilderType GetBuilderTypeAtPosition (BranchSkin branchSkin, float position) {
			// If there are no ranges, everything gets processed with the default mesh builder.
			for (int i = 0; i < branchSkin.ranges.Count; i++) {
				if (position >= branchSkin.ranges [i].from && position <= branchSkin.ranges [i].to) {
					return branchSkin.ranges [i].builderType;
				}
			}
			return BuilderType.Default;
		}
		/// <summary>
		/// Gets the mesh builder assigned to a builder type.
		/// </summary>
		/// <param name="builderType">Builder type.</param>
		/// <returns>Builder assigned to a builder type.</returns>
		IBranchMeshBuilder GetBuilder (BuilderType builderType) {
			if (_branchMeshBuilders.ContainsKey (builderType)) {
				return _branchMeshBuilders [builderType];
			}
			return null;
		}
		/// <summary>
		/// Gets the mesh builder assigned at a branchskin position.
		/// </summary>
		/// <param name="branchSkin">BranchSkin class.</param>
		/// <param name="position">Position on the branch skin.</param>
		/// <returns>Builder assigned to the branch skin position.</returns>
		IBranchMeshBuilder GetBuilderAtPosition (BranchSkin branchSkin, float position) {
			BuilderType builderType = GetBuilderTypeAtPosition (branchSkin, position);
			_branchMeshBuilders [builderType].SetAngleTolerance (branchAngleTolerance);
			return _branchMeshBuilders [builderType];
		}
		#endregion
		
		#region Mesh Branch
		/// <summary>
		/// Builds a mesh from this clas branchSkins.
		/// </summary>
		/// <returns>Mesh object.</returns>
		protected Mesh MeshBranchSkins () { // TODO: remmove
			// Generate structural mesh data for reach branch skin.
			int vertexOffset = 0;
			for (int i= 0; i < branchSkins.Count; i++) {
				// Clear structural data on the branch skin.
				branchSkins [i].Clear ();
				branchSkins [i].vertexOffset = vertexOffset;

				Vector3[] basePolygon;
				Vector3[] topPolygon;
				List<float> baseRadialPositions = new List<float> ();
				List<float> topRadialPositions = new List<float> ();
				int currentBranchId = -1;
				bool topSegmentHasSamePosition = false;
				for (int j = 0; j < branchSkins [i].segments.Count; j++) {
				bool forceNewBaseVertices = false;
					if (j != branchSkins [i].segments.Count - 1 && branchSkins [i].positionsAtSkin [j] == branchSkins [i].positionsAtSkin [j + 1]) {
						topSegmentHasSamePosition = true;
					} else {
						topSegmentHasSamePosition = false;
					}
					if (!topSegmentHasSamePosition) {
						IBranchMeshBuilder meshBuilder = GetBranchMeshBuilder (branchSkins [i].builders [j]);
						forceNewBaseVertices = false;
						if (branchSkins [i].ids [j] != currentBranchId) {
							currentBranchId = branchSkins [i].ids [j];
							branchSkins [i].RegisterBranchStartVertexIndex (currentBranchId, branchSkins [i].vertices.Count);
							forceNewBaseVertices = true;
						}
						basePolygon = meshBuilder.GetPolygonAt (branchSkins [i], j, ref baseRadialPositions, globalScale, (branchSkins [i].isTrunk?1f:baseRadiusScaleFactor));
						if (j == branchSkins [i].segments.Count - 1) {
							topPolygon = null;
						} else {
							meshBuilder = GetBranchMeshBuilder (branchSkins [i].builders [j + 1]);
							topPolygon = meshBuilder.GetPolygonAt (branchSkins [i], j + 1, ref topRadialPositions, globalScale, (branchSkins [i].isTrunk?1f:baseRadiusScaleFactor));
						}
						AddSegmentMesh (branchSkins [i], j, basePolygon, topPolygon, ref baseRadialPositions, ref topRadialPositions, globalScale, useHardNormals, forceNewBaseVertices);
						baseRadialPositions.Clear ();
						topRadialPositions.Clear ();
					}
				}

				vertexOffset += branchSkins [i].vertices.Count;
			}

			Mesh mesh = new Mesh ();
			meshVertices.Clear ();
			meshTriangles.Clear ();
			TriangleInfo triangleInfo;

			// Set mesh vertices and triangles.
			for (int bsIndex = 0; bsIndex < branchSkins.Count; bsIndex ++) {
				meshVertices.AddRange (branchSkins [bsIndex].vertices);
				for (int i = 0; i < branchSkins [bsIndex].triangleInfos.Count; i++) {
					triangleInfo = branchSkins [bsIndex].triangleInfos [i];
					meshTriangles.Add (triangleInfo.firstVertexIndex + branchSkins [bsIndex].vertexOffset);
					meshTriangles.Add (triangleInfo.secondVertexIndex + branchSkins [bsIndex].vertexOffset);
					meshTriangles.Add (triangleInfo.thirdVertexIndex + branchSkins [bsIndex].vertexOffset);
				}
			}

			mesh.vertices = meshVertices.ToArray ();
			mesh.triangles = meshTriangles.ToArray ();

			mesh.RecalculateNormals ();
			if (!useHardNormals) SeamlessNormals (mesh);
			#if UNITY_5_6_OR_NEWER
			mesh.RecalculateTangents ();
			#else
			//BranchMeshMetaBuilder.GetInstance ().RecalculateTangents (mesh);
			#endif
			mesh.RecalculateBounds ();

			// Cleaning
			meshVertices.Clear ();
			meshTriangles.Clear ();

			return mesh;
		}
		/// <summary>
		/// Builds a mesh from this clas branchSkins.
		/// </summary>
		/// <returns>Mesh object.</returns>
		protected void MeshBranchSkins (Dictionary<int, Mesh> branchMeshes) {
			// Generate structural mesh data for reach branch skin.
			//int vertexOffset = 0;
			for (int i= 0; i < branchSkins.Count; i++) {
				// Clear structural data on the branch skin.
				branchSkins [i].Clear ();

				// BRANCH SKIN SEGMENT PROCESSING
				Vector3[] basePolygon;
				Vector3[] topPolygon;
				List<float> baseRadialPositions = new List<float> ();
				List<float> topRadialPositions = new List<float> ();
				int currentBranchId = -1;
				bool topSegmentHasSamePosition = false;
				bool forceNewBaseVertices = false;
				for (int j = 0; j < branchSkins [i].segments.Count; j++) {
					if (j != branchSkins [i].segments.Count - 1 && branchSkins [i].positionsAtSkin [j] == branchSkins [i].positionsAtSkin [j + 1]) {
						topSegmentHasSamePosition = true;
					} else {
						topSegmentHasSamePosition = false;
					}
					if (!topSegmentHasSamePosition) {
						IBranchMeshBuilder meshBuilder = GetBranchMeshBuilder (branchSkins [i].builders [j]);
						forceNewBaseVertices = false;
						if (branchSkins [i].ids [j] != currentBranchId) {
							currentBranchId = branchSkins [i].ids [j];
							branchSkins [i].RegisterBranchStartVertexIndex (currentBranchId, branchSkins [i].vertices.Count);
							forceNewBaseVertices = true;
						}
						basePolygon = meshBuilder.GetPolygonAt (branchSkins [i], j, ref baseRadialPositions, globalScale, (branchSkins [i].isTrunk?1f:baseRadiusScaleFactor));
						if (j == branchSkins [i].segments.Count - 1) {
							topPolygon = null;
						} else {
							meshBuilder = GetBranchMeshBuilder (branchSkins [i].builders [j + 1]);
							topPolygon = meshBuilder.GetPolygonAt (branchSkins [i], j + 1, ref topRadialPositions, globalScale, (branchSkins [i].isTrunk?1f:baseRadiusScaleFactor));
						}
						AddSegmentMesh (branchSkins [i], j, basePolygon, topPolygon, ref baseRadialPositions, ref topRadialPositions, globalScale, useHardNormals, forceNewBaseVertices);
						baseRadialPositions.Clear ();
						topRadialPositions.Clear ();
					}
				}
				//vertexOffset += branchSkins [i].vertices.Count; // TODO: move to mesh merge.

				// BRANCH SKIN MESHING
				Mesh mesh = new Mesh ();
				meshVertices.Clear ();
				meshTriangles.Clear ();
				TriangleInfo triangleInfo;

				meshVertices.AddRange (branchSkins [i].vertices);
				for (int j = 0; j < branchSkins [i].triangleInfos.Count; j++) {
					triangleInfo = branchSkins [i].triangleInfos [j];
					/*
					meshTriangles.Add (triangleInfo.firstVertexIndex + branchSkins [bsIndex].vertexOffset);
					meshTriangles.Add (triangleInfo.secondVertexIndex + branchSkins [bsIndex].vertexOffset);
					meshTriangles.Add (triangleInfo.thirdVertexIndex + branchSkins [bsIndex].vertexOffset);
					*/
					meshTriangles.Add (triangleInfo.firstVertexIndex);
					meshTriangles.Add (triangleInfo.secondVertexIndex);
					meshTriangles.Add (triangleInfo.thirdVertexIndex);
				}

				mesh.vertices = meshVertices.ToArray ();
				mesh.triangles = meshTriangles.ToArray ();

				mesh.RecalculateNormals ();
				if (!useHardNormals) SeamlessNormals (mesh);
				#if UNITY_5_6_OR_NEWER
				mesh.RecalculateTangents ();
				#else
				//BranchMeshMetaBuilder.GetInstance ().RecalculateTangents (mesh);
				#endif
				mesh.RecalculateBounds ();

				branchMeshes.Add (branchSkins[i].id, mesh);

				// Cleaning
				meshVertices.Clear ();
				meshTriangles.Clear ();
			}
		}
		/// <summary>
		/// Produces the data (vertex, triangles and faces) to build a mesh from a BranchSkin instance. 
		/// The vertices, triangles and faces data is saved at the BranchSkin instance.
		/// </summary>
		/// <param name="branchSkin">BranchSkin instance.</param>
		/// <param name="useHardNormals">Flag to use hard normals (independent normals per face, same number of triangles but 3x the vertices).</param>
		public virtual void AddSegmentMesh (
			BranchMeshBuilder.BranchSkin branchSkin, 
			int segmentIndex, 
			Vector3[] basePolygon, 
			Vector3[] topPolygon, 
			ref List<float> baseRadialPositions,
			ref List<float> topRadialPositions, 
			float scale, 
			bool useHardNormals,
			bool forceNewBaseVertices) 
		{
			if (useHardNormals) {
				AddSegmentMeshHardNormals (branchSkin, segmentIndex, basePolygon, topPolygon, ref baseRadialPositions, ref topRadialPositions, scale);
			} else {
				AddSegmentMeshSoftNormals (branchSkin, segmentIndex, basePolygon, topPolygon, ref baseRadialPositions, ref topRadialPositions, scale, forceNewBaseVertices);
			}
		}
		/// <summary>
		/// Produces soft normals (shared vertices) mesh data for a segment pair on the branch skin.
		/// </summary>
		/// <param name="branchSkin">BranchSkin instance.</param>
		/// <param name="segmentIndex">Base segment index.</param>
		/// <param name="basePolygon">Array of Vector3 positions corresponding to the base segment on the branch skin.</param>
		/// <param name="topPolygon">Array of Vector3 positions corresponding to the top segment on the branch skin.</param>
		/// <param name="baseRadialPositions">Array of radial positions (0 to 1 position on the segment circunference) corresponding to the base segment on the branch skin.</param>
		/// <param name="topRadialPositions">Array of radial positions (0 to 1 position on the segment circunference) corresponding to the top segment on the branch skin.</param>
		/// <param name="scale">Scale to apply to vertices.</param>
		public void AddSegmentMeshSoftNormals (
			BranchMeshBuilder.BranchSkin branchSkin, 
			int segmentIndex, 
			Vector3[] basePolygon, 
			Vector3[] topPolygon,
			ref List<float> baseRadialPositions,
			ref List<float> topRadialPositions, 
			float scale,
			bool forceNewBaseVertices) 
		{
			int refVertexIndex;

			// Add the base vertices if previous are not found or force flag is true.
			if (branchSkin.previousSegmentStartIndex == -1 || forceNewBaseVertices) {
				branchSkin.previousSegmentStartIndex = branchSkin.vertices.Count;
				int polygonIndex;
				float radialPositionFraction = 1f / (float) basePolygon.Length;
				for (int i = 0; i <= basePolygon.Length; i++) {
					polygonIndex = (i == basePolygon.Length)?0:i;
					branchSkin.vertices.Add (basePolygon [polygonIndex]);
					branchSkin.vertexInfos.Add (new BranchMeshBuilder.VertexInfo (polygonIndex, radialPositionFraction * i, segmentIndex));
				}
			}
			if (segmentIndex == branchSkin.segments.Count - 1) {
				// MESH TIP.
				refVertexIndex = branchSkin.vertices.Count;
				if (branchSkin.isFaceCountUsed) branchSkin.faceCount++;
				if (branchSkin.segments[segmentIndex] == 3) {
					// Add triangle.
					branchSkin.triangleInfos.Add (new BranchMeshBuilder.TriangleInfo (branchSkin.faceCount, segmentIndex, 
						branchSkin.previousSegmentStartIndex, 
						branchSkin.previousSegmentStartIndex + 1, 
						branchSkin.previousSegmentStartIndex + 2));
				} else if (branchSkin.segments[segmentIndex] == 4) {
					// Add quad.
					branchSkin.triangleInfos.Add (new BranchMeshBuilder.TriangleInfo (branchSkin.faceCount, segmentIndex, 
						branchSkin.previousSegmentStartIndex, 
						branchSkin.previousSegmentStartIndex + 1, 
						branchSkin.previousSegmentStartIndex + 3));
					branchSkin.triangleInfos.Add (new BranchMeshBuilder.TriangleInfo (branchSkin.faceCount, segmentIndex, 
						branchSkin.previousSegmentStartIndex + 1, 
						branchSkin.previousSegmentStartIndex + 2, 
						branchSkin.previousSegmentStartIndex + 3));
				} else {
					// ADD POLYGON
					int centerIndex = branchSkin.vertices.Count;
					branchSkin.vertices.Add (branchSkin.centers [segmentIndex] * scale);
					branchSkin.vertexInfos.Add (new BranchMeshBuilder.VertexInfo (0, 0, segmentIndex));
					for (int i = 0 ; i < branchSkin.segments[segmentIndex]; i++) {
						branchSkin.triangleInfos.Add (new BranchMeshBuilder.TriangleInfo (branchSkin.faceCount, segmentIndex, 
							branchSkin.previousSegmentStartIndex + i,
							branchSkin.previousSegmentStartIndex + i + 1,
							centerIndex));
					}
				}
				branchSkin.faceCount++;
				branchSkin.isFaceCountUsed = false;
			} else {
				// Add the top vertices.
				int polygonIndex;
				float radialPositionFraction = 1f / (float) topPolygon.Length;
				for (int i = 0; i <= topPolygon.Length; i++) {
					branchSkin.vertices.Add (topPolygon [(i == topPolygon.Length)?0:i]);
					polygonIndex = (i == topPolygon.Length)?0:i;
					if (i == 0) {
						branchSkin.vertexInfos.Add (new BranchMeshBuilder.VertexInfo (polygonIndex, radialPositionFraction * i, segmentIndex + 1, branchSkin.vertexInfos.Count + topPolygon.Length));
					} else {
						branchSkin.vertexInfos.Add (new BranchMeshBuilder.VertexInfo (polygonIndex, radialPositionFraction * i, segmentIndex + 1));
					}
				}
				if (basePolygon.Length == topPolygon.Length) {
					// BASE AND TOP POLYGON HAS THE SAME NUMbER OF SIDES.
					if (branchSkin.isFaceCountUsed) branchSkin.faceCount++;
					for (int i = 0; i < basePolygon.Length; i++) {
						// Add triangles.
						branchSkin.triangleInfos.Add (
							new BranchMeshBuilder.TriangleInfo (branchSkin.faceCount, segmentIndex, 
								branchSkin.previousSegmentStartIndex + i, 
								branchSkin.previousSegmentStartIndex + i + 1,
								branchSkin.previousSegmentStartIndex + basePolygon.Length + 1 + i));
						branchSkin.triangleInfos.Add (
							new BranchMeshBuilder.TriangleInfo (branchSkin.faceCount, segmentIndex, 
								branchSkin.previousSegmentStartIndex + i + 1, 
								branchSkin.previousSegmentStartIndex + basePolygon.Length + 1 + i + 1, 
								branchSkin.previousSegmentStartIndex + basePolygon.Length + 1 + i));
						branchSkin.faceCount++;
						branchSkin.isFaceCountUsed = false;
					}
				} else {
					// DYNAMIC SEGMENT MATCHING. (LOOKAHEAD IMPLEMENTATION).
					int maxSides = basePolygon.Length;
					int minSides = topPolygon.Length;
					bool invert = false;
					float baseAngleSeg = Mathf.PI * 2f / (float)basePolygon.Length;
					float topAngleSeg = Mathf.PI * 2f / (float)topPolygon.Length;
					int topIndex = 0;
					if (topPolygon.Length > basePolygon.Length) {
						maxSides = topPolygon.Length;
						minSides = basePolygon.Length;
						invert = true;
						baseAngleSeg = Mathf.PI * 2f / (float)topPolygon.Length;
						topAngleSeg = Mathf.PI * 2f / (float)basePolygon.Length;
					}
					for (int baseIndex = 0; baseIndex < maxSides; baseIndex++) {
						if ((baseIndex + 1) * baseAngleSeg >= (topIndex * topAngleSeg) + (topAngleSeg / 2f)) {
							// Quad
							if (branchSkin.isFaceCountUsed) branchSkin.faceCount++;
							if (invert) {
								// Add Quads
								branchSkin.triangleInfos.Add (new BranchMeshBuilder.TriangleInfo (branchSkin.faceCount, segmentIndex, 
									branchSkin.previousSegmentStartIndex + topIndex, // 2
									branchSkin.previousSegmentStartIndex + basePolygon.Length + 1 + baseIndex + 1, //1
									branchSkin.previousSegmentStartIndex + basePolygon.Length + 1 + baseIndex)); // 0
								branchSkin.triangleInfos.Add (new BranchMeshBuilder.TriangleInfo (branchSkin.faceCount, segmentIndex, 
									branchSkin.previousSegmentStartIndex + topIndex, // 2
									branchSkin.previousSegmentStartIndex + topIndex + 1,  // 3
									branchSkin.previousSegmentStartIndex + basePolygon.Length + 1 + baseIndex + 1)); // 1
							} else {
								// Add Quads
								branchSkin.triangleInfos.Add (new BranchMeshBuilder.TriangleInfo (branchSkin.faceCount, segmentIndex, 
									branchSkin.previousSegmentStartIndex + baseIndex, // 0 
									branchSkin.previousSegmentStartIndex + baseIndex + 1, // 1 
									branchSkin.previousSegmentStartIndex + basePolygon.Length + 1 + topIndex)); // 2
								branchSkin.triangleInfos.Add (new BranchMeshBuilder.TriangleInfo (branchSkin.faceCount, segmentIndex, 
									branchSkin.previousSegmentStartIndex + baseIndex + 1, 
									branchSkin.previousSegmentStartIndex + basePolygon.Length + 1 + topIndex + 1, 
									branchSkin.previousSegmentStartIndex + basePolygon.Length + 1 + topIndex));
							}
							topIndex++;
							branchSkin.faceCount++;
							branchSkin.isFaceCountUsed = false;
						} else {
							// Tris
							if (invert) {
								// Add triangles.
								branchSkin.triangleInfos.Add (new BranchMeshBuilder.TriangleInfo (branchSkin.faceCount, segmentIndex, 
									branchSkin.previousSegmentStartIndex + topIndex, // 2 
									branchSkin.previousSegmentStartIndex + basePolygon.Length + 1 + baseIndex + 1, // 1
									branchSkin.previousSegmentStartIndex + basePolygon.Length + 1 + baseIndex)); // 0
							} else {
								// Add triangles.
								branchSkin.triangleInfos.Add (new BranchMeshBuilder.TriangleInfo (branchSkin.faceCount, segmentIndex,
									branchSkin.previousSegmentStartIndex + baseIndex, // 0
									branchSkin.previousSegmentStartIndex + baseIndex + 1, // 1
									branchSkin.previousSegmentStartIndex + basePolygon.Length + 1 + topIndex)); // 2
							}
							branchSkin.isFaceCountUsed = true;
						}
					}
				}
				branchSkin.previousSegmentStartIndex = branchSkin.vertices.Count - topPolygon.Length - 1;
			}
		}
		/// <summary>
		/// Produces hard normals (shared vertices) mesh data for a segment pair on the branch skin.
		/// </summary>
		/// <param name="branchSkin">BranchSkin instance.</param>
		/// <param name="segmentIndex">Base segment index.</param>
		/// <param name="basePolygon">Array of Vector3 positions corresponding to the base segment on the branch skin.</param>
		/// <param name="topPolygon">Array of Vector3 positions corresponding to the top segment on the branch skin.</param>
		/// <param name="baseRadialPositions">Array of radial positions (0 to 1 position on the segment circunference) corresponding to the base segment on the branch skin.</param>
		/// <param name="topRadialPositions">Array of radial positions (0 to 1 position on the segment circunference) corresponding to the top segment on the branch skin.</param>
		/// <param name="scale">Scale to apply to vertices.</param>
		public void AddSegmentMeshHardNormals (
			BranchMeshBuilder.BranchSkin branchSkin, 
			int segmentIndex, 
			Vector3[] basePolygon, 
			Vector3[] topPolygon, 
			ref List<float> baseRadialPositions,
			ref List<float> topRadialPositions, 
			float scale) 
		{
			Vector3 vectorA, vectorB, vectorC, vectorD;
			int j;
			int refVertexIndex;
			int baseIndexOffset;
			float baseRadialPositionFraction, topRadialPositionFraction;
			
			topVertices.Clear ();
			topVertexInfos.Clear ();

			refVertexIndex = branchSkin.vertices.Count;
			baseRadialPositionFraction = 1f / (float) basePolygon.Length;
			baseIndexOffset = basePolygon.Length * 2;

			if (segmentIndex == branchSkin.segments.Count - 1) {
				// MESH TIP.
				if (branchSkin.isFaceCountUsed) branchSkin.faceCount++;
				if (branchSkin.segments[segmentIndex] == 3) {
					// Add vertices.
					branchSkin.vertices.Add (basePolygon [0]);
					branchSkin.vertices.Add (basePolygon [1]);
					branchSkin.vertices.Add (basePolygon [2]);
					// Add vertexInfos.
					branchSkin.vertexInfos.Add (new BranchMeshBuilder.VertexInfo (0, 0, segmentIndex));
					branchSkin.vertexInfos.Add (new BranchMeshBuilder.VertexInfo (1, baseRadialPositionFraction, segmentIndex));
					branchSkin.vertexInfos.Add (new BranchMeshBuilder.VertexInfo (2, baseRadialPositionFraction * 2, segmentIndex));
					// Add triangles.
					branchSkin.triangleInfos.Add (new BranchMeshBuilder.TriangleInfo (branchSkin.faceCount, segmentIndex, 
						refVertexIndex, 
						refVertexIndex + 1,
						refVertexIndex + 2));
				} else if (branchSkin.segments[segmentIndex] == 4) {
					// Add vertices.
					branchSkin.vertices.Add (basePolygon [0]);
					branchSkin.vertices.Add (basePolygon [1]);
					branchSkin.vertices.Add (basePolygon [2]);
					branchSkin.vertices.Add (basePolygon [3]);
					// Add vertexInfos.
					branchSkin.vertexInfos.Add (new BranchMeshBuilder.VertexInfo (0, 0, segmentIndex));
					branchSkin.vertexInfos.Add (new BranchMeshBuilder.VertexInfo (1, baseRadialPositionFraction, segmentIndex));
					branchSkin.vertexInfos.Add (new BranchMeshBuilder.VertexInfo (2, baseRadialPositionFraction * 2, segmentIndex));
					branchSkin.vertexInfos.Add (new BranchMeshBuilder.VertexInfo (3, baseRadialPositionFraction * 3, segmentIndex));
					// Add triangles.
					branchSkin.triangleInfos.Add (new BranchMeshBuilder.TriangleInfo (branchSkin.faceCount, segmentIndex, 
						refVertexIndex, refVertexIndex + 1, refVertexIndex + 3));
					branchSkin.triangleInfos.Add (new BranchMeshBuilder.TriangleInfo (branchSkin.faceCount, segmentIndex, 
						refVertexIndex + 1, refVertexIndex + 2, refVertexIndex + 3));
				} else {
					// ADD POLYGON
					j = 0;
					int centerIndex = refVertexIndex;
					branchSkin.vertices.Add (branchSkin.centers [segmentIndex] * scale);
					branchSkin.vertexInfos.Add (new BranchMeshBuilder.VertexInfo (0, 0, segmentIndex));
					for (int i = 0 ; i <= basePolygon.Length; i++) {
						j = i == basePolygon.Length?0:i;
						branchSkin.vertices.Add (basePolygon [j]);
						branchSkin.vertexInfos.Add (new BranchMeshBuilder.VertexInfo (j, baseRadialPositionFraction * i, segmentIndex));
					}
					for (int i = 0 ; i < basePolygon.Length; i++) {
						branchSkin.triangleInfos.Add (new BranchMeshBuilder.TriangleInfo (branchSkin.faceCount, segmentIndex, 
							refVertexIndex + i + 1,
							refVertexIndex + i + 2,
							centerIndex));
					}
				}
				branchSkin.faceCount++;
				branchSkin.isFaceCountUsed = false;
			} else {
				// Add base vertices.
				if (basePolygon.Length >= topPolygon.Length) {
					for (int i = 0; i < basePolygon.Length; i++) {
						j = (i == basePolygon.Length - 1)?0:i + 1;
						branchSkin.vertices.Add (basePolygon [i]);
						branchSkin.vertices.Add (basePolygon [j]);
						branchSkin.vertexInfos.Add (new BranchMeshBuilder.VertexInfo (i, baseRadialPositionFraction * i, segmentIndex));
						branchSkin.vertexInfos.Add (new BranchMeshBuilder.VertexInfo (j, baseRadialPositionFraction * (i + 1), segmentIndex));
					}
				}
				topRadialPositionFraction = 1f / (float) topPolygon.Length;
				if (basePolygon.Length == topPolygon.Length) {
					if (branchSkin.isFaceCountUsed) branchSkin.faceCount++;
					// BASE AND TOP POLYGON HAS THE SAME NUMbER OF SIDES.
					for (int i = 0; i < basePolygon.Length; i++) {
						j = (i == basePolygon.Length - 1)?0:i + 1;
						vectorC = topPolygon [i];
						vectorD = topPolygon [j];
						// Add vertices.
						topVertices.Add (vectorC);
						topVertices.Add (vectorD);
						// Add vertex infos.
						topVertexInfos.Add (new BranchMeshBuilder.VertexInfo (i, topRadialPositionFraction * i, segmentIndex + 1));
						topVertexInfos.Add (new BranchMeshBuilder.VertexInfo (j, topRadialPositionFraction * (i + 1), segmentIndex + 1));

						// Add triangles.
						branchSkin.triangleInfos.Add (new BranchMeshBuilder.TriangleInfo (branchSkin.faceCount, segmentIndex, 
							refVertexIndex + 1 + (i * 2), // 1
							refVertexIndex + baseIndexOffset + (i * 2), // 2
							refVertexIndex + (i * 2))); // 0
						branchSkin.triangleInfos.Add (new BranchMeshBuilder.TriangleInfo (branchSkin.faceCount, segmentIndex, 
							refVertexIndex + baseIndexOffset + 1 + (i * 2), // 3
							refVertexIndex + baseIndexOffset + (i * 2), // 2
							refVertexIndex + 1 + (i * 2))); // 1

						branchSkin.faceCount++;
						branchSkin.isFaceCountUsed = false;
					}
				} else {
					// DYNAMIC SEGMENT MATCHING. (LOOKAHEAD IMPLEMENTATION).
					int maxSides = basePolygon.Length;
					int minSides = topPolygon.Length;
					bool invert = false;
					float baseAngleSeg = Mathf.PI * 2f / (float)basePolygon.Length;
					float topAngleSeg = Mathf.PI * 2f / (float)topPolygon.Length;
					int topIndex = 0;
					int nextBaseIndex, nextTopIndex;
					if (topPolygon.Length > basePolygon.Length) {
						baseVertices.Clear ();
						baseVertexInfos.Clear ();
						baseIndexOffset = (basePolygon.Length * 2) + (topPolygon.Length - basePolygon.Length);
						maxSides = topPolygon.Length;
						minSides = basePolygon.Length;
						invert = true;
						baseAngleSeg = Mathf.PI * 2f / (float)topPolygon.Length;
						topAngleSeg = Mathf.PI * 2f / (float)basePolygon.Length;
					}
					int quadsToVisit = minSides;
					for (int baseIndex = 0; baseIndex < maxSides; baseIndex++) {
						if ((baseIndex + 1) * baseAngleSeg >= (topIndex * topAngleSeg) + (topAngleSeg / 2f) && quadsToVisit > 0) {
							// Quad
							quadsToVisit--;
							if (branchSkin.isFaceCountUsed) branchSkin.faceCount++;
							if (invert) {
								//if (topIndex == minSides) topIndex = 0;
								nextBaseIndex = baseIndex == topPolygon.Length - 1?0:baseIndex + 1;
								nextTopIndex = topIndex == basePolygon.Length - 1?0:topIndex + 1;
								vectorA = basePolygon [topIndex];
								vectorB = basePolygon [nextTopIndex];
								vectorC = topPolygon [baseIndex];
								vectorD = topPolygon [nextBaseIndex];
								// Add vertices.
								baseVertices.Add (vectorA);
								baseVertices.Add (vectorB);
								topVertices.Add (vectorC);
								topVertices.Add (vectorD);
								// Add vertex infos.
								baseVertexInfos.Add (new BranchMeshBuilder.VertexInfo (topIndex, baseRadialPositionFraction * topIndex, segmentIndex));
								baseVertexInfos.Add (new BranchMeshBuilder.VertexInfo (nextTopIndex, baseRadialPositionFraction * (topIndex + 1), segmentIndex));
								topVertexInfos.Add (new BranchMeshBuilder.VertexInfo (baseIndex, topRadialPositionFraction * baseIndex, segmentIndex + 1));
								topVertexInfos.Add (new BranchMeshBuilder.VertexInfo (nextBaseIndex, topRadialPositionFraction * (baseIndex + 1), segmentIndex + 1));
								// Add triangles.
								branchSkin.triangleInfos.Add (new BranchMeshBuilder.TriangleInfo (branchSkin.faceCount, segmentIndex, 
									refVertexIndex + baseVertices.Count - 2, // 2 C basePolygon
									refVertexIndex + baseIndexOffset + topVertices.Count - 1, // 1 B topPolygon + 1
									refVertexIndex + baseIndexOffset + topVertices.Count - 2)); // 0 A topPolygon
								branchSkin.triangleInfos.Add (new BranchMeshBuilder.TriangleInfo (branchSkin.faceCount, segmentIndex, 
									refVertexIndex + baseVertices.Count - 2, // 2 C basePolygon
									refVertexIndex + baseVertices.Count - 1, // 3 D basePolygon + 1
									refVertexIndex + baseIndexOffset + topVertices.Count - 1)); // 1 B topPolygon + 1
							} else {
								nextTopIndex = topIndex == topPolygon.Length - 1?0:topIndex + 1;
								vectorC = topPolygon [topIndex];
								vectorD = topPolygon [nextTopIndex];
								// Add vertices.
								topVertices.Add (vectorC);
								topVertices.Add (vectorD);
								// Add vertex infos.
								topVertexInfos.Add (new BranchMeshBuilder.VertexInfo (topIndex, topRadialPositionFraction * topIndex, segmentIndex + 1));
								topVertexInfos.Add (new BranchMeshBuilder.VertexInfo (nextTopIndex, topRadialPositionFraction * (topIndex + 1), segmentIndex + 1));
								// Add triangles.
								branchSkin.triangleInfos.Add (new BranchMeshBuilder.TriangleInfo (branchSkin.faceCount, segmentIndex, 
									refVertexIndex + (baseIndex * 2), // 0
									refVertexIndex + (baseIndex * 2) + 1, // 1
									refVertexIndex + baseIndexOffset + topVertices.Count - 2)); // 2
								branchSkin.triangleInfos.Add (new BranchMeshBuilder.TriangleInfo (branchSkin.faceCount, segmentIndex, 
									refVertexIndex + (baseIndex * 2) + 1, // 1
									refVertexIndex + baseIndexOffset + topVertices.Count - 1, // 3
									refVertexIndex + baseIndexOffset + topVertices.Count - 2)); // 2
							}
							topIndex++;
							branchSkin.faceCount++;
							branchSkin.isFaceCountUsed = false;
						} else {
							// Tris
							if (invert) {
								int _topIndex = topIndex;
								if (topIndex == minSides) {
									_topIndex = 0;
								}
								nextBaseIndex = baseIndex == topPolygon.Length - 1?0:baseIndex + 1;
								vectorA = topPolygon [baseIndex];
								vectorB = topPolygon [nextBaseIndex];
								vectorC = basePolygon [_topIndex];
								// Add vertices.
								topVertices.Add (vectorA);
								topVertices.Add (vectorB);
								baseVertices.Add (vectorC);
								// Add vertex infos.
								topVertexInfos.Add (new BranchMeshBuilder.VertexInfo (baseIndex, topRadialPositionFraction * baseIndex, segmentIndex + 1));
								topVertexInfos.Add (new BranchMeshBuilder.VertexInfo (nextBaseIndex, topRadialPositionFraction * (baseIndex + 1), segmentIndex + 1));
								baseVertexInfos.Add (new BranchMeshBuilder.VertexInfo (_topIndex, baseRadialPositionFraction * topIndex, segmentIndex));
								
								// Add triangles.
								branchSkin.triangleInfos.Add (new BranchMeshBuilder.TriangleInfo (branchSkin.faceCount, segmentIndex, 
									refVertexIndex + baseVertices.Count - 1, // 2 C basePolygon
									refVertexIndex + baseIndexOffset + topVertices.Count - 1, // 1 B topPolygon + 1
									refVertexIndex + baseIndexOffset + topVertices.Count - 2)); // 0 A topPolygon
							} else {
								if (topIndex == minSides) j = 0;
								else j = topIndex;
								vectorC = topPolygon [j];
								// Add vertices.
								topVertices.Add (vectorC);
								// Add vertex infos.
								topVertexInfos.Add (new BranchMeshBuilder.VertexInfo (j, topRadialPositionFraction * topIndex, segmentIndex + 1));
								// Add triangles.
								branchSkin.triangleInfos.Add (new BranchMeshBuilder.TriangleInfo (branchSkin.faceCount, segmentIndex, 
									refVertexIndex + (baseIndex * 2), 
									refVertexIndex + (baseIndex * 2) + 1, 
									refVertexIndex + baseIndexOffset + topVertices.Count - 1));
							}
							branchSkin.isFaceCountUsed = true;
						}
					}
					if (invert) {
						branchSkin.vertices.AddRange (baseVertices);
						branchSkin.vertexInfos.AddRange (baseVertexInfos);
					}
				}
				branchSkin.vertices.AddRange (topVertices);
				branchSkin.vertexInfos.AddRange (topVertexInfos);
			}
		}
		/// <summary>
		/// On non shared vertices mode: takes the first and final vertices of each 
		/// branch segment and sets their normals to a median.
		/// </summary>
		/// <param name="mesh">Mesh.</param>
		protected void SeamlessNormals (Mesh mesh) {
			Vector3 firstNormal, lastNormal, medianNormal;
			meshNormals.Clear ();
			meshNormals.AddRange (mesh.normals);

			VertexInfo vertexInfo;
			// Foreach branchskin (bsIndex).
			for (int bsIndex = 0; bsIndex < branchSkins.Count; bsIndex ++) {
				// Foreach vertexinfo (i)
				for (int i = 0; i < branchSkins [bsIndex].vertexInfos.Count; i++) {
					vertexInfo = branchSkins [bsIndex].vertexInfos [i];
					if (vertexInfo.lastIndexAtSegment >= 0) {
						firstNormal = meshNormals [i + branchSkins [bsIndex].vertexOffset];
						lastNormal = meshNormals [vertexInfo.lastIndexAtSegment + branchSkins [bsIndex].vertexOffset];
						medianNormal = ((firstNormal + lastNormal) / 2f).normalized;
						meshNormals [i + branchSkins [bsIndex].vertexOffset] = medianNormal;
						meshNormals [vertexInfo.lastIndexAtSegment + branchSkins [bsIndex].vertexOffset] = medianNormal;
					}
				}
			}

			// Foreach branchskin (bsIndex)
			if (averageNormalsLevelLimit > 0) {
				for (int bsIndex = 0; bsIndex < branchSkins.Count; bsIndex ++) {
					// Get the first branch on the BranchSkin.
					BroccoTree.Branch mainBranch = idToFirstBranch [branchSkins[bsIndex].id];
					if (mainBranch.parent == null) {
						BranchSkin childBranchSkin;
						for (int i = 0; i < mainBranch.branches.Count; i++) {
							if (!mainBranch.branches[i].IsFollowUp ()) {
								childBranchSkin = idToBranchSkin [mainBranch.branches[i].id];
								AverageNormals (mainBranch, mainBranch.branches[i], childBranchSkin, ref meshNormals);
							}
						}
					}
				}
			}

			mesh.SetNormals (meshNormals);
			// Cleaning
			firstLastSegmentVertices.Clear ();
			meshNormals.Clear ();
		}
		protected void AverageNormals (BroccoTree.Branch parentBranch, BroccoTree.Branch childBranch, BranchSkin childBranchSkin, ref List<Vector3> normals) {
			VertexInfo vertexInfo;
			Vector3 normal;
			Vector3 targetVector; // Each normal will have this vector as target to lerp to according to its position relative to the mesh skin surface.
			float segmentPosition;
			float parentGirth = parentBranch.GetGirthAtPosition (childBranch.position);
			float childGirth = childBranch.GetGirthAtLength (parentGirth);
			float lerpAtSurface = Mathf.Cos (childGirth / parentGirth * Mathf.PI / 2f); // Used to calculate the target vector at the surface for a normal.
			float surfacePosition = parentGirth / childBranchSkin.length;
			float thresholdPosition = surfacePosition * 2f;
			for (int j = 0; j < childBranchSkin.vertexInfos.Count; j++) {
				vertexInfo = childBranchSkin.vertexInfos [j];
				segmentPosition = childBranchSkin.positionsAtSkin[vertexInfo.segmentIndex];
				if (segmentPosition < thresholdPosition) {
					normal = meshNormals [j + childBranchSkin.vertexOffset];
					targetVector = Vector3.Lerp (normal, childBranch.direction, lerpAtSurface * 1.2f);
					if (segmentPosition <= surfacePosition + 0.05f) {
						normals [j + childBranchSkin.vertexOffset] = Vector3.Lerp (normal, targetVector, segmentPosition / surfacePosition);
					} else {
						normals [j + childBranchSkin.vertexOffset] = Vector3.Lerp (targetVector, normal, (segmentPosition - surfacePosition) / (thresholdPosition - surfacePosition));
					}
				}
			}
		}
		#endregion

		#region Geometry processing
		/// <summary>
		/// Traverses a tree branch structure and collects information about
		/// the girth on its branches.
		/// </summary>
		/// <param name="tree">Tree.</param>
		protected void WeighGirth (BroccoTree tree) {
			List<BroccoTree.Branch> branches = tree.GetDescendantBranches ();

			for (int i = 0; i < branches.Count; i++) {
				float baseGirth = branches[i].GetGirthAtPosition (0f);
				float mediumGirth = branches[i].GetGirthAtPosition (0f);
				float topGirth = branches[i].GetGirthAtPosition (0f);
				float avgGirth = (baseGirth + mediumGirth + topGirth) / 3f;

				// Check avg girth.
				if (minAvgGirth == -1f || avgGirth < minAvgGirth) {
					minAvgGirth = avgGirth;
				}
				if (avgGirth > maxAvgGirth) {
					maxAvgGirth = avgGirth;
				}

				// Check against minGirth.
				if (minGirth == -1 || baseGirth < minGirth) {
					minGirth = baseGirth;
				}
				if (mediumGirth < minGirth) {
					minGirth = mediumGirth;
				}
				if (topGirth < minGirth) {
					minGirth = topGirth;
				}

				// Check against maxGirth.
				if (baseGirth > maxGirth) {
					maxGirth = baseGirth;
				}
				if (mediumGirth > maxGirth) {
					maxGirth = mediumGirth;
				}
				if (topGirth > maxGirth) {
					maxGirth = topGirth;
				}
			}
		}
		#endregion

		#region Maintenance
		/// <summary>
		/// Clear this instance variables.
		/// </summary>
		public virtual void Clear () {
			vertexCount = 0;
			verticesGenerated = 0;
			trianglesGenerated = 0;
			minGirth = -1f;
			maxGirth = -1f;
			minAvgGirth = -1f;
			maxAvgGirth = -1f;
			branchSkins.Clear ();
			//ClearVertexInfos ();
			ClearTriangleInfos ();
		}
		#endregion
	}
}