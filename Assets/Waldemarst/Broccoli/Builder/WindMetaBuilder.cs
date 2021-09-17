using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Broccoli.Base;
using Broccoli.Model;

namespace Broccoli.Builder
{
	/// <summary>
	/// Wind meta builder.
	/// Analyzes trees to provide wind weight based on UV2 and colors channels.
	/// </summary>
	public class WindMetaBuilder {
		#region CompoundBranch class
		/// <summary>
		/// The CompoundBranch class keeps record and calculates distance to tree
		/// origin for a series of followup branches on the tree (parent branch -> follo up branch).
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
			/// <summary>
			/// True to apply wind mapping to roots.
			/// </summary>
			public bool applyToRoots = false;
			#endregion

			#region Constructor
			/// <summary>
			/// Initializes a new instance of the <see cref="Broccoli.Builder.WindMetaBuilder+CompoundBranch"/> class.
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
		/// The wind resistance.
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
		/// <summary>
		/// The weight curve used to get the UV2 values for wind.
		/// </summary>
		public AnimationCurve weightCurve;
		public AnimationCurve weightSensibilityCurve = null;
		public AnimationCurve weightAngleCurve = null;
		public bool useMultiPhaseOnTrunk = true;
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

		#region Constructor
		/// <summary>
		/// Initializes a new instance of the <see cref="Broccoli.Builder.WindMetaBuilder"/> class.
		/// </summary>
		public WindMetaBuilder () {
			weightSensibilityCurve = new AnimationCurve ();
			weightSensibilityCurve.AddKey (new Keyframe ());
			weightSensibilityCurve.AddKey (new Keyframe (1, 1, 2, 2));
		}
		#endregion

		#region Compound Branches
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
		void AnalyzeBranch (BroccoTree.Branch branch, CompoundBranch parentCompoundBranch = null, int phaseGroupId = -1) {
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
				phaseGroupId = branch.id;
			}
			parentCompoundBranch.phaseGroupId = phaseGroupId;
			branchIdToCompoundBranchId.Add (branch.id, parentCompoundBranch.id);
			for (int i = 0; i < branch.branches.Count; i++) {
				if (!branch.branches[i].isRoot || (branch.branches[i].isRoot && applyToRoots)) {
					if (branch.branches[i].IsFollowUp ()) { // TODO: follow new phase!!
						if (useMultiPhaseOnTrunk && parentCompoundBranch.isMain) {
							AnalyzeBranch (branch.branches [i], null, phaseGroupId);
						} else {
							AnalyzeBranch (branch.branches [i], parentCompoundBranch, phaseGroupId);
						}
					} else {
						AnalyzeBranch (branch.branches[i], null, phaseGroupId);
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
		/// Return the BranchSkin instance a branch belongs to.
		/// </summary>
		/// <param name="branchId">Branch identifier.</param>
		/// <returns>BranchSkin instance or null if none is found.</returns>
		public BranchMeshBuilder.BranchSkin GetBranchSkin (int branchId) {
			if (_branchIdToBranchSkin.ContainsKey (branchId)) {
				return _branchIdToBranchSkin [branchId];
			}
			return null;
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
			ClearCompoundBranches ();
			_branchIdToBranchSkin.Clear ();
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

		#region UV2 and Color
		/// <summary>
		/// Gets the UV2 wind weight at a given branch position on the tree.
		/// </summary>
		/// <returns>The UV2 value.</returns>
		/// <param name="branch">Branch.</param>
		/// <param name="position">Position on the branch.</param>
		public Vector4 GetUV2 (BroccoTree.Branch branch, float position) {
			if (branchIdToCompoundBranchId.ContainsKey (branch.id)) {
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

					return new Vector4 (u, v, 1, 1);
				}
			}
			return Vector4.zero;
		}
		/// <summary>
		/// Gets the color wind weight at a given branch position on the tree.
		/// </summary>
		/// <returns>The color value.</returns>
		/// <param name="branch">Branch.</param>
		/// <param name="position">Position on the branch.</param>
		public Color GetColor (BroccoTree.Branch branch, float position) {
			if (branchIdToCompoundBranchId.ContainsKey (branch.id)) {
				if (compoundBranches.ContainsKey (branchIdToCompoundBranchId [branch.id])) {
					CompoundBranch compoundBranch = compoundBranches [branchIdToCompoundBranchId [branch.id]];
					if (!compoundBranchToColor.ContainsKey (compoundBranch.phaseGroupId)) {
						if (compoundBranch.phaseGroupId == -1) {
							if (!compoundBranchToColor.ContainsKey (compoundBranch.phaseGroupId)) {
								compoundBranchToColor.Add (compoundBranch.phaseGroupId, new Color (0f, 0f, 0f, 1f));
							}
						} else {
							if (!compoundBranchToColor.ContainsKey (compoundBranch.phaseGroupId)) {
								compoundBranchToColor.Add (compoundBranch.phaseGroupId, new Color (Random.Range (0f, 1f), 0f, 0f, 1f));
							}
						}
					}
					return compoundBranchToColor [compoundBranch.phaseGroupId];
				}
			}
			return Color.black;
		}
		#endregion
	}
}
