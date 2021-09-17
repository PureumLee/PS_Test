using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Broccoli.Model;

namespace Broccoli.Builder
{
	/// <summary>
	/// Provides methods to set uv, uv2 and color32 values on branch meshes.
	/// </summary>
	public class StylizedMetaBuilder {
		#region Vars
		public int gridSize = 2;
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
		/// The vertex information for setting UV values.
		/// </summary>
		Dictionary<int, List<BranchMeshBuilder.VertexInfo>> vertexInfos;
		/// <summary>
		/// The triangle information for setting UV values.
		/// </summary>
		Dictionary<int, List<BranchMeshBuilder.TriangleInfo>> triangleInfos;
		/// <summary>
		/// Max girth found at the tree.
		/// </summary>
		//private float maxGirth = 1f;
		#endregion

		#region Singleton
		/// <summary>
		/// This class singleton instance.
		/// </summary>
		static StylizedMetaBuilder _stylizedMetaBuilder = null;
		/// <summary>
		/// Gets the singleton instance for this class.
		/// </summary>
		/// <returns>The instance.</returns>
		public static StylizedMetaBuilder GetInstance () {
			if (_stylizedMetaBuilder == null) {
				_stylizedMetaBuilder = new StylizedMetaBuilder ();
			}
			return _stylizedMetaBuilder;
		}
		#endregion

		#region UV Methods
		/// <summary>
		/// Sets UV values for a tree.
		/// </summary>
		/// <returns>Array of assigned UV values.</returns>
		/// <param name="mesh">Mesh.</param>
		/// <param name="tree">Tree.</param>
		/// <param name="refVertexInfos">Reference vertex infos.</param>
		public Vector2[] SetMeshUVs (Mesh mesh, 
			BroccoTree tree, 
			Dictionary<int, List<BranchMeshBuilder.VertexInfo>> refVertexInfos,
			Dictionary<int, List<BranchMeshBuilder.TriangleInfo>> refTriangleInfos) 
		{
			this.vertexInfos = refVertexInfos;
			this.triangleInfos = refTriangleInfos;
			Vector2[] uvs = new Vector2 [mesh.vertices.Length];
		
			if (mesh != null) {
				for (int i = 0; i < tree.branches.Count; i++) {
					if (vertexInfos.ContainsKey (tree.branches[i].id)) {
						SetBranchMeshUVs (ref uvs, tree.branches[i], 
							vertexInfos [tree.branches[i].id], 
							triangleInfos [tree.branches[i].id]);
					}
				}
				for (int i = 0; i < tree.branches.Count; i++) {
				}
				if (mesh.vertices.Length == uvs.Length) {
					mesh.uv = uvs;
				} else {
					Vector2[] extendedUVs = new Vector2[mesh.vertices.Length];
					uvs.CopyTo (extendedUVs, 0);
					mesh.uv = extendedUVs;
				}
			}
			this.vertexInfos = null;
			this.triangleInfos = null;
			return uvs;
		}
		/// <summary>
		/// Sets UV values for a branch.
		/// </summary>
		/// <param name="uvs">UV values to fill.</param>
		/// <param name="branch">Branch.</param>
		/// <param name="branchVertexInfos">Branch vertex infos.</param>
		/// <param name="perimeterLength">Perimeter length of the max girth of the tree.</param>
		/// <param name="accumBranchLength">Accum branch length.</param>
		void SetBranchMeshUVs (ref Vector2[] uvs, 
			BroccoTree.Branch branch, 
			List<BranchMeshBuilder.VertexInfo> branchVertexInfos,
			List<BranchMeshBuilder.TriangleInfo> branchTriangleInfos) 
		{
			gridSize = 10;
			float column, row;
			int triangleIndexOffset = Random.Range (0, gridSize * gridSize);
			for (int i = 0; i < branchTriangleInfos.Count; i++) {
				BranchMeshBuilder.TriangleInfo triangleInfo = branchTriangleInfos [i];
				column = (triangleInfo.faceIndex + triangleIndexOffset) % gridSize;
				row = ((triangleInfo.faceIndex + triangleIndexOffset) / (float) gridSize);
				Debug.Log ("col: " + column + ", row: " + row);
				uvs [triangleInfo.firstVertexIndex] = new Vector2 (column, row);
				uvs [triangleInfo.secondVertexIndex] = new Vector2 (column, row);
				uvs [triangleInfo.thirdVertexIndex] = new Vector2 (column, row);
			}
			for (int i = 0; i < branch.branches.Count; i++) {
				if (vertexInfos.ContainsKey (branch.branches[i].id)) {
					SetBranchMeshUVs (ref uvs, 
						branch.branches[i], 
						vertexInfos [branch.branches[i].id],
						triangleInfos [branch.branches[i].id]);
				}
			}
			/*
			for (int i = 0; i < branchVertexInfos.Count; i ++) {
				if (branchVertexInfos[i].radiusIndex == 0) {
					if (isGirthSensitive && girthSensitivity > 0) {
						girth = branchVertexInfos[i].girth;
						if (girth < 0.3f) {
							girth = 0.3f;
						}
						localAccumBranchLength += 
							(accumBranchLength + branchVertexInfos[i].position * branch.length / girth / maxGirth / girthSensitivity) - lastLength;
						lastLength = localAccumBranchLength;
					} else {
						localAccumBranchLength += (accumBranchLength + branchVertexInfos[i].position * branch.length) - lastLength;
						lastLength = localAccumBranchLength;
					}
					displacementX = 
						displacementDeltaX * localAccumBranchLength / perimeterLength;
					vPosition = localAccumBranchLength / perimeterLength;
				}
				uvs [branchVertexInfos[i].vertexIndex] = new Vector2 (
					GetUPositionValue (branchVertexInfos[i].radiusPosition, branchVertexInfos[i].polygonSides, displacementX),
					vPosition);
			}
			*/
			/*
			float displacementX = 0f;
			float vPosition = 0f;
			float girth = 0f;
			float lastLength = accumBranchLength;
			float localAccumBranchLength = accumBranchLength;
			for (int i = 0; i < branchVertexInfos.Count; i ++) {
				if (branchVertexInfos[i].radiusIndex == 0) {
					if (isGirthSensitive && girthSensitivity > 0) {
						girth = branchVertexInfos[i].girth;
						if (girth < 0.3f) {
							girth = 0.3f;
						}
						localAccumBranchLength += 
							(accumBranchLength + branchVertexInfos[i].position * branch.length / girth / maxGirth / girthSensitivity) - lastLength;
						lastLength = localAccumBranchLength;
					} else {
						localAccumBranchLength += (accumBranchLength + branchVertexInfos[i].position * branch.length) - lastLength;
						lastLength = localAccumBranchLength;
					}
					displacementX = 
						displacementDeltaX * localAccumBranchLength / perimeterLength;
					vPosition = localAccumBranchLength / perimeterLength;
				}
				uvs [branchVertexInfos[i].vertexIndex] = new Vector2 (
					GetUPositionValue (branchVertexInfos[i].radiusPosition, branchVertexInfos[i].polygonSides, displacementX),
					vPosition);
			}
			for (int i = 0; i < branch.branches.Count; i++) {
				if (vertexInfos.ContainsKey (branch.branches[i].id)) {
					if (branch.branches[i] == branch.followUp) {
						SetMeshUVs (uvs, branch.branches[i], vertexInfos [branch.branches[i].id], 
							perimeterLength, localAccumBranchLength);
					} else {
						float uvMappingOffset = 0f;
						if (applyMappingOffsetFromParent) {
							uvMappingOffset = lastLength + ((localAccumBranchLength - lastLength) * branch.branches[i].position);
						}
						SetMeshUVs (uvs, branch.branches[i], vertexInfos [branch.branches[i].id], 
							perimeterLength, uvMappingOffset); 
					}
				}
			}
			*/
		}
		public void SetMeshUV3s (Mesh mesh, 
			BroccoTree tree, 
			Dictionary<int, List<BranchMeshBuilder.VertexInfo>> refVertexInfos) 
		{
			this.vertexInfos = refVertexInfos;
			Vector4[] _uv3s = new Vector4[mesh.vertices.Length];
			Vector3[] _vertices = mesh.vertices;
			List<Vector4> uv3s = new List<Vector4> (_uv3s);
			if (mesh != null) {
				for (int i = 0; i < _uv3s.Length; i++) {
					uv3s[i] = new Vector4 (_vertices[i].x, _vertices[i].y, _vertices[i].z, 0.25f);
				}
			}
			mesh.SetUVs (2, uv3s);
			this.vertexInfos = null;
		}
		/// <summary>
		/// Sets UV3 values for a tree.
		/// </summary>
		/// <returns>Array of assigned UV values.</returns>
		/// <param name="mesh">Mesh.</param>
		/// <param name="tree">Tree.</param>
		/// <param name="refVertexInfos">Reference vertex infos.</param>
		public void SetMeshUV5s (Mesh mesh, 
			BroccoTree tree, 
			Dictionary<int, List<BranchMeshBuilder.VertexInfo>> refVertexInfos) 
		{
			#if UNITY_2018_2_OR_NEWER
			this.vertexInfos = refVertexInfos;
			Vector4[] _uv5s = new Vector4[mesh.vertices.Length];
			List<Vector4> uv5s = new List<Vector4> (_uv5s);
			if (mesh != null) {
				for (int h = 0; h < tree.branches.Count; h++) {
					if (vertexInfos.ContainsKey (tree.branches[h].id)) {
						SetMeshUV5s (uv5s, tree.branches[h]);
					}
				}
			}
			mesh.SetUVs (4, uv5s);
			this.vertexInfos = null;
			#endif
		}
		void SetMeshUV5s (List<Vector4> uv5s, 
			BroccoTree.Branch branch) 
		{
			/*
			if (vertexInfos.ContainsKey (branch.id)) {
				for (int i = 0; i < vertexInfos[branch.id].Count; i++) {
					uv5s [vertexInfos[branch.id][i].vertexIndex] = new Vector4 (branch.id, branch.helperStructureLevelId, branch.isTuned?1:0, 0);
				}
				for (int h = 0; h < branch.branches.Count; h++) {
					SetMeshUV5s (uv5s, branch.branches[h]);
				}
			}
			*/
			// REFACTOR
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