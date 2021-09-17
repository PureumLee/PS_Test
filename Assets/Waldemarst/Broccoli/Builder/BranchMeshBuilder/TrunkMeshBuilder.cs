using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Broccoli.Base;
using Broccoli.Model;

namespace Broccoli.Builder {
	/// <summary>
	/// Gives methods to help create mesh segments using BranchSkin instances.
	/// </summary>
	public class TrunkMeshBuilder : IBranchMeshBuilder {
		#region Class BranchInfo
		/// <summary>
		/// Class containing the information to process the mesh of a branch.
		/// </summary>
		protected class BranchInfo {
			/// <summary>
			/// Modes on how to integrate roots to the trunk mesh.
			/// </summary>
			public enum RootMode {
				/// <summary>
				/// Roots are simulated at the trunk surface.
				/// </summary>
				Pseudo,
				/// <summary>
				/// The trunk mesh integrates with existing roots from the tree structure.
				/// </summary>
				Integration
			}
			/// <summary>
			/// Root mode to integrate to the trunk mesh.
			/// </summary>
			public RootMode rootMode = RootMode.Pseudo;
			public int displacementPoints = 3;
			public float girthAtBase;
			public float girthAtTop;
			public float minScaleAtBase;
			public float maxScaleAtBase;
			public float minAngleVariance;
			public float maxAngleVariance;
			public float range;
			public float twirl;
			public float strength;
			public AnimationCurve scaleCurve;
		}
		#endregion

		#region Vars
		protected Dictionary<int, BranchInfo> _branchInfos = new Dictionary<int, BranchInfo> ();
		protected Dictionary<int, BezierCurve> _baseCurves = new Dictionary<int, BezierCurve> ();
		protected Dictionary<int, BezierCurve> _topCurves = new Dictionary<int, BezierCurve> ();
		public float angleTolerance = 200f;
		float segmentPosition = 0f;
		float tTwirlAngle = 0f;
		float globalScale = 1f;
		#endregion

		#region Interface
		public virtual void SetAngleTolerance (float angleTolerance) {
			//this.angleTolerance = angleTolerance * 2.5f;
		}
		public virtual float GetAngleTolerance () {
			return angleTolerance;
		}
		public virtual void SetGlobalScale (float globalScale) { this.globalScale = globalScale; }
		public virtual float GetGlobalScale () { return this.globalScale; }
		/// <summary>
		/// Get the branch mesh builder type.
		/// </summary>
		/// <returns>Branch mesh builder type.</returns>
		public virtual BranchMeshBuilder.BuilderType GetBuilderType () {
			return BranchMeshBuilder.BuilderType.Trunk;
		}
		/// <summary>
		/// Called right after a BranchSkin is created.
		/// </summary>
		/// <param name="rangeIndex">Index of the branch skin range to process.</param>
		/// <param name="branchSkin">BranchSkin instance to process.</param>
		/// <param name="firstBranch">The first branch instance on the BranchSkin instance.</param>
		/// <param name="parentBranchSkin">Parent BranchSkin instance to process.</param>
		/// <param name="parentBranch">The parent branch of the first branch of the BranchSkin instance.</param>
		/// <returns>True if any processing gets done.</returns>
		public virtual bool PreprocessBranchSkinRange (int rangeIndex, BranchMeshBuilder.BranchSkin branchSkin, BroccoTree.Branch firstBranch, BranchMeshBuilder.BranchSkin parentBranchSkin = null, BroccoTree.Branch parentBranch = null) {
			// Add Relevant range positions.
			// TODO: Apply exponential distribution of points.
			// TODO: Implement relevant positions as mandatory vs optional (merging between points).
			for (int i = 0; i < branchSkin.ranges.Count; i++) {
				if (branchSkin.ranges[i].builderType == BranchMeshBuilder.BuilderType.Trunk &&
					branchSkin.ranges[i].to > branchSkin.ranges[i].from)
				{
					//float positionStep = (branchSkin.ranges[i].to - branchSkin.ranges[i].from) / (float)branchSkin.ranges[i].subdivisions;
					float relativePositionStep = 1f / (float)branchSkin.ranges[i].subdivisions;
					float relativeRelevantPosition = 0f;
					float finalRelevantPosition = 0f;
					for (int j = 1; j < branchSkin.ranges[i].subdivisions; j++) {
						relativeRelevantPosition = relativePositionStep * j;
						// Cuadratic
						relativeRelevantPosition = relativeRelevantPosition * relativeRelevantPosition;
						finalRelevantPosition = branchSkin.ranges[i].from + ((branchSkin.ranges[i].to - branchSkin.ranges[i].from) * relativeRelevantPosition);
						branchSkin.AddRelevantPosition (finalRelevantPosition, 0.2f);
					}
					return true;
				}
			}
			return false;
		}
		public virtual Vector3 GetBranchSkinPositionOffset (float positionAtBranch, BroccoTree.Branch branch, float rollAngle, Vector3 forward, BranchMeshBuilder.BranchSkin branchSkin) {
			return Vector3.zero;
		}
		#endregion

		#region Branches
		/// <summary>
		/// Registers values to process the mesh of a branch given its id.
		/// </summary>
		/// <param name="branchId"></param>
		/// <param name="scaleAtBase"></param>
		/// <param name="range"></param>
		/// <param name="strength"></param>
		/// <param name="scaleCurve"></param>
		public void RegisterPseudoTrunk (
			BroccoTree.Branch branch, 
			BranchMeshBuilder.BranchSkin branchSkin,
			int displacementPoints,
			float range,
			float minScaleAtBase,
			float maxScaleAtBase,
			float minAngleVariance,
			float maxAngleVariance,
			float twirl,
			float strength, 
			AnimationCurve scaleCurve)
		{
			// Return if the branch is already registered.
			if (_branchInfos.ContainsKey (branch.id)) {
				return;
			}

			// Save the parameters for the branch.
			BranchInfo branchInfo = new BranchInfo ();
			branchInfo.displacementPoints = displacementPoints;
			branchInfo.girthAtBase = BranchMeshBuilder.BranchSkin.GetGirthAtPosition (0f, branch, branchSkin);
			branchInfo.girthAtTop = BranchMeshBuilder.BranchSkin.GetGirthAtPosition (range, branch, branchSkin);
			branchInfo.minScaleAtBase = minScaleAtBase;
			branchInfo.maxScaleAtBase = maxScaleAtBase;
			branchInfo.minAngleVariance = minAngleVariance;
			branchInfo.maxAngleVariance = maxAngleVariance;
			branchInfo.twirl = twirl;
			branchInfo.range = range;
			branchInfo.strength = strength;
			branchInfo.scaleCurve = new AnimationCurve (scaleCurve.keys);
			_branchInfos.Add (branch.id, branchInfo);

			// Create curve at top of the branch.
			BezierCurve topCurve = GetBezierCircle (displacementPoints, branchInfo.girthAtTop, minAngleVariance, maxAngleVariance);
			_topCurves.Add (branch.id, topCurve);

			// Create curve at base of the branch.
			BezierCurve baseCurve = GetBezierCircle (displacementPoints, branchInfo.girthAtBase, minAngleVariance, maxAngleVariance);
			AddDisplacement (baseCurve, minScaleAtBase, maxScaleAtBase);
			_baseCurves.Add (branch.id, baseCurve);
		}
		#endregion

		#region Vertices
		public virtual Vector3[] GetPolygonAt (
			BranchMeshBuilder.BranchSkin branchSkin,
			int segmentIndex,
			ref List<float> radialPositions,
			float scale,
			float radiusScale = 1f)
		{
			// Set the scale.
			if (_branchInfos.ContainsKey (branchSkin.id)) {
				BranchInfo branchInfo = _branchInfos [branchSkin.id];
				segmentPosition = branchInfo.scaleCurve.Evaluate (Mathf.InverseLerp (0f, branchInfo.range, branchSkin.positionsAtSkin [segmentIndex]));
				tTwirlAngle = branchInfo.twirl * 2f * Mathf.PI;
			}
			Vector3[] polygon = GetPolygonAt (branchSkin.id, branchSkin.centers [segmentIndex], branchSkin.directions [segmentIndex], branchSkin.normals [segmentIndex],
				branchSkin.girths [segmentIndex], branchSkin.segments [segmentIndex], ref radialPositions, scale, radiusScale);
			//Debug.Log ("GetPolygonAt " + segmentIndex + ", segments: " + polygon.Length);
			return polygon;
		}
		/// <summary>
		/// Get an array of vertices around a center point with some rotation.
		/// </summary>
		/// <param name="branchSkinId">Id of the branch.</param>
		/// <param name="center">Center of the polygon</param>
		/// <param name="lookAt">Look at rotation.</param>
		/// <param name="normal"></param>
		/// <param name="radius">Radius of the polygon.</param>
		/// <param name="polygonSides">Number of sides for the polygon.</param>
		/// <param name="scale">Global scale to apply to the polygon.</param>
		/// <param name="radialPositions"></param>
		/// <param name="isBase"></param>
		/// <param name="radiusScale"></param>
		/// <returns>Vertices for the polygon <see cref="System.Collections.Generic.List`1[[UnityEngine.Vector3]]"/> points.</returns>
		Vector3[] GetPolygonAt (
			int branchSkinId,
			Vector3 center, 
			Vector3 lookAt, 
			Vector3 normal, 
			float radius, 
			int polygonSides,
			ref List<float> radialPositions,
			float scale,
			float radiusScale = 1f)
		{
			center *= scale;
			radius *= scale * radiusScale;
			
			Vector3 [] polygonVertices = new Vector3 [0];
			BezierCurve bezierCurve;
			bezierCurve = BezierCurve.Lerp (_baseCurves [branchSkinId], _topCurves [branchSkinId], segmentPosition);
			float tAngle = 0f;
			float cos, sin;

			if (polygonSides >= 3) {
				List<CurvePoint> points;
				if (GlobalSettings.experimentalMergeCurvePointsByDistanceEnabled) {
					points = BezierCurve.MergeCurvePointsByDistance (bezierCurve.GetPoints (angleTolerance), 0.05f);
				} else {
					points = bezierCurve.GetPoints (angleTolerance);
				}
				//List<CurvePoint> points = bezierCurve.GetPoints (angleTolerance);
				polygonVertices = new Vector3 [points.Count - 1];
				for (int i = 0; i < points.Count - 1; i++) {
					//Debug.Log ("Point " + i + ", pos: " + points[i].relativePosition);
					Vector3 point = points[i].position * scale;
					radialPositions.Add (points[i].relativePosition);
					
					//Add rotation.
					tAngle = Mathf.Lerp (tTwirlAngle, 0f, segmentPosition);

					cos = Mathf.Cos (tAngle);
					sin = Mathf.Sin (tAngle);
					point = new Vector3 (cos * point.x - sin * point.y, sin * point.x + cos * point.y);

					Quaternion rotation = Quaternion.LookRotation (lookAt, normal);
					point = rotation * point;
					polygonVertices [i] = point + center;
				}
			} else {
				Debug.LogError ("Polygon sides is expected to be equal or greater than 3.");
			}
			return polygonVertices;
		}
		/// <summary>
		/// Gets the number of segments (like polygon sides) as resolution for a branch position.
		/// </summary>
		/// <param name="branchSkin">BranchSkin instance.</param>
		/// <param name="branchSkinPosition">Position along the BranchSkin instance.</param>
		/// <param name="branchAvgGirth">Branch average girth.</param>
		/// <returns>The number polygon sides.</returns>
		public virtual int GetNumberOfSegments (BranchMeshBuilder.BranchSkin branchSkin, float branchSkinPosition, float branchAvgGirth) {
			float tPosition = 0;
			if (_branchInfos.ContainsKey (branchSkin.id)) {
				BranchInfo branchInfo = _branchInfos [branchSkin.id];
				tPosition = branchInfo.scaleCurve.Evaluate (Mathf.InverseLerp (0f, branchInfo.range, branchSkinPosition));
			}
			BezierCurve bezierCurve = BezierCurve.Lerp (_baseCurves [branchSkin.id], _topCurves [branchSkin.id], tPosition);
			List<CurvePoint> points;
			if (GlobalSettings.experimentalMergeCurvePointsByDistanceEnabled) {
				points = BezierCurve.MergeCurvePointsByDistance (bezierCurve.GetPoints (angleTolerance), 0.05f);
			} else {
				points = bezierCurve.GetPoints (angleTolerance);
			}
			//Debug.Log ("GetNumberOfSegments " + branchSkinPosition + ", segments: " + points.Count);
			//List<CurvePoint> points = bezierCurve.GetPoints (angleTolerance);
			return points.Count - 1;
		}
		#endregion

		#region Bezier Curve
		/// <summary>
		/// Get a circle bezier curve.
		/// </summary>
		/// <param name="pointyPoints">Half the number of nodes the curve will have.</param>
		/// <param name="radius">Rdius of the circle.</param>
		/// <param name="minAngleVariation">Minimum variation on the position of the nodes along the circle circumference.</param>
		/// <param name="maxAngleVariation">Maximum variation on the position of the nodes along the circle circumference.</param>
		/// <returns>Circular bezier curve.</returns>
		public static BezierCurve GetBezierCircle (int pointyPoints, float radius, float minAngleVariation, float maxAngleVariation) {
			// https://stackoverflow.com/questions/1734745/how-to-create-circle-with-b%C3%A9zier-curves
			// Handle length = (4/3)*tan(pi/(2n))
			BezierCurve curve = new BezierCurve ();
			pointyPoints *= 2;
			float stepAngle = Mathf.PI * 2 / (float)pointyPoints;
			float nodeX = 0f;
			float nodeY = 0f;
			BezierNode lastNode = null;
			float[] angles = new float[pointyPoints];
			float[] anglesDiff = new float[pointyPoints];
			float angleVariance = Random.Range (minAngleVariation, maxAngleVariation);

			// Get randomized angles.
			//for (int i = 0; i <= pointyPoints; i++) {
			for (int i = 0; i <= pointyPoints; i++) {
				if (i < pointyPoints) {
					angles[i] = i * stepAngle + Random.Range (-angleVariance / 2f, angleVariance / 2f);
				}
				if (i > 0) {
					anglesDiff[i - 1] = (i==pointyPoints?Mathf.PI * 2 + angles[0]:angles[i]) - angles[i - 1];
				}
			}
			for (int i = 0; i < pointyPoints; i++) {
				nodeX = Mathf.Cos (angles[i]) * radius;
				nodeY = Mathf.Sin (angles[i]) * radius;
				BezierNode node = new BezierNode (new Vector2 (nodeX, nodeY));
				if (i == 0) {
					lastNode = node;
				}
				node.handle1 = new Vector3 (nodeY, -nodeX) * (4/3) * Mathf.Tan(Mathf.PI/ (float)(2 * pointyPoints));
				node.handle2 = -node.handle1;
				curve.AddNode (node);
			}
			curve.AddNode (lastNode);
			return curve;
		}
		/// <summary>
		/// Add scaled displacement for even nodes fron the center of the circle.
		/// </summary>
		/// <param name="bezierCurve">Circular bezier curve.</param>
		/// <param name="minScale">Minimum scale to apply with the displacement.</param>
		/// <param name="maxScale">Maximum scale to apply with the displacement.</param>
		public void AddDisplacement (BezierCurve bezierCurve, float minScale, float maxScale) {
			bool applyDisplacement = false;
			float scale = 1f;
			for (int i = 0; i < bezierCurve.nodeCount; i++) {
				if (applyDisplacement) {
					scale = Random.Range (minScale, maxScale);
					bezierCurve.nodes [i].position *= scale;
					bezierCurve.nodes [i].handle1 *= scale;
					bezierCurve.nodes [i].handle2 *= scale;
				}
				applyDisplacement = !applyDisplacement;
			}
		}
		#endregion
	}
}