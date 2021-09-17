using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Broccoli.Model;

namespace Broccoli.Builder
{
	/// <summary>
	/// Provides methods to set uv, uv2 and color32 values on branch meshes.
	/// </summary>
	public class BranchMeshMetaBuilder {
		#region Vars
		/// <summary>
		/// The displacement change on the x axis for applying UV values.
		/// </summary>
		public float displacementDeltaX = 0f;
		/// <summary>
		/// The displacement change on the y axis for applying UV values.
		/// </summary>
		public float displacementDeltaY = 0f;
		/// <summary>
		/// UV mapping is sensitive to the girth of the branch section.
		/// </summary>
		public bool isGirthSensitive = false;
		/// <summary>
		/// UV mapping offset from parent branch.
		/// </summary>
		public bool applyMappingOffsetFromParent = false;
		/// <summary>
		/// How much the UV mapping is sensitive to the girth.
		/// </summary>
		public float girthSensitivity = 0.8f;
		/// <summary>
		/// Max girth found at the tree.
		/// </summary>
		private float maxGirth = 1f;
		/// <summary>
		/// Relationship between branches given their id and the branch skin they belong to.
		/// </summary>
		/// <typeparam name="int">Branch id.</typeparam>
		/// <typeparam name="BranchMeshBuilder.BranchSkin">Branch skin instance.</typeparam>
		private Dictionary<int, BranchMeshBuilder.BranchSkin> _branchIdToBranchSkin = new Dictionary<int, BranchMeshBuilder.BranchSkin> ();
		/// <summary>
		/// Relationship between branches given their id.
		/// </summary>
		/// <typeparam name="int">Branch id.</typeparam>
		/// <typeparam name="BroccoTree.Branch">Branch instance.</typeparam>
		private Dictionary<int, BroccoTree.Branch> _branchIdToBranch = new Dictionary<int, BroccoTree.Branch> ();
		/// <summary>
		/// Relationship between a branch and its length at the tree.
		/// </summary>
		/// <typeparam name="int">Branch id.</typeparam>
		/// <typeparam name="float">Length at position 0 of the branch.</typeparam>
		private Dictionary<int, float> _branchIdToAccumLength = new Dictionary<int, float> ();
		#endregion

		#region Singleton
		/// <summary>
		/// This class singleton instance.
		/// </summary>
		static BranchMeshMetaBuilder _branchMeshMetaBuilder = null;
		/// <summary>
		/// Gets the singleton instance for this class.
		/// </summary>
		/// <returns>The instance.</returns>
		public static BranchMeshMetaBuilder GetInstance () {
			if (_branchMeshMetaBuilder == null) {
				_branchMeshMetaBuilder = new BranchMeshMetaBuilder ();
			}
			return _branchMeshMetaBuilder;
		}
		#endregion

		#region Setup
		/// <summary>
		/// Process branch skins and tree to apply mapping to branch meshes.
		/// </summary>
		/// <param name="tree">BroccoTree to process.</param>
		/// <param name="branchSkins">List of branch skins belonging to the tree.</param>
		public void BeginUsage (BroccoTree tree,List<BranchMeshBuilder.BranchSkin> branchSkins) {
			BuildBranchIdToBranchSkin (branchSkins);
			_branchIdToBranch.Clear ();
			_branchIdToAccumLength.Clear ();
			for (int i = 0; i < tree.branches.Count; i++) {
				BuildBranchIdToBranch (tree.branches[i], 0f);
			}
			maxGirth = tree.maxGirth;
		}
		/// <summary>
		/// Clears data after this meta builder has been used.
		/// </summary>
		public void EndUsage () {
			_branchIdToBranchSkin.Clear ();
			_branchIdToBranch.Clear ();
			_branchIdToAccumLength.Clear ();
		}
		/// <summary>
		/// Builds a relationship structure between branch ids and the branch skin instance they belong to.
		/// </summary>
		/// <param name="branchSkins">List of BranchSkin instances.</param>
		void BuildBranchIdToBranchSkin (List<BranchMeshBuilder.BranchSkin> branchSkins) {
			_branchIdToBranchSkin.Clear ();
			for (int i = 0; i < branchSkins.Count; i++) {
				for (int j = 0; j < branchSkins [i].ids.Count; j++) {
					if (!_branchIdToBranchSkin.ContainsKey (branchSkins [i].ids [j])) {
						_branchIdToBranchSkin.Add (branchSkins [i].ids [j], branchSkins [i]);
					}
				}
			}
		}
		/// <summary>
		/// Builds a relationship between branches and their ids.
		/// </summary>
		/// <param name="branch">Branch to process.</param>
		void BuildBranchIdToBranch (BroccoTree.Branch branch, float accumLength) {
			_branchIdToBranch.Add (branch.id, branch);
			_branchIdToAccumLength.Add (branch.id, accumLength);
			for (int i = 0; i < branch.branches.Count; i++) {
				BuildBranchIdToBranch (branch.branches[i], accumLength + branch.length * branch.branches[i].position);
			}
		}
		#endregion

		#region UV Methods
		public void NewSetMeshUVs (Mesh branchSkinMesh, int branchSkinId) {
			BranchMeshBuilder.BranchSkin branchSkin = _branchIdToBranchSkin [branchSkinId];
			float displacementX = 0f;
			float vPosition = 0f;
			float girth = 0f;
			float accumBranchLength = _branchIdToAccumLength [branchSkinId];
			float lastLength = accumBranchLength;
			float localAccumBranchLength = accumBranchLength;
			float displacementFactor;
			if (displacementDeltaY < 0) {
				displacementFactor = 1f / (1 - displacementDeltaY);
			} else {
				displacementFactor = 1 + displacementDeltaY;
			}
			float perimeterLength = 2 * Mathf.PI * maxGirth * displacementFactor;
			int segmentIndex;
			List<Vector2> uvs = new List<Vector2> ();
			branchSkinMesh.GetUVs (0, uvs);
			if (uvs.Count == 0) {
				uvs = new List<Vector2> (new Vector2[branchSkinMesh.vertexCount]);
			}
			for (int i = 0; i < branchSkinMesh.vertexCount; i++) {
				segmentIndex = branchSkin.vertexInfos [i].segmentIndex;
				if (branchSkin.vertexInfos [i].radiusIndex == 0) {
					if (isGirthSensitive && girthSensitivity > 0) {
						girth = branchSkin.girths [segmentIndex];
						if (girth < 0.3f) {
							girth = 0.3f;
						}
						/*
						localAccumBranchLength += (accumBranchLength + branchSkin.positionsAtSkin[segmentIndex] * branchSkin.length / girth / maxGirth / girthSensitivity) - lastLength;
						lastLength = localAccumBranchLength;
						*/
						localAccumBranchLength += (accumBranchLength + branchSkin.positionsAtSkin[segmentIndex] * branchSkin.length / girth / maxGirth / girthSensitivity) - lastLength;
						lastLength = localAccumBranchLength;
					} else {
						localAccumBranchLength += (accumBranchLength + branchSkin.positionsAtSkin [segmentIndex] * branchSkin.length) - lastLength;
						lastLength = localAccumBranchLength;
					}
					displacementX = displacementDeltaX * localAccumBranchLength / perimeterLength;
					vPosition = localAccumBranchLength / perimeterLength;
				}
				uvs [i] = new Vector2 (
					GetUPositionValue (
						branchSkin.vertexInfos [i].radiusPosition, 
						branchSkin.segments [segmentIndex], 
						displacementX),
					vPosition);
			}
			branchSkinMesh.SetUVs (0, uvs);
			/*
			for (int i = 0; i < branch.branches.Count; i++) {
				if (_branchIdToBranchSkin.ContainsKey (branch.branches[i].id)) {
					if (branch.branches[i] == branch.followUp) {
						SetMeshUVs (uvs, branch.branches[i], perimeterLength, localAccumBranchLength);
					} else {
						float uvMappingOffset = 0f;
						if (applyMappingOffsetFromParent) {
							uvMappingOffset = lastLength + ((localAccumBranchLength - lastLength) * branch.branches[i].position);
						}
						SetMeshUVs (uvs, branch.branches[i], perimeterLength, uvMappingOffset); 
					}
				}
			}
			*/
		}
		/// <summary>
		/// Sets UV values for a tree.
		/// </summary>
		/// <returns>Array of assigned UV values.</returns>
		/// <param name="mesh">Mesh.</param>
		/// <param name="tree">Tree.</param>
		/// <param name="branchSkins">List of BranchSkin instances for the current tree.</param>
		public Vector2[] SetMeshUVs (Mesh mesh, 
			BroccoTree tree, 
			List<BranchMeshBuilder.BranchSkin> branchSkins)
		{ // TODO:Cut
			Vector2[] uvs = new Vector2 [mesh.vertices.Length];
			BuildBranchIdToBranchSkin (branchSkins);
		
			if (mesh != null) {
				float displacementFactor;
				if (displacementDeltaY < 0) {
					displacementFactor = 1f / (1 - displacementDeltaY);
				} else {
					displacementFactor = 1 + displacementDeltaY;
				}
				maxGirth = tree.maxGirth;
				float perimeterLength = 2 * Mathf.PI * maxGirth * displacementFactor;
				for (int i = 0; i < tree.branches.Count; i++) {
					if (_branchIdToBranchSkin.ContainsKey (tree.branches [i].id)) {
						SetMeshUVs (uvs, tree.branches[i], perimeterLength, 0f);
					}
				}
				if (mesh.vertices.Length == uvs.Length) {
					mesh.uv = uvs;
				} else {
					Vector2[] extendedUVs = new Vector2[mesh.vertices.Length];
					uvs.CopyTo (extendedUVs, 0);
					mesh.uv = extendedUVs;
				}
			}
			return uvs;
		}
		/// <summary>
		/// Set UV values for a branch.
		/// </summary>
		/// <param name="uvs">Array of UV values.</param>
		/// <param name="branch">Branch instance.</param>
		/// <param name="perimeterLength">Length of the segment perimeter at the begining of the branch.</param>
		/// <param name="accumBranchLength">Accumulated branch length.</param>
		void SetMeshUVs (Vector2[] uvs, 
			BroccoTree.Branch branch, 
			float perimeterLength,
			float accumBranchLength = 0f) 
		{
			int branchVertexStartIndex;
			int branchVertexCount;
			BranchMeshBuilder.BranchSkin branchSkin = _branchIdToBranchSkin [branch.id];
			branchSkin.GetVertexStartAndCount (branch.id, out branchVertexStartIndex, out branchVertexCount);

			float displacementX = 0f;
			float vPosition = 0f;
			float girth = 0f;
			float lastLength = accumBranchLength;
			float localAccumBranchLength = accumBranchLength;
			int segmentIndex;
			for (int i = branchVertexStartIndex; i < branchVertexStartIndex + branchVertexCount; i ++) {
				segmentIndex = branchSkin.vertexInfos [i].segmentIndex;
				if (branchSkin.vertexInfos [i].radiusIndex == 0) {
					if (isGirthSensitive && girthSensitivity > 0) {
						girth = branchSkin.girths [segmentIndex];
						if (girth < 0.3f) {
							girth = 0.3f;
						}
						localAccumBranchLength += 
							(accumBranchLength + branchSkin.positions [segmentIndex] * branch.length / girth / maxGirth / girthSensitivity) - lastLength;
						lastLength = localAccumBranchLength;
					} else {
						localAccumBranchLength += (accumBranchLength + branchSkin.positions [segmentIndex] * branch.length) - lastLength;
						lastLength = localAccumBranchLength;
					}
					displacementX = 
						displacementDeltaX * localAccumBranchLength / perimeterLength;
					vPosition = localAccumBranchLength / perimeterLength;
				}
				uvs [branchSkin.vertexOffset + i] = new Vector2 (
					GetUPositionValue (
						branchSkin.vertexInfos [i].radiusPosition, 
						branchSkin.segments [segmentIndex], 
						displacementX),
					vPosition);
			}
			for (int i = 0; i < branch.branches.Count; i++) {
				if (_branchIdToBranchSkin.ContainsKey (branch.branches[i].id)) {
					if (branch.branches[i] == branch.followUp) {
						SetMeshUVs (uvs, branch.branches[i], perimeterLength, localAccumBranchLength);
					} else {
						float uvMappingOffset = 0f;
						if (applyMappingOffsetFromParent) {
							uvMappingOffset = lastLength + ((localAccumBranchLength - lastLength) * branch.branches[i].position);
						}
						SetMeshUVs (uvs, branch.branches[i], perimeterLength, uvMappingOffset); 
					}
				}
			}
		}
		/// <summary>
		/// Sets UV3 values for a tree.
		/// </summary>
		/// <returns>Array of assigned UV values.</returns>
		/// <param name="mesh">Mesh.</param>
		/// <param name="tree">Tree.</param>
		/// <param name="branchSkins">List of BranchSkin instances for the current tree.</param>
		public void SetMeshUV3s (Mesh mesh, 
			BroccoTree tree, 
			List<BranchMeshBuilder.BranchSkin> branchSkins)
		{
			Vector4[] _uv3s = new Vector4[mesh.vertices.Length];
			Vector3[] _vertices = mesh.vertices;
			BuildBranchIdToBranchSkin (branchSkins);
			List<Vector4> uv3s = new List<Vector4> (_uv3s);
			if (mesh != null) {
				for (int i = 0; i < _uv3s.Length; i++) {
					uv3s[i] = new Vector4 (_vertices[i].x, _vertices[i].y, _vertices[i].z, 0.25f);
				}
			}
			mesh.SetUVs (2, uv3s);
		}
		/// <summary>
		/// Sets UV3 values for a tree.
		/// </summary>
		/// <returns>Array of assigned UV values.</returns>
		/// <param name="mesh">Mesh.</param>
		/// <param name="tree">Tree.</param>
		/// <param name="branchSkins">List of BranchSkin instances for the current tree.</param>
		public void SetMeshUV5s (Mesh mesh, 
			BroccoTree tree, 
			List<BranchMeshBuilder.BranchSkin> branchSkins)
		{
			#if UNITY_2018_2_OR_NEWER
			BuildBranchIdToBranchSkin (branchSkins);
			Vector4[] _uv5s = new Vector4[mesh.vertices.Length];
			List<Vector4> uv5s = new List<Vector4> (_uv5s);
			if (mesh != null) {
				for (int h = 0; h < tree.branches.Count; h++) {
					SetMeshUV5s (uv5s, tree.branches[h]);
				}
			}
			mesh.SetUVs (4, uv5s);
			#endif
		}
		/// <summary>
		/// Set UV5 values for a branch.
		/// </summary>
		/// <param name="uv5s">UV5 array for the mesh.</param>
		/// <param name="branch">Branch instance.</param>
		void SetMeshUV5s (List<Vector4> uv5s, 
			BroccoTree.Branch branch) 
		{
			if (_branchIdToBranchSkin.ContainsKey (branch.id)) {
				BranchMeshBuilder.BranchSkin branchSkin = _branchIdToBranchSkin [branch.id];
				int startIndex, vertexCount;
				branchSkin.GetVertexStartAndCount (branch.id, out startIndex, out vertexCount);
				for (int i = startIndex; i < startIndex + vertexCount; i++) {
					uv5s [branchSkin.vertexOffset + i] = 
						new Vector4 (branch.id, branch.helperStructureLevelId, branch.isTuned?1:0, 0);
				}
			}
			for (int h = 0; h < branch.branches.Count; h++) {
				SetMeshUV5s (uv5s, branch.branches[h]);
			}
		}
		/// <summary>
		/// Gets the U position value.
		/// </summary>
		/// <returns>The U position value.</returns>
		/// <param name="radialPosition">Radial position (0, 1).</param>
		/// <param name="polygonSides">Polygon sides.</param>
		/// <param name="displacementX">Displacement x.</param>
		float GetUPositionValue (float radialPosition, int polygonSides, float displacementX = 0) {
			//radiusIndex = polygonSides - radiusIndex - 1;
			//float deltaRadius = 1f / polygonSides;
			float pos = radialPosition + displacementX;
			//return pos + 1f;
			return pos;
		}
		/// <summary>
		/// Gets the V position value.
		/// </summary>
		/// <returns>The V position value.</returns>
		/// <param name="vPosition">V position.</param>
		/// <param name="radiusIndex">Radius index.</param>
		/// <param name="polygonSides">Polygon sides.</param>
		/// <param name="displacementY">Displacement y.</param>
		float GetVPositionValue (float vPosition, float radialPosition, int polygonSides, float displacementY = 0) {
			if (displacementY == 0) {
				return vPosition;
			} else {
				float vOffset = Mathf.Sin (radialPosition * 2 * Mathf.PI) * displacementY;
				return vPosition + vOffset;
			}
		}
		#endregion

		#region UV2 and Color
		/// <summary>
		/// Sets the UV2 property of a mesh to zero.
		/// </summary>
		/// <returns>Array of Vector2 set to zero.</returns>
		/// <param name="mesh">Mesh to apply the color to.</param>
		public Vector2[] SetZeroedUV2 (Mesh mesh) {
			Vector2[] zeroedUV2s = new Vector2[mesh.vertices.Length];
			mesh.uv2 = zeroedUV2s;
			return zeroedUV2s;
		}
		public Vector2[] SetZeroedUV4 (Mesh mesh) {
			Vector2[] zeroedUV4s = new Vector2[mesh.vertices.Length];
			mesh.uv4 = zeroedUV4s;
			return zeroedUV4s;
		}
		/// <summary>
		/// Sets the colors property of a mesh to neutral gray.
		/// </summary>
		/// <returns>Array of color32 set to black.</returns>
		/// <param name="mesh">Mesh to apply the color to.</param>
		public Color[] SetBlackColor (Mesh mesh) {
			Color[] grays = new Color[mesh.vertices.Length];
			for (int i = 0; i < grays.Length; i++) {
				grays[i] = new Color (0, 0, 0, 1f);
			}
			mesh.colors = grays;
			return grays;
		}
		/// <summary>
		/// Sets the colors property of a mesh to white.
		/// </summary>
		/// <returns>Array of color32 set to white.</returns>
		/// <param name="mesh">Mesh to apply the color to.</param>
		public Color[] SetWhiteColor (Mesh mesh) {
			Color[] colors = new Color[mesh.vertices.Length];
			for (int i = 0; i < colors.Length; i++) {
				colors[i] = new Color (1f, 1f, 1f, 1f);
			}
			mesh.colors = colors;
			return colors;
		}
		#endregion

		#region Tangents
		/// <summary>
		/// Recalculates tangents for a mesh.
		/// </summary>
		/// <param name="mesh">Mesh.</param>
		public void RecalculateTangents(Mesh mesh)
		{
			int triangleCount = mesh.triangles.Length;
			int vertexCount = mesh.vertices.Length;

			Vector3[] tan1 = new Vector3[vertexCount];
			Vector3[] tan2 = new Vector3[vertexCount];
			Vector4[] tangents = new Vector4[vertexCount];
			for(long a = 0; a < triangleCount; a+=3)
			{
				long i1 = mesh.triangles[a+0];
				long i2 = mesh.triangles[a+1];
				long i3 = mesh.triangles[a+2];
				Vector3 v1 = mesh.vertices[i1];
				Vector3 v2 = mesh.vertices[i2];
				Vector3 v3 = mesh.vertices[i3];
				Vector2 w1 = mesh.uv[i1];
				Vector2 w2 = mesh.uv[i2];
				Vector2 w3 = mesh.uv[i3];
				float x1 = v2.x - v1.x;
				float x2 = v3.x - v1.x;
				float y1 = v2.y - v1.y;
				float y2 = v3.y - v1.y;
				float z1 = v2.z - v1.z;
				float z2 = v3.z - v1.z;
				float s1 = w2.x - w1.x;
				float s2 = w3.x - w1.x;
				float t1 = w2.y - w1.y;
				float t2 = w3.y - w1.y;
				float r = 1.0f / (s1 * t2 - s2 * t1);
				Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
				Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);
				tan1[i1] += sdir;
				tan1[i2] += sdir;
				tan1[i3] += sdir;
				tan2[i1] += tdir;
				tan2[i2] += tdir;
				tan2[i3] += tdir;
			}
			for (long a = 0; a < vertexCount; ++a)
			{
				Vector3 n = mesh.normals[a];
				Vector3 t = tan1[a];
				Vector3 tmp = (t - n * Vector3.Dot(n, t)).normalized;
				tangents[a] = new Vector4(tmp.x, tmp.y, tmp.z);
				tangents[a].w = (Vector3.Dot(Vector3.Cross(n, t), tan2[a]) < 0.0f) ? -1.0f : 1.0f;
			}
			mesh.tangents = tangents;
		}
		/// <summary>
		/// Set the mesh tangents to zero.
		/// </summary>
		/// <param name="mesh">Mesh.</param>
		public void TangentsToZero (Mesh mesh) {
			int i = mesh.vertices.Length;
			Vector4[] tangents = new Vector4[i];
			for (int j = 0; j < i; j++) {
				tangents[j] = new Vector4 (1, 0, 0, 0);
			}
			mesh.tangents = tangents;
		}
		#endregion
	}
}