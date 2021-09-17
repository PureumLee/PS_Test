using System.Collections.Generic;

using UnityEngine;

using Broccoli.Model;
using Broccoli.Manager;

namespace Broccoli.Builder
{
	/// <summary>
	/// Wind meta builder.
	/// Analyzes trees to provide wind weight based on UV2 and colors channels.
	/// </summary>
	public class STWindMetaBuilder {
		#region CompoundBranch class
		/// <summary>
		/// The CompoundBranch class keeps record and calculates distance to tree
		/// origin for a series of followup branches on the tree (parent branch -> follow up branch).
		/// </summary>
		class CompoundBranch {
			#region Vars
			/// <summary>
			/// Identifier for the compound.
			/// </summary>
			public int id = -1;
			/// <summary>
			/// Identifier for the swing phase on a group of branches.
			/// </summary>
			public int phaseGroupId = -1;
			public float phase = 0f;
			/// <summary>
			/// The branches on the compound.
			/// </summary>
			public Dictionary<int, BroccoTree.Branch> branches = new Dictionary<int, BroccoTree.Branch> ();
			/// <summary>
			/// Relationship between a member branch and its distance 
			/// to the origin of the compound branch.
			/// </summary>
			public Dictionary<int, float> localLength = new Dictionary<int, float> ();
			/// <summary>
			/// Relationship between a member branch and its distance to the tree origin.
			/// </summary>
			public Dictionary<int, float> lengthsFromTreeOrigin = new Dictionary<int, float> ();
			/// <summary>
			/// Sum of the lengths of the member branches.
			/// </summary>
			public float length = 0f;
			/// <summary>
			/// Only the trunk branches of the tree are main compounds.
			/// </summary>
			public bool isMain = false;
			/// <summary>
			/// Length on the main (trunk) compound where this compounds or parent
			/// compound comes from.
			/// </summary>
			public float lengthFromMain = 0f;
			#endregion

			#region Constructor
			/// <summary>
			/// Initializes a new instance of the <see cref="Broccoli.Builder.STWindMetaBuilder+CompoundBranch"/> class.
			/// </summary>
			/// <param name="originBranch">Origin branch for the compound.</param>
			public CompoundBranch (BroccoTree.Branch originBranch) {
				id = originBranch.id;
				branches.Add (id, originBranch);
				localLength.Add (id, 0f);
				length = originBranch.length;
			}
			#endregion

			#region Ops
			/// <summary>
			/// Adds a branch to the compound.
			/// </summary>
			/// <returns><c>true</c>, if branch was added, <c>false</c> otherwise.</returns>
			/// <param name="followUpBranch">Follow up branch to add.</param>
			public bool AddBranch (BroccoTree.Branch followUpBranch) {
				if (!branches.ContainsKey (followUpBranch.id)) {
					branches.Add (followUpBranch.id, followUpBranch);
					localLength.Add (followUpBranch.id, length);
					length += followUpBranch.length;
					return true;
				}
				return false;
			}
			/// <summary>
			/// Gets the length from the compound origin to a position on the member branch.
			/// </summary>
			/// <returns>The local length.</returns>
			/// <param name="branch">Member branch.</param>
			/// <param name="position">Position on member branch.</param>
			public float GetLocalLength (BroccoTree.Branch branch, float position = 0f) {
				if (branches.ContainsKey (branch.id)) {
					float branchLength = localLength [branch.id];
					if (position > 0f) {
						branchLength += branch.length * position;
					}
					return branchLength;
				}
				return 0f;
			}
			/// <summary>
			/// Gets the length from tree origin to a position on the member branch.
			/// </summary>
			/// <returns>The length from tree origin.</returns>
			/// <param name="branch">Member branch.</param>
			/// <param name="position">Position on member branch.</param>
			public float GetLengthFromTreeOrigin (BroccoTree.Branch branch, float position = 0f) {
				if (branches.ContainsKey (branch.id)) {
					float lengthFromOrigin = lengthsFromTreeOrigin [branch.id];
					if (position > 0f) {
						lengthFromOrigin += branch.length * position;
					}
					return lengthFromOrigin;
				}
				return 0f;
			}
			/// <summary>
			/// Gets the length from the main branch (trunk) to a position on the member branch.
			/// </summary>
			/// <returns>The length from main branch.</returns>
			/// <param name="branch">Member branch.</param>
			/// <param name="position">Position on member branch.</param>
			public float GetLengthFromMainBranch (BroccoTree.Branch branch, float position = 0f) {
				return GetLengthFromTreeOrigin (branch, position) - lengthFromMain;
			}
			/// <summary>
			/// Clear this instance.
			/// </summary>
			public void Clear () {
				branches.Clear ();
				localLength.Clear ();
				lengthsFromTreeOrigin.Clear ();
			}
			#endregion
		}
		#endregion

		#region Vars
		/// <summary>
		/// The compound branches.
		/// </summary>
		Dictionary<int, CompoundBranch> compoundBranches = new Dictionary<int, CompoundBranch> ();
		/// <summary>
		/// The branches on the analyzed tree.
		/// </summary>
		/// <typeparam name="int">Id of the branch.</typeparam>
		/// <typeparam name="BroccoTree.Branch">Branch.</typeparam>
		/// <returns>Branch given its id.</returns>
		public Dictionary<int, BroccoTree.Branch> branches = new Dictionary<int, BroccoTree.Branch> ();
		/// <summary>
		/// Relationship between all branche ids and the id of the compounds they belong to.
		/// </summary>
		Dictionary<int, int> branchIdToCompoundBranchId = new Dictionary<int, int> ();
		/// <summary>
		/// The color of the branch to.
		/// </summary>
		public Dictionary<int, Color> compoundBranchToColor = new Dictionary<int, Color> ();
		/// <summary>
		/// The maximum length found on the compound branches after analyzing the tree.
		/// </summary>
		float maxCompoundBranchLengthFromTreeOrigin = 0f;
		/// <summary>
		/// The maximum length found on the compound branches from the main trunk.
		/// </summary>
		float maxCompoundBranchLengthFromMain = 0f;
		/// <summary>
		/// The wind factor used to multiply the UV2 value.
		/// </summary>
		public float windSpread = 1f;
		/// <summary>
		/// The wind amplitude.
		/// </summary>
		float _windAmplitude = 0f;
		/// <summary>
		/// Gets or sets the wind resistance.
		/// </summary>
		/// <value>The wind resistance.</value>
		public float windAmplitude {
			get { return _windAmplitude; }
			set {
				weightCurve = AnimationCurve.EaseInOut (value, 0f, 1f, 1f);
				_windAmplitude = value;
			}
		}
		public float sproutTurbulence = 1f;
		public float sproutSway = 1f;
		/// <summary>
		/// The weight curve used to get the UV2 values for wind.
		/// </summary>
		public AnimationCurve weightCurve;
		public AnimationCurve weightSensibilityCurve = null;
		public AnimationCurve weightAngleCurve = null;
		public bool useMultiPhaseOnTrunk = true;
		public bool isST7 = false;
		/// <summary>
		/// True to apply wind mapping to roots.
		/// </summary>
		public bool applyToRoots = false;
		/// <summary>
		/// Relationship between branches given their id and the branch skin they belong to.
		/// </summary>
		/// <typeparam name="int">Branch id.</typeparam>
		/// <typeparam name="BranchMeshBuilder.BranchSkin">Branch skin instance.</typeparam>
		private Dictionary<int, BranchMeshBuilder.BranchSkin> _branchIdToBranchSkin = new Dictionary<int, BranchMeshBuilder.BranchSkin> ();
		#endregion

		#region Channel Vars
		/// <summary>
		/// The UVs (channel 0) on each meshId.
		/// </summary>
		Dictionary<int, List<Vector4>> meshIdToUV = new Dictionary<int, List<Vector4>> ();
		/// <summary>
		/// The UV2s (channel 1) on each meshId.
		/// </summary>
		Dictionary<int, List<Vector4>> meshIdToUV2 = new Dictionary<int, List<Vector4>> ();
		/// <summary>
		/// The UV3s (channel 2) on each meshId.
		/// </summary>
		Dictionary<int, List<Vector4>> meshIdToUV3 = new Dictionary<int, List<Vector4>> ();
		/// <summary>
		/// The UV4s (channel 3) on each meshId.
		/// </summary>
		Dictionary<int, List<Vector4>> meshIdToUV4 = new Dictionary<int, List<Vector4>> ();
		/// <summary>
		/// The Color channel on each meshId.
		/// </summary>
		Dictionary<int, List<Color>> meshIdToColor = new Dictionary<int, List<Color>> ();
		#endregion

		#region Constructor
		/// <summary>
		/// Initializes a new instance of the <see cref="Broccoli.Builder.STWindMetaBuilder"/> class.
		/// </summary>
		public STWindMetaBuilder () {
			weightSensibilityCurve = new AnimationCurve ();
			weightSensibilityCurve.AddKey (new Keyframe ());
			weightSensibilityCurve.AddKey (new Keyframe (1, 1, 2, 2));
		}
		#endregion

		#region Analyze
		/// <summary>
		/// Analyzes the tree.
		/// Builds the compound branches and updates the lengths on them.
		/// </summary>
		/// <param name="tree">Tree.</param>
		public void AnalyzeTree (BroccoTree tree, List<BranchMeshBuilder.BranchSkin> branchSkins) {
			Clear ();
			BuildBranchIdToBranchSkin (branchSkins);
			for (int i = 0; i < tree.branches.Count; i++) {
				AnalyzeBranch (tree.branches[i]);
			}
			var enumerator = compoundBranches.GetEnumerator ();
			while (enumerator.MoveNext()) {
				var compoundBranchesPair = enumerator.Current;
				if (compoundBranchesPair.Value.length > maxCompoundBranchLengthFromTreeOrigin) {
					maxCompoundBranchLengthFromTreeOrigin = compoundBranchesPair.Value.length;
				}
			}
			SetTreeLengths (tree);
		}
		/// <summary>
		/// Analyzes a branch on the AnalyzeTree process.
		/// </summary>
		/// <param name="branch">Branch to analyze.</param>
		/// <param name="parentCompoundBranch">Parent compound branch.</param>
		/// <param name="baseGroupId">Identifier for a group of branches coming from a branch immediate to the trunk.</param>
		void AnalyzeBranch (BroccoTree.Branch branch, CompoundBranch parentCompoundBranch = null, int phaseGroupId = -1, float branchPhase = 0) {
			if (!branches.ContainsKey (branch.id)) {
				branches.Add (branch.id, branch);
			}
			if (parentCompoundBranch == null) {
				parentCompoundBranch = new CompoundBranch (branch);
				if (branch.parent == null) {
					parentCompoundBranch.isMain = true;
				}
				compoundBranches.Add (parentCompoundBranch.id, parentCompoundBranch);
			} else {
				parentCompoundBranch.AddBranch (branch);
			}
			if (!parentCompoundBranch.isMain && phaseGroupId == -1) {
				branchPhase = Random.Range (0f, 15f);
				phaseGroupId = branch.id;
			}
			parentCompoundBranch.phase = branchPhase;
			parentCompoundBranch.phaseGroupId = phaseGroupId;
			branchIdToCompoundBranchId.Add (branch.id, parentCompoundBranch.id);
			for (int i = 0; i < branch.branches.Count; i++) {
				if (!branch.branches[i].isRoot || (branch.branches[i].isRoot && applyToRoots)) {
					if (branch.branches[i].IsFollowUp ()) { // TODO: follow new phase!!
						if (useMultiPhaseOnTrunk && parentCompoundBranch.isMain) {
							AnalyzeBranch (branch.branches [i], null, phaseGroupId, branchPhase);
						} else {
							AnalyzeBranch (branch.branches [i], parentCompoundBranch, phaseGroupId, branchPhase);
						}
					} else {
						AnalyzeBranch (branch.branches[i], null, phaseGroupId, branchPhase);
					}
				}
			}
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
		/// Clears the compound branches.
		/// </summary>
		void ClearCompoundBranches () {
			var enumerator = compoundBranches.GetEnumerator ();
			while (enumerator.MoveNext ()) {
				enumerator.Current.Value.Clear ();
			}
			compoundBranches.Clear ();
			branchIdToCompoundBranchId.Clear ();
			compoundBranchToColor.Clear ();
			maxCompoundBranchLengthFromTreeOrigin = 0f;
			maxCompoundBranchLengthFromMain = 0f;
		}
		/// <summary>
		/// Gets a compound branch given a member branch id.
		/// </summary>
		/// <returns>The compound branch if the id is found to be a member, null otherwise.</returns>
		/// <param name="branchId">Branch identifier.</param>
		CompoundBranch GetCompoundBranch (int branchId) {
			if (branchIdToCompoundBranchId.ContainsKey (branchId)) {
				if (compoundBranches.ContainsKey (branchIdToCompoundBranchId [branchId])) {
					return compoundBranches [branchIdToCompoundBranchId [branchId]];
				}
			}
			return null;
		}
		/// <summary>
		/// Clear this instance.
		/// </summary>
		public void Clear () {
			branches.Clear ();
			_branchIdToBranchSkin.Clear ();
			ClearCompoundBranches ();
		}
		#endregion

		#region Lengths
		/// <summary>
		/// Sets the tree lengths on the AnalyzeTree process.
		/// </summary>
		/// <param name="tree">Tree.</param>
		void SetTreeLengths (BroccoTree tree) {
			for (int i = 0; i < tree.branches.Count; i++) {
				SetBranchLength (tree.branches[i]);
			}
		}
		/// <summary>
		/// Sets the length values for a member branch on the compound branch objects.
		/// </summary>
		/// <param name="branch">Member branch.</param>
		/// <param name="lengthFromOrigin">Accumulated length from origin.</param>
		/// <param name="lengthFromMain">Length from main.</param>
		void SetBranchLength (BroccoTree.Branch branch, float lengthFromOrigin = 0f, float lengthFromMain = 0f) {
			CompoundBranch compoundBranch = GetCompoundBranch (branch.id);
			if (compoundBranch != null) {
				// No a follow-up branch, then is the base of the compoundbranch.
				//if (!branch.IsFollowUp () || branch.parent.parent == null) {
					BroccoTree.Branch evalBranch = branch;
					float baseLengthFromOrigin;
					while (evalBranch != null) {
						baseLengthFromOrigin = lengthFromOrigin + compoundBranch.GetLocalLength (evalBranch);
						if (!compoundBranch.lengthsFromTreeOrigin.ContainsKey (evalBranch.id)) {
							compoundBranch.lengthsFromTreeOrigin.Add (evalBranch.id, baseLengthFromOrigin);
						}
						compoundBranch.lengthFromMain = lengthFromMain;
						evalBranch = evalBranch.followUp;
					}
				//}
				for (int i = 0; i < branch.branches.Count; i++) {
					if (compoundBranch.isMain) {
						SetBranchLength (branch.branches[i], 
							compoundBranch.GetLocalLength (branch, branch.branches[i].position),
							compoundBranch.GetLocalLength (branch, branch.branches[i].position));
					} else {
						SetBranchLength (branch.branches[i],
							compoundBranch.GetLengthFromTreeOrigin (branch, branch.branches[i].position),
							lengthFromMain);
					}
					if (lengthFromMain + branch.length > maxCompoundBranchLengthFromMain + branch.length) {
						maxCompoundBranchLengthFromMain = lengthFromMain + branch.length;
					}
				}
			}
		}
		#endregion

		#region Branch Mesh
		public void SetBranchesWindData (
			BroccoTree tree,
			int branchMeshId,
			Mesh branchMesh)
		{
			if (!meshIdToUV.ContainsKey (branchMeshId)) {
				meshIdToUV.Add (branchMeshId, new List<Vector4>());
			}
			if (!meshIdToUV2.ContainsKey (branchMeshId)) {
				meshIdToUV2.Add (branchMeshId, new List<Vector4>());
			}
			if (!meshIdToUV3.ContainsKey (branchMeshId)) {
				meshIdToUV3.Add (branchMeshId, new List<Vector4>());
			}
			if (!meshIdToUV4.ContainsKey (branchMeshId)) {
				meshIdToUV4.Add (branchMeshId, new List<Vector4>());
			}
			if (!meshIdToColor.ContainsKey (branchMeshId)) {
				meshIdToColor.Add (branchMeshId, new List<Color>());
			}
			branchMesh.GetUVs (0, meshIdToUV [branchMeshId]);
			branchMesh.GetUVs (1, meshIdToUV2 [branchMeshId]);
			branchMesh.GetUVs (2, meshIdToUV3 [branchMeshId]);
			branchMesh.GetUVs (3, meshIdToUV4 [branchMeshId]);
			branchMesh.GetColors (meshIdToColor [branchMeshId]);

			List<Vector3> _vertices = new List<Vector3> ();
			branchMesh.GetVertices (_vertices);
			List<Vector4> _uvs = meshIdToUV [branchMeshId];
			List<Vector4> _uv2s = meshIdToUV2 [branchMeshId];
			List<Vector4> _uv3s = meshIdToUV3 [branchMeshId];
			List<Vector4> _uv4s = meshIdToUV4 [branchMeshId];
			List<Color> _colors = meshIdToColor [branchMeshId];

			for (int i = 0; i < tree.branches.Count; i++) {
				SetBranchWindData (
					ref _vertices, ref _uvs, ref _uv2s, ref _uv3s, ref _uv4s, ref _colors, tree.branches [i]);
			}

			branchMesh.SetUVs (0, meshIdToUV[branchMeshId]);
			branchMesh.SetUVs (1, meshIdToUV2[branchMeshId]);
			branchMesh.SetUVs (2, meshIdToUV3[branchMeshId]);
			branchMesh.SetUVs (3, meshIdToUV4[branchMeshId]);
			branchMesh.SetColors (meshIdToColor [branchMeshId]);
		}
		/// <summary>
		/// Sets the branch wind weight.
		/// </summary>
		/// <param name="uv2s">UV2 array.</param>
		/// <param name="colors">Colors.</param>
		/// <param name="branch">Branch.</param>
		/// <param name="branchVertexInfos">Branch vertex infos.</param>
		void SetBranchWindData (
			ref List<Vector3> localVertices,
			ref List<Vector4> localUVs, 
			ref List<Vector4> localUV2s, 
			ref List<Vector4> localUV3s, 
			ref List<Vector4> localUV4s, 
			ref List<Color> localColors, 
			BroccoTree.Branch branch) 
		{
			Vector4 windUV = Vector4.zero;
			Vector4 windUV2 = Vector4.zero;
			Vector4 windUV3 = Vector4.zero;
			Vector4 windUV4 = Vector4.zero;

			// For each vertex on this branch.
			Vector3 positionInBranch = branch.GetPointAtPosition (0f);
			if (isST7) {
				windUV4 = new Vector4 (0, 0, 0, 1);
			} else {
				windUV2 = GetUV2ST8 (positionInBranch, 0, 0);
			}

			if (_branchIdToBranchSkin.ContainsKey (branch.id)) {
				BranchMeshBuilder.BranchSkin branchSkin = _branchIdToBranchSkin [branch.id];
				int startIndex, vertexCount, segmentIndex;
				branchSkin.GetVertexStartAndCount (branch.id, out startIndex, out vertexCount);
				for (int i = startIndex; i < startIndex + vertexCount; i++) {
					segmentIndex = branchSkin.vertexInfos [i].segmentIndex;
					windUV = GetUV (branch, branchSkin.positions [segmentIndex], localUVs[branchSkin.vertexOffset + i]);
					if (isST7) {
						windUV2 = GetUV2ST7 (localVertices [branchSkin.vertexOffset + i], 0f);
					} else {
						windUV3 = GetUV3ST8 (localVertices [branchSkin.vertexOffset + i], positionInBranch.z);
					}
					localUVs [branchSkin.vertexOffset + i] = windUV;
					localUV2s [branchSkin.vertexOffset + i] = windUV2;
					localUV3s [branchSkin.vertexOffset + i] = windUV3;
					localUV4s [branchSkin.vertexOffset + i] = windUV4;
					if (localColors.Count > 0) localColors [branchSkin.vertexOffset + i] = Color.white;
				}
			}

			// Set wind data to children banches
			for (int i = 0; i < branch.branches.Count; i++) {
				SetBranchWindData (
					ref localVertices, ref localUVs, ref localUV2s, ref localUV3s, ref localUV4s, ref localColors, branch.branches [i]);
			}
		}
		#endregion

		#region Sprout Mesh
		public void SetSproutsWindData (
			BroccoTree tree,
			int sproutMeshId,
			Mesh sproutMesh,
			List<MeshManager.MeshPart> meshParts)
		{
			if (!meshIdToUV.ContainsKey (sproutMeshId)) { // TODO: Only if ST8
				meshIdToUV.Add (sproutMeshId, new List<Vector4>());
			}
			if (!meshIdToUV2.ContainsKey (sproutMeshId)) {
				meshIdToUV2.Add (sproutMeshId, new List<Vector4>());
			}
			if (!meshIdToUV3.ContainsKey (sproutMeshId)) {
				meshIdToUV3.Add (sproutMeshId, new List<Vector4>());
			}
			if (!meshIdToUV4.ContainsKey (sproutMeshId)) {
				meshIdToUV4.Add (sproutMeshId, new List<Vector4>());
			}
			if (!meshIdToColor.ContainsKey (sproutMeshId)) {
				meshIdToColor.Add (sproutMeshId, new List<Color>());
			}
			sproutMesh.GetUVs (0, meshIdToUV [sproutMeshId]);
			sproutMesh.GetUVs (1, meshIdToUV2 [sproutMeshId]);
			sproutMesh.GetUVs (2, meshIdToUV3 [sproutMeshId]);
			if (meshIdToUV3 [sproutMeshId].Count == 0) {
				meshIdToUV3 [sproutMeshId] = new List<Vector4> (new Vector4[sproutMesh.vertices.Length]);
			}
			sproutMesh.GetUVs (3, meshIdToUV4 [sproutMeshId]);
			if (meshIdToUV4 [sproutMeshId].Count == 0) {
				meshIdToUV4 [sproutMeshId] = new List<Vector4> (new Vector4[sproutMesh.vertices.Length]);
			}
			sproutMesh.GetColors (meshIdToColor [sproutMeshId]);

			List<Vector3> _vertices = new List<Vector3> ();
				sproutMesh.GetVertices (_vertices);
				List<Vector4> _uvs = meshIdToUV [sproutMeshId];
				List<Vector4> _uv2s = meshIdToUV2 [sproutMeshId];
				List<Vector4> _uv3s = meshIdToUV3 [sproutMeshId];
				List<Vector4> _uv4s = meshIdToUV4 [sproutMeshId];
				List<Color> _colors = meshIdToColor [sproutMeshId];

			for (int i = 0; i < meshParts.Count; i++) {
				SetSproutWindData (ref _vertices, ref _uvs, ref _uv2s, ref _uv3s, ref _uv4s, ref _colors, meshParts[i]);
			}

			sproutMesh.SetUVs (0, meshIdToUV[sproutMeshId]);
			sproutMesh.SetUVs (1, meshIdToUV2[sproutMeshId]);
			sproutMesh.SetUVs (2, meshIdToUV3[sproutMeshId]);
			sproutMesh.SetUVs (3, meshIdToUV4[sproutMeshId]);
			//sproutMesh.SetColors (meshIdToColor [sproutMeshId]);
		}
		void SetSproutWindData (
			ref List<Vector3> localVertices,
			ref List<Vector4> localUVs, 
			ref List<Vector4> localUV2s, 
			ref List<Vector4> localUV3s, 
			ref List<Vector4> localUV4s, 
			ref List<Color> localColors,
			MeshManager.MeshPart meshPart) 
		{
			int index = 0;
			Vector3 originalVertex = Vector3.zero;
			Vector4 originalUV = Vector4.zero;
			Vector4 originalUV2 = Vector4.zero;
			Vector2 windUV = Vector2.zero;
			Vector4 windUV2 = Vector4.zero;
			Vector4 windUV3 = Vector4.zero;
			Vector4 windUV4 = Vector4.zero;

			// Called once per sprout, to get the UV values at a branch length.
			windUV = GetUV (branches [meshPart.branchId], meshPart.position, true);
			float sproutRandomValue = Random.Range (0f, 16f);
			for (int j = 0; j < meshPart.length; j++) {
				index = j + meshPart.startIndex;
				originalVertex = localVertices [index];
				originalUV = localUVs [index];
				originalUV2 = localUV2s [index];
				localUVs [index] = new Vector4 (originalUV.x, originalUV.y, windUV.x, windUV.y);
				if (isST7) {
					localUV2s [index] = GetUV2ST7 (meshPart.origin, originalUV.w * meshPart.origin.x);
					localUV3s [index] = GetUV3ST7 (meshPart.origin, sproutRandomValue, originalUV.w);
					localUV4s [index] = new Vector4 (originalVertex.y - meshPart.origin.y, originalUV.w * meshPart.origin.z, 0f, 1f);
				} else {
					localUV2s [index] = GetUV2ST8 (meshPart.origin, originalUV.z, originalUV.w);
					localUV3s [index] = GetUV3ST8 (originalVertex, meshPart.origin.z);
					localUV4s [index] = GetUV4ST8 (originalUV2.z, sproutRandomValue);
				}
				localColors [index] = GetColor (branches [meshPart.branchId], meshPart.position);
			}
		}
		#endregion

		#region Channels
        /// <summary>
		/// Gets the UV wind weight at a given branch position on the tree.
		/// </summary>
		/// <returns>The UV value.</returns>
		/// <param name="branch">Branch.</param>
		/// <param name="position">Position on the branch.</param>
		public Vector4 GetUV (BroccoTree.Branch branch, float position, Vector4 originalUV, bool isSprout = false) {
			Vector2 uv = GetUV (branch, position, isSprout);
			return new Vector4 (originalUV.x, originalUV.y, uv.x, uv.y);
		}
		public Vector2 GetUV (BroccoTree.Branch branch, float position, bool isSprout = false) {
			if (branchIdToCompoundBranchId.ContainsKey (branch.id)) {
				int compoundBranchId = branchIdToCompoundBranchId [branch.id];
				float branchPhase = compoundBranches [compoundBranchId].phase;
				if (compoundBranches.ContainsKey (branchIdToCompoundBranchId [branch.id])) {
					CompoundBranch compoundBranch = compoundBranches [branchIdToCompoundBranchId [branch.id]];
					float u, v;
					u = compoundBranch.GetLengthFromTreeOrigin (branch, position) / maxCompoundBranchLengthFromTreeOrigin;
					u *= weightCurve.Evaluate (u) * (useMultiPhaseOnTrunk?0.4f:0.7f) * windSpread;

					if (compoundBranch.isMain && useMultiPhaseOnTrunk) {
						v = 0;
					} else {
						v = compoundBranch.GetLengthFromMainBranch (branch, position) / maxCompoundBranchLengthFromTreeOrigin;
						v = weightSensibilityCurve.Evaluate (v) * (!useMultiPhaseOnTrunk?0.85f:0.4f) * windSpread;
					}
					//return new Vector2 (Mathf.Clamp01 (v * 5f), (isST7?branchPhase:compoundBranchId));
					return new Vector2 (Mathf.Clamp01 (v * 5f), branchPhase);
				}
			}
			return Vector2.zero;
		}
		public Vector4 GetUV2ST7 (Vector3 vertexPosition, float zPosition) {
			return new Vector4 (vertexPosition.x, vertexPosition.y, vertexPosition.z, zPosition);
		}
		public Vector4 GetUV2ST8 (Vector3 point, float u, float v) {
			return new Vector4 (u, v, point.x, point.y);
		}
		public Vector4 GetUV3ST7 (Vector3 point, float sproutValue, float v) {
			return new Vector4 (v * 0.5f * sproutTurbulence, sproutValue * sproutSway, Random.Range(2f, 15f) * 0f, 0f);
		}
		public Vector4 GetUV3ST8 (Vector3 vertexPosition, float zPosition) {
			return new Vector4 (vertexPosition.x, vertexPosition.y * windAmplitude, vertexPosition.z, zPosition);
		}
		public Vector4 GetUV4ST8 (float xPosition, float yValue) {
			return new Vector4 (xPosition, yValue, 5, 2);
		}
		/// <summary>
		/// Gets the color wind weight at a given branch position on the tree.
		/// </summary>
		/// <returns>The color value.</returns>
		/// <param name="branch">Branch.</param>
		/// <param name="position">Position on the branch.</param>
		public Color GetColor (BroccoTree.Branch branch, float position) {
			return Color.white;
			/*
			if (branchIdToCompoundBranchId.ContainsKey (branch.id)) {
				if (compoundBranches.ContainsKey (branchIdToCompoundBranchId [branch.id])) {
					CompoundBranch compoundBranch = compoundBranches [branchIdToCompoundBranchId [branch.id]];
					if (!compoundBranchToColor.ContainsKey (compoundBranch.phaseGroupId)) {
						if (compoundBranch.phaseGroupId == -1) {
							if (!compoundBranchToColor.ContainsKey (compoundBranch.phaseGroupId)) {
								compoundBranchToColor.Add (compoundBranch.phaseGroupId, new Color (0f, 0f, 0f, 0f));
							}
						} else {
							if (!compoundBranchToColor.ContainsKey (compoundBranch.phaseGroupId)) {
								compoundBranchToColor.Add (compoundBranch.phaseGroupId, new Color (Random.Range (0f, 1f), 0f, 0f, 0.5f));
							}
						}
					}
					return compoundBranchToColor [compoundBranch.phaseGroupId];
				}
			}
			return Color.black;
			*/
		}
		#endregion
	}
}
